/**
 * ChartScale.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// What the charts share, BarChart, DonutChart, SimpleBarChart, SimpleLineChart
/// and SimpleScatterChart: the numbers of their labels, the rounding of an axis to
/// steps, the spacing of the labels of an axis, the place of a value on a plot,
/// the shortening of a long name, and the test of a value to draw.
///
enum ChartScale {
    // The most digits after the decimal point that a chart writes:
    // setFractionDigits(1 << 31) crashed, in the writing of its numbers (the
    // review of 30 October 2026). The message is the same in the four ports.
    static let maxDigits = 100
    static let tooManyDigits = "The fraction digits of a chart are at most 100."

    // Formats a label with minDigits to maxDigits decimal places, rounding
    // the exact value half to even. The label has a "." decimal separator and
    // no grouping whatever the locale, and a value that rounds to zero has no
    // minus sign.
    static func format(_ value: Float, _ minDigits: Int, _ maxDigits: Int) -> String {
        return format(value, minDigits, maxDigits, 3)
    }

    // Formats a label as format does, a number of 16 digits or more with the
    // significant digits and a power of ten.
    static func format(_ value: Float, _ minDigits: Int, _ maxDigits: Int, _ significant: Int) -> String {
        if value.isNaN {
            return "NaN"
        }
        if value.isInfinite {
            return (value < 0) ? "-Infinity" : "Infinity"
        }
        // A number of 16 digits or more as three significant digits and a
        // power of ten, 3e38 and 1.23e20, where it was written out in full
        if abs(value) >= 1e15 {
            return shortForm(value, max(significant, 1))
        }
        // A minimum above the maximum is lowered to it, as in Java's NumberFormat
        let maxDigits = max(maxDigits, 0)
        let minDigits = min(max(minDigits, 0), maxDigits)
        // String(format:) without a locale writes the exact digits with a "."
        var label = String(format: "%.\(maxDigits)f", Double(value))
        if let point = label.firstIndex(of: ".") {
            while label.distance(from: point, to: label.endIndex) - 1 > minDigits && label.hasSuffix("0") {
                label.removeLast()
            }
            if label.hasSuffix(".") {
                label.removeLast()
            }
        }
        if label.hasPrefix("-") && label.allSatisfy({ $0 == "-" || $0 == "0" || $0 == "." }) {
            label.removeFirst()
        }
        return label
    }

    // The value, a whole number of 16 digits or more, as its exact digits
    // rounded half to even to its significant digits, three in a label, its
    // trailing zeros left out, and a power of ten: 3e38, 1.23e20, -4.5e16. As
    // Java's BigDecimal(value).round(MathContext(3, HALF_EVEN)), the
    // precision less the scale less 1 the exponent.
    private static func shortForm(_ value: Float, _ significant: Int) -> String {
        // A Float of 2^24 or more is a whole number, written exactly
        let digits = Array(String(format: "%.0f", Double(abs(value))).utf8).map { Int($0) - 48 }
        var unit = 1    // 10 to the significant digits less 1
        for _ in 1..<significant {
            unit *= 10
        }
        var head = digits[0..<significant].reduce(0) { $0 * 10 + $1 }
        var exponent = digits.count - 1
        let rest = digits[significant...]
        let first = rest.first ?? 0
        let more = rest.dropFirst().contains { $0 != 0 }
        if first > 5 || (first == 5 && (more || head % 2 == 1)) {
            head += 1
            if head == unit * 10 {
                head = unit
                exponent += 1
            }
        }
        var mantissa = String(head / unit)
        var fraction = String(String(head % unit + unit).dropFirst())
        while fraction.hasSuffix("0") {
            fraction.removeLast()
        }
        if !fraction.isEmpty {
            mantissa += "." + fraction
        }
        return ((value < 0) ? "-" : "") + mantissa + "e" + String(exponent)
    }

    // Returns the number of decimal places, at most maxDigits, that write the
    // axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
    static func fractionDigitsOf(_ step: Float, _ maxDigits: Int) -> Int {
        var digits = 0
        while digits < maxDigits {
            let scaled = Double(step) * pow(10.0, Double(digits))
            // A whole number that is not 0: a step under 0.0001 was within
            // the tolerance of 0, and had no digits (the review of 26 October
            // 2026, the fourth)
            if scaled.rounded() != 0 && abs(scaled - scaled.rounded()) < 1e-4 {
                return digits
            }
            digits += 1
        }
        return max(maxDigits, 0)
    }

    // Returns the significant digits of a number of an axis written with a power of ten
    // that show it apart from the next, a step away: three, or more, up to nine,
    // one more for each power of ten between the number and the step. The simple
    // charts wrote an axis of 4.84e15 to 4.85e15 as 4.84e15 at each line (the
    // review of 28 October 2026).
    static func stepSignificant(_ value: Float, _ step: Float) -> Int {
        return Swift.min(9, Swift.max(3, powerOfTen(Double(value)) - powerOfTen(Double(step)) + 1))
    }

    // The power of ten of the number's first digit, 0 for 0 or a number that
    // is not one, counted by division, so that the ports agree.
    private static func powerOfTen(_ number: Double) -> Int {
        var value = Swift.abs(number)
        if value == 0 || value.isNaN || value.isInfinite {
            return 0
        }
        var power = 0
        while value >= 10 {
            value /= 10
            power += 1
        }
        while value < 1 {
            value *= 10
            power -= 1
        }
        return power
    }

    // Returns the significant digits of the numbers of an axis written with a
    // power of ten, from lo up by the lines of the step, with the digits:
    // three, or more, up to nine, as many as write them apart. An axis of
    // 4.84e15 to 4.85e15 was written 4.84e15 at each line (the review of 28
    // October 2026).
    static func axisSignificant(_ lo: Float, _ step: Float, _ lines: Int, _ digits: Int) -> Int {
        for significant in 3..<9 {
            var apart = true
            var last = ""
            var i = 0
            while i <= lines && apart {
                let label = format(lo + step * Float(i), digits, digits, significant)
                apart = i == 0 || label != last
                last = label
                i += 1
            }
            if apart {
                return significant
            }
        }
        return 9
    }

    ///
    /// Returns the span of the range of flat data at the value: 1, or a hundred
    /// thousandth of the value when that is more, since above 2^24 a Float has
    /// no room for 1 more, and the range would stay flat.
    ///
    static func flatDataSpan(_ value: Float) -> Float {
        return larger(1.0, abs(value)*1e-5)
    }

    ///
    /// Returns the range of flat data at the value, (lo, hi): from the value up
    /// by its flat data span, or up to the value when that is past the largest
    /// Float. Data of the largest Float was drawn on an axis of 0 to 1.
    ///
    static func flatRange(_ value: Float) -> (lo: Float, hi: Float) {
        let span = flatDataSpan(value)
        if isValue(value + span) {
            return (value, value + span)
        }
        return (value - span, value)
    }

    ///
    /// Returns the range from 0 to 1, with one grid line, of data with no
    /// number, or whose range does not fit a Float: a series of NaN or infinite
    /// values, or of values near the largest Float, whose span is infinite
    /// (review of 12 October 2026, R3).
    ///
    private static func unitRange() -> Round {
        let round = Round()
        round.maxValue = 1.0
        round.numOfGridLines = 1
        return round
    }

    ///
    /// Rounds the axis range to "nice" values for clean grid lines.
    /// Uses the span (max - min) to support negative values and
    /// zero crossings. Rounds max up and min down to step multiples.
    ///
    static func roundMaxAndMinValues(_ maxValue: Float, _ minValue: Float) -> Round {
        var span = maxValue - minValue
        if !ChartScale.isValue(span) {
            return unitRange()      // No number, or a span past the largest Float
        }
        if span <= 0.0 { span = flatDataSpan(minValue) }   // guard against flat data

        let exponent = Int(floor(log(Double(span)) / log(10.0)))
        let normalizedSpan = span * Float(pow(10.0, Double(-exponent)))

        // Snap span up to a "nice" value with paired grid line count
        var niceSpan: Float
        var numOfGridLines: Int

        if      normalizedSpan > 9.00 { niceSpan = 10.0; numOfGridLines = 10 }
        else if normalizedSpan > 8.00 { niceSpan =  9.00; numOfGridLines =  9 }
        else if normalizedSpan > 7.00 { niceSpan =  8.00; numOfGridLines =  8 }
        else if normalizedSpan > 6.00 { niceSpan =  7.00; numOfGridLines =  7 }
        else if normalizedSpan > 5.00 { niceSpan =  6.00; numOfGridLines =  6 }
        else if normalizedSpan > 4.00 { niceSpan =  5.00; numOfGridLines =  5 }
        else if normalizedSpan > 3.50 { niceSpan =  4.00; numOfGridLines =  8 }
        else if normalizedSpan > 3.00 { niceSpan =  3.50; numOfGridLines =  7 }
        else if normalizedSpan > 2.50 { niceSpan =  3.00; numOfGridLines =  6 }
        else if normalizedSpan > 2.00 { niceSpan =  2.50; numOfGridLines =  5 }
        else if normalizedSpan > 1.75 { niceSpan =  2.00; numOfGridLines =  8 }
        else if normalizedSpan > 1.50 { niceSpan =  1.75; numOfGridLines =  7 }
        else if normalizedSpan > 1.25 { niceSpan =  1.50; numOfGridLines =  6 }
        else if normalizedSpan > 1.00 { niceSpan =  1.25; numOfGridLines =  5 }
        else                          { niceSpan =  1.00; numOfGridLines = 10 }

        // Scale back to original magnitude and compute step
        let step = niceSpan * Float(pow(10.0, Double(exponent))) / Float(numOfGridLines)

        let round = Round()

        // Round max up, min down to nearest step multiple
        // Rounded by the Float's own rounded, not ceil and floor, which
        // Android's C library has for Double alone (the Swift Package
        // Index's Android build, 18 October 2026)
        round.maxValue = (maxValue / step).rounded(.up) * step
        round.minValue = (minValue / step).rounded(.down) * step
        if !ChartScale.isValue(round.maxValue - round.minValue) {
            // Rounded past the largest Float: the range of the values as
            // they are, in lines of a round step where there is one; it fell
            // back to 0 to 1, and the bars of 3e38 went past the plot
            round.maxValue = maxValue
            round.minValue = minValue
            round.numOfGridLines = roundLines(minValue, maxValue, numOfGridLines)
            return round
        }
        // The product of a step can round to inside the values: 591 steps of
        // 0.1 are 59.100002, and a point at 59.1 was not on its axis (the
        // review of 30 October 2026)
        round.maxValue = Swift.max(round.maxValue, maxValue)
        round.minValue = Swift.min(round.minValue, minValue)

        // Recount grid lines from actual rounded range
        round.numOfGridLines = Int(((round.maxValue - round.minValue) / step).rounded())
        if round.numOfGridLines < 1 || round.maxValue <= round.minValue {
            // Flat data on a step, which the rounding leaves flat
            round.maxValue = round.minValue + step
            round.numOfGridLines = 1
        }

        return round
    }

    /// Returns how many grid lines apart the labels of the value axis are
    /// drawn: 1 when a label fits the spacing of the lines, and more when it
    /// needs more room, so that no two labels meet.
    static func axisLabelStep(_ spacing: Float, _ needed: Float, _ lines: Int) -> Int {
        var every = 1
        while every < lines && spacing * Float(every) < needed {
            every += 1
        }
        return every
    }

    ///
    /// Returns where a label under an axis starts: centered on its place, but
    /// inside lo to hi. A first category or a last number was written past the
    /// chart's edge.
    ///
    static func placed(_ center: Float, _ width: Float, _ lo: Float, _ hi: Float) -> Float {
        return larger(lo, smaller(center - width / 2, hi - width))
    }

    ///
    /// Returns how many places apart the labels under an axis are drawn: every
    /// places, or more when a label held inside lo to hi would come nearer to
    /// the one before it than the gap. The labels are centered on their
    /// places, of the widths.
    ///
    static func placedStep(_ every: Int, _ centers: [Float], _ widths: [Float],
            _ lo: Float, _ hi: Float, _ gap: Float) -> Int {
        var every = Swift.max(every, 1)
        while every < centers.count && !fits(every, centers, widths, lo, hi, gap) {
            every += 1
        }
        return every
    }

    // Tells if the labels at every places, held inside lo to hi, are apart by
    // the gap.
    private static func fits(_ every: Int, _ centers: [Float], _ widths: [Float],
            _ lo: Float, _ hi: Float, _ gap: Float) -> Bool {
        var end = -Float.infinity
        for i in stride(from: 0, to: centers.count, by: every) {
            let at = placed(centers[i], widths[i], lo, hi)
            if at < end {
                return false
            }
            end = at + widths[i] + gap
        }
        return true
    }

    // Tells if the value is a number to draw: a value that is NaN or infinite
    // has no bar, like a value missing at the end of a series.
    static func isValue(_ value: Float) -> Bool {
        return value.isFinite
    }
    ///
    /// The axis of a range of values: its first and last value, and its steps.
    ///
    struct Axis {
        let min: Float
        let max: Float
        let lines: Int
    }

    ///
    /// Returns the axis of the values: the range set, or the range of the
    /// values rounded to steps.
    ///
    static func axisOf(_ values: [Float], _ lo: Float, _ hi: Float, _ set: Bool) -> Axis {
        if set {
            return Axis(min: lo, max: hi, lines: roundLines(lo, hi, Swift.max(roundMaxAndMinValues(hi, lo).numOfGridLines, 1)))
        }
        var lo = values[0]
        var hi = values[0]
        for v in values.dropFirst() {
            lo = smaller(lo, v)
            hi = larger(hi, v)
        }
        if hi == lo {
            (lo, hi) = flatRange(lo)
        }
        let round = roundMaxAndMinValues(hi, lo)
        return Axis(min: round.minValue, max: round.maxValue, lines: round.numOfGridLines)
    }

    ///
    /// Returns the number of grid lines of a range that is set, from 2 to 10,
    /// the nearest to the lines given, fewer first, whose step is a round
    /// number, 1, 2, 2.5 or 5 times a power of ten; or the lines given when
    /// no number is. The range 3 to 17 in the 6 lines of its rounded span was
    /// written 3.00, 5.33, 7.67; in 7 it is 3, 5, 7.
    ///
    static func roundLines(_ lo: Float, _ hi: Float, _ lines: Int) -> Int {
        let span = Double(hi) - Double(lo)
        for d in 0...8 {
            if isRound(span, lines - d) {
                return lines - d
            }
            if isRound(span, lines + d) {
                return lines + d
            }
        }
        return lines
    }

    // Tells if the span in the lines, from 2 to 10, has a round step.
    private static func isRound(_ span: Double, _ lines: Int) -> Bool {
        if lines < 2 || lines > 10 {
            return false
        }
        let step = span / Double(lines)
        if !(step > 0.0) || step.isInfinite {
            return false
        }
        let m = step / pow(10.0, floor(log10(step)))
        return abs(m - 1.0) < 1e-5 || abs(m - 2.0) < 2e-5 || abs(m - 2.5) < 2.5e-5
                || abs(m - 5.0) < 5e-5 || abs(m - 10.0) < 1e-4
    }

    ///
    /// Returns the text, shortened with an ellipsis to fit the width in the
    /// font at the size, or the text when it fits: a name too long for a
    /// chart's column of names. The text is cut by code points, as in Java.
    ///
    static func shorten(_ font: Font, _ size: Float, _ text: String, _ width: Float) -> String {
        if font.stringWidth(size, text) <= width {
            return text
        }
        // The longest start of the text that fits with the ellipsis, found by
        // halving the number of code points it keeps
        let scalars = Array(text.unicodeScalars)
        var best = "\u{2026}"
        var lo = 0
        var hi = scalars.count - 1
        while lo <= hi {
            let mid = (lo + hi) / 2
            let shorter = shortened(scalars, mid)
            if font.stringWidth(size, shorter) <= width {
                best = shorter
                lo = mid + 1
            } else {
                hi = mid - 1
            }
        }
        return best
    }

    // The first code points of the text, its trailing spaces left out, and an
    // ellipsis.
    private static func shortened(_ scalars: [Unicode.Scalar], _ codePoints: Int) -> String {
        var cut = codePoints
        while cut > 0 && scalars[cut - 1] == " " {
            cut -= 1
        }
        var shorter = String.UnicodeScalarView()
        shorter.append(contentsOf: scalars[0..<cut])
        return String(shorter) + "\u{2026}"
    }

    ///
    /// Returns the distance along a length of a value whose distance from the
    /// start of the axis is d, on an axis of the span: d * length / span, in
    /// Floats, as it always was, or in Doubles when the product is past the
    /// largest Float. Bars of 3e38 on an axis of 0 to 3e38 had infinite
    /// coordinates (the review of 26 October 2026, the second).
    ///
    static func scaled(_ d: Float, _ length: Float, _ span: Float) -> Float {
        let product = d * length
        if isValue(product) {
            return product / span
        }
        return Float(Double(d) * Double(length) / Double(span))
    }

    ///
    /// Returns the number of digits after the decimal point that the numbers of
    /// an axis of the step need, 2 at least: data under 0.01 was written 0.00
    /// at every grid line.
    ///
    static func digitsOf(_ step: Float) -> Int {
        return Swift.max(2, stepDigits(step))
    }

    ///
    /// Returns the number of digits after the decimal point that write the step
    /// of an axis: as many as it has, or six significant digits of a step that
    /// is not round. They were six at most, and data under 0.000001 was
    /// written 0.000000.
    ///
    static func stepDigits(_ step: Float) -> Int {
        if !isValue(step) || step <= 0 {
            return 0
        }
        let significant = 5 - Int(floor(log10(Double(step))))
        return fractionDigitsOf(step, Swift.max(6, significant))
    }

    ///
    /// Tells if the span of the values, the largest less the smallest, is a
    /// number: values of -3e38 and 3e38 have a span past the largest float,
    /// which no axis can draw.
    ///
    static func spanFits(_ values: [Float]) -> Bool {
        if values.isEmpty {
            return true
        }
        var lo = values[0]
        var hi = values[0]
        for value in values {
            lo = smaller(lo, value)
            hi = larger(hi, value)
        }
        return isValue(hi - lo)
    }

    ///
    /// Returns the value held to the axis and to one span of it past each end,
    /// so that a value far outside a range that is set is drawn just past the
    /// plot, where it is clipped, and not at a coordinate too large for a PDF.
    /// A value on the axis is returned as it is.
    ///
    static func held(_ value: Float, _ axis: Axis) -> Float {
        let span = Double(axis.max) - Double(axis.min)
        return Float(larger(Double(axis.min) - span, smaller(Double(axis.max) + span, Double(value))))
    }

    ///
    /// Returns the smaller of two numbers, or NaN when one is NaN, as Go's min
    /// does: Swift.min drops a NaN that is not its first argument, and an axis
    /// of a NaN could differ from the other ports' (the review of 28 October
    /// 2026).
    ///
    static func smaller<T: BinaryFloatingPoint>(_ a: T, _ b: T) -> T {
        return (a.isNaN || b.isNaN) ? T.nan : Swift.min(a, b)
    }

    ///
    /// Returns the larger of two numbers, or NaN when one is NaN, as Go's max
    /// does.
    ///
    static func larger<T: BinaryFloatingPoint>(_ a: T, _ b: T) -> T {
        return (a.isNaN || b.isNaN) ? T.nan : Swift.max(a, b)
    }

    /// Tells if the value is on the axis, its ends included.
    static func onAxis(_ value: Float, _ axis: Axis) -> Bool {
        return value >= axis.min && value <= axis.max
    }

    ///
    /// The words of a chart for its values outside a range that is set, of
    /// which it would say nothing otherwise, or "" when there are none: "1 point
    /// outside the range", "2 points outside the range".
    ///
    /// - Parameter count: the number of values outside.
    /// - Parameter noun: what the chart counts, in the singular.
    /// - Returns: the note.
    ///
    static func outsideNote(_ count: Int, _ noun: String) -> String {
        if count == 0 {
            return ""
        }
        return "\(count) \(noun)\(count > 1 ? "s" : "") outside the range"
    }

    ///
    /// The description set by the caller with the note, if any, as a sentence
    /// after it: the note is in the figure, and in what a screen reader gets of
    /// it, whoever wrote the description.
    ///
    /// - Parameter description: the description set.
    /// - Parameter note: the note, or "".
    /// - Returns: the description.
    ///
    static func withNote(_ description: String, _ note: String) -> String {
        if note.isEmpty {
            return description
        }
        return description + SimpleBarChart.punctuationAfter(description, ".") + " " + note + "."
    }

    ///
    /// Draws the note, small and in the gray of the numbers, on the baseline,
    /// its right end at x and inside the width from lo to hi, past which it is
    /// shortened.
    ///
    static func drawOutsideNote(_ page: Page, _ font: Font, _ size: Float, _ note: String,
            _ x: Float, _ lo: Float, _ hi: Float, _ baseline: Float) {
        let text = shorten(font, size, note, hi - lo)
        page.drawString(font, size, text, Swift.max(lo, x - font.stringWidth(size, text)), baseline,
                Util.toRGB(SimpleBarChart.MUTED), nil)
    }

    ///
    /// Tells if the axis can be drawn: the span of a range that is set, or of
    /// values that are, can be past the largest float.
    ///
    static func drawable(_ axis: Axis) -> Bool {
        return isValue(axis.max - axis.min)
    }

    ///
    /// Joins two parts of a chart's description, the title and the subtitle, as
    /// two sentences: "Vehicles. By model year", the second's first letter a
    /// capital when it is a to z. A part that is empty is left out, and the
    /// first keeps its own full stop, question or exclamation mark. They were
    /// joined with a comma, and a subtitle kept its capital after it.
    ///
    static func join(_ first: String, _ second: String) -> String {
        if second.isEmpty {
            return first
        }
        if first.isEmpty {
            return second
        }
        // The second a sentence, its first letter a capital when it is a to z
        var second = second
        let start = second.unicodeScalars.first!
        if start >= "a" && start <= "z" {
            second = String(Character(Unicode.Scalar(start.value - 32)!)) + String(second.unicodeScalars.dropFirst())
        }
        let end = first.unicodeScalars.last!
        return first + ((end == "." || end == "?" || end == "!") ? " " : ". ") + second
    }

    ///
    /// Groups the digits of a number before its decimal point in thousands with
    /// commas: 23507 is 23,507.
    ///
    static func groupDigits(_ label: String) -> String {
        for c in label.unicodeScalars where (c < "0" || c > "9") && c != "-" && c != "." {
            return label    // A power of ten has no thousands, and Infinity no digits
        }
        var chars = Array(label)
        let start = chars.first == "-" ? 1 : 0
        let end = chars.firstIndex(of: ".") ?? chars.count
        var i = end - 3
        while i > start {
            chars.insert(",", at: i)
            i -= 3
        }
        return String(chars)
    }
}   // End of ChartScale.swift
