/*
 * ChartScale.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.math.*;

/**
 * What the charts share, BarChart, DonutChart, SimpleBarChart, SimpleLineChart
 * and SimpleScatterChart: the numbers of their labels, the rounding of an axis to
 * steps, the spacing of the labels of an axis, the place of a value on a plot,
 * the shortening of a long name, and the test of a value to draw.
 */
final class ChartScale {
    private ChartScale() {
    }

    /**
     * Formats a label with minDigits to maxDigits decimal places, rounding the
     * exact value half to even. The label has a "." decimal separator and no
     * grouping whatever the default locale, and a value that rounds to zero
     * has no minus sign.
     */
    static String format(float value, int minDigits, int maxDigits) {
        if (Float.isNaN(value) || Float.isInfinite(value)) {
            return String.valueOf(value);
        }
        // A number of 16 digits or more as three significant digits and a
        // power of ten, 3e38 and 1.23e20, where it was written out in full
        if (Math.abs(value) >= 1e15f) {
            BigDecimal round = new BigDecimal(value).round(new MathContext(3, RoundingMode.HALF_EVEN));
            int exponent = round.precision() - round.scale() - 1;
            BigDecimal mantissa = round.movePointLeft(exponent).stripTrailingZeros();
            return mantissa.toPlainString() + "e" + exponent;
        }
        // A minimum above the maximum is lowered to it, as in NumberFormat
        maxDigits = Math.max(maxDigits, 0);
        minDigits = Math.min(Math.max(minDigits, 0), maxDigits);
        BigDecimal label = new BigDecimal(value)
                .setScale(maxDigits, RoundingMode.HALF_EVEN)
                .stripTrailingZeros();
        if (label.scale() < minDigits) {
            label = label.setScale(minDigits);
        }
        return label.toPlainString();
    }

    /**
     * Returns the number of decimal places, at most maxDigits, that write the
     * axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
     */
    static int fractionDigitsOf(float step, int maxDigits) {
        for (int digits = 0; digits < maxDigits; digits++) {
            double scaled = step * Math.pow(10, digits);
            // rint, a double, as the other ports round: round is a long, which
            // stops at 2^63, so a step of 1e19 had fraction digits (review of
            // 17 October 2026)
            if (Math.abs(scaled - Math.rint(scaled)) < 1e-4) {
                return digits;
            }
        }
        return Math.max(maxDigits, 0);
    }

