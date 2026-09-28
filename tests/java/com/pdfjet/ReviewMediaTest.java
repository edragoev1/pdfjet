/*
 * ReviewMediaTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Locale;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.zip.CRC32;
import org.junit.jupiter.api.Test;

class ReviewMediaTest {
    private static final String THAI = "fonts/NotoSansThai/NotoSansThai-Regular.ttf";

    private static byte[] font(String path) throws Exception {
        return TestSupport.readAll(TestSupport.open(path));
    }

    private static int uint16(byte[] font, int offset) {
        return ((font[offset] & 0xFF) << 8) | (font[offset + 1] & 0xFF);
    }

    // Where the directory entry of the table of the name begins.
    private static int entry(byte[] font, String name) {
        for (int i = 0; i < uint16(font, 4); i++) {
            int entry = 12 + 16*i;
            if (new String(font, entry, 4, StandardCharsets.UTF_8).equals(name)) {
                return entry;
            }
        }
        throw new IllegalArgumentException("the font has no " + name + " table");
    }

    // The font with the table of the name spelled differently, so that
    // PDFjet does not read it and the font has none.
    private static byte[] without(byte[] font, String name) {
        byte[] patched = Arrays.copyOf(font, font.length);
        patched[entry(font, name)] = 'z';
        return patched;
    }

    // The font with the table of the name replaced by the bytes, which are put
    // at the end of the font.
    private static byte[] withTable(byte[] font, String name, byte[] table) {
        int entry = entry(font, name);
        byte[] patched = Arrays.copyOf(font, font.length + table.length);
        System.arraycopy(table, 0, patched, font.length, table.length);
        ByteBuffer.wrap(patched, entry + 8, 8).putInt(font.length).putInt(table.length);
        return patched;
    }

    // Loads the font and draws with it.
    private static void draws(byte[] font) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font f = new Font(pdf, new ByteArrayInputStream(font));
        new TextLine(f, "Ab1 กิ่ x").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
    }

    // A GPOS table of one lookup, of the given number of MarkToBase subtables
    // that are all the same one: its marks and its letters are a coverage
    // table of format 2 with one range, of glyph 0 alone, but at the coverage
    // index 65535, and its letters have no classes of marks.
    private static byte[] gposBomb(int subtables) {
        ByteBuffer gpos = ByteBuffer.allocate(42 + 2*subtables);
        gpos.putInt(0x00010000);
        gpos.putShort((short) 0);       // The script list
        gpos.putShort((short) 0);       // The feature list
        gpos.putShort((short) 10);      // The lookup list
        gpos.putShort((short) 1);       // One lookup
        gpos.putShort((short) 4);       // At 4 from the lookup list
        gpos.putShort((short) 4);       // MarkToBase
        gpos.putShort((short) 0);       // No flags
        gpos.putShort((short) subtables);
        for (int i = 0; i < subtables; i++) {
            gpos.putShort((short) (6 + 2*subtables));   // Every subtable is the one after the lookup
        }
        gpos.putShort((short) 1);       // Format 1
        gpos.putShort((short) 12);      // The marks
        gpos.putShort((short) 12);      // The letters
        gpos.putShort((short) 0);       // No classes of marks
        gpos.putShort((short) 22);      // The mark array
        gpos.putShort((short) 22);      // The letter array
        gpos.putShort((short) 2);       // A coverage table of format 2
        gpos.putShort((short) 1);       // One range
        gpos.putShort((short) 0);       // From glyph 0
        gpos.putShort((short) 0);       // To glyph 0
        gpos.putShort((short) 65535);
        return Arrays.copyOf(gpos.array(), gpos.position() + 16);
    }

    @Test
    void aGposTableOfSubtablesThatAreOneAnotherLoadsAtOnce() throws Exception {
        // The coverage index of the one glyph makes room for 65,536 glyphs,
        // and each of the 20,000 subtables went through all of them: 40 KB of
        // GPOS table kept a font from loading for a minute.
        byte[] font = withTable(font(THAI), "GPOS", gposBomb(20000));
        long start = System.nanoTime();
        draws(font);
        long elapsed = (System.nanoTime() - start) / 1000000;
        assertTrue(elapsed < 2000, "the font loads in " + elapsed + " ms");
    }

    // A character map of a format 4 subtable alone, for the Windows platform,
    // of the segments of the start and end codes, each of delta 0, followed by
    // the segment of 0xFFFF that ends every table.
    private static byte[] cmapOfSegments(int[] starts, int[] ends) {
        int segments = starts.length + 1;
        ByteBuffer cmap = ByteBuffer.allocate(28 + 8*segments);
        cmap.putShort((short) 0);       // The version
        cmap.putShort((short) 1);       // One encoding record
        cmap.putShort((short) 3);       // Windows
        cmap.putShort((short) 1);       // Unicode BMP
        cmap.putInt(12);
        cmap.putShort((short) 4);       // Format 4
        cmap.putShort((short) (16 + 8*segments));
        cmap.putShort((short) 0);       // The language
        cmap.putShort((short) (2*segments));
        cmap.putShort((short) 0);       // The search range, the entry selector and the range shift
        cmap.putShort((short) 0);
        cmap.putShort((short) 0);
        for (int end : ends) {
            cmap.putShort((short) end);
        }
        cmap.putShort((short) 0xFFFF);
        cmap.putShort((short) 0);       // The reserved pad
        for (int start : starts) {
            cmap.putShort((short) start);
        }
        cmap.putShort((short) 0xFFFF);
        for (int i = 0; i < 2*segments; i++) {
            cmap.putShort((short) 0);   // The deltas and the range offsets
        }
        return cmap.array();
    }

    @Test
    void theCharacterMapIsReadInOnePassOverItsSegments() throws Exception {
        // 32,766 segments of the character 0xFFFE alone, before the last one:
        // a search of every segment for every character took 2^31
        // comparisons. The font has no OS/2 table, so that every character is
        // looked up.
        byte[] font = without(font(THAI), "OS/2");
        int[] starts = new int[32766];
        Arrays.fill(starts, 0xFFFE);
        long start = System.nanoTime();
        OTF otf = new OTF(new ByteArrayInputStream(withTable(font, "cmap", cmapOfSegments(starts, starts))));
        long elapsed = (System.nanoTime() - start) / 1000000;
        assertTrue(elapsed < 2000, "the font loads in " + elapsed + " ms");
        // The first segment of the character is the one it is in.
        assertEquals(0xFFFE, otf.unicodeToGID[0xFFFE]);
        assertEquals(0, otf.unicodeToGID['A']);
        // Segments of 'A' to 'C' and 'X' to 'Z', with a delta of 0, map each
        // character to the glyph of its code, and none between them.
        otf = new OTF(new ByteArrayInputStream(withTable(font, "cmap",
                cmapOfSegments(new int[] {'A', 'X'}, new int[] {'C', 'Z'}))));
        int[][] glyphs = {{'@', 0}, {'A', 'A'}, {'C', 'C'}, {'D', 0}, {'W', 0}, {'X', 'X'}, {'Z', 'Z'}, {'[', 0}};
        for (int[] glyph : glyphs) {
            assertEquals(glyph[1], otf.unicodeToGID[glyph[0]], "character " + (char) glyph[0]);
        }
    }

    @Test
    void aFontWithoutAnOS2TableHasItsCharacters() throws Exception {
        // The OS/2 table says the first and the last character of the font; a
        // font without one has every character of its character map, where it
        // had none and drew every character as .notdef.
        byte[] font = font(THAI);
        OTF with = new OTF(new ByteArrayInputStream(font));
        OTF without = new OTF(new ByteArrayInputStream(without(font, "OS/2")));
        for (int ch : new int[] {'A', 'z', 'ก'}) {
            assertTrue(without.unicodeToGID[ch] != 0, "character " + (char) ch);
            assertEquals(with.unicodeToGID[ch], without.unicodeToGID[ch], "character " + (char) ch);
        }
    }

    // The message that a document of the compliance that holds the image of
    // the file fails with, or "" when it is completed.
    private static String pdfaError(Compliance level, String path) throws Exception {
        return pdfaImageError(level, TestSupport.open(path));
    }

    // The message that a document of the compliance that holds the image fails
    // with, or "" when it is completed.
    static String pdfaImageError(Compliance level, InputStream stream) throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), level);
        pdf.setTitle("Title");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Text").setLocation(50f, 50f).drawOn(page);
        try {
            Image image = new Image(pdf, stream);
            image.setAltDescription("An image").setLocation(50f, 100f);
            image.drawOn(page);
            pdf.complete();
        } catch (IllegalStateException e) {
            return e.getMessage();
        }
        return "";
    }

    @Test
    void aPDFADocumentHoldsNoImageItsLevelHasNot() throws Exception {
        // The output intent of PDF/A is sRGB, so its images are not CMYK;
        // PDF/A-1 has no soft masks, and 8 bits per component at most.
        assertEquals("A document of PDF_A_2B cannot hold a CMYK image: "
                + "its output intent is sRGB, so its images are gray or RGB.",
                pdfaError(Compliance.PDF_A_2B, "images/cmyk.jpg"));
        assertEquals("A document of PDF_A_1B cannot hold an image with transparency: "
                + "PDF/A-1 has no soft masks, so its images are opaque.",
                pdfaError(Compliance.PDF_A_1B, "PngSuite/BASN6A08.PNG"));
        assertEquals("A document of PDF_A_1A cannot hold an image of 16 bits per component: "
                + "PDF/A-1 has 8 at most.",
                pdfaError(Compliance.PDF_A_1A, "PngSuite/BASN2C16.PNG"));
        // PDF/A-2 and PDF/A-3 hold both, and a document that is not PDF/A all three.
        assertEquals("", pdfaError(Compliance.PDF_A_2B, "PngSuite/BASN6A08.PNG"));
        assertEquals("", pdfaError(Compliance.PDF_A_3B, "PngSuite/BASN2C16.PNG"));
        assertEquals("", pdfaError(Compliance.PDF_UA_1, "images/cmyk.jpg"));
        assertEquals("", pdfaError(Compliance.PDF_A_1B, "PngSuite/BASN2C08.PNG"));
    }

    private static SVGImage svg(String svg) throws Exception {
        return new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8)));
    }

    private static String draw(String svg) throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        SVGImage image = svg(svg);
        image.setLocation(0f, 0f);
        TestSupport.assertXY(image.getWidth(), image.getHeight(), image.drawOn(page));
        return TestSupport.content(page);
    }

    private static String repeat(String text, int count) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++) {
            sb.append(text);
        }
        return sb.toString();
    }

    @Test
    void theCommentsOfAStyleSheetAreLeftOutInOnePass() throws Exception {
        String svg = "<svg width=\"10\" height=\"10\"><style>" + repeat("/**/", 40000)
                + ".a { fill: /* red */ blue } /* .a { fill: red } */ .b { fill: green } /* not closed .a { fill: red }"
                + "</style><rect class=\"a\" width=\"5\" height=\"5\"/></svg>";
        long start = System.nanoTime();
        String content = draw(svg);
        long elapsed = (System.nanoTime() - start) / 1000000;
        assertTrue(elapsed < 2000, "the style sheet is read in " + elapsed + " ms");
        assertTrue(content.startsWith("0 0 1 rg\n"), content);
    }

    @Test
    void theRulesOfTheClassesOfAnElementAreFoundByClass() throws Exception {
        // 20,000 rules and 20,000 elements of three classes each: every
        // element went through every rule for each of its classes.
        StringBuilder sb = new StringBuilder("<svg width=\"10\" height=\"10\"><style>");
        for (int i = 0; i < 20000; i++) {
            sb.append(".c").append(i).append("{fill:red}");
        }
        sb.append("</style>").append(repeat("<g class=\"x y z\"/>", 20000)).append("</svg>");
        long start = System.nanoTime();
        svg(sb.toString());
        long elapsed = (System.nanoTime() - start) / 1000000;
        assertTrue(elapsed < 2000, "the image is read in " + elapsed + " ms");
        // The rules are those of the style sheet, in its order, whatever the
        // order of the classes, and a class named twice has its rules once.
        String content = draw("<svg width=\"10\" height=\"10\"><style>.b{fill:red} .a{fill:blue} .b{stroke:green}"
                + "</style><rect class=\"b a b\" width=\"5\" height=\"5\"/></svg>");
        assertTrue(content.startsWith("0 0 1 rg\n") && content.contains("0 0.5 0 RG\n"), content);
    }

    // The first control points of the cubic curves of the path data, rounded
    // to two decimals.
    private static String firstControlPoints(String data) {
        List<String> points = new ArrayList<String>();
        for (PathOp op : SVG.toPDF(SVG.getOperations(data))) {
            if (op.cmd == 'C') {
                points.add(String.format(Locale.ROOT, "%.2f,%.2f", op.x1, op.y1));
            }
        }
        return String.join(" ", points);
    }

    @Test
    void aSmoothCurveReflectsTheControlPointOfACurveOfItsKind() {
        // T reflects the control point of the quadratic curve before, Q or T,
        // and else starts from the current point; S reflects the second
        // control point of the cubic curve before, C or S, and else starts
        // from the current point. The first control point of a cubic curve
        // made of a quadratic one is two thirds of the way to the quadratic
        // control point.
        String[][] cases = {
            // The second T reflects (15, -10), the control point of the first.
            {"M0 0 Q 5 10 10 0 T 20 0 T 30 0", "3.33,6.67 13.33,-6.67 23.33,6.67"},
            {"M0 0 Q 5 10 10 0 t 10 0 t 10 0", "3.33,6.67 13.33,-6.67 23.33,6.67"},
            {"M0 0 C 0 10 10 10 10 0 T 20 0", "0.00,10.00 10.00,0.00"},
            {"M0 0 Q 5 10 10 0 S 20 10 20 0", "3.33,6.67 10.00,0.00"},
            {"M0 0 C 0 10 10 10 10 0 S 20 -10 20 0", "0.00,10.00 10.00,-10.00"},
            {"M0 0 S 10 10 20 0 S 30 -10 40 0", "0.00,0.00 30.00,-10.00"},
        };
        for (String[] c : cases) {
            assertEquals(c[1], firstControlPoints(c[0]), c[0]);
        }
    }

    @Test
    void aCommandAfterZStartsAtTheStartOfTheSubpathItClosed() throws Exception {
        // A line after Z, with no moveto, starts where the closed subpath did;
        // the path is stroked once, and filled with every subpath in place.
        String content = draw("<svg width=\"100\" height=\"100\">"
                + "<path d=\"M10 10 L50 10 L50 50 Z L 90 90\" fill=\"red\" stroke=\"black\"/></svg>");
        assertEquals("1 0 0 rg\n10 782 m\n50 782 l\n50 742 l\n10 782 m\n90 702 l\nf\n"
                + "0 0 0 RG\n1 w\n10 782 m\n50 782 l\n50 742 l\nh\n10 782 m\n90 702 l\nS\n", content);
    }

    // A PNG file with the IHDR of the size, bit depth and color type, and the
    // IDAT chunks.
    private static byte[] png(int width, int height, int bitDepth, int colorType, byte[]... idats) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        bos.write(new byte[] {(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'}, 0, 8);
        ByteBuffer ihdr = ByteBuffer.allocate(13);
        ihdr.putInt(width).putInt(height).put((byte) bitDepth).put((byte) colorType)
                .put((byte) 0).put((byte) 0).put((byte) 0);
        chunk(bos, "IHDR", ihdr.array());
        for (byte[] idat : idats) {
            chunk(bos, "IDAT", idat);
        }
        chunk(bos, "IEND", new byte[0]);
        return bos.toByteArray();
    }

    private static void chunk(ByteArrayOutputStream bos, String type, byte[] data) {
        byte[] name = type.getBytes(StandardCharsets.US_ASCII);
        CRC32 crc = new CRC32();
        crc.update(name);
        crc.update(data);
        bos.write(ByteBuffer.allocate(4).putInt(data.length).array(), 0, 4);
        bos.write(name, 0, 4);
        bos.write(data, 0, data.length);
        bos.write(ByteBuffer.allocate(4).putInt((int) crc.getValue()).array(), 0, 4);
    }

    private static byte[] concat(byte[] a, byte[] b) {
        byte[] joined = Arrays.copyOf(a, a.length + b.length);
        System.arraycopy(b, 0, joined, a.length, b.length);
        return joined;
    }

    @Test
    void anUnknownPNGFilterTypeIsRefused() {
        // Filter types 0 to 4 are all PNG defines; libpng refuses a row of
        // another one, which was read as if it had no filter.
        byte[] png = png(2, 1, 8, 2, Compressor.deflate(new byte[] {5, 1, 2, 3, 4, 5, 6}));
        assertEquals("Invalid PNG filter type 5.",
                assertThrows(Exception.class, () -> new PNGImage(new ByteArrayInputStream(png))).getMessage());
    }

    @Test
    void thePNGFiltersAreUndone() throws Exception {
        // Two rows of two RGB pixels, the second row with each filter, over a
        // first row of Sub.
        byte[] first = {1, 10, 20, 30, 5, 5, 5};
        byte[] want = {10, 20, 30, 15, 25, 35};
        byte[][][] cases = {
            {{0, 1, 2, 3, 4, 5, 6}, {1, 2, 3, 4, 5, 6}},
            {{1, 1, 2, 3, 4, 5, 6}, {1, 2, 3, 5, 7, 9}},
            {{2, 1, 2, 3, 4, 5, 6}, {11, 22, 33, 19, 30, 41}},
            // (left + above) / 2, with the left one of the first pixel 0.
            {{3, 1, 2, 3, 4, 5, 6}, {6, 12, 18, 14, 23, 32}},
            // The first pixel takes the one above, as it is nearest; so does
            // the second, of left 6, above 15 and above on the left 10.
            {{4, 1, 2, 3, 4, 5, 6}, {11, 22, 33, 19, 30, 41}},
        };
        for (byte[][] c : cases) {
            PNGImage png = new PNGImage(new ByteArrayInputStream(
                    png(2, 2, 8, 2, Compressor.deflate(concat(first, c[0])))));
            assertArrayEquals(concat(want, c[1]), Decompressor.inflate(png.getData()), "filter " + c[0][0]);
        }
    }

    @Test
    void theIDATChunksAreJoinedInTheirOrder() throws Exception {
        // The compressed rows split into chunks of one byte each.
        byte[] rows = new byte[64*(1 + 3*64)];
        for (int i = 0; i < rows.length; i++) {
            rows[i] = (byte) ((i % (1 + 3*64) == 0) ? 0 : i * 7);
        }
        byte[] deflated = Compressor.deflate(rows);
        byte[][] idats = new byte[deflated.length][];
        for (int i = 0; i < deflated.length; i++) {
            idats[i] = new byte[] {deflated[i]};
        }
        PNGImage split = new PNGImage(new ByteArrayInputStream(png(64, 64, 8, 2, idats)));
        PNGImage whole = new PNGImage(new ByteArrayInputStream(png(64, 64, 8, 2, deflated)));
        assertArrayEquals(Decompressor.inflate(whole.getData()), Decompressor.inflate(split.getData()));
    }

    // A truecolor PNG with a pHYs chunk of the given pixels per unit and unit.
    private static byte[] pngWithPhys(int width, int height, int x, int y, int unit) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        bos.write(new byte[] {(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'}, 0, 8);
        ByteBuffer ihdr = ByteBuffer.allocate(13);
        ihdr.putInt(width).putInt(height).put((byte) 8).put((byte) 2)
                .put((byte) 0).put((byte) 0).put((byte) 0);
        chunk(bos, "IHDR", ihdr.array());
        chunk(bos, "pHYs", ByteBuffer.allocate(9).putInt(x).putInt(y).put((byte) unit).array());
        chunk(bos, "IDAT", Compressor.deflate(new byte[height*(1 + 3*width)]));
        chunk(bos, "IEND", new byte[0]);
        return bos.toByteArray();
    }

    @Test
    void aPhysicalSizeOfMorePixelsThanAPNGNumberHoldsIsPassedOver() throws Exception {
        // The numbers of a PNG chunk are 2^31 - 1 at most; one past it drew
        // the image a thousandth of a point wide.
        for (int ppm : new int[] {0x80000000, 0xFFFFFFFF}) {
            PNGImage png = new PNGImage(new ByteArrayInputStream(pngWithPhys(8, 8, ppm, 4724, 1)));
            assertEquals(0f, png.getPhysicalWidth(), 0f);
            assertEquals(0f, png.getPhysicalHeight(), 0f);
        }
    }

    @Test
    void aJPEGAReaderCannotDecodeIsRefused() throws Exception {
        // A lossless, a hierarchical or an arithmetic coded JPEG is not one
        // the DCTDecode filter of a PDF reader decodes.
        for (int sof : new int[] {0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF}) {
            byte[] jpeg = jpeg(sof);
            Exception e = assertThrows(Exception.class, () -> new JPGImage(new ByteArrayInputStream(jpeg)));
            assertEquals("Error: The JPEG is lossless, hierarchical or arithmetic coded (SOF"
                    + (sof - 0xC0) + "), which a PDF reader cannot decode.", e.getMessage());
        }
        for (int sof : new int[] {0xC0, 0xC1, 0xC2}) {
            assertNotNull(new JPGImage(new ByteArrayInputStream(jpeg(sof))));
        }
    }

    private static byte[] jpeg(int sof) {
        int[] bytes = {0xFF, 0xD8, 0xFF, sof, 0x00, 0x11, 8, 0, 8, 0, 8, 3,
                1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0, 0xFF, 0xD9};
        byte[] jpeg = new byte[bytes.length];
        for (int i = 0; i < bytes.length; i++) {
            jpeg[i] = (byte) bytes[i];
        }
        return jpeg;
    }

    @Test
    void theImageObjectHasEveryPixelOfAWideImage() throws Exception {
        // A float holds every whole number only up to 2^24, and 2^24 + 1
        // pixels were written as 2^24.
        int width = (1 << 24) + 1;
        byte[] png = png(width, 1, 1, 0, Compressor.deflate(new byte[1 + (width + 7)/8]));
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        new Image(pdf, new ByteArrayInputStream(png));
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        assertTrue(TestSupport.latin1(bos.toByteArray()).contains("/Width 16777217\n"),
                "the image object has not the width of the image");
    }

    @Test
    void theLinkOfATurnedImageCoversItAsItIsDrawn() throws Exception {
        // The link covered the image as it is drawn unturned, which a quarter
        // turn makes as wide as it was tall.
        for (int degrees : new int[] {0, 90, 180, 270}) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            PDF pdf = new PDF(bos);
            Page page = new Page(pdf, Letter.PORTRAIT);
            Image image = new Image(pdf, TestSupport.open("images/GLA250.png"));
            image.setRotation(degrees).setURIAction("https://pdfjet.com").setLocation(10f, 20f);
            image.drawOn(page);
            pdf.complete();
            Matcher match = Pattern.compile("/Rect \\[([\\d.]+) ([\\d.]+) ([\\d.]+) ([\\d.]+)\\]")
                    .matcher(TestSupport.latin1(bos.toByteArray()));
            assertTrue(match.find(), degrees + ": no link");
            double[] rect = new double[4];
            for (int i = 0; i < 4; i++) {
                rect[i] = Double.parseDouble(match.group(i + 1));
            }
            double w = image.getWidth();
            double h = image.getHeight();
            if (degrees == 90 || degrees == 270) {
                double t = w;
                w = h;
                h = t;
            }
            assertEquals(10.0, rect[0], 0.0, degrees + ": /Rect " + Arrays.toString(rect));
            assertEquals(792.0 - 20.0, rect[1], 0.0, degrees + ": /Rect " + Arrays.toString(rect));
            assertEquals(w, rect[2] - rect[0], 0.01, degrees + ": /Rect " + Arrays.toString(rect));
            assertEquals(h, rect[1] - rect[3], 0.01, degrees + ": /Rect " + Arrays.toString(rect));
        }
    }

    @Test
    void textFitsInAnyWidthAtAFontSizeOfZero() throws Exception {
        // Text of no size has no width; the size divided the width.
        Font font = new Font(TestSupport.newPDF(), TestSupport.open(THAI));
        for (Font f : new Font[] {TestSupport.helvetica(TestSupport.newPDF()), font}) {
            f.setSize(0f);
            assertEquals(5, f.getFitChars("Hello", 10f));
            assertEquals(0, f.getFitChars("Hello", -1f));
        }
    }

    @Test
    void aFontIsEmbeddedOnceWhenItIsAddedTwice() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        String path = TestSupport.file(THAI).getPath();
        Font font1 = new Font(pdf, path);
        Font font2 = new Font(pdf, path);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font1, "A").setLocation(50f, 50f).drawOn(page);
        new TextLine(font2, "B").setLocation(50f, 80f).drawOn(page);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, raw.split("/Length1 ", -1).length - 1, "the font is embedded more than once");
    }

    @Test
    void aFontFileNotEndingInStreamIsToldByItsFirstBytes() throws Exception {
        // A stream font under another name is read as a stream font, as the
        // constructor of a stream reads it.
        java.io.File copy = java.io.File.createTempFile("pdfjet", ".font");
        try {
            java.nio.file.Files.copy(TestSupport.file("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream").toPath(),
                    copy.toPath(), java.nio.file.StandardCopyOption.REPLACE_EXISTING);
            Font font = new Font(TestSupport.newPDF(), copy.getPath());
            assertEquals(new Font(TestSupport.newPDF(), TestSupport.open(
                    "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")).name, font.name);
        } finally {
            copy.delete();
        }
    }
}
