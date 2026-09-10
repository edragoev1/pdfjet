/*
 * CircleAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A circle annotation.
 */
public class CircleAnnotation extends BaseAnnotation {
    /**
     * Creates a circle annotation.
     */
    public CircleAnnotation() {
        super.annotationType = Annotation.Circle;
    }
}
