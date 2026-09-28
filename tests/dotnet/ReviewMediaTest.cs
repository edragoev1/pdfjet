/*
 * ReviewMediaTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
public class ReviewMediaTest {
    private const string THAI = "fonts/NotoSansThai/NotoSansThai-Regular.ttf";

    private static byte[] FontBytes(string path) {
        return File.ReadAllBytes(TestSupport.RepoPath(path));
    }

    private static int UInt16(byte[] font, int offset) {
        return (font[offset] << 8) | font[offset + 1];
    }

    private static void Put32(byte[] buf, int offset, int value) {
        buf[offset] = (byte) (value >> 24);
        buf[offset + 1] = (byte) (value >> 16);
        buf[offset + 2] = (byte) (value >> 8);
        buf[offset + 3] = (byte) value;
    }

    // Where the directory entry of the table of the name begins.
    private static int Entry(byte[] font, string name) {
        for (int i = 0; i < UInt16(font, 4); i++) {
            int entry = 12 + 16*i;
            if (Encoding.UTF8.GetString(font, entry, 4).Equals(name)) {
                return entry;
            }
        }
        throw new ArgumentException("the font has no " + name + " table");
    }

    // The font with the table of the name spelled differently, so that
    // PDFjet does not read it and the font has none.
    private static byte[] Without(byte[] font, string name) {
        byte[] patched = (byte[]) font.Clone();
        patched[Entry(font, name)] = (byte) 'z';
        return patched;
    }

    // The font with the table of the name replaced by the bytes, which are
    // put at the end of the font.
    private static byte[] WithTable(byte[] font, string name, byte[] table) {
        int entry = Entry(font, name);
        byte[] patched = new byte[font.Length + table.Length];
        Array.Copy(font, patched, font.Length);
        Array.Copy(table, 0, patched, font.Length, table.Length);
        Put32(patched, entry + 8, font.Length);
        Put32(patched, entry + 12, table.Length);
        return patched;
    }

    // Loads the font and draws with it.
    private static void Draws(byte[] font) {
        PDF pdf = TestSupport.NewPDF();
        Font f = new Font(pdf, new MemoryStream(font));
        new TextLine(f, "Ab1 กิ่ x").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
    }

    private sealed class Bytes {
        private readonly MemoryStream ms = new MemoryStream();

        internal Bytes Put16(int value) {
            ms.WriteByte((byte) (value >> 8));
            ms.WriteByte((byte) value);
            return this;
        }

        internal Bytes Put32(int value) {
            return Put16(value >> 16).Put16(value);
        }

        internal Bytes Zeros(int count) {
            ms.Write(new byte[count], 0, count);
            return this;
        }

        internal byte[] ToArray() {
            return ms.ToArray();
        }
    }

    // A GPOS table of one lookup, of the given number of MarkToBase subtables
    // that are all the same one: its marks and its letters are a coverage
    // table of format 2 with one range, of glyph 0 alone, but at the coverage
    // index 65535, and its letters have no classes of marks.
    private static byte[] GposBomb(int subtables) {
        Bytes gpos = new Bytes();
        gpos.Put32(0x00010000);
        gpos.Put16(0).Put16(0);     // The script and the feature lists
        gpos.Put16(10);             // The lookup list
        gpos.Put16(1).Put16(4);     // One lookup, at 4 from the lookup list
        gpos.Put16(4).Put16(0);     // MarkToBase, no flags
        gpos.Put16(subtables);
        for (int i = 0; i < subtables; i++) {
            gpos.Put16(6 + 2*subtables);    // Every subtable is the one after the lookup
        }
        gpos.Put16(1);              // Format 1
        gpos.Put16(12).Put16(12);   // The marks and the letters
        gpos.Put16(0);              // No classes of marks
        gpos.Put16(22).Put16(22);   // The mark and the letter arrays
        gpos.Put16(2).Put16(1);     // A coverage table of format 2, one range
        gpos.Put16(0).Put16(0);     // Of glyph 0 alone
        gpos.Put16(65535);
        return gpos.Zeros(16).ToArray();
    }

    [Fact]
    public void AGposTableOfSubtablesThatAreOneAnotherLoadsAtOnce() {
        // The coverage index of the one glyph makes room for 65,536 glyphs,
        // and each of the 20,000 subtables went through all of them: 40 KB of
        // GPOS table kept a font from loading for a minute.
        byte[] font = WithTable(FontBytes(THAI), "GPOS", GposBomb(20000));
        Stopwatch watch = Stopwatch.StartNew();
        Draws(font);
        Assert.True(watch.ElapsedMilliseconds < 2000, "the font loads in " + watch.ElapsedMilliseconds + " ms");
    }

    // A character map of a format 4 subtable alone, for the Windows platform,
    // of the segments of the start and end codes, each of delta 0, followed by
    // the segment of 0xFFFF that ends every table.
    private static byte[] CmapOfSegments(int[] starts, int[] ends) {
        int segments = starts.Length + 1;
        Bytes cmap = new Bytes();
        cmap.Put16(0).Put16(1);     // The version, one encoding record
        cmap.Put16(3).Put16(1);     // Windows, Unicode BMP
        cmap.Put32(12);
        cmap.Put16(4);              // Format 4
        cmap.Put16(16 + 8*segments);
        cmap.Put16(0);              // The language
        cmap.Put16(2*segments);
        cmap.Put16(0).Put16(0).Put16(0);
        foreach (int end in ends) {
            cmap.Put16(end);
        }
        cmap.Put16(0xFFFF);
        cmap.Put16(0);              // The reserved pad
        foreach (int start in starts) {
            cmap.Put16(start);
        }
        cmap.Put16(0xFFFF);
        return cmap.Zeros(4*segments).ToArray();     // The deltas and the range offsets
    }

    [Fact]
    public void TheCharacterMapIsReadInOnePassOverItsSegments() {
        // 32,766 segments of the character 0xFFFE alone, before the last one:
        // a search of every segment for every character took 2^31
        // comparisons. The font has no OS/2 table, so that every character is
        // looked up.
        byte[] font = Without(FontBytes(THAI), "OS/2");
        int[] starts = new int[32766];
        int[] ends = new int[32766];
        for (int i = 0; i < starts.Length; i++) {
            starts[i] = 0xFFFE;
            ends[i] = 0xFFFE;
        }
        Stopwatch watch = Stopwatch.StartNew();
        OTF otf = new OTF(new MemoryStream(WithTable(font, "cmap", CmapOfSegments(starts, ends))));
        Assert.True(watch.ElapsedMilliseconds < 2000, "the font loads in " + watch.ElapsedMilliseconds + " ms");
        // The first segment of the character is the one it is in.
        Assert.Equal(0xFFFE, otf.unicodeToGID[0xFFFE]);
        Assert.Equal(0, otf.unicodeToGID['A']);
        // Segments of 'A' to 'C' and 'X' to 'Z', with a delta of 0, map each
        // character to the glyph of its code, and none between them.
        otf = new OTF(new MemoryStream(WithTable(font, "cmap",
                CmapOfSegments(new int[] {'A', 'X'}, new int[] {'C', 'Z'}))));
        int[,] glyphs = {{'@', 0}, {'A', 'A'}, {'C', 'C'}, {'D', 0}, {'W', 0}, {'X', 'X'}, {'Z', 'Z'}, {'[', 0}};
        for (int i = 0; i < glyphs.GetLength(0); i++) {
            Assert.Equal(glyphs[i, 1], otf.unicodeToGID[glyphs[i, 0]]);
        }
    }

    [Fact]
    public void AFontWithoutAnOS2TableHasItsCharacters() {
        // The OS/2 table says the first and the last character of the font; a
        // font without one has every character of its character map, where it
        // had none and drew every character as .notdef.
        byte[] font = FontBytes(THAI);
        OTF with = new OTF(new MemoryStream(font));
        OTF without = new OTF(new MemoryStream(Without(font, "OS/2")));
        foreach (int ch in new int[] {'A', 'z', 'ก'}) {
            Assert.NotEqual(0, without.unicodeToGID[ch]);
            Assert.Equal(with.unicodeToGID[ch], without.unicodeToGID[ch]);
        }
    }

    // The message the image of the file fails a document of the compliance
    // with, or "" when the document holds it.
    private static string PDFAError(Compliance compliance, string path) {
        PDF pdf = new PDF(new MemoryStream(), compliance);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, TestSupport.RepoPath(THAI));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Text").SetLocation(50f, 50f).DrawOn(page);
        try {
            new Image(pdf, TestSupport.RepoPath(path)).SetAltDescription("An image")
                    .SetLocation(50f, 100f).DrawOn(page);
            pdf.Complete();
        } catch (InvalidOperationException e) {
            return e.Message;
        }
        return "";
    }

    [Fact]
    public void APDFADocumentHoldsNoImageItsLevelHasNot() {
        // The output intent of PDF/A is sRGB, so its images are not CMYK;
        // PDF/A-1 has no soft masks, and 8 bits per component at most.
        Assert.Equal("A document of PDF_A_2B cannot hold a CMYK image: "
                + "its output intent is sRGB, so its images are gray or RGB.",
                PDFAError(Compliance.PDF_A_2B, "images/cmyk.jpg"));
        Assert.Equal("A document of PDF_A_1B cannot hold an image with transparency: "
                + "PDF/A-1 has no soft masks, so its images are opaque.",
                PDFAError(Compliance.PDF_A_1B, "PngSuite/BASN6A08.PNG"));
        Assert.Equal("A document of PDF_A_1A cannot hold an image of 16 bits per component: "
                + "PDF/A-1 has 8 at most.",
                PDFAError(Compliance.PDF_A_1A, "PngSuite/BASN2C16.PNG"));
        // PDF/A-2 and PDF/A-3 hold both, and a document that is not PDF/A all three.
        Assert.Equal("", PDFAError(Compliance.PDF_A_2B, "PngSuite/BASN6A08.PNG"));
        Assert.Equal("", PDFAError(Compliance.PDF_A_3B, "PngSuite/BASN2C16.PNG"));
        Assert.Equal("", PDFAError(Compliance.PDF_UA_1, "images/cmyk.jpg"));
        Assert.Equal("", PDFAError(Compliance.PDF_A_1B, "PngSuite/BASN2C08.PNG"));
    }

    private static string DrawSVG(string svg) {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        SVGImage image = new SVGImage(new MemoryStream(Encoding.UTF8.GetBytes(svg)));
        image.SetLocation(0f, 0f);
        image.DrawOn(page);
        return TestSupport.Content(page);
    }

    private static string Repeat(string text, int count) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++) {
            sb.Append(text);
        }
        return sb.ToString();
    }

    [Fact]
    public void TheCommentsOfAStyleSheetAreLeftOutInOnePass() {
        string svg = "<svg width=\"10\" height=\"10\"><style>" + Repeat("/**/", 40000)
                + ".a { fill: /* red */ blue } /* .a { fill: red } */ .b { fill: green } /* not closed .a { fill: red }"
                + "</style><rect class=\"a\" width=\"5\" height=\"5\"/></svg>";
        Stopwatch watch = Stopwatch.StartNew();
        string content = DrawSVG(svg);
        Assert.True(watch.ElapsedMilliseconds < 2000, "the style sheet is read in " + watch.ElapsedMilliseconds + " ms");
        Assert.StartsWith("0 0 1 rg\n", content);
    }

    [Fact]
    public void TheRulesOfTheClassesOfAnElementAreFoundByClass() {
        // 20,000 rules and 20,000 elements of three classes each: every
        // element went through every rule for each of its classes.
        StringBuilder sb = new StringBuilder("<svg width=\"10\" height=\"10\"><style>");
        for (int i = 0; i < 20000; i++) {
            sb.Append(".c").Append(i).Append("{fill:red}");
        }
        sb.Append("</style>").Append(Repeat("<g class=\"x y z\"/>", 20000)).Append("</svg>");
        Stopwatch watch = Stopwatch.StartNew();
        new SVGImage(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())));
        Assert.True(watch.ElapsedMilliseconds < 2000, "the image is read in " + watch.ElapsedMilliseconds + " ms");
        // The rules are those of the style sheet, in its order, whatever the
        // order of the classes, and a class named twice has its rules once.
        string content = DrawSVG("<svg width=\"10\" height=\"10\"><style>.b{fill:red} .a{fill:blue} .b{stroke:green}"
                + "</style><rect class=\"b a b\" width=\"5\" height=\"5\"/></svg>");
        Assert.StartsWith("0 0 1 rg\n", content);
        Assert.Contains("0 0.5 0 RG\n", content);
    }

    // The first control points of the cubic curves of the path data, rounded
    // to two decimals.
    private static string FirstControlPoints(string data) {
        List<string> points = new List<string>();
        foreach (PathOp op in SVG.ToPDF(SVG.GetOperations(data))) {
            if (op.cmd == 'C') {
                points.Add(op.x1.ToString("F2", CultureInfo.InvariantCulture) + ","
                        + op.y1.ToString("F2", CultureInfo.InvariantCulture));
            }
        }
        return String.Join(" ", points);
    }

    [Fact]
    public void ASmoothCurveReflectsTheControlPointOfACurveOfItsKind() {
        // T reflects the control point of the quadratic curve before, Q or T,
        // and else starts from the current point; S reflects the second
        // control point of the cubic curve before, C or S, and else starts
        // from the current point. The first control point of a cubic curve
        // made of a quadratic one is two thirds of the way to the quadratic
        // control point.
        string[,] cases = {
            // The second T reflects (15, -10), the control point of the first.
            {"M0 0 Q 5 10 10 0 T 20 0 T 30 0", "3.33,6.67 13.33,-6.67 23.33,6.67"},
            {"M0 0 Q 5 10 10 0 t 10 0 t 10 0", "3.33,6.67 13.33,-6.67 23.33,6.67"},
            {"M0 0 C 0 10 10 10 10 0 T 20 0", "0.00,10.00 10.00,0.00"},
            {"M0 0 Q 5 10 10 0 S 20 10 20 0", "3.33,6.67 10.00,0.00"},
            {"M0 0 C 0 10 10 10 10 0 S 20 -10 20 0", "0.00,10.00 10.00,-10.00"},
            {"M0 0 S 10 10 20 0 S 30 -10 40 0", "0.00,0.00 30.00,-10.00"},
        };
        for (int i = 0; i < cases.GetLength(0); i++) {
            Assert.True(cases[i, 1] == FirstControlPoints(cases[i, 0]),
                    cases[i, 0] + ": " + FirstControlPoints(cases[i, 0]));
        }
    }

    [Fact]
    public void ACommandAfterZStartsAtTheStartOfTheSubpathItClosed() {
        // A line after Z, with no moveto, starts where the closed subpath did;
        // the path is stroked once, and filled with every subpath in place.
        string content = DrawSVG("<svg width=\"100\" height=\"100\">"
                + "<path d=\"M10 10 L50 10 L50 50 Z L 90 90\" fill=\"red\" stroke=\"black\"/></svg>");
        Assert.Equal("1 0 0 rg\n10 782 m\n50 782 l\n50 742 l\n10 782 m\n90 702 l\nf\n"
                + "0 0 0 RG\n1 w\n10 782 m\n50 782 l\n50 742 l\nh\n10 782 m\n90 702 l\nS\n", content);
    }

    private static void WriteChunk(MemoryStream ms, string type, byte[] data) {
        byte[] name = Encoding.ASCII.GetBytes(type);
        CRC32 crc = new CRC32();
        crc.Update(name, 0, 4);
        crc.Update(data, 0, data.Length);
        byte[] length = new byte[4];
        Put32(length, 0, data.Length);
        ms.Write(length);
        ms.Write(name);
        ms.Write(data);
        byte[] checksum = new byte[4];
        Put32(checksum, 0, unchecked((int) crc.GetValue()));
        ms.Write(checksum);
    }

    // A PNG file with the IHDR of the size, bit depth and color type, and the
    // image data in IDAT chunks of the given size, or one when it is 0.
    private static byte[] Png(int width, int height, int bitDepth, int colorType, byte[] idat, int chunkSize = 0) {
        MemoryStream ms = new MemoryStream();
        ms.Write(new byte[] {0x89, (byte) 'P', (byte) 'N', (byte) 'G', (byte) '\r', (byte) '\n', 0x1A, (byte) '\n'});
        byte[] ihdr = new byte[13];
        Put32(ihdr, 0, width);
        Put32(ihdr, 4, height);
        ihdr[8] = (byte) bitDepth;
        ihdr[9] = (byte) colorType;
        WriteChunk(ms, "IHDR", ihdr);
        if (chunkSize == 0) {
            chunkSize = Math.Max(idat.Length, 1);
        }
        for (int i = 0; i < idat.Length; i += chunkSize) {
            byte[] chunk = new byte[Math.Min(chunkSize, idat.Length - i)];
            Array.Copy(idat, i, chunk, 0, chunk.Length);
            WriteChunk(ms, "IDAT", chunk);
        }
        WriteChunk(ms, "IEND", new byte[0]);
        return ms.ToArray();
    }

    [Fact]
    public void AnUnknownPNGFilterTypeIsRefused() {
        // Filter types 0 to 4 are all PNG defines; libpng refuses a row of
        // another one, which was read as if it had no filter.
        byte[] png = Png(2, 1, 8, 2, Compressor.Deflate(new byte[] {5, 1, 2, 3, 4, 5, 6}));
        Assert.Equal("Invalid PNG filter type 5.",
                Assert.ThrowsAny<Exception>(() => new PNGImage(new MemoryStream(png))).Message);
    }

    [Fact]
    public void ThePNGFiltersAreUndone() {
        // Two rows of two RGB pixels, the second row with each filter, over a
        // first row of Sub.
        byte[] first = {1, 10, 20, 30, 5, 5, 5};
        byte[] want = {10, 20, 30, 15, 25, 35};
        byte[][] rows = {
            new byte[] {0, 1, 2, 3, 4, 5, 6}, new byte[] {1, 2, 3, 4, 5, 6},
            new byte[] {1, 1, 2, 3, 4, 5, 6}, new byte[] {1, 2, 3, 5, 7, 9},
            new byte[] {2, 1, 2, 3, 4, 5, 6}, new byte[] {11, 22, 33, 19, 30, 41},
            // (left + above) / 2, with the left one of the first pixel 0.
            new byte[] {3, 1, 2, 3, 4, 5, 6}, new byte[] {6, 12, 18, 14, 23, 32},
            // The first pixel takes the one above, as it is nearest; so does
            // the second, of left 6, above 15 and above on the left 10.
            new byte[] {4, 1, 2, 3, 4, 5, 6}, new byte[] {11, 22, 33, 19, 30, 41},
        };
        for (int i = 0; i < rows.Length; i += 2) {
            byte[] data = new byte[first.Length + rows[i].Length];
            first.CopyTo(data, 0);
            rows[i].CopyTo(data, first.Length);
            PNGImage png = new PNGImage(new MemoryStream(Png(2, 2, 8, 2, Compressor.Deflate(data))));
            byte[] expected = new byte[want.Length + rows[i + 1].Length];
            want.CopyTo(expected, 0);
            rows[i + 1].CopyTo(expected, want.Length);
            Assert.Equal(expected, Decompressor.Inflate(png.GetData()));
        }
    }

    [Fact]
    public void TheDataOfManyIDATChunksIsJoinedInOrder() {
        // Each IDAT chunk holds 7 bytes of the image data, which is read as
        // one stream across them.
        byte[] rows = new byte[64 * (1 + 3*16)];
        for (int i = 0; i < rows.Length; i++) {
            rows[i] = (i % 49 == 0) ? (byte) 0 : (byte) i;
        }
        byte[] idat = Compressor.Deflate(rows);
        byte[] one = Decompressor.Inflate(new PNGImage(new MemoryStream(Png(16, 64, 8, 2, idat))).GetData());
        byte[] many = Decompressor.Inflate(new PNGImage(new MemoryStream(Png(16, 64, 8, 2, idat, 7))).GetData());
        Assert.Equal(one, many);
        Assert.Equal(64 * 3*16, many.Length);
    }

    [Fact]
    public void APhysicalSizeOfMorePixelsThanAPNGNumberHoldsIsPassedOver() {
        // The numbers of a PNG chunk are 2^31 - 1 at most; one past it drew
        // the image a thousandth of a point wide.
        foreach (uint ppm in new uint[] {0x80000000, 0xFFFFFFFF}) {
            MemoryStream ms = new MemoryStream();
            ms.Write(new byte[] {0x89, (byte) 'P', (byte) 'N', (byte) 'G', (byte) '\r', (byte) '\n', 0x1A, (byte) '\n'});
            byte[] ihdr = new byte[13];
            Put32(ihdr, 0, 8);
            Put32(ihdr, 4, 8);
            ihdr[8] = 8;
            ihdr[9] = 2;
            WriteChunk(ms, "IHDR", ihdr);
            byte[] phys = new byte[9];
            Put32(phys, 0, unchecked((int) ppm));
            Put32(phys, 4, 4724);
            phys[8] = 1;
            WriteChunk(ms, "pHYs", phys);
            WriteChunk(ms, "IDAT", Compressor.Deflate(new byte[8*(1 + 3*8)]));
            WriteChunk(ms, "IEND", new byte[0]);
            PNGImage png = new PNGImage(new MemoryStream(ms.ToArray()));
            Assert.Equal(0f, png.GetPhysicalWidth());
            Assert.Equal(0f, png.GetPhysicalHeight());
        }
    }

    private static byte[] Jpeg(int sof) {
        return new byte[] {0xFF, 0xD8, 0xFF, (byte) sof, 0x00, 0x11, 8, 0, 8, 0, 8, 3,
                1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0, 0xFF, 0xD9};
    }

    [Fact]
    public void AJPEGAReaderCannotDecodeIsRefused() {
        // A lossless, a hierarchical or an arithmetic coded JPEG is not one
        // the DCTDecode filter of a PDF reader decodes.
        foreach (int sof in new int[] {0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF}) {
            Exception e = Assert.ThrowsAny<Exception>(() => new JPGImage(new MemoryStream(Jpeg(sof))));
            Assert.Equal("Error: The JPEG is lossless, hierarchical or arithmetic coded (SOF"
                    + (sof - 0xC0) + "), which a PDF reader cannot decode.", e.Message);
        }
        foreach (int sof in new int[] {0xC0, 0xC1, 0xC2}) {
            new JPGImage(new MemoryStream(Jpeg(sof)));
        }
    }

    [Fact]
    public void TheImageObjectHasEveryPixelOfAWideImage() {
        // A float holds every whole number only up to 2^24, and 2^24 + 1
        // pixels were written as 2^24.
        int width = (1 << 24) + 1;
        byte[] png = Png(width, 1, 1, 0, Compressor.Deflate(new byte[1 + (width + 7)/8]));
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Image image = new Image(pdf, new MemoryStream(png));
        new Page(pdf, Letter.PORTRAIT);
        image.SetLocation(0f, 0f);
        pdf.Complete();
        Assert.Contains("/Width 16777217\n", TestSupport.Latin1(stream.ToArray()));
    }

    [Fact]
    public void TheLinkOfATurnedImageCoversItAsItIsDrawn() {
        // The link covered the image as it is drawn unturned, which a quarter
        // turn makes as wide as it was tall.
        foreach (int degrees in new int[] {0, 90, 180, 270}) {
            MemoryStream stream = new MemoryStream();
            PDF pdf = new PDF(stream);
            Page page = new Page(pdf, Letter.PORTRAIT);
            Image image = new Image(pdf, TestSupport.RepoPath("images/GLA250.png"));
            image.SetRotation(degrees).SetURIAction("https://pdfjet.com").SetLocation(10f, 20f);
            image.DrawOn(page);
            pdf.Complete();
            Match match = Regex.Match(TestSupport.Latin1(stream.ToArray()),
                    @"/Rect \[([\d.]+) ([\d.]+) ([\d.]+) ([\d.]+)\]");
            Assert.True(match.Success, degrees + ": no link");
            double[] rect = new double[4];
            for (int i = 0; i < 4; i++) {
                rect[i] = double.Parse(match.Groups[i + 1].Value, CultureInfo.InvariantCulture);
            }
            double w = image.GetWidth();
            double h = image.GetHeight();
            if (degrees == 90 || degrees == 270) {
                (w, h) = (h, w);
            }
            Assert.Equal(10.0, rect[0]);
            Assert.Equal(792.0 - 20.0, rect[1]);
            Assert.True(Math.Abs(rect[2] - rect[0] - w) <= 0.01, degrees + ": /Rect " + String.Join(" ", rect));
            Assert.True(Math.Abs(rect[1] - rect[3] - h) <= 0.01, degrees + ": /Rect " + String.Join(" ", rect));
        }
    }

    [Fact]
    public void TextFitsInAnyWidthAtAFontSizeOfZero() {
        // Text of no size has no width; the size divided the width.
        Font font = new Font(TestSupport.NewPDF(), TestSupport.RepoPath(THAI));
        foreach (Font f in new Font[] {TestSupport.Helvetica(TestSupport.NewPDF()), font}) {
            f.SetSize(0f);
            Assert.Equal(5, f.GetFitChars("Hello", 10f));
            Assert.Equal(0, f.GetFitChars("Hello", -1f));
        }
    }

    [Fact]
    public void AFontIsEmbeddedOnceWhenItIsAddedTwice() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font1 = new Font(pdf, TestSupport.RepoPath(THAI));
        Font font2 = new Font(pdf, TestSupport.RepoPath(THAI));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font1, "A").SetLocation(50f, 50f).DrawOn(page);
        new TextLine(font2, "B").SetLocation(50f, 80f).DrawOn(page);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, (raw.Length - raw.Replace("/Length1 ", "").Length) / "/Length1 ".Length);
    }
}
}
