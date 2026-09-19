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
    List<StructElement> kids = new ArrayList<StructElement>();

    void addKidStructElem(StructElement structElem) {
        this.kids.add(structElem);
    }
}
