/*
 * TextFrameTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    // 200 paragraphs of a few lines each, the first word of each its number,
    // p000 to p199, which no other word begins with.
    private static TextFrame Novel(Font font) {
        List<String> paragraphs = new List<String>();
        for (int i = 0; i < 200; i++) {
            System.Text.StringBuilder text = new System.Text.StringBuilder("p" + i.ToString("000"));
            for (int j = 0; j < 30 + i % 17; j++) {
                text.Append(" word").Append(j);
            }
            paragraphs.Add(text.ToString());
        }
        return new TextFrame(font, paragraphs).SetLocation(72f, 72f).SetWidth(468f);
    }

    // The page each paragraph starts on, by its number.
    private static int[] PagesOf(List<Page> pages) {
        int[] pageOf = new int[200];
        Array.Fill(pageOf, -1);
        for (int p = 0; p < pages.Count; p++) {
            string content = TestSupport.Content(pages[p]);
            for (int i = 0; i < 200; i++) {
                if (content.Contains(TestSupport.Hex("p" + i.ToString("000")))) {
                    Assert.True(pageOf[i] == -1, "paragraph " + i + " starts on two pages");
                    pageOf[i] = p;
                }
            }
        }
        return pageOf;
    }

    [Fact]
    public void AFrameFlowsOntoAsManyPagesAsTheTextNeeds() {
        PDF pdf = TestSupport.NewPDF();
        TextFrame frame = Novel(TestSupport.Helvetica(pdf));
        List<Page> pages = new List<Page>();
        frame.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(pages.Count > 5, pages.Count + " pages");
        Assert.False(frame.HasMoreText());
        // Every paragraph is drawn once, in order, and every page has text.
        int[] pageOf = PagesOf(pages);
        for (int i = 0; i < 200; i++) {
            Assert.True(pageOf[i] >= 0, "paragraph " + i + " is not drawn");
            Assert.True(i == 0 || pageOf[i] >= pageOf[i - 1], "paragraph " + i + " is out of order");
        }
        Assert.Equal(pages.Count - 1, pageOf[199]);
        // The text keeps the margin of its location at the bottom too: no
        // baseline under 72 points from the bottom of the page.
        System.Text.RegularExpressions.Regex td = new System.Text.RegularExpressions.Regex("[-0-9.]+ ([-0-9.]+) Td\n");
        int baselines = 0;
        foreach (Page page in pages) {
            foreach (System.Text.RegularExpressions.Match m in td.Matches(TestSupport.Content(page))) {
                float y = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                Assert.True(y >= 72f, "a baseline at y = " + m.Groups[1].Value);
                baselines++;
            }
        }
        Assert.True(baselines > 100, baselines + " baselines");
        // The frame has no height of its own, as before.
        Assert.Equal(0f, frame.GetHeight());
    }

    [Fact]
    public void AFrameWithAHeightHasItOnEveryPage() {
        PDF pdf = TestSupport.NewPDF();
        List<Page> tall = new List<Page>();
        Novel(TestSupport.Helvetica(pdf)).DrawOn(pdf, tall, Letter.PORTRAIT);
        TextFrame frame = Novel(TestSupport.Helvetica(pdf)).SetHeight(300f);
        List<Page> pages = new List<Page>();
        float[] xy = frame.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(pages.Count > tall.Count, pages.Count + " pages, not more than " + tall.Count);
        Assert.Equal(300f, frame.GetHeight());
        TestSupport.AssertXY(540f, 372f, xy);
        // An empty frame needs no page.
        List<Page> none = new List<Page>();
        TestSupport.AssertXY(10f, 20f, new TextFrame(new List<Paragraph>()).SetLocation(10f, 20f)
                .DrawOn(pdf, none, Letter.PORTRAIT));
        Assert.Empty(none);
    }

    // A paragraph of text lines in the font, each added with Add, or with
    // AddJoined when it starts with "+", which is not part of its text.
    internal static Paragraph JoinedParagraph(Font font, params string[] texts) {
        Paragraph paragraph = new Paragraph();
        foreach (string text in texts) {
            if (text.StartsWith("+", StringComparison.Ordinal)) {
                paragraph.AddJoined(new TextLine(font, text.Substring(1)));
            } else {
                paragraph.Add(new TextLine(font, text));
            }
        }
        return paragraph;
    }

    private static string DrawJoined(float width, Alignment? alignment, params string[] texts) {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph paragraph = JoinedParagraph(font, texts);
        if (alignment != null) {
            paragraph.SetTextAlignment(alignment.Value);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(width).DrawOn(page);
        return TestSupport.Content(page);
    }

    [Fact]
    public void AJoinedTextLineHasNoSpaceBeforeIt() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        string content = DrawJoined(300f, null, "one", "+,", "two");
        float[] one = TestSupport.PositionOf(content, "one");
        float[] comma = TestSupport.PositionOf(content, ",");
        float[] two = TestSupport.PositionOf(content, "two");
        TestSupport.AssertNear(one[0] + font.StringWidth("one"), comma[0], TestSupport.DELTA, "comma x");
        TestSupport.AssertNear(comma[0] + font.StringWidth(", "), two[0], TestSupport.DELTA, "two x");
        TestSupport.AssertNear(one[1], two[1], TestSupport.DELTA, "two y");
    }

    [Fact]
    public void ARowDoesNotBreakInsideAJoinedWord() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        // "aaa bbb" fits in the row, and "aaa bbbccc" does not, so bbb goes on
        // the next row with the ccc joined to it.
        float width = font.StringWidth("aaa bbb") + 1f;
        string content = DrawJoined(width, null, "aaa bbb", "+ccc");
        float[] aaa = TestSupport.PositionOf(content, "aaa");
        float[] bbb = TestSupport.PositionOf(content, "bbb");
        float[] ccc = TestSupport.PositionOf(content, "ccc");
        Assert.True(bbb[1] < aaa[1], "bbb is on the second row");
        TestSupport.AssertNear(10f, bbb[0], TestSupport.DELTA, "bbb x");
        TestSupport.AssertNear(bbb[1], ccc[1], TestSupport.DELTA, "ccc y");
        TestSupport.AssertNear(bbb[0] + font.StringWidth("bbb"), ccc[0], TestSupport.DELTA, "ccc x");
    }

    [Fact]
    public void AJoinedWordWiderThanTheFrameBreaksWhereItIsJoined() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        string content = DrawJoined(font.StringWidth("abc") + 1f, null, "abc", "+def");
        float[] abc = TestSupport.PositionOf(content, "abc");
        float[] def = TestSupport.PositionOf(content, "def");
        Assert.True(def[1] < abc[1], "def is on the second row");
        TestSupport.AssertNear(10f, def[0], TestSupport.DELTA, "def x");
    }

    [Fact]
    public void ASpaceWhereTheyMeetKeepsJoinedTextLinesApart() {
        Assert.Equal(DrawJoined(300f, null, "one", "two"), DrawJoined(300f, null, "one", "+ two"));
        Assert.Equal(DrawJoined(300f, null, "one ", "two"), DrawJoined(300f, null, "one ", "+two"));
    }

    [Fact]
    public void AJustifiedRowDoesNotWidenAJoin() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        string content = DrawJoined(200f, Alignment.JUSTIFY,
                "one two three", "+,", "four five six seven eight nine ten eleven twelve");
        float[] three = TestSupport.PositionOf(content, "three");
        float[] comma = TestSupport.PositionOf(content, ",");
        float[] four = TestSupport.PositionOf(content, "four");
        TestSupport.AssertNear(three[1], four[1], TestSupport.DELTA, "four y");
        TestSupport.AssertNear(three[0] + font.StringWidth("three"), comma[0], TestSupport.DELTA, "comma x");
        // The spaces are widened: four is further than one space after the comma.
        Assert.True(four[0] > comma[0] + font.StringWidth(", ") + 1f, "the row is justified");
    }

    // Two text lines, the first in Courier, whose space is wide, and the second
    // in Helvetica, or the other way round when courierFirst is false.
    private static string DrawMixed(float width, Alignment? alignment, bool courierFirst,
            string first, string second, bool column) {
        PDF pdf = TestSupport.NewPDF();
        Font courier = new Font(pdf, CoreFont.COURIER);
        Font helvetica = TestSupport.Helvetica(pdf);
        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(courierFirst ? courier : helvetica, first))
                .Add(new TextLine(courierFirst ? helvetica : courier, second));
        if (alignment != null) {
            paragraph.SetTextAlignment(alignment.Value);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        if (column) {
            TextColumn textColumn = new TextColumn();
            textColumn.SetWidth(width);
            textColumn.SetTextAlignment(alignment ?? Alignment.LEFT);
            textColumn.AddParagraph(paragraph);
            textColumn.SetLocation(10f, 10f);
            textColumn.DrawOn(page);
        } else {
            new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(width).DrawOn(page);
        }
        return TestSupport.Content(page);
    }

    internal static void CheckTheNarrowerSpaceIsUsed(bool column) {
        Font courier = new Font(TestSupport.NewPDF(), CoreFont.COURIER);
        Font helvetica = TestSupport.Helvetica(TestSupport.NewPDF());
        // Code, then text: the space is Helvetica's, at the start of the text.
        string content = DrawMixed(300f, null, true, "x", "and more", column);
        float[] x = TestSupport.PositionOf(content, "x");
        Assert.True(content.Contains("<" + TestSupport.Hex("x") + ">"), "x has no space after it");
        TestSupport.AssertNear(x[0] + courier.StringWidth("x"), TestSupport.PositionOf(content, " and")[0],
                TestSupport.DELTA, "and x");
        // Text, then code: the space is still Helvetica's, after the text.
        content = DrawMixed(300f, null, false, "use", "x", column);
        TestSupport.AssertNear(TestSupport.PositionOf(content, "use")[0] + helvetica.StringWidth("use "),
                TestSupport.PositionOf(content, "x")[0], TestSupport.DELTA, "x x");
    }

    internal static void CheckAMovedSpaceDoesNotStartARow(bool column) {
        Font courier = new Font(TestSupport.NewPDF(), CoreFont.COURIER);
        string content = DrawMixed(courier.StringWidth("aaa") + 5f, null, true, "aaa", "bbb", column);
        float[] aaa = TestSupport.PositionOf(content, "aaa");
        float[] bbb = TestSupport.PositionOf(content, "bbb");
        Assert.True(bbb[1] < aaa[1], "bbb is on the second row");
        TestSupport.AssertNear(10f, bbb[0], TestSupport.DELTA, "bbb x");
        Assert.True(!content.Contains("<" + TestSupport.Hex(" bbb")), "bbb has no space before it");
    }

    internal static void CheckAJustifiedRowWidensAMovedSpace(bool column) {
        Font courier = new Font(TestSupport.NewPDF(), CoreFont.COURIER);
        Font helvetica = TestSupport.Helvetica(TestSupport.NewPDF());
        string content = DrawMixed(150f, Alignment.JUSTIFY, true, "x",
                "one two three four five six seven eight nine ten eleven twelve", column);
        float[] x = TestSupport.PositionOf(content, "x");
        float[] one = TestSupport.PositionOf(content, column ? " one" : "one");
        float[] two = TestSupport.PositionOf(content, "two");
        // Where one and two start; a text column draws the space before one with it.
        float oneStart = column ? one[0] + helvetica.StringWidth(" ") : one[0];
        TestSupport.AssertNear(x[1], one[1], TestSupport.DELTA, "one y");
        // The space before one, which Courier's text line left to it, is as
        // wide as the space after it: both are widened alike.
        float before = oneStart - (x[0] + courier.StringWidth("x"));
        float after = two[0] - (oneStart + helvetica.StringWidth("one"));
        Assert.True(before > helvetica.StringWidth(" ") + 0.1f, "the row is justified");
        TestSupport.AssertNear(after, before, TestSupport.DELTA, "space before one");
    }

    [Fact]
    public void TheSpaceBetweenTwoTextLinesIsTheNarrowerOfTheirSpaces() {
        CheckTheNarrowerSpaceIsUsed(false);
    }

    [Fact]
    public void AMovedSpaceDoesNotStartARow() {
        CheckAMovedSpaceDoesNotStartARow(false);
    }

    [Fact]
    public void AJustifiedRowWidensAMovedSpace() {
        CheckAJustifiedRowWidensAMovedSpace(false);
    }

    internal static void CheckALinkEndsBeforeTheSpaceAfterIt(bool column) {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(font, "see the link").SetURIAction("https://pdfjet.com").SetUnderline(true))
                .Add(new TextLine(font, "after it"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        if (column) {
            TextColumn textColumn = new TextColumn();
            textColumn.SetWidth(300f);
            textColumn.AddParagraph(paragraph);
            textColumn.SetLocation(10f, 10f);
            textColumn.DrawOn(page);
        } else {
            new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(300f).DrawOn(page);
        }
        string content = TestSupport.Content(page);
        // The underlined text ends at the word, and the space is the text's after it.
        Assert.True(content.Contains(TestSupport.Hex("link") + ">"), content);
        Assert.True(content.Contains("<" + TestSupport.Hex(" after")), content);
    }

    [Fact]
    public void ALinkEndsBeforeTheSpaceAfterIt() {
        CheckALinkEndsBeforeTheSpaceAfterIt(false);
    }

    [Fact]
    public void ManyJoinedTextLinesAreMeasuredInLinearTime() {
        // Every text line is one word joined to the word before it, so the
        // width of the words joined to a word was measured over and over.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph paragraph = new Paragraph(new TextLine(font, "word"));
        for (int i = 0; i < 20000; i++) {
            paragraph.AddJoined(new TextLine(font, "x"));
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stopwatch watch = Stopwatch.StartNew();
        new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(300f).DrawOn(page);
        long milliseconds = watch.ElapsedMilliseconds;
        Assert.True(milliseconds < 3000, milliseconds + " ms");
    }
}
}
