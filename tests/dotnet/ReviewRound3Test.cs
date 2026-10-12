/*
 * ReviewRound3Test.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The findings of the third review, of 12 October 2026, as tests.
/// </summary>
public class ReviewRound3Test {
    // A text rendering mode set back to 0 is written: Tr lasts past ET, and
    // the text after an invisible line stayed invisible (F1).
    [Fact]
    public void TheTextRenderingModeIsSetBackToZero() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "plain").SetLocation(50f, 50f).DrawOn(page);
        Assert.DoesNotContain(" Tr\n", TestSupport.Content(page));
        page.SetTextRenderingMode(3);
        new TextLine(font, "hidden").SetLocation(50f, 100f).DrawOn(page);
        page.SetTextRenderingMode(0);
        new TextLine(font, "VISIBLE AGAIN").SetLocation(50f, 150f).DrawOn(page);
        string content = TestSupport.Content(page);
        string before = content.Substring(0, content.IndexOf(TestSupport.Hex("VISIBLE AGAIN"), StringComparison.Ordinal));
        int zero = before.LastIndexOf("0 Tr\n", StringComparison.Ordinal);
        Assert.True(zero != -1 && before.LastIndexOf("3 Tr\n", StringComparison.Ordinal) < zero, content);
    }

    // A text frame at NaN, or a Markdown text with NaN margins, is refused:
    // no word was placed, and pages were made until the memory ran out (F4).
    [Fact]
    public void ANaNLocationOrMarginIsRefused() {
        float[][] cases = {
            new float[] {float.NaN, 50f, 200f, 0f}, new float[] {50f, float.NaN, 200f, 0f},
            new float[] {50f, 50f, float.NaN, 0f}, new float[] {50f, 50f, 200f, float.PositiveInfinity}};
        foreach (float[] xywh in cases) {
            PDF pdf = TestSupport.NewPDF();
            TextFrame frame = new TextFrame(TestSupport.Helvetica(pdf), new List<string> {"Hello frame text"})
                    .SetWidth(xywh[2]).SetHeight(xywh[3]);
            frame.SetLocation(xywh[0], xywh[1]);
            List<Page> pages = new List<Page>();
            Exception e = Assert.Throws<ArgumentException>(() => frame.DrawOn(pdf, pages, Letter.PORTRAIT));
            Assert.Equal("The text frame's location and size must be numbers, not NaN or infinite.", e.Message);
            Assert.Empty(pages);
        }
        foreach (float margin in new float[] {float.NaN, float.PositiveInfinity}) {
            PDF pdf = TestSupport.NewPDF();
            Font font = TestSupport.Helvetica(pdf);
            Markdown markdown = new Markdown(font, font, font, font, font).SetMargins(margin, 72f, 72f, 72f);
            List<Page> pages = new List<Page>();
            Exception e = Assert.Throws<ArgumentException>(
                    () => markdown.DrawOn(pdf, "Some *text*.", pages, Letter.PORTRAIT));
            Assert.Equal("The margins of the Markdown must be numbers, not NaN or infinite.", e.Message);
            Assert.Empty(pages);
        }
    }

    // The rows of Markdown tables past 100,000 cells are counted, not kept:
    // each row was padded to the header's 64 columns first, and 2 MB of rows
    // of "|" made 1.65 GB of cells before the document was refused (C2).
    [Fact]
    public void TheRowsPastTheTableLimitAreNotKept() {
        System.Text.StringBuilder text = new System.Text.StringBuilder("|");
        for (int i = 0; i < 64; i++) {
            text.Append(" a |");
        }
        text.Append("\n|");
        for (int i = 0; i < 64; i++) {
            text.Append("---|");
        }
        text.Append("\n");
        for (int i = 0; i < 3000; i++) {
            text.Append("|\n");
        }
        List<MarkdownParser.Block> blocks = MarkdownParser.Parse(text.ToString());
        Assert.True(blocks[0].rows.Count <= 100000 / 64 + 1, blocks[0].rows.Count + " rows kept");
        Assert.Equal(64 * 3001, MarkdownParser.TableCells(blocks));
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Markdown markdown = new Markdown(font, font, font, font, font);
        Exception e = Assert.Throws<ArgumentException>(
                () => markdown.DrawOn(pdf, text.ToString(), new List<Page>(), Letter.PORTRAIT));
        Assert.Equal("The tables of the Markdown have more than 100000 cells: draw a table that large with BigTable.",
                e.Message);
    }
}
}
