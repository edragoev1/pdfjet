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
    public static final String DOCUMENT = "Document";
    public static final String PART = "Part";
    public static final String DIV = "Div";
    public static final String SECT = "Sect";

    // Headings
    public static final String H1 = "H1";
    public static final String H2 = "H2";
    public static final String H3 = "H3";
    public static final String H4 = "H4";
    public static final String H5 = "H5";
    public static final String H6 = "H6";

    // Paragraphs and text
    public static final String P = "P";
    public static final String TITLE = "Title";
    public static final String LBL = "Lbl";

    // Inline text
    public static final String SPAN = "Span";
    public static final String EM = "Em";
    public static final String STRONG = "Strong";

    // Links and annotations
    public static final String LINK = "Link";
    public static final String ANNOT = "Annot";

    // Lists
    public static final String L = "L";   // List container
    public static final String LI = "LI"; // List item

    // Tables
    public static final String TABLE = "Table";
    public static final String TR = "TR";
    public static final String TH = "TH";
    public static final String TD = "TD";
    public static final String THEAD = "THead";
    public static final String TBODY = "TBody";
    public static final String TFOOT = "TFoot";
    public static final String CAPTION = "Caption";

    // Figures
    public static final String FIGURE = "Figure";
    public static final String ARTIFACT = "Artifact";

    protected int objNumber;
    protected String structure = null;
    protected int pageObjNumber;
    protected int mcid = 0;
    protected String language = null;
    protected String actualText = null;
    protected String altDescription = null;
    Annotation annotation = null;
    protected List<StructElem> kids = null;

    /** The default constructor */
    public StructElem() {
        this.kids = new ArrayList<StructElem>();
    }

    public void addKidStructElem(StructElem structElem) {
        this.kids.add(structElem);
    }
}
