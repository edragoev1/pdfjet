/*
 * Alignment.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
    /// <summary>
    /// Used to specify the horizontal and vertical alignment, for example of the
    /// text in a Cell, TextBox, TextBlock, Paragraph or TextColumn.
    /// </summary>
    public enum Alignment {
        /// <summary>Aligns to the left.</summary>
        LEFT,
        /// <summary>Aligns to the right.</summary>
        RIGHT,
        /// <summary>Centers horizontally or vertically.</summary>
        CENTER,
        /// <summary>Justifies the text.</summary>
        JUSTIFY,
        /// <summary>Aligns to the top.</summary>
        TOP,
        /// <summary>Aligns to the bottom.</summary>
        BOTTOM
    }
}
