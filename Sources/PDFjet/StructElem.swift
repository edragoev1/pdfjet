/**
 * StructElem.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Defines the StructElem types.
 */
public class StructElem {
    // Defines standard PDF structure element tags
    // as defined in ISO 32000-1 (PDF 1.7) and PDF/UA specifications

    // Document-level structure elements
    /// The whole document.
    public static let Document = "Document"
    /// A large division of the document.
    public static let Part = "Part"
    /// A generic block-level element.
    public static let Div = "Div"
    /// A section.
    public static let Sect = "Sect"

    // Heading elements
    /// A level 1 heading.
    public static let H1 = "H1"
    /// A level 2 heading.
    public static let H2 = "H2"
    /// A level 3 heading.
    public static let H3 = "H3"
    /// A level 4 heading.
    public static let H4 = "H4"
    /// A level 5 heading.
    public static let H5 = "H5"
    /// A level 6 heading.
    public static let H6 = "H6"

    // Paragraph and text elements
    /// A paragraph.
    public static let P = "P"
    /// A title.
    public static let Title = "Title"
    /// A label, such as the bullet or number of a list item.
    public static let Lbl = "Lbl"

    // Inline text
    /// A generic inline element.
    public static let Span = "Span"
    /// Emphasized text.
    public static let Em = "Em"
    /// Strongly emphasized text.
    public static let Strong = "Strong"

    // Links and annotations
    /// A link.
    public static let Link = "Link"
    /// An annotation.
    public static let Annot = "Annot"

    // List elements
    /// A list.
    public static let L = "L"   // List
    /// A list item.
    public static let LI = "LI" // List Item

    // Table elements
    /// A table.
    public static let Table = "Table"
    /// A table row.
    public static let TR = "TR"     // Table Row
    /// A table header cell.
    public static let TH = "TH"     // Table Header
    /// A table data cell.
    public static let TD = "TD"     // Table Data
    /// A group of table header rows.
    public static let THead = "THead" // Table Header group
    /// A group of table body rows.
    public static let TBody = "TBody" // Table Body group
    /// A group of table footer rows.
    public static let TFoot = "TFoot" // Table Footer group
    /// A table caption.
    public static let Caption = "Caption"

    // Figure and special elements
    /// A figure.
    public static let Figure = "Figure"
    /// Content that is not part of the document structure, such as page numbers.
    public static let Artifact = "Artifact"

    var objNumber: Int?
    var structure: String?
    var pageObjNumber: Int?
    var mcid = 0
    var language: String?
    var altDescription: String?
    var actualText: String?
    var annotation: Annotation?
}
