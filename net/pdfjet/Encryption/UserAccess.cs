/**
 * UserAccess.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// Represents the user access permissions for an encrypted PDF document as defined in
    /// ISO 32000-2 (PDF 2.0) Table 22. Permissions are stored as flags in a 32-bit integer.
    /// </summary>
    [Flags]
    public enum UserAccess {
        /// <summary>
        /// No permissions are granted. This is the default state.
        /// Reserved bits (0-2, 13-31) must be zero.
        /// </summary>
        None = 0,

        /// <summary>
        /// Permission to print the document (possibly not at the highest quality level,
        /// depending on whether <see cref="PrintHighQuality"/> is also set).
        /// (Bit position: 3)
        /// </summary>
        Print = 1 << 3, // Decimal: 8

        /// <summary>
        /// Permission to modify the contents of the document by operations other than
        /// those controlled by <see cref="ModifyAnnotations"/>, <see cref="FillFormFields"/>,
        /// and <see cref="AssembleDocument"/>.
        /// (Bit position: 4)
        /// </summary>
        ModifyContents = 1 << 4, // Decimal: 16

        /// <summary>
        /// Permission to copy or otherwise extract text and graphics from the document,
        /// including for accessibility purposes.
        /// (Bit position: 5)
        /// </summary>
        CopyContents = 1 << 5, // Decimal: 32

        /// <summary>
        /// Permission to add, modify, or delete text annotations and interactive form fields.
        /// Note: This permission is not used in PDF 2.0 but is retained for legacy support.
        /// (Bit position: 6)
        /// </summary>
        ModifyAnnotations = 1 << 6, // Decimal: 64

        /// <summary>
        /// Permission to fill existing interactive form fields (including signature fields),
        /// even if <see cref="ModifyContents"/> is not set.
        /// (Bit position: 9)
        /// </summary>
        FillFormFields = 1 << 9, // Decimal: 512

        /// <summary>
        /// Permission to extract text and graphics (in support of accessibility to
        /// users with disabilities or for other purposes).
        /// (Bit position: 10)
        /// </summary>
        ExtractContentsForAccessibility = 1 << 10, // Decimal: 1024

        /// <summary>
        /// Permission to assemble the document: insert, rotate, or delete pages and
        /// create bookmarks or thumbnail images.
        /// (Bit position: 11)
        /// </summary>
        AssembleDocument = 1 << 11, // Decimal: 2048

        /// <summary>
        /// Permission to print the document to a representation from which a faithful
        /// digital copy of the PDF content could be generated. When this bit is clear
        /// (and <see cref="Print"/> is set), printing is limited to a low-level
        /// representation of the appearance, possibly of degraded quality.
        /// (Bit position: 12)
        /// </summary>
        PrintHighQuality = 1 << 12 // Decimal: 4096
    }
}
