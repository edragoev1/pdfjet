// pdfobj.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"fmt"
	"math"
	"runtime"
	"slices"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
)

// PDFobj is an object of a PDF that was read with PDF.Read, which holds the
// tokens of its dictionary and its stream. See Example_34, Example_48 and
// Example_50.
type PDFobj struct {
	number       int      // The object number
	offset       int      // The object offset
	end          int      // Where the object ends at the latest
	read         int      // The offset up to which its tokens were read
	dict         []string // The object dictionary
	streamOffset int      // The stream offset
	stream       []byte   // The compressed stream
	data         []byte   // The decompressed data
	undecoded    bool     // The stream is decoded when the data is first asked for
	budget       *decodeBudget
	gsNumber     int   // Graphics savedState Number
	root         bool  // The catalog that the trailer's /Root names
	font         *Font // A font added to the objects, filled in when they are added to a PDF
	// The /Resources a page inherits from the page tree, when it is a large
	// dictionary in a node: shared by the pages under the node, not copied
	// into each (see addInheritedEntries)
	inheritedResources []string
	inheritedFrom      int // The number of that node
	// The objects of the PDF it was read from, by number, for the masks of
	// an image copied into another PDF; nil for an object not read
	readObjects map[int]*PDFobj
	// The number in the PDF being written of an object that a page that was
	// read uses, like a stamp: this object stands for it among the objects,
	// and only Merge reads the number
	writtenNumber int
}

// maxDecodedTotal is what all the streams of one PDF may decode to together,
// 256 MiB: a PDF of a few megabytes can hold hundreds of streams that each
// decode to hundreds of megabytes, and each one alone is within the limit of
// a stream. A variable, for the tests.
var maxDecodedTotal = decompressor.MaxDecodedLength

// readPerByte is how many times the length of a PDF the reader may read in
// all to find the objects that its cross-reference sections list and the
// endstream of the streams whose /Length is wrong. Each of those reads ends
// where its object does, but a PDF can list one object many times, and the
// last object of a section ends at the end of the PDF.
const readPerByte = 8

// decodeBudget is what the streams of one PDF may still decode to, shared by
// its objects: each stream decoded takes its length from it. It also holds
// what the reader may still read of the PDF, readPerByte times its length,
// so that no PDF is read in more than linear time: thousands of streams with
// a wrong /Length and no endstream each searched to the end of the PDF.
type decodeBudget struct {
	left     int
	readLeft int
	// The objects of the object streams that take nothing of the budget,
	// one for each bytesPerFreeObject of the PDF, as many as a PDF of its
	// length can hold of its own; those past them take objectCost of it
	freeObjects int
}

// bytesPerFreeObject is the bytes of a PDF for each object of its object
// streams that takes nothing of the budget: each one took objectCost, and a
// real PDF of 5 MB of 1.1 million objects was refused (review of 12 October
// 2026, R32). A PDF of a few KB still makes no more than the budget's worth.
const bytesPerFreeObject = 8

func newDecodeBudget(size int) *decodeBudget {
	return &decodeBudget{left: maxDecodedTotal, readLeft: readPerByte * size,
		freeObjects: size / bytesPerFreeObject}
}

// errDecodedTotal is the error of a PDF whose streams decode to more than
// maxDecodedTotal together.
type errDecodedTotal struct{ limit int }

func (e errDecodedTotal) Error() string {
	return fmt.Sprintf("the streams of the PDF decode to more than %d bytes together", e.limit)
}

// newPDFobj creates an object with an empty dictionary.
func newPDFobj() *PDFobj {
	obj := new(PDFobj)
	obj.dict = make([]string, 0)
	obj.gsNumber = -1
	return obj
}

func (obj *PDFobj) add(token string) {
	obj.dict = append(obj.dict, token)
}

// GetNumber returns the object number.
func (obj *PDFobj) GetNumber() int {
	return obj.number
}

// GetDict returns the object dictionary.
func (obj *PDFobj) GetDict() []string {
	return obj.dict
}

// GetData returns the uncompressed stream data. A stream that is not a
// cross-reference or an object stream is decoded the first time its data is
// asked for, and the data is kept in the object: the first call changes the
// object, so two goroutines do not call it at once on the same object. The
// streams of one PDF that Read returns share a budget of 256 MiB of decoded
// data, maxDecodedTotal: a stream past what is left of it, or one that
// cannot be decoded, has no data, nil, as in the other ports.
func (obj *PDFobj) GetData() []byte {
	if obj.undecoded {
		// A stream that is not a cross-reference or an object stream is
		// decoded when its data is first asked for, not when the PDF is
		// read, so that reading a PDF of large images takes the memory of
		// none of them.
		obj.undecoded = false
		// Read recovers what decodeStream panics with, and GetData is
		// called after it, so a runtime error in a decoder is no data
		// here, as the other ports catch every exception: the result is
		// nil, as nothing was returned before the panic.
		defer func() {
			if r := recover(); r != nil {
				obj.data = nil
			}
		}()
		obj.data = obj.decodeStream()
	}
	return obj.data
}

// setStreamAndData copies the stream from the buffer, decrypts it when the
// PDF is encrypted, and decodes it with the filters of its /Filter entry. The
// decrypted stream replaces the encrypted one, so that it can be copied.
func (obj *PDFobj) setStreamAndData(buf []byte, length int, dec *decryptor, budget *decodeBudget) *PDFobj {
	if obj.stream == nil {
		obj.budget = budget
		if actual := streamLength(buf, obj.streamOffset, length, obj.end, budget); actual != length {
			length = actual
			obj.setLength(length)
		}
		// A /Length the PDF does not have is checked before the stream is
		// made, so that a file of a few bytes that says its stream is a
		// gigabyte takes no memory.
		if length < 0 || obj.streamOffset < 0 || length > len(buf)-obj.streamOffset {
			panic("The stream of an object is not in the PDF.")
		}
		obj.stream = make([]byte, length)
		copy(obj.stream, buf[obj.streamOffset:obj.streamOffset+length])
		if dec != nil {
			obj.stream = dec.decryptStream(obj, obj.stream)
			obj.setLength(len(obj.stream))
		}
		if objType := obj.GetValue("/Type"); objType == "/XRef" || objType == "/ObjStm" {
			obj.data = obj.decodeStream() // The objects in it are read now
		} else {
			obj.undecoded = true
		}
	}
	return obj
}

