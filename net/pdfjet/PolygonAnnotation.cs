/*
 * PolygonAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>A polygon annotation.</summary>
public class PolygonAnnotation : BaseAnnotation {
    /// <summary>Creates a polygon annotation.</summary>
    public PolygonAnnotation() {
        base.annotationType = Annotation.Polygon;
    }

    /// <summary>Sets the vertices of the polygon as pairs of x and y coordinates.</summary>
    public PolygonAnnotation SetVertices(float[] vertices) {
        base.vertices = vertices;
        return this;
    }
}
}
