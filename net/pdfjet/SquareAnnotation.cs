/**
 * SquareAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
public class SquareAnnotation : BaseAnnotation {
    public SquareAnnotation() {
        base.annotationType = Annotation.Square;
    }
}
}
