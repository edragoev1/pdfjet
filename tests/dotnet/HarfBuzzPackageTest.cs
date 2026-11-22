/*
 * HarfBuzzPackageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace PDFjet.NET {
// The NuGet package PDFjet.HarfBuzz, which this project references as a
// program adds it: ComplexText's shaper when none is set, and the same lines
// as hb-shape's.
public class HarfBuzzPackageTest {
    private const string WORLD = "resources/fonts/world/";
    private static readonly string[][] LANGUAGES = {
        new[] {"Hindi", "hi", WORLD + "NotoSansDevanagari/NotoSansDevanagari-Regular.otf", "hin"},
        new[] {"Khmer", "km", WORLD + "NotoSansKhmer/NotoSansKhmer-Regular.otf", "khm"},
        new[] {"Lao", "lo", WORLD + "NotoSansLao/NotoSansLao-Regular.otf", "lao"},
        new[] {"Burmese", "my", WORLD + "NotoSansMyanmar/NotoSansMyanmar-Regular.otf", "mya"},
    };

    private readonly ITestOutputHelper output;

    public HarfBuzzPackageTest(ITestOutputHelper output) {
        this.output = output;
    }

    private static List<string> Paragraphs(string code) {
        List<string> list = new List<string>();
        foreach (string line in File.ReadAllLines(TestSupport.RepoPath("resources/data/udhr/udhr_" + code + ".txt"), Encoding.UTF8)) {
            if (line.Trim().Length > 0) {
                list.Add(line.Trim());
            }
        }
        return list;
    }

    // Found by its name: ComplexText's shaper when none is set, hb-shape or not
    [Fact]
    public void IsTheShaperWhenNoneIsSet() {
        Assert.NotNull(ComplexText.HarfBuzzPackage);
        PDF pdf = new PDF(new MemoryStream());
        Font khmer = new Font(pdf, TestSupport.RepoPath(LANGUAGES[1][2]));
        khmer.SetSize(12f);
        float[] xy = new ComplexText(khmer, Paragraphs("khm").GetRange(0, 3)).SetLanguage("km").SetWidth(300f)
                .SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        Assert.True(xy[1] > 50 + 3 * 12, "drawn down to " + xy[1]);
        pdf.Complete();
    }

    // The same lines as hb-shape's, glyph for glyph and place for place, on
    // the Universal Declaration of Human Rights in four languages, with IBM
    // Plex Sans for what the script's font has not
    [Fact]
    public void ShapesAsHBShape() {
        HBShape hbShape = new HBShape();
        try {
            hbShape.GetVersion();
        } catch (InvalidOperationException) {
            return;     // hb-shape is not installed
        }
        IShaper package = ComplexText.HarfBuzzPackage;
        Assert.NotNull(package);
        byte[] latin = File.ReadAllBytes(TestSupport.RepoPath("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"));
        foreach (string[] l in LANGUAGES) {
            byte[][] files = {File.ReadAllBytes(TestSupport.RepoPath(l[2])), latin};
            int count = 0;
            foreach (string paragraph in Paragraphs(l[3])) {
                ShapingRequest request = new ShapingRequest(paragraph, l[1], files, 10.5f, 512f);
                List<ShapedLine> got = package.Shape(request);
                List<ShapedLine> want = hbShape.Shape(request);
                Assert.Equal(want.Count, got.Count);
                for (int i = 0; i < want.Count; i++) {
                    Assert.Equal(want[i].Start, got[i].Start);
                    Assert.Equal(want[i].End, got[i].End);
                    Assert.Equal(want[i].FontIndexes, got[i].FontIndexes);
                    Assert.Equal(want[i].Glyphs, got[i].Glyphs);
                    Assert.Equal(want[i].Positions, got[i].Positions);
                    Assert.Equal(want[i].Width, got[i].Width);
                }
                count += got.Count;
            }
            output.WriteLine(l[0] + ": " + count + " lines alike");
        }
    }


    // The engine keeps the fonts of the last documents only, never all of a
    // server's, each of its own array (ultra review, 22 November 2026)
    [Fact]
    public void KeepsTheLastFontsOnly() {
        byte[] file = File.ReadAllBytes(TestSupport.RepoPath(LANGUAGES[1][2]));
        PDFjet.HarfBuzz.HarfBuzzEngine engine = new PDFjet.HarfBuzz.HarfBuzzEngine();
        for (int i = 0; i < 30; i++) {
            engine.ShapeRuns((byte[]) file.Clone(), new[] {"ក"}, "km");
            Assert.True(engine.FontsKept <= 8, "after " + (i + 1) + ": " + engine.FontsKept);
        }
    }

    // Hebrew in a paragraph, of a fallback font, shaped left to right, as
    // ComplexText draws, by the package and hb-shape alike, and the lines
    // broken without an error (ultra review, 22 November 2026)
    [Fact]
    public void HebrewInAParagraphIsShapedLeftToRight() {
        byte[][] files = {
            File.ReadAllBytes(TestSupport.RepoPath(LANGUAGES[0][2])),
            File.ReadAllBytes(TestSupport.RepoPath(WORLD + "IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.otf")),
        };
        ShapingRequest request = new ShapingRequest("שלום עולם नमस्ते दुनिया", "hi", files, 12f, 80f);
        List<ShapedLine> got = ComplexText.HarfBuzzPackage.Shape(request);
        Assert.True(got.Count >= 2, got.Count + " lines");
        HBShape hbShape = new HBShape();
        try {
            hbShape.GetVersion();
        } catch (InvalidOperationException) {
            return;
        }
        List<ShapedLine> want = hbShape.Shape(request);
        Assert.Equal(want.Count, got.Count);
        for (int i = 0; i < want.Count; i++) {
            Assert.Equal(want[i].Glyphs, got[i].Glyphs);
            Assert.Equal(want[i].Start, got[i].Start);
        }
    }

    // A Thai line with half of an emoji, to be broken, is drawn, not a
    // failed document (ultra review, 22 November 2026)
    [Fact]
    public void AThaiLineWithALoneSurrogateIsDrawn() {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        Font thai = new Font(pdf, TestSupport.RepoPath(WORLD + "IBMPlexSansThai/IBMPlexSansThai-Regular.otf"));
        thai.SetSize(12f);
        string text = "ข้อ ๑ มนุษย์ทั้งหลายเกิดมามีอิสระและเสมอภาคกันในเกียรติศักดิ์และสิทธิ\uD83D ต่างมีเหตุผลและมโนธรรม";
        new TextBlock(thai, text).SetWidth(120f).SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        Assert.True(output.ToArray().Length > 1000);
    }
}
}   // End of namespace PDFjet.NET
