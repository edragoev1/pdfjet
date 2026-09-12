/**
 * UserAccess.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Represents the user access permissions for an encrypted PDF document as
/// defined in ISO 32000-2 (PDF 2.0) Table 22. Permissions are stored as flags
/// in a 32-bit integer.
///
public enum UserAccess: Int {
    /// No permissions are granted. This is the default state.
    /// Reserved bits (0-2, 13-31) must be zero.
    case NONE = 0

    /// Permission to print the document (possibly not at the highest quality
    /// level, depending on whether PRINT_HIGH_QUALITY is also set).
    /// (Bit position: 3)
    case PRINT = 8

    /// Permission to modify the contents of the document by operations other
    /// than those controlled by MODIFY_ANNOTATIONS, FILL_FORM_FIELDS, and
    /// ASSEMBLE_DOCUMENT.
    /// (Bit position: 4)
    case MODIFY_CONTENTS = 16

    /// Permission to copy or otherwise extract text and graphics from the
    /// document, including for accessibility purposes.
    /// (Bit position: 5)
    case COPY_CONTENTS = 32

    /// Permission to add, modify, or delete text annotations and interactive
    /// form fields. Note: This permission is not used in PDF 2.0 but is
    /// retained for legacy support.
    /// (Bit position: 6)
    case MODIFY_ANNOTATIONS = 64

    /// Permission to fill existing interactive form fields (including
    /// signature fields), even if MODIFY_CONTENTS is not set.
    /// (Bit position: 9)
    case FILL_FORM_FIELDS = 512

    /// Permission to extract text and graphics (in support of accessibility
    /// to users with disabilities or for other purposes).
    /// (Bit position: 10)
    case EXTRACT_CONTENTS_FOR_ACCESSIBILITY = 1024

    /// Permission to assemble the document: insert, rotate, or delete pages
    /// and create bookmarks or thumbnail images.
    /// (Bit position: 11)
    case ASSEMBLE_DOCUMENT = 2048

    /// Permission to print the document to a representation from which a
    /// faithful digital copy of the PDF content could be generated. When this
    /// bit is clear (and PRINT is set), printing is limited to a low-level
    /// representation of the appearance, possibly of degraded quality.
    /// (Bit position: 12)
    case PRINT_HIGH_QUALITY = 4096

    ///
    /// Returns the bit mask of this permission.
    ///
    /// - Returns: the bit mask.
    ///
    public func getValue() -> Int {
        return rawValue
    }

    ///
    /// Checks if this permission is contained in the given flags.
    ///
    /// - Parameter flags: the permissions flags.
    /// - Returns: true if this permission is set in the flags.
    ///
    public func isSetIn(_ flags: Int) -> Bool {
        return (flags & rawValue) == rawValue
    }
}   // End of UserAccess.swift
