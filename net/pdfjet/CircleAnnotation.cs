/*
 * CircleAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>A circle annotation.</summary>
public class CircleAnnotation : BaseAnnotation {
    /// <summary>Creates a circle annotation.</summary>
    public CircleAnnotation() {
        base.annotationType = Annotation.Circle;
    }
}
}
