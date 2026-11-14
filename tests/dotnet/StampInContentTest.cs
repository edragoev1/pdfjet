/**
 * StampInContentTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using Xunit;

namespace PDFjet.NET {
// A stamp with no description of its own, drawn inside the real content of a
// tagged page, is part of that content, not an artifact inside it (ISO
// 14289-1, 7.1): letters drawn once as stamps and placed in a paragraph.
public class StampInContentTest {
    private static Stamp Letter(PDF pdf) {
        Stamp stamp = new Stamp(pdf).SetSize(6f, 8f);
        stamp.MoveTo(0f, 8f).LineTo(3f, 0f).LineTo(6f, 8f).FillPath();
        stamp.Complete();
        return stamp;
    }

    private static int Count(string text, string part) {
        int n = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i >= 0;
                i = text.IndexOf(part, i + 1, StringComparison.Ordinal)) {
            n++;
        }
        return n;
    }

    [Fact]
    public void AnUndescribedStampInAParagraphIsPartOfIt() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1).SetTitle("Title");
        Page page = new Page(pdf, PDFjet.NET.Letter.PORTRAIT);
        Stamp stamp = Letter(pdf);
        page.AddBDC(StructElem.P, "km", "AAA", "AAA");
        for (int i = 0; i < 3; i++) {
            stamp.SetLocation(50f + 7f * i, 50f);
            stamp.DrawOn(page);
        }
        page.AddEMC();
        string content = TestSupport.Content(page);
        Assert.DoesNotContain("/Artifact BMC", content);
        Assert.Equal(1, Count(content, "BDC\n"));
        Assert.Equal(1, Count(content, "EMC\n"));
        Assert.Equal(3, Count(content, " Do\n"));
        pdf.Complete();
        Assert.Contains("/ActualText", TestSupport.Latin1(stream.ToArray()));
    }

    [Fact]
    public void AnUndescribedStampOutsideContentIsStillAnArtifact() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Page page = new Page(pdf, PDFjet.NET.Letter.PORTRAIT);
        Stamp stamp = Letter(pdf);
        stamp.SetLocation(50f, 50f);
        stamp.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("/Artifact BMC\n", content);
        Assert.Equal(1, Count(content, "EMC\n"));
    }

    [Fact]
    public void ADescribedStampInAParagraphIsAnElementOfItsOwn() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Page page = new Page(pdf, PDFjet.NET.Letter.PORTRAIT);
        Stamp stamp = Letter(pdf);
        stamp.SetAltDescription("A logo");
        page.AddBDC(StructElem.P, "en", "Text", "Text");
        stamp.SetLocation(50f, 50f);
        stamp.DrawOn(page);
        page.AddEMC();
        Assert.Equal(2, Count(TestSupport.Content(page), "EMC\n"));
    }

    [Fact]
    public void AnUntaggedPageMarksNothing() {
        PDF pdf = new PDF(new MemoryStream());
        Page page = new Page(pdf, PDFjet.NET.Letter.PORTRAIT);
        Stamp stamp = Letter(pdf);
        page.AddBDC(StructElem.P, "km", "A", "A");
        stamp.SetLocation(50f, 50f);
        stamp.DrawOn(page);
        page.AddEMC();
        string content = TestSupport.Content(page);
        Assert.False(content.Contains("BMC") || content.Contains("BDC") || content.Contains("EMC"), content);
    }
}
}
