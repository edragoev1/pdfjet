/*
 * Heading.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A heading of a tagged document, H1 to H6, as it was drawn, which the
 * document has a bookmark of when it has no bookmarks of its own.
 */
class Heading {
    final int level;
    final String title;
    final Page page;
    final float top;    // The top of its text on the page

    Heading(int level, String title, Page page, float top) {
        this.level = level;
        this.title = title;
        this.page = page;
        this.top = top;
    }

    // The level of a heading, 1 for H1 to 6 for H6, or 0 for a structure type
    // that is not a heading.
    static int levelOf(StructElem structure) {
        if (structure == null) {
            return 0;
        }
        switch (structure) {
        case H1: return 1;
        case H2: return 2;
        case H3: return 3;
        case H4: return 4;
        case H5: return 5;
        case H6: return 6;
        default: return 0;
        }
    }
}   // End of Heading.java
