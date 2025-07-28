package pdfjet

/**
 *  otf.go
 *
©2025 PDFjet Software

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
	"bytes"
	"compress/zlib"
	"fmt"
	"io"
	"log"
	"strings"
	"unicode/utf16"

	"github.com/edragoev1/pdfjet/src/content"
)

// FontTable is used to construct font table objects.
type FontTable struct {
	name     string
	checkSum uint32
	offset   int
	length   int
}

// OTF is used to construct TTF and OTF font objects.
type OTF struct {
	fontName           string
	fontInfo           string
	buf                []byte
	index              int
	compressed         bytes.Buffer
	unitsPerEm         int
	bBoxLLx            int16
	bBoxLLy            int16
	bBoxURx            int16
	bBoxURy            int16
	ascent             int16
	descent            int16
	firstChar          rune
	lastChar           rune
	capHeight          int16
	postVersion        uint32
	italicAngle        uint32
	underlinePosition  int16
	underlineThickness int16
	advanceWidth       []uint16
	unicodeToGID       []int
	cff                bool
	cffOff             int
	cffLen             int
	format             int
	count              int
	stringOffset       int
}

// NewOTF is the constructor for TTF and OTF fonts.
func NewOTF(reader io.Reader) *OTF {
	otf := new(OTF)
	otf.buf = content.GetFromReader(reader)
	otf.unicodeToGID = make([]int, 0x10000)

	// Extract the OTF metadata
	version := readUint32(otf)
	if version == 0x00010000 || // Win OTF
		version == 0x74727565 || // Mac TTF
		version == 0x4F54544F { // CFF OTF
		// We should be able to read this font.
	} else {
		fmt.Println("OTF version == " + fmt.Sprint(version) + " is not supported.")
	}

	numOfTables := int(readUint16(otf))
	readUint16(otf) // Skip the search range.
	readUint16(otf) // Skip the entry selector.
	readUint16(otf) // Skip the range shift.

	var cmapTable *FontTable
	for i := 0; i < numOfTables; i++ {
		table := new(FontTable)
		table.name = string(readNBytes(otf, 4))
		table.checkSum = readUint32(otf)
		table.offset = int(readUint32(otf))
		table.length = int(readUint32(otf))

		k := otf.index // Save the current index
		switch table.name {
		case "head":
			getHeadTable(otf, table)
		case "hhea":
			getHheaTable(otf, table)
		case "OS/2":
			getOs2Table(otf, table)
		case "name":
			getNameTable(otf, table)
		case "hmtx":
			getHmtxTable(otf, table)
		case "post":
			getPostTable(otf, table)
		case "CFF ":
			getCffTable(otf, table)
		case "cmap":
			cmapTable = table
		}
		otf.index = k // Restore the index
	}

	// This table must be processed last
	getCmapTable(otf, cmapTable)

	writer := zlib.NewWriter(&otf.compressed)
	if otf.cff {
		_, err := writer.Write(otf.buf[otf.cffOff : otf.cffOff+otf.cffLen])
		if err != nil {
			log.Fatal(err)
		}
	} else {
		_, err := writer.Write(otf.buf)
		if err != nil {
			log.Fatal(err)
		}
	}
	writer.Close()

	return otf
}

func getHeadTable(otf *OTF, table *FontTable) {
	otf.index = table.offset + 16
	_ = readUint16(otf) // Skip the flags
	otf.unitsPerEm = int(readUint16(otf))
	otf.index += 16
	otf.bBoxLLx = readInt16(otf)
	otf.bBoxLLy = readInt16(otf)
	otf.bBoxURx = readInt16(otf)
	otf.bBoxURy = readInt16(otf)
}

func getHheaTable(otf *OTF, table *FontTable) {
	otf.index = table.offset + 4
	otf.ascent = readInt16(otf)
	otf.descent = readInt16(otf)
	otf.index += 26
	otf.advanceWidth = make([]uint16, readUint16(otf))
}

func getOs2Table(otf *OTF, table *FontTable) {
	otf.index = table.offset + 64
	otf.firstChar = rune(readUint16(otf))
	otf.lastChar = rune(readUint16(otf))
	otf.index += 20
	otf.capHeight = int16(readUint16(otf))
}

func getNameTable(otf *OTF, table *FontTable) {
	otf.index = table.offset
	otf.format = int(readUint16(otf))
	otf.count = int(readUint16(otf))
	otf.stringOffset = int(readUint16(otf))
	var macFontInfo strings.Builder
	var winFontInfo strings.Builder

	for r := 0; r < otf.count; r++ {
		platformID := readUint16(otf)
		encodingID := readUint16(otf)
		languageID := readUint16(otf)
		nameID := readUint16(otf)
		length := int(readUint16(otf))
		offset := int(readUint16(otf))
		index1 := table.offset + otf.stringOffset + offset
		index2 := index1 + length
		buffer := otf.buf[index1:index2]

		if platformID == 1 && encodingID == 0 && languageID == 0 {
			// Macintosh
			if nameID == 6 {
				otf.fontName = string(buffer)
			} else {
				macFontInfo.WriteString(otf.fontName)
				macFontInfo.WriteString("\n")
			}
		} else if platformID == 3 && encodingID == 1 && languageID == 0x409 {
			// Windows
			w := make([]uint16, len(buffer)/2)
			for i := 0; i < len(w); i++ {
				w[i] = uint16(buffer[2*i])<<8 | uint16(buffer[2*i+1])
			}
			str := string(utf16.Decode(w))
			if nameID == 6 {
				otf.fontName = str
			} else {
				winFontInfo.WriteString(str)
				winFontInfo.WriteString("\n")
			}
		}
	}
	otf.fontInfo = winFontInfo.String()
	if otf.fontInfo == "" {
		otf.fontInfo = macFontInfo.String()
	}
}

func getCmapTable(otf *OTF, table *FontTable) {
	otf.index = table.offset
	tableOffset := otf.index
	otf.index += 2
	numRecords := int(readUint16(otf))

	// Process the encoding records
	format4subtable := false
	subtableOffset := 0
	for i := 0; i < numRecords; i++ {
		platformID := readUint16(otf)
		encodingID := readUint16(otf)
		subtableOffset = int(readUint32(otf))
		if platformID == 3 && encodingID == 1 {
			format4subtable = true
			break
		}
	}
	if !format4subtable {
		log.Fatal("Format 4 subtable not found in this font.")
	}

	otf.index = tableOffset + subtableOffset

	readUint16(otf) // Skip the format
	tableLen := readUint16(otf)
	readUint16(otf) // Skip the language
	segCount := int(readUint16(otf) / 2)

	otf.index += 6 // Skip to the endCount[]
	endCount := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		endCount[i] = readUint16(otf)
	}

	otf.index += 2 // Skip the reservedPad
	startCount := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		startCount[i] = readUint16(otf)
	}

	idDelta := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		idDelta[i] = readUint16(otf)
	}

	idRangeOffset := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		idRangeOffset[i] = readUint16(otf)
	}

	glyphIDArray := make([]uint16, (int(tableLen)-(16+8*segCount))/2)
	for i := 0; i < len(glyphIDArray); i++ {
		glyphIDArray[i] = readUint16(otf)
	}

	for ch := otf.firstChar; ch <= otf.lastChar; ch++ {
		seg := getSegmentFor(ch, startCount, endCount, segCount)
		if seg != -1 {
			gid := 0
			offset := int(idRangeOffset[seg])
			if offset == 0 {
				gid = (int(idDelta[seg]) + int(ch)) % 65536
			} else {
				offset /= 2
				offset -= segCount - seg
				gid = int(glyphIDArray[offset+(int(ch)-int(startCount[seg]))])
				if gid != 0 {
					gid += int(idDelta[seg]) % 65536
				}
			}
			otf.unicodeToGID[ch] = gid
		}
	}
}

func getHmtxTable(otf *OTF, table *FontTable) {
	otf.index = table.offset
	for i := 0; i < len(otf.advanceWidth); i++ {
		otf.advanceWidth[i] = readUint16(otf)
		otf.index += 2
	}
}

func getPostTable(otf *OTF, table *FontTable) {
	otf.index = table.offset
	otf.postVersion = readUint32(otf)
	otf.italicAngle = readUint32(otf)
	otf.underlinePosition = readInt16(otf)
	otf.underlineThickness = readInt16(otf)
}

func getCffTable(otf *OTF, table *FontTable) {
	otf.cff = true
	otf.cffOff = table.offset
	otf.cffLen = table.length
}

func getSegmentFor(ch rune, startCount, endCount []uint16, segCount int) int {
	segment := -1
	for i := 0; i < segCount; i++ {
		if uint16(ch) <= endCount[i] && uint16(ch) >= startCount[i] {
			segment = i
			break
		}
	}
	return segment
}

func readInt16(otf *OTF) int16 {
	value := int16(otf.buf[otf.index]) << 8
	otf.index++
	value |= int16(otf.buf[otf.index])
	otf.index++
	return value
}

func readUint8(otf *OTF) uint8 {
	value := otf.buf[otf.index]
	otf.index++
	return value
}

func readUint16(otf *OTF) uint16 {
	value := uint16(otf.buf[otf.index]) << 8
	otf.index++
	value |= uint16(otf.buf[otf.index])
	otf.index++
	return value
}

func readUint32(otf *OTF) uint32 {
	value := uint32(otf.buf[otf.index]) << 24
	otf.index++
	value |= uint32(otf.buf[otf.index]) << 16
	otf.index++
	value |= uint32(otf.buf[otf.index]) << 8
	otf.index++
	value |= uint32(otf.buf[otf.index])
	otf.index++
	return value
}

func readNBytes(otf *OTF, n int) []byte {
	bytes := make([]byte, 0)
	for i := 0; i < n; i++ {
		bytes = append(bytes, otf.buf[otf.index])
		otf.index++
	}
	return bytes
}
