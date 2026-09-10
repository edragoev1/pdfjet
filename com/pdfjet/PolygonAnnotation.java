/*
 * PolygonAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * A polygon annotation.
 */
public class PolygonAnnotation extends BaseAnnotation {
    /**
     * Creates a polygon annotation.
     */
    public PolygonAnnotation() {
        super.annotationType = Annotation.Polygon;
    }

    /**
     * Sets the vertices of the polygon.
     *
     * @param vertices the x and y coordinates of the vertices, one pair after another.
     * @return this PolygonAnnotation object.
     */
    public PolygonAnnotation setVertices(float[] vertices) {
        super.vertices = vertices;
        return this;
    }
}
