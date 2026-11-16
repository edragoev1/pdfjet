/*
 * ReviewNineOneNineTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The faults found by the review of 9.1.9 (16 November 2026), each kept from
/// coming back.
/// </summary>
public class ReviewNineOneNineTest {
    private const string CASCADIA = "tests/fonts/CascadiaMono-Regular.ttf";

    private static int Count(string text, string part) {
        int n = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i >= 0;
                i = text.IndexOf(part, i + 1, StringComparison.Ordinal)) {
            n++;
        }
        return n;
    }

    private static int U16(byte[] font, int at) {
        return font[at] << 8 | font[at + 1];
    }

    private static int U32(byte[] font, int at) {
        return font[at] << 24 | font[at + 1] << 16 | font[at + 2] << 8 | font[at + 3];
    }

    private static void PutU32(byte[] font, int at, int value) {
        font[at] = (byte) (value >> 24);
        font[at + 1] = (byte) (value >> 16);
        font[at + 2] = (byte) (value >> 8);
        font[at + 3] = (byte) value;
    }

    // Returns where the directory entry of the table begins.
    private static int Entry(byte[] font, string name) {
        for (int i = 0; i < U16(font, 4); i++) {
            int entry = 12 + 16 * i;
            if (Encoding.ASCII.GetString(font, entry, 4) == name) {
                return entry;
            }
        }
        throw new Exception("no " + name + " table");
    }

    // Returns the outline of the font with the glyphs given made the bytes
    // given, written after the end of the font, which the glyf table is made
    // to reach.
    private static GlyphOutline OutlineWithGlyphs(byte[] ttf, Dictionary<int, byte[]> glyphs) {
        int head = U32(ttf, Entry(ttf, "head") + 8);
        Assert.Equal(1, U16(ttf, head + 50));   // The long offsets of loca
        int locaOffset = U32(ttf, Entry(ttf, "loca") + 8);
        int glyfEntry = Entry(ttf, "glyf");
        int glyfOffset = U32(ttf, glyfEntry + 8);
        List<byte> data = new List<byte>(ttf);
        Dictionary<int, int> starts = new Dictionary<int, int>();
        foreach (KeyValuePair<int, byte[]> glyph in glyphs) {
            while ((data.Count - glyfOffset) % 4 != 0) {
                data.Add(0);
            }
            starts[glyph.Key] = data.Count - glyfOffset;
            data.AddRange(glyph.Value);
        }
        byte[] font = data.ToArray();
        foreach (KeyValuePair<int, byte[]> glyph in glyphs) {
            // The glyph after is not one of those given, so its start is kept
            int start = starts[glyph.Key];
            PutU32(font, locaOffset + 4 * glyph.Key, start);
            PutU32(font, locaOffset + 4 * glyph.Key + 4, start + glyph.Value.Length);
        }
        PutU32(font, glyfEntry + 12, font.Length - glyfOffset);
        return new GlyphOutline(font, false);
    }

    // Returns a composite glyph of the parts, each placed at 0 0.
    private static byte[] CompositeGlyph(params int[] parts) {
        List<byte> glyph = new List<byte> {0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0};    // -1 contours, the box
        for (int i = 0; i < parts.Length; i++) {
            int flags = 0x0002;     // ARGS_ARE_XY_VALUES, the arguments bytes
            if (i < parts.Length - 1) {
                flags |= 0x0020;    // MORE_COMPONENTS
            }
            glyph.Add((byte) (flags >> 8));
            glyph.Add((byte) flags);
            glyph.Add((byte) (parts[i] >> 8));
            glyph.Add((byte) parts[i]);
            glyph.Add(0);
            glyph.Add(0);
        }
        return glyph.ToArray();
    }

    // A composite glyph of four parts, each of four parts, 16 deep, down to an
    // O, was 4^16 Os read; the work of a glyph is bounded.
    [Fact]
    public void ATrueTypeCompositeGlyphOfManyPartsIsRefusedQuickly() {
        Font cascadia = new Font(TestSupport.NewPDF(), TestSupport.RepoPath(CASCADIA));
        int o = cascadia.unicodeToGID.Get('O');
        // Every other glyph of the last 40, so that the glyph after each keeps
        // its start, and none is the O
        Dictionary<int, byte[]> glyphs = new Dictionary<int, byte[]>();
        int below = o;
        int first = cascadia.advanceWidth.Length - 40;
        int gid = first;
        for (int level = 0; level < 16; level++) {
            glyphs[gid] = CompositeGlyph(below, below, below, below);
            below = gid;
            gid += 2;
        }
        GlyphOutline outline = OutlineWithGlyphs(cascadia.program.font, glyphs);
        // Two levels are drawn, as a font may have them
        Assert.Equal(32, Count(outline.Outline(first + 2).operators, " m\n"));
        Stopwatch watch = Stopwatch.StartNew();
        int top = below;
        Assert.Throws<ArgumentException>(() => outline.Outline(top));
        Assert.True(watch.ElapsedMilliseconds < 2000, watch.ElapsedMilliseconds + " ms");
    }

    // Returns the outline of IBM Plex Sans with the charstring of glyph 1 made
    // the bytes given.
    private static GlyphOutline CFFOutlineWithCharString(byte[] cs) {
        byte[] cff = TestSupport.PlexSans(TestSupport.NewPDF()).program.font;
        byte[] data = new byte[cff.Length + cs.Length];
        Array.Copy(cff, data, cff.Length);
        Array.Copy(cs, 0, data, cff.Length, cs.Length);
        GlyphOutline outline = new GlyphOutline(data, true);
        outline.charStrings.objects[1] = cff.Length;
        outline.charStrings.objects[2] = data.Length;
        return outline;
    }

    // A charstring's arithmetic made a point of 1.15e18 units, written as a
    // number of no meaning, or one Swift trapped on; a point past the bound
    // is a damaged glyph.
    [Fact]
    public void ACFFGlyphOfAPointFarOutIsRefused() {
        // 32767 dup mul dup mul 0 rmoveto 0 1 rlineto endchar
        byte[] cs = {0x1C, 0x7F, 0xFF, 0x0C, 0x1B, 0x0C, 0x18, 0x0C, 0x1B, 0x0C, 0x18, 0x8B, 0x15, 0x8B, 0x8C, 0x05, 0x0E};
        GlyphOutline outline = CFFOutlineWithCharString(cs);
        Assert.Throws<ArgumentException>(() => outline.Outline(1));
    }

    private static byte[] Concat(params byte[][] parts) {
        List<byte> all = new List<byte>();
        foreach (byte[] p in parts) {
            all.AddRange(p);
        }
        return all.ToArray();
    }

    // roll and index take their operands as numbers before integers, so that a
    // count of 2^31 is refused in the four ports alike.
    [Fact]
    public void ACFFRollOrIndexOfAnOperandTooLargeIsRefused() {
        // 2^31 as 32767 1 add 65536 mul, with 65536 as 32767 2 mul 2 add
        byte[] big = {0x1C, 0x7F, 0xFF, 0x8C, 0x0C, 0x0A, 0x1C, 0x7F, 0xFF, 0x8D, 0x0C, 0x18, 0x8D, 0x0C, 0x0A, 0x0C, 0x18};
        Dictionary<string, byte[]> ops = new Dictionary<string, byte[]> {
            {"roll", Concat(new byte[] {0x8C, 0x8D, 0x8E, 0x8E}, big, new byte[] {0x0C, 0x1E})},
            {"index", Concat(new byte[] {0x8C}, big, new byte[] {0x0C, 0x1D})},
        };
        foreach (KeyValuePair<string, byte[]> op in ops) {
            byte[] cs = Concat(op.Value, new byte[] {0x8B, 0x8B, 0x15, 0x0E});  // 0 0 rmoveto endchar
            GlyphOutline outline = CFFOutlineWithCharString(cs);
            bool refused = false;
            try {
                outline.Outline(1);
            } catch (ArgumentException) {
                refused = true;
            }
            Assert.True(refused, op.Key + ": no error");
        }
    }

    // A position of a glyph not finite, or far off the page, is an error, not
    // a number of no meaning in the content.
    [Fact]
    public void DrawGlyphsAPositionNotFiniteIsAnError() {
        foreach (float x in new float[] {float.PositiveInfinity, float.NaN, 2e6f}) {
            PDF pdf = new PDF(new MemoryStream());
            Font font = TestSupport.PlexSans(pdf);
            Page page = new Page(pdf, Letter.PORTRAIT);
            ArgumentException e = Assert.Throws<ArgumentException>(
                    () => page.DrawGlyphs(font, new int[] {36}, new float[] {x, 100f}, "A", null));
            Assert.Contains("positions between", e.Message);
        }
    }

    // The header of a BigTable whose header and rows have one font took the
    // rows' fallback font: the header has its own, and the rows theirs.
    [Fact]
    public void BigTableOfOneFontGivesTheHeaderItsOwnFallbackFont() {
        PDF pdf = TestSupport.NewPDF();
        Font plex = TestSupport.PlexSans(pdf);
        Font kr = new Font(pdf, IBMPlexSansKR.Regular);
        BigTable bt = new BigTable(pdf, plex, plex, Letter.PORTRAIT).SetFallbackFonts(kr, null);
        Assert.Same(kr, bt.MeasureOf(true).fallback);
        Assert.Null(bt.MeasureOf(false).fallback);
        bt = new BigTable(pdf, plex, plex, Letter.PORTRAIT).SetFallbackFonts(null, kr);
        Assert.Null(bt.MeasureOf(true).fallback);
        Assert.Same(kr, bt.MeasureOf(false).fallback);
    }

    // Two lines that meet end to end, or edge to edge, may paint one pixel, the
    // hairline of width 0 most of all: they are stroked in the order of the
    // cells, or the color on top of that pixel changed when the borders were
    // joined.
    [Fact]
    public void TableLinesThatMeetOverlap() {
        TableLines.Segment black = new TableLines.Segment(true, 50f, 332.94f, 350.82f, 0f, 0f, 0f, 0f, 0f);
        TableLines.Segment blue = new TableLines.Segment(true, 50f, 350.82f, 368.69f, 0f, 0f, 1f, 0f, 0f);
        Assert.True(black.Overlaps(blue) && blue.Overlaps(black), "hairlines that meet end to end do not overlap");
        TableLines.Segment red = new TableLines.Segment(false, 100f, 50f, 148f, 1f, 0f, 0f, 3f, 0f);
        TableLines.Segment hairline = new TableLines.Segment(true, 148f, 100f, 120f, 0f, 0f, 0f, 0f, 0f);
        Assert.True(red.Overlaps(hairline), "a hairline at the end of a wide line does not overlap it");
        TableLines.Segment far = new TableLines.Segment(true, 160f, 100f, 120f, 0f, 0f, 0f, 0f, 0f);
        Assert.False(red.Overlaps(far), "lines 10 points apart overlap");
    }

    // A heading, a rule and a table: the rule is not a paragraph to measure,
    // and Java and C# threw on its text, which is none.
    [Fact]
    public void MarkdownHeadingRuleAndTableAreDrawn() {
        StringBuilder words = new StringBuilder();
        for (int i = 0; i < 1500; i++) {
            words.Append("word ");
        }
        string md = "# " + words + "\n\n---\n\n| a | b |\n|---|---|\n| 1 | 2 |\n";
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Markdown");
        Markdown markdown = new Markdown(
                new Font(pdf, CoreFont.HELVETICA),
                new Font(pdf, CoreFont.HELVETICA_BOLD),
                new Font(pdf, CoreFont.HELVETICA_OBLIQUE),
                new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE),
                new Font(pdf, CoreFont.COURIER));
        List<Page> pages = new List<Page>();
        markdown.DrawOn(pdf, md, pages, Letter.PORTRAIT);
        StringBuilder text = new StringBuilder();
        foreach (Page page in pages) {
            text.Append(TestSupport.Content(page));
        }
        Assert.True(pages.Count >= 2, pages.Count + " pages");
        Assert.Contains("a", text.ToString());
    }

    // Drawing on a page of an existing PDF that has no content: the content
    // restored a graphics state it never saved, "Q" first.
    [Fact]
    public void DrawingOnAnEmptyPageOfAnExistingPDFIsBalanced() {
        List<PDFobj> objects = TestSupport.Read(PDFTest.PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"}));
        PDF pdf = new PDF(new MemoryStream());
        Page page = new Page(pdf, pdf.GetPageObjects(objects)[0]);
        page.DrawRect(10f, 10f, 20f, 20f);
        page.Complete(objects);
        foreach (PDFobj obj in objects) {
            if (obj.stream == null) {
                continue;
            }
            string content = TestSupport.Latin1(obj.stream);
            if (content.Contains(" l\n")) {
                Assert.StartsWith("q\nQ\n", content);
                return;
            }
        }
        throw new Exception("no content drawn");
    }
}
}   // End of ReviewNineOneNineTest.cs
