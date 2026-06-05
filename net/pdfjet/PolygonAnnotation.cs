/**
 * PolygonAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class PolygonAnnotation : BaseAnnotation {
    public PolygonAnnotation() {
        base.annotationType = Annotation.Polygon;
    }

    public void SetVertices(float[] vertices) {
        base.vertices = vertices;
    }
}
}
