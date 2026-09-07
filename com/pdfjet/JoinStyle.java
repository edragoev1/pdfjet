/*
 * JoinStyle.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify the join style when joining two lines.
 * See the Page and Line classes for more details.
 */
public enum JoinStyle {
    /** Join style MITER */
    MITER,
    /** Join style ROUND */
    ROUND,
    /** Join style BEVEL */
    BEVEL;
}
