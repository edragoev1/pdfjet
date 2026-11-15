/*
 * DrawGlyphsTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.zip.Inflater;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.junit.jupiter.api.Test;

// drawGlyphs draws glyphs another engine shaped, by their numbers, as
// outlines read from the font file: paths, no text and no font in the
// document; a glyph drawn more than once a form XObject placed wherever it
// is; in a tagged document marked content with the text as its actual text.
class DrawGlyphsTest {

    private static final String DEVANAGARI = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf";
    private static final String CASCADIA = "tests/fonts/CascadiaMono-Regular.ttf";

    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i >= 0; i = text.indexOf(part, i + 1)) {
            n++;
        }
        return n;
    }

    private static String document(ByteArrayOutputStream bos) {
        return TestSupport.latin1(bos.toByteArray());
    }

    // The content of each form XObject of the document, inflated.
    private static List<String> forms(byte[] doc) throws Exception {
        List<String> forms = new ArrayList<String>();
        String text = TestSupport.latin1(doc);
        for (int i = text.indexOf("/Subtype /Form"); i >= 0; i = text.indexOf("/Subtype /Form", i + 1)) {
            int start = text.indexOf("stream\n", i) + 7;
            int end = text.indexOf("\nendstream", start);
            Inflater inflater = new Inflater();
            inflater.setInput(Arrays.copyOfRange(doc, start, end));
            byte[] out = new byte[1 << 16];
            int n = inflater.inflate(out);
            inflater.end();
            forms.add(TestSupport.latin1(Arrays.copyOf(out, n)));
        }
        return forms;
    }

    @Test
    void theGlyphsAreOutlinesAndNoFontIsWritten() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.plexSans(pdf);
        font.setSize(12f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        int b = font.unicodeToGID.get('B');
        page.drawGlyphs(font, new int[] {a, b}, new float[] {50f, 100f, 60f, 100f}, "AB", "en");
        String content = TestSupport.content(page);
        // Each placed by the matrix of the em at its place
        assertTrue(content.contains("q 0.012 0 0 0.012 50 692 cm /Fm"), content);
        assertTrue(content.contains("q 0.012 0 0 0.012 60 692 cm /Fm"), content);
        assertFalse(content.contains("BT"), content);
        assertFalse(content.contains("Tf"), content);
        assertFalse(content.contains("BDC"), content);  // Untagged: just the shapes
        pdf.complete();
        String doc = document(bos);
        assertFalse(doc.contains("/FontFile"), "no font program");
        assertFalse(doc.contains("/Type /Font"), "no font");
        assertFalse(doc.contains("/ToUnicode"), "no ToUnicode map");
        // The outlines: paths in the font's units, filled, no text
        List<String> forms = forms(bos.toByteArray());
        assertEquals(2, forms.size());
        for (String form : forms) {
            assertTrue(form.contains(" m\n") && form.contains(" l\n") && form.endsWith("f\n"), form);
            assertFalse(form.contains("BT") || form.contains("Tj"), form);
        }
    }

    @Test
    void aGlyphDrawnTwiceIsOneStampPlacedTwice() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.plexSans(pdf);
        font.setSize(10f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        int b = font.unicodeToGID.get('B');
        int space = font.unicodeToGID.get(' ');
        page.drawGlyphs(font, new int[] {a, space, b, a},
                new float[] {50f, 100f, 60f, 100f, 65f, 100f, 70f, 100f}, "A BA", null);
        String content = TestSupport.content(page);
        Matcher m = Pattern.compile("q 0.01 0 0 0.01 (50|70) 692 cm (/Fm\\d+) Do Q\n").matcher(content);
        assertTrue(m.find(), content);
        String name = m.group(2);
        assertTrue(m.find(), content);
        assertEquals(name, m.group(2));
        assertEquals(3, count(content, " Do Q\n"), content);   // The space draws nothing
        // Drawn again, in another run and at another size, A is placed again
        font.setSize(20f);
        page.drawGlyphs(font, new int[] {a}, new float[] {80f, 100f}, "A", null);
        content = TestSupport.content(page);
        assertTrue(content.contains("q 0.02 0 0 0.02 80 692 cm " + name + " Do Q\n"), content);
        pdf.complete();
        String doc = document(bos);
        assertEquals(2, count(doc, "/Subtype /Form"), "A and B, each once");
        assertTrue(doc.contains(name + " "), "the stamp is a resource");
    }

    @Test
    void inATaggedParagraphTheGlyphsCarryTheirText() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        page.addBDC(StructElem.P, "hi", null, null);
        page.drawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, "क", "hi");
        page.addEMC();
        String content = TestSupport.content(page);
        assertTrue(content.contains("/P <</MCID 0>>\nBDC\n/Span <</Lang <" + Util.toHexString(Util.utf8("hi"))
                + "> /ActualText <" + Page.toUTF16Hex("क") + ">>> BDC\nq "), content);
        assertTrue(content.contains(" Do Q\nEMC\nEMC\n"), content);
        assertEquals(2, count(content, "BDC\n"), content);
        pdf.complete();
        assertFalse(document(bos).contains("/FontFile"), "no font program");
    }

    @Test
    void outsideAnyContentTheGlyphsAreAParagraphOfTheirOwn() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        page.drawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, "A", null);
        String content = TestSupport.content(page);
        assertTrue(content.contains("/P <</MCID 0>>\nBDC\n/Span <</ActualText <" + Page.toUTF16Hex("A")
                + ">>> BDC\n"), content);
        // A tagged document asks for the text the glyphs stand for
        final Page next = new Page(pdf, Letter.PORTRAIT);
        assertThrows(IllegalArgumentException.class,
                () -> next.drawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, null, null));
    }

    @Test
    void aFontUsedForTextTooIsWritten() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        page.drawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, "A", null);
        new TextLine(font, "Text").setLocation(50f, 200f).drawOn(page);
        pdf.complete();
        assertTrue(document(bos).contains("/FontFile3"), "the font of the text line");
    }

    @Test
    void trueTypeOutlinesAreCubicCurvesAndCompositesHaveTheirParts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font cascadia = new Font(pdf, TestSupport.open(CASCADIA));
        GlyphOutline outline = new GlyphOutline(cascadia.program.font, false);
        assertEquals(1.0 / 2048, outline.matrixOf(1)[0], 1e-12);
        // O: two contours of quadratic curves, drawn as cubic ones
        String o = outline.outline(cascadia.unicodeToGID.get('O')).operators;
        assertEquals(2, count(o, " m\n"), o);
        assertTrue(o.contains(" c\n"), o);
        // Aacute, a composite glyph: the contours of A and of the acute
        int aacute = cascadia.unicodeToGID.get(0x00C1);
        int capitalA = cascadia.unicodeToGID.get('A');
        int contoursOfA = count(outline.outline(capitalA).operators, " m\n");
        String composite = outline.outline(aacute).operators;
        assertTrue(count(composite, " m\n") > contoursOfA, composite);
        assertTrue(composite.startsWith(outline.outline(capitalA).operators)
                || composite.endsWith(outline.outline(capitalA).operators), composite);

        Font devanagari = new Font(pdf, TestSupport.open(DEVANAGARI));
        GlyphOutline noto = new GlyphOutline(devanagari.program.font, false);
        String ii = noto.outline(12).operators;     // iideva, a composite glyph
        assertTrue(ii.contains(" m\n") && ii.contains(" c\n"), ii);
        assertEquals("", noto.outline(devanagari.unicodeToGID.get(' ')).operators);
    }

    @Test
    void cffOutlinesHaveTheirSubroutinesAndMatrix() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font plex = TestSupport.plexSans(pdf);
        GlyphOutline outline = new GlyphOutline(plex.program.font, true);
        assertEquals(0.001, outline.matrixOf(1)[0], 1e-12);
        GlyphOutline.Path o = outline.outline(plex.unicodeToGID.get('O'));
        assertEquals(2, count(o.operators, " m\n"), o.operators);
        assertTrue(o.yMin < 0 && o.yMax > 600, o.yMin + " " + o.yMax);
        assertEquals("", outline.outline(plex.unicodeToGID.get(' ')).operators);
    }

    @Test
    void cffOutlinesOfAFontMatrixOfTheirOwnAreThoseOfTheTrueTypeEdition() throws Exception {
        // Cascadia Mono's .otf has 2,048 units to the em, in its FontMatrix,
        // 0.00048828: its glyphs, of the numbers of those of its .ttf, are of
        // their size and in their place
        Font otf = new Font(TestSupport.newPDF(), "fonts/CascadiaMono/CascadiaMono-Regular.otf");
        // In a PDF of its own: a font of the name and the glyphs of one read
        // before is that one
        Font ttf = new Font(TestSupport.newPDF(), TestSupport.open(CASCADIA));
        GlyphOutline cff = new GlyphOutline(otf.program.font, true);
        GlyphOutline glyf = new GlyphOutline(ttf.program.font, false);
        assertTrue(otf.program.cff && !ttf.program.cff);
        assertEquals(1.0 / 2048, cff.matrixOf(1)[0], 1e-8);
        for (char c : "OAgé".toCharArray()) {
            int gid = otf.unicodeToGID.get(c);
            assertTrue(gid != 0 && gid == ttf.unicodeToGID.get(c), c + ": glyph " + gid);
            GlyphOutline.Path a = cff.outline(gid);
            GlyphOutline.Path b = glyf.outline(gid);
            String box = a.xMin + " " + a.yMin + " " + a.xMax + " " + a.yMax;
            assertEquals(b.xMin + " " + b.yMin + " " + b.xMax + " " + b.yMax, box, String.valueOf(c));
        }
    }

    @Test
    void numbersAreHundredthsRoundedHalfAwayFromZero() {
        assertEquals("0", GlyphOutline.decimals(-0.004, 2));
        assertEquals("1.5", GlyphOutline.decimals(1.5, 2));
        assertEquals("-2.67", GlyphOutline.decimals(-8.0 / 3, 2));
        assertEquals("0.0058594", GlyphOutline.decimals(12.0 / 2048, 7));
        assertEquals("100", GlyphOutline.decimals(99.999, 2));
    }

    @Test
    void attachTextCarriesTheTextAsAnAssociatedFile() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Text").setLocation(50f, 200f).drawOn(page);
        pdf.attachText("article-1.txt", "सभी text");
        pdf.complete();
        String doc = document(bos);
        assertTrue(doc.contains("/Subtype /text#2Fplain;#20charset=utf-8"), "the media type");
        assertTrue(doc.contains("/AFRelationship /Source"), "the relationship");
        assertTrue(doc.contains("/AF ["), "an associated file");
        assertTrue(doc.contains("/Desc "), "the description");
    }

    @Test
    void attachTextIsRefusedInPDFA1AndPDFA2() throws Exception {
        for (Compliance compliance : new Compliance[] {Compliance.PDF_A_1B, Compliance.PDF_A_2B}) {
            final PDF pdf = new PDF(new ByteArrayOutputStream(), compliance);
            assertThrows(IllegalStateException.class, () -> pdf.attachText("a.txt", "text"));
        }
    }

    @Test
    void misuseIsReported() throws Exception {
        TestSupport.allowCoreFonts(true);
        try {
            final PDF pdf = new PDF(new ByteArrayOutputStream());
            final Font plex = TestSupport.plexSans(pdf);
            final Font core = TestSupport.helvetica(pdf);
            final Page page = new Page(pdf, Letter.PORTRAIT);
            final int a = plex.unicodeToGID.get('A');
            assertThrows(IllegalArgumentException.class,
                    () -> page.drawGlyphs(core, new int[] {1}, new float[] {0f, 0f}, "A", null));
            assertThrows(IllegalArgumentException.class,
                    () -> page.drawGlyphs(plex, new int[] {a}, new float[] {0f}, "A", null));
            assertThrows(IllegalArgumentException.class,
                    () -> page.drawGlyphs(plex, new int[] {plex.advanceWidth.length}, new float[] {0f, 0f}, "A", null));
            assertThrows(IllegalArgumentException.class,
                    () -> page.drawGlyphs(plex, new int[] {-1}, new float[] {0f, 0f}, "A", null));
        } finally {
            TestSupport.allowCoreFonts(false);
        }
    }
}
