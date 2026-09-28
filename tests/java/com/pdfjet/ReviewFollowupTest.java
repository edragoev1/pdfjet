/*
 * ReviewFollowupTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;

/**
 * What was left open by the review: CMYK colors in PDF/A, the footer of a big
 * table, and the destinations and headings in a container.
 */
class ReviewFollowupTest {
    private static final float DELTA = TestSupport.DELTA;

    @Test
    void aPDFADocumentUsesNoCMYKColor() throws Exception {
        // The output intent of PDF/A is sRGB, so its colors are not CMYK, as
        // its images are not.
        Compliance[] levels = {Compliance.PDF_A_1B, Compliance.PDF_A_2B, Compliance.PDF_A_3A_UA_1};
        for (Compliance level : levels) {
            for (boolean pen : new boolean[] {true, false}) {
                final Page page = new Page(new PDF(new ByteArrayOutputStream(), level), Letter.PORTRAIT);
                String before = TestSupport.content(page);
                IllegalStateException e = assertThrows(IllegalStateException.class, () -> {
                    if (pen) {
                        page.setPenColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
                    } else {
                        page.setBrushColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
                    }
                });
                assertEquals("A document of " + level + " cannot use a CMYK color: "
                        + "its output intent is sRGB, so its colors are gray or RGB.", e.getMessage());
                // Nothing is written
                assertEquals(before, TestSupport.content(page));
            }
        }
        // A document that is not PDF/A uses them
        for (Compliance level : new Compliance[] {Compliance.PDF_1_7, Compliance.PDF_UA_1}) {
            Page page = new Page(new PDF(new ByteArrayOutputStream(), level), Letter.PORTRAIT);
            page.setPenColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
            page.setBrushColorCMYK(0.1f, 0.2f, 0.3f, 0.4f);
            String content = TestSupport.content(page);
            assertTrue(content.contains("0.1 0.2 0.3 0.4 K\n"), content);
            assertTrue(content.contains("0.1 0.2 0.3 0.4 k\n"), content);
        }
    }

    @Test
    void theFooterOfABigTableIsAPaginationArtifact() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font font = TestSupport.helvetica(pdf);
        List<String[]> rows = new ArrayList<String[]>();
        for (int i = 0; i < 100; i++) {
            rows.add(new String[] {"n" + i, "City, " + i, i + ".5"});
        }
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.setNumberOfColumns(3);
        table.setTableData(new String[] {"Name", "City", "Total"}, rows);
        table.setLocation(10f, 10f);
        table.complete();
        List<Page> pages = table.getPages();
        // The content of the last page, which is written when the PDF is
        String content = TestSupport.content(pages.get(pages.size() - 1));
        // The footer is marked once, as a footer, and not inside a plain artifact
        int footer = content.indexOf("/Artifact <</Type /Pagination /Subtype /Footer>> BDC\n");
        assertTrue(footer >= 0 && footer < content.indexOf(TestSupport.hex("Page 2 of 2")), content);
        assertFalse(content.contains("/Artifact BMC\n/Artifact <<"), content);
        pdf.complete();
    }

    // Draws the text line with a destination, as a heading, in a container at
    // 100, 200 turned by the degrees, or on the page at x, y moved by 100, 200
    // when there is no container, and returns the page.
    private static Page drawInContainer(boolean inContainer, float degrees) throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        TextLine line = new TextLine(font, "Heading");
        line.setDestination("heading");
        line.setStructureType(StructElem.H1);
        if (inContainer) {
            line.setLocation(0f, 50f);
            Container container = new Container(100f, 100f);
            container.setLocation(100f, 200f);
            container.setRotation(degrees);
            container.add(line);
            container.drawOn(page);
        } else {
            line.setLocation(100f, 250f);
            line.drawOn(page);
        }
        return page;
    }

    @Test
    void aDestinationAndAHeadingInAContainerAreWhereTheTextIs() throws Exception {
        // Moved with the container, as if drawn where it puts the text
        Page want = drawInContainer(false, 0f);
        Page got = drawInContainer(true, 0f);
        // The left of the page, where the destination of a text line is, is
        // the left of the container
        assertEquals(want.destinations.get(0).xPosition + 100f, got.destinations.get(0).xPosition, DELTA);
        assertEquals(want.destinations.get(0).yPosition, got.destinations.get(0).yPosition, DELTA);
        assertEquals(want.pdf.headings.get(0).top, got.pdf.headings.get(0).top, DELTA);
        assertEquals(250f - 12f, got.pdf.headings.get(0).top, DELTA);

        // Turned a quarter, the text runs down the page from 50 left of the
        // center, 150 and 250, and its top 12 above the baseline is 12 right of it
        Page turned = drawInContainer(true, 90f);
        assertEquals(162f, turned.destinations.get(0).xPosition, DELTA);
        assertEquals(792f - 200f, turned.destinations.get(0).yPosition, DELTA);
        assertEquals(200f, turned.pdf.headings.get(0).top, DELTA);
    }
}
