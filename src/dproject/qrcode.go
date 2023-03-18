package dproject

/**
 *
Copyright (c) 2009 Kazuhiko Arase

URL: http://www.d-project.com/

Licensed under the MIT license:
  http://www.opensource.org/licenses/mit-license.php

The word "QR Code" is registered trademark of
DENSO WAVE INCORPORATED
  http://www.denso-wave.com/qrcode/faqpatent-e.html
*/

import (
	"log"

	pdfjet "github.com/edragoev1/pdfjet/src"
)

// QRCode used to create 2D QR Code barcodes. Please see Example_20.
// @author Kazuhiko Arase
type QRCode struct {
	PAD0              int
	PAD1              int
	modules           [][]*bool
	moduleCount       int
	errorCorrectLevel int
	x                 float32
	y                 float32
	qrData            []byte
	m1                float32 // Module length
	color             int32
}

// NewQRCode is used to create 2D QR Code barcodes.
// @param str the string to encode.
// @param errorCorrectLevel the desired error correction level.
func NewQRCode(str string, errorCorrectLevel int) *QRCode {
	qrcode := new(QRCode)
	qrcode.PAD0 = 0xEC
	qrcode.PAD1 = 0x11
	qrcode.qrData = []byte(str)
	qrcode.moduleCount = 33 // Magic Number
	qrcode.m1 = 2.0
	qrcode.errorCorrectLevel = errorCorrectLevel
	qrcode.make(false, qrcode.getBestMaskPattern())
	return qrcode
}

// SetPosition sets the position where this barcode will be drawn on the page.
// @param x the x coordinate of the top left corner of the barcode.
// @param y the y coordinate of the top left corner of the barcode.
func (qrcode *QRCode) SetPosition(x, y float32) {
	qrcode.SetLocation(x, y)
}

// SetLocation sets the location where this barcode will be drawn on the page.
// @param x the x coordinate of the top left corner of the barcode.
// @param y the y coordinate of the top left corner of the barcode.
func (qrcode *QRCode) SetLocation(x, y float32) {
	qrcode.x = x
	qrcode.y = y
}

// SetModuleLength sets the module length of this barcode.
// The default value is 2.0f
// @param moduleLength the specified module length.
func (qrcode *QRCode) SetModuleLength(moduleLength float32) {
	qrcode.m1 = moduleLength
}

// SetColor sets the color of the barcode.
func (qrcode *QRCode) SetColor(color int32) {
	qrcode.color = color
}

// DrawOn draws this barcode on the specified page.
// @param page the specified page.
// @return x and y coordinates of the bottom right corner of this component.
func (qrcode *QRCode) DrawOn(page *pdfjet.Page) []float32 {
	page.SetBrushColor(qrcode.color)
	for row := 0; row < len(qrcode.modules); row++ {
		for col := 0; col < len(qrcode.modules); col++ {
			if qrcode.isDark(row, col) {
				page.FillRect(
					qrcode.x+float32(col)*qrcode.m1,
					qrcode.y+float32(row)*qrcode.m1,
					qrcode.m1,
					qrcode.m1)
			}
		}
	}
	w := qrcode.m1 * float32(len(qrcode.modules))
	h := qrcode.m1 * float32(len(qrcode.modules))
	return []float32{qrcode.x + w, qrcode.y + h}
}

func (qrcode *QRCode) getData() [][]*bool {
	return qrcode.modules
}

func (qrcode *QRCode) isDark(row, col int) bool {
	if qrcode.modules[row][col] != nil {
		return *qrcode.modules[row][col]
	}
	return false
}

func (qrcode *QRCode) getModuleCount() int {
	return qrcode.moduleCount
}

func (qrcode *QRCode) getBestMaskPattern() int {
	minLostPoint := 0
	pattern := 0
	for i := 0; i < 8; i++ {
		qrcode.make(true, i)
		lostPoint := getLostPoint(qrcode)
		if i == 0 || minLostPoint > lostPoint {
			minLostPoint = lostPoint
			pattern = i
		}
	}
	return pattern
}

func (qrcode *QRCode) make(test bool, maskPattern int) {
	qrcode.modules = make([][]*bool, qrcode.moduleCount)
	for i := range qrcode.modules {
		qrcode.modules[i] = make([]*bool, qrcode.moduleCount)
	}

	qrcode.setupPositionProbePattern(0, 0)
	qrcode.setupPositionProbePattern(qrcode.moduleCount-7, 0)
	qrcode.setupPositionProbePattern(0, qrcode.moduleCount-7)

	qrcode.setupPositionAdjustPattern()
	qrcode.setupTimingPattern()
	qrcode.setupTypeInfo(test, maskPattern)

	qrcode.mapData(qrcode.createData(qrcode.errorCorrectLevel), maskPattern)
}

