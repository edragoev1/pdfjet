/*
 * NumberFormat.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;

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
        String label = value.ToString("F" + maxFractionDigits, CultureInfo.InvariantCulture);
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
}   // End of NumberFormat.cs
}   // End of package PDFjet.NET
