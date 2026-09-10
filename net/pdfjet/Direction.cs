/*
 * Direction.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/*
 * Used to specify the text writing direction in TextBox.cs
 */
namespace PDFjet.NET {
/// <summary>Used to specify the text direction in TextBox.</summary>
public enum Direction {
    /// <summary>The text runs from left to right.</summary>
    LEFT_TO_RIGHT,
    /// <summary>The text runs from bottom to top.</summary>
    BOTTOM_TO_TOP,
    /// <summary>The text runs from top to bottom.</summary>
    TOP_TO_BOTTOM,
}
}   // End of namespace PDFjet.NET
