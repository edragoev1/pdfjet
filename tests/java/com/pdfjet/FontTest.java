/*
 * FontTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
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
    @SuppressWarnings("deprecation")    // The CJK fonts are deprecated, and still tested
    void everyCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce() throws Exception {
        Font font = new Font(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
        assertEquals(20f, font.stringWidth(10f, "日本"), 0f);
        assertEquals(10f, font.stringWidth(10f, "𠀋"), 0f);
    }

    // A CJK font does not draw or measure the soft hyphen and the other
    // characters not drawn: each was drawn, and measured, 1 em wide (review of
    // 12 October 2026, R20).
    @Test
    @SuppressWarnings("deprecation")    // The CJK fonts are deprecated, and still tested
    void aCjkFontDrawsNoIgnorableCharacter() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT);
        font.setSize(10f);
        String marked = "日\u00AD本\uFE0F\u200B\u200D\uFEFF";
        assertEquals(20f, font.stringWidth(10f, marked), 0f);
        assertEquals(2, font.getFitChars(marked, 15f));
        assertEquals(marked.length(), font.getFitChars(marked, 20f));
        assertEquals(cjkContent(pdf, font, "日本"), cjkContent(pdf, font, marked));
    }

    private static String cjkContent(PDF pdf, Font font, String text) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, text).setLocation(10f, 10f).drawOn(page);
        return TestSupport.content(page);
    }

    @Test
    void readsATrueTypeFont() throws Exception {
        String path = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
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

    @Test
    void aCoreFontNumberOutsideTheFourteenIsRejected() throws Exception {
        PDF pdf = TestSupport.newPDF();
        assertThrows(IllegalArgumentException.class, () -> new Font(pdf, 0));
        assertThrows(IllegalArgumentException.class, () -> new Font(pdf, 15));
    }

    @Test
    void theLineGapOfAFontSpacesTheLinesOfATextBlock() throws Exception {
        // IBM Plex Sans SC has a line gap of one em.
        String path = "fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.ttf";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        PDF pdf = TestSupport.newPDF();
        Font sc = new Font(pdf, TestSupport.open(path));
        assertEquals(10f, sc.getLineGap(10f), 0.001f);
        assertEquals(0f, new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
                .getLineGap(10f), 0f);
        // The ascent, 8.8, the descent, 1.2, and the line gap, 10, for each line.
        sc.setSize(10f);
        TestSupport.assertXY(500f, 40f, new TextBlock(sc, "日本\n日本").setLocation(0f, 0f).drawOn(null));
    }

    @Test
    void aStreamFontPathOpensTheTrueTypeFont() throws Exception {
        // PDFjet reads .otf and .ttf fonts alone from 9.0.5: a path to a
        // .stream file opens the .ttf file of the same name.
        for (String path : new String[] {
                "fonts/NotoSans/NotoSans-Regular.ttf.stream", "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"}) {
            String want = path.substring(0, path.indexOf('.')) + ".ttf";
            assertEquals(TestSupport.file(want).getPath(), Font.fontFileOf(TestSupport.file(path).getPath()), path);
            assertFalse(new Font(TestSupport.newPDF(), TestSupport.file(path).getPath()).getName().isEmpty(), path);
        }
        // Any other font is refused in PDFjet's words.
        Exception e = assertThrows(Exception.class, () -> new Font(TestSupport.newPDF(),
                new java.io.ByteArrayInputStream("\u000ePlexSans-Bold\u0000\u0000\u0000".getBytes("ISO-8859-1"))));
        assertEquals("Invalid font file: not an OpenType or TrueType font: PDFjet reads .otf and .ttf fonts.", e.getMessage());
    }

    @Test
    void aCoreFontDrawsTheWinAnsiCharactersFrom128To159() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        // ’ is 146 in WinAnsi and 222 units wide, where a space is 278.
        assertEquals(2.22f, font.stringWidth(10f, "\u2019"), 0.001f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Don\u2019t \u20ac5 \u2014 \u201cHi\u201d").setLocation(10f, 20f).drawOn(page);
        assertTrue(TestSupport.content(page).toLowerCase(Locale.ROOT).contains("<446f6e927420803520972093486994>"),
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

    // IBM Plex Sans, read from its .ttf file. Its space is 236 units wide
    // and its .notdef 472, of 1000 units to the em; it has the characters
    // from U+0020 to U+FFFD, and no Thai.
    private static Font ibmPlexSans(PDF pdf) throws Exception {
        assumeTrue(TestSupport.file("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf").exists(), "the fonts directory is not here");
        return new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
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
        String space = String.format("%04X", font.unicodeToGID.get(0x20));
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
                font.unicodeToGID.get('a'), font.unicodeToGID.get(0x20));
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
        pdf.setCompliance(Compliance.PDF_UA_1).setTitle("Test");
        pdf.setTitle("Title");
        Font font = ibmPlexSans(pdf);
        int replacement = font.unicodeToGID.get(0xFFFD);
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

    // A soft hyphen takes no width, as it is drawn only where a line breaks at
    // it, as a hyphen (review of 11 October 2026, #21); the kerning of the
    // characters on either side of it is kept.
    @Test
    void aSoftHyphenHasNoWidthAndANoBreakSpaceIsASpace() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        font.setKernPairs(true);
        assertEquals(font.stringWidth(10f, "AV"), font.stringWidth(10f, "A\u00adV"), 0f, "a soft hyphen");
        Font plex = ibmPlexSans(TestSupport.newPDF());
        assertEquals(plex.stringWidth(10f, "T"), plex.stringWidth(10f, "T\u00ad"), 0f, "a soft hyphen in Plex");
        assertEquals(font.stringWidth(10f, ". "), font.stringWidth(10f, ".\u00a0"), 0f);
        // KPX period space -60
        assertEquals(4.96f, font.stringWidth(10f, ".\u00a0"), 0.001f);
    }

    @Test
    void aFallbackFontDrawsOnlyTheCharactersTheFontHasNoGlyphFor() throws Exception {
        assumeTrue(TestSupport.file("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.ttf").exists(), "the fonts directory is not here");
        PDF pdf = TestSupport.newPDF();
        Font latin = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Font jp = new Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.ttf"));
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
    void twoFontsOfOneNameWithOtherGlyphsAreBothEmbedded() throws Exception {
        // A font and another of its name with other glyphs, as a font and a
        // subset of it made by a font tool are, are two font programs. A PDF
        // embedded the font file of the first of two fonts of one name for
        // both, so the text drawn with the second came out in the glyphs of
        // the first: 中文字 read as Ι㈜♡. The other is Noto Sans with a wider
        // .notdef.
        String path = "fonts/NotoSans/NotoSans-Regular.ttf";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        byte[] ttf = java.nio.file.Files.readAllBytes(TestSupport.file(path).toPath());
        byte[] other = ttf.clone();
        int tables = (ttf[4] & 0xFF) << 8 | (ttf[5] & 0xFF);
        for (int i = 0; i < tables; i++) {
            int entry = 12 + 16 * i;
            if (new String(ttf, entry, 4, "ISO-8859-1").equals("hmtx")) {
                int at = (ttf[entry + 8] & 0xFF) << 24 | (ttf[entry + 9] & 0xFF) << 16 |
                        (ttf[entry + 10] & 0xFF) << 8 | (ttf[entry + 11] & 0xFF);
                int width = ((ttf[at] & 0xFF) << 8 | (ttf[at + 1] & 0xFF)) + 1;
                other[at] = (byte) (width >> 8);
                other[at + 1] = (byte) width;
            }
        }
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        float y = 50f;
        for (byte[] font : new byte[][] {ttf, other}) {
            new TextLine(new Font(pdf, new java.io.ByteArrayInputStream(font)), "A").setLocation(50f, y).drawOn(page);
            y += 20f;
        }
        pdf.complete();
        assertEquals(2, embeddedFonts(bos.toByteArray()));
    }

    @Test
    void oneFontProgramIsEmbeddedOnce() throws Exception {
        // The same font added twice is one font program.
        assumeTrue(TestSupport.file("fonts/IBMPlexSans/IBMPlexSans-Regular.otf").exists(),
                "the fonts directory is not here");
        assertEquals(1, embeddedFonts(documentWithFonts(
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")));
        assertEquals(1, embeddedFonts(documentWithFonts(
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")));
    }

    @Test
    void theFontDescriptorHasTheItalicAngleAndFlagOfThePostTable() throws Exception {
        // IBM's .ttf files of IBM Plex round the angle of their .otf files to a
        // whole degree.
        assumeTrue(TestSupport.file("fonts/IBMPlexSans/IBMPlexSans-Italic.otf").exists(),
                "the fonts directory is not here");
        String[][] fonts = {
            {"fonts/IBMPlexSans/IBMPlexSans-Italic.otf", "-11.31"},
            {"fonts/IBMPlexSans/IBMPlexSans-Italic.ttf", "-11"},
            {"fonts/IBMPlexSerif/IBMPlexSerif-Italic.ttf", "-14"},
            {"fonts/IBMPlexMono/IBMPlexMono-Italic.ttf", "-9"},
            {"fonts/JetBrainsMono/JetBrainsMono-Italic.ttf", "-9"},
            {"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "0"},
        };
        for (String[] font : fonts) {
            String pdf = TestSupport.latin1(documentWithFonts(font[0]));
            assertTrue(pdf.contains("/ItalicAngle " + font[1] + "\n"), font[0]);
            // Nonsymbolic, and Italic for an italic font.
            assertTrue(pdf.contains(font[1].equals("0") ? "/Flags 32\n" : "/Flags 96\n"), font[0]);
        }
        assertEquals("0.05", OpenTypeFont.italicAngleOf(3277));
        assertEquals("0", OpenTypeFont.italicAngleOf(-1));
        assertEquals("-90", OpenTypeFont.italicAngleOf(-90 * 65536));
    }

    @Test
    void aGlyphOfALetterAndASignOfItIsCopiedAsTheLetter() {
        // A font may draw the micro sign with the glyph of mu, as Source Serif 4
        // does, and the ohm, kelvin and angstrom signs with those of omega, K and
        // A with a ring: the glyph is copied as the letter, which Greek text
        // needs, and which Unicode makes the signs. The space still wins over
        // the no-break space, and the right single quotation mark over the
        // modifier letter apostrophe.
        GlyphMap unicodeToGID = new GlyphMap();
        int[][] glyphs = {
            {0x00B5, 0x03BC}, {0x03A9, 0x2126}, {0x004B, 0x212A}, {0x00C5, 0x212B},
            {0x0020, 0x00A0}, {0x02BC, 0x2019},
        };
        for (int gid = 1; gid <= glyphs.length; gid++) {
            for (int c : glyphs[gid - 1]) {
                unicodeToGID.set(c, gid);
            }
        }
        int[] unicodeOf = GlyphMap.unicodeOfGlyphs(unicodeToGID);
        int[] want = {0x03BC, 0x03A9, 0x004B, 0x00C5, 0x0020, 0x2019};
        for (int gid = 1; gid <= want.length; gid++) {
            assertEquals(want[gid - 1], unicodeOf[gid], "glyph " + gid);
        }
    }

    // Returns the characters the ToUnicode maps of the objects give their
    // glyphs, each in hexadecimal, as 06F1.
    private static Set<String> toUnicodeValues(List<PDFobj> objects) {
        Set<String> values = new HashSet<String>();
        Pattern entry = Pattern.compile("<[0-9A-Fa-f]{4}> <([0-9A-Fa-f]+)>");
        for (PDFobj obj : objects) {
            byte[] data = obj.getData();
            if (data == null) {
                continue;
            }
            String cmap = TestSupport.latin1(data);
            if (cmap.startsWith("/CIDInit")) {
                Matcher m = entry.matcher(cmap);
                while (m.find()) {
                    values.add(m.group(1).toUpperCase(Locale.ROOT));
                }
            }
        }
        return values;
    }

    @Test
    void aGlyphOfSeveralCharactersMapsToTheCharacterDrawn() throws Exception {
        // A glyph a font draws for several characters maps to the character
        // the document drew with it, not to the lowest of them: a Persian ۱۳۹۸
        // was copied as the Arabic ١٣٩٨, and the full-width slash of km／h as a
        // division slash (review of 11 October 2026, #8).
        assumeTrue(TestSupport.file("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf").exists(),
                "the fonts directory is not here");
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font arabic = new Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        Font tc = new Font(pdf, TestSupport.open("fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.ttf"));
        new TextLine(arabic, "\u06F1\u06F3\u06F9\u06F8").setLocation(50f, 50f).drawOn(page);
        new TextLine(tc, "60km\uFF0Fh").setLocation(50f, 100f).drawOn(page);
        pdf.complete();
        Set<String> values = toUnicodeValues(TestSupport.read(bos.toByteArray()));
        for (String want : new String[] {"06F1", "06F3", "06F9", "06F8", "FF0F"}) {
            assertTrue(values.contains(want), "no glyph maps to U+" + want);
        }
        for (String other : new String[] {"0661", "0663", "0669", "2215"}) {
            assertFalse(values.contains(other),
                    "a glyph maps to U+" + other + ", which the document did not draw");
        }
    }

    // A document that draws two characters of one glyph gives the second its
    // own as actual text, as the ToUnicode map gives the glyph the first: "١٢٣"
    // and then "۱۲۳" copied the second as "١۲٣", and "∕" then "／" in a stamp
    // gave "∕" twice (review of 12 October 2026, R15).
    @Test
    void theSecondCharacterOfAGlyphHasItsActualText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font arabic = new Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        assertEquals(arabic.unicodeToGID.get(0x0661), arabic.unicodeToGID.get(0x06F1));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(arabic, "\u0661\u0662\u0663").setLocation(50f, 50f).drawOn(page);
        assertFalse(TestSupport.content(page).contains("/ActualText"), TestSupport.content(page));
        new TextLine(arabic, "\u06F1\u06F2\u06F3").setLocation(50f, 100f).drawOn(page);
        String content = TestSupport.content(page);
        for (String c : new String[] {"\u06F1", "\u06F3"}) {
            assertTrue(content.contains("/ActualText <" + Page.toUTF16Hex(c) + ">"), content);
        }

        Font tc = new Font(pdf, TestSupport.open("fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.ttf"));
        assertEquals(tc.unicodeToGID.get(0x2215), tc.unicodeToGID.get(0xFF0F));
        Stamp stamp = new Stamp(pdf);
        stamp.drawText(tc, 12f, 0f, 12f, "1\u22152");
        stamp.drawText(tc, 12f, 0f, 24f, "1\uFF0F2");
        content = stampContent(stamp);
        assertTrue(content.contains("/ActualText <" + Page.toUTF16Hex("\uFF0F") + ">"), content);
        assertFalse(content.contains("/ActualText <" + Page.toUTF16Hex("\u2215") + ">"), content);
    }

    // A stamp gives a zero width non-joiner or joiner its font has no glyph
    // for to the actual text of the glyph before it, as a page does: it was left
    // out (review of 12 October 2026, R34).
    @Test
    void aStampKeepsAJoinerInTheActualText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"));
        assertEquals(0, font.unicodeToGID.get(0x200C));
        Stamp stamp = new Stamp(pdf);
        stamp.drawText(font, 12f, 0f, 12f, "a\u200C\u200Eb");
        String actualText = "/ActualText <" + Page.toUTF16Hex("a\u200C") + ">";
        assertTrue(stampContent(stamp).contains(actualText), stampContent(stamp));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "a\u200C\u200Eb").setLocation(50f, 50f).drawOn(page);
        assertTrue(TestSupport.content(page).contains(actualText), TestSupport.content(page));
    }

    private static String stampContent(Stamp stamp) throws Exception {
        java.lang.reflect.Field buf = Stamp.class.getDeclaredField("buf");
        buf.setAccessible(true);
        return TestSupport.latin1(((ByteArrayOutputStream) buf.get(stamp)).toByteArray());
    }
}
