/*
 * ChartScale.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// What the charts share, Chart, BarChart, SimpleBarChart and
/// SimpleScatterChart: the numbers of their labels, the rounding of an axis to
/// steps, the spacing of the labels of an axis, the colors of the series that
/// have none, and the test of a value to draw. A chart uses these, not another
/// chart, so that one can go without the others.
/// </summary>
internal static class ChartScale {
    internal static readonly int[] DEFAULT_PALETTE = {
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    };

    /// <summary>
    /// Formats a label with minDigits to maxDigits decimal places, rounding the
    /// exact value half to even. The label has a "." decimal separator and no
    /// grouping whatever the default locale, and a value that rounds to zero
    /// has no minus sign.
    /// </summary>
    internal static String Format(float value, int minDigits, int maxDigits) {
        // A minimum above the maximum is lowered to it, as in NumberFormat
        maxDigits = Math.Max(maxDigits, 0);
        minDigits = Math.Min(Math.Max(minDigits, 0), maxDigits);
        NumberFormat nf = NumberFormat.GetInstance();
        nf.SetMaximumFractionDigits(maxDigits);
        nf.SetMinimumFractionDigits(minDigits);
        return nf.Format(value);
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
            return UnitRange();     // Rounded past the largest float
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
}   // End of ChartScale.cs
}   // End of namespace PDFjet.NET
