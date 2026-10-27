/*
 * ChartScale.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.Numerics;

namespace PDFjet.NET {
/// <summary>
/// What the charts share, BarChart, DonutChart, SimpleBarChart, SimpleLineChart
/// and SimpleScatterChart: the numbers of their labels, the rounding of an axis to
/// steps, the spacing of the labels of an axis, the place of a value on a plot,
/// the shortening of a long name, and the test of a value to draw.
/// </summary>
internal static class ChartScale {
    /// <summary>
    /// Formats a label with minDigits to maxDigits decimal places, rounding the
    /// exact value half to even. The label has a "." decimal separator and no
    /// grouping whatever the default locale, and a value that rounds to zero
    /// has no minus sign.
    /// </summary>
    internal static String Format(float value, int minDigits, int maxDigits) {
        // A number of 16 digits or more as three significant digits and a
        // power of ten, 3e38 and 1.23e20, where it was written out in full
        if (IsValue(value) && Math.Abs(value) >= 1e15f) {
            return Short(value);
        }
        // A minimum above the maximum is lowered to it, as in NumberFormat
        maxDigits = Math.Max(maxDigits, 0);
        minDigits = Math.Min(Math.Max(minDigits, 0), maxDigits);
        NumberFormat nf = NumberFormat.GetInstance();
        nf.SetMaximumFractionDigits(maxDigits);
        nf.SetMinimumFractionDigits(minDigits);
        return nf.Format(value);
    }

    // The value as three significant digits, rounded half to even from its
    // exact binary value, and a power of ten, as Java's
    // new BigDecimal(value).round(new MathContext(3, RoundingMode.HALF_EVEN))
    // gives: 3e38, 1.23e20, -4.5e16. A float of 1e15 or more is an integer.
    private static String Short(float value) {
        int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        bool negative = bits < 0;
        int exponent = (bits >> 23) & 0xFF;
        int mantissa = (bits & 0x7FFFFF) | (1 << 23);  // Normal, as 1e15 is
        BigInteger exact = new BigInteger(mantissa) << (exponent - 150);
        String digits = exact.ToString(CultureInfo.InvariantCulture);
        int power = digits.Length - 1;
        BigInteger kept = BigInteger.Parse(digits.Substring(0, 3), CultureInfo.InvariantCulture);
        BigInteger unit = BigInteger.Pow(10, digits.Length - 3);
        BigInteger rest = exact - kept * unit;
        int half = (rest * 2).CompareTo(unit);
        if (half > 0 || (half == 0 && !kept.IsEven)) {
            kept += 1;
        }
        if (kept == 1000) {
            kept = 100;
            power++;
        }
        String mantissaText = kept.ToString(CultureInfo.InvariantCulture);
        mantissaText = (mantissaText.Substring(0, 1) + "." + mantissaText.Substring(1)).TrimEnd('0').TrimEnd('.');
        return (negative ? "-" : "") + mantissaText + "e" + power.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the number of decimal places, at most maxDigits, that write the
    /// axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
    /// </summary>
    internal static int FractionDigitsOf(float step, int maxDigits) {
        for (int digits = 0; digits < maxDigits; digits++) {
            double scaled = step * Math.Pow(10, digits);
            if (Math.Abs(scaled - Math.Round(scaled)) < 1e-4) {
                return digits;
            }
        }
        return Math.Max(maxDigits, 0);
    }

    /// <summary>
    /// Rounds axis range to "nice" values for clean grid lines.
    /// Uses the span (max - min) to support negative values and
    /// zero crossings. Rounds max up and min down to step multiples.
    /// </summary>
    internal static Round RoundMaxAndMinValues(float maxValue, float minValue) {
        float span = maxValue - minValue;
        if (!IsValue(span)) {
            return UnitRange();     // No number, or a span past the largest float
        }
        if (span <= 0f) { span = FlatDataSpan(minValue); }    // guard against flat data

        int exponent = (int) Math.Floor(Math.Log(span) / Math.Log(10));
        float normalizedSpan = span * (float) Math.Pow(10, -exponent);

        // Snap span up to a "nice" value with paired grid line count
        float niceSpan;
        int numOfGridLines;

        if      (normalizedSpan > 9.00f) { niceSpan = 10.0f; numOfGridLines = 10; }
        else if (normalizedSpan > 8.00f) { niceSpan =  9.00f; numOfGridLines =  9; }
        else if (normalizedSpan > 7.00f) { niceSpan =  8.00f; numOfGridLines =  8; }
        else if (normalizedSpan > 6.00f) { niceSpan =  7.00f; numOfGridLines =  7; }
        else if (normalizedSpan > 5.00f) { niceSpan =  6.00f; numOfGridLines =  6; }
        else if (normalizedSpan > 4.00f) { niceSpan =  5.00f; numOfGridLines =  5; }
        else if (normalizedSpan > 3.50f) { niceSpan =  4.00f; numOfGridLines =  8; }
        else if (normalizedSpan > 3.00f) { niceSpan =  3.50f; numOfGridLines =  7; }
        else if (normalizedSpan > 2.50f) { niceSpan =  3.00f; numOfGridLines =  6; }
        else if (normalizedSpan > 2.00f) { niceSpan =  2.50f; numOfGridLines =  5; }
        else if (normalizedSpan > 1.75f) { niceSpan =  2.00f; numOfGridLines =  8; }
        else if (normalizedSpan > 1.50f) { niceSpan =  1.75f; numOfGridLines =  7; }
        else if (normalizedSpan > 1.25f) { niceSpan =  1.50f; numOfGridLines =  6; }
        else if (normalizedSpan > 1.00f) { niceSpan =  1.25f; numOfGridLines =  5; }
        else                             { niceSpan =  1.00f; numOfGridLines = 10; }

        float step = niceSpan * (float) Math.Pow(10, exponent) / numOfGridLines;

        Round round = new Round();

        // Round max up, min down to nearest step multiple
        round.maxValue = (float) Math.Ceiling(maxValue / step) * step;
        round.minValue = (float) Math.Floor(minValue / step) * step;
        if (!IsValue(round.maxValue - round.minValue)) {
            // Rounded past the largest float: the range of the values as
            // they are, in lines of a round step where there is one; it fell
            // back to 0 to 1, and the bars of 3e38 went past the plot
            round.maxValue = maxValue;
            round.minValue = minValue;
            round.numOfGridLines = RoundLines(minValue, maxValue, numOfGridLines);
            return round;
        }

        round.numOfGridLines = (int) Math.Round((round.maxValue - round.minValue) / step);
        if (round.numOfGridLines < 1 || round.maxValue <= round.minValue) {
            // Flat data on a step, which the rounding leaves flat
            round.maxValue = round.minValue + step;
            round.numOfGridLines = 1;
        }

        return round;
    }

    // Returns the range from 0 to 1, with one grid line, of data with no number,
    // or whose range does not fit a float: a series of NaN or infinite values,
    // or of values near the largest float, whose span is infinite (review of
    // 12 October 2026, R3).
    private static Round UnitRange() {
        Round round = new Round();
        round.maxValue = 1f;
        round.numOfGridLines = 1;
        return round;
    }

    // Returns the span of the range of flat data at the value: 1, or a hundred
    // thousandth of the value when that is more, since above 2^24 a float has no
    // room for 1 more, and the range would stay flat.
    internal static float FlatDataSpan(float value) {
        return Math.Max(1f, Math.Abs(value) * 1e-5f);
    }

    /// <summary>
    /// Returns how many grid lines apart the labels of the value axis are
    /// drawn: 1 when a label fits the spacing of the lines, and more when it
    /// needs more room, so that no two labels meet.
    /// </summary>
    internal static int AxisLabelStep(float spacing, float needed, int lines) {
        int every = 1;
        while (every < lines && spacing * every < needed) {
            every++;
        }
        return every;
    }

    // Tells if the value is a number to draw: a value that is NaN or infinite
    // has no bar, like a value missing at the end of a series.
    internal static bool IsValue(float value) {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>
    /// The axis of a range of values: its first and last value, and its steps.
    /// </summary>
    internal sealed class Axis {
        internal readonly float min;
        internal readonly float max;
        internal readonly int lines;

        internal Axis(float min, float max, int lines) {
            this.min = min;
            this.max = max;
            this.lines = lines;
        }
    }

    /// <summary>
    /// Returns the axis of the values: the range set, or the range of the
    /// values rounded to steps.
    /// </summary>
    internal static Axis AxisOf(float[] values, float lo, float hi, bool set) {
        if (set) {
            return new Axis(lo, hi, RoundLines(lo, hi, Math.Max(RoundMaxAndMinValues(hi, lo).numOfGridLines, 1)));
        }
        lo = values[0];
        hi = values[0];
        for (int i = 1; i < values.Length; i++) {
            lo = Math.Min(lo, values[i]);
            hi = Math.Max(hi, values[i]);
        }
        if (hi == lo) {
            hi = lo + FlatDataSpan(lo);
        }
        Round round = RoundMaxAndMinValues(hi, lo);
        return new Axis(round.minValue, round.maxValue, round.numOfGridLines);
    }

    /// <summary>
    /// Returns the number of grid lines of a range that is set, from 2 to 10,
    /// the nearest to the lines given, fewer first, whose step is a round
    /// number, 1, 2, 2.5 or 5 times a power of ten; or the lines given when
    /// no number is. The range 3 to 17 in the 6 lines of its rounded span was
    /// written 3.00, 5.33, 7.67; in 7 it is 3, 5, 7.
    /// </summary>
    internal static int RoundLines(float lo, float hi, int lines) {
        double span = (double) hi - (double) lo;
        for (int d = 0; d <= 8; d++) {
            if (IsRound(span, lines - d)) {
                return lines - d;
            }
            if (IsRound(span, lines + d)) {
                return lines + d;
            }
        }
        return lines;
    }

    // Tells if the span in the lines, from 2 to 10, has a round step.
    private static bool IsRound(double span, int lines) {
        if (lines < 2 || lines > 10) {
            return false;
        }
        double step = span / lines;
        if (!(step > 0.0) || Double.IsInfinity(step)) {
            return false;
        }
        double m = step / Math.Pow(10.0, Math.Floor(Math.Log10(step)));
        return Math.Abs(m - 1.0) < 1e-5 || Math.Abs(m - 2.0) < 2e-5 || Math.Abs(m - 2.5) < 2.5e-5
                || Math.Abs(m - 5.0) < 5e-5 || Math.Abs(m - 10.0) < 1e-4;
    }

    /// <summary>
    /// Returns the text, shortened with an ellipsis to fit the width in the
    /// font at the size, or the text when it fits: a name too long for a
    /// chart's column of names.
    /// </summary>
    internal static String Shorten(Font font, float size, String text, float width) {
        if (font.StringWidth(size, text) <= width) {
            return text;
        }
        // The longest start of the text that fits with the ellipsis, found by
        // halving the number of code points it keeps
        int count = CodePointCount(text);
        String best = "\u2026";
        int lo = 0;
        int hi = count - 1;
        while (lo <= hi) {
            int mid = (int) ((uint) (lo + hi) >> 1);
            String shorter = Shortened(text, mid);
            if (font.StringWidth(size, shorter) <= width) {
                best = shorter;
                lo = mid + 1;
            } else {
                hi = mid - 1;
            }
        }
        return best;
    }

    // The number of code points of the text, a surrogate pair one.
    private static int CodePointCount(String text) {
        int count = 0;
        for (int i = 0; i < text.Length; i++) {
            if (Char.IsHighSurrogate(text[i]) && i + 1 < text.Length && Char.IsLowSurrogate(text[i + 1])) {
                i++;
            }
            count++;
        }
        return count;
    }

    // The first code points of the text, its trailing spaces left out, and an
    // ellipsis.
    private static String Shortened(String text, int codePoints) {
        int cut = 0;
        for (int k = 0; k < codePoints; k++) {
            if (Char.IsHighSurrogate(text[cut]) && cut + 1 < text.Length && Char.IsLowSurrogate(text[cut + 1])) {
                cut++;
            }
            cut++;
        }
        while (cut > 0 && text[cut - 1] == ' ') {
            cut--;
        }
        return text.Substring(0, cut) + "\u2026";
    }

    /// <summary>
    /// Returns the distance along a length of a value whose distance from the
    /// start of the axis is d, on an axis of the span: d * length / span, in
    /// floats, as it always was, or in doubles when the product is past the
    /// largest float. Bars of 3e38 on an axis of 0 to 3e38 threw, their
    /// coordinates infinite (the review of 26 October 2026, the second).
    /// </summary>
    internal static float Scaled(float d, float length, float span) {
        float product = d * length;
        if (IsValue(product)) {
            return product / span;
        }
        return (float) ((double) d * (double) length / (double) span);
    }

    /// <summary>
    /// Returns the number of digits after the decimal point that the numbers of
    /// an axis of the step need, 2 at least and 6 at most: data under 0.01 was
    /// written 0.00 at every grid line.
    /// </summary>
    internal static int DigitsOf(float step) {
        return IsValue(step) && step > 0f ? Math.Max(2, FractionDigitsOf(step, 6)) : 2;
    }

    /// <summary>
    /// Tells if the span of the values, the largest less the smallest, is a
    /// number: values of -3e38 and 3e38 have a span past the largest float,
    /// which no axis can draw.
    /// </summary>
    internal static bool SpanFits(float[] values) {
        if (values.Length == 0) {
            return true;
        }
        float lo = values[0];
        float hi = values[0];
        foreach (float value in values) {
            lo = Math.Min(lo, value);
            hi = Math.Max(hi, value);
        }
        return IsValue(hi - lo);
    }

    /// <summary>
    /// Returns the value held to the axis and to one span of it past each end,
    /// so that a value far outside a range that is set is drawn just past the
    /// plot, where it is clipped, and not at a coordinate too large for a PDF.
    /// A value on the axis is returned as it is.
    /// </summary>
    internal static float Held(float value, Axis axis) {
        double span = (double) axis.max - (double) axis.min;
        return (float) Math.Max((double) axis.min - span, Math.Min((double) axis.max + span, value));
    }

    /// <summary>Tells if the value is on the axis, its ends included.</summary>
    internal static bool OnAxis(float value, Axis axis) {
        return value >= axis.min && value <= axis.max;
    }

    /// <summary>
    /// Tells if the axis can be drawn: the span of a range that is set, or of
    /// values that are, can be past the largest float.
    /// </summary>
    internal static bool Drawable(Axis axis) {
        return IsValue(axis.max - axis.min);
    }

    /// <summary>
    /// Joins two parts of a chart's description, the title and the subtitle, as
    /// two sentences: "Vehicles. By model year", the second's first letter a
    /// capital when it is a to z. A part that is empty is left out, and the
    /// first keeps its own full stop, question or exclamation mark. They were
    /// joined with a comma, and a subtitle kept its capital after it.
    /// </summary>
    internal static String Join(String first, String second) {
        if (second.Length == 0) {
            return first;
        }
        if (first.Length == 0) {
            return second;
        }
        // The second a sentence, its first letter a capital when it is a to z
        char start = second[0];
        if (start >= 'a' && start <= 'z') {
            second = (char) (start - 'a' + 'A') + second.Substring(1);
        }
        char end = first[first.Length - 1];
        return first + ((end == '.' || end == '?' || end == '!') ? " " : ". ") + second;
    }

    /// <summary>
    /// Groups the digits of a number before its decimal point in thousands with
    /// commas: 23507 is 23,507.
    /// </summary>
    internal static String GroupDigits(String label) {
        if (label.IndexOf('e') >= 0) {
            return label;   // A power of ten has no thousands
        }
        int start = label.StartsWith("-", StringComparison.Ordinal) ? 1 : 0;
        int end = label.IndexOf('.');
        if (end < 0) {
            end = label.Length;
        }
        System.Text.StringBuilder sb = new System.Text.StringBuilder(label);
        for (int i = end - 3; i > start; i -= 3) {
            sb.Insert(i, ',');
        }
        return sb.ToString();
    }
}   // End of ChartScale.cs
}   // End of namespace PDFjet.NET