// decodeStream returns the decoded stream. A cross-reference stream or an
// object stream that cannot be decoded panics, as the objects in it cannot be
// read. Any other stream that cannot be decoded, like an image or the content
// of a page that is cut short, has no data: the rest of the PDF is read, and a
// merge copies the stream as it is.
func (obj *PDFobj) decodeStream() (data []byte) {
	if objType := obj.GetValue("/Type"); objType == "/XRef" || objType == "/ObjStm" {
		return obj.decode(obj.stream)
	}
	defer func() {
		if r := recover(); r != nil {
			if _, ok := r.(runtime.Error); ok {
				panic(r)
			}
			data = nil
		}
	}()
	return obj.decode(obj.stream)
}

// streamLength returns the length of the stream that starts at the offset.
// It is the /Length when the endstream keyword follows it, as it must; when it
// does not -- a /Length that is missing, too short or too long -- the stream
// ends at the end of line before the next endstream, as MuPDF and pdf.js read
// it. A stream with no endstream after it keeps its /Length.
//
// The endstream is looked for before end, where the object ends at the
// latest, and within what is left of what the budget may read: a PDF of
// thousands of streams with a wrong /Length and no endstream was searched to
// its end for each of them. A search the budget does not finish keeps the
// /Length too.
func streamLength(buf []byte, offset, length, end int, budget *decodeBudget) int {
	if offset < 0 || offset > len(buf) || endstreamAfter(buf, offset, length, budget) != -1 {
		return length
	}
	end = max(min(end, len(buf), offset+budget.readLeft), offset)
	found := endstreamOf(buf, offset, end)
	if found == -1 {
		found = indexOf(buf, "endstream", offset, end)
	}
	if found == -1 {
		budget.readLeft -= end - offset
		return length
	}
	budget.readLeft -= found - offset
	if found > offset && buf[found-1] == '\n' {
		found--
	}
	if found > offset && buf[found-1] == '\r' {
		found--
	}
	return found - offset
}

// endstreamOf returns the offset of the endstream keyword that ends the stream
// that starts at from, before to, or -1: the first endstream that ends no
// stream nested in it, as the stream of an embedded PDF, unfiltered, holds
// objects and streams of its own, each a stream keyword after a dictionary and
// an endstream, and the first endstream would cut it short. The endstream is
// the stream's only when every object that started in the stream has ended:
// one inside an object still open is that object's, whose stream keyword was
// not found, and the search gives up, -1, for the caller to read the stream
// as before.
func endstreamOf(buf []byte, from, to int) int {
	to = min(to, len(buf))
	streams := 0
	objects := 0
	for i := max(from, 0); i < to; i++ {
		switch {
		case buf[i] == 's' && i+len("stream") <= to && startsWith(buf, i, "stream"):
			if i-3 >= from && startsWith(buf, i-3, "end") {
				if streams == 0 {
					if objects == 0 {
						return i - 3
					}
					return -1
				}
				streams--
			} else if opensStream(buf, i) {
				streams++
			}
		case buf[i] == 'e' && startsWith(buf, i, "endobj"):
			if objects > 0 {
				objects--
			}
		case buf[i] >= '0' && buf[i] <= '9' && isObjectStart(buf, i):
			objects++
			for i < to && buf[i] != 'j' { // Past the obj
				i++
			}
		}
	}
	return -1
}

// opensStream returns true when the stream keyword at the offset opens a
// stream: after the >> of its dictionary and white space, and before an end of
// line, as the syntax of a stream object has it.
func opensStream(buf []byte, off int) bool {
	after := off + len("stream")
	if after >= len(buf) || (buf[after] != '\r' && buf[after] != '\n') {
		return false
	}
	i := off - 1
	for i >= 0 && isWhiteSpace(int(buf[i])) {
		i--
	}
	return i >= 1 && buf[i] == '>' && buf[i-1] == '>'
}

// endobjAfter returns the offset after the endobj keyword that follows the
// endstream at the offset, after white space, or -1.
func endobjAfter(buf []byte, endstream int) int {
	i := endstream + len("endstream")
	for i < len(buf) && isWhiteSpace(int(buf[i])) {
		i++
	}
	if startsWith(buf, i, "endobj") {
		return i + len("endobj")
	}
	return -1
}

// endstreamAfter returns the offset of the endstream keyword that follows the
// /Length of the stream at the offset, after white space, or -1. The white
// space is taken from the budget, as streams whose /Length ends in the same
// run of white space each read it.
func endstreamAfter(buf []byte, offset, length int, budget *decodeBudget) int {
	if offset < 0 || length < 0 || length > len(buf)-offset {
		return -1
	}
	i := offset + length
	end := min(len(buf), i+max(budget.readLeft, 0))
	for i < end && isWhiteSpace(int(buf[i])) {
		i++
	}
	budget.readLeft -= i - (offset + length)
	if startsWith(buf, i, "endstream") {
		return i
	}
	return -1
}

// setLength sets the /Length of the stream, replacing a reference to the
// length, and adds it to a stream dictionary that has none.
func (obj *PDFobj) setLength(length int) {
	i := slices.Index(obj.dict, "/Length")
	if i == -1 {
		if open := slices.Index(obj.dict, "<<"); open != -1 {
			obj.dict = insertArrayAt(obj.dict, []string{"/Length", strconv.Itoa(length)}, open+1)
		}
		return
	}
	if i+1 >= len(obj.dict) {
		return
	}
	if i+3 < len(obj.dict) && obj.dict[i+3] == "R" {
		obj.dict = slices.Delete(obj.dict, i+2, i+4)
	}
	obj.dict[i+1] = strconv.Itoa(length)
}

// decode decodes the stream with each filter of its /Filter entry in turn. A
// filter that is not supported, like DCTDecode, ends the decoding, and the
// data is what the filters before it decoded. It panics if a Flate stream
// cannot be inflated, or a stream decodes to more than
// decompressor.MaxDecodedLength bytes.
func (obj *PDFobj) decode(stream []byte) []byte {
	decoded := stream
	for i, filter := range obj.getFilters() {
		switch filter {
		case "/Crypt":
			// What it does is done: decryptStream reads it (review of
			// 30 October 2026, B10)
		case "/FlateDecode", "/Fl":
			data, err := decompressor.InflateStream(decoded, obj.maxDecodedLength())
			obj.decoded(data, err)
			decoded = obj.applyDecodeParms(data, i)
		case "/LZWDecode", "/LZW":
			earlyChange := getDecodeParm(obj.getDecodeParms(i), "/EarlyChange", 1)
			data, err := decompressor.LZWDecodeWithEarlyChange(decoded, obj.maxDecodedLength(), earlyChange)
			obj.decoded(data, err)
			decoded = obj.applyDecodeParms(data, i)
		case "/ASCIIHexDecode", "/AHx":
			decoded = obj.decodedHex(decompressor.ASCIIHexDecode(decoded))
		case "/ASCII85Decode", "/A85":
			data, err := decompressor.ASCII85DecodeWithMaxLength(decoded, obj.maxDecodedLength())
			obj.decoded(data, err)
			decoded = data
		case "/RunLengthDecode", "/RL":
			data, err := decompressor.RunLengthDecodeWithMaxLength(decoded, obj.maxDecodedLength())
			obj.decoded(data, err)
			decoded = data
		default:
			return decoded
		}
	}
	return decoded
}

