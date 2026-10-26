// chartscale.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

// What the charts share, Chart, BarChart, SimpleBarChart and
// SimpleScatterChart: the numbers of their labels, the rounding of an axis to
// steps, the spacing of the labels of an axis, the colors of the series that
// have none, and the test of a value to draw. A chart uses these, not
// another chart, so that one can go without the others.

import (
	"math"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
)

// roundedRange is the range of an axis and its number of grid lines.
type roundedRange struct {
	minValue       float32
	maxValue       float32
	numOfGridLines int
}

// newRoundedRange constructor.
func newRoundedRange() *roundedRange {
	round := new(roundedRange)
	return round
}

// defaultPalette holds the stroke colors of the series that have no color.
var defaultPalette = [...]int32{
	color.Blue,
	color.Red,
	color.Green,
	color.Orange,
	color.Purple,
	color.DarkCyan,
	color.Magenta,
	color.Olive,
}

// format formats a label with minDigits to maxDigits decimal places, rounding
// the exact value half to even. The label has a "." decimal separator and no
// grouping, and a value that rounds to zero has no minus sign.
func format(value float32, minDigits, maxDigits int) string {
	if math.IsNaN(float64(value)) {
		return "NaN"
	}
	if math.IsInf(float64(value), 0) {
		if value < 0 {
			return "-Infinity"
		}
		return "Infinity"
	}
	// A minimum above the maximum is lowered to it, as in Java's NumberFormat
	maxDigits = max(maxDigits, 0)
	minDigits = min(max(minDigits, 0), maxDigits)
	label := strconv.FormatFloat(float64(value), 'f', maxDigits, 64)
	if point := strings.IndexByte(label, '.'); point != -1 {
		end := len(label)
		for end-point-1 > minDigits && label[end-1] == '0' {
			end--
		}
		if end-point-1 == 0 {
			end = point
		}
		label = label[:end]
	}
	if strings.Trim(label, "-0.") == "" {
		label = strings.TrimPrefix(label, "-")
	}
	return label
}

// fractionDigitsOf returns the number of decimal places, at most maxDigits,
// that write the axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
func fractionDigitsOf(step float32, maxDigits int) int {
	for digits := 0; digits < maxDigits; digits++ {
		scaled := float64(float64(step) * math.Pow(10, float64(digits)))
		if math.Abs(scaled-math.Round(scaled)) < 1e-4 {
			return digits
		}
	}
	return max(maxDigits, 0)
}

// flatDataSpan returns the span of the range of flat data at the value: 1,
// or a hundred thousandth of the value when that is more, since above 2^24 a
// float32 has no room for 1 more, and the range would stay flat.
func flatDataSpan(value float32) float32 {
	return max(1.0, float32(abs32(value)*1e-5))
}

