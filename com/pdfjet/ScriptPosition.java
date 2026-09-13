/*
 * ScriptPosition.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify whether text is drawn as normal text, as a subscript or as a
 * superscript.
 */
public enum ScriptPosition {
    /** Normal text. */
    NORMAL,
    /** Subscript text, below the baseline. */
    SUBSCRIPT,
    /** Superscript text, above the baseline. */
    SUPERSCRIPT
}