// maxDecodedLength is what a stream of the object may decode to: the limit
// of a stream, or what is left of the PDF's budget when that is less.
func (obj *PDFobj) maxDecodedLength() int {
	if obj.budget != nil && obj.budget.left < decompressor.MaxDecodedLength {
		return obj.budget.left
	}
	return decompressor.MaxDecodedLength
}

// decoded takes the decoded data from the PDF's budget, or panics with the
// error of the decoding: the budget's when the budget is what was passed.
func (obj *PDFobj) decoded(data []byte, err error) {
	if err != nil {
		if obj.budget != nil && obj.budget.left < decompressor.MaxDecodedLength &&
			strings.Contains(err.Error(), "decodes to more than") {
			panic(errDecodedTotal{maxDecodedTotal})
		}
		panic(err)
	}
	if obj.budget != nil {
		obj.budget.left = max(0, obj.budget.left-len(data))
	}
}

// decodedHex takes the output of ASCIIHexDecode from the PDF's budget, as the
// other filters' is (review of 11 October 2026, #32). ASCII85Decode stops at
// the budget itself, as a 'z' of it is four bytes.
func (obj *PDFobj) decodedHex(data []byte) []byte {
	if len(data) > obj.maxDecodedLength() {
		obj.decoded(nil, fmt.Errorf("the stream decodes to more than %d bytes", obj.maxDecodedLength()))
	}
	obj.decoded(data, nil)
	return data
}

// getFilters returns the filters of the stream, whose /Filter can be an object
// of its own, a name or an array: the data was the bytes as they are.
func (obj *PDFobj) getFilters() []string {
	i := slices.Index(obj.dict, "/Filter") + 1
	if i == 0 || !isObjectReference(obj.dict, i) {
		return obj.getValues("/Filter")
	}
	filters := make([]string, 0)
	number, _ := strconv.Atoi(obj.dict[i])
	if filter := obj.readObjects[number]; filter != nil {
		for _, token := range objectValueTokens(filter) {
			if token != "[" && token != "]" {
				filters = append(filters, token)
			}
		}
	}
	return filters
}

// hasIdentityCrypt returns true for a stream with a /Crypt filter that is the
// Identity one, whose name is the default: the stream is not encrypted.
func (obj *PDFobj) hasIdentityCrypt() bool {
	for i, filter := range obj.getFilters() {
		if filter == "/Crypt" {
			parms := obj.getDecodeParms(i)
			j := slices.Index(parms, "/Name")
			return j == -1 || tokenAt(parms, j+1) == "/Identity"
		}
	}
	return false
}

// getValues returns the elements of the array that is the value of the key,
// or the value itself when it is not an array.
func (obj *PDFobj) getValues(key string) []string {
	values := make([]string, 0)
	i := slices.Index(obj.dict, key) + 1
	if i == 0 || i >= len(obj.dict) {
		return values
	}
	if obj.dict[i] != "[" {
		return append(values, obj.dict[i])
	}
	for i++; i < len(obj.dict) && obj.dict[i] != "]"; i++ {
		values = append(values, obj.dict[i])
	}
	return values
}

// applyDecodeParms undoes the predictor in the parameters of the filter at the
// index, of an image too: the data of an image is its samples, which a copy
// of the image does not use, as it copies the stream.
func (obj *PDFobj) applyDecodeParms(decoded []byte, index int) []byte {
	parms := obj.getDecodeParms(index)
	return decompressor.ApplyPredictor(
		decoded,
		getDecodeParm(parms, "/Predictor", 1),
		getDecodeParm(parms, "/Colors", 1),
		getDecodeParm(parms, "/BitsPerComponent", 8),
		getDecodeParm(parms, "/Columns", 1))
}

// getDecodeParms returns the tokens of the parameters dictionary of the filter
// at the index. /DecodeParms is a dictionary for a single filter, or an array
// with a dictionary or null for each filter.
func (obj *PDFobj) getDecodeParms(index int) []string {
	dict := obj.dict
	i := slices.Index(dict, "/DecodeParms") + 1
	if i == 0 || i >= len(dict) {
		return nil
	}
	array := dict[i] == "["
	if array {
		i++
	}
	for element := 0; i < len(dict) && dict[i] != "]"; element++ {
		start := i
		if dict[i] == "<<" {
			level := 0
			for {
				token := dict[i]
				i++
				if token == "<<" {
					level++
				} else if token == ">>" {
					level--
				}
				if level <= 0 || i >= len(dict) {
					break
				}
			}
		} else if i+2 < len(dict) && dict[i+2] == "R" {
			i += 3 // A reference to a dictionary, which is not supported.
		} else {
			i++ // null
		}
		if element == index {
			if dict[start] == "<<" {
				return dict[start:i]
			}
			return nil
		}
		if !array {
			break
		}
	}
	return nil
}

// getDecodeParm returns the integer value of the key in the parameters
// dictionary. A value that is not a 32-bit integer gets the default, as in
// the Java and C# ports.
func getDecodeParm(parms []string, key string, defaultValue int) int {
	i := slices.Index(parms, key)
	if i != -1 && i+1 < len(parms) {
		if value, err := strconv.ParseInt(parms[i+1], 10, 32); err == nil {
			return int(value)
		}
	}
	return defaultValue
}

// setStream sets the object stream.
func (obj *PDFobj) setStream(stream []byte) *PDFobj {
	obj.stream = stream
	return obj
}

// setNumber sets the object number.
func (obj *PDFobj) setNumber(number int) *PDFobj {
	obj.number = number
	return obj
}

