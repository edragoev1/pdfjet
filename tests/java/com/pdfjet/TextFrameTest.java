/*
 * TextFrameTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.Arrays;
import org.junit.jupiter.api.Test;

class TextFrameTest {
    // Draws two paragraphs of one line and returns how far below the first the second starts.
    private static float paragraphDistance(Float gap) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Paragraph first = new Paragraph(new TextLine(font, "one"));
        Paragraph second = new Paragraph(new TextLine(font, "two"));
        TextFrame frame = new TextFrame(Arrays.asList(first, second)).setLocation(10f, 10f).setWidth(300f);
        if (gap != null) {
            frame.setParagraphGap(gap);
        }
        frame.drawOn(new Page(pdf, Letter.PORTRAIT));
        return second.getY1() - first.getY1();
    }

    @Test
    void theGapIsAddedToTheLineSoParagraphsNeverOverlap() throws Exception {
        float line = TestSupport.helvetica(TestSupport.newPDF()).getBodyHeight();
        assertEquals(2f * line, paragraphDistance(null), TestSupport.DELTA);    // one empty line
        assertEquals(line, paragraphDistance(0f), TestSupport.DELTA);
        assertEquals(line + 10f, paragraphDistance(10f), TestSupport.DELTA);
    }

    @Test
    void theDefaultGapIsAnEmptyLineOfTheNextParagraph() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Paragraph heading = new Paragraph(new TextLine(font, "Heading").setFontSize(24f));
        Paragraph body = new Paragraph(new TextLine(font, "body"));
        new TextFrame(Arrays.asList(heading, body)).setLocation(10f, 10f).setWidth(300f)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
        // The heading, then one empty line in the size of the body text
        assertEquals(font.getBodyHeight(24f) + font.getBodyHeight(font.getSize()),
                body.getY1() - heading.getY1(), TestSupport.DELTA);
    }

    // Draws one paragraph with the alignment in a frame 200 wide at x 10, and returns it.
    private static Paragraph drawAligned(Alignment alignment, String text) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Paragraph paragraph = new Paragraph(new TextLine(TestSupport.helvetica(pdf), text));
        paragraph.setTextAlignment(alignment);
        new TextFrame(Arrays.asList(paragraph)).setLocation(10f, 10f).setWidth(200f)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
        return paragraph;
    }

    @Test
    void aRightAlignedParagraphEndsAtTheRightEdge() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        Paragraph paragraph = drawAligned(Alignment.RIGHT, "Hello");
        // The text ends at the right edge; the space after it is past the edge.
        assertEquals(210f - font.stringWidth("Hello"), paragraph.getTextX(), TestSupport.DELTA);
        assertEquals(210f + font.stringWidth(" "), paragraph.getX2(), TestSupport.DELTA);
    }

    @Test
    void aCenteredParagraphHasTheSameSpaceOnBothSides() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        Paragraph paragraph = drawAligned(Alignment.CENTER, "Hello");
        assertEquals(10f + (200f - font.stringWidth("Hello")) / 2f, paragraph.getTextX(), TestSupport.DELTA);
    }

    @Test
    void aJustifiedParagraphLeavesItsLastRowAsItIs() throws Exception {
        String text = "one two three four five six seven eight nine ten eleven twelve thirteen";
        Paragraph left = drawAligned(Alignment.LEFT, text);
        Paragraph justified = drawAligned(Alignment.JUSTIFY, text);
        assertEquals(true, left.getY2() - left.getY1() > 20f, "more than one row");
        assertEquals(left.getY2(), justified.getY2(), TestSupport.DELTA);
        assertEquals(left.getX2(), justified.getX2(), TestSupport.DELTA);
    }
}
