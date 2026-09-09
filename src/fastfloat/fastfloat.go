package fastfloat

import (
	"math"
)

// ToByteArray converts a float32 to its byte array representation
func ToByteArray(value float32) []byte {
	// Handle special cases
	if math.IsNaN(float64(value)) {
		return []byte("NaN")
	}
	if math.IsInf(float64(value), 1) {
		return []byte("Infinity")
	}
	if math.IsInf(float64(value), -1) {
		return []byte("-Infinity")
	}

	// Round to 2 decimal places
	rounded := math.Round(float64(value*100)) / 100

	negative := rounded < 0
	if negative {
		rounded = -rounded
	}

	integerPart := int(rounded)
	decimalPart := rounded - float64(integerPart)
	decimalDigits := int(math.Round(decimalPart * 100))

	// Handle carry-over from rounding
	if decimalDigits >= 100 {
		decimalDigits -= 100
		integerPart++
	}

	// Determine if we need decimal places
	hasDecimal := decimalDigits > 0
	trailingZeros := 0

	if hasDecimal {
		// Count trailing zeros
		if decimalDigits%10 == 0 {
			trailingZeros = 1
			if decimalDigits/10%10 == 0 {
				trailingZeros = 2
			}
		}
		hasDecimal = trailingZeros < 2
	}

	// Calculate lengths
	intDigits := 1
	if integerPart > 0 {
		intDigits = int(math.Log10(float64(integerPart))) + 1
	}
	totalLength := 0
	if negative {
		totalLength = 1 + intDigits
	} else {
		totalLength = intDigits
	}
	if hasDecimal {
		totalLength += 1 + (2 - trailingZeros)
	}

	result := make([]byte, totalLength)
	pos := 0

	// Add sign
	if negative {
		result[pos] = '-'
		pos++
	}

	// Add integer part
	pos = writeInt(integerPart, result, pos, intDigits)

	// Add decimal part if needed
	if hasDecimal {
		result[pos] = '.'
		pos++
		if trailingZeros < 2 {
			result[pos] = byte('0' + decimalDigits/10)
			pos++
		}
		if trailingZeros < 1 {
			result[pos] = byte('0' + decimalDigits%10)
			pos++
		}
	}

	return result
}

func writeInt(value int, buffer []byte, pos int, digits int) int {
	for i := digits - 1; i >= 0; i-- {
		buffer[pos+i] = byte('0' + value%10)
		value /= 10
	}
	return pos + digits
}
