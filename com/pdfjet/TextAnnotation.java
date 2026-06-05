/**
 * TextAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

public class TextAnnotation extends BaseAnnotation {
    public TextAnnotation() {
        super.annotationType = Annotation.Text;
    }
}
