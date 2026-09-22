/*
 * Relationship.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Says how a file attached to the document relates to what the document shows.
 * A document of PDF/A-3 names one for each file it carries, and a reader shows
 * it to the person who opens the document.
 */
public enum Relationship {
    /** The file is the source the document was made from. */
    SOURCE("/Source"),
    /** The file holds the data behind what the document shows, such as the numbers of a chart. */
    DATA("/Data"),
    /** The file is the same content in another form, such as the invoice of a bill in XML. */
    ALTERNATIVE("/Alternative"),
    /** The file adds to the document without being part of what it shows. */
    SUPPLEMENT("/Supplement"),
    /** The relationship is none of the others, or is not known. */
    UNSPECIFIED("/Unspecified");

    final String value;

    Relationship(String value) {
        this.value = value;
    }
}   // End of Relationship.java
