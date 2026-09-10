/*
 * SquareAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A square annotation.
 */
public class SquareAnnotation extends BaseAnnotation {
    /**
     * Creates a square annotation.
     */
    public SquareAnnotation() {
        super.annotationType = Annotation.Square;
    }
}
