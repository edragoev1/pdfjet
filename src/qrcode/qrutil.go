// qrutil.go
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

package qrcode

import (
	"strconv"
)

// patternPositionTable holds the centers of the alignment patterns of each
// version, in rows and columns.
var patternPositionTable = [][]int{
	{},
	{6, 18},
	{6, 22},
	{6, 26},
	{6, 30},
	{6, 34},
	{6, 22, 38},
	{6, 24, 42},
	{6, 26, 46},
	{6, 28, 50},
	{6, 30, 54},
	{6, 32, 58},
	{6, 34, 62},
	{6, 26, 46, 66},
	{6, 26, 48, 70},
	{6, 26, 50, 74},
	{6, 30, 54, 78},
	{6, 30, 56, 82},
	{6, 30, 58, 86},
	{6, 34, 62, 90},
	{6, 28, 50, 72, 94},
	{6, 26, 50, 74, 98},
	{6, 30, 54, 78, 102},
	{6, 28, 54, 80, 106},
	{6, 32, 58, 84, 110},
	{6, 30, 58, 86, 114},
	{6, 34, 62, 90, 118},
	{6, 26, 50, 74, 98, 122},
	{6, 30, 54, 78, 102, 126},
	{6, 26, 52, 78, 104, 130},
	{6, 30, 56, 82, 108, 134},
	{6, 34, 60, 86, 112, 138},
	{6, 30, 58, 86, 114, 142},
	{6, 34, 62, 90, 118, 146},
	{6, 30, 54, 78, 102, 126, 150},
	{6, 24, 50, 76, 102, 128, 154},
	{6, 28, 54, 80, 106, 132, 158},
	{6, 32, 58, 84, 110, 136, 162},
	{6, 26, 54, 82, 110, 138, 166},
	{6, 30, 58, 86, 114, 142, 170},
}

func getPatternPosition(typeNumber int) []int {
	return patternPositionTable[typeNumber-1]
}

func getErrorCorrectPolynomial(errorCorrectLength int) *qrPolynomial {
	buf1 := make([]int, 1)
	buf1[0] = 1
	polynomial := newQRPolynomial(buf1, 0)
	for i := 0; i < errorCorrectLength; i++ {
		buf2 := make([]int, 2)
		buf2[0] = 1
		buf2[1] = gexp(i)
		polynomial = polynomial.multiply(newQRPolynomial(buf2, 0))
	}
	return polynomial
}

func getMask(maskPattern, i, j int) bool {
	switch maskPattern {

	case pattern000:
		return (i+j)%2 == 0
	case pattern001:
		return (i % 2) == 0
	case pattern010:
		return (j % 3) == 0
	case pattern011:
		return (i+j)%3 == 0
	case pattern100:
		return (i/2+j/3)%2 == 0
	case pattern101:
		return (i*j)%2+(i*j)%3 == 0
	case pattern110:
		return ((i*j)%2+(i*j)%3)%2 == 0
	case pattern111:
		return ((i*j)%3+(i+j)%2)%2 == 0

	default:
		panic("Illegal mask pattern: " + strconv.Itoa(maskPattern))
	}
}

// getLostPoint returns the penalty of the modules with the rules of ISO/IEC
// 18004 7.8.3.1: N1 = 3 for five modules of a color in a row or a column, and
// 1 for each one more; N2 = 3 for each block of 2 by 2 modules of a color; N3
// = 40 for each dark-light-dark-dark-dark-light-dark pattern of a row or a
// column with 4 light modules before or after it, the light quiet zone
// counting; and N4 = 10 for each 5% that the dark modules are away from half
// the modules.
func getLostPoint(modules [][]bool) int {
	moduleCount := len(modules)
	lostPoint := 0
	// dark returns the module of the row and the column, or of the column and
	// the row across; outside the symbol is the quiet zone, which is light.
	dark := func(across bool, i, j int) bool {
		if j < 0 || j >= moduleCount {
			return false
		}
		if across {
			return modules[j][i]
		}
		return modules[i][j]
	}

	for _, across := range []bool{false, true} {
		for i := 0; i < moduleCount; i++ {
			// N1
			run := 1
			for j := 1; j <= moduleCount; j++ {
				if j < moduleCount && dark(across, i, j) == dark(across, i, j-1) {
					run++
					continue
				}
				if run >= 5 {
					lostPoint += 3 + run - 5
				}
				run = 1
			}
			// N3
			for j := 0; j+6 < moduleCount; j++ {
				if dark(across, i, j) &&
					!dark(across, i, j+1) &&
					dark(across, i, j+2) &&
					dark(across, i, j+3) &&
					dark(across, i, j+4) &&
					!dark(across, i, j+5) &&
					dark(across, i, j+6) &&
					(isLight(dark, across, i, j-4, j) || isLight(dark, across, i, j+7, j+11)) {
					lostPoint += 40
				}
			}
		}
	}

	// N2
	for row := 0; row < moduleCount-1; row++ {
		for col := 0; col < moduleCount-1; col++ {
			d := modules[row][col]
			if d == modules[row+1][col] && d == modules[row][col+1] && d == modules[row+1][col+1] {
				lostPoint += 3
			}
		}
	}

	// N4
	darkCount := 0
	for row := 0; row < moduleCount; row++ {
		for col := 0; col < moduleCount; col++ {
			if modules[row][col] {
				darkCount++
			}
		}
	}
	total := moduleCount * moduleCount
	lostPoint += abs(2*darkCount-total) * 10 / total * 10

	return lostPoint
}

// isLight tells if the modules from j to k, not including k, of the row or
// the column are light.
func isLight(dark func(across bool, i, j int) bool, across bool, i, j, k int) bool {
	for ; j < k; j++ {
		if dark(across, i, j) {
			return false
		}
	}
	return true
}

func abs(value int) int {
	if value < 0 {
		return -value
	}
	return value
}

// g15 returns the generator polynomial of the BCH(15, 5) code of the format
// information: x^10 + x^8 + x^5 + x^4 + x^2 + x + 1.
func g15() int {
	return (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0)
}

// g15Mask returns the mask pattern 101010000010010 that is XORed with the
// format information, so it is never all zeros.
func g15Mask() int {
	return (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1)
}

func getBCHTypeInfo(data int) int {
	d := data << 10
	for getBCHDigit(d)-getBCHDigit(g15()) >= 0 {
		d ^= (g15() << (getBCHDigit(d) - getBCHDigit(g15())))
	}
	return ((data << 10) | d) ^ g15Mask()
}

// g18 returns the generator polynomial of the BCH(18, 6) code of the version
// information: x^12 + x^11 + x^10 + x^9 + x^8 + x^5 + x^2 + 1.
func g18() int {
	return (1 << 12) | (1 << 11) | (1 << 10) | (1 << 9) | (1 << 8) | (1 << 5) | (1 << 2) | (1 << 0)
}

func getBCHTypeNumber(data int) int {
	d := data << 12
	for getBCHDigit(d)-getBCHDigit(g18()) >= 0 {
		d ^= (g18() << (getBCHDigit(d) - getBCHDigit(g18())))
	}
	return (data << 12) | d
}

func getBCHDigit(data int) int {
	digit := 0
	for data != 0 {
		digit++
		data >>= 1
	}
	return digit
}
