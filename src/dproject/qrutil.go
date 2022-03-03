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
	"fmt"
	"math"
	"os"
)

func getErrorCorrectPolynomial(errorCorrectLength int) *Polynomial {
	buf1 := make([]int, 1)
	buf1[0] = 1
	polynomial := NewPolynomial(buf1, 0)
	for i := 0; i < errorCorrectLength; i++ {
		buf2 := make([]int, 2)
		buf2[0] = 1
		buf2[1] = NewQRMath().gexp(i)
		polynomial = polynomial.multiply(NewPolynomial(buf2, 0))
	}
	return polynomial
}

func getMask(maskPattern, i, j int) bool {
	switch maskPattern {

	case PATTERN000:
		return (i+j)%2 == 0
	case PATTERN001:
		return (i % 2) == 0
	case PATTERN010:
		return (j % 3) == 0
	case PATTERN011:
		return (i+j)%3 == 0
	case PATTERN100:
		return (i/2+j/3)%2 == 0
	case PATTERN101:
		return (i*j)%2+(i*j)%3 == 0
	case PATTERN110:
		return ((i*j)%2+(i*j)%3)%2 == 0
	case PATTERN111:
		return ((i*j)%3+(i+j)%2)%2 == 0

	default:
		fmt.Println("Illegal mask pattern.")
		os.Exit(1)
	}

	return false
}

func getLostPoint(qrCode *QRCode) int {
	moduleCount := qrCode.getModuleCount()
	lostPoint := 0

	// LEVEL1
	for row := 0; row < moduleCount; row++ {
		for col := 0; col < moduleCount; col++ {
			sameCount := 0
			dark := qrCode.isDark(row, col)
			for r := -1; r <= 1; r++ {
				if row+r < 0 || moduleCount <= row+r {
					continue
				}
				for c := -1; c <= 1; c++ {
					if col+c < 0 || moduleCount <= col+c {
						continue
					}
					if r == 0 && c == 0 {
						continue
					}
					if dark == qrCode.isDark(row+r, col+c) {
						sameCount++
					}
				}
			}
			if sameCount > 5 {
				lostPoint += (3 + sameCount - 5)
			}
		}
	}

	// LEVEL2
	for row := 0; row < moduleCount-1; row++ {
		for col := 0; col < moduleCount-1; col++ {
			count := 0
			if qrCode.isDark(row, col) {
				count++
			}
			if qrCode.isDark(row+1, col) {
				count++
			}
			if qrCode.isDark(row, col+1) {
				count++
			}
			if qrCode.isDark(row+1, col+1) {
				count++
			}
			if count == 0 || count == 4 {
				lostPoint += 3
			}
		}
	}

	// LEVEL3
	for row := 0; row < moduleCount; row++ {
		for col := 0; col < moduleCount-6; col++ {
			if qrCode.isDark(row, col) &&
				!qrCode.isDark(row, col+1) &&
				qrCode.isDark(row, col+2) &&
				qrCode.isDark(row, col+3) &&
				qrCode.isDark(row, col+4) &&
				!qrCode.isDark(row, col+5) &&
				qrCode.isDark(row, col+6) {
				lostPoint += 40
			}
		}
	}

	for col := 0; col < moduleCount; col++ {
		for row := 0; row < moduleCount-6; row++ {
			if qrCode.isDark(row, col) &&
				!qrCode.isDark(row+1, col) &&
				qrCode.isDark(row+2, col) &&
				qrCode.isDark(row+3, col) &&
				qrCode.isDark(row+4, col) &&
				!qrCode.isDark(row+5, col) &&
				qrCode.isDark(row+6, col) {
				lostPoint += 40
			}
		}
	}

	// LEVEL4
	darkCount := 0
	for col := 0; col < moduleCount; col++ {
		for row := 0; row < moduleCount; row++ {
			if qrCode.isDark(row, col) {
				darkCount++
			}
		}
	}

	ratio := int(math.Abs(100.0*float64(darkCount)/float64(moduleCount)/float64(moduleCount)-50.0) / 5.0)
	lostPoint += ratio * 10.0

	return lostPoint
}

// g15 returns ... TODO
func g15() int {
	return (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0)
}

// g15Mask returns ... TODO
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

func getBCHDigit(data int) int {
	digit := 0
	for data != 0 {
		digit++
		// TODO:
		data >>= 1
	}
	return digit
}
