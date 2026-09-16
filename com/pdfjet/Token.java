/*
 * Token.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.nio.charset.StandardCharsets;

/**
 * Byte sequences of the PDF syntax. The arrays are shared by every PDF being
 * written, so the class is not public.
 */
class Token {
    private Token() {
    }

    // Fundamental structural tokens
    /** A space. */
    static final byte SPACE = (byte) ' ';
    /** A line feed. */
    static final byte NEWLINE = (byte) '\n';
    /** Begins a dictionary. */
    static final byte[] BEGIN_DICTIONARY = "<<\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends a dictionary. */
    static final byte[] END_DICTIONARY = ">>\n".getBytes(StandardCharsets.US_ASCII);
    /** Begins a stream. */
    static final byte[] STREAM = "stream\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends a stream. */
    static final byte[] END_STREAM = "\nendstream\n".getBytes(StandardCharsets.US_ASCII);

    // Object management tokens
    /** Follows the object number to begin an indirect object. */
    static final byte[] NEW_OBJ = " 0 obj\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends an indirect object. */
    static final byte[] END_OBJ = "endobj\n".getBytes(StandardCharsets.US_ASCII);
    /** Follows an object number to make an indirect reference. */
    static final byte[] OBJ_REF = " 0 R\n".getBytes(StandardCharsets.US_ASCII);

    // Text and content tokens
    /** Begins a text object. */
    static final byte[] BEGIN_TEXT = "BT\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends a text object. */
    static final byte[] END_TEXT = "ET\n".getBytes(StandardCharsets.US_ASCII);

    // Essential property tokens (used everywhere)
    /** The /Length key. */
    static final byte[] LENGTH = "/Length ".getBytes(StandardCharsets.US_ASCII);
    /** The /Type key. */
    static final byte[] TYPE = "/Type ".getBytes(StandardCharsets.US_ASCII);
    /** The /Resources key. */
    static final byte[] RESOURCES = "/Resources ".getBytes(StandardCharsets.US_ASCII);
}
