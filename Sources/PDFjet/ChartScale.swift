/**
 * ChartScale.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// What the charts share, Chart, BarChart, SimpleBarChart and
/// SimpleScatterChart: the numbers of their labels, the rounding of an axis to
/// steps, the spacing of the labels of an axis, the colors of the series that
/// have none, and the test of a value to draw. A chart uses these, not another
/// chart, so that one can go without the others.
///
enum ChartScale {
    static let DEFAULT_PALETTE = [
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    ]

    // Formats a label with minDigits to maxDigits decimal places, rounding
    // the exact value half to even. The label has a "." decimal separator and
    // no grouping whatever the locale, and a value that rounds to zero has no
    // minus sign.
    static func format(_ value: Float, _ minDigits: Int, _ maxDigits: Int) -> String {
        if value.isNaN {
            return "NaN"
        }
        if value.isInfinite {
            return (value < 0) ? "-Infinity" : "Infinity"
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

    // Returns the number of decimal places, at most maxDigits, that write the
    // axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
    static func fractionDigitsOf(_ step: Float, _ maxDigits: Int) -> Int {
        var digits = 0
        while digits < maxDigits {
            let scaled = Double(step) * pow(10.0, Double(digits))
            if abs(scaled - scaled.rounded()) < 1e-4 {
                return digits
            }
            digits += 1
        }
        return max(maxDigits, 0)
    }

    ///
    /// Returns the span of the range of flat data at the value: 1, or a hundred
    /// thousandth of the value when that is more, since above 2^24 a Float has
    /// no room for 1 more, and the range would stay flat.
    ///
    static func flatDataSpan(_ value: Float) -> Float {
        return Swift.max(1.0, abs(value)*1e-5)
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
            return unitRange()      // Rounded past the largest Float
        }

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
            lo = Swift.min(lo, v)
            hi = Swift.max(hi, v)
        }
        if hi == lo {
            hi = lo + flatDataSpan(lo)
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
        let scalars = Array(text.unicodeScalars)
        var end = scalars.count
        while end > 0 {
            end -= 1
            var cut = end
            while cut > 0 && scalars[cut - 1] == " " {
                cut -= 1
            }
            var shorter = String.UnicodeScalarView()
            shorter.append(contentsOf: scalars[0..<cut])
            let candidate = String(shorter) + "\u{2026}"
            if font.stringWidth(size, candidate) <= width {
                return candidate
            }
        }
        return "\u{2026}"
    }

    ///
    /// Returns the value held to the axis and to one span of it past each end,
    /// so that a value far outside a range that is set is drawn just past the
    /// plot, where it is clipped, and not at a coordinate too large for a PDF.
    /// A value on the axis is returned as it is.
    ///
    static func held(_ value: Float, _ axis: Axis) -> Float {
        let span = Double(axis.max) - Double(axis.min)
        return Float(Swift.max(Double(axis.min) - span, Swift.min(Double(axis.max) + span, Double(value))))
    }

    /// Tells if the value is on the axis, its ends included.
    static func onAxis(_ value: Float, _ axis: Axis) -> Bool {
        return value >= axis.min && value <= axis.max
    }

    ///
    /// Tells if the axis can be drawn: the span of a range that is set, or of
    /// values that are, can be past the largest float.
    ///
    static func drawable(_ axis: Axis) -> Bool {
        return isValue(axis.max - axis.min)
    }

    ///
    /// Groups the digits of a number before its decimal point in thousands with
    /// commas: 23507 is 23,507.
    ///
    static func groupDigits(_ label: String) -> String {
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
