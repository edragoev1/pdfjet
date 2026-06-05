/**
 * Field.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Please see Example_45
 */
namespace PDFjet.NET {
public class Field {
    internal float x;
    internal String[] values;
    internal String[] altDescription;
    internal String[] actualText;
    internal bool format;

    public Field(float x, String[] values) : this(x, values, false) {
    }

    public Field(float x, String[] values, bool format) {
        this.x = x;
        this.values = values;
        this.format = format;
        if (values != null) {
            altDescription = new String[values.Length];
            actualText     = new String[values.Length];
            for (int i = 0; i < values.Length; i++) {
                this.altDescription[i] = values[i];
                this.actualText[i]     = values[i];
            }
        }
    }

    public Field SetAltDescription(String altDescription) {
        this.altDescription[0] = altDescription;
        return this;
    }

    public Field SetActualText(String actualText) {
        this.actualText[0] = actualText;
        return this;
    }
}   // End of Field.cs
}   // End of namespace PDFjet.NET
