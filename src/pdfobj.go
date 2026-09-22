// pdfobj.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"math"
	"runtime"
	"slices"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
)

// PDFobj is an object of a PDF that was read with PDF.Read, which holds the
// tokens of its dictionary and its stream. See Example_20, Example_37 and
// Example_50.
type PDFobj struct {
	number       int      // The object number
	offset       int      // The object offset
	dict         []string // The object dictionary
	streamOffset int      // The stream offset
	stream       []byte   // The compressed stream
	data         []byte   // The decompressed data
	gsNumber     int      // Graphics savedState Number
	root         bool     // The catalog that the trailer's /Root names
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

// GetData returns the uncompressed stream data.
func (obj *PDFobj) GetData() []byte {
	return obj.data
}

// setStreamAndData copies the stream from the buffer, decrypts it when the
// PDF is encrypted, and decodes it with the filters of its /Filter entry. The
// decrypted stream replaces the encrypted one, so that it can be copied.
func (obj *PDFobj) setStreamAndData(buf []byte, length int, dec *decryptor) *PDFobj {
	if obj.stream == nil {
		if actual := streamLength(buf, obj.streamOffset, length); actual != length {
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
		obj.data = obj.decodeStream()
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
func streamLength(buf []byte, offset, length int) int {
	if offset < 0 || offset > len(buf) {
		return length
	}
	if length >= 0 && length <= len(buf)-offset {
		i := offset + length
		for i < len(buf) && isWhiteSpace(int(buf[i])) {
			i++
		}
		if startsWith(buf, i, "endstream") {
			return length
		}
	}
	end := indexOf(buf, "endstream", offset)
	if end == -1 {
		return length
	}
	if end > offset && buf[end-1] == '\n' {
		end--
	}
	if end > offset && buf[end-1] == '\r' {
		end--
	}
	return end - offset
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
	for i, filter := range obj.getValues("/Filter") {
		switch filter {
		case "/FlateDecode", "/Fl":
			data, err := decompressor.Inflate(decoded)
			if err != nil {
				panic(err)
			}
			decoded = obj.applyDecodeParms(data, i)
		case "/LZWDecode", "/LZW":
			data, err := decompressor.LZWDecode(decoded)
			if err != nil {
				panic(err)
			}
			decoded = obj.applyDecodeParms(data, i)
		case "/ASCIIHexDecode", "/AHx":
			decoded = decompressor.ASCIIHexDecode(decoded)
		case "/ASCII85Decode", "/A85":
			decoded = decompressor.ASCII85Decode(decoded)
		case "/RunLengthDecode", "/RL":
			data, err := decompressor.RunLengthDecode(decoded)
			if err != nil {
				panic(err)
			}
			decoded = data
		default:
			return decoded
		}
	}
	return decoded
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
// index. Images keep it, as they are copied with their stream, and their data
// is not used.
func (obj *PDFobj) applyDecodeParms(decoded []byte, index int) []byte {
	if obj.GetValue("/Subtype") == "/Image" {
		return decoded
	}
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
// object has no such key. A dictionary or an array that the PDF ends in the
// middle of is closed where it ends, since a PDF that was read can hold
// anything.
func (obj *PDFobj) GetValue(key string) string {
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == key {
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
	}
	return ""
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
func (obj *PDFobj) getObjectNumbers(key string) []int {
	numbers := make([]int, 0)
	for i := 0; i < len(obj.dict); i++ {
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
				value, err := strconv.ParseFloat(tokenAt(obj.dict, i+2+j), 32)
				if err != nil {
					return letter.Portrait()
				}
				box[j] = value
			}
			width, height := math.Abs(box[2]-box[0]), math.Abs(box[3]-box[1])
			if width == 0 || height == 0 {
				return letter.Portrait()
			}
			return pagesize.NewPageSize(float32(width), float32(height))
		}
	}
	return letter.Portrait()
}

// getLength return the length value.
func (obj *PDFobj) getLength(objects []*PDFobj) int {
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
	number, err := strconv.Atoi(tokenAt(obj.dict, i+1))
	if err != nil {
		panic(errors.New("The /Length of a stream is not a number."))
	}
	if i+2 >= len(obj.dict) {
		panic(errors.New("The dictionary ends after the /Length."))
	}
	if obj.dict[i+2] == "0" {
		if i+3 >= len(obj.dict) {
			panic(errors.New("The dictionary ends after the /Length."))
		}
		if obj.dict[i+3] == "R" {
			return obj.getLengthFromObject(objects, number)
		}
	}
	return number
}

// getLengthFromObject returns the /Length stored in the object with the number.
func (obj *PDFobj) getLengthFromObject(objects []*PDFobj, number int) int {
	for _, obj := range objects {
		if obj.number == number {
			length, err := strconv.Atoi(tokenAt(obj.dict, 3))
			if err != nil {
				panic(errors.New("The /Length of a stream is not a number."))
			}
			return length
		}
	}
	return 0
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
	for _, number := range numbers {
		page := objectNumbered(objects, number)
		if page == nil {
			continue
		}
		if data := page.data; data != nil {
			content.data = append(content.data, data...)
			content.data = append(content.data, '\n') // A stream can end in the middle of a line.
		}
	}
	return content
}

// GetResourcesObject returns the resources object of this page, or nil if it has none.
func (obj *PDFobj) GetResourcesObject(objects []*PDFobj) *PDFobj {
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

// AddCoreFontResource adds a core font to the resources of this page and returns it.
func (obj *PDFobj) AddCoreFontResource(coreFont *corefont.CoreFont, objects *[]*PDFobj) *Font {
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
	for i := 0; i < len(obj.dict); i++ {
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
	for i := 0; i < len(obj.dict); i++ {
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

// SetGraphicsState sets the graphics state.
func (obj *PDFobj) SetGraphicsState(gs *GraphicsState, objects *[]*PDFobj) *PDFobj {
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
		return obj
	}
	// The graphics states go in the /ExtGState dictionary of the resources,
	// which can be an object of its own. Resources without one get it.
	i := index
	for i < len(resources.dict) && resources.dict[i] != "/ExtGState" {
		i++
	}
	if i == len(resources.dict) {
		resources.dict = insertArrayAt(resources.dict, []string{"/ExtGState", "<<", ">>"}, index)
		index += 2
	} else if tokenAt(resources.dict, i+1) == "<<" {
		index = i + 2
	} else { // "/ExtGState 12 0 R"
		o := objectAt(*objects, tokenAt(resources.dict, i+1))
		if o == nil {
			return obj
		}
		resources = o
		index = slices.Index(resources.dict, "<<") + 1
	}
	obj.gsNumber = getMaxGSNumber(resources)
	name := "/GS" + strconv.Itoa(obj.gsNumber+1)
	resources.dict = insertArrayAt(resources.dict, []string{
		name, "<<",
		"/CA", string(fastfloat.ToByteArray(gs.GetAlphaStroking())),
		"/ca", string(fastfloat.ToByteArray(gs.GetAlphaNonStroking())),
		">>"}, index)
	obj.AddPrefixContent([]byte("q\n"+name+" gs\n"), objects)
	return obj
}
