// pdfobj.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"log"
	"slices"
	"strconv"
	"strings"
	"unicode"

	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/decompressor"
	"github.com/edragoev1/pdfjet/src/letter"
)

// PDFobj is used to create Java or .NET objects that represent the objects in PDF document.
// See the PDF specification for more information.
type PDFobj struct {
	number       int      // The object number
	offset       int      // The object offset
	dict         []string // The object dictionary
	streamOffset int      // The stream offset
	stream       []byte   // The compressed stream
	data         []byte   // The decompressed data
	gsNumber     int      // Graphics State Number
}

// NewPDFobj is used to create Java or .NET objects that represent the objects in PDF document.
// See the PDF specification for more information.
// Also see Example_19.
func NewPDFobj() *PDFobj {
	obj := new(PDFobj)
	obj.dict = make([]string, 0)
	obj.gsNumber = -1
	return obj
}

func (obj *PDFobj) add(token string) {
	obj.dict = append(obj.dict, token)
}

func (obj *PDFobj) getNumber() int {
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

// SetStreamAndData sets the object stream.
func (obj *PDFobj) SetStreamAndData(buf []byte, length int) *PDFobj {
	obj.stream = make([]byte, length)
	for i := 0; i < length; i++ {
		obj.stream[i] = buf[obj.streamOffset+i]
	}
	filter := obj.getValue("/Filter")
	if filter == "/FlateDecode" {
		data, _ := decompressor.Inflate(obj.stream)
		obj.data = obj.applyDecodeParms(data)
	} else if filter == "/LZWDecode" {
		obj.data = obj.applyDecodeParms(decompressor.LZWDecode(obj.stream))
	} else {
		// Assume no compression for now.
		obj.data = obj.stream
	}
	return obj
}

// applyDecodeParms undoes the predictor of the /DecodeParms dictionary. Images
// keep it, as they are copied with their stream, and their data is not used.
func (obj *PDFobj) applyDecodeParms(decoded []byte) []byte {
	if obj.getValue("/Subtype") == "/Image" {
		return decoded
	}
	return decompressor.ApplyPredictor(
		decoded,
		obj.getDecodeParm("/Predictor", 1),
		obj.getDecodeParm("/Colors", 1),
		obj.getDecodeParm("/BitsPerComponent", 8),
		obj.getDecodeParm("/Columns", 1))
}

// getDecodeParm returns the integer value of the key in the /DecodeParms
// dictionary. A value that is not a 32-bit integer gets the default, as in
// the Java and C# ports.
func (obj *PDFobj) getDecodeParm(key string, defaultValue int) int {
	tokens := strings.Split(obj.getValue("/DecodeParms"), " ")
	for i := 0; i < len(tokens)-1; i++ {
		if tokens[i] == key {
			if value, err := strconv.ParseInt(tokens[i+1], 10, 32); err == nil {
				return int(value)
			}
			break
		}
	}
	return defaultValue
}

// SetStream sets the object stream.
func (obj *PDFobj) SetStream(stream []byte) *PDFobj {
	obj.stream = stream
	return obj
}

// SetNumber sets the object number.
func (obj *PDFobj) SetNumber(number int) *PDFobj {
	obj.number = number
	return obj
}

// getValue returns the dictionary value for the specified key.
func (obj *PDFobj) getValue(key string) string {
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == key {
			token := obj.dict[i+1]
			if token == "<<" {
				var sb strings.Builder
				sb.WriteString("<< ")
				i += 2
				for obj.dict[i] != ">>" {
					sb.WriteString(obj.dict[i])
					sb.WriteString(" ")
					i++
				}
				sb.WriteString(">>")
				return sb.String()
			} else if token == "[" {
				var sb strings.Builder
				sb.WriteString("[ ")
				i += 2
				for obj.dict[i] != "]" {
					sb.WriteString(obj.dict[i])
					sb.WriteString(" ")
					i++
				}
				sb.WriteString("]")
				return sb.String()
			}
			return token
		}
	}
	return ""
}

