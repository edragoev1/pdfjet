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

    /**
     * Two characters copied as the same text, as a letter and its
     * presentation form, need no actual text for the second: Arabic text had
     * three times the spans it needed, its content 27% larger (T2).
     */
    @Test
    void aCharacterCopiedAsTheFirstHasNoActualText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        assertEquals(font.unicodeToGID.get(0x0644), font.unicodeToGID.get(0xFEDD));
        font.useChar(0xFEDD, font.unicodeToGID.get(0xFEDD));
        assertFalse(font.drawnAsAnother(0x0644));
        font.useChar(0x0661, font.unicodeToGID.get(0x0661));
        assertTrue(font.drawnAsAnother(0x06F1));
    }

    /**
     * A glyph drawn for another character stays in the span of a word with
     * moved marks, whose actual text gives the character: in a span of its
     * own, the word's span began with a moved mark and the space that stands
     * for it, and "ةِمى" was copied as "ىمِ ة" (T1).
     */
    @Test
    void aWordWithMarksIsCopiedWithoutASpaceInside() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "\u0628").setLocation(50f, 50f).drawOn(page);
        // The glyph of ة as drawn first for another character, which no two
        // characters of this font copied as different text are
        font.program.chars.put(font.unicodeToGID.get(0x0629), (int) 'X');
        new TextLine(font, "\u0629\u0650\u0645\u0649").setLocation(50f, 100f).drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains("/ActualText <FEFF0020"), content);
        assertTrue(content.contains("/ActualText <" + Page.toUTF16Hex("\u0629\u0650\u0645\u0649") + ">"), content);
    }

    /**
     * A cell's word broken between its characters breaks again at a soft
     * hyphen, as TextBlock and TextFrame do: "1234<SHY>5678" was
     * 123 / 4<SHY>56 / 78 (T4).
     */
    @Test
    void aCellBreaksAtASoftHyphenAfterACharacterBreak() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        java.lang.reflect.Method wrapCellText = Table.class.getDeclaredMethod("wrapCellText", List.class, int.class);
        wrapCellText.setAccessible(true);
        Cell cell = new Cell(font, "1234\u00AD5678");
        cell.setWidth(font.stringWidth("123"));
        cell.setLeftPadding(0f);
        cell.setRightPadding(0f);
        List<String> lines = new ArrayList<String>();
        @SuppressWarnings("unchecked")
        List<String> wrapped = (List<String>) wrapCellText.invoke(new Table(), Arrays.asList(cell), 0);
        for (String line : wrapped) {
            lines.add(line.replace("\u00AD", ""));
        }
        assertEquals(Arrays.asList("123", "4-", "567", "8"), lines);
        Cell wide = new Cell(font, "W");
        wide.setWidth(1f);
        assertEquals(Arrays.asList("W"), wrapCellText.invoke(new Table(), Arrays.asList(wide), 0));
    }

    /**
     * A CJK font fits all of a text in a width of more characters than it
     * has, as 1e30 or an infinity, in every port: Go and C# fit none (T6).
     */
    @Test
    @SuppressWarnings("deprecation")    // The CJK fonts are deprecated, and still tested
    void aCJKFontFitsAllOfATextInAHugeWidth() throws Exception {
        Font font = new Font(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
        String text = "\u4E2D\u6587\u5B57";
        for (float width : new float[] {1e30f, Float.POSITIVE_INFINITY, Float.MAX_VALUE}) {
            assertEquals(3, font.getFitChars(text, width), "width " + width);
        }
        for (float width : new float[] {Float.NaN, -1f, 0f}) {
            assertEquals(0, font.getFitChars(text, width), "width " + width);
        }
        assertEquals(2, font.getFitChars(text, 2f * font.getSize()));
    }

    /**
     * A zero width joiner the font has no glyph for, in left to right text
     * between two LRMs, is in the actual text of the run, as the stamp keeps
     * it: it was left out (T7).
     */
    @Test
    void aJoinerInARunIsInItsActualText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/NotoSansJP/NotoSansJP-Regular.ttf"));
        assertEquals(0, font.unicodeToGID.get(0x200D));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "d\u200D\u200E\u200Ce\u200D").setLocation(50f, 50f).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("/ActualText <" + Page.toUTF16Hex("\u200Ee\u200D\u200E") + ">"), content);
    }
}