    /**
     * Rounds axis range to "nice" values for clean grid lines.
     * Uses the span (max - min) to support negative values and
     * zero crossings. Rounds max up and min down to step multiples.
     */
    static Round roundMaxAndMinValues(float maxValue, float minValue) {
        float span = maxValue - minValue;
        if (!isValue(span)) {
            return unitRange();     // No number, or a span past the largest float
        }
        if (span <= 0f) { span = flatDataSpan(minValue); }  // guard against flat data

        int exponent = (int) Math.floor(Math.log(span) / Math.log(10));
        float normalizedSpan = span * (float) Math.pow(10, -exponent);

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

        // Scale back to original magnitude and compute step
        float step = niceSpan * (float) Math.pow(10, exponent) / numOfGridLines;

        Round round = new Round();

        // Round max up, min down to nearest step multiple
        round.maxValue = (float) Math.ceil(maxValue / step) * step;
        round.minValue = (float) Math.floor(minValue / step) * step;
        if (!isValue(round.maxValue - round.minValue)) {
            // Rounded past the largest float: the range of the values as
            // they are, in lines of a round step where there is one; it fell
            // back to 0 to 1, and the bars of 3e38 went past the plot
            round.maxValue = maxValue;
            round.minValue = minValue;
            round.numOfGridLines = roundLines(minValue, maxValue, numOfGridLines);
            return round;
        }

        // Recount grid lines from actual rounded range
        round.numOfGridLines = Math.round((round.maxValue - round.minValue) / step);
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
    private static Round unitRange() {
        Round round = new Round();
        round.maxValue = 1f;
        round.numOfGridLines = 1;
        return round;
    }

    // Returns the span of the range of flat data at the value: 1, or a
    // hundred thousandth of the value when that is more, since above 2^24 a
    // float has no room for 1 more, and the range would stay flat.
    static float flatDataSpan(float value) {
        return Math.max(1f, Math.abs(value) * 1e-5f);
    }

    /**
     * Returns how many grid lines apart the labels of the value axis are
     * drawn: 1 when a label fits the spacing of the lines, and more when it
     * needs more room, so that no two labels meet.
     */
    static int axisLabelStep(float spacing, float needed, int lines) {
        int every = 1;
        while (every < lines && spacing * every < needed) {
            every++;
        }
        return every;
    }

    // Tells if the value is a number to draw: a value that is NaN or infinite
    // has no bar, like a value missing at the end of a series.
    static boolean isValue(float value) {
        return !Float.isNaN(value) && !Float.isInfinite(value);
    }

    /**
     * The axis of a range of values: its first and last value, and its steps.
     */
    static final class Axis {
        final float min;
        final float max;
        final int lines;

        Axis(float min, float max, int lines) {
            this.min = min;
            this.max = max;
            this.lines = lines;
        }
    }

    /**
     * Returns the axis of the values: the range set, or the range of the
     * values rounded to steps.
     */
    static Axis axisOf(float[] values, float lo, float hi, boolean set) {
        if (set) {
            return new Axis(lo, hi, roundLines(lo, hi, Math.max(roundMaxAndMinValues(hi, lo).numOfGridLines, 1)));
        }
        lo = values[0];
        hi = values[0];
        for (int i = 1; i < values.length; i++) {
            lo = Math.min(lo, values[i]);
            hi = Math.max(hi, values[i]);
        }
        if (hi == lo) {
            hi = lo + flatDataSpan(lo);
        }
        Round round = roundMaxAndMinValues(hi, lo);
        return new Axis(round.minValue, round.maxValue, round.numOfGridLines);
    }

    /**
     * Returns the number of grid lines of a range that is set, from 2 to 10,
     * the nearest to the lines given, fewer first, whose step is a round
     * number, 1, 2, 2.5 or 5 times a power of ten; or the lines given when
     * no number is. The range 3 to 17 in the 6 lines of its rounded span was
     * written 3.00, 5.33, 7.67; in 7 it is 3, 5, 7.
     */
    static int roundLines(float lo, float hi, int lines) {
        double span = (double) hi - (double) lo;
        for (int d = 0; d <= 8; d++) {
            if (isRound(span, lines - d)) {
                return lines - d;
            }
            if (isRound(span, lines + d)) {
                return lines + d;
            }
        }
        return lines;
    }

    // Tells if the span in the lines, from 2 to 10, has a round step.
    private static boolean isRound(double span, int lines) {
        if (lines < 2 || lines > 10) {
            return false;
        }
        double step = span / lines;
        if (!(step > 0.0) || Double.isInfinite(step)) {
            return false;
        }
        double m = step / Math.pow(10.0, Math.floor(Math.log10(step)));
        return Math.abs(m - 1.0) < 1e-5 || Math.abs(m - 2.0) < 2e-5 || Math.abs(m - 2.5) < 2.5e-5
                || Math.abs(m - 5.0) < 5e-5 || Math.abs(m - 10.0) < 1e-4;
    }

    /**
     * Returns the text, shortened with an ellipsis to fit the width in the
     * font at the size, or the text when it fits: a name too long for a
     * chart's column of names.
     */
    static String shorten(Font font, float size, String text, float width) {
        if (font.stringWidth(size, text) <= width) {
            return text;
        }
        // The longest start of the text that fits with the ellipsis, found by
        // halving the number of code points it keeps
        int count = text.codePointCount(0, text.length());
        String best = "\u2026";
        int lo = 0;
        int hi = count - 1;
        while (lo <= hi) {
            int mid = (lo + hi) >>> 1;
            String shorter = shortened(text, mid);
            if (font.stringWidth(size, shorter) <= width) {
                best = shorter;
                lo = mid + 1;
            } else {
                hi = mid - 1;
            }
        }
        return best;
    }

    // The first code points of the text, its trailing spaces left out, and an
    // ellipsis.
    private static String shortened(String text, int codePoints) {
        int cut = text.offsetByCodePoints(0, codePoints);
        while (cut > 0 && text.charAt(cut - 1) == ' ') {
            cut--;
        }
        return text.substring(0, cut) + "\u2026";
    }

    /**
     * Returns the distance along a length of a value whose distance from the
     * start of the axis is d, on an axis of the span: d * length / span, in
     * floats, as it always was, or in doubles when the product is past the
     * largest float. Bars of 3e38 on an axis of 0 to 3e38 threw, their
     * coordinates infinite (the review of 26 October 2026, the second).
     */
    static float scaled(float d, float length, float span) {
        float product = d * length;
        if (isValue(product)) {
            return product / span;
        }
        return (float) ((double) d * (double) length / (double) span);
    }

    /**
     * Returns the number of digits after the decimal point that the numbers of
     * an axis of the step need, 2 at least and 6 at most: data under 0.01 was
     * written 0.00 at every grid line.
     */
    static int digitsOf(float step) {
        return isValue(step) && step > 0f ? Math.max(2, fractionDigitsOf(step, 6)) : 2;
    }

    /**
     * Tells if the span of the values, the largest less the smallest, is a
     * number: values of -3e38 and 3e38 have a span past the largest float,
     * which no axis can draw.
     */
    static boolean spanFits(float[] values) {
        if (values.length == 0) {
            return true;
        }
        float lo = values[0];
        float hi = values[0];
        for (float value : values) {
            lo = Math.min(lo, value);
            hi = Math.max(hi, value);
        }
        return isValue(hi - lo);
    }

    /**
     * Returns the value held to the axis and to one span of it past each end,
     * so that a value far outside a range that is set is drawn just past the
     * plot, where it is clipped, and not at a coordinate too large for a PDF.
     * A value on the axis is returned as it is.
     */
    static float held(float value, Axis axis) {
        double span = (double) axis.max - (double) axis.min;
        return (float) Math.max((double) axis.min - span, Math.min((double) axis.max + span, value));
    }

    /**
     * Tells if the value is on the axis, its ends included.
     */
    static boolean onAxis(float value, Axis axis) {
        return value >= axis.min && value <= axis.max;
    }

    /**
     * Tells if the axis can be drawn: the span of a range that is set, or of
     * values that are, can be past the largest float.
     */
    static boolean drawable(Axis axis) {
        return isValue(axis.max - axis.min);
    }

    /**
     * Joins two parts of a chart's description, the title and the subtitle, as
     * two sentences: "Vehicles. By model year", the second's first letter a
     * capital when it is a to z. A part that is empty is left out, and the
     * first keeps its own full stop, question or exclamation mark. They were
     * joined with a comma, and a subtitle kept its capital after it.
     */
    static String join(String first, String second) {
        if (second.isEmpty()) {
            return first;
        }
        if (first.isEmpty()) {
            return second;
        }
        // The second a sentence, its first letter a capital when it is a to z
        char start = second.charAt(0);
        if (start >= 'a' && start <= 'z') {
            second = (char) (start - 'a' + 'A') + second.substring(1);
        }
        char end = first.charAt(first.length() - 1);
        return first + ((end == '.' || end == '?' || end == '!') ? " " : ". ") + second;
    }

    /**
     * Groups the digits of a number before its decimal point in thousands with
     * commas: 23507 is 23,507.
     */
    static String groupDigits(String label) {
        if (label.indexOf('e') >= 0) {
            return label;   // A power of ten has no thousands
        }
        int start = label.startsWith("-") ? 1 : 0;
        int end = label.indexOf('.');
        if (end < 0) {
            end = label.length();
        }
        StringBuilder sb = new StringBuilder(label);
        for (int i = end - 3; i > start; i -= 3) {
            sb.insert(i, ',');
        }
        return sb.toString();
    }
}   // End of ChartScale.java
