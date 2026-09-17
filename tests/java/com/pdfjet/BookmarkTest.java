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

import java.io.ByteArrayOutputStream;
import java.util.List;
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

    private static PDFobj item(List<PDFobj> objects, String title) {
        for (PDFobj obj : objects) {
            String value = obj.getValue("/Title");
            if (!value.isEmpty() && obj.getValue("/Producer").isEmpty()
                    && TestSupport.utf16Hex(value).equals(title)) {
                return obj;
            }
        }
        throw new AssertionError("no outline item " + title);
    }

    @Test
    void nestedBookmarksMakeATreeThatReadersNeedNotRepair() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Bookmark root = new Bookmark(pdf);
        root.addBookmark(page, new Title(font, "A", 10f, 10f));
        Bookmark b = root.addBookmark(page, new Title(font, "B", 10f, 30f));
        b.addBookmark(page, new Title(font, "B1", 10f, 50f));
        Bookmark b2 = b.addBookmark(page, new Title(font, "B2", 10f, 70f));
        b2.addBookmark(page, new Title(font, "B2a", 10f, 90f));
        root.addBookmark(page, new Title(font, "C", 10f, 110f));
        pdf.complete();

        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        PDFobj outlines = null;
        for (PDFobj obj : objects) {
            if (obj.getValue("/Type").equals("/Outlines")) {
                outlines = obj;
            }
        }
        assertNotNull(outlines);
        String root0 = String.valueOf(outlines.getNumber());
        // The outline dictionary has the items of the first level.
        assertEquals(String.valueOf(item(objects, "A").getNumber()), outlines.getValue("/First"));
        assertEquals(String.valueOf(item(objects, "C").getNumber()), outlines.getValue("/Last"));
        assertEquals("3", outlines.getValue("/Count"));
        assertEquals(root0, item(objects, "A").getValue("/Parent"));
        assertEquals(root0, item(objects, "C").getValue("/Parent"));

        // A nested item has the item above it as its parent, and a closed item
        // counts the items that opening it shows.
        PDFobj itemB = item(objects, "B");
        PDFobj itemB2 = item(objects, "B2");
        assertEquals("-2", itemB.getValue("/Count"));
        assertEquals(String.valueOf(item(objects, "B1").getNumber()), itemB.getValue("/First"));
        assertEquals(String.valueOf(itemB2.getNumber()), itemB.getValue("/Last"));
        assertEquals(String.valueOf(itemB.getNumber()), item(objects, "B1").getValue("/Parent"));
        assertEquals(String.valueOf(itemB.getNumber()), itemB2.getValue("/Parent"));
        assertEquals(String.valueOf(itemB2.getNumber()), item(objects, "B1").getValue("/Next"));
        assertEquals(String.valueOf(item(objects, "B1").getNumber()), itemB2.getValue("/Prev"));
        assertEquals("-1", itemB2.getValue("/Count"));
        assertEquals(String.valueOf(itemB2.getNumber()), item(objects, "B2a").getValue("/Parent"));
        assertEquals("", item(objects, "B2a").getValue("/Count"));
    }

    @Test
    void aTitleIsATextStringThatEveryReaderDecodes() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Bookmark root = new Bookmark(pdf);
        root.addBookmark(page, new Title(TestSupport.helvetica(pdf), "\u00dcbersicht \u2013 r\u00e9sum\u00e9", 10f, 10f));
        pdf.complete();
        PDFobj obj = item(TestSupport.read(bos.toByteArray()), "\u00dcbersicht \u2013 r\u00e9sum\u00e9");
        assertEquals("<feff00dc", obj.getValue("/Title").substring(0, 9).toLowerCase());
    }
}
