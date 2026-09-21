/*
 * StructElement.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

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
    // True for an element a drawable goes on adding to after the page it was
    // made on is written, like the Table of a table that runs over pages. It
    // is written when the document is completed; every other element is
    // written with its page and let go of.
    boolean open = false;

    void addKidObjNumber(int objNumber) {
        this.kids.add(objNumber);
    }
}
