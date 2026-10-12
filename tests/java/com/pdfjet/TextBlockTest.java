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

import java.util.ArrayList;
import java.util.List;

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
    void aWrappedLineIsDrawnWithTheSpaceItWasWrappedAt() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, TEN_WORDS).setLocation(0f, 0f).setWidth(60f).drawOn(page);
        String content = TestSupport.content(page);
        // Six lines: after each of the first five, the space it was wrapped at,
        // so that a screen reader and a text extractor see the word break; the
        // last line has none.
        String space = "[<" + TestSupport.hex(" ") + ">] TJ\n";
        assertEquals(5, (content.length() - content.replace(space, "").length()) / space.length(), content);
        assertTrue(content.contains("[<" + TestSupport.hex("ten") + ">] TJ\nET"), content);
        // Drawn after the line, not in its text: the line is measured without it
        assertTrue(content.contains("[<" + TestSupport.hex("one two") + ">] TJ\n"), content);
    }

    @Test
    void paddingWiderThanTheBlockDrawsOneCharacterALine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        // The text area was negative, and the word was broken past its end.
        TextBlock block = new TextBlock(font, "Hello");
        block.setLocation(50f, 50f).setWidth(100f).setPadding(60f).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("H")));
        assertTrue(content.contains(TestSupport.hex("o")));
    }

    @Test
    void theUnderlineIsDrawnInTheTextColorAtTheFontThickness() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setPenColor(Color.red);     // the underline took the pen color
        TextBlock block = new TextBlock(font, "Hello");
        block.setLocation(50f, 50f).setTextColor(Color.blue);
        block.setUnderline(true);
        block.setBorderWidth(3f);        // and the border width
        block.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("0 0 1 RG"));
        assertFalse(content.contains("3 w"));
        assertTrue(content.contains(font.getUnderlineThickness(font.getSize()) + " w"));
    }

    @Test
    void theCornerRadiusRoundsABackgroundWithoutABorder() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock block = new TextBlock(font, "Hello");
        block.setLocation(50f, 50f).setBackgroundColor(Color.yellow);
        block.setCornerRadius(10f);
        block.drawOn(page);
        // A rounded rectangle draws its corners with the curve operator.
        assertTrue(TestSupport.content(page).contains(" c\n"));
    }

    @Test
    void aNullTextColorLeavesTheTextColorUnchanged() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setBrushColor(Color.red);
        TextBlock block = new TextBlock(font, "Hi");
        block.setLocation(50f, 50f);
        block.setTextColor((float[]) null);
        block.drawOn(page);
        TestSupport.assertRGB(0f, 0f, 0f, block.getTextColor());
        assertTrue(TestSupport.content(page).contains("0 0 0 rg"));
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
    @Test
    void isTaggedAsItsStructureType() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock heading = new TextBlock(font, "Invoice").setStructureType(StructElem.H1);
        heading.setLocation(50f, 50f);
        heading.drawOn(page);
        TextBlock paragraph = new TextBlock(font, "Thank you for your order.");
        paragraph.setLocation(50f, 100f);
        paragraph.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("/H1 <</MCID 0>>") && content.contains("/P <</MCID 1>>"), content);
    }


    @Test
    void aWordIsBrokenBetweenItsCharactersAsTheGoPortBreaksIt() throws Exception {
        // Five lines: pneumono, ultramicros, copicsilico, volcanoco, niosis
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        TextBlock block = new TextBlock(font, "pneumonoultramicroscopicsilicovolcanoconiosis").setWidth(60f);
        TestSupport.assertXY(60f, 69.36f, block.setLocation(0f, 0f).drawOn(null));
    }

    @Test
    void aLongWordIsBrokenInTimeThatGrowsWithTheWord() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        StringBuilder word = new StringBuilder();
        for (int i = 0; i < 10000; i++) {
            word.append("abcdefghij");     // 100,000 characters
        }
        TextBlock block = new TextBlock(font, word.toString()).setWidth(100f);
        final float[][] xy = new float[1][];
        long took = TestSupport.shortestMillis(3000L, () -> xy[0] = block.setLocation(0f, 0f).drawOn(null));
        assertTrue(took < 3000L, "breaking a word of 100,000 characters took " + took + " ms");
        TestSupport.assertXY(100f, 78030f, xy[0]);     // 5625 lines of 13.872 points
    }

    @Test
    void aLongRightToLeftWordIsBrokenInTimeThatGrowsWithTheWord() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"));
        StringBuilder word = new StringBuilder();
        for (int i = 0; i < 25000; i++) {
            word.append("محمد");     // 100,000 letters, which took about 20 minutes
        }
        TextBlock block = new TextBlock(font, word.toString()).setRightToLeft(true).setWidth(100f);
        final float[][] xy = new float[1][];
        long took = TestSupport.shortestMillis(3000L, () -> xy[0] = block.setLocation(0f, 0f).drawOn(null));
        assertTrue(took < 3000L, "breaking a right to left word of 100,000 letters took " + took + " ms");
        TestSupport.assertXY(100f, 120006f, xy[0]);    // As the Go port breaks it
    }

    // A right to left text cut to its height keeps the start of its last line,
    // at the right as it is drawn, and its "..." at the left, where the line
    // ends in the reading (review of 11 October 2026, #7).
    @Test
    void aRightToLeftTextCutToItsHeightKeepsTheStartOfItsLastLine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"));
        TextBlock block = new TextBlock(font, "שלום עולם אחד שתיים שלוש ארבע חמש שש שבע שמונה").setRightToLeft(true);
        block.setSize(80f, 20f);
        java.lang.reflect.Method getTextLines = TextBlock.class.getDeclaredMethod("getTextLines");
        getTextLines.setAccessible(true);
        java.lang.reflect.Method linesThatFit =
                TextBlock.class.getDeclaredMethod("linesThatFit", TextLine[].class, float.class);
        linesThatFit.setAccessible(true);
        TextLine[] lines = (TextLine[]) getTextLines.invoke(block);
        float fontSize = 12f;   // The size of a new TextBlock
        float leading = font.getAscent(fontSize) + font.getDescent(fontSize) + font.getLineGap(fontSize);
        TextLine[] fit = (TextLine[]) linesThatFit.invoke(block, lines, leading);
        assertTrue(fit.length < lines.length, "the text is not cut: " + fit.length + " lines of " + lines.length);
        String last = lines[fit.length - 1].text;
        String got = fit[fit.length - 1].text;
        assertTrue(got.startsWith("...") && got.endsWith(last.substring(last.offsetByCodePoints(0, 3))),
                "the last line \"" + got + "\", cut from \"" + last + "\", has lost its start");
    }

    // A character Unicode does not draw without a glyph, as the variation
    // selector after ™ in text pasted from a phone, a zero width space or a
    // word joiner, takes no width and draws nothing, where it was a box with a
    // width (review of 11 October 2026, #9).
    @Test
    void anIgnorableCharacterIsNotDrawn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        String plain = "Brand\u2122 \u00A9 2026 word";
        String marked = "Brand\u2122\uFE0F \u00A9\uFE0F 2026\u200B wo\u2060rd";
        assertEquals(font.stringWidth(12f, plain), font.stringWidth(12f, marked), 0f,
                "the text is as wide with the marks as without");
        assertEquals(draw(pdf, font, plain), draw(pdf, font, marked), "the marks draw something");
        // A supplementary one, a tag character, is not drawn either.
        assertEquals(draw(pdf, font, plain), draw(pdf, font, plain + "\uDB40\uDC01"));
    }

    // A core font does not draw or measure them either, nor the marks of
    // direction, the joiners and the byte order mark: they were spaces, so that
    // "©️ 2026" had two (review of 12 October 2026, R14). getFitChars kerns with
    // the next character drawn, not with a soft hyphen, as stringWidth does (R19).
    @Test
    void aCoreFontDrawsNoIgnorableCharacter() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        font.setSize(12f);
        String plain = "ab \u00A9 2026 word";
        String marked = "a\uFE0Fb \u00A9\uFE0F 2026\u200B wo\u2060r\u200Dd\u200E\u200C\u200F\uFEFF\uDB40\uDC01";
        assertEquals(font.stringWidth(12f, plain), font.stringWidth(12f, marked), 0f,
                "the text is as wide with the marks as without");
        assertEquals(draw(pdf, font, plain), draw(pdf, font, marked), "the marks draw something");
        // A hair over the width, as float rounds it
        font.setKernPairs(true);
        float width = font.stringWidth(12f, "AVAV") + 0.001f;
        assertEquals(6, font.getFitChars("A\u00ADVA\u00ADV", width));
    }

    private static String draw(PDF pdf, Font font, String text) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine line = new TextLine(font, text);
        line.setLocation(10f, 10f);
        line.drawOn(page);
        return TestSupport.content(page);
    }

    // A Stamp leaves out the marks of direction and the joiners its font has
    // no glyph for, as Page does: Hebrew with brackets, reordered for drawing,
    // wrote two .notdef glyphs (review of 11 October 2026, #10).
    @Test
    void aStampLeavesOutTheMarksOfDirection() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"));
        String text = Bidi.reorderVisually("שלום (abc) עולם");
        assertTrue(text.indexOf('\u200E') != -1 || text.indexOf('\u200F') != -1,
                "the text has no marks of direction: " + text);
        Stamp stamp = new Stamp(pdf).setSize(100f, 50f);
        stamp.drawText(font, 12f, 0f, 12f, text);
        java.lang.reflect.Field buf = Stamp.class.getDeclaredField("buf");
        buf.setAccessible(true);
        String content = TestSupport.latin1(((java.io.ByteArrayOutputStream) buf.get(stamp)).toByteArray());
        // A mark drawn as a glyph the font lacks is in a span of its own, after
        // the first run of glyphs; this text has no character the font lacks
        assertFalse(content.contains("/ActualText"), "the stamp draws a mark as a glyph the font lacks: " + content);
        String glyphs = content.substring(content.indexOf('<') + 1, content.indexOf('>'));
        for (int i = 0; i + 4 <= glyphs.length(); i += 4) {
            assertFalse(glyphs.substring(i, i + 4).equals("0000"), "the stamp draws .notdef: " + content);
        }
    }

    // SOFT_HYPHEN_WORD is a word of soft hyphens, and softHyphenWidth a width
    // that fits its start up to the first of them that a line can break at
    // past "Donau", with the hyphen, and no more.
    static final String SOFT_HYPHEN_WORD = "Donau\u00addampf\u00adschiff\u00adfahrt";

    static float softHyphenWidth(Font font) {
        return font.stringWidth(font.getSize(), "Donaudampf-") + 1f;
    }

    private static TextLine[] textLines(TextBlock block) throws Exception {
        java.lang.reflect.Method getTextLines = TextBlock.class.getDeclaredMethod("getTextLines");
        getTextLines.setAccessible(true);
        return (TextLine[]) getTextLines.invoke(block);
    }

    // A word too wide for its line breaks at a soft hyphen, the line ending
    // with a hyphen; it was drawn as a hyphen wherever it was, and broken
    // between its characters (review of 11 October 2026, #21).
    @Test
    void breaksAWordAtASoftHyphen() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        TextBlock block = new TextBlock(font, "x " + SOFT_HYPHEN_WORD);
        block.setWidth(softHyphenWidth(font) + 2 * block.getPadding());
        List<String> lines = new ArrayList<>();
        for (TextLine line : textLines(block)) {
            lines.add(line.text.replace("\u00ad", ""));
        }
        assertTrue(lines.size() >= 2 && lines.get(0).equals("x Donau-"), "the lines " + lines);
        for (String line : lines.subList(0, lines.size() - 1)) {
            assertTrue(line.endsWith("-"), "the lines " + lines + ": " + line + " is not broken at a soft hyphen");
        }
        assertEquals("x Donaudampfschifffahrt", String.join("", lines).replace("-", ""), "the text");
        // A word that fits draws no hyphen.
        TextLine[] fits = textLines(new TextBlock(font, SOFT_HYPHEN_WORD).setWidth(500f));
        assertTrue(fits.length == 1 && !fits[0].text.contains("-"), "the lines of a word that fits: " + fits.length);
    }

    // A word broken between its characters breaks at a soft hyphen where one
    // fits, as TextFrame breaks each row: "1234\u00ad5678" was "12", "34", "56",
    // "78" with no hyphen, where TextFrame draws "34-", and a line ran across
    // the soft hyphen of "aaaaaaaaaa\u00adbbbbbbbbbb" where "aa-" fit (review of
    // 12 October 2026, R18).
    @Test
    void aWordBrokenBetweenCharactersBreaksAtASoftHyphen() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        // Two digits and a hyphen fit, three digits do not.
        assertEquals("12|34-|56|78", lineTexts(font, "1234\u00AD5678",
                font.stringWidth(font.getSize(), "00-") + 1f, false));
        assertEquals("aaaa|aaaa|aa-|bbbb|bbbb|bb", lineTexts(font, "aaaaaaaaaa\u00ADbbbbbbbbbb",
                font.stringWidth(font.getSize(), "aaaa") + 1f, false));
    }

    // A right to left text block breaks a word at a soft hyphen, the hyphen at
    // the end of the line, on its left: it was cut between two characters, with
    // no hyphen (review of 12 October 2026, R17).
    @Test
    void aRightToLeftWordBreaksAtASoftHyphen() throws Exception {
        Font font = new Font(TestSupport.newPDF(), TestSupport.open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"));
        font.setSize(12f);
        float width = font.stringWidth(12f, "\u05E9\u05DC\u05D5\u05DD-") + 1f;
        String word = "\u05E9\u05DC\u05D5\u05DD\u00AD\u05E2\u05D5\u05DC\u05DD\u00AD\u05D0\u05D7\u05D3";
        String lines = "-\u05DD\u05D5\u05DC\u05E9|-\u05DD\u05DC\u05D5\u05E2|\u05D3\u05D7\u05D0";
        assertEquals(lines, lineTexts(font, word, width, true));
        assertEquals("\u05D0|" + lines, lineTexts(font, "\u05D0 " + word, width, true));
    }

    // Returns the lines of a text block of the text, at the width of text, joined by "|".
    private static String lineTexts(Font font, String text, float width, boolean rightToLeft) throws Exception {
        TextBlock block = new TextBlock(font, text);
        block.setRightToLeft(rightToLeft);
        block.setWidth(width + 2 * block.getPadding());
        List<String> lines = new ArrayList<>();
        for (TextLine line : textLines(block)) {
            lines.add(line.text.replace("\u00AD", ""));
        }
        return String.join("|", lines);
    }
}
