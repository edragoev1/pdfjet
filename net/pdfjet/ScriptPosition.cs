/*
 * ScriptPosition.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
/// <summary>
/// Used to specify whether text is drawn as normal text, as a subscript or as a
/// superscript.
/// </summary>
public enum ScriptPosition {
    /// <summary>Normal text.</summary>
    NORMAL,
    /// <summary>Subscript text, below the baseline.</summary>
    SUBSCRIPT,
    /// <summary>Superscript text, above the baseline.</summary>
    SUPERSCRIPT,
}
}   // End of namespace PDFjet.NET
