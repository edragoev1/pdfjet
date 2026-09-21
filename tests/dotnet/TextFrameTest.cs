/*
 * TextFrameTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
public class TextFrameTest {
    // Draws two paragraphs of one line and returns how far below the first the second starts.
    private static float ParagraphDistance(float? gap) {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph first = new Paragraph(new TextLine(font, "one"));
        Paragraph second = new Paragraph(new TextLine(font, "two"));
        TextFrame frame = new TextFrame(new List<Paragraph> {first, second}).SetLocation(10f, 10f).SetWidth(300f);
        if (gap != null) {
            frame.SetParagraphGap(gap.Value);
        }
        frame.DrawOn(new Page(pdf, Letter.PORTRAIT));
        return second.GetY1() - first.GetY1();
    }

    [Fact]
    public void TheGapIsAddedToTheLineSoParagraphsNeverOverlap() {
        float line = TestSupport.Helvetica(TestSupport.NewPDF()).GetBodyHeight();
        TestSupport.AssertNear(2f * line, ParagraphDistance(null), TestSupport.DELTA);  // one empty line
        TestSupport.AssertNear(line, ParagraphDistance(0f), TestSupport.DELTA);
        TestSupport.AssertNear(line + 10f, ParagraphDistance(10f), TestSupport.DELTA);
    }

    [Fact]
    public void ANegativeGapIsTakenAsZero() {
        TestSupport.AssertNear(ParagraphDistance(0f), ParagraphDistance(-5f), TestSupport.DELTA);
    }

    [Fact]
    public void TheDefaultGapIsAnEmptyLineOfTheNextParagraph() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph heading = new Paragraph(new TextLine(font, "Heading").SetFontSize(24f));
        Paragraph body = new Paragraph(new TextLine(font, "body"));
        new TextFrame(new List<Paragraph> {heading, body}).SetLocation(10f, 10f).SetWidth(300f)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
        // The heading, then one empty line in the size of the body text
        TestSupport.AssertNear(font.GetBodyHeight(24f) + font.GetBodyHeight(font.GetSize()),
                body.GetY1() - heading.GetY1(), TestSupport.DELTA);
    }

    // Draws one paragraph with the alignment in a frame 200 wide at x 10, and returns it.
    private static Paragraph DrawAligned(Alignment alignment, string text) {
        PDF pdf = TestSupport.NewPDF();
        Paragraph paragraph = new Paragraph(new TextLine(TestSupport.Helvetica(pdf), text));
        paragraph.SetTextAlignment(alignment);
        new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(200f)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
        return paragraph;
    }

    // Draws one paragraph in a frame of the width at x 0, and returns how far down its text reaches.
    private static float TextHeight(string text, float width) {
        PDF pdf = TestSupport.NewPDF();
        Paragraph paragraph = new Paragraph(new TextLine(TestSupport.Helvetica(pdf), text));
        new TextFrame(new List<Paragraph> {paragraph}).SetLocation(0f, 10f).SetWidth(width)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
        return paragraph.GetY2() - paragraph.GetY1();
    }

    [Fact]
    public void ARowTakesTheWordsThatFitWithoutTheSpaceAfterThem() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        float oneRow = TextHeight("one two", 300f);
        float width = font.StringWidth("one ") + font.StringWidth("two");
        TestSupport.AssertNear(oneRow, TextHeight("one two", width), TestSupport.DELTA);
        Assert.True(TextHeight("one two", width - 0.1f) > oneRow, "two rows");
        // A word as wide as the frame is not broken.
        TestSupport.AssertNear(oneRow, TextHeight("Hello", font.StringWidth("Hello")), TestSupport.DELTA);
    }

    [Fact]
    public void ARightAlignedParagraphEndsAtTheRightEdge() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Paragraph paragraph = DrawAligned(Alignment.RIGHT, "Hello");
        // The text ends at the right edge; the space after it is past the edge.
        TestSupport.AssertNear(210f - font.StringWidth("Hello"), paragraph.GetTextX(), TestSupport.DELTA);
        TestSupport.AssertNear(210f + font.StringWidth(" "), paragraph.GetX2(), TestSupport.DELTA);
    }

    [Fact]
    public void ACenteredParagraphHasTheSameSpaceOnBothSides() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Paragraph paragraph = DrawAligned(Alignment.CENTER, "Hello");
        TestSupport.AssertNear(10f + (200f - font.StringWidth("Hello")) / 2f, paragraph.GetTextX(), TestSupport.DELTA);
    }

    [Fact]
    public void AJustifiedParagraphLeavesItsLastRowAsItIs() {
        string text = "one two three four five six seven eight nine ten eleven twelve thirteen";
        Paragraph left = DrawAligned(Alignment.LEFT, text);
        Paragraph justified = DrawAligned(Alignment.JUSTIFY, text);
        Assert.True(left.GetY2() - left.GetY1() > 20f, "more than one row");
        TestSupport.AssertNear(left.GetY2(), justified.GetY2(), TestSupport.DELTA);
        TestSupport.AssertNear(left.GetX2(), justified.GetX2(), TestSupport.DELTA);
    }
    [Fact]
    public void ParagraphsWithALabelAreAList() {
        // The label of an item is drawn where the item begins, so that it
        // reads before the text of the item and not after all of the text.
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = TestSupport.Helvetica(pdf);
        List<Paragraph> paragraphs = new List<Paragraph>();
        string[] texts = {"alpha beta", "gamma delta"};
        for (int i = 0; i < texts.Length; i++) {
            paragraphs.Add(new Paragraph().Add(new TextLine(font, texts[i]))
                    .SetListLabel(new TextLine(font, (i + 1) + "."), 15f));
        }
        // A paragraph with no label ends the list.
        paragraphs.Add(new Paragraph().Add(new TextLine(font, "epsilon")));
        TextFrame frame = new TextFrame(paragraphs);
        frame.SetLocation(70f, 50f);
        frame.SetWidth(300f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        frame.DrawOn(page);
        string content = TestSupport.Latin1(page.GetContent());
        // The label of an item is drawn before the text of the item.
        Assert.True(content.IndexOf(TestSupport.Hex("1.")) < content.IndexOf(TestSupport.Hex("alpha")));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, raw.Split(new string[] {"/S /L\n"}, StringSplitOptions.None).Length - 1);
        Assert.Equal(2, raw.Split(new string[] {"/S /LI\n"}, StringSplitOptions.None).Length - 1);
        Assert.Equal(2, raw.Split(new string[] {"/S /Lbl\n"}, StringSplitOptions.None).Length - 1);
        Assert.Equal(2, raw.Split(new string[] {"/S /LBody\n"}, StringSplitOptions.None).Length - 1);
    }

}
}
