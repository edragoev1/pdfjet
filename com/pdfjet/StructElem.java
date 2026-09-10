/*
 * StructElem.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * Defines the StructElem types.
 */
public class StructElem {
    // Document structure
    /** The root of the structure tree. */
    public static final String DOCUMENT = "Document";
    /** A large division of the document. */
    public static final String PART = "Part";
    /** A generic block-level grouping. */
    public static final String DIV = "Div";
    /** A section. */
    public static final String SECT = "Sect";

    // Headings
    /** Heading level 1. */
    public static final String H1 = "H1";
    /** Heading level 2. */
    public static final String H2 = "H2";
    /** Heading level 3. */
    public static final String H3 = "H3";
    /** Heading level 4. */
    public static final String H4 = "H4";
    /** Heading level 5. */
    public static final String H5 = "H5";
    /** Heading level 6. */
    public static final String H6 = "H6";

    // Paragraphs and text
    /** A paragraph. */
    public static final String P = "P";
    /** The title of the document. */
    public static final String TITLE = "Title";
    /** A label, such as the bullet or number of a list item. */
    public static final String LBL = "Lbl";

    // Inline text
    /** A generic inline grouping. */
    public static final String SPAN = "Span";
    /** Emphasized text. */
    public static final String EM = "Em";
    /** Strongly emphasized text. */
    public static final String STRONG = "Strong";

    // Links and annotations
    /** A link. */
    public static final String LINK = "Link";
    /** An annotation. */
    public static final String ANNOT = "Annot";

    // Lists
    /** A list. */
    public static final String L = "L";
    /** A list item. */
    public static final String LI = "LI";

    // Tables
    /** A table. */
    public static final String TABLE = "Table";
    /** A table row. */
    public static final String TR = "TR";
    /** A table header cell. */
    public static final String TH = "TH";
    /** A table data cell. */
    public static final String TD = "TD";
    /** The header rows of a table. */
    public static final String THEAD = "THead";
    /** The body rows of a table. */
    public static final String TBODY = "TBody";
    /** The footer rows of a table. */
    public static final String TFOOT = "TFoot";
    /** The caption of a table or figure. */
    public static final String CAPTION = "Caption";

    // Figures
    /** A figure. */
    public static final String FIGURE = "Figure";
    /** Content that is not part of the logical structure, such as page decorations. */
    public static final String ARTIFACT = "Artifact";

    /** The object number of this element. */
    protected int objNumber;
    /** The structure type, for example "P". */
    protected String structure = null;
    /** The object number of the page this element is on. */
    protected int pageObjNumber;
    /** The marked content ID. */
    protected int mcid = 0;
    /** The language of the content. */
    protected String language = null;
    /** The actual text of the content. */
    protected String actualText = null;
    /** The alternate description of the content. */
    protected String altDescription = null;
    Annotation annotation = null;
    /** The child elements. */
    protected List<StructElem> kids = null;

    /** The default constructor */
    public StructElem() {
        this.kids = new ArrayList<StructElem>();
    }

    /**
     * Adds a child structure element.
     *
     * @param structElem the child element.
     */
    public void addKidStructElem(StructElem structElem) {
        this.kids.add(structElem);
    }
}
