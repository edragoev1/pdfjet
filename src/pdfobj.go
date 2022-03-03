package pdfjet

/**
 * pdfobj.go
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"fmt"
	"log"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/decompressor"
	"github.com/edragoev1/pdfjet/src/letter"
	"strconv"
	"strings"
	"unicode"
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
func (obj *PDFobj) SetStreamAndData(buf []byte, length int) {
	obj.stream = make([]byte, length)
	for i := 0; i < length; i++ {
		obj.stream[i] = buf[obj.streamOffset+i]
	}
	if obj.getValue("/Filter") == "/FlateDecode" {
		obj.data = decompressor.Inflate(obj.stream)
	} else {
		// Assume no compression for now.
		// In the future we may handle LZW compression ...
		obj.data = obj.stream
	}
}

// SetStream sets the object stream.
func (obj *PDFobj) SetStream(stream []byte) {
	obj.stream = stream
}

// SetNumber sets the object number.
func (obj *PDFobj) SetNumber(number int) {
	obj.number = number
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
			} else {
				return token
			}
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

// GetContentsObject returns the contect object.
func (obj *PDFobj) GetContentsObject(objects []*PDFobj) *PDFobj {
	for i := 0; i < len(obj.dict); i++ {
		if obj.dict[i] == "/Contents" {
			if obj.dict[i+1] == "[" {
				token := obj.dict[i+2]
				index, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				}
				return objects[index-1]
			}
			token := obj.dict[i+1]
			index, err := strconv.Atoi(token)
			if err != nil {
				log.Fatal(err)
			}
			return objects[index-1]
		}
	}
	return nil
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
func (obj *PDFobj) SetGraphicsState(gs *GraphicsState, objects *[]*PDFobj) {
	var obj2 *PDFobj
	index := -1
	for i, token := range obj.dict {
		if token == "/Resources" {
			token2 := obj.dict[i+1]
			if token2 == "<<" {
				obj2 = obj
				index = i + 2
			} else {
				index, err := strconv.Atoi(token2)
				if err != nil {
					log.Fatal(err)
				}
				obj2 = (*objects)[index-1]
				for j := 0; j < len(obj2.dict); j++ {
					if obj2.dict[j] == "<<" {
						index = j + 1
						break
					}
				}
			}
			break
		}
	}

	gsNumber := getMaxGSNumber(obj)
	if gsNumber == 0 { // No existing ExtGState dictionary
		obj.dict = insertStringAt(obj.dict, "/ExtGState", index) // Add ExtGState dictionary
		index++
		obj.dict = insertStringAt(obj.dict, "<<", index)
	} else {
		for index < len(obj.dict) {
			token := obj.dict[index]
			if token == "/ExtGState" {
				index++
				break
			}
			index++
		}
	}

	index++
	obj.dict = insertStringAt(obj.dict, "/GS"+strconv.Itoa(gsNumber+1), index)
	index++
	obj.dict = insertStringAt(obj.dict, "<<", index)
	index++
	obj.dict = insertStringAt(obj.dict, "/CA", index)
	index++
	obj.dict = insertStringAt(obj.dict, fmt.Sprintf("%f", gs.GetAlphaStroking()), index)
	index++
	obj.dict = insertStringAt(obj.dict, "/ca", index)
	index++
	obj.dict = insertStringAt(obj.dict, fmt.Sprintf("%f", gs.GetAlphaNonStroking()), index)
	index++
	obj.dict = insertStringAt(obj.dict, ">>", index)
	if gsNumber == 0 {
		index++
		obj.dict = insertStringAt(obj.dict, ">>", index)
	}

	var buf strings.Builder
	buf.WriteString("q\n")
	buf.WriteString("/GS" + strconv.Itoa(gsNumber+1) + " gs\n")
	obj.addPrefixContent([]byte(buf.String()), objects)
}
