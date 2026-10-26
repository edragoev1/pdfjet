/*
 * FlowTextTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

// The flows of these tests are on Letter pages with margins of 50 points: the
// text is 512 points wide, from x 50, and the pages hold 692 points of it,
// from y 50 to 742.
class FlowTextTest {
    private static final float MARGIN = 50f;
    private static final float BOTTOM = 742f;
    private static final float WIDTH = 512f;
    private static final float HEIGHT = 692f;

    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static FlowText flow(PDF pdf) {
        return new FlowText(pdf, Letter.PORTRAIT).setMargins(MARGIN);
    }

    private static Paragraph paragraph(Font font, String text) {
        return new Paragraph(new TextLine(font, text));
    }

    // An image of the repository, 100 points wide and as tall as the height,
    // narrower than the text.
    private static Image image(PDF pdf, float height) throws Exception {
        Image image = new Image(pdf, TestSupport.open("images/up-arrow.png"));
        return image.scaleBy(100f / image.getWidth(), height / image.getHeight());
    }

    @Test
    void paragraphsFlowAcrossPagesBetweenTheMargins() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        for (int i = 1; i <= 120; i++) {
            paragraphs.add(paragraph(font, "Paragraph " + i + ", long enough to be wrapped onto a second line "
                    + "of the text, which is as wide as the page less its margins of fifty points."));
        }
        List<Page> pages = new ArrayList<Page>();
        float[] xy = flow(pdf).add(paragraphs, 6f).drawOn(pages);
        assertTrue(pages.size() >= 2, "120 paragraphs on " + pages.size() + " page");
        for (int i = 0; i < paragraphs.size(); i++) {
            Paragraph p = paragraphs.get(i);
            assertTrue(p.getY1() >= MARGIN - TestSupport.DELTA && p.getY2() <= BOTTOM + TestSupport.DELTA,
                    "paragraph " + (i + 1) + " from " + p.getY1() + " to " + p.getY2() + ", outside the margins");
        }
        assertEquals(MARGIN + WIDTH, xy[0], TestSupport.DELTA);     // the right margin
        assertEquals(paragraphs.get(119).getY2(), xy[1], TestSupport.DELTA);
    }

    @Test
    void anImageThatDoesNotFitGoesWholeToTheNextPage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Image small = image(pdf, 40f);
        Image large = image(pdf, 200f);
        Paragraph paragraph = paragraph(font, "Above the images");
        List<Page> pages = new ArrayList<Page>();
        flow(pdf).add(paragraph).addSpace(600f).add(small).add(large).drawOn(pages);
        assertEquals(paragraph.getY2() + 600f, small.y, TestSupport.DELTA);    // under the space
        assertEquals(2, pages.size());
        assertEquals(MARGIN, large.y, TestSupport.DELTA);   // at the top of the next page
        assertEquals(200f, large.getHeight(), TestSupport.DELTA);
    }

    @Test
    void anImageTooLargeIsMadeAsLargeAsTheTextAndThePage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Image wide = image(pdf, 100f);
        wide.scaleBy(20f, 1f);      // 2,000 points wide
        Image tall = image(pdf, 2000f);
        float shape = tall.getWidth() / tall.getHeight();
        List<Page> pages = new ArrayList<Page>();
        flow(pdf).add(wide).add(tall).drawOn(pages);
        assertEquals(WIDTH, wide.getWidth(), TestSupport.DELTA);
        assertEquals(25.6f, wide.getHeight(), TestSupport.DELTA);  // 100 * 512 / 2,000
        assertEquals(HEIGHT, tall.getHeight(), TestSupport.DELTA);
        assertEquals(shape, tall.getWidth() / tall.getHeight(), 1e-4f);
        assertEquals(MARGIN, tall.y, TestSupport.DELTA);    // at the top of the second page
        assertEquals(2, pages.size());
    }

    @Test
    void anImageIsAlignedLeftCenterOrRight() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Image left = image(pdf, 50f);
        Image center = image(pdf, 50f);
        Image right = image(pdf, 50f);
        List<Page> pages = new ArrayList<Page>();
        flow(pdf).add(left).add(center, Alignment.CENTER).add(right, Alignment.RIGHT).drawOn(pages);
        float w = left.getWidth();
        assertEquals(MARGIN, left.x, TestSupport.DELTA);
        assertEquals(MARGIN + (WIDTH - w) / 2f, center.x, TestSupport.DELTA);
        assertEquals(MARGIN + WIDTH - w, right.x, TestSupport.DELTA);
        assertEquals(left.y + 50f, center.y, TestSupport.DELTA);   // one below the other
    }

    @Test
    void aHeadingKeepsWithTheFirstLineAfterIt() throws Exception {
        for (boolean followed : new boolean[] {true, false}) {
            PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            Paragraph heading = new Paragraph(new TextLine(font, "A heading").setFontSize(16f))
                    .setStructureType(StructElem.H1);
            float headingHeight = font.getAscent(16f) + font.getDescent(16f);
            // Room for the heading below the image, 2 points more, but not
            // for a line after it too
            Image filler = image(pdf, HEIGHT - headingHeight - 2f);
            FlowText flow = flow(pdf).add(filler).add(heading);
            Paragraph text = paragraph(font, "The text under the heading");
            if (followed) {
                flow.add(text);
            }
            List<Page> pages = new ArrayList<Page>();
            flow.drawOn(pages);
            if (followed) {
                assertEquals(MARGIN, heading.getY1(), TestSupport.DELTA);  // on the next page with its text
                assertEquals(heading.getY2(), text.getY1(), TestSupport.DELTA);
                assertEquals(2, pages.size());
            } else {
                assertEquals(filler.y + filler.getHeight(), heading.getY1(), TestSupport.DELTA);
                assertEquals(1, pages.size());
            }
        }
    }

    @Test
    void aSpaceAtTheTopOfAPageIsLeftOut() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Paragraph first = paragraph(font, "At the top of the first page");
        Image filler = image(pdf, HEIGHT);
        Paragraph second = paragraph(font, "At the top of the second page");
        List<Page> pages = new ArrayList<Page>();
        flow(pdf).addSpace(30f).add(first).add(filler).addSpace(30f).add(second).drawOn(pages);
        assertEquals(MARGIN, first.getY1(), TestSupport.DELTA);    // a space before everything
        assertEquals(MARGIN, second.getY1(), TestSupport.DELTA);   // a space after a full page
        assertEquals(3, pages.size());      // The text, the image, the text
    }

    @Test
    void startsOnAPageBelowTheFirstPageTop() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page first = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        Paragraph paragraph = paragraph(font, "Under the letterhead");
        List<Page> pages = new ArrayList<Page>();
        flow(pdf).setFirstPageTop(200f).add(paragraph).drawOn(first, pages);
        assertEquals(200f, paragraph.getY1(), TestSupport.DELTA);
        assertEquals(0, pages.size());      // the first page is not added
    }

    @Test
    void nothingToDrawMakesNoPage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        List<Page> pages = new ArrayList<Page>();
        flow(pdf).addSpace(20f).add(new Paragraph()).drawOn(pages);
        assertEquals(0, pages.size());
    }

    @Test
    void marginsThatLeaveNoRoomAreRefused() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        FlowText flow = flow(pdf).setMargins(400f).add(paragraph(font, "text"));
        assertThrows(IllegalArgumentException.class, () -> flow.drawOn(pages));
        assertEquals(0, pages.size());
    }
}
