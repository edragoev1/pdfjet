/**
 * PolygonAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

public class PolygonAnnotation extends BaseAnnotation {
    public PolygonAnnotation() {
        super.annotationType = Annotation.Polygon;
    }

    public void setVertices(float[] vertices) {
        super.vertices = vertices;
    }
}
