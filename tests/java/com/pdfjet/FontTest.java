/*
 * FontTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assumptions.assumeTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.nio.ByteBuffer;
import org.junit.jupiter.api.Test;

class FontTest {
    @Test
    void coreFontWidthsComeFromTheAfmMetrics() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        assertEquals("Helvetica", font.getName());
        assertEquals(12f, font.getSize(), 0f);
        // H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
        assertEquals(27.336f, font.stringWidth(12f, "Hello"), 0.001f);
        assertEquals(27.336f, font.stringWidth("Hello"), 0.001f);
        font.setSize(24f);
        assertEquals(54.672f, font.stringWidth("Hello"), 0.001f);
    }

    @Test
    void theWidthOfNoTextIsZero() throws Exception {
        assertEquals(0f, TestSupport.helvetica(TestSupport.newPDF()).stringWidth(null), 0f);
    }

    @Test
    void kerningPairsNarrowTheText() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        assertEquals(16.008f, font.stringWidth(12f, "AV"), 0.001f);
        font.setKernPairs(true);
        // KPX A V -70
        assertEquals(15.168f, font.stringWidth(12f, "AV"), 0.001f);
    }

    @Test
    void coreFontVerticalMetrics() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        assertEquals(11.172f, font.getAscent(12f), 0.001f);
        assertEquals(2.7f, font.getDescent(12f), 0.001f);
        assertEquals(13.872f, font.getBodyHeight(12f), 0.001f);
    }

    @Test
    void getFitCharsCountsTheCharactersThatFit() throws Exception {
        assertEquals(5, TestSupport.helvetica(TestSupport.newPDF()).getFitChars("Hello world", 30f));
    }

    @Test
    void everyCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce() throws Exception {
        Font font = new Font(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
        assertEquals(20f, font.stringWidth(10f, "日本"), 0f);
        assertEquals(10f, font.stringWidth(10f, "𠀋"), 0f);
    }

    @Test
    void readsAStreamFont() throws Exception {
        String path = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        InputStream in = TestSupport.open(path);
        try {
            Font font = new Font(TestSupport.newPDF(), in);
            assertEquals("IBMPlexSans", font.getName());
            assertEquals(28.32f, font.stringWidth(12f, "Hello"), 0.001f);
        } finally {
            in.close();
        }
    }

    // The embedded font file of a PDF that draws a line of text in the font.
    private static byte[] embeddedFontFile(byte[] fontStream) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = new Font(pdf, new ByteArrayInputStream(fontStream));
        new TextLine(font, "Hello").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        for (PDFobj obj : TestSupport.read(bos.toByteArray())) {
            if (obj.getValue("/Subtype").equals("/CIDFontType0C")) {
                return obj.getData();
            }
        }
        return null;
    }

    @Test
    void anOpenTypeStreamFontKeepsItsOtherTablesAndEmbedsOnlyItsCFFData() throws Exception {
        String path = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        InputStream in = TestSupport.open(path);
        byte[] whole;
        try {
            whole = TestSupport.readAll(in);
        } finally {
            in.close();
        }
        // The name, the info and the metrics come first, then 'R' with the
        // length of the other tables of the font, and then the CFF data.
        int i = 1 + (whole[0] & 0xFF);
        i += 3 + (((whole[i] & 0xFF) << 16) | ((whole[i + 1] & 0xFF) << 8) | (whole[i + 2] & 0xFF));
        i += 4 + ByteBuffer.wrap(whole, i, 4).getInt();
        assertEquals('R', whole[i]);
        int length = ByteBuffer.wrap(whole, i + 1, 4).getInt();
        // The same stream without the tables, as streams were written before.
        byte[] cffOnly = new byte[whole.length - 5 - length];
        System.arraycopy(whole, 0, cffOnly, 0, i);
        System.arraycopy(whole, i + 5 + length, cffOnly, i, whole.length - i - 5 - length);
        assertEquals('Y', cffOnly[i]);

        byte[] embedded = embeddedFontFile(whole);
        assertTrue(embedded.length > 0);
        assertArrayEquals(embedded, embeddedFontFile(cffOnly));
    }

    // The content of a page with a line of Thai in the font: po pla, the upper
    // vowel sara ii on it and the tone mark mai ek above the vowel.
    private static String thaiContent(String path) throws Exception {
        PDF pdf = TestSupport.newPDF();
        InputStream in = TestSupport.open(path);
        Font font;
        try {
            font = new Font(pdf, in);
        } finally {
            in.close();
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "\u0E1B\u0E35\u0E48").setLocation(50f, 50f).drawOn(page);
        return TestSupport.content(page);
    }

    @Test
    void aStreamFontPlacesTheMarksAsTheOpenTypeFontDoes() throws Exception {
        assumeTrue(TestSupport.file("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf").exists(), "the fonts directory is not here");
        assertEquals(thaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"), thaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf.stream"));
    }

    @Test
    void aCoreFontNumberOutsideTheFourteenIsRejected() throws Exception {
        PDF pdf = TestSupport.newPDF();
        assertThrows(IllegalArgumentException.class, () -> new Font(pdf, 0));
        assertThrows(IllegalArgumentException.class, () -> new Font(pdf, 15));
    }

    @Test
    void theLineGapOfAFontSpacesTheLinesOfATextBlock() throws Exception {
        String path = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        PDF pdf = TestSupport.newPDF();
        Font jp = new Font(pdf, TestSupport.open(path));
        assertEquals(10f, jp.getLineGap(10f), 0.001f);
        assertEquals(10f, new Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf")).getLineGap(10f), 0.001f);
        assertEquals(0f, new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
                .getLineGap(10f), 0f);
        // The ascent, 8.8, the descent, 1.2, and the line gap, 10, for each line.
        jp.setSize(10f);
        TestSupport.assertXY(500f, 40f, new TextBlock(jp, "日本\n日本").setLocation(0f, 0f).drawOn(null));
    }

    @Test
    void aCoreFontDrawsTheWinAnsiCharactersFrom128To159() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        // ’ is 146 in WinAnsi and 222 units wide, where a space is 278.
        assertEquals(2.22f, font.stringWidth(10f, "\u2019"), 0.001f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Don\u2019t \u20ac5 \u2014 \u201cHi\u201d").setLocation(10f, 20f).drawOn(page);
        assertTrue(TestSupport.content(page).toLowerCase().contains("<446f6e927420803520972093486994>"),
                TestSupport.content(page));
    }

    @Test
    void aCoreFontDrawsDeleteAsASpace() throws Exception {
        // WinAnsi draws a bullet at 127, where the widths of the core fonts
        // have a space: U+007F is a control, drawn as a space as the C1
        // controls are.
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        assertEquals(font.stringWidth(10f, " "), font.stringWidth(10f, "\u007f"), 0f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "a\u007fb\u0085c").setLocation(10f, 20f).drawOn(page);
        assertTrue(TestSupport.content(page).contains("<6120622063>"), TestSupport.content(page));
    }

    // IBM Plex Sans, read from its stream file. Its space is 236 units wide
    // and its .notdef 472, of 1000 units to the em; it has the characters
    // from U+0020 to U+FFFD, and no Thai.
    private static Font ibmPlexSans(PDF pdf) throws Exception {
        assumeTrue(TestSupport.file("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream").exists(), "the fonts directory is not here");
        return new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
    }

    @Test
    void aCharacterTheFontDoesNotHaveIsDrawnWithNotdef() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = ibmPlexSans(pdf);
        // ก, U+0E01, is in the range of the font and not in the font, and 😀,
        // U+1F600, is past its range: both are drawn with .notdef, as wide as
        // it is. A control character is drawn as a space.
        assertEquals(4.72f, font.stringWidth(10f, "ก"), 0.001f);
        assertEquals(4.72f, font.stringWidth(10f, "😀"), 0.001f);
        for (String c : new String[] {"\t", "\u007f", "\u0085"}) {
            assertEquals(2.36f, font.stringWidth(10f, c), 0.001f, String.format("U+%04X", (int) c.charAt(0)));
        }
        // The width of the text that fits is that of the glyphs drawn.
        assertEquals(2, font.setSize(10f).getFitChars("กก", 9.5f));
        // The glyph of a missing character is .notdef, in a span whose actual
        // text is the character, so that a copy of the text has it and not
        // the U+FFFD the ToUnicode map gives .notdef. A control is the space
        // glyph.
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "ก\t😀").setLocation(10f, 20f).drawOn(page);
        String content = TestSupport.content(page);
        String space = String.format("%04X", font.unicodeToGID[0x20]);
        for (String want : new String[] {
                "/Span <</ActualText <FEFF0E01>>> BDC\n<0000> Tj\nEMC\n<" + space + ">",
                "/Span <</ActualText <FEFFD83DDE00>>> BDC\n<0000> Tj\nEMC\n"}) {
            assertTrue(content.contains(want), content);
        }
        // The text a glyph maps to is the character, and a space for a control.
        assertEquals("ก", Page.textOf(font, 0x0E01));
        assertEquals(" ", Page.textOf(font, 0x0085));
    }

    @Test
    void aStampDrawsACharacterTheFontDoesNotHaveWithNotdef() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = ibmPlexSans(pdf);
        new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = new Stamp(pdf).setSize(100f, 50f);
        stamp.drawText(font, 10f, 5f, 20f, "aก\t");
        stamp.complete();
        pdf.complete();
        String want = String.format("<%04X> Tj\n/Span <</ActualText <FEFF0E01>>> BDC\n<0000> Tj\nEMC\n<%04X> Tj\n",
                font.unicodeToGID['a'], font.unicodeToGID[0x20]);
        String out = TestSupport.latin1(bos.toByteArray());
        assertTrue(out.contains(want), out);
    }

    @Test
    void aCompliantDocumentDrawsACharacterTheFontDoesNotHaveWithoutNotdef() throws Exception {
        // PDF/UA and PDF/A forbid .notdef, so a PDF/UA document draws the
        // replacement character of the font, as wide as it is, with the missing
        // character as its actual text, and never glyph 0.
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = ibmPlexSans(pdf);
        int replacement = font.unicodeToGID[0xFFFD];
        assertTrue(replacement != 0, "IBM Plex Sans has no U+FFFD");
        float width = (float) font.glyphAdvance(replacement) * 10f / (float) font.unitsPerEm;
        assertEquals(width, font.stringWidth(10f, "ก"), 0.001f, "a character in the range");
        assertEquals(width, font.stringWidth(10f, "😀"), 0.001f, "a character past the range");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "ก😀").setLocation(10f, 20f).drawOn(page);
        String content = TestSupport.content(page);
        String glyph = String.format("<%04X> Tj\nEMC\n", replacement);
        for (String want : new String[] {
                "/Span <</ActualText <FEFF0E01>>> BDC\n" + glyph,
                "/Span <</ActualText <FEFFD83DDE00>>> BDC\n" + glyph}) {
            assertTrue(content.contains(want), content);
        }
        assertTrue(!content.contains("<0000>"), ".notdef is drawn: " + content);
        // The stamp draws it so too.
        Stamp stamp = new Stamp(pdf).setSize(100f, 50f);
        stamp.drawText(font, 10f, 5f, 20f, "ก");
        stamp.complete();
        pdf.complete();
        String out = TestSupport.latin1(bos.toByteArray());
        assertTrue(out.contains("/Span <</ActualText <FEFF0E01>>> BDC\n" + glyph), out);
        // The character still has no glyph, so a fallback font draws it.
        assertTrue(!font.hasGlyph(0x0E01), "a missing character has a glyph");
    }

    @Test
    void aControlCharacterStaysInTheFontOfItsText() throws Exception {
        // A control character is drawn as a space by the font, not by the
        // fallback font: the fallback font would draw it as a space too.
        PDF pdf = TestSupport.newPDF();
        Font latin = ibmPlexSans(pdf);
        Font helvetica = TestSupport.helvetica(pdf);
        for (Font font : new Font[] {latin, helvetica}) {
            for (int c : new int[] {'\t', 0x7F, 0x85}) {
                assertTrue(font.hasGlyph(c), font.name + String.format(" has no glyph for U+%04X", c));
            }
        }
        assertTrue(!latin.hasGlyph(0x0E01) && !latin.hasGlyph(0x1F600), "a character drawn with .notdef has a glyph");
    }

    @Test
    void aSoftHyphenIsAHyphenAndANoBreakSpaceIsASpace() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        font.setKernPairs(true);
        assertEquals(font.stringWidth(10f, "T-"), font.stringWidth(10f, "T\u00ad"), 0f);
        assertEquals(font.stringWidth(10f, ". "), font.stringWidth(10f, ".\u00a0"), 0f);
        // KPX period space -60
        assertEquals(4.96f, font.stringWidth(10f, ".\u00a0"), 0.001f);
    }

    @Test
    void aFallbackFontDrawsOnlyTheCharactersTheFontHasNoGlyphFor() throws Exception {
        assumeTrue(TestSupport.file("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream").exists(), "the fonts directory is not here");
        PDF pdf = TestSupport.newPDF();
        Font latin = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Font jp = new Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"));
        Font helvetica = TestSupport.helvetica(pdf);
        // The Latin letters after the Japanese ones are in the font again.
        assertEquals(latin.stringWidth(10f, "abc") + jp.stringWidth(10f, "\u65e5\u672c") + latin.stringWidth(10f, "def"),
                latin.stringWidth(jp, 10f, "abc\u65e5\u672cdef"), 0.001f);
        // A core font has a fallback font too.
        assertEquals(helvetica.stringWidth(10f, "Tokyo ") + jp.stringWidth(10f, "\u6771\u4eac"),
                helvetica.stringWidth(jp, 10f, "Tokyo \u6771\u4eac"), 0.001f);
        // A character that neither font has stays in the font.
        assertEquals(latin.stringWidth(10f, "x\u0e01y"), latin.stringWidth(jp, 10f, "x\u0e01y"), 0f);
        // A combining mark stays with the character before it, in one run of one font.
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(latin, "\u65e5\u0301").setFallbackFont(jp).setLocation(10f, 20f).drawOn(page);
        assertEquals(1, TestSupport.content(page).split(" Tf\n").length - 1, TestSupport.content(page));
    }
    // The number of font programs the PDF embeds, and its bytes.
    private static int embeddedFonts(byte[] pdf) {
        return TestSupport.latin1(pdf).split("/FontFile").length - 1;
    }

    // Draws a character with each font of a PDF made from the font files.
    private static byte[] documentWithFonts(String... paths) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        float y = 50f;
        for (String path : paths) {
            Font font = new Font(pdf, TestSupport.open(path)).setSize(12f);
            new TextLine(font, "\u4e2d").setLocation(50f, y).drawOn(page);
            y += 20f;
        }
        pdf.complete();
        return bos.toByteArray();
    }

    @Test
    void aFontAndASubsetOfItWithTheSameNameAreBothEmbedded() throws Exception {
        // PDFjet ships subsets of the Noto CJK fonts whose name inside is the
        // name of the whole font. A PDF embedded the font file of the first
        // of two fonts of one name for both, so the text drawn with the
        // second came out in the glyphs of the first: 中文字 read as Ι㈜♡.
        assumeTrue(TestSupport.file("fonts/NotoSansSC/NotoSansSC-Regular.ttf").exists(),
                "the fonts directory is not here");
        assertEquals(2, embeddedFonts(documentWithFonts(
                "fonts/NotoSansSC/NotoSansSC-Regular.ttf",
                "fonts/NotoSansSC/NotoSansSC-Regular-SC3500.ttf")));
        assertEquals(2, embeddedFonts(documentWithFonts(
                "fonts/NotoSansSC/NotoSansSC-Regular.ttf.stream",
                "fonts/NotoSansSC/NotoSansSC-Regular-SC3500.ttf.stream")));
    }

    @Test
    void oneFontProgramIsEmbeddedOnce() throws Exception {
        // The same font added twice, and the same font read from a .ttf and
        // from the .stream file made of it, are one font program: Example_28
        // draws with both and embeds one of each font.
        assumeTrue(TestSupport.file("fonts/IBMPlexSans/IBMPlexSans-Regular.otf").exists(),
                "the fonts directory is not here");
        assertEquals(1, embeddedFonts(documentWithFonts(
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")));
        assertEquals(1, embeddedFonts(documentWithFonts(
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")));
    }
}
