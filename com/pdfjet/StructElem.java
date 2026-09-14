/*
 * StructElem.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * The structure element types of a tagged (PDF/UA) document, as in ISO 32000-1
 * section 14.8.4. A TextLine is a P by default; see TextLine.setStructureType.
 */
public enum StructElem {
    // Document structure
    /** The Document structure element. */
    DOCUMENT("Document"),
    /** The Part structure element. */
    PART("Part"),
    /** The Div structure element. */
    DIV("Div"),
    /** The Sect structure element. */
    SECT("Sect"),
    // Headings
    /** The H1 structure element. */
    H1("H1"),
    /** The H2 structure element. */
    H2("H2"),
    /** The H3 structure element. */
    H3("H3"),
    /** The H4 structure element. */
    H4("H4"),
    /** The H5 structure element. */
    H5("H5"),
    /** The H6 structure element. */
    H6("H6"),
    // Paragraphs and labels
    /** The P structure element. */
    P("P"),
    /** The Title structure element. */
    TITLE("Title"),
    /** The Lbl structure element. */
    LBL("Lbl"),
    // Inline
    /** The Span structure element. */
    SPAN("Span"),
    /** The Em structure element. */
    EM("Em"),
    /** The Strong structure element. */
    STRONG("Strong"),
    /** The Link structure element. */
    LINK("Link"),
    /** The Annot structure element. */
    ANNOT("Annot"),
    // Lists
    /** The L structure element. */
    L("L"),
    /** The LI structure element. */
    LI("LI"),
    // Tables
    /** The Table structure element. */
    TABLE("Table"),
    /** The TR structure element. */
    TR("TR"),
    /** The TH structure element. */
    TH("TH"),
    /** The TD structure element. */
    TD("TD"),
    /** The THead structure element. */
    THEAD("THead"),
    /** The TBody structure element. */
    TBODY("TBody"),
    /** The TFoot structure element. */
    TFOOT("TFoot"),
    /** The Caption structure element. */
    CAPTION("Caption"),
    // Figures and artifacts
    /** The Figure structure element. */
    FIGURE("Figure"),
    /** The Artifact structure element. */
    ARTIFACT("Artifact");

    final String type;

    StructElem(String type) {
        this.type = type;
    }
}
