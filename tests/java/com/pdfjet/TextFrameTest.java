/*
 * TextFrameTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.Arrays;
import static org.junit.jupiter.api.Assertions.assertTrue;
import java.util.ArrayList;
import java.util.List;
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
    void aNegativeGapIsTakenAsZero() throws Exception {
        assertEquals(paragraphDistance(0f), paragraphDistance(-5f), TestSupport.DELTA);
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

    // Draws one paragraph in a frame of the width at x 0, and returns how far down its text reaches.
    private static float textHeight(String text, float width) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Paragraph paragraph = new Paragraph(new TextLine(TestSupport.helvetica(pdf), text));
        new TextFrame(Arrays.asList(paragraph)).setLocation(0f, 10f).setWidth(width)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
        return paragraph.getY2() - paragraph.getY1();
    }

    @Test
    void aRowTakesTheWordsThatFitWithoutTheSpaceAfterThem() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        float oneRow = textHeight("one two", 300f);
        float width = font.stringWidth("one ") + font.stringWidth("two");
        assertEquals(oneRow, textHeight("one two", width), TestSupport.DELTA);
        assertEquals(true, textHeight("one two", width - 0.1f) > oneRow, "two rows");
        // A word as wide as the frame is not broken.
        assertEquals(oneRow, textHeight("Hello", font.stringWidth("Hello")), TestSupport.DELTA);
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
    @Test
    void paragraphsWithALabelAreAList() throws Exception {
        // The label of an item is drawn where the item begins, so that it
        // reads before the text of the item and not after all of the text.
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = TestSupport.helvetica(pdf);
        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        String[] texts = {"alpha beta", "gamma delta"};
        for (int i = 0; i < texts.length; i++) {
            paragraphs.add(new Paragraph().add(new TextLine(font, texts[i]))
                    .setListLabel(new TextLine(font, (i + 1) + "."), 15f));
        }
        // A paragraph with no label ends the list.
        paragraphs.add(new Paragraph().add(new TextLine(font, "epsilon")));
        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(70f, 50f);
        frame.setWidth(300f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        frame.drawOn(page);
        String content = TestSupport.latin1(page.getContent());
        // The label of an item is drawn before the text of the item.
        assertTrue(content.indexOf(TestSupport.hex("1.")) < content.indexOf(TestSupport.hex("alpha")),
                content);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, raw.split("/S /L\n", -1).length - 1, raw);
        assertEquals(2, raw.split("/S /LI\n", -1).length - 1, raw);
        assertEquals(2, raw.split("/S /Lbl\n", -1).length - 1, raw);
        assertEquals(2, raw.split("/S /LBody\n", -1).length - 1, raw);
    }

}