func (qrcode *QRCode) mapData(data []byte, maskPattern int) {
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
				if qrcode.modules[row][col-c] == nil {
					dark := false
					if byteIndex < len(data) {
						dark = (((data[byteIndex] >> bitIndex) & 1) == 1)
					}
					mask := getMask(maskPattern, row, col-c)
					if mask {
						dark = !dark
					}
					qrcode.modules[row][col-c] = &dark
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
	pos := []int{6, 26} // Magic Numbers
	for i := 0; i < len(pos); i++ {
		for j := 0; j < len(pos); j++ {
			row := pos[i]
			col := pos[j]
			if qrcode.modules[row][col] != nil {
				continue
			}
			for r := -2; r <= 2; r++ {
				for c := -2; c <= 2; c++ {
					value := r == -2 || r == 2 || c == -2 || c == 2 || (r == 0 && c == 0)
					qrcode.modules[row+r][col+c] = &value
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
			value := (0 <= r && r <= 6 && (c == 0 || c == 6)) ||
				(0 <= c && c <= 6 && (r == 0 || r == 6)) ||
				(2 <= r && r <= 4 && 2 <= c && c <= 4)
			qrcode.modules[row+r][col+c] = &value
		}
	}
}

func (qrcode *QRCode) setupTimingPattern() {
	for r := 8; r < qrcode.moduleCount-8; r++ {
		if qrcode.modules[r][6] != nil {
			continue
		}
		value := r%2 == 0
		qrcode.modules[r][6] = &value
	}
	for c := 8; c < qrcode.moduleCount-8; c++ {
		if qrcode.modules[6][c] != nil {
			continue
		}
		value := c%2 == 0
		qrcode.modules[6][c] = &value
	}
}

func (qrcode *QRCode) setupTypeInfo(test bool, maskPattern int) {
	data := qrcode.errorCorrectLevel<<3 | maskPattern
	bits := getBCHTypeInfo(data)

	for i := 0; i < 15; i++ {
		mod := (!test && ((bits>>i)&1) == 1)
		if i < 6 {
			qrcode.modules[i][8] = &mod
		} else if i < 8 {
			qrcode.modules[i+1][8] = &mod
		} else {
			qrcode.modules[qrcode.moduleCount-15+i][8] = &mod
		}
	}

	for i := 0; i < 15; i++ {
		mod := (!test && ((bits>>i)&1) == 1)
		if i < 8 {
			qrcode.modules[8][qrcode.moduleCount-i-1] = &mod
		} else if i < 9 {
			qrcode.modules[8][15-i-1+1] = &mod
		} else {
			qrcode.modules[8][15-i-1] = &mod
		}
	}

	value := !test
	qrcode.modules[qrcode.moduleCount-8][8] = &value
}

func (qrcode *QRCode) createData(errorCorrectLevel int) []byte {
	rsblock := new(RSBlock)
	rsBlocks := rsblock.getRSBlocks(errorCorrectLevel)

	var buffer = NewBitBuffer()
	buffer.put(4, 4)
	buffer.put(len(qrcode.qrData), 8)
	for i := 0; i < len(qrcode.qrData); i++ {
		buffer.put(int(qrcode.qrData[i]), 8)
	}

	totalDataCount := 0
	for i := 0; i < len(rsBlocks); i++ {
		totalDataCount += rsBlocks[i].getDataCount()
	}

	if buffer.getLengthInBits() > totalDataCount*8 {
		log.Fatal("String length overflow. (" + string(buffer.getLengthInBits()) + ")")
		/* TODO:
			   throw new IllegalArgumentException("String length overflow. ("
		           + buffer.getLengthInBits()
		           + ">"
		           +  totalDataCount * 8
		           + ")")
		*/
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
		buffer.put(qrcode.PAD0, 8)
		if buffer.getLengthInBits() >= totalDataCount*8 {
			break
		}
		buffer.put(qrcode.PAD1, 8)
	}

	return qrcode.createBytes(buffer, rsBlocks)
}

func maxOfIntegers(a, b int) int {
	if a >= b {
		return a
	}
	return b
}

func (qrcode *QRCode) createBytes(buffer *BitBuffer, rsBlocks []*RSBlock) []byte {
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
		rawPoly := NewPolynomial(dcdata[r], rsPoly.getLength()-1)
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
