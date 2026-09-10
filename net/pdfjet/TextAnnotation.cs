/*
 * TextAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>A text note annotation.</summary>
public class TextAnnotation : BaseAnnotation {
    /// <summary>Creates a text note annotation.</summary>
    public TextAnnotation() {
        base.annotationType = Annotation.Text;
    }
}
}
