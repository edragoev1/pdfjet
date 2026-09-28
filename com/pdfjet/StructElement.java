/*
 * StructElement.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

/**
 * A structure element of the structure tree that PDF writes for a tagged
 * document: the marked content it refers to and its kids.
 */
class StructElement {
    int objNumber;
    String structure = null;
    int pageObjNumber;
    // The marked content this element refers to, or -1 for an element that
    // groups its kids, like a table row.
    int mcid = 0;
    // The attributes dictionary, like <</O /Table /Scope /Column>>, or null.
    String attributes = null;
    // The parent element, or null for a child of the Document element.
    StructElement parent = null;
    String language = null;
    String actualText = null;
    String altDescription = null;
    Annotation annotation = null;
    // The object numbers of the kids. A parent keeps the numbers and not the
    // kids, so that a page can write its elements and let go of them.
    List<Integer> kids = new ArrayList<Integer>();
    // The marked contents of an element that holds several of them, like a
    // paragraph whose words are drawn one at a time; and, in their order
    // among them, the elements of the words that are links, as the negative
    // of their object numbers.
    List<Integer> mcids = new ArrayList<Integer>();
    // True for an element a drawable goes on adding to after the page it was
    // made on is written, like the Table of a table that runs over pages. It
    // is written when the document is completed; every other element is
    // written with its page and let go of.
    boolean open = false;

    void addKidObjNumber(int objNumber) {
        this.kids.add(objNumber);
    }

    // The structure types that PDF makes inline, which stand in a paragraph;
    // one that is a kid of an element that groups others, like the Document,
    // stands as a block, and says so with the attribute Placement Block, or
    // PAC warns of it as a possibly inappropriate use: a figure, a link or an
    // annotation drawn on its own, and not inside a paragraph.
    private static final Set<String> INLINE_LEVEL = new HashSet<String>(Arrays.asList(
            "Figure", "Formula", "Form", "Note", "Link", "Annot"));

    // The structure types that group others and hold blocks, not text.
    private static final Set<String> GROUPING = new HashSet<String>(Arrays.asList(
            "Document", "Part", "Art", "Sect", "Div", "BlockQuote",
            "Caption", "TOC", "TOCI", "Index", "NonStruct", "Private"));

    // Whether the element is of an inline type and stands as a block, a kid of
    // the Document or of another element that groups others.
    boolean placedAsBlock() {
        if (!INLINE_LEVEL.contains(structure)) {
            return false;
        }
        return parent == null || GROUPING.contains(parent.structure);
    }

    // The attributes with Placement Block among those of the owner Layout: in
    // the Layout attributes the element has, like the BBox of a figure, or in
    // their own dictionary beside attributes of another owner.
    static String withPlacementBlock(String attributes) {
        final String layout = "<</O /Layout ";
        if (attributes == null || attributes.isEmpty()) {
            return "<</O /Layout /Placement /Block>>";
        }
        if (attributes.startsWith(layout)) {
            return layout + "/Placement /Block " + attributes.substring(layout.length());
        }
        return "[" + attributes + " <</O /Layout /Placement /Block>>]";
    }
}
