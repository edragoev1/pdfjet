/*
 * OTFTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
// The OpenType and TrueType fonts that are not valid, as the Go fuzz targets
// of the font loaders found them: each fails with a message, and none reads
// past the font or allocates what the font does not have.
public class OTFTest {
    private const string THAI = "fonts/NotoSansThai/NotoSansThai-Regular.ttf";
    private const string PLEX = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf";

    private static byte[] FontBytes(string path) {
        return File.ReadAllBytes(TestSupport.RepoPath(path));
    }

    // The message that loading the font fails with.
    private static string Error(byte[] font) {
        return Assert.ThrowsAny<Exception>(
                () => new Font(TestSupport.NewPDF(), new MemoryStream(font))).Message;
    }

    // Loads the font and draws with it.
    private static void Draws(byte[] font) {
        PDF pdf = TestSupport.NewPDF();
        Font f = new Font(pdf, new MemoryStream(font));
        new TextLine(f, "Ab1 กิ่ x").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
    }

    private static int UInt16(byte[] font, int offset) {
        return (font[offset] << 8) | font[offset + 1];
    }

    private static int UInt32(byte[] font, int offset) {
        return (UInt16(font, offset) << 16) | UInt16(font, offset + 2);
    }

    // Where the directory entry of the table of the name begins: its four
    // letters, then its checksum, offset and length.
    private static int Entry(byte[] font, string name) {
        for (int i = 0; i < UInt16(font, 4); i++) {
            int entry = 12 + 16*i;
            if (Encoding.UTF8.GetString(font, entry, 4).Equals(name)) {
                return entry;
            }
        }
        throw new ArgumentException("the font has no " + name + " table");
    }

    // Where the table of the name begins in the font.
    private static int Table(byte[] font, string name) {
        return UInt32(font, Entry(font, name) + 8);
    }

    // Where the format 4 subtable of the character map begins: the one of the
    // Windows platform, which PDFjet reads.
    private static int Cmap4(byte[] font) {
        int cmap = Table(font, "cmap");
        for (int i = 0; i < UInt16(font, cmap + 2); i++) {
            int record = cmap + 4 + 8*i;
            if (UInt16(font, record) == 3 && UInt16(font, record + 2) == 1) {
                return cmap + UInt32(font, record + 4);
            }
        }
        throw new ArgumentException("the font has no character map of the Windows platform");
    }

    // The font with the two bytes at the offset changed.
    private static byte[] With(byte[] font, int offset, int value) {
        byte[] patched = (byte[]) font.Clone();
        patched[offset] = (byte) (value >> 8);
        patched[offset + 1] = (byte) value;
        return patched;
    }

    private static byte[] Truncated(byte[] font, int length) {
        byte[] shorter = new byte[length];
        Array.Copy(font, shorter, length);
        return shorter;
    }

    [Fact]
    public void AFontThatEndsWhereATableIsReadIsRefused() {
        byte[] font = FontBytes(THAI);
        // The directory, and then the tables it points at, are read from the
        // front of the font, so every one of these ends in the middle of a read.
        foreach (int length in new int[] {4, 5, 12, 13, 100, 1000}) {
            Assert.Equal("Invalid font file: the font ends too soon.",
                    Error(Truncated(font, length)));
        }
        Draws(font);
    }

    [Fact]
    public void AFontWithoutWhatItIsDrawnWithIsRefused() {
        byte[] ttf = FontBytes(THAI);
        byte[] otf = FontBytes(PLEX);
        int head = Table(ttf, "head");
        int hhea = Table(ttf, "hhea");

        // The name of the character map table, changed to one PDFjet does not
        // read, so that the font has none.
        byte[] noCmap = (byte[]) ttf.Clone();
        noCmap[Entry(ttf, "cmap")] = (byte) 'x';
        Assert.Equal("Invalid font file: no character map.", Error(noCmap));

        Assert.Equal("Invalid font file: the units per em.", Error(With(ttf, head + 18, 0)));
        Assert.Equal("Invalid font file: the units per em.", Error(With(ttf, head + 18, 15)));
        Assert.Equal("Invalid font file: the units per em.", Error(With(ttf, head + 18, 16385)));
        Assert.Equal("Invalid font file: no advance widths.", Error(With(ttf, hhea + 34, 0)));
        Assert.Equal("Invalid font file: the character map is not format 4.",
                Error(With(ttf, Cmap4(ttf), 6)));
        // The length of the CFF table, past the end of the font.
        Assert.Equal("Invalid font file: the CFF table is not in the font.",
                Error(With(otf, Entry(otf, "CFF ") + 12, 0x7FFF)));
        Draws(otf);
    }

    // The font with the table of the name spelled differently, so that
    // PDFjet does not read it and the font has none.
    private static byte[] Without(byte[] font, string name) {
        byte[] patched = (byte[]) font.Clone();
        patched[Entry(font, name)] = (byte) 'z';
        return patched;
    }

    [Fact]
    public void AFontWithNoNameOfItsOwnIsRefused() {
        // The name goes into the PDF as the name of the font, where a name
        // that is not a PDF name would break the syntax, so a font with none
        // is refused as a stream font with none is.
        Assert.Equal("Invalid font file: the font name.", Error(Without(FontBytes(THAI), "name")));
    }

    [Fact]
    public void AFontWithoutTheTablesItNeedsNoneOfStillDraws() {
        // Without OS/2 the font says it holds no characters and maps none of
        // them; without post it has no underline; without GPOS its marks are
        // not placed. None of the three stops it from drawing.
        byte[] font = FontBytes(THAI);
        foreach (string name in new string[] {"OS/2", "post", "GPOS"}) {
            Draws(Without(font, name));
        }
    }

    [Fact]
    public void ANameRecordOutsideTheFontIsLeftOut() {
        // The offset of the first name record, past the end of the font. The
        // font keeps its other records and still draws.
        byte[] font = FontBytes(THAI);
        Draws(With(font, Table(font, "name") + 16, 0xFFFF));
    }

    [Fact]
    public void ASegmentOutsideTheGlyphIDArrayGivesNoGlyph() {
        // The length of the format 4 subtable, cut to its header and the four
        // arrays of its segments, so that its glyph ID array holds nothing and
        // every segment that is read through it points outside.
        byte[] font = FontBytes(THAI);
        int subtable = Cmap4(font);
        Draws(With(font, subtable + 2, 16 + 4*UInt16(font, subtable + 6)));
    }

    [Fact]
    public void AGposTableIsNotReadPastTheWorkAFontNeeds() {
        // The lookups of the GPOS table, and the subtables of its first
        // lookup, changed to the most a font can say it has. Read to the end
        // it says, the font takes every byte of memory there is and never
        // loads; read to the work a font needs, it loads at once.
        byte[] font = FontBytes(THAI);
        int gpos = Table(font, "GPOS");
        int lookupList = gpos + UInt16(font, gpos + 8);
        int lookup = lookupList + UInt16(font, lookupList + 2);
        Draws(With(With(font, lookupList, 0xFFFF), lookup + 4, 0xFFFF));
    }

    // The cap height PDFjet reads of the font.
    private static int CapHeight(byte[] font) {
        return new OTF(new MemoryStream(font)).capHeight;
    }

    // The font with the length of the table of the name in its directory changed.
    private static byte[] Length(byte[] font, string name, int length) {
        int entry = Entry(font, name);
        return With(With(font, entry + 12, length >> 16), entry + 14, length & 0xFFFF);
    }

    [Fact]
    public void TheCapHeightIsReadOnlyFromAnOS2TableThatHasIt() {
        // The cap height of both fonts, sCapHeight in OS/2 and the top of the
        // H, is 714, so sCapHeight is changed to 999 to tell the two apart.
        // Noto Sans Thai has the short offsets of loca, and Noto Sans the long
        // ones.
        foreach (string path in new string[] {THAI, "fonts/NotoSans/NotoSans-Regular.ttf"}) {
            byte[] font = FontBytes(path);
            int os2 = Table(font, "OS/2");
            font = With(font, os2 + 88, 999);
            Assert.Equal(999, CapHeight(font));
            // A version 1 table ends before sCapHeight: the 999 is not its own.
            Assert.Equal(714, CapHeight(With(font, os2, 1)));
            // Nor is it of a version 2 table that ends before it.
            Assert.Equal(714, CapHeight(Length(font, "OS/2", 88)));
        }
    }

    [Fact]
    public void AFontWithoutTheCapHeightOrTheOutlineOfAnHHasItsAscent() {
        // IBM Plex Sans has CFF outlines, and so no glyf table to find the top
        // of the H in; its ascent is 1025. Noto Sans Thai without its loca
        // table has no offset for its H; its ascent is 1061.
        byte[] otf = FontBytes(PLEX);
        Assert.Equal(1025, CapHeight(With(otf, Table(otf, "OS/2"), 1)));
        byte[] ttf = FontBytes(THAI);
        ttf = With(ttf, Table(ttf, "OS/2"), 1);
        Assert.Equal(1061, CapHeight(Without(ttf, "loca")));
        // A loca table that ends before the offsets of the H, and a glyf table
        // that ends before the header of the H.
        Assert.Equal(1061, CapHeight(Length(ttf, "loca", 4)));
        Assert.Equal(1061, CapHeight(Length(ttf, "glyf", 4)));
    }
}
}   // End of namespace PDFjet.NET