// GetObjectNumbers returns the object numbers.
func (obj *PDFobj) GetObjectNumbers(key string) []int {
	numbers := make([]int, 0)
	for i := 0; i < len(obj.dict); i++ {
		token := obj.dict[i]
		if token == key {
			i++
			str := obj.dict[i]
			if str == "[" {
				for {
					i++
					str = obj.dict[i]
					if str == "]" {
						break
					}
					objNumber, err := strconv.Atoi(str)
					if err != nil {
						log.Fatal(err)
					}
					numbers = append(numbers, objNumber)
					i++ // 0
					i++ // R
				}
			} else {
				objNumber, err := strconv.Atoi(str)
				if err != nil {
					log.Fatal(err)
				}
				numbers = append(numbers, objNumber)
			}
			break
		}
	}
	return numbers
}

// GetPageSize returns the page size.
func (obj *PDFobj) GetPageSize() [2]float32 {
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == "/MediaBox" {
			f1, err1 := strconv.ParseFloat(obj.dict[i+4], 32)
			if err1 != nil {
				log.Fatal(err1)
			}
			f2, err2 := strconv.ParseFloat(obj.dict[i+5], 32)
			if err2 != nil {
				log.Fatal(err2)
			}
			return [2]float32{float32(f1), float32(f2)}
		}
	}
	return letter.Portrait
}

// GetLength return the length value.
func (obj *PDFobj) GetLength(objects []*PDFobj) int {
	for i := 0; i < len(obj.dict); i++ {
		token := obj.dict[i]
		if token == "/Length" {
			number, err := strconv.Atoi(obj.dict[i+1])
			if err != nil {
				log.Fatal(err)
			}
			if obj.dict[i+2] == "0" &&
				obj.dict[i+3] == "R" {
				return obj.getLength(objects, number)
			}
			return number
		}
	}
	return 0
}

