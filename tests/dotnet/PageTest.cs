/*
 * PageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
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

    [Fact]
    public void AGoToLinkPointsAtItsDestinationOnAnotherPage() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go").SetGoToAction("there").SetLocation(50f, 50f).DrawOn(page1);
        new Rect(10f, 10f, 20f, 20f).SetGoToAction("there").DrawOn(page1);
        new TextLine(font, "Nowhere").SetGoToAction("missing").SetLocation(50f, 100f).DrawOn(page1);
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        page2.AddDestination("there", 30f, 100f);
        pdf.Complete();
        string file = TestSupport.Latin1(stream.ToArray());
        // The text and the rect link to the destination, 100 points down page 2; the
        // link to a destination no page has is written without a /Dest
        Assert.Equal(2, file.Split("/Dest [").Length - 1);
        Assert.Equal(2, file.Split("/XYZ 30 692 0]").Length - 1);
        Assert.Equal(3, file.Split("/Subtype /Link").Length - 1);
    }

    [Fact]
    public void ATextLineAddsItsDestinationWhenItIsDrawn() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go").SetGoToAction("there").SetLocation(50f, 50f).DrawOn(page1);
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        TextLine target = new TextLine(font, "There").SetDestination("there");
        target.SetLocation(30f, 100f + font.GetSize());
        target.DrawOn(page2);
        pdf.Complete();
        string file = TestSupport.Latin1(stream.ToArray());
        // The destination is at the left edge of page 2, a font size above the baseline.
        Assert.Equal("there", target.GetDestination());
        Assert.Equal(1, file.Split("/Dest [").Length - 1);
        Assert.Equal(1, file.Split("/XYZ 0 692 0]").Length - 1);
    }

    [Fact]
    public void PositiveAnglesTurnClockwise() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        // y grows downward, so text turned a quarter clockwise runs down the page
        // and text turned a quarter counterclockwise runs up from its location.
        float[] down = new TextLine(font, "Down").SetTextRotation(90).SetLocation(100f, 100f).DrawOn(page);
        float[] up = new TextLine(font, "Up").SetTextRotation(-90).SetLocation(300f, 100f).DrawOn(page);
        Assert.True(down[1] > 100f + font.StringWidth("Down") / 2f, "down " + down[1]);
        Assert.Equal(100f, up[1], 0.01f);
        page.SetRotation(90);
        pdf.Complete();
        string file = TestSupport.Latin1(stream.ToArray());
        Assert.Contains("/Rotate 90", file);
    }

    [Fact]
    public void APathWithFewerThanTwoPointsPaintsNothing() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        List<Point> path = new List<Point>();
        page.DrawPath(path, PathOperator.STROKE);
        path.Add(new Point(10f, 10f));
        page.DrawPath(path, PathOperator.STROKE);
        Assert.Equal("", TestSupport.Content(page));
    }

    [Fact]
    public void TheShapesAreDrawnOrFilled() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.DrawCircle(50f, 50f, 10f);
        page.FillCircle(50f, 50f, 10f);
        page.DrawRoundedRect(10f, 10f, 100f, 50f, 5f, 5f);
        page.FillRoundedRect(10f, 10f, 100f, 50f, 5f, 5f);
        // A drawn shape is stroked with S, and a filled one filled with f.
        List<string> painted = new List<string>();
        foreach (string token in TestSupport.Content(page).Split((char[]) null, System.StringSplitOptions.RemoveEmptyEntries)) {
            if (token == "S" || token == "f") {
                painted.Add(token);
            }
        }
        Assert.Equal(new List<string> {"S", "f", "S", "f"}, painted);
    }

    [Fact]
    public void ARadioButtonFontSizeLeavesTheFontAlone() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new RadioButton(font, "Yes").SetLocation(50f, 50f).SetFontSize(20f).DrawOn(page);
        Assert.Equal(12f, font.GetSize());
        Assert.Contains(" 20 Tf\n", TestSupport.Content(page));
    }
}
}
