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

    // The cap height PDFjet reads of the font.
    private static int capHeight(byte[] font) throws Exception {
        return new OTF(new ByteArrayInputStream(font)).capHeight;
    }

    // The font with the length of the table of the name in its directory changed.
    private static byte[] length(byte[] font, String name, int length) {
        int entry = entry(font, name);
        return with(with(font, entry + 12, length >>> 16), entry + 14, length & 0xFFFF);
    }

    @Test
    void theCapHeightIsReadOnlyFromAnOS2TableThatHasIt() throws Exception {
        // The cap height of both fonts, sCapHeight in OS/2 and the top of the
        // H, is 714, so sCapHeight is changed to 999 to tell the two apart.
        // Noto Sans Thai has the short offsets of loca, and Noto Sans the long
        // ones.
        for (String path : new String[] {THAI, "fonts/NotoSans/NotoSans-Regular.ttf"}) {
            byte[] font = font(path);
            int os2 = table(font, "OS/2");
            font = with(font, os2 + 88, 999);
            assertEquals(999, capHeight(font), path + ", version 4");
            // A version 1 table ends before sCapHeight: the 999 is not its own.
            assertEquals(714, capHeight(with(font, os2, 1)), path + ", version 1");
            // Nor is it of a version 2 table that ends before it.
            assertEquals(714, capHeight(length(font, "OS/2", 88)), path + ", a table of 88 bytes");
        }
    }

    @Test
    void aFontWithoutTheCapHeightOrTheOutlineOfAnHHasItsAscent() throws Exception {
        // IBM Plex Sans has CFF outlines, and so no glyf table to find the top
        // of the H in; its ascent is 1025. Noto Sans Thai without its loca
        // table has no offset for its H; its ascent is 1061.
        byte[] otf = font(PLEX);
        assertEquals(1025, capHeight(with(otf, table(otf, "OS/2"), 1)), "CFF outlines");
        byte[] ttf = font(THAI);
        ttf = with(ttf, table(ttf, "OS/2"), 1);
        assertEquals(1061, capHeight(without(ttf, "loca")), "no loca table");
        // A loca table that ends before the offsets of the H, and a glyf table
        // that ends before the header of the H.
        assertEquals(1061, capHeight(length(ttf, "loca", 4)), "a loca table of 4 bytes");
        assertEquals(1061, capHeight(length(ttf, "glyf", 4)), "a glyf table of 4 bytes");
    }
}