func (obj *PDFobj) getLength(objects []*PDFobj, number int) int {
	for _, obj := range objects {
		if obj.number == number {
			length, err := strconv.Atoi(obj.dict[3])
			if err != nil {
				log.Fatal(obj.dict[3])
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
	numbers := obj.GetObjectNumbers("/Contents")
	if len(numbers) == 1 {
		contents := objects[numbers[0]-1]
		if contents.stream != nil {
			return contents
		}
		// "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
		numbers = make([]int, 0)
		i := slices.Index(contents.dict, "[")
		for i != -1 && i+3 < len(contents.dict) && contents.dict[i+3] == "R" {
			number, err := strconv.Atoi(contents.dict[i+1])
			if err != nil {
				log.Fatal(err)
			}
			numbers = append(numbers, number)
			i += 3
		}
	}
	if len(numbers) == 0 {
		return nil
	}
	if len(numbers) == 1 {
		return objects[numbers[0]-1]
	}
	content := NewPDFobj()
	content.data = make([]byte, 0)
	for _, number := range numbers {
		if data := objects[number-1].data; data != nil {
			content.data = append(content.data, data...)
			content.data = append(content.data, '\n') // A stream can end in the middle of a line.
		}
	}
	return content
}

func (obj *PDFobj) getResourcesObject(objects []*PDFobj) *PDFobj {
	for i, token := range obj.dict {
		if token == "/Resources" {
			token = obj.dict[i+1]
			if token == "<<" {
				return obj
			}
			objNumber, err := strconv.Atoi(token)
			if err != nil {
				log.Fatal(err)
			}
			return objects[objNumber-1]
		}
	}
	return nil
}

func (obj *PDFobj) addCoreFontResource(coreFont *corefont.CoreFont, objects *[]*PDFobj) *Font {
	font := NewCoreFontForPDFobj(coreFont)
	font.fontID = strings.ToUpper(strings.ReplaceAll(font.name, "-", "_"))
	obj2 := NewPDFobj()
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

	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == "/Resources" {
			i++
			token := obj.dict[i]
			if token == "<<" { // Direct resources object
				obj.addFontResource(obj, objects, font.fontID, obj2.number)
			} else if unicode.IsDigit(rune(token[0])) { // Indirect resources object
				objNumber, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				}
				obj.addFontResource((*objects)[objNumber-1], objects, font.fontID, obj2.number)
			}
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
		for i := 0; i < len(obj2.dict); i++ {
			if obj2.dict[i] == "/Resources" {
				obj2.dict = insertStringAt(obj2.dict, "/Font", i+2)
				obj2.dict = insertStringAt(obj2.dict, "<<", i+3)
				obj2.dict = insertStringAt(obj2.dict, ">>", i+4)
				break
			}
		}
	}

	for i := 0; i < len(obj2.dict); i++ {
		if obj2.dict[i] == "/Font" {
			token := obj2.dict[i+1]
			if token == "<<" {
				obj2.dict = insertStringAt(obj2.dict, "/"+fontID, i+2)
				obj2.dict = insertStringAt(obj2.dict, strconv.Itoa(number), i+3)
				obj2.dict = insertStringAt(obj2.dict, "0", i+4)
				obj2.dict = insertStringAt(obj2.dict, "R", i+5)
				return
			} else if unicode.IsDigit(rune(token[0])) {
				index, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				}
				obj3 := (*objects)[index-1]
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
			return insertArrayAt(dict, list, i+2)
		}
	}
	if dict[3] == "<<" {
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
			token = obj.dict[i+1]
			if token == "<<" {
				obj.dict = insertNewObject(obj.dict, list, objType)
			} else {
				objNumber, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				}
				obj.dict = insertNewObject((*objects)[objNumber-1].dict, list, objType)
			}
			return
		}
	}

	// Handle the case where the page originally does not have any font resources.
	list = []string{objType, "<<", tag + number, number, "0", "R", ">>"}
	for i, token := range obj.dict {
		if token == "/Resources" {
			obj.dict = insertArrayAt(obj.dict, list, i+2)
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
			token = obj.dict[i+1]
			if token == "<<" { // Direct resources object
				addResource("/XObject", obj, objects, image.objNumber)
			} else { // Indirect resources object
				objNumber, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				}
				addResource("/XObject", (*objects)[objNumber-1], objects, image.objNumber)
			}
			return
		}
	}
}

// AddFontResource adds font resource.
func (obj *PDFobj) AddFontResource(font *Font, objects *[]*PDFobj) {
	for i, token := range obj.dict {
		if token == "/Resources" {
			token = obj.dict[i+1]
			if token == "<<" { // Direct resources object
				addResource("/Font", obj, objects, font.objNumber)
			} else { // Indirect resources object
				objNumber, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				}
				addResource("/Font", (*objects)[objNumber-1], objects, font.objNumber)
			}
			return
		}
	}
}

