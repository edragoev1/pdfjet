/*
 * Field.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Please see Example_45
/// </summary>
public class Field {
    /// <summary>The horizontal position of this field within the form.</summary>
    protected internal float x;
    /// <summary>The label of this field.</summary>
    protected internal String label;
    /// <summary>The value of this field.</summary>
    protected internal String value;

    /// <summary>
    /// Creates a Field class that will be used in a Form class
    /// </summary>
    /// <param name="x">the horizontal position within the Form</param>
    /// <param name="label">the field label</param>
    /// <param name="value">the field value</param>
    public Field(float x, String label, String value) {
        this.x = x;
        this.label = label;
        this.value = value;
    }
}
}   // End of namespace PDFjet.NET
