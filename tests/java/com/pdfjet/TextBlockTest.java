/*
 * TextBlockTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class TextBlockTest {
    private static final String TEN_WORDS = "one two three four five six seven eight nine ten";

    @Test
    void aNewlineIsOneEmptyLine() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        float[] x = new TextBlock(font, "x").setLocation(0f, 0f).drawOn(null);
        TestSupport.assertXY(500f, 13.872f, x);
        TestSupport.assertXY(x[0], x[1], new TextBlock(font, "\n").setLocation(0f, 0f).drawOn(null));
        TestSupport.assertXY(x[0], x[1], new TextBlock(font, "").setLocation(0f, 0f).drawOn(null));
    }

    @Test
    void withoutAHeightTheBlockIsAsTallAsItsText() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        TextBlock block = new TextBlock(font, TEN_WORDS).setLocation(0f, 0f).setWidth(60f);
        // Six lines of 13.872 points.
        TestSupport.assertXY(60f, 83.232f, block.drawOn(null));
        assertEquals(83.232f, block.getHeight(), TestSupport.DELTA);
        assertEquals(60f, block.getWidth(), 0f);
    }

    @Test
    void aHeightCutsTheTextThatDoesNotFitAndAlignsTheRest() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        TextBlock block = new TextBlock(font, TEN_WORDS).setLocation(0f, 0f).setSize(60f, 30f);
        // Two of the six lines fit
        TestSupport.assertXY(60f, 30f, block.drawOn(null));
        assertEquals(30f, block.getHeight(), 0f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        block.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("...")), content);
        assertFalse(content.contains(TestSupport.hex("ten")), content);

        // Aligned to the bottom of a block 10 points taller than its two lines,
        // the text sits where a block without a height draws it 10 points lower
        page = new Page(pdf, Letter.PORTRAIT);
        block.setSize(60f, 2 * 13.872f + 10f).setVerticalAlignment(Alignment.BOTTOM).drawOn(page);
        String bottom = TestSupport.content(page);
        page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "one two three four").setLocation(0f, 10f).setWidth(60f).drawOn(page);
        String lower = TestSupport.content(page);
        String textMatrix = lower.substring(lower.indexOf("1 0 0 1 0 "), lower.indexOf(" Tm\n") + 4);
        assertTrue(bottom.contains(textMatrix), textMatrix + " missing from " + bottom);
    }

    @Test
    void strikeoutDrawsALineThroughEachLine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "one\ntwo").setLocation(0f, 0f).setStrikeout(true).drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(2, content.split(" l\n").length - 1, content);
    }

    @Test
    void drawOnAPageWritesEveryWord() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] xy = new TextBlock(TestSupport.helvetica(pdf), TEN_WORDS).setLocation(0f, 0f).setWidth(60f).drawOn(page);
        TestSupport.assertXY(60f, 83.232f, xy);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("one")), content);
        assertTrue(content.contains(TestSupport.hex("ten")), content);
    }

    @Test
    void transparentLeavesTheTextColorUnchanged() throws Exception {
        TextBlock block = new TextBlock(TestSupport.helvetica(TestSupport.newPDF()), "x");
        block.setTextColor(Color.blue).setTextColor(Color.transparent);
        TestSupport.assertRGB(0f, 0f, 1f, block.getTextColor());
    }

    @Test
    void setFontChangesTheFallbackFontUnlessAnotherWasSet() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font helvetica = TestSupport.helvetica(pdf);
        Font courier = new Font(pdf, CoreFont.COURIER);
        TextBlock block = new TextBlock(helvetica, "x").setFont(courier);
        assertSame(courier, block.fallbackFont);
        block.setFallbackFont(helvetica).setFont(new Font(pdf, CoreFont.TIMES_ROMAN));
        assertSame(helvetica, block.fallbackFont);
    }
}
