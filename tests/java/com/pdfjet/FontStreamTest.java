/*
 * FontStreamTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.EOFException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.zip.DeflaterOutputStream;
import org.junit.jupiter.api.Test;

// The stream fonts that are not valid, as the Go fuzz targets of the stream
// fonts found them: each fails with a message, and none reads past its data
// or allocates what it does not have.
class FontStreamTest {
    // The metrics of a stream font: the units per em, the first and the last
    // character, the advance widths and the character map.
    private static byte[] metrics(int unitsPerEm, int firstChar, int lastChar, int widths, int cmap) {
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        for (int v : new int[] {unitsPerEm, 0, -200, 1000, 800, 800, -200, firstChar, lastChar, 700, -100, 50}) {
            writeInt32(buf, v);
        }
        writeInt32(buf, widths);
        buf.write(new byte[2*widths], 0, 2*widths);
        writeInt32(buf, cmap);
        buf.write(new byte[2*cmap], 0, 2*cmap);
        return buf.toByteArray();
    }

    // A stream font with the name and the metrics, and a font file of 4 bytes.
    private static byte[] stream(String name, byte[] metrics) throws Exception {
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        buf.write(name.length());
        for (char c : name.toCharArray()) {
            buf.write(c);
        }
        buf.write(new byte[3], 0, 3);   // No license text
        ByteArrayOutputStream compressed = new ByteArrayOutputStream();
        DeflaterOutputStream zlib = new DeflaterOutputStream(compressed);
        zlib.write(metrics);
        zlib.close();
        writeInt32(buf, compressed.size());
        compressed.writeTo(buf);
        buf.write('N');
        writeInt32(buf, 8);
        writeInt32(buf, 4);
        buf.write(new byte[4], 0, 4);
        return buf.toByteArray();
    }

    private static void writeInt32(ByteArrayOutputStream buf, int v) {
        buf.write(v >> 24);
        buf.write(v >> 16);
        buf.write(v >> 8);
        buf.write(v);
    }

    private static Exception error(byte[] stream) {
        return assertThrows(Exception.class,
                () -> new Font(TestSupport.newPDF(), new ByteArrayInputStream(stream)));
    }

    @Test
    void aMarkAfterAGlyphPastTheAdvanceWidthsIsDrawn() throws Exception {
        // IBM Plex Sans JP maps ↺ and 14 other arrows to glyphs past the end
        // of its advance widths, which the offsets of the marks looked up.
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"));
        new TextLine(font, "↺́ x").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
    }

    @Test
    void lengthsThatTheStreamDoesNotHaveTakeNoMemory() {
        // The metrics say they are 4 GB long, and the stream ends.
        byte[] stream = {1, 'A', 0, 0, 0, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF};
        Runtime runtime = Runtime.getRuntime();
        long before = runtime.totalMemory() - runtime.freeMemory();
        assertTrue(error(stream) instanceof EOFException);
        assertTrue(runtime.totalMemory() - runtime.freeMemory() - before < 64L*1024*1024);
    }

    @Test
    void metricsItCannotDrawWithAreRejected() throws Exception {
        assertEquals("Invalid font stream: the units per em.",
                error(stream("A", metrics(0, 32, 126, 1, 0x10000))).getMessage());
        assertEquals("Invalid font stream: the first or last character.",
                error(stream("A", metrics(1000, -1, 126, 1, 0x10000))).getMessage());
        assertEquals("Invalid font stream: the first or last character.",
                error(stream("A", metrics(1000, 32, 0x10000, 1, 0x10000))).getMessage());
        assertEquals("Invalid font stream: no advance widths.",
                error(stream("A", metrics(1000, 32, 126, 0, 0x10000))).getMessage());
        assertEquals("Invalid font stream: the character map.",
                error(stream("A", metrics(1000, 32, 126, 1, 0x100))).getMessage());
        assertEquals("Invalid font stream: the metrics end too soon.",
                error(stream("A", Arrays.copyOf(metrics(1000, 32, 126, 1, 0x10000), 100))).getMessage());

        byte[] valid = stream("A", metrics(1000, 32, 126, 1, 0x10000));
        new Font(TestSupport.newPDF(), new ByteArrayInputStream(valid));
        new Font(new ArrayList<PDFobj>(), new ByteArrayInputStream(valid));
    }

    @Test
    void aNameThatIsNotAPDFNameIsRejected() throws Exception {
        for (String name : new String[] {"", "Noto Sans", "Noto/Sans", "Noto(Sans", "Noto#20Sans"}) {
            assertEquals("Invalid font stream: the font name.",
                    error(stream(name, metrics(1000, 32, 126, 1, 0x10000))).getMessage());
        }
    }

    @Test
    void aFontFileShorterThanItsSizeIsRejected() throws Exception {
        byte[] stream = stream("A", metrics(1000, 32, 126, 1, 0x10000));
        assertTrue(error(Arrays.copyOf(stream, stream.length - 1)) instanceof EOFException);
    }

    @Test
    void aGlyphPastTheAdvanceWidthsHasTheWidthOfTheLastOne() throws Exception {
        // Two advance widths, 500 and 700, and "A" maps to glyph 5 past them.
        byte[] metrics = metrics(1000, 32, 126, 2, 0x10000);
        metrics[52] = (byte) (500 >> 8);
        metrics[53] = (byte) 500;
        metrics[54] = (byte) (700 >> 8);
        metrics[55] = (byte) 700;
        metrics[61 + 2*'A'] = 5;
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = new Font(pdf, new ByteArrayInputStream(stream("A", metrics)));
        assertEquals(7f, font.stringWidth(10f, "A"), 0.001f);
        new TextLine(font, "A").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        assertTrue(TestSupport.latin1(bos.toByteArray()).contains("/DW 700\n"));
    }
}
