/*
 * TextFrameTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
}
}
