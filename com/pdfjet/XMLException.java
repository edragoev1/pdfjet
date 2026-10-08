/*
 * XMLException.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/** The document is not the XML that XMLParser reads; the message says where. */
final class XMLException extends Exception {
    private static final long serialVersionUID = 1L;

    XMLException(String message) {
        super(message);
    }
}   // End of XMLException.java
