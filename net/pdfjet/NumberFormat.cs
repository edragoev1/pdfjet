/*
 * NumberFormat.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
public class NumberFormat {
    int minFractionDigits = 0;
    int maxFractionDigits = 0;

    public static NumberFormat GetInstance() {
        return new NumberFormat();
    }

    public NumberFormat SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    public NumberFormat SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    public String Format(double value) {
        String format = "0.";
        for (int i = 0; i < maxFractionDigits; i++) {
            format += "0";
        }
        return value.ToString(format);
    }
}   // End of NumberFormat.cs
}   // End of package PDFjet.NET