// roundMaxAndMinValues rounds the axis range to "nice" values for clean grid lines.
// Uses the span (max - min) to support negative values and zero crossings.
// Rounds max up and min down to step multiples, then recomputes grid lines
// to ensure they match the final rounded range.
func roundMaxAndMinValues(maxValue, minValue float32) *roundedRange {
	span := maxValue - minValue
	if !isValue(span) {
		return unitRange() // No number, or a span past the largest float32
	}
	if span <= 0 {
		span = flatDataSpan(minValue) // Guard against flat data
	}

	exponent := int(math.Floor(math.Log(float64(span)) / math.Log(10)))
	normalizedSpan := float32(span * float32(math.Pow(10, float64(-exponent))))

	// Snap span up to a "nice" value with paired grid line count
	var niceSpan float32
	var numOfGridLines int

	if normalizedSpan > 9.00 {
		niceSpan = 10.0
		numOfGridLines = 10
	} else if normalizedSpan > 8.00 {
		niceSpan = 9.00
		numOfGridLines = 9
	} else if normalizedSpan > 7.00 {
		niceSpan = 8.00
		numOfGridLines = 8
	} else if normalizedSpan > 6.00 {
		niceSpan = 7.00
		numOfGridLines = 7
	} else if normalizedSpan > 5.00 {
		niceSpan = 6.00
		numOfGridLines = 6
	} else if normalizedSpan > 4.00 {
		niceSpan = 5.00
		numOfGridLines = 5
	} else if normalizedSpan > 3.50 {
		niceSpan = 4.00
		numOfGridLines = 8
	} else if normalizedSpan > 3.00 {
		niceSpan = 3.50
		numOfGridLines = 7
	} else if normalizedSpan > 2.50 {
		niceSpan = 3.00
		numOfGridLines = 6
	} else if normalizedSpan > 2.00 {
		niceSpan = 2.50
		numOfGridLines = 5
	} else if normalizedSpan > 1.75 {
		niceSpan = 2.00
		numOfGridLines = 8
	} else if normalizedSpan > 1.50 {
		niceSpan = 1.75
		numOfGridLines = 7
	} else if normalizedSpan > 1.25 {
		niceSpan = 1.50
		numOfGridLines = 6
	} else if normalizedSpan > 1.00 {
		niceSpan = 1.25
		numOfGridLines = 5
	} else {
		niceSpan = 1.00
		numOfGridLines = 10
	}

	// Scale back to original magnitude and compute step
	step := float32(niceSpan*float32(math.Pow(10, float64(exponent)))) / float32(numOfGridLines)

	round := newRoundedRange()

	// Round max up, min down to nearest step multiple
	round.maxValue = float32(float32(math.Ceil(float64(maxValue/step))) * step)
	round.minValue = float32(float32(math.Floor(float64(minValue/step))) * step)
	if !isValue(round.maxValue - round.minValue) {
		return unitRange() // Rounded past the largest float32
	}

	// Recount grid lines from actual rounded range
	round.numOfGridLines = int(math.Round(float64((round.maxValue - round.minValue) / step)))
	if round.numOfGridLines < 1 || round.maxValue <= round.minValue {
		// Flat data on a step, which the rounding leaves flat
		round.maxValue = round.minValue + step
		round.numOfGridLines = 1
	}

	return round
}

// unitRange returns the range from 0 to 1, with one grid line, of data with
// no number, or whose range does not fit a float32: a series of NaN or
// infinite values, or of values near the largest float32, whose span is
// infinite (review of 12 October 2026, R3).
func unitRange() *roundedRange {
	round := newRoundedRange()
	round.maxValue = 1
	round.numOfGridLines = 1
	return round
}

// axisLabelStep returns how many grid lines apart the labels of the value
// axis are drawn: 1 when a label fits the spacing of the lines, and more when
// it needs more room, so that no two labels meet.
func axisLabelStep(spacing, needed float32, lines int) int {
	every := 1
	for every < lines && float32(spacing*float32(every)) < needed {
		every++
	}
	return every
}

// isValue tells if the value is a number to draw: a value that is NaN or
// infinite has no bar, like a value missing at the end of a series.
func isValue(value float32) bool {
	return !math.IsNaN(float64(value)) && !math.IsInf(float64(value), 0)
}

// abs32 returns the absolute value of a float32.
func abs32(value float32) float32 {
	if value < 0 {
		return -value
	}
	return value
}

// groupDigits groups the digits of a number before its decimal point in
// thousands with commas: 23507 is 23,507.
func groupDigits(label string) string {
	start := 0
	if strings.HasPrefix(label, "-") {
		start = 1
	}
	end := strings.IndexByte(label, '.')
	if end < 0 {
		end = len(label)
	}
	for i := end - 3; i > start; i -= 3 {
		label = label[:i] + "," + label[i:]
	}
	return label
}

// The axis of a range of values: its first and last value, and its steps.
type chartAxis struct {
	min, max float32
	lines    int
}

// axisOf returns the axis of the values: the range set, or the
// range of the values rounded to steps.
func axisOf(values []float32, lo, hi float32, set bool) chartAxis {
	if set {
		return chartAxis{lo, hi, max(roundMaxAndMinValues(hi, lo).numOfGridLines, 1)}
	}
	lo, hi = values[0], values[0]
	for _, v := range values[1:] {
		lo, hi = min(lo, v), max(hi, v)
	}
	if hi == lo {
		hi = lo + flatDataSpan(lo)
	}
	round := roundMaxAndMinValues(hi, lo)
	return chartAxis{round.minValue, round.maxValue, round.numOfGridLines}
}
