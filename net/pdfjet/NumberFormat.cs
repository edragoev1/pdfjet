/*
 * NumberFormat.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.Numerics;

namespace PDFjet.NET {
/// <summary>Formats numbers with a minimum and a maximum number of fraction digits.</summary>
internal class NumberFormat {
    int minFractionDigits = 0;
    int maxFractionDigits = 0;

    /// <summary>Returns a new number format.</summary>
    public static NumberFormat GetInstance() {
        return new NumberFormat();
    }

    /// <summary>Sets the minimum number of fraction digits. A minimum above the maximum raises the maximum, as in Java.</summary>
    public NumberFormat SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = Math.Max(minFractionDigits, 0);
        if (this.maxFractionDigits < this.minFractionDigits) {
            this.maxFractionDigits = this.minFractionDigits;
        }
        return this;
    }

    /// <summary>Sets the maximum number of fraction digits. A maximum below the minimum lowers the minimum, as in Java.</summary>
    public NumberFormat SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = Math.Max(maxFractionDigits, 0);
        if (this.minFractionDigits > this.maxFractionDigits) {
            this.minFractionDigits = this.maxFractionDigits;
        }
        return this;
    }

    /// <summary>
    /// Formats the value with at least the minimum and at most the maximum number of fraction digits,
    /// rounding the exact value half to even. The result has a "." decimal separator and no grouping
    /// whatever the current culture, and a value that rounds to zero has no minus sign.
    /// </summary>
    public String Format(double value) {
        if (Double.IsNaN(value)) {
            return "NaN";
        }
        if (Double.IsInfinity(value)) {
            return (value < 0) ? "-Infinity" : "Infinity";
        }
        String label = Fixed(value, maxFractionDigits);
        int point = label.IndexOf('.');
        if (point != -1) {
            int end = label.Length;
            while (end - point - 1 > minFractionDigits && label[end - 1] == '0') {
                end--;
            }
            if (end - point - 1 == 0) {
                end = point;
            }
            label = label.Substring(0, end);
        }
        if (label.Trim('-', '0', '.').Length == 0) {
            label = label.TrimStart('-');
        }
        return label;
    }

    // The value with the digits after the point, the exact value of the double
    // rounded half to even, as Java's DecimalFormat rounds it. ToString("F")
    // does so on .NET 8, but on .NET Framework it rounds 15 digits of the
    // value half away from zero: 0.125 was 0.13, and 1.005 1.01, where Java
    // has 0.12 and 1.00 (review of 17 October 2026).
    private static String Fixed(double value, int digits) {
        long bits = BitConverter.DoubleToInt64Bits(value);
        bool negative = bits < 0;
        int exponent = (int) ((bits >> 52) & 0x7FF);
        long mantissa = bits & 0xFFFFFFFFFFFFFL;
        if (exponent == 0) {
            exponent = 1;   // Subnormal
        } else {
            mantissa |= 1L << 52;
        }
        exponent -= 1075;   // value = mantissa * 2^exponent
        BigInteger scaled = new BigInteger(mantissa) * BigInteger.Pow(10, digits);
        if (exponent >= 0) {
            scaled <<= exponent;
        } else {
            BigInteger divisor = BigInteger.One << -exponent;
            BigInteger remainder;
            scaled = BigInteger.DivRem(scaled, divisor, out remainder);
            int half = (remainder * 2).CompareTo(divisor);
            if (half > 0 || (half == 0 && !scaled.IsEven)) {
                scaled += 1;
            }
        }
        String text = scaled.ToString(CultureInfo.InvariantCulture);
        if (digits > 0) {
            text = text.PadLeft(digits + 1, '0');
            text = text.Substring(0, text.Length - digits) + "." + text.Substring(text.Length - digits);
        }
        return negative ? "-" + text : text;
    }
}   // End of NumberFormat.cs
}   // End of package PDFjet.NET
