package com.pdfjet;

import java.nio.charset.StandardCharsets;

/**
 * Byte sequences of the PDF syntax.
 */
public class Token {
    /** The default constructor */
    public Token() {
    }

    // Fundamental structural tokens
    /** A space. */
    public static final byte SPACE = (byte) ' ';
    /** A line feed. */
    public static final byte NEWLINE = (byte) '\n';
    /** Begins a dictionary. */
    public static final byte[] BEGIN_DICTIONARY = "<<\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends a dictionary. */
    public static final byte[] END_DICTIONARY = ">>\n".getBytes(StandardCharsets.US_ASCII);
    /** Begins a stream. */
    public static final byte[] STREAM = "stream\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends a stream. */
    public static final byte[] END_STREAM = "\nendstream\n".getBytes(StandardCharsets.US_ASCII);

    // Object management tokens
    /** Follows the object number to begin an indirect object. */
    public static final byte[] NEW_OBJ = " 0 obj\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends an indirect object. */
    public static final byte[] END_OBJ = "endobj\n".getBytes(StandardCharsets.US_ASCII);
    /** Follows an object number to make an indirect reference. */
    public static final byte[] OBJ_REF = " 0 R\n".getBytes(StandardCharsets.US_ASCII);

    // Text and content tokens
    /** Begins a text object. */
    public static final byte[] BEGIN_TEXT = "BT\n".getBytes(StandardCharsets.US_ASCII);
    /** Ends a text object. */
    public static final byte[] END_TEXT = "ET\n".getBytes(StandardCharsets.US_ASCII);

    // Essential property tokens (used everywhere)
    /** The /Length key. */
    public static final byte[] LENGTH = "/Length ".getBytes(StandardCharsets.US_ASCII);
    /** The /Type key. */
    public static final byte[] TYPE = "/Type ".getBytes(StandardCharsets.US_ASCII);
    /** The /Resources key. */
    public static final byte[] RESOURCES = "/Resources ".getBytes(StandardCharsets.US_ASCII);
}
