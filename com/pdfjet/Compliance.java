/*
 * Compliance.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to set PDF/UA and PDF/A compliance.
 * See the constructors in the PDF class.
 */
public enum Compliance {
    /** PDF 1.7 without a compliance profile. */
    PDF_17,
    /** PDF/UA-1, for universal accessibility. */
    PDF_UA_1,
    /** PDF/A-1a, for archiving, with the accessibility requirements of level A. */
    PDF_A_1A,
    /** PDF/A-1b, for archiving, level B (basic). */
    PDF_A_1B,
    /** PDF/A-2a, for archiving, with the accessibility requirements of level A. */
    PDF_A_2A,
    /** PDF/A-2b, for archiving, level B (basic). */
    PDF_A_2B,
    /** PDF/A-3a, for archiving, with the accessibility requirements of level A. */
    PDF_A_3A,
    /** PDF/A-3b, for archiving, level B (basic). */
    PDF_A_3B;
}
