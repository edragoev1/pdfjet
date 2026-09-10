/*
 * SquareAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>A square annotation.</summary>
public class SquareAnnotation : BaseAnnotation {
    /// <summary>Creates a square annotation.</summary>
    public SquareAnnotation() {
        base.annotationType = Annotation.Square;
    }
}
}
