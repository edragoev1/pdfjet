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

// What the charts share, BarChart, DonutChart, SimpleBarChart, SimpleLineChart
// and SimpleScatterChart: the numbers of their labels, the rounding of an axis to
// steps, the spacing of the labels of an axis, the place of a value on a plot,
// the shortening of a long name, and the test of a value to draw.

import (
	"math"
	"strconv"
	"strings"
	"unicode/utf8"
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

// format formats a label with minDigits to maxDigits decimal places, rounding
// the exact value half to even. The label has a "." decimal separator and no
// grouping, and a value that rounds to zero has no minus sign.
func format(value float32, minDigits, maxDigits int) string {
	return formatSignificant(value, minDigits, maxDigits, 3)
}

// formatSignificant formats a label as format does, a number of 16 digits or
// more with the significant digits and a power of ten.
func formatSignificant(value float32, minDigits, maxDigits, significant int) string {
	if math.IsNaN(float64(value)) {
		return "NaN"
	}
	if math.IsInf(float64(value), 0) {
		if value < 0 {
			return "-Infinity"
		}
		return "Infinity"
	}
	// A number of 16 digits or more as three significant digits and a
	// power of ten, 3e38 and 1.23e20, where it was written out in full; 'e'
	// rounds the exact value half to even, as Java's BigDecimal does
	if abs32(value) >= 1e15 {
		short := strconv.FormatFloat(float64(value), 'e', max(significant, 1)-1, 64)
		e := strings.IndexByte(short, 'e')
		mantissa := strings.TrimRight(strings.TrimRight(short[:e], "0"), ".")
		exponent, _ := strconv.Atoi(short[e+1:])
		return mantissa + "e" + strconv.Itoa(exponent)
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
		// A whole number that is not 0: a step under 0.0001 was within the
		// tolerance of 0, and had no digits (the review of 26 October 2026,
		// the fourth)
		if math.Round(scaled) != 0 && math.Abs(scaled-math.Round(scaled)) < 1e-4 {
			return digits
		}
	}
	return max(maxDigits, 0)
}

// axisSignificant returns the significant digits of the numbers of an axis
// written with a power of ten, from lo up by the lines of the step, with the
// digits: three, or more, up to nine, as many as write them apart. An axis of
// 4.84e15 to 4.85e15 was written 4.84e15 at each line (the review of 28
// October 2026).
func axisSignificant(lo, step float32, lines, digits int) int {
	for significant := 3; significant < 9; significant++ {
		apart := true
		last := ""
		for i := 0; i <= lines && apart; i++ {
			label := formatSignificant(lo+float32(step*float32(i)), digits, digits, significant)
			apart = i == 0 || label != last
			last = label
		}
		if apart {
			return significant
		}
	}
	return 9
}

// stepSignificant returns the significant digits of a number of an axis written with a power of ten
// that show it apart from the next, a step away: three, or more, up to nine,
// one more for each power of ten between the number and the step. The simple
// charts wrote an axis of 4.84e15 to 4.85e15 as 4.84e15 at each line (the
// review of 28 October 2026).
func stepSignificant(value, step float32) int {
	return min(9, max(3, powerOfTen(float64(value))-powerOfTen(float64(step))+1))
}

// powerOfTen returns the power of ten of the number's first digit, 0 for 0 or
// a number that is not one, counted by division, so that the ports agree.
func powerOfTen(value float64) int {
	value = math.Abs(value)
	if value == 0 || math.IsNaN(value) || math.IsInf(value, 0) {
		return 0
	}
	power := 0
	for value >= 10 {
		value /= 10
		power++
	}
	for value < 1 {
		value *= 10
		power--
	}
	return power
}

// flatDataSpan returns the span of the range of flat data at the value: 1,
// or a hundred thousandth of the value when that is more, since above 2^24 a
// float32 has no room for 1 more, and the range would stay flat.
func flatDataSpan(value float32) float32 {
	return max(1.0, float32(abs32(value)*1e-5))
}

// flatRange returns the range of flat data at the value, lo and hi: from the
// value up by its flat data span, or up to the value when that is past the
// largest float32. Data of the largest float32 was drawn on an axis of 0 to 1.
func flatRange(value float32) (float32, float32) {
	span := flatDataSpan(value)
	if isValue(value + span) {
		return value, value + span
	}
	return value - span, value
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
		// Rounded past the largest float32: the range of the values as they
		// are, in lines of a round step where there is one; it fell back to 0
		// to 1, and the bars of 3e38 went past the plot
		round.maxValue = maxValue
		round.minValue = minValue
		round.numOfGridLines = roundLines(minValue, maxValue, numOfGridLines)
		return round
	}
	// The product of a step can round to inside the values: 591 steps of 0.1
	// are 59.100002, and a point at 59.1 was not on its axis (the review of
	// 30 October 2026)
	round.maxValue = max(round.maxValue, maxValue)
	round.minValue = min(round.minValue, minValue)

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

// placed returns where a label under an axis starts: centered on its place,
// but inside lo to hi. A first category or a last number was written past the
// chart's edge.
func placed(center, width, lo, hi float32) float32 {
	return max(lo, min(center-float32(width/2), hi-width))
}

// placedStep returns how many places apart the labels under an axis are
// drawn: every places, or more when a label held inside lo to hi would come
// nearer to the one before it than the gap. The labels are centered on their
// places, of the widths.
func placedStep(every int, centers, widths []float32, lo, hi, gap float32) int {
	every = max(every, 1)
	for every < len(centers) && !fits(every, centers, widths, lo, hi, gap) {
		every++
	}
	return every
}

// fits tells if the labels at every places, held inside lo to hi, are apart
// by the gap.
func fits(every int, centers, widths []float32, lo, hi, gap float32) bool {
	end := float32(math.Inf(-1))
	for i := 0; i < len(centers); i += every {
		at := placed(centers[i], widths[i], lo, hi)
		if at < end {
			return false
		}
		end = at + widths[i] + gap
	}
	return true
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

// held returns the value held to the axis and to one span of it past each
// end, so that a value far outside a range that is set is drawn just past the
// plot, where it is clipped, and not at a coordinate too large for a PDF. A
// value on the axis is returned as it is.
func held(value float32, axis chartAxis) float32 {
	span := float64(axis.max) - float64(axis.min)
	return float32(max(float64(axis.min)-span, min(float64(axis.max)+span, float64(value))))
}

// onAxis tells if the value is on the axis, its ends included.
func onAxis(value float32, axis chartAxis) bool {
	return value >= axis.min && value <= axis.max
}

// drawable tells if the axis can be drawn: the span of a range that is set,
// or of values that are, can be past the largest float.
func drawable(axis chartAxis) bool {
	return isValue(axis.max - axis.min)
}

// groupDigits groups the digits of a number before its decimal point in
// thousands with commas: 23507 is 23,507.
func groupDigits(label string) string {
	for i := 0; i < len(label); i++ {
		if c := label[i]; (c < '0' || c > '9') && c != '-' && c != '.' {
			return label // A power of ten has no thousands, and Infinity no digits
		}
	}
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
		return chartAxis{lo, hi, roundLines(lo, hi, max(roundMaxAndMinValues(hi, lo).numOfGridLines, 1))}
	}
	lo, hi = values[0], values[0]
	for _, v := range values[1:] {
		lo, hi = min(lo, v), max(hi, v)
	}
	if hi == lo {
		lo, hi = flatRange(lo)
	}
	round := roundMaxAndMinValues(hi, lo)
	return chartAxis{round.minValue, round.maxValue, round.numOfGridLines}
}

// roundLines returns the number of grid lines of a range that is set, from 2
// to 10, the nearest to the lines given, fewer first, whose step is a round
// number, 1, 2, 2.5 or 5 times a power of ten; or the lines given when no
// number is. The range 3 to 17 in the 6 lines of its rounded span was written
// 3.00, 5.33, 7.67; in 7 it is 3, 5, 7.
func roundLines(lo, hi float32, lines int) int {
	span := float64(hi) - float64(lo)
	for d := 0; d <= 8; d++ {
		if isRound(span, lines-d) {
			return lines - d
		}
		if isRound(span, lines+d) {
			return lines + d
		}
	}
	return lines
}

// isRound tells if the span in the lines, from 2 to 10, has a round step.
func isRound(span float64, lines int) bool {
	if lines < 2 || lines > 10 {
		return false
	}
	step := span / float64(lines)
	if !(step > 0.0) || math.IsInf(step, 0) {
		return false
	}
	m := step / math.Pow(10.0, math.Floor(math.Log10(step)))
	return math.Abs(m-1.0) < 1e-5 || math.Abs(m-2.0) < 2e-5 || math.Abs(m-2.5) < 2.5e-5 ||
		math.Abs(m-5.0) < 5e-5 || math.Abs(m-10.0) < 1e-4
}

// shorten returns the text, shortened with an ellipsis to fit the width in
// the font at the size, or the text when it fits: a name too long for a
// chart's column of names. It shortens by runes.
func shorten(font *Font, size float32, text string, width float32) string {
	if font.StringWidth(size, text) <= width {
		return text
	}
	// The longest start of the text that fits with the ellipsis, found by
	// halving the number of runes it keeps
	count := utf8.RuneCountInString(text)
	best := "…"
	lo := 0
	hi := count - 1
	for lo <= hi {
		mid := int(uint(lo+hi) >> 1)
		shorter := shortened(text, mid)
		if font.StringWidth(size, shorter) <= width {
			best = shorter
			lo = mid + 1
		} else {
			hi = mid - 1
		}
	}
	return best
}

// shortened returns the first runes of the text, its trailing spaces left
// out, and an ellipsis.
func shortened(text string, runes int) string {
	cut := 0
	for i := 0; i < runes; i++ {
		_, n := utf8.DecodeRuneInString(text[cut:])
		cut += n
	}
	for cut > 0 && text[cut-1] == ' ' {
		cut--
	}
	return text[:cut] + "…"
}

// scaled returns the distance along a length of a value whose distance from
// the start of the axis is d, on an axis of the span: d * length / span, in
// float32, as it always was, or in float64 when the product is past the
// largest float32. Bars of 3e38 on an axis of 0 to 3e38 drew at infinite
// coordinates (the review of 26 October 2026, the second).
func scaled(d, length, span float32) float32 {
	product := float32(d * length)
	if isValue(product) {
		return product / span
	}
	return float32(float64(float64(d)*float64(length)) / float64(span))
}

// digitsOf returns the number of digits after the decimal point that the
// numbers of an axis of the step need, 2 at least: data under 0.01 was
// written 0.00 at every grid line.
func digitsOf(step float32) int {
	return max(2, stepDigits(step))
}

// stepDigits returns the number of digits after the decimal point that write
// the step of an axis: as many as it has, or six significant digits of a step
// that is not round. They were six at most, and data under 0.000001 was
// written 0.000000.
func stepDigits(step float32) int {
	if !isValue(step) || step <= 0.0 {
		return 0
	}
	significant := 5 - int(math.Floor(math.Log10(float64(step))))
	return fractionDigitsOf(step, max(6, significant))
}

// spanFits tells if the span of the values, the largest less the smallest,
// is a number: values of -3e38 and 3e38 have a span past the largest float,
// which no axis can draw.
func spanFits(values []float32) bool {
	if len(values) == 0 {
		return true
	}
	lo := values[0]
	hi := values[0]
	for _, value := range values {
		lo = min(lo, value)
		hi = max(hi, value)
	}
	return isValue(hi - lo)
}

// join joins two parts of a chart's description, the title and the subtitle,
// as two sentences: "Vehicles. By model year", the second's first letter a
// capital when it is a to z. A part that is empty is left out, and the first
// keeps its own full stop, question or exclamation mark. They were joined
// with a comma, and a subtitle kept its capital after it.
func join(first, second string) string {
	if second == "" {
		return first
	}
	if first == "" {
		return second
	}
	// The second a sentence, its first letter a capital when it is a to z
	if start := second[0]; start >= 'a' && start <= 'z' {
		second = string(start-'a'+'A') + second[1:]
	}
	end := first[len(first)-1]
	if end == '.' || end == '?' || end == '!' {
		return first + " " + second
	}
	return first + ". " + second
}
