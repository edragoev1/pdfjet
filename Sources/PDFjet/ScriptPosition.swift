/**
 * ScriptPosition.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to specify whether text is drawn as normal text, as a subscript or as a
/// superscript.
///
public enum ScriptPosition {
    /// Normal text.
    case NORMAL
    /// Subscript text, below the baseline.
    case SUBSCRIPT
    /// Superscript text, above the baseline.
    case SUPERSCRIPT
}
