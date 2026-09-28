/*
 * BookmarkTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertTrue;

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

    // A document with the headings, as text lines of their structure types,
    // H1 on the first page and the others on the second.
    private static PDF headingsDoc(ByteArrayOutputStream bos, boolean tagged, String... headings)
            throws Exception {
        PDF pdf = tagged ? new PDF(bos, Compliance.PDF_UA_1) : new PDF(bos);
        if (tagged) {
            pdf.setTitle("Title");
        }
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = null;
        for (int i = 0; i < headings.length; i += 2) {
            if (page == null || headings[i].equals("H1") && i > 0) {
                page = new Page(pdf, Letter.PORTRAIT);
            }
            new TextLine(font, headings[i + 1]).setStructureType(StructElem.valueOf(headings[i]))
                    .setLocation(70f, 100f + 40f * (i / 2)).drawOn(page);
        }
        return pdf;
    }

    // A tagged document with headings and no bookmarks of its own has the
    // bookmarks of its headings, each under the heading of a higher level
    // before it, and each at the top of its heading on its page, as PAC asks.
    @Test
    void aTaggedDocumentHasTheBookmarksOfItsHeadings() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = headingsDoc(bos, true,
                "H1", "Intro", "H2", "What  it is", "H3", "In short", "H2", "Why", "H1", "Use");
        pdf.complete();
        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        PDFobj outlines = null;
        for (PDFobj obj : objects) {
            if (obj.getValue("/Type").equals("/Outlines")) {
                outlines = obj;
            }
        }
        assertNotNull(outlines);
        assertEquals(String.valueOf(item(objects, "Intro").getNumber()), outlines.getValue("/First"));
        assertEquals(String.valueOf(item(objects, "Use").getNumber()), outlines.getValue("/Last"));
        // The whitespace of a title is collapsed, as addBookmark does
        String intro = String.valueOf(item(objects, "Intro").getNumber());
        assertEquals(intro, item(objects, "What it is").getValue("/Parent"));
        assertEquals(String.valueOf(item(objects, "What it is").getNumber()), item(objects, "In short").getValue("/Parent"));
        assertEquals(intro, item(objects, "Why").getValue("/Parent"));
        assertEquals(String.valueOf(item(objects, "Why").getNumber()), item(objects, "What it is").getValue("/Next"));
        // The destination: the page of the heading, and the top of its text
        String introDest = item(objects, "Intro").getValue("/Dest");
        String useDest = item(objects, "Use").getValue("/Dest");
        assertTrue(introDest.contains("/XYZ"), introDest);
        assertNotEquals(introDest.trim().split("\\s+")[1], useDest.trim().split("\\s+")[1],
                "Intro and Use, on two pages, go to one: " + introDest + ", " + useDest);
        // 792 - (100 - 12): the top of a line of 12 points at 100
        assertTrue(introDest.contains("/XYZ 0 704 0"), introDest);
    }

    // A document that is not tagged has no headings, and one with bookmarks
    // of its own keeps them.
    @Test
    void theBookmarksOfHeadingsOnlyInATaggedDocumentWithoutBookmarks() throws Exception {
        ByteArrayOutputStream untagged = new ByteArrayOutputStream();
        headingsDoc(untagged, false, "H1", "Intro").complete();
        assertFalse(TestSupport.latin1(untagged.toByteArray()).contains("/Outlines"),
                "a document that is not tagged has bookmarks of its headings");

        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF own = headingsDoc(bos, true, "H1", "Intro");
        Page page = new Page(own, Letter.PORTRAIT);
        Font font = new Font(own, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        new Bookmark(own).addBookmark(page, new Title(font, "Mine", 10f, 10f));
        own.complete();
        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        item(objects, "Mine");
        for (PDFobj obj : objects) {
            String value = obj.getValue("/Title");
            if (!value.isEmpty() && obj.getValue("/Producer").isEmpty()) {
                assertNotEquals("Intro", TestSupport.utf16Hex(value),
                        "the bookmarks of the document were replaced by those of its headings");
            }
        }
    }
}
