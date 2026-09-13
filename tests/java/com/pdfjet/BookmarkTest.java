/*
 * BookmarkTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertSame;

import org.junit.jupiter.api.Test;

class BookmarkTest {
    @Test
    void theRootHasNoTitleAndNoDestination() throws Exception {
        Bookmark root = new Bookmark(TestSupport.newPDF());
        assertNull(root.getTitle());
        assertNull(root.getDestinationName());
    }

    @Test
    void aTitleHasItsWhitespaceCollapsed() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Bookmark root = new Bookmark(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Bookmark child = root.addBookmark(page, new Title(TestSupport.helvetica(pdf), "Chapter\t one\n  intro", 10f, 10f));
        assertEquals("Chapter one intro", child.getTitle());
        assertNotNull(child.getDestinationName());
        assertSame(root, child.getParent());
    }
}
