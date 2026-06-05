/**
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

    public void SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
    }

    public void SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
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