// GetValue returns the dictionary value for the specified key, or "" when the
// object has no such key. The key is an entry of the dictionary of the object,
// and not of a dictionary inside it: the /Type of a /Group before the /Type of
// a page is not the type of the page. A dictionary or an array that the PDF
// ends in the middle of is closed where it ends, since a PDF that was read can
// hold anything.
func (obj *PDFobj) GetValue(key string) string {
	open := slices.Index(obj.dict, "<<")
	if open == -1 {
		return ""
	}
	i := dictEntryIndex(obj.dict[open:], key)
	if i == -1 {
		return ""
	}
	i += open
	if i+1 >= len(obj.dict) {
		return ""
	}
	token := obj.dict[i+1]
	if token == "<<" {
		return obj.valueUpTo(i+2, ">>", "<< ")
	} else if token == "[" {
		return obj.valueUpTo(i+2, "]", "[ ")
	}
	return token
}

// valueTokens returns the tokens of the value of the key: one, a reference to
// an object, as 5 0 R, or an array or a dictionary with the arrays and
// dictionaries in it, from its opening token to its closing one.
func (obj *PDFobj) valueTokens(key string) []string {
	open := slices.Index(obj.dict, "<<")
	if open == -1 {
		return nil
	}
	i := dictEntryIndex(obj.dict[open:], key)
	if i == -1 {
		return nil
	}
	start := open + i + 1
	if start+2 < len(obj.dict) && obj.dict[start+2] == "R" {
		return obj.dict[start : start+3]
	}
	depth := 0
	for j := start; j < len(obj.dict); j++ {
		switch obj.dict[j] {
		case "[", "<<":
			depth++
		case "]", ">>":
			depth--
		}
		if depth <= 0 {
			return obj.dict[start : j+1]
		}
	}
	return obj.dict[start:]
}

// valueUpTo returns the tokens from the index up to the closing one, with the
// opening before them and the closing after them.
func (obj *PDFobj) valueUpTo(i int, closing, opening string) string {
	var sb strings.Builder
	sb.WriteString(opening)
	for i < len(obj.dict) && obj.dict[i] != closing {
		sb.WriteString(obj.dict[i])
		sb.WriteString(" ")
		i++
	}
	sb.WriteString(closing)
	return sb.String()
}

// getObjectNumbers returns the object numbers of the key, which is one
// reference or an array of them. A dictionary that ends in the middle of
// them, or a token that is not a number where one belongs, ends the list: a
// PDF that was read can hold anything.
// entryIndexOf returns the index of the key among this dictionary's own
// entries, not those of the dictionaries in it, or -1 when it has none.
func (obj *PDFobj) entryIndexOf(key string) int {
	open := slices.Index(obj.dict, "<<")
	if open == -1 {
		return slices.Index(obj.dict, key) // Tokens with no dictionary, as an object made by hand
	}
	i := dictEntryIndex(obj.dict[open:], key)
	if i == -1 {
		return -1
	}
	return i + open
}

func (obj *PDFobj) getObjectNumbers(key string) []int {
	numbers := make([]int, 0)
	// The key of this dictionary, not one of a dictionary in it: the
	// /Contents of an annotation of /Annots before the page's own was taken
	// for the page's (review of 17 October 2026)
	for i := obj.entryIndexOf(key); i >= 0 && i < len(obj.dict); i++ {
		token := obj.dict[i]
		if token == key {
			i++
			if i >= len(obj.dict) {
				break
			}
			str := obj.dict[i]
			if str == "[" {
				for {
					i++
					if i >= len(obj.dict) {
						break
					}
					str = obj.dict[i]
					if str == "]" {
						break
					}
					objNumber, err := strconv.Atoi(str)
					if err != nil {
						break
					}
					numbers = append(numbers, objNumber)
					i++ // 0
					i++ // R
				}
			} else if objNumber, err := strconv.Atoi(str); err == nil {
				numbers = append(numbers, objNumber)
			}
			break
		}
	}
	return numbers
}

// GetPageSize returns the width and height of the page, which its /MediaBox
// gives as the two corners of a rectangle, in either order. A page with no
// /MediaBox of its own, one that is not four numbers, or an empty one is
// letter size, as MuPDF and pdf.js draw it. PDF.GetPageObjects gives a page
// the box it inherits from the page tree, which a page of another program's
// PDF often does not carry itself, and the numbers of a box that is an object
// of its own or refers to them.
func (obj *PDFobj) GetPageSize() pagesize.PageSize {
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == "/MediaBox" {
			if tokenAt(obj.dict, i+1) != "[" {
				break
			}
			box := [4]float64{}
			for j := 0; j < 4; j++ {
				// Only a number in decimal, as in the four ports: Java read
				// 595f, and Go, Swift and Java hex (review of 17 October 2026)
				token := tokenAt(obj.dict, i+2+j)
				value, err := strconv.ParseFloat(token, 32)
				if err != nil || !isDecimalNumber(token) {
					return letter.Portrait()
				}
				box[j] = value
			}
			width, height := math.Abs(box[2]-box[0]), math.Abs(box[3]-box[1])
			// A box of a NaN or infinite size is no size, as one of 0 is not:
			// a page of NaN by 792 stopped Swift where it was watermarked
			// (review of 12 October 2026, K1).
			if !(width > 0 && height > 0) || math.IsInf(width, 0) || math.IsInf(height, 0) ||
				math.IsInf(float64(float32(width)), 0) || math.IsInf(float64(float32(height)), 0) {
				return letter.Portrait()
			}
			return pagesize.NewPageSize(float32(width), float32(height))
		}
	}
	return letter.Portrait()
}

// getLength returns the /Length of the stream. A /Length that is an object of
// its own is found by its number in numbered, which holds the newest version of
// each object: an object of a PDF that was updated is in it once, as the last
// one of its number.
func (obj *PDFobj) getLength(numbered map[int]*PDFobj) int {
	// The entry of the dictionary, and not a /Length inside another value or
	// that is the value of another entry, "/Height/Length", as pdf.js tests
	// it in issue19611.
	open := slices.Index(obj.dict, "<<")
	if open == -1 {
		return 0
	}
	i := dictEntryIndex(obj.dict[open:], "/Length")
	if i == -1 {
		return 0
	}
	i += open
	number := toLength(tokenAt(obj.dict, i+1))
	if i+2 >= len(obj.dict) {
		panic(errors.New("The dictionary ends after the /Length."))
	}
	if obj.dict[i+2] == "0" {
		if i+3 >= len(obj.dict) {
			panic(errors.New("The dictionary ends after the /Length."))
		}
		if obj.dict[i+3] == "R" {
			return getLengthFromObject(numbered, number)
		}
	}
	return number
}

