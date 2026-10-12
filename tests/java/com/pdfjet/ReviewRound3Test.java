/*
 * ReviewRound3Test.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;

/** The findings of the third review, of 12 October 2026, as tests. */
class ReviewRound3Test {
    /**
     * A text rendering mode set back to 0 is written: Tr lasts past ET, and
     * the text after an invisible line stayed invisible (F1).
     */
    @Test
    void theTextRenderingModeIsSetBackToZero() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "plain").setLocation(50f, 50f).drawOn(page);
        assertFalse(TestSupport.content(page).contains(" Tr\n"));
        page.setTextRenderingMode(3);
        new TextLine(font, "hidden").setLocation(50f, 100f).drawOn(page);
        page.setTextRenderingMode(0);
        new TextLine(font, "VISIBLE AGAIN").setLocation(50f, 150f).drawOn(page);
        String content = TestSupport.content(page);
        String before = content.substring(0, content.indexOf(TestSupport.hex("VISIBLE AGAIN")));
        int zero = before.lastIndexOf("0 Tr\n");
        assertTrue(zero != -1 && before.lastIndexOf("3 Tr\n") < zero, content);
    }

    /**
     * A text frame at NaN, or a Markdown text with NaN margins, is refused:
     * no word was placed, and pages were made until the memory ran out (F4).
     */
    @Test
    void aNaNLocationOrMarginIsRefused() throws Exception {
        float[][] cases = {
            {Float.NaN, 50f, 200f, 0f}, {50f, Float.NaN, 200f, 0f},
            {50f, 50f, Float.NaN, 0f}, {50f, 50f, 200f, Float.POSITIVE_INFINITY}};
        for (float[] xywh : cases) {
            final PDF pdf = TestSupport.newPDF();
            final TextFrame frame = new TextFrame(TestSupport.helvetica(pdf), Arrays.asList("Hello frame text"))
                    .setWidth(xywh[2]).setHeight(xywh[3]);
            frame.setLocation(xywh[0], xywh[1]);
            final List<Page> pages = new ArrayList<Page>();
            Exception e = assertThrows(IllegalArgumentException.class, () -> frame.drawOn(pdf, pages, Letter.PORTRAIT));
            assertEquals("The text frame's location and size must be numbers, not NaN or infinite.", e.getMessage());
            assertEquals(0, pages.size());
        }
        for (float margin : new float[] {Float.NaN, Float.POSITIVE_INFINITY}) {
            final PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            final Markdown markdown = new Markdown(font, font, font, font, font).setMargins(margin, 72f, 72f, 72f);
            final List<Page> pages = new ArrayList<Page>();
            Exception e = assertThrows(IllegalArgumentException.class,
                    () -> markdown.drawOn(pdf, "Some *text*.", pages, Letter.PORTRAIT));
            assertEquals("The margins of the Markdown must be numbers, not NaN or infinite.", e.getMessage());
            assertEquals(0, pages.size());
        }
    }

    /**
     * The rows of Markdown tables past 100,000 cells are counted, not kept:
     * each row was padded to the header's 64 columns first, and 2 MB of rows
     * of "|" made 1.65 GB of cells before the document was refused (C2).
     */
    @Test
    void theRowsPastTheTableLimitAreNotKept() throws Exception {
        StringBuilder text = new StringBuilder("|");
        for (int i = 0; i < 64; i++) {
            text.append(" a |");
        }
        text.append("\n|");
        for (int i = 0; i < 64; i++) {
            text.append("---|");
        }
        text.append("\n");
        for (int i = 0; i < 3000; i++) {
            text.append("|\n");
        }
        List<MarkdownParser.Block> blocks = MarkdownParser.parse(text.toString());
        assertTrue(blocks.get(0).rows.size() <= 100000 / 64 + 1, blocks.get(0).rows.size() + " rows kept");
        assertEquals(64 * 3001, MarkdownParser.tableCells(blocks));
        final PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        final Markdown markdown = new Markdown(font, font, font, font, font);
        final String markdownText = text.toString();
        Exception e = assertThrows(IllegalArgumentException.class,
                () -> markdown.drawOn(pdf, markdownText, new ArrayList<Page>(), Letter.PORTRAIT));
        assertEquals("The tables of the Markdown have more than 100000 cells: draw a table that large with BigTable.",
                e.getMessage());
    }
}
