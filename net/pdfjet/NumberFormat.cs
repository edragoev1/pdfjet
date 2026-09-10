/*
 * NumberFormat.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>Formats numbers with a fixed number of fraction digits.</summary>
public class NumberFormat {
    int minFractionDigits = 0;
    int maxFractionDigits = 0;

    /// <summary>Returns a new number format.</summary>
    public static NumberFormat GetInstance() {
        return new NumberFormat();
    }

    /// <summary>Sets the minimum number of fraction digits. Format does not use this value.</summary>
    public NumberFormat SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    /// <summary>Sets the maximum number of fraction digits.</summary>
    public NumberFormat SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    /// <summary>Formats the value with the maximum number of fraction digits.</summary>
    public String Format(double value) {
        String format = "0.";
        for (int i = 0; i < maxFractionDigits; i++) {
            format += "0";
        }
        return value.ToString(format);
    }
}   // End of NumberFormat.cs
}   // End of package PDFjet.NET