// getLengthFromObject returns the /Length stored in the object with the number,
// or 0 when the PDF has no such object.
func getLengthFromObject(numbered map[int]*PDFobj, number int) int {
	obj, ok := numbered[number]
	if !ok {
		return 0
	}
	return toLength(tokenAt(obj.dict, 3))
}

// toLength returns the length that the token holds. A real number and null are
// other programs' ways to write it, and were refused: the stream ends at its
// endstream when the length is wrong, as with null, for which it is 0 (review
// of 30 October 2026, B11).
func toLength(token string) int {
	if token == "null" {
		return 0
	}
	if number, err := strconv.Atoi(token); err == nil {
		return number
	}
	value, err := strconv.ParseFloat(token, 64)
	if err != nil || !isDecimalNumber(token) || math.Abs(value) > math.MaxInt32 {
		panic(errors.New("The /Length of a stream is not a number."))
	}
	return int(value)
}

// GetContentObject returns the content object of the page, or nil if it has
// no contents. The content of a page can be split into several streams, listed
// in an array that is either in the page dictionary or an object of its own.
// Together they are one content stream, so they are returned joined in a new
// object, which is not in the objects list.
func (obj *PDFobj) GetContentObject(objects []*PDFobj) *PDFobj {
	numbers := obj.getObjectNumbers("/Contents")
	if len(numbers) == 1 {
		contents := objectNumbered(objects, numbers[0])
		if contents == nil {
			return nil
		}
		if contents.stream != nil {
			return contents
		}
		// "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
		numbers = make([]int, 0)
		i := slices.Index(contents.dict, "[")
		for i != -1 && i+3 < len(contents.dict) && contents.dict[i+3] == "R" {
			number, err := strconv.Atoi(contents.dict[i+1])
			if err != nil {
				break
			}
			numbers = append(numbers, number)
			i += 3
		}
	}
	if len(numbers) == 0 {
		return nil
	}
	if len(numbers) == 1 {
		return objectNumbered(objects, numbers[0])
	}
	content := newPDFobj()
	content.data = make([]byte, 0)
	// Each stream copied into the content is charged to the budget, as the
	// copy is memory of its own, which decoding the stream does not pay for:
	// /Contents [4 0 R 4 0 R ...] of a stream of 16 MB made gigabytes of a PDF
	// of a few KB (review of 11 October 2026, #29), and 64 pages of /Contents
	// [4 0 R 5 0 R] held 1 GB against the budget of 256 MB (review of
	// 12 October 2026, R16). Past the budget, the page has no content, as a
	// stream past it has no data.
	for _, number := range numbers {
		page := objectNumbered(objects, number)
		if page == nil {
			continue
		}
		if data := page.GetData(); data != nil {
			if page.budget != nil {
				page.budget.left -= len(data) + 1
				if page.budget.left < 0 {
					page.budget.left = 0
					return nil
				}
			}
			content.data = append(content.data, data...)
			content.data = append(content.data, '\n') // A stream can end in the middle of a line.
		}
	}
	return content
}

// GetResourcesObject returns the resources object of this page, or nil if it has none.
func (obj *PDFobj) GetResourcesObject(objects []*PDFobj) *PDFobj {
	if obj.inheritedResources != nil {
		// The dictionary the page shares with the others under its node, as
		// an object of its own, which is read and not changed
		resources := newPDFobj()
		resources.dict = obj.inheritedResources
		return resources
	}
	for i, token := range obj.dict {
		if token == "/Resources" {
			if i+1 >= len(obj.dict) {
				return nil
			}
			token = obj.dict[i+1]
			if token == "<<" {
				return obj
			}
			objNumber, err := strconv.Atoi(token)
			if err != nil {
				return nil
			}
			return objectNumbered(objects, objNumber)
		}
	}
	return nil
}

// objectNumbered returns the object with the number, or nil when the PDF that
// was read does not have one: a reference can name an object that is not in
// the file.
func objectNumbered(objects []*PDFobj, number int) *PDFobj {
	if number < 1 || number > len(objects) {
		return nil
	}
	return objects[number-1]
}

// tokenAt returns the token at the index, or an empty string when the
// dictionary of an object that was read ends before it.
func tokenAt(dict []string, index int) string {
	if index < 0 || index >= len(dict) {
		return ""
	}
	return dict[index]
}

// objectAt returns the object that the token names, or nil when the token is
// not the number of an object the PDF has: a page of a file that was changed
// can name an object that is not in it.
func objectAt(objects []*PDFobj, token string) *PDFobj {
	number, err := strconv.Atoi(token)
	if err != nil {
		return nil
	}
	return objectNumbered(objects, number)
}

// indexAt returns the index to insert at, which is the end of the dictionary
// when the index is past it.
func indexAt(dict []string, index int) int {
	if index < 0 {
		return 0
	}
	return min(index, len(dict))
}

// ownResources gives the page a copy of the /Resources it shares with the
// others under its node, before a resource is added to it, so that the
// resource is the page's alone, as it was when every page had a copy. A page
// with no /Resources gets an empty one: the fonts added to it were lost, with
// no error (review of 30 October 2026, B5).
func (obj *PDFobj) ownResources() {
	entry := append([]string{"/Resources"}, obj.inheritedResources...)
	if obj.inheritedResources == nil {
		if obj.entryIndexOf("/Resources") != -1 {
			return
		}
		entry = []string{"/Resources", "<<", ">>"}
	}
	if open := slices.Index(obj.dict, "<<"); open != -1 {
		obj.dict = insertArrayAt(obj.dict, entry, open+1)
	}
	obj.inheritedResources = nil
}

// AddCoreFontResource adds a core font to the resources of this page and returns it.
func (obj *PDFobj) AddCoreFontResource(coreFont *corefont.CoreFont, objects *[]*PDFobj) *Font {
	obj.ownResources()
	font := newCoreFontForPDFobj(coreFont)
	font.fontID = strings.ToUpper(strings.ReplaceAll(font.name, "-", "_"))
	obj2 := newPDFobj()
	obj2.dict = append(obj2.dict, "<<")
	obj2.dict = append(obj2.dict, "/Type")
	obj2.dict = append(obj2.dict, "/Font")
	obj2.dict = append(obj2.dict, "/Subtype")
	obj2.dict = append(obj2.dict, "/Type1")
	obj2.dict = append(obj2.dict, "/BaseFont")
	obj2.dict = append(obj2.dict, "/"+font.name)
	if font.name != "Symbol" && font.name != "ZapfDingbats" {
		obj2.dict = append(obj2.dict, "/Encoding")
		obj2.dict = append(obj2.dict, "/WinAnsiEncoding")
	}
	obj2.dict = append(obj2.dict, ">>")
	obj2.number = len(*objects) + 1
	*objects = append(*objects, obj2)

	// The first /Resources of the page is the one, and the font is added to
	// it once: adding it to the page again, after a resources object that
	// names the page itself has grown the page dictionary, never ended.
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == "/Resources" {
			token := tokenAt(obj.dict, i+1)
			if token == "<<" { // Direct resources object
				obj.addFontResource(obj, objects, font.fontID, obj2.number)
			} else if resources := objectAt(*objects, token); resources != nil {
				obj.addFontResource(resources, objects, font.fontID, obj2.number) // Indirect
			}
			break
		}
	}

	return font
}

