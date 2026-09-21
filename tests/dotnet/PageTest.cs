/*
 * PageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
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
    public void AColorOrPenWidthThatIsSetAlreadyIsNotWrittenAgain() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetBrushColor(Color.black);    // Written, as a new page has written no color
        page.SetBrushColor(Color.black);
        page.SetPenColor(Color.red);
        page.SetPenColor(new float[] {1f, 0f, 0f});
        page.SetPenWidth(0f);
        page.SetDefaultPenWidth();
        Assert.Equal("0 0 0 rg\n1 0 0 RG\n0 w\n", TestSupport.Content(page));
    }

    [Fact]
    public void QAndQKeepWhatTheContentHasWritten() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetBrushColor(Color.black);
        page.SaveGraphicsState();
        page.SetBrushColor(Color.blue);
        page.SetBrushColor(Color.blue);
        page.RestoreGraphicsState();
        page.SetBrushColor(Color.black);    // Q restored black
        page.SetBrushColor(Color.blue);
        Assert.Equal("0 0 0 rg\nq\n0 0 1 rg\nQ\n0 0 1 rg\n", TestSupport.Content(page));
    }

    [Fact]
    public void TransformScalesTheHeightUntilTheStateIsRestored() {
        // Transform divides the height of the page by the vertical scale, so
        // the y coordinates that follow are measured in the space it made. A
        // restore has the height back, where every coordinate after it was
        // measured from the scaled height.
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        float[] values = new float[9];
        values[Page.MSCALE_X] = 2f;
        values[Page.MSCALE_Y] = 2f;
        page.MoveTo(10f, 100f);
        page.SaveGraphicsState();
        page.Transform(values);
        page.MoveTo(10f, 100f);
        page.RestoreGraphicsState();
        page.MoveTo(10f, 100f);
        Assert.Equal("10 692 m\nq\n2 0 0 2 0 0 cm\n10 296 m\nQ\n10 692 m\n",
                TestSupport.Content(page));
    }

    [Fact]
    public void AnRgbColorAfterACmykColorIsWritten() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetBrushColor(Color.black);
        page.SetBrushColorCMYK(0f, 0f, 0f, 1f);
        page.SetBrushColor(Color.black);
        Assert.Equal("0 0 0 rg\n0 0 0 1 k\n0 0 0 rg\n", TestSupport.Content(page));
    }

    [Fact]
    public void TheFontOfTheTextIsWrittenWhenItChanges() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "a").SetLocation(10f, 20f).DrawOn(page);
        new TextLine(font, "b").SetLocation(10f, 40f).DrawOn(page);
        new TextLine(font, "c").SetFontSize(14f).SetLocation(10f, 60f).DrawOn(page);
        page.SaveGraphicsState();
        new TextLine(font, "d").SetLocation(10f, 80f).DrawOn(page);
        page.RestoreGraphicsState();
        new TextLine(font, "e").SetFontSize(14f).SetLocation(10f, 100f).DrawOn(page);
        string content = TestSupport.Content(page);
        List<string> fonts = new List<string>();
        foreach (string line in content.Split('\n')) {
            if (line.EndsWith(" Tf")) {
                fonts.Add(line.Substring(line.IndexOf(' ') + 1));
            }
        }
        Assert.Equal(new List<string> {"12 Tf", "14 Tf", "12 Tf"}, fonts);
    }

    [Fact]
    public void FillRectWritesOneRectangleWithTheEdgesOfThePath() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.FillRect(10f, 20f, 30f, 40f);
        Assert.Equal("10 732 30 40 re\nf\n", TestSupport.Content(page));

        // A path wrote the top edge at 792 and the bottom one at 791.99, where
        // rounding the height alone would make it 0.
        page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.FillRect(0f, 0.004f, 1f, 0.004f);
        Assert.Equal("0 791.99 1 0.01 re\nf\n", TestSupport.Content(page));

        // Far outside the page the rectangle is still a path.
        page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.FillRect(200000f, 0f, 10f, 10f);
        Assert.Equal("200000 792 m\n200010 792 l\n200010 782 l\n200000 782 l\nf\n", TestSupport.Content(page));
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
    public void AppendWritesStringsInUtf8AndIntegersAndNumbersInDecimal() {
        string euros = new string('€', 256);
        string digits = string.Concat(System.Linq.Enumerable.Repeat("0123456789", 1000)) + "é";
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.Append("BT é≠\U0001F600 ");
        page.Append(-2147483648);
        page.Append(' ');
        page.Append(2147483647);
        page.Append(' ');
        page.Append(-8388607.5f);
        page.Append(' ');
        page.Append(0.125f);
        page.Append(' ');
        page.Append(euros);
        page.Append(' ');
        page.Append(digits);
        string expected = "BT é≠\U0001F600 -2147483648 2147483647 -8388607.5 0.13 " + euros + " " + digits;
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(expected), page.GetContent());
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

    [Fact]
    public void ImportedContentIsSeparatedFromTheOperatorAfterIt() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.DrawContents(System.Text.Encoding.Latin1.GetBytes("BT ET"), 100f, 0f, 0f, 1f, 1f);
        Assert.Contains("BT ET\nQ\n", TestSupport.Content(page));
    }
    [Fact]
    public void BeginStructElementGroupsWhatIsDrawnIntoAList() {
        // The items of a list are drawn one at a time, so nothing but the page
        // can hold them together: L for the list, LI for each item, and Lbl
        // and LBody for the label and the body of the item.
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.BeginStructElement(StructElem.L);
        string[] items = {"one", "two"};
        for (int i = 0; i < items.Length; i++) {
            page.BeginStructElement(StructElem.LI);
            new TextLine(font, (i + 1) + ".").SetStructureType(StructElem.LBL)
                    .SetLocation(50f, 50f + 20f*i).DrawOn(page);
            page.BeginStructElement(StructElem.LBODY);
            new TextLine(font, items[i]).SetLocation(70f, 50f + 20f*i).DrawOn(page);
            page.EndStructElement();
            page.EndStructElement();
        }
        page.EndStructElement();
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, raw.Split(new string[] {"/S /L\n"}, StringSplitOptions.None).Length - 1);
        Assert.Equal(2, raw.Split(new string[] {"/S /LI\n"}, StringSplitOptions.None).Length - 1);
        Assert.Equal(2, raw.Split(new string[] {"/S /Lbl\n"}, StringSplitOptions.None).Length - 1);
        Assert.Equal(2, raw.Split(new string[] {"/S /LBody\n"}, StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void EndStructElementWithoutABeginIsRefused() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        Exception e = Assert.ThrowsAny<Exception>(() => page.EndStructElement());
        Assert.Equal("EndStructElement was called without a matching BeginStructElement.",
                e.Message);
    }

}
}
