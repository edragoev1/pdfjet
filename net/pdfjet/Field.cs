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
    protected internal float x;
    protected internal String label;
    protected internal String value;

    /**
     * Creates a Field class that will be used in a Form class
     *
     * @param x the horizontal position within the Form
     * @param label the field label
     * @param value the field value
     */
    public Field(float x, String label, String value) {
        this.x = x;
        this.label = label;
        this.value = value;
    }
}
}   // End of namespace PDFjet.NET