// addFontResource adds font resource.
func (obj *PDFobj) addFontResource(obj2 *PDFobj, objects *[]*PDFobj, fontID string, number int) {
	fonts := false
	for _, token := range obj2.dict {
		if token == "/Font" {
			fonts = true
			break
		}
	}
	if !fonts {
		// Direct resources follow "/Resources <<" in the page dictionary; an
		// indirect resources object has no /Resources key, and its entries
		// follow its first "<<".
		i := slices.Index(obj2.dict, "/Resources")
		if i == -1 {
			i = slices.Index(obj2.dict, "<<") + 1
		} else {
			i += 2
		}
		obj2.dict = insertArrayAt(obj2.dict, []string{"/Font", "<<", ">>"}, indexAt(obj2.dict, i))
	}

	for i := 0; i < len(obj2.dict); i++ {
		if obj2.dict[i] == "/Font" {
			token := tokenAt(obj2.dict, i+1)
			if token == "<<" {
				obj2.dict = insertStringAt(obj2.dict, "/"+fontID, i+2)
				obj2.dict = insertStringAt(obj2.dict, strconv.Itoa(number), i+3)
				obj2.dict = insertStringAt(obj2.dict, "0", i+4)
				obj2.dict = insertStringAt(obj2.dict, "R", i+5)
				return
			} else if obj3 := objectAt(*objects, token); obj3 != nil {
				for j := 0; j < len(obj3.dict); j++ {
					if obj3.dict[j] == "<<" {
						obj3.dict = insertStringAt(obj3.dict, "/"+fontID, j+1)
						obj3.dict = insertStringAt(obj3.dict, strconv.Itoa(number), j+2)
						obj3.dict = insertStringAt(obj3.dict, "0", j+3)
						obj3.dict = insertStringAt(obj3.dict, "R", j+4)
						return
					}
				}
			}
		}
	}
}

func insertNewObject(dict, list []string, objType string) []string {
	for _, token := range dict {
		if token == list[0] {
			return dict
		}
	}
	for i := 0; i < len(dict); i++ {
		token := dict[i]
		if token == objType {
			return insertArrayAt(dict, list, indexAt(dict, i+2))
		}
	}
	if tokenAt(dict, 3) == "<<" {
		return insertArrayAt(dict, list, 4)
	}
	return dict
}

func addResource(objType string, obj *PDFobj, objects *[]*PDFobj, objNumber int) {
	tag := "/Im"
	if objType == "/Font" {
		tag = "/F"
	}

	number := strconv.Itoa(objNumber)
	list := []string{tag + number, number, "0", "R"}
	for i := 0; i < len(obj.dict); i++ {
		token := obj.dict[i]
		if token == objType {
			token = tokenAt(obj.dict, i+1)
			if token == "<<" {
				obj.dict = insertNewObject(obj.dict, list, objType)
			} else if obj2 := objectAt(*objects, token); obj2 != nil {
				obj2.dict = insertNewObject(obj2.dict, list, objType)
			}
			return
		}
	}

	// Handle the case where the page originally does not have any font resources.
	list = []string{objType, "<<", tag + number, number, "0", "R", ">>"}
	for i, token := range obj.dict {
		if token == "/Resources" {
			obj.dict = insertArrayAt(obj.dict, list, indexAt(obj.dict, i+2))
			return
		}
	}
	for i, token := range obj.dict {
		if token == "<<" {
			obj.dict = insertArrayAt(obj.dict, list, i+1)
			return
		}
	}
}

// AddImageResource adds an image resource.
func (obj *PDFobj) AddImageResource(image *Image, objects *[]*PDFobj) {
	obj.ownResources()
	for i, token := range obj.dict {
		if token == "/Resources" {
			token = tokenAt(obj.dict, i+1)
			if token == "<<" { // Direct resources object
				addResource("/XObject", obj, objects, image.objNumber)
			} else if resources := objectAt(*objects, token); resources != nil {
				addResource("/XObject", resources, objects, image.objNumber) // Indirect resources object
			}
			return
		}
	}
}

// AddFontResource adds font resource.
func (obj *PDFobj) AddFontResource(font *Font, objects *[]*PDFobj) {
	obj.ownResources()
	for i, token := range obj.dict {
		if token == "/Resources" {
			token = tokenAt(obj.dict, i+1)
			if token == "<<" { // Direct resources object
				addResource("/Font", obj, objects, font.objNumber)
			} else if resources := objectAt(*objects, token); resources != nil {
				addResource("/Font", resources, objects, font.objNumber) // Indirect resources object
			}
			return
		}
	}
}

// AddContent appends a content stream to the contents of this page.
func (obj *PDFobj) AddContent(content []byte, objects *[]*PDFobj) {
	obj2 := newPDFobj()
	obj2.setNumber(len(*objects) + 1)
	obj2.setStream(content)
	*objects = append(*objects, obj2)

	objNumber := strconv.Itoa(obj2.number)
	// The page's own /Contents, not an annotation's in it
	for i := obj.entryIndexOf("/Contents"); i >= 0 && i < len(obj.dict); i++ {
		if obj.dict[i] == "/Contents" {
			i++
			token := tokenAt(obj.dict, i)
			if token == "[" {
				// Array of content objects, which can end before its "]".
				for i++; i < len(obj.dict); i += 3 {
					if obj.dict[i] == "]" {
						obj.dict = insertStringAt(obj.dict, "R", i)
						obj.dict = insertStringAt(obj.dict, "0", i)
						obj.dict = insertStringAt(obj.dict, objNumber, i)
						return
					}
				}
				return
			} else {
				// Single content object
				obj3 := objectAt(*objects, token)
				if obj3 == nil {
					return
				}
				if obj3.data == nil && obj3.stream == nil {
					// This is not a stream object!
					for j := 0; j < len(obj3.dict); j++ {
						if obj3.dict[j] == "]" {
							obj3.dict = insertStringAt(obj3.dict, "R", j)
							obj3.dict = insertStringAt(obj3.dict, "0", j)
							obj3.dict = insertStringAt(obj3.dict, objNumber, j)
							return
						}
					}
				}
				if tokenAt(obj.dict, i+1) != "0" || tokenAt(obj.dict, i+2) != "R" {
					return // Not a whole "n 0 R" to put in an array.
				}
				obj.dict = insertStringAt(obj.dict, "[", i)
				obj.dict = insertStringAt(obj.dict, "]", i+4)
				obj.dict = insertStringAt(obj.dict, "R", i+4)
				obj.dict = insertStringAt(obj.dict, "0", i+4)
				obj.dict = insertStringAt(obj.dict, objNumber, i+4)
				return
			}
		}
	}
}

