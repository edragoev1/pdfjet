/*
 * OTFTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import java.io.ByteArrayInputStream;
import java.util.Arrays;
import org.junit.jupiter.api.Test;

// The OpenType and TrueType fonts that are not valid, as the Go fuzz targets
// of the font loaders found them: each fails with a message, and none reads
// past the font or allocates what the font does not have.
class OTFTest {
    private static byte[] font(String path) throws Exception {
        return TestSupport.readAll(TestSupport.open(path));
    }

    private static final String THAI = "fonts/NotoSansThai/NotoSansThai-Regular.ttf";
    private static final String PLEX = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf";

    // The message that loading the font fails with.
    private static String error(byte[] font) {
        return assertThrows(Exception.class,
                () -> new Font(TestSupport.newPDF(), new ByteArrayInputStream(font))).getMessage();
    }

    // Loads the font and draws with it.
    private static void draws(byte[] font) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font f = new Font(pdf, new ByteArrayInputStream(font));
        new TextLine(f, "Ab1 กิ่ x").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
    }

    private static int uint16(byte[] font, int offset) {
        return ((font[offset] & 0xFF) << 8) | (font[offset + 1] & 0xFF);
    }

    private static int uint32(byte[] font, int offset) {
        return (uint16(font, offset) << 16) | uint16(font, offset + 2);
    }

    // Where the directory entry of the table of the name begins: its four
    // letters, then its checksum, offset and length.
    private static int entry(byte[] font, String name) {
        for (int i = 0; i < uint16(font, 4); i++) {
            int entry = 12 + 16*i;
            if (new String(font, entry, 4, java.nio.charset.StandardCharsets.UTF_8).equals(name)) {
                return entry;
            }
        }
        throw new IllegalArgumentException("the font has no " + name + " table");
    }

    // Where the table of the name begins in the font.
    private static int table(byte[] font, String name) {
        return uint32(font, entry(font, name) + 8);
    }

    // Where the format 4 subtable of the character map begins: the one of the
    // Windows platform, which PDFjet reads.
    private static int cmap4(byte[] font) {
        int cmap = table(font, "cmap");
        for (int i = 0; i < uint16(font, cmap + 2); i++) {
            int record = cmap + 4 + 8*i;
            if (uint16(font, record) == 3 && uint16(font, record + 2) == 1) {
                return cmap + uint32(font, record + 4);
            }
        }
        throw new IllegalArgumentException("the font has no character map of the Windows platform");
    }

    // The font with the two bytes at the offset changed.
    private static byte[] with(byte[] font, int offset, int value) {
        byte[] patched = Arrays.copyOf(font, font.length);
        patched[offset] = (byte) (value >> 8);
        patched[offset + 1] = (byte) value;
        return patched;
    }

    @Test
    void aFontThatEndsWhereATableIsReadIsRefused() throws Exception {
        byte[] font = font(THAI);
        // The directory, and then the tables it points at, are read from the
        // front of the font, so every one of these ends in the middle of a read.
        for (int length : new int[] {4, 5, 12, 13, 100, 1000}) {
            assertEquals("Invalid font file: the font ends too soon.",
                    error(Arrays.copyOf(font, length)));
        }
        draws(font);
    }

    @Test
    void aFontWithoutWhatItIsDrawnWithIsRefused() throws Exception {
        byte[] ttf = font(THAI);
        byte[] otf = font(PLEX);
        int head = table(ttf, "head");
        int hhea = table(ttf, "hhea");

        // The name of the character map table, changed to one PDFjet does not
        // read, so that the font has none.
        byte[] noCmap = Arrays.copyOf(ttf, ttf.length);
        noCmap[entry(ttf, "cmap")] = 'x';
        assertEquals("Invalid font file: no character map.", error(noCmap));

        assertEquals("Invalid font file: the units per em.", error(with(ttf, head + 18, 0)));
        assertEquals("Invalid font file: the units per em.", error(with(ttf, head + 18, 15)));
        assertEquals("Invalid font file: the units per em.", error(with(ttf, head + 18, 16385)));
        assertEquals("Invalid font file: no advance widths.", error(with(ttf, hhea + 34, 0)));
        assertEquals("Invalid font file: the character map is not format 4.",
                error(with(ttf, cmap4(ttf), 6)));
        // The length of the CFF table, past the end of the font.
        assertEquals("Invalid font file: the CFF table is not in the font.",
                error(with(otf, entry(otf, "CFF ") + 12, 0x7FFF)));
        draws(otf);
    }

    // The font with the table of the name spelled differently, so that
    // PDFjet does not read it and the font has none.
    private static byte[] without(byte[] font, String name) {
        byte[] patched = Arrays.copyOf(font, font.length);
        patched[entry(font, name)] = 'z';
        return patched;
    }

    @Test
    void aFontWithNoNameOfItsOwnIsRefused() throws Exception {
        // The name goes into the PDF as the name of the font, where a name
        // that is not a PDF name would break the syntax, so a font with none
        // is refused as a stream font with none is.
        assertEquals("Invalid font file: the font name.", error(without(font(THAI), "name")));
    }

    @Test
    void aFontWithoutTheTablesItNeedsNoneOfStillDraws() throws Exception {
        // Without OS/2 the font says it holds no characters and maps none of
        // them; without post it has no underline; without GPOS its marks are
        // not placed. None of the three stops it from drawing.
        byte[] font = font(THAI);
        for (String name : new String[] {"OS/2", "post", "GPOS"}) {
            draws(without(font, name));
        }
    }

    @Test
    void aNameRecordOutsideTheFontIsLeftOut() throws Exception {
        // The offset of the first name record, past the end of the font. The
        // font keeps its other records and still draws.
        byte[] font = font(THAI);
        draws(with(font, table(font, "name") + 16, 0xFFFF));
    }

    @Test
    void aSegmentOutsideTheGlyphIDArrayGivesNoGlyph() throws Exception {
        // The length of the format 4 subtable, cut to its header and the four
        // arrays of its segments, so that its glyph ID array holds nothing and
        // every segment that is read through it points outside.
        byte[] font = font(THAI);
        int subtable = cmap4(font);
        draws(with(font, subtable + 2, 16 + 4*uint16(font, subtable + 6)));
    }

    @Test
    void aGposTableIsNotReadPastTheWorkAFontNeeds() throws Exception {
        // The lookups of the GPOS table, and the subtables of its first
        // lookup, changed to the most a font can say it has. Read to the end
        // it says, the font takes every byte of memory there is and never
        // loads; read to the work a font needs, it loads at once.
        byte[] font = font(THAI);
        int gpos = table(font, "GPOS");
        int lookupList = gpos + uint16(font, gpos + 8);
        int lookup = lookupList + uint16(font, lookupList + 2);
        draws(with(with(font, lookupList, 0xFFFF), lookup + 4, 0xFFFF));
    }
}
