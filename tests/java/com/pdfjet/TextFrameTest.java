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

    // 200 paragraphs of a few lines each, the first word of each its number,
    // p000 to p199, which no other word begins with.
    private static TextFrame novel(Font font) {
        List<String> paragraphs = new ArrayList<String>();
        for (int i = 0; i < 200; i++) {
            StringBuilder text = new StringBuilder(String.format("p%03d", i));
            for (int j = 0; j < 30 + i % 17; j++) {
                text.append(" word").append(j);
            }
            paragraphs.add(text.toString());
        }
        return new TextFrame(font, paragraphs).setLocation(72f, 72f).setWidth(468f);
    }

    // The page each paragraph starts on, by its number.
    private static int[] pagesOf(List<Page> pages) {
        int[] pageOf = new int[200];
        java.util.Arrays.fill(pageOf, -1);
        for (int p = 0; p < pages.size(); p++) {
            String content = TestSupport.content(pages.get(p));
            for (int i = 0; i < 200; i++) {
                if (content.contains(TestSupport.hex(String.format("p%03d", i)))) {
                    assertEquals(-1, pageOf[i], "paragraph " + i + " starts on two pages");
                    pageOf[i] = p;
                }
            }
        }
        return pageOf;
    }

    @Test
    void aFrameFlowsOntoAsManyPagesAsTheTextNeeds() throws Exception {
        PDF pdf = TestSupport.newPDF();
        TextFrame frame = novel(TestSupport.helvetica(pdf));
        List<Page> pages = new ArrayList<Page>();
        frame.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() > 5, pages.size() + " pages");
        assertTrue(!frame.hasMoreText());
        // Every paragraph is drawn once, in order, and every page has text.
        int[] pageOf = pagesOf(pages);
        for (int i = 0; i < 200; i++) {
            assertTrue(pageOf[i] >= 0, "paragraph " + i + " is not drawn");
            assertTrue(i == 0 || pageOf[i] >= pageOf[i - 1], "paragraph " + i + " is out of order");
        }
        assertEquals(pages.size() - 1, pageOf[199]);
        // The text keeps the margin of its location at the bottom too: no
        // baseline under 72 points from the bottom of the page.
        java.util.regex.Pattern td = java.util.regex.Pattern.compile("[-0-9.]+ ([-0-9.]+) Td\n");
        int baselines = 0;
        for (Page page : pages) {
            java.util.regex.Matcher m = td.matcher(TestSupport.content(page));
            while (m.find()) {
                assertTrue(Float.parseFloat(m.group(1)) >= 72f, "a baseline at y = " + m.group(1));
                baselines++;
            }
        }
        assertTrue(baselines > 100, baselines + " baselines");
        // The frame has no height of its own, as before.
        assertEquals(0f, frame.getHeight(), 0f);
    }

    @Test
    void aFrameWithAHeightHasItOnEveryPage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        List<Page> tall = new ArrayList<Page>();
        novel(TestSupport.helvetica(pdf)).drawOn(pdf, tall, Letter.PORTRAIT);
        TextFrame frame = novel(TestSupport.helvetica(pdf)).setHeight(300f);
        List<Page> pages = new ArrayList<Page>();
        float[] xy = frame.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() > tall.size(), pages.size() + " pages, not more than " + tall.size());
        assertEquals(300f, frame.getHeight(), 0f);
        TestSupport.assertXY(540f, 372f, xy);
        // An empty frame needs no page.
        List<Page> none = new ArrayList<Page>();
        TestSupport.assertXY(10f, 20f, new TextFrame(new ArrayList<Paragraph>()).setLocation(10f, 20f)
                .drawOn(pdf, none, Letter.PORTRAIT));
        assertEquals(0, none.size());
    }

    // A paragraph of text lines in the font, each added with add, or with
    // addJoined when it starts with "+", which is not part of its text.
    static Paragraph joinedParagraph(Font font, String... texts) {
        Paragraph paragraph = new Paragraph();
        for (String text : texts) {
            if (text.startsWith("+")) {
                paragraph.addJoined(new TextLine(font, text.substring(1)));
            } else {
                paragraph.add(new TextLine(font, text));
            }
        }
        return paragraph;
    }

    private static String drawJoined(float width, Alignment alignment, String... texts)
            throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Paragraph paragraph = joinedParagraph(font, texts);
        if (alignment != null) {
            paragraph.setTextAlignment(alignment);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextFrame(Arrays.asList(paragraph)).setLocation(10f, 10f).setWidth(width).drawOn(page);
        return TestSupport.content(page);
    }

    @Test
    void aJoinedTextLineHasNoSpaceBeforeIt() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        String content = drawJoined(300f, null, "one", "+,", "two");
        float[] one = TestSupport.positionOf(content, "one");
        float[] comma = TestSupport.positionOf(content, ",");
        float[] two = TestSupport.positionOf(content, "two");
        assertEquals(one[0] + font.stringWidth("one"), comma[0], TestSupport.DELTA);
        assertEquals(comma[0] + font.stringWidth(", "), two[0], TestSupport.DELTA);
        assertEquals(one[1], two[1], TestSupport.DELTA);
    }

    @Test
    void aRowDoesNotBreakInsideAJoinedWord() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        // "aaa bbb" fits in the row, and "aaa bbbccc" does not, so bbb goes on
        // the next row with the ccc joined to it.
        float width = font.stringWidth("aaa bbb") + 1f;
        String content = drawJoined(width, null, "aaa bbb", "+ccc");
        float[] aaa = TestSupport.positionOf(content, "aaa");
        float[] bbb = TestSupport.positionOf(content, "bbb");
        float[] ccc = TestSupport.positionOf(content, "ccc");
        assertTrue(bbb[1] < aaa[1], "bbb is on the second row");
        assertEquals(10f, bbb[0], TestSupport.DELTA);
        assertEquals(bbb[1], ccc[1], TestSupport.DELTA);
        assertEquals(bbb[0] + font.stringWidth("bbb"), ccc[0], TestSupport.DELTA);
    }

    @Test
    void aJoinedWordWiderThanTheFrameBreaksWhereItIsJoined() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        String content = drawJoined(font.stringWidth("abc") + 1f, null, "abc", "+def");
        float[] abc = TestSupport.positionOf(content, "abc");
        float[] def = TestSupport.positionOf(content, "def");
        assertTrue(def[1] < abc[1], "def is on the second row");
        assertEquals(10f, def[0], TestSupport.DELTA);
    }

    @Test
    void aSpaceWhereTheyMeetKeepsJoinedTextLinesApart() throws Exception {
        assertEquals(drawJoined(300f, null, "one", "two"), drawJoined(300f, null, "one", "+ two"));
        assertEquals(drawJoined(300f, null, "one ", "two"), drawJoined(300f, null, "one ", "+two"));
    }

    @Test
    void aJustifiedRowDoesNotWidenAJoin() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        String content = drawJoined(200f, Alignment.JUSTIFY,
                "one two three", "+,", "four five six seven eight nine ten eleven twelve");
        float[] three = TestSupport.positionOf(content, "three");
        float[] comma = TestSupport.positionOf(content, ",");
        float[] four = TestSupport.positionOf(content, "four");
        assertEquals(three[1], four[1], TestSupport.DELTA);
        assertEquals(three[0] + font.stringWidth("three"), comma[0], TestSupport.DELTA);
        // The spaces are widened: four is further than one space after the comma.
        assertTrue(four[0] > comma[0] + font.stringWidth(", ") + 1f, "the row is justified");
    }
}
