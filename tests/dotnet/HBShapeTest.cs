/*
 * HBShapeTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
// The tests of HBShape need hb-shape, HarfBuzz's program: they pass without
// testing anything where it is not installed.
public class HBShapeTest {
    private const string DEVANAGARI = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf";
    private const string KHMER = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf";

    private static HBShape Installed() {
        try {
            return new HBShape().GetVersion().Contains("HarfBuzz") ? new HBShape() : null;
        } catch (InvalidOperationException) {
            return null;
        }
    }

    private static string[] Udhr(string code) {
        return File.ReadAllLines(TestSupport.RepoPath("resources/data/udhr/udhr_" + code + ".txt"), Encoding.UTF8);
    }

    // Khmer breaks at the ends of words, as ICU's dictionary has them; Hindi
    // at its spaces; each line starts where the last ended
    [Fact]
    public void BreaksBetweenWords() {
        HBShape hb = Installed();
        if (hb == null) {
            return;
        }
        foreach (string[] c in new[] {new[] {"khm", "km", KHMER}, new[] {"hin", "hi", DEVANAGARI}}) {
            byte[] font = File.ReadAllBytes(TestSupport.RepoPath(c[2]));
            int breaks = 0;
            foreach (string paragraph in Udhr(c[0])) {
                if (paragraph.Length == 0) {
                    continue;
                }
                List<ShapedLine> lines = hb.Shape(new ShapingRequest(paragraph, c[1], new[] {font}, 12f, 300f));
                int[] cps = DictionaryBreaks.CodePoints(paragraph);
                HashSet<int> ends = new HashSet<int>(DictionaryBreaks.WordEnds(cps));
                for (int i = 0; i < lines.Count; i++) {
                    if (i > 0) {
                        Assert.Equal(lines[i - 1].End, lines[i].Start);
                    }
                    if (i == lines.Count - 1) {
                        Assert.Equal(paragraph.Length, lines[i].End);
                        continue;
                    }
                    breaks++;
                    int at = DictionaryBreaks.CodePoints(paragraph.Substring(0, lines[i].End)).Length;
                    Assert.True(cps[at - 1] == ' ' || ends.Contains(at), c[0] + ": a break inside a word at " + at);
                }
            }
            Assert.True(breaks >= 50, c[0] + ": " + breaks + " breaks");
        }
    }

    // Without a shaper set, ComplexText shapes with hb-shape
    [Fact]
    public void ComplexTextShapesWithHBShapeWhenNoShaperIsSet() {
        if (Installed() == null) {
            return;
        }
        PDF pdf = new PDF(new MemoryStream());
        Font khmer = new Font(pdf, TestSupport.RepoPath(KHMER));
        khmer.SetSize(12f);
        float[] xy = new ComplexText(khmer, Udhr("khm")[2]).SetLanguage("km").SetWidth(300f).SetLocation(50f, 50f)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
        Assert.True(xy[1] > 50 + 3 * 12, "drawn down to " + xy[1]);
        pdf.Complete();
    }

    // ComplexText draws Hindi with an English word, in two fonts, and Khmer
    [Fact]
    public void DrawsComplexText() {
        HBShape hb = Installed();
        if (hb == null) {
            return;
        }
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        Font hindi = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        Font khmer = new Font(pdf, TestSupport.RepoPath(KHMER));
        Font latin = new Font(pdf, TestSupport.RepoPath("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"));
        foreach (Tuple<Font, string, string> c in new[] {
                Tuple.Create(hindi, Udhr("hin")[2] + " PDFjet", "hi"), Tuple.Create(khmer, Udhr("khm")[2], "km")}) {
            c.Item1.SetSize(12f);
            Page page = new Page(pdf, Letter.PORTRAIT);
            float[] xy = new ComplexText(c.Item1, c.Item2).SetFallbackFont(latin).SetLanguage(c.Item3)
                    .SetShaper(hb).SetWidth(300f).SetLocation(50f, 50f).DrawOn(page);
            Assert.True(xy[1] > 50 + 3 * 12, c.Item3 + ": drawn down to " + xy[1]);
        }
        pdf.Complete();
        Assert.True(output.ToArray().Length > 1000);
    }
}
}
