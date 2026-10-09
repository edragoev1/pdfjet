/*
 * XMLException.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/** The document is not the XML that XMLParser reads; the message says where. */
public final class XMLException extends Exception {
    private static final long serialVersionUID = 1L;

    /**
     * Makes the exception of a document that is not the XML XMLParser reads,
     * or of what a reader of the elements finds wrong in them.
     *
     * @param message what is wrong, and where.
     */
    public XMLException(String message) {
        super(message);
    }
}   // End of XMLException.java
