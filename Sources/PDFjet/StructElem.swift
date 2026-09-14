/**
 * StructElem.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/// The structure element types of a tagged (PDF/UA) document, as in ISO 32000-1
/// section 14.8.4, with the name written to the PDF as the raw value. A TextLine
/// is a P by default; see TextLine.setStructureType.
public enum StructElem: String {
    /// The Document structure element.
    case DOCUMENT = "Document"
    /// The Part structure element.
    case PART = "Part"
    /// The Div structure element.
    case DIV = "Div"
    /// The Sect structure element.
    case SECT = "Sect"
    /// The H1 structure element.
    case H1 = "H1"
    /// The H2 structure element.
    case H2 = "H2"
    /// The H3 structure element.
    case H3 = "H3"
    /// The H4 structure element.
    case H4 = "H4"
    /// The H5 structure element.
    case H5 = "H5"
    /// The H6 structure element.
    case H6 = "H6"
    /// The P structure element.
    case P = "P"
    /// The Title structure element.
    case TITLE = "Title"
    /// The Lbl structure element.
    case LBL = "Lbl"
    /// The Span structure element.
    case SPAN = "Span"
    /// The Em structure element.
    case EM = "Em"
    /// The Strong structure element.
    case STRONG = "Strong"
    /// The Link structure element.
    case LINK = "Link"
    /// The Annot structure element.
    case ANNOT = "Annot"
    /// The L structure element.
    case L = "L"
    /// The LI structure element.
    case LI = "LI"
    /// The Table structure element.
    case TABLE = "Table"
    /// The TR structure element.
    case TR = "TR"
    /// The TH structure element.
    case TH = "TH"
    /// The TD structure element.
    case TD = "TD"
    /// The THead structure element.
    case THEAD = "THead"
    /// The TBody structure element.
    case TBODY = "TBody"
    /// The TFoot structure element.
    case TFOOT = "TFoot"
    /// The Caption structure element.
    case CAPTION = "Caption"
    /// The Figure structure element.
    case FIGURE = "Figure"
    /// The Artifact structure element.
    case ARTIFACT = "Artifact"
}
