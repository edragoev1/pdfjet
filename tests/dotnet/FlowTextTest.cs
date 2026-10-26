/*
 * FlowTextTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
// The flows of these tests are on Letter pages with margins of 50 points: the
// text is 512 points wide, from x 50, and the pages hold 692 points of it,
// from y 50 to 742.
public class FlowTextTest {
    private const float MARGIN = 50f;
    private const float BOTTOM = 742f;
    private const float WIDTH = 512f;
    private const float HEIGHT = 692f;

    private static FlowText Flow(PDF pdf) {
        return new FlowText(pdf, Letter.PORTRAIT).SetMargins(MARGIN);
    }

    private static Paragraph NewParagraph(Font font, string text) {
        return new Paragraph(new TextLine(font, text));
    }

    // An image of the repository, 100 points wide and as tall as the height,
    // narrower than the text.
    private static Image NewImage(PDF pdf, float height) {
        Image image = new Image(pdf, TestSupport.Open("images/up-arrow.png"));
        return image.ScaleBy(100f / image.GetWidth(), height / image.GetHeight());
    }

    [Fact]
    public void ParagraphsFlowAcrossPagesBetweenTheMargins() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<Paragraph> paragraphs = new List<Paragraph>();
        for (int i = 1; i <= 120; i++) {
            paragraphs.Add(NewParagraph(font, "Paragraph " + i + ", long enough to be wrapped onto a second line "
                    + "of the text, which is as wide as the page less its margins of fifty points."));
        }
        List<Page> pages = new List<Page>();
        float[] xy = Flow(pdf).Add(paragraphs, 6f).DrawOn(pages);
        Assert.True(pages.Count >= 2, "120 paragraphs on " + pages.Count + " page");
        for (int i = 0; i < paragraphs.Count; i++) {
            Paragraph p = paragraphs[i];
            Assert.True(p.GetY1() >= MARGIN - TestSupport.DELTA && p.GetY2() <= BOTTOM + TestSupport.DELTA,
                    "paragraph " + (i + 1) + " from " + p.GetY1() + " to " + p.GetY2() + ", outside the margins");
        }
        TestSupport.AssertNear(MARGIN + WIDTH, xy[0], TestSupport.DELTA);      // the right margin
        TestSupport.AssertNear(paragraphs[119].GetY2(), xy[1], TestSupport.DELTA);
    }

    [Fact]
    public void AnImageThatDoesNotFitGoesWholeToTheNextPage() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Image small = NewImage(pdf, 40f);
        Image large = NewImage(pdf, 200f);
        Paragraph paragraph = NewParagraph(font, "Above the images");
        List<Page> pages = new List<Page>();
        Flow(pdf).Add(paragraph).AddSpace(600f).Add(small).Add(large).DrawOn(pages);
        TestSupport.AssertNear(paragraph.GetY2() + 600f, small.y, TestSupport.DELTA);    // under the space
        Assert.Equal(2, pages.Count);
        TestSupport.AssertNear(MARGIN, large.y, TestSupport.DELTA);     // at the top of the next page
        TestSupport.AssertNear(200f, large.GetHeight(), TestSupport.DELTA);
    }

    [Fact]
    public void AnImageTooLargeIsMadeAsLargeAsTheTextAndThePage() {
        PDF pdf = TestSupport.NewPDF();
        Image wide = NewImage(pdf, 100f);
        wide.ScaleBy(20f, 1f);      // 2,000 points wide
        Image tall = NewImage(pdf, 2000f);
        float shape = tall.GetWidth() / tall.GetHeight();
        List<Page> pages = new List<Page>();
        Flow(pdf).Add(wide).Add(tall).DrawOn(pages);
        TestSupport.AssertNear(WIDTH, wide.GetWidth(), TestSupport.DELTA);
        TestSupport.AssertNear(25.6f, wide.GetHeight(), TestSupport.DELTA);   // 100 * 512 / 2,000
        TestSupport.AssertNear(HEIGHT, tall.GetHeight(), TestSupport.DELTA);
        TestSupport.AssertNear(shape, tall.GetWidth() / tall.GetHeight(), 1e-4f);
        TestSupport.AssertNear(MARGIN, tall.y, TestSupport.DELTA);      // at the top of the second page
        Assert.Equal(2, pages.Count);
    }

    [Fact]
    public void AnImageIsAlignedLeftCenterOrRight() {
        PDF pdf = TestSupport.NewPDF();
        Image left = NewImage(pdf, 50f);
        Image center = NewImage(pdf, 50f);
        Image right = NewImage(pdf, 50f);
        List<Page> pages = new List<Page>();
        Flow(pdf).Add(left).Add(center, Alignment.CENTER).Add(right, Alignment.RIGHT).DrawOn(pages);
        float w = left.GetWidth();
        TestSupport.AssertNear(MARGIN, left.x, TestSupport.DELTA);
        TestSupport.AssertNear(MARGIN + (WIDTH - w) / 2f, center.x, TestSupport.DELTA);
        TestSupport.AssertNear(MARGIN + WIDTH - w, right.x, TestSupport.DELTA);
        TestSupport.AssertNear(left.y + 50f, center.y, TestSupport.DELTA);     // one below the other
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AHeadingKeepsWithTheFirstLineAfterIt(bool followed) {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph heading = new Paragraph(new TextLine(font, "A heading").SetFontSize(16f))
                .SetStructureType(StructElem.H1);
        float headingHeight = font.GetAscent(16f) + font.GetDescent(16f);
        // Room for the heading below the image, 2 points more, but not for a
        // line after it too
        Image filler = NewImage(pdf, HEIGHT - headingHeight - 2f);
        FlowText flow = Flow(pdf).Add(filler).Add(heading);
        Paragraph text = NewParagraph(font, "The text under the heading");
        if (followed) {
            flow.Add(text);
        }
        List<Page> pages = new List<Page>();
        flow.DrawOn(pages);
        if (followed) {
            TestSupport.AssertNear(MARGIN, heading.GetY1(), TestSupport.DELTA);    // on the next page with its text
            TestSupport.AssertNear(heading.GetY2(), text.GetY1(), TestSupport.DELTA);
            Assert.Equal(2, pages.Count);
        } else {
            TestSupport.AssertNear(filler.y + filler.GetHeight(), heading.GetY1(), TestSupport.DELTA);
            Assert.Single(pages);
        }
    }

    [Fact]
    public void ASpaceAtTheTopOfAPageIsLeftOut() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph first = NewParagraph(font, "At the top of the first page");
        Image filler = NewImage(pdf, HEIGHT);
        Paragraph second = NewParagraph(font, "At the top of the second page");
        List<Page> pages = new List<Page>();
        Flow(pdf).AddSpace(30f).Add(first).Add(filler).AddSpace(30f).Add(second).DrawOn(pages);
        TestSupport.AssertNear(MARGIN, first.GetY1(), TestSupport.DELTA);      // a space before everything
        TestSupport.AssertNear(MARGIN, second.GetY1(), TestSupport.DELTA);     // a space after a full page
        Assert.Equal(3, pages.Count);       // The text, the image, the text
    }

    [Fact]
    public void StartsOnAPageBelowTheFirstPageTop() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page first = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        Paragraph paragraph = NewParagraph(font, "Under the letterhead");
        List<Page> pages = new List<Page>();
        Flow(pdf).SetFirstPageTop(200f).Add(paragraph).DrawOn(first, pages);
        TestSupport.AssertNear(200f, paragraph.GetY1(), TestSupport.DELTA);
        Assert.Empty(pages);        // the first page is not added
    }

    [Fact]
    public void NothingToDrawMakesNoPage() {
        PDF pdf = TestSupport.NewPDF();
        List<Page> pages = new List<Page>();
        Flow(pdf).AddSpace(20f).Add(new Paragraph()).DrawOn(pages);
        Assert.Empty(pages);
    }

    [Fact]
    public void MarginsThatLeaveNoRoomAreRefused() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        FlowText flow = Flow(pdf).SetMargins(400f).Add(NewParagraph(font, "text"));
        Assert.Throws<ArgumentException>(() => flow.DrawOn(pages));
        Assert.Empty(pages);
    }
}
}   // End of namespace PDFjet.NET
