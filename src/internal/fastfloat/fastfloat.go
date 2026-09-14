// Package fastfloat converts float32 values to their text representation.
package fastfloat

import (
	"math"
	"strconv"
)

// IsWritable returns true for a number a PDF can hold: a PDF number has no
// exponent and no NaN or infinity, and readers keep integers in 32 bits, so a
// number must be finite and below 2^31.
func IsWritable(value float32) bool {
	return math.Abs(float64(value)) < 2147483648 // False for NaN and the infinities
}

// ToByteArray converts a float32 to its byte array representation. A value
// that is not writable is written as 0, so a caller that did not check it
// still writes valid syntax; the callers of the pdfjet package check it.
func ToByteArray(value float32) []byte {
	if !IsWritable(value) {
		return []byte("0")
	}

	magnitude := math.Abs(float64(value))
	if magnitude >= 8388608 {
		// A float of 2^23 or more is a whole number: write all its digits
		digits := strconv.FormatFloat(magnitude, 'f', 0, 64)
		if value < 0 {
			return []byte("-" + digits)
		}
		return []byte(digits)
	}

	// Round to 2 decimal places, halves away from zero. A float times 100
	// is exact in a float64, so the exact value of the float is rounded.
	scaled := magnitude * 100
	hundredths := int(scaled)
	if scaled-float64(hundredths) >= 0.5 {
		hundredths++
	}

	// A value that rounds to zero, like -0.001 and -0.0, is written 0
	negative := value < 0 && hundredths > 0
	integerPart := hundredths / 100
	decimalDigits := hundredths % 100

	// Count the digits, leaving out the trailing zeros of the decimals
	intDigits := 1
	for i := integerPart; i >= 10; i /= 10 {
		intDigits++
	}
	fractionDigits := 0
	if decimalDigits > 0 {
		fractionDigits = 2
		if decimalDigits%10 == 0 {
			fractionDigits = 1
		}
	}
	totalLength := intDigits
	if negative {
		totalLength++
	}
	if fractionDigits > 0 {
		totalLength += 1 + fractionDigits
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
	if fractionDigits > 0 {
		result[pos] = '.'
		pos++
		result[pos] = byte('0' + decimalDigits/10)
		pos++
		if fractionDigits > 1 {
			result[pos] = byte('0' + decimalDigits%10)
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
