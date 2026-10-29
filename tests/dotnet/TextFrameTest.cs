/*
 * TextFrameTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
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
    public void GetTextBottomIsTheBottomOfTheTextInAFrameWithAHeight() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Paragraph first = new Paragraph(new TextLine(font, "one"));
        Paragraph second = new Paragraph(new TextLine(font, "two"));
        TextFrame frame = new TextFrame(new List<Paragraph> {first, second}).SetLocation(10f, 10f).SetWidth(300f)
                .SetHeight(500f);
        TestSupport.AssertNear(0f, frame.GetTextBottom(), TestSupport.DELTA);  // before DrawOn
        float[] xy = frame.DrawOn(page);
        TestSupport.AssertNear(510f, xy[1], TestSupport.DELTA);                // the frame's bottom
        TestSupport.AssertNear(second.GetY2(), frame.GetTextBottom(), TestSupport.DELTA);
        Assert.True(frame.GetTextBottom() < 100f);

        // With no height, DrawOn returns the bottom of the text too.
        Paragraph third = new Paragraph(new TextLine(font, "three"));
        frame = new TextFrame(new List<Paragraph> {third}).SetLocation(10f, 200f).SetWidth(300f);
        xy = frame.DrawOn(page);
        TestSupport.AssertNear(xy[1], frame.GetTextBottom(), TestSupport.DELTA);
        TestSupport.AssertNear(third.GetY2(), frame.GetTextBottom(), TestSupport.DELTA);
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
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        // The label of an item is drawn where the item begins, so that it
        // reads before the text of the item and not after all of the text.
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1).SetTitle("Test");
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
    public void AJustifiedRowDrawsEachWordWithTheSpaceAfterIt() {
        string content = DrawJoined(200f, Alignment.JUSTIFY,
                "one two three four five six seven eight nine ten eleven twelve thirteen");
        // So that a screen reader and a text extractor see the word breaks;
        // the stretch of the row still places each word.
        Assert.Contains("<" + TestSupport.Hex("one ") + ">", content);
        Assert.Contains("<" + TestSupport.Hex("two ") + ">", content);
        // The last row is not justified, and is drawn whole, as before
        Assert.Contains("<" + TestSupport.Hex("eight nine ten eleven twelve thirteen"), content);
    }

    // A paragraph whose link keeps no space after it: Paragraph.SpaceMovesToNext
    // moves the space to the start of the text line after the link.
    private static string DrawLinked(float width, Alignment alignment) {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph paragraph = new Paragraph();
        paragraph.Add(new TextLine(font, "Please see"));
        paragraph.Add(new TextLine(font, "our website").SetURIAction("https://pdfjet.com"));
        paragraph.Add(new TextLine(font, "for more details about the library and what it does."));
        paragraph.SetTextAlignment(alignment);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(width).DrawOn(page);
        return TestSupport.Content(page);
    }

    [Fact]
    public void TheSpaceAfterALinkIsDrawn() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        // In a justified row the space moved to the next text line is drawn
        // after the link; and when the word after the link starts the next row,
        // the row ends with it, justified or not.
        float onOneRow = font.StringWidth("Please see our website for more details") + 5f;
        float rowEndsAtLink = font.StringWidth("Please see our website") + 5f;
        Assert.Contains(TestSupport.Hex("website "), DrawLinked(onOneRow, Alignment.JUSTIFY));
        Assert.Contains(TestSupport.Hex("website "), DrawLinked(rowEndsAtLink, Alignment.JUSTIFY));
        Assert.Contains(TestSupport.Hex("website "), DrawLinked(rowEndsAtLink, Alignment.LEFT));
        // A row not justified draws the space at the start of the next text line
        Assert.Contains("<" + TestSupport.Hex(" for"), DrawLinked(onOneRow, Alignment.LEFT));
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

    // A link that ends a paragraph has the space that every word has after it:
    // its underline stopped one space after the word (14 October 2026).
    [Fact]
    public void TheUnderlineOfTheLastWordStopsAtTheWord() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(font, "Tell the"))
                .Add(new TextLine(font, "users").SetURIAction("https://pdfjet.com").SetUnderline(true));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(300f).DrawOn(page);
        String content = TestSupport.Content(page);
        Match line = Regex.Match(content, "([0-9.]+) [0-9.]+ m\n([0-9.]+) [0-9.]+ l");
        Assert.True(line.Success, "no underline:\n" + content);
        float x1 = float.Parse(line.Groups[1].Value, CultureInfo.InvariantCulture);
        float x2 = float.Parse(line.Groups[2].Value, CultureInfo.InvariantCulture);
        TestSupport.AssertNear(font.StringWidth("users"), x2 - x1, TestSupport.DELTA, "the underline");
    }

    // Every text line is one word joined to the word before it, so the width
    // of the words joined to a word was measured over and over. Four times as
    // many lines take about four times the time, not sixteen; the two times
    // are measured in the same run, the shortest of three each, so that a
    // busy computer slows both (a limit of 3 s failed under a fuzzer's load,
    // 9 October 2026). At most twelve times: a limit of ten failed at 10.4 on
    // a slow runner of the CI (29 October 2026), and measuring over and over
    // again would take sixteen.
    [Fact]
    public void ManyJoinedTextLinesAreMeasuredInLinearTime() {
        TimeSpan small = JoinedLinesTime(5000);
        TimeSpan large = JoinedLinesTime(20000);
        Assert.True(large.Ticks <= 12 * small.Ticks, "20000 joined lines took "
                + large.TotalMilliseconds + " ms, and 5000 " + small.TotalMilliseconds + " ms");
    }

    private static TimeSpan JoinedLinesTime(int count) {
        TimeSpan shortest = TimeSpan.MaxValue;
        for (int run = 0; run < 3; run++) {
            PDF pdf = TestSupport.NewPDF();
            Font font = TestSupport.Helvetica(pdf);
            Paragraph paragraph = new Paragraph(new TextLine(font, "word"));
            for (int i = 0; i < count; i++) {
                paragraph.AddJoined(new TextLine(font, "x"));
            }
            Page page = new Page(pdf, Letter.PORTRAIT);
            Stopwatch watch = Stopwatch.StartNew();
            new TextFrame(new List<Paragraph> {paragraph}).SetLocation(10f, 10f).SetWidth(300f).DrawOn(page);
            if (watch.Elapsed < shortest) {
                shortest = watch.Elapsed;
            }
        }
        return shortest;
    }

    // A word wider than the frame is cut between its characters as TextBlock
    // cuts one: never before a Thai vowel that stays with the letter before
    // it, nor after one written before its letter. TextFrame cut after any
    // code point and drew a row "าษาไ" (review of 11 October 2026, #14).
    [Fact]
    public void CutsAWordWhereTextBlockDoes() {
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.RepoPath("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.ttf"));
        font.SetSize(12f);
        String word = "ภาษาไทยภาษาไทยภาษาไทย";
        TextLine textLine = new TextLine(font, word);
        for (float w = 1f; w <= 120f; w++) {
            TextFrame tf = new TextFrame(new List<Paragraph>()).SetWidth(w);
            String rest = word;
            while (rest.Length > 0) {
                String head = tf.HeadThatFits(textLine, rest);
                int last = char.ConvertToUtf32(head, head.Length - (char.IsLowSurrogate(head[head.Length - 1]) ? 2 : 1));
                Assert.False(TextBlock.IsLeadingVowel(last) && head.Length < rest.Length,
                        "width " + w + ": \"" + head + "\" ends on a vowel written before its letter");
                rest = rest.Substring(head.Length);
                Assert.False(rest.Length > 0 && TextBlock.StaysWithPrevious(char.ConvertToUtf32(rest, 0)),
                        "width " + w + ": \"" + rest + "\" starts a row on a vowel that stays with the letter before it");
            }
        }
    }

    // A word too wide for the frame breaks at a soft hyphen, the row ending
    // with a hyphen, and no soft hyphen is drawn (review of 11 October 2026,
    // #21).
    [Fact]
    public void BreaksAWordAtASoftHyphen() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        string content = DrawJoined(TextBlockTest.SoftHyphenWidth(font), null, TextBlockTest.SoftHyphenWord);
        Assert.True(content.Contains(TestSupport.Hex("Donaudampf-"), StringComparison.Ordinal),
                "no row of Donaudampf-: " + content);
        foreach (System.Text.RegularExpressions.Match match in
                System.Text.RegularExpressions.Regex.Matches(content, "<([0-9A-F]+)>")) {
            string hex = match.Groups[1].Value;
            for (int i = 0; i + 2 <= hex.Length; i += 2) {
                Assert.False(hex.Substring(i, 2) == "AD", "a soft hyphen is drawn: " + content);
            }
        }
    }

    // Returns the rows of text that a core font drew on the page, one text line
    // each, in the order they were drawn: the baseline of each, from the top
    // of the page, and its text.
    private static List<(float y, string text)> RowsOf(Page page) {
        List<(float y, string text)> rows = new List<(float y, string text)>();
        foreach (Match m in Regex.Matches(TestSupport.Content(page), "([0-9.]+) ([0-9.]+) Td\n\\[<([0-9A-F]*)>\\] TJ")) {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            string hex = m.Groups[3].Value;
            for (int i = 0; i + 2 <= hex.Length; i += 2) {
                text.Append((char) Convert.ToInt32(hex.Substring(i, 2), 16));
            }
            rows.Add((page.GetHeight() - float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), text.ToString()));
        }
        return rows;
    }

    // A frame that breaks inside a text line ends below the last row it drew
    // of it: GetTextBottom gave the bottom of the last whole text line, here
    // the top of the frame, as if nothing were drawn (the review of 28 October
    // 2026).
    [Fact]
    public void GetTextBottomIsTheBottomOfTheLastRowWhenTheFrameBreaksInsideATextLine() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<string> words = new List<string>();
        for (int i = 1; i <= 400; i++) {
            words.Add(i.ToString(CultureInfo.InvariantCulture));
        }
        TextFrame frame = new TextFrame(font, new List<string> {string.Join(" ", words)}).SetWidth(200f).SetHeight(100f);
        float descent = font.GetDescent(font.GetSize());
        int next = 1;   // The number of the word the frame draws next
        foreach (float y in new float[] {100f, 300f}) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            frame.SetLocation(10f, y).DrawOn(page);
            List<(float y, string text)> rows = RowsOf(page);
            Assert.True(rows.Count >= 2, "at " + y + ": " + rows.Count + " rows");
            // The frame goes on with the word after the last one it drew.
            Assert.Equal(next.ToString(CultureInfo.InvariantCulture), rows[0].text.Trim().Split(' ')[0]);
            (float y, string text) last = rows[rows.Count - 1];
            TestSupport.AssertNear(last.y + descent, frame.GetTextBottom(), TestSupport.DELTA, "the bottom of the last row");
            Assert.True(frame.GetTextBottom() > y + 90f && frame.GetTextBottom() <= y + 100f,
                    "at " + y + ": the text of the frame ends at " + frame.GetTextBottom());
            string[] lastWords = last.text.Trim().Split(' ');
            next = int.Parse(lastWords[lastWords.Length - 1], CultureInfo.InvariantCulture) + 1;
        }
    }

    // A font that kerns a space with the letters beside it, as Source Serif 4
    // does T, V, W and Y, made a justified row too wide: it was measured whole,
    // with the kerning, and its words placed one by one, without it, so the
    // rows ended past the right edge (the review of 28 October 2026).
    [Fact]
    public void AJustifiedRowInAKernedFontEndsAtTheRightEdge() {
        PDF pdf = TestSupport.NewPDF();
        Font font = KerningTest.KernedFont(pdf, "fonts/SourceSerif4/SourceSerif4-Regular.ttf", true);
        font.SetSize(14f);
        string text = "A Vote Was Taken Yesterday To Vary The Way We Work, and To Try It Today: "
                + "We Vote, You Wait, They Vary It, We Try Again, Today, Tomorrow, Yesterday.";
        Paragraph paragraph = new Paragraph().Add(new TextLine(font, text)).SetTextAlignment(Alignment.JUSTIFY);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextFrame(new List<Paragraph> {paragraph}).SetLocation(40f, 40f).SetWidth(300f).DrawOn(page);
        // Each word of a justified row is drawn at a location of its own, and
        // the last row whole, at one: the last location of a row is its last
        // word's.
        string[] words = text.Split(' ');
        MatchCollection locations = Regex.Matches(TestSupport.Content(page), "([0-9.]+) ([0-9.]+) Td");
        int rows = 0;
        for (int i = 0; i < locations.Count - 1; i++) {
            if (locations[i + 1].Groups[2].Value != locations[i].Groups[2].Value) {
                float x = float.Parse(locations[i].Groups[1].Value, CultureInfo.InvariantCulture);
                TestSupport.AssertNear(340f, x + font.StringWidth(14f, words[i]), TestSupport.DELTA,
                        "the end of the row of " + words[i]);
                rows++;
            }
        }
        Assert.True(rows >= 2, rows + " justified rows");
    }
}
}