func (obj *PDFobj) addContent(content []byte, objects *[]*PDFobj) {
	obj2 := NewPDFobj()
	obj2.SetNumber(len(*objects) + 1)
	obj2.SetStream(content)
	*objects = append(*objects, obj2)

	objNumber := strconv.Itoa(obj2.number)
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == "/Contents" {
			i++
			token := obj.dict[i]
			if token == "[" {
				// Array of content objects
				for {
					i++
					token = obj.dict[i]
					if token == "]" {
						obj.dict = insertStringAt(obj.dict, "R", i)
						obj.dict = insertStringAt(obj.dict, "0", i)
						obj.dict = insertStringAt(obj.dict, objNumber, i)
						return
					}
					i += 2 // Skip the 0 and R
				}
			} else {
				// Single content object
				index, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				}
				obj3 := (*objects)[index-1]
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

/**
 * Adds new content object before the existing content objects.
 * The original code was provided by Stefan Ostermann author of ScribMaster and HandWrite Pro.
 * Additional code to handle PDFs with indirect array of stream objects was written by EDragoev.
 *
 * @param content
 * @param objects
 */
func (obj *PDFobj) addPrefixContent(content []byte, objects *[]*PDFobj) {
	obj2 := NewPDFobj()
	obj2.SetNumber(len(*objects) + 1)
	obj2.SetStream(content)
	*objects = append(*objects, obj2)

	objNumber := strconv.Itoa(obj2.number)
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == "/Contents" {
			i++
			token := obj.dict[i]
			if token == "[" {
				// Array of content object streams
				i++
				obj.dict = insertStringAt(obj.dict, "R", i)
				obj.dict = insertStringAt(obj.dict, "0", i)
				obj.dict = insertStringAt(obj.dict, objNumber, i)
				return
			}
			// Single content object
			index, err := strconv.Atoi(token)
			if err != nil {
				log.Fatal(err)
			}
			obj3 := (*objects)[index-1]
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

func getMaxGSNumber(obj *PDFobj) int {
	numbers := make([]int, 0)
	for _, token := range obj.dict {
		if strings.HasPrefix(token, "/GS") {
			number, err := strconv.Atoi(token[3:])
			if err != nil {
				log.Fatal(err)
			}
			numbers = append(numbers, number)
		}
	}

	if len(numbers) == 0 {
		return 0
	}

	maxNumber := 0
	for _, number := range numbers {
		if number > maxNumber {
			maxNumber = number
		}
	}
	return maxNumber
}

// SetGraphicsState sets the graphics state.
func (obj *PDFobj) SetGraphicsState(gs *GraphicsState, objects *[]*PDFobj) *PDFobj {
	var resources *PDFobj
	index := -1
	for i, token := range obj.dict {
		if token == "/Resources" {
			token2 := obj.dict[i+1]
			if token2 == "<<" {
				resources = obj
				index = i + 2
			} else {
				index2, err := strconv.Atoi(token2)
				if err != nil {
					log.Fatal(err)
				}
				resources = (*objects)[index2-1]
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
	obj.gsNumber = getMaxGSNumber(resources)
	if obj.gsNumber == 0 { // No existing ExtGState dictionary
		resources.dict = insertStringAt(resources.dict, "/ExtGState", index) // Add ExtGState dictionary
		index++
		resources.dict = insertStringAt(resources.dict, "<<", index)
	} else {
		for index < len(resources.dict) {
			token := resources.dict[index]
			if token == "/ExtGState" {
				index++
				break
			}
			index++
		}
	}
	index++
	resources.dict = insertStringAt(resources.dict, "/GS"+strconv.Itoa(obj.gsNumber+1), index)
	index++
	resources.dict = insertStringAt(resources.dict, "<<", index)
	index++
	resources.dict = insertStringAt(resources.dict, "/CA", index)
	index++
	resources.dict = insertStringAt(resources.dict, formatFloat32(gs.GetAlphaStroking()), index)
	index++
	resources.dict = insertStringAt(resources.dict, "/ca", index)
	index++
	resources.dict = insertStringAt(resources.dict, formatFloat32(gs.GetAlphaNonStroking()), index)
	index++
	resources.dict = insertStringAt(resources.dict, ">>", index)
	if obj.gsNumber == 0 {
		index++
		resources.dict = insertStringAt(resources.dict, ">>", index)
	}

	var buf strings.Builder
	buf.WriteString("q\n")
	buf.WriteString("/GS" + strconv.Itoa(obj.gsNumber+1) + " gs\n")
	obj.addPrefixContent([]byte(buf.String()), objects)
	return obj
}

// formatFloat32 formats a float the way the Java and .NET editions do,
// with the shortest representation that round-trips - "0.75", not "0.750000".
func formatFloat32(value float32) string {
	return strconv.FormatFloat(float64(value), 'g', -1, 32)
}