// wrapContents puts a content stream before the content streams of this page
// and another after them, in a /Contents array of its own, or gives a page
// that has none the second. It returns false when the /Contents is not a
// stream or an array of them, and changes nothing.
func (obj *PDFobj) wrapContents(before, after []byte, objects *[]*PDFobj) bool {
	refs, start, end, found := obj.contentRefs(*objects)
	if found && refs == nil {
		return false
	}
	addStream := func(content []byte) []string {
		stream := newPDFobj().setNumber(len(*objects) + 1).setStream(content)
		*objects = append(*objects, stream)
		return []string{strconv.Itoa(stream.number), "0", "R"}
	}
	if !found {
		// A page with no content: AddContent added nothing, and the drawing
		// was lost
		end := -1
		for i := len(obj.dict) - 1; i >= 0; i-- {
			if obj.dict[i] == ">>" {
				end = i
				break
			}
		}
		if end == -1 {
			return false
		}
		value := slices.Concat([]string{"/Contents", "["}, addStream(after), []string{"]"})
		obj.dict = slices.Concat(obj.dict[:end], value, obj.dict[end:])
		return true
	}
	value := slices.Concat([]string{"["}, addStream(before), refs, addStream(after), []string{"]"})
	obj.dict = slices.Concat(obj.dict[:start], value, obj.dict[end:])
	return true
}

// contentRefs returns the references of the content streams of this page,
// "n 0 R" each, and where the value of its /Contents is in its dictionary, from
// start to end; found is false for a page with no /Contents, and refs nil for
// a /Contents that is not a stream or an array of them.
func (obj *PDFobj) contentRefs(objects []*PDFobj) (refs []string, start, end int, found bool) {
	i := obj.entryIndexOf("/Contents")
	if i == -1 {
		return nil, 0, 0, false
	}
	start = i + 1
	array := func(tokens []string, from int) ([]string, int) {
		refs := []string{} // An empty array is valid, and not nil
		for j := from; j < len(tokens); j += 3 {
			if tokens[j] == "]" {
				return refs, j + 1
			}
			if !isObjectReference(tokens, j) {
				return nil, 0
			}
			refs = append(refs, tokens[j:j+3]...)
		}
		return nil, 0
	}
	if tokenAt(obj.dict, start) == "[" {
		refs, end = array(obj.dict, start+1)
		if refs == nil {
			return nil, 0, 0, true
		}
		return refs, start, end, true
	}
	if !isObjectReference(obj.dict, start) {
		return nil, 0, 0, true
	}
	end = start + 3
	contents := objectAt(objects, obj.dict[start])
	if contents == nil {
		return nil, 0, 0, true
	}
	if contents.data == nil && contents.stream == nil {
		// An array of streams that is an object of its own
		k := slices.Index(contents.dict, "[")
		if k == -1 {
			return nil, 0, 0, true
		}
		refs, _ = array(contents.dict, k+1)
		return refs, start, end, true
	}
	return obj.dict[start:end:end], start, end, true
}

// mediaBoxOrigin returns the lower left corner of the /MediaBox of this page,
// or 0, 0 when it has none that is four numbers.
func (obj *PDFobj) mediaBoxOrigin() (float32, float32) {
	i := slices.Index(obj.dict, "/MediaBox")
	if i == -1 || tokenAt(obj.dict, i+1) != "[" {
		return 0, 0
	}
	var box [4]float64
	for j := range box {
		token := tokenAt(obj.dict, i+2+j)
		value, err := strconv.ParseFloat(token, 32)
		if err != nil || !isDecimalNumber(token) || math.IsNaN(value) || math.IsInf(value, 0) {
			return 0, 0
		}
		box[j] = value
	}
	return float32(min(box[0], box[2])), float32(min(box[1], box[3]))
}

// AddPrefixContent adds a content stream before the existing content streams
// of this page. The original code was provided by Stefan Ostermann author of
// ScribMaster and HandWrite Pro. Additional code to handle PDFs with indirect
// array of stream objects was written by EDragoev.
func (obj *PDFobj) AddPrefixContent(content []byte, objects *[]*PDFobj) {
	obj2 := newPDFobj()
	obj2.setNumber(len(*objects) + 1)
	obj2.setStream(content)
	*objects = append(*objects, obj2)

	objNumber := strconv.Itoa(obj2.number)
	// The page's own /Contents, not an annotation's in it
	for i := obj.entryIndexOf("/Contents"); i >= 0 && i < len(obj.dict); i++ {
		if obj.dict[i] == "/Contents" {
			i++
			token := tokenAt(obj.dict, i)
			if token == "[" {
				// Array of content object streams
				i++
				obj.dict = insertStringAt(obj.dict, "R", i)
				obj.dict = insertStringAt(obj.dict, "0", i)
				obj.dict = insertStringAt(obj.dict, objNumber, i)
				return
			}
			// Single content object
			obj3 := objectAt(*objects, token)
			if obj3 == nil {
				return
			}
			if obj3.data == nil && obj3.stream == nil {
				// This is not a stream object!
				for j := 0; j < len(obj3.dict); j++ {
					if obj3.dict[j] == "[" {
						j++
						obj3.dict = insertStringAt(obj3.dict, "R", j)
						obj3.dict = insertStringAt(obj3.dict, "0", j)
						obj3.dict = insertStringAt(obj3.dict, objNumber, j)
						return
					}
				}
			}
			if tokenAt(obj.dict, i+1) != "0" || tokenAt(obj.dict, i+2) != "R" {
				return // Not a whole "n 0 R" to put in an array.
			}
			obj.dict = insertStringAt(obj.dict, "[", i)
			obj.dict = insertStringAt(obj.dict, "]", i+4)
			i++
			obj.dict = insertStringAt(obj.dict, "R", i)
			obj.dict = insertStringAt(obj.dict, "0", i)
			obj.dict = insertStringAt(obj.dict, objNumber, i)
			return
		}
	}
}

