// qrcode.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//
// Original author: Kazuhiko Arase, 2009
// URL: http://www.d-project.com/
// Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
//
// The word "QR Code" is a registered trademark of
// DENSO WAVE INCORPORATED
// http://www.denso-wave.com/qrcode/faqpatent-e.html
//
// Modified and adapted for use in PDFjet by PDFjet Software

// Package qrcode creates QR code barcodes.
package qrcode

import (
	"strconv"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// QRCode used to create 2D QR Code barcodes. Please see Example_20.
type QRCode struct {
	pad0                 int
	pad1                 int
	modules              [][]bool // The dark modules
	reserved             [][]bool // The modules of the function patterns and the format information
	typeNumber           int      // The version of the symbol, from 4 to 40
	moduleCount          int      // 4 * typeNumber + 17
	errorCorrectionLevel errorcorrectionlevel.ErrorCorrectionLevel
	x                    float32
	y                    float32
	qrData               []byte
	eci                  bool    // The data is not all ASCII, and starts with the ECI of UTF-8
	m1                   float32 // Module length
	color                int32
	altDescription       string
}

// eciBits are the bits of the ECI that says the bytes are UTF-8: the mode
// indicator 0111 and the ECI assignment number 26 in 8 bits.
const eciBits = 12

// NewQRCode is used to create 2D QR Code barcodes. The string is encoded in
// UTF-8, and the symbol is the smallest that holds it, from version 4, 33 by
// 33 modules, to version 40, 177 by 177 modules. At version 40 it holds up to
// 2,953 bytes at level L, 2,331 at M, 1,663 at Q and 1,273 at H, a byte fewer
// when the string is not all ASCII: it then starts with the ECI that tells
// the reader the bytes are UTF-8. It panics if the string does not fit in a
// version 40 symbol.
// @param str the string to encode.
// @param errorCorrectionLevel the desired error correction level.
func NewQRCode(str string, errorCorrectionLevel errorcorrectionlevel.ErrorCorrectionLevel) *QRCode {
	qrcode := new(QRCode)
	qrcode.pad0 = 0xEC
	qrcode.pad1 = 0x11
	qrcode.qrData = []byte(str)
	for _, b := range qrcode.qrData {
		if b > 127 {
			qrcode.eci = true
		}
	}
	qrcode.typeNumber = getTypeNumber(len(qrcode.qrData), qrcode.eci, errorCorrectionLevel)
	qrcode.moduleCount = 4*qrcode.typeNumber + 17
	qrcode.m1 = 2.0
	qrcode.errorCorrectionLevel = errorCorrectionLevel
	qrcode.make(qrcode.createData(errorCorrectionLevel))
	return qrcode
}

// getTypeNumber returns the smallest version, from 4, whose data codewords
// hold the data, and the ECI before it when there is one.
func getTypeNumber(dataLength int, eci bool, errorCorrectionLevel errorcorrectionlevel.ErrorCorrectionLevel) int {
	for typeNumber := 4; typeNumber <= 40; typeNumber++ {
		if getLengthInBits(dataLength, eci, typeNumber) <= getDataCount(typeNumber, errorCorrectionLevel)*8 {
			return typeNumber
		}
	}
	maxLength := (getDataCount(40, errorCorrectionLevel)*8 - getLengthInBits(0, eci, 40)) / 8
	panic("The data is too long for a QR code at level " +
		levelName(errorCorrectionLevel) + ": " + strconv.Itoa(dataLength) +
		" bytes, at most " + strconv.Itoa(maxLength) + ".")
}

// getLengthInBits returns the bits of the ECI, if any, and of the mode
// indicator, the character count and the data, in byte mode.
func getLengthInBits(dataLength int, eci bool, typeNumber int) int {
	bits := 4 + getCharacterCountBits(typeNumber) + 8*dataLength
	if eci {
		bits += eciBits
	}
	return bits
}

// getCharacterCountBits returns the bits of the character count of byte mode:
// 8 up to version 9, and 16 after it.
func getCharacterCountBits(typeNumber int) int {
	if typeNumber < 10 {
		return 8
	}
	return 16
}

func getDataCount(typeNumber int, errorCorrectionLevel errorcorrectionlevel.ErrorCorrectionLevel) int {
	rsblock := new(qrRSBlock)
	dataCount := 0
	for _, block := range rsblock.getRSBlocks(typeNumber, errorCorrectionLevel) {
		dataCount += block.getDataCount()
	}
	return dataCount
}

func levelName(errorCorrectionLevel errorcorrectionlevel.ErrorCorrectionLevel) string {
	return []string{"M", "L", "H", "Q"}[errorCorrectionLevel]
}

// SetLocation sets the location where this barcode will be drawn on the page.
// @param x the x coordinate of the top left corner of the barcode.
// @param y the y coordinate of the top left corner of the barcode.
func (qrcode *QRCode) SetLocation(x, y float32) pdfjet.Drawable {
	qrcode.x = x
	qrcode.y = y
	return qrcode
}

// SetModuleLength sets the module length of this barcode.
// The default value is 2.0f
// @param moduleLength the specified module length.
func (qrcode *QRCode) SetModuleLength(moduleLength float32) *QRCode {
	qrcode.m1 = moduleLength
	return qrcode
}

// SetModuleColor sets the color of the barcode.
func (qrcode *QRCode) SetModuleColor(color int32) *QRCode {
	qrcode.color = color
	return qrcode
}

// DrawOn draws this barcode on the specified page. The dark modules next to
// each other in a row are filled as one rectangle, and the pen and the brush
// of the page are as they were after it.
// @param page the specified page.
// @return x and y coordinates of the bottom right corner of this component.
func (qrcode *QRCode) DrawOn(page *pdfjet.Page) [2]float32 {
	size := qrcode.m1 * float32(qrcode.moduleCount)
	if page != nil {
		// Described, the QR code is a figure of a tagged document; not
		// described, its modules, which carry no text, are decoration.
		if qrcode.altDescription != "" {
			page.AddBDC(structelem.Figure, "", "", qrcode.altDescription)
		} else {
			page.AddArtifactBMC()
		}
		page.SaveGraphicsState()
		page.SetBrushColor(qrcode.color)
		for row := 0; row < qrcode.moduleCount; row++ {
			col := 0
			for col < qrcode.moduleCount {
				if !qrcode.modules[row][col] {
					col++
					continue
				}
				start := col
				for col < qrcode.moduleCount && qrcode.modules[row][col] {
					col++
				}
				page.FillRect(
					qrcode.x+float32(start)*qrcode.m1,
					qrcode.y+float32(row)*qrcode.m1,
					float32(col-start)*qrcode.m1,
					qrcode.m1)
			}
		}
		page.RestoreGraphicsState()
		if qrcode.altDescription != "" {
			page.SetFigureBoundingBox(qrcode.x, qrcode.y, size, size)
		}
		page.AddEMC()
	}
	return [2]float32{qrcode.x + size, qrcode.y + size}
}

// SetAltDescription sets what the QR code says, such as the web address it
// carries, for a screen reader: a tagged document, PDF/UA or a PDF/A of level
// A, then has the QR code as a figure of that description. Without one, it is
// decoration, which a screen reader skips.
func (qrcode *QRCode) SetAltDescription(altDescription string) *QRCode {
	qrcode.altDescription = altDescription
	return qrcode
}

// GetModules returns the modules of the QR code: true for dark and false for light modules.
func (qrcode *QRCode) GetModules() [][]*bool {
	modules := make([][]*bool, qrcode.moduleCount)
	for row := range modules {
		modules[row] = make([]*bool, qrcode.moduleCount)
		for col := range modules[row] {
			dark := qrcode.modules[row][col]
			modules[row][col] = &dark
		}
	}
	return modules
}

// make places the function patterns and the codewords, and masks them with
// the mask pattern of the lowest penalty, with its format information.
func (qrcode *QRCode) make(data []byte) {
	qrcode.modules = newMatrix(qrcode.moduleCount)
	qrcode.reserved = newMatrix(qrcode.moduleCount)

	qrcode.setupPositionProbePattern(0, 0)
	qrcode.setupPositionProbePattern(qrcode.moduleCount-7, 0)
	qrcode.setupPositionProbePattern(0, qrcode.moduleCount-7)

	qrcode.setupPositionAdjustPattern()
	qrcode.setupTimingPattern()
	qrcode.setupTypeInfo(qrcode.modules, 0) // Reserves the modules of the format information
	if qrcode.typeNumber >= 7 {
		qrcode.setupTypeNumber()
	}
	qrcode.mapData(data)

	// Each mask is tried with its format information in place, as ISO/IEC
	// 18004 asks, and the first of the lowest penalty is taken.
	var best [][]bool
	minLostPoint := 0
	for maskPattern := 0; maskPattern < 8; maskPattern++ {
		modules := qrcode.applyMask(maskPattern)
		lostPoint := getLostPoint(modules)
		if best == nil || lostPoint < minLostPoint {
			minLostPoint = lostPoint
			best = modules
		}
	}
	qrcode.modules = best
	qrcode.reserved = nil
}

// newMatrix returns a square matrix of light modules.
func newMatrix(moduleCount int) [][]bool {
	matrix := make([][]bool, moduleCount)
	for i := range matrix {
		matrix[i] = make([]bool, moduleCount)
	}
	return matrix
}

// set sets the module of a function pattern or of the format or version
// information.
func (qrcode *QRCode) set(row, col int, dark bool) {
	qrcode.modules[row][col] = dark
	qrcode.reserved[row][col] = true
}

// applyMask returns the modules with the mask pattern applied to the
// codewords, and the format information of the mask pattern.
func (qrcode *QRCode) applyMask(maskPattern int) [][]bool {
	modules := newMatrix(qrcode.moduleCount)
	for row := 0; row < qrcode.moduleCount; row++ {
		for col := 0; col < qrcode.moduleCount; col++ {
			dark := qrcode.modules[row][col]
			if !qrcode.reserved[row][col] && getMask(maskPattern, row, col) {
				dark = !dark
			}
			modules[row][col] = dark
		}
	}
	qrcode.setupTypeInfo(modules, maskPattern)
	return modules
}

// mapData places the bits of the codewords in the modules that are not
// reserved, in two module wide columns from the bottom right corner, up and
// down in turn; the modules left over are light.
func (qrcode *QRCode) mapData(data []byte) {
	inc := -1
	row := qrcode.moduleCount - 1
	bitIndex := 7
	byteIndex := 0

	for col := qrcode.moduleCount - 1; col > 0; col -= 2 {
		if col == 6 {
			col--
		}
		for {
			for c := 0; c < 2; c++ {
				if !qrcode.reserved[row][col-c] {
					dark := false
					if byteIndex < len(data) {
						dark = (((data[byteIndex] >> bitIndex) & 1) == 1)
					}
					qrcode.modules[row][col-c] = dark
					bitIndex--
					if bitIndex == -1 {
						byteIndex++
						bitIndex = 7
					}
				}
			}

			row += inc
			if row < 0 || qrcode.moduleCount <= row {
				row -= inc
				inc = -inc
				break
			}
		}
	}
}

func (qrcode *QRCode) setupPositionAdjustPattern() {
	pos := getPatternPosition(qrcode.typeNumber)
	for i := 0; i < len(pos); i++ {
		for j := 0; j < len(pos); j++ {
			row := pos[i]
			col := pos[j]
			if qrcode.reserved[row][col] {
				continue
			}
			for r := -2; r <= 2; r++ {
				for c := -2; c <= 2; c++ {
					qrcode.set(row+r, col+c, r == -2 || r == 2 || c == -2 || c == 2 || (r == 0 && c == 0))
				}
			}
		}
	}
}

func (qrcode *QRCode) setupPositionProbePattern(row, col int) {
	for r := -1; r <= 7; r++ {
		for c := -1; c <= 7; c++ {
			if row+r <= -1 || qrcode.moduleCount <= row+r || col+c <= -1 || qrcode.moduleCount <= col+c {
				continue
			}
			qrcode.set(row+r, col+c, (0 <= r && r <= 6 && (c == 0 || c == 6)) ||
				(0 <= c && c <= 6 && (r == 0 || r == 6)) ||
				(2 <= r && r <= 4 && 2 <= c && c <= 4))
		}
	}
}

func (qrcode *QRCode) setupTimingPattern() {
	for r := 8; r < qrcode.moduleCount-8; r++ {
		if !qrcode.reserved[r][6] {
			qrcode.set(r, 6, r%2 == 0)
		}
	}
	for c := 8; c < qrcode.moduleCount-8; c++ {
		if !qrcode.reserved[6][c] {
			qrcode.set(6, c, c%2 == 0)
		}
	}
}

// setupTypeNumber places the version number of versions 7 and up twice,
// next to two finder patterns.
func (qrcode *QRCode) setupTypeNumber() {
	bits := getBCHTypeNumber(qrcode.typeNumber)
	for i := 0; i < 18; i++ {
		dark := ((bits >> i) & 1) == 1
		qrcode.set(i/3, i%3+qrcode.moduleCount-8-3, dark)
		qrcode.set(i%3+qrcode.moduleCount-8-3, i/3, dark)
	}
}

// setupTypeInfo places the format information, the error correction level
// and the mask pattern, twice in the modules, and the dark module next to the
// bottom left finder pattern. The modules it takes are reserved.
func (qrcode *QRCode) setupTypeInfo(modules [][]bool, maskPattern int) {
	data := int(qrcode.errorCorrectionLevel)<<3 | maskPattern
	bits := getBCHTypeInfo(data)
	put := func(row, col int, dark bool) {
		modules[row][col] = dark
		qrcode.reserved[row][col] = true
	}

	for i := 0; i < 15; i++ {
		dark := ((bits >> i) & 1) == 1
		if i < 6 {
			put(i, 8, dark)
		} else if i < 8 {
			put(i+1, 8, dark)
		} else {
			put(qrcode.moduleCount-15+i, 8, dark)
		}
	}

	for i := 0; i < 15; i++ {
		dark := ((bits >> i) & 1) == 1
		if i < 8 {
			put(8, qrcode.moduleCount-i-1, dark)
		} else if i < 9 {
			put(8, 15-i-1+1, dark)
		} else {
			put(8, 15-i-1, dark)
		}
	}

	put(qrcode.moduleCount-8, 8, true)
}

func (qrcode *QRCode) createData(errorCorrectionLevel errorcorrectionlevel.ErrorCorrectionLevel) []byte {
	rsblock := new(qrRSBlock)
	rsBlocks := rsblock.getRSBlocks(qrcode.typeNumber, errorCorrectionLevel)

	var buffer = newBitBuffer()
	if qrcode.eci {
		buffer.put(7, 4)  // ECI
		buffer.put(26, 8) // UTF-8
	}
	buffer.put(4, 4) // Byte mode
	buffer.put(len(qrcode.qrData), getCharacterCountBits(qrcode.typeNumber))
	for i := 0; i < len(qrcode.qrData); i++ {
		buffer.put(int(qrcode.qrData[i]), 8)
	}

	totalDataCount := 0
	for i := 0; i < len(rsBlocks); i++ {
		totalDataCount += rsBlocks[i].getDataCount()
	}

	if buffer.getLengthInBits() > totalDataCount*8 {
		panic("String length overflow. (" +
			strconv.Itoa(buffer.getLengthInBits()) + ">" +
			strconv.Itoa(totalDataCount*8) + ")")
	}

	if buffer.getLengthInBits()+4 <= totalDataCount*8 {
		buffer.put(0, 4)
	}

	// padding
	for (buffer.getLengthInBits() % 8) != 0 {
		buffer.putBit(false)
	}

	// padding
	for {
		if buffer.getLengthInBits() >= totalDataCount*8 {
			break
		}
		buffer.put(qrcode.pad0, 8)
		if buffer.getLengthInBits() >= totalDataCount*8 {
			break
		}
		buffer.put(qrcode.pad1, 8)
	}

	return qrcode.createBytes(buffer, rsBlocks)
}

func maxOfIntegers(a, b int) int {
	if a >= b {
		return a
	}
	return b
}

func (qrcode *QRCode) createBytes(buffer *bitBuffer, rsBlocks []*qrRSBlock) []byte {
	offset := 0
	maxDcCount := 0
	maxEcCount := 0
	dcdata := make([][]int, len(rsBlocks))
	ecdata := make([][]int, len(rsBlocks))

	for r := 0; r < len(rsBlocks); r++ {
		dcCount := rsBlocks[r].getDataCount()
		ecCount := rsBlocks[r].getTotalCount() - dcCount

		maxDcCount = maxOfIntegers(maxDcCount, dcCount)
		maxEcCount = maxOfIntegers(maxEcCount, ecCount)

		dcdata[r] = make([]int, dcCount)
		for i := 0; i < len(dcdata[r]); i++ {
			dcdata[r][i] = int(0xff) & int(buffer.getBuffer()[i+offset])
		}
		offset += dcCount

		rsPoly := getErrorCorrectPolynomial(ecCount)
		rawPoly := newQRPolynomial(dcdata[r], rsPoly.getLength()-1)
		modPoly := rawPoly.mod(rsPoly)
		ecdata[r] = make([]int, rsPoly.getLength()-1)
		for i := 0; i < len(ecdata[r]); i++ {
			modIndex := i + modPoly.getLength() - len(ecdata[r])
			ecdata[r][i] = 0
			if modIndex >= 0 {
				ecdata[r][i] = modPoly.get(modIndex)
			}
		}
	}

	totalCodeCount := 0
	for i := 0; i < len(rsBlocks); i++ {
		totalCodeCount += rsBlocks[i].getTotalCount()
	}

	data := make([]byte, totalCodeCount)
	index := 0
	for i := 0; i < maxDcCount; i++ {
		for r := 0; r < len(rsBlocks); r++ {
			if i < len(dcdata[r]) {
				data[index] = byte(dcdata[r][i])
				index++
			}
		}
	}

	for i := 0; i < maxEcCount; i++ {
		for r := 0; r < len(rsBlocks); r++ {
			if i < len(ecdata[r]) {
				data[index] = byte(ecdata[r][i])
				index++
			}
		}
	}

	return data
}
