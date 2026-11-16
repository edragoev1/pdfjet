/*
 * PolygonAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
/// <summary>A polygon annotation.</summary>
public class PolygonAnnotation : BaseAnnotation {
    /// <summary>Creates a polygon annotation.</summary>
    public PolygonAnnotation() {
        base.annotationType = Annotation.Polygon;
        // No vertices until set: an empty polygon, as in Go and Swift
        base.vertices = new float[0];
    }

    /// <summary>Sets the vertices of the polygon as pairs of x and y coordinates.</summary>
    public PolygonAnnotation SetVertices(float[] vertices) {
        base.vertices = vertices ?? new float[0];
        return this;
    }
}
}