// getMaxGSNumber returns the largest number of the /GS names in the
// dictionary, or 0. A name with no number after /GS, like /GSa, is skipped.
func getMaxGSNumber(obj *PDFobj) int {
	maxGSNumber := 0
	for _, token := range obj.dict {
		if strings.HasPrefix(token, "/GS") {
			if number, err := strconv.Atoi(token[3:]); err == nil {
				maxGSNumber = max(maxGSNumber, number)
			}
		}
	}
	return maxGSNumber
}

// maxResourceNumber returns the largest number of the /GS, /Fm and /OC names
// in the resources of this page, or 0, so that the names of what is drawn on
// it are not those of what it holds. They are all read from the objects of the
// PDF, in the resources or in an /ExtGState, /XObject or /Properties dictionary
// that is an object of its own.
func (obj *PDFobj) maxResourceNumber() int {
	dict := obj.inheritedResources
	if dict == nil {
		i := obj.entryIndexOf("/Resources")
		if i == -1 {
			return 0
		}
		dict = obj.dict
		if isObjectReference(obj.dict, i+1) {
			number, _ := strconv.Atoi(obj.dict[i+1])
			resources := obj.readObjects[number]
			if resources == nil {
				return 0
			}
			dict = resources.dict
		}
	}
	maximum := maxNameNumber(dict)
	for i, token := range dict {
		if (token == "/ExtGState" || token == "/XObject" || token == "/Properties") &&
			isObjectReference(dict, i+1) {
			number, _ := strconv.Atoi(dict[i+1])
			if entries := obj.readObjects[number]; entries != nil {
				maximum = max(maximum, maxNameNumber(entries.dict))
			}
		}
	}
	return maximum
}

// maxNameNumber returns the largest number of the names in the tokens that
// are /GS, /Fm or /OC and one to nine digits, or 0.
func maxNameNumber(tokens []string) int {
	maximum := 0
	for _, token := range tokens {
		if len(token) >= 4 && (token[:3] == "/GS" || token[:3] == "/Fm" || token[:3] == "/OC") &&
			isGSName("/GS"+token[3:]) {
			number, _ := strconv.Atoi(token[3:])
			maximum = max(maximum, number)
		}
	}
	return maximum
}

// addResourceEntry adds the name and its value to a dictionary of the
// resources of this page, like /ExtGState, unless it has the name.
func (obj *PDFobj) addResourceEntry(category, name string, value []string, objects *[]*PDFobj) {
	resources, index := obj.resourceEntries(category, objects)
	if resources == nil {
		return
	}
	for i := index; i < len(resources.dict) && resources.dict[i] != ">>"; i = dictValueEnd(resources.dict, i+1) {
		if resources.dict[i] == name {
			return
		}
	}
	resources.dict = insertArrayAt(resources.dict, append([]string{name}, value...), index)
}

// resourceEntries returns the object that holds a dictionary of the resources
// of this page, like /ExtGState, and the index of its first entry, or nil. The
// resources and the dictionary can be objects of their own, and resources
// without the dictionary get it.
func (obj *PDFobj) resourceEntries(category string, objects *[]*PDFobj) (*PDFobj, int) {
	obj.ownResources()
	var resources *PDFobj
	index := -1
	for i, token := range obj.dict {
		if token == "/Resources" {
			token2 := tokenAt(obj.dict, i+1)
			if token2 == "<<" {
				resources = obj
				index = i + 2
			} else if o := objectAt(*objects, token2); o != nil {
				resources = o
				for j := 0; j < len(resources.dict); j++ {
					if resources.dict[j] == "<<" {
						index = j + 1
						break
					}
				}
			}
			break
		}
	}
	if resources == nil || index == -1 {
		return nil, -1
	}
	i := index
	for i < len(resources.dict) && resources.dict[i] != category {
		i++
	}
	if i == len(resources.dict) {
		resources.dict = insertArrayAt(resources.dict, []string{category, "<<", ">>"}, index)
		return resources, index + 2
	}
	if tokenAt(resources.dict, i+1) == "<<" {
		return resources, i + 2
	}
	o := objectAt(*objects, tokenAt(resources.dict, i+1)) // "/ExtGState 12 0 R"
	if o == nil {
		return nil, -1
	}
	return o, slices.Index(o.dict, "<<") + 1
}

// SetGraphicsState sets the graphics state.
func (obj *PDFobj) SetGraphicsState(gs *GraphicsState, objects *[]*PDFobj) *PDFobj {
	resources, index := obj.resourceEntries("/ExtGState", objects)
	if resources == nil {
		return obj
	}
	entries := strings.Split(gs.entries(), " ")
	name := sameState(resources, index, entries)
	if name != "" {
		n, _ := strconv.Atoi(name[3:])
		obj.gsNumber = n - 1
	} else {
		obj.gsNumber = getMaxGSNumber(resources)
		name = "/GS" + strconv.Itoa(obj.gsNumber+1)
		state := append([]string{name, "<<"}, entries...)
		state = append(state, ">>")
		resources.dict = insertArrayAt(resources.dict, state, index)
	}
	obj.AddPrefixContent([]byte("q\n"+name+" gs\n"), objects)
	return obj
}

// sameState returns the name of a /GS state of the /ExtGState dictionary
// whose entries at index are these entries and no others, or "": setting the
// same state again reuses it rather than adding another (Stefan Ostermann's
// fork of PDFjet, 19 October 2026). Only our own /GS names with a number are
// reused, and only for exactly the same entries, so a state with a blend mode
// is not taken for one without.
func sameState(obj *PDFobj, index int, entries []string) string {
	i := index
	for i < len(obj.dict) && obj.dict[i] != ">>" {
		key := obj.dict[i]
		end := dictValueEnd(obj.dict, i+1)
		if isGSName(key) &&
			end-i == len(entries)+3 &&
			obj.dict[i+1] == "<<" &&
			slices.Equal(obj.dict[i+2:end-1], entries) {
			return key
		}
		i = end
	}
	return ""
}

// isGSName returns true for /GS and one to nine digits, as Java's matches of
// "/GS[0-9]{1,9}".
func isGSName(key string) bool {
	if !strings.HasPrefix(key, "/GS") || len(key) < 4 || len(key) > 12 {
		return false
	}
	for _, c := range key[3:] {
		if c < '0' || c > '9' {
			return false
		}
	}
	return true
}
