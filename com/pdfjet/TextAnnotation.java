/*
 * TextAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A text note annotation.
 */
public class TextAnnotation extends BaseAnnotation {
    /**
     * Creates a text note annotation.
     */
    public TextAnnotation() {
        super.annotationType = Annotation.Text;
    }
}
