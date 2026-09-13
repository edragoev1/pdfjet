/*
 * PageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class PageTest {
    [Fact]
    public void ANewPageTracksTheDefaultGraphicsState() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Assert.Equal(1f, page.GetPenWidth());
        TestSupport.AssertRGB(0f, 0f, 0f, page.GetPenColor());
        TestSupport.AssertRGB(0f, 0f, 0f, page.GetBrushColor());
    }

    [Fact]
    public void CmykSettersWriteCmykAndTrackTheRgbOfTheStandard() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetPenColorCMYK(0f, 1f, 1f, 0f);
        page.SetBrushColorCMYK(0f, 0f, 0f, 1f);
        Assert.Equal("0 1 1 0 K\n0 0 0 1 k\n", TestSupport.Content(page));
        TestSupport.AssertRGB(1f, 0f, 0f, page.GetPenColor());
        TestSupport.AssertRGB(0f, 0f, 0f, page.GetBrushColor());
    }

    [Fact]
    public void RestoreGraphicsStateRestoresTheTrackedState() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetPenColor(0xFF0000);
        page.SaveGraphicsState();
        page.SetPenWidth(3f);
        page.SetPenColor(0x00FF00);
        page.RestoreGraphicsState();
        Assert.Equal(1f, page.GetPenWidth());
        TestSupport.AssertRGB(1f, 0f, 0f, page.GetPenColor());
        Assert.EndsWith("q\n3 w\n0 1 0 RG\nQ\n", TestSupport.Content(page));
    }

    [Fact]
    public void GettersReturnCopies() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.GetPenColor()[0] = 1f;
        page.GetBrushColor()[0] = 1f;
        TestSupport.AssertRGB(0f, 0f, 0f, page.GetPenColor());
        TestSupport.AssertRGB(0f, 0f, 0f, page.GetBrushColor());
        page.DrawLine(0f, 0f, 10f, 10f);
        page.GetContent()[0] = (byte) 'X';
        Assert.NotEqual('X', TestSupport.Content(page)[0]);
    }

    [Fact]
    public void DrawLineWritesAStrokedPathWithTheYFlipped() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.DrawLine(10f, 20f, 30f, 40f);
        Assert.Contains("10 772 m\n30 752 l\nS\n", TestSupport.Content(page));
    }
}
}
