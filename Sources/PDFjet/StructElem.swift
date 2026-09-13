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
    public static let DOCUMENT = "Document"
    /// A large division of the document.
    public static let PART = "Part"
    /// A generic block-level element.
    public static let DIV = "Div"
    /// A section.
    public static let SECT = "Sect"

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
    public static let TITLE = "Title"
    /// A label, such as the bullet or number of a list item.
    public static let LBL = "Lbl"

    // Inline text
    /// A generic inline element.
    public static let SPAN = "Span"
    /// Emphasized text.
    public static let EM = "Em"
    /// Strongly emphasized text.
    public static let STRONG = "Strong"

    // Links and annotations
    /// A link.
    public static let LINK = "Link"
    /// An annotation.
    public static let ANNOT = "Annot"

    // List elements
    /// A list.
    public static let L = "L"   // List
    /// A list item.
    public static let LI = "LI" // List Item

    // Table elements
    /// A table.
    public static let TABLE = "Table"
    /// A table row.
    public static let TR = "TR"     // Table Row
    /// A table header cell.
    public static let TH = "TH"     // Table Header
    /// A table data cell.
    public static let TD = "TD"     // Table Data
    /// A group of table header rows.
    public static let THEAD = "THead" // Table Header group
    /// A group of table body rows.
    public static let TBODY = "TBody" // Table Body group
    /// A group of table footer rows.
    public static let TFOOT = "TFoot" // Table Footer group
    /// A table caption.
    public static let CAPTION = "Caption"

    // Figure and special elements
    /// A figure.
    public static let FIGURE = "Figure"
    /// Content that is not part of the document structure, such as page numbers.
    public static let ARTIFACT = "Artifact"

    var objNumber: Int?
    var structure: String?
    var pageObjNumber: Int?
    var mcid = 0
    var language: String?
    var altDescription: String?
    var actualText: String?
    var annotation: Annotation?
}
