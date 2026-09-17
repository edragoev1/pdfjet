/*
 * TextColumnTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
public class TextColumnTest {
    private const string EIGHT_WORDS = "alpha beta gamma delta epsilon zeta eta theta";

    private static TextColumn Column(Font font, string text, Alignment alignment) {
        TextColumn column = new TextColumn();
        column.SetWidth(200f);
        column.SetTextAlignment(alignment);
        column.AddParagraph(new Paragraph(new TextLine(font, text)));
        column.SetLocation(100f, 100f);
        return column;
    }

    // The x of every Td of the content, in the order they are written, with the y.
    private static List<float[]> Positions(string content) {
        List<float[]> list = new List<float[]>();
        foreach (string line in content.Split('\n')) {
            if (line.EndsWith(" Td")) {
                string[] parts = line.Split(' ');
                list.Add(new float[] {
                        float.Parse(parts[0]), float.Parse(parts[1])});
            }
        }
        return list;
    }

    private static float LastXOfFirstLine(List<float[]> positions) {
        float y = positions[0][1];
        float lastX = 0f;
        foreach (float[] position in positions) {
            if (position[1] == y) {
                lastX = position[0];
            }
        }
        return lastX;
    }

    [Fact]
    public void AParagraphIsAsTallAsItsTextAndNoTaller() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        // The ascent and the descent of one line, not a whole line of spacing.
        TextColumn column = Column(font, "one short line", Alignment.LEFT);
        Assert.Equal(font.GetBodyHeight(font.GetSize()), column.GetSize().GetHeight(),
                TestSupport.DELTA);
        // A cell measures the column as it measures its own text.
        Cell withColumn = new Cell(font);
        TextColumn inCell = new TextColumn();
        inCell.SetWidth(200f);
        inCell.AddParagraph(new Paragraph(new TextLine(font, "one short line")));
        withColumn.SetTextColumn(inCell);
        Assert.Equal(new Cell(font, "one short line").GetHeight(200f),
                withColumn.GetHeight(200f), TestSupport.DELTA);
    }

    [Fact]
    public void RightAlignedTextReachesTheRightEdge() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Column(font, EIGHT_WORDS, Alignment.RIGHT).DrawOn(page);
        // "zeta" is the last token of the first line; its space is not text.
        float lastX = LastXOfFirstLine(Positions(TestSupport.Content(page)));
        Assert.Equal(300f, lastX + font.StringWidth(font.GetSize(), "zeta"), TestSupport.DELTA);
    }

    [Fact]
    public void AJustifiedLineReachesBothEdges() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Column(font, EIGHT_WORDS + " iota kappa", Alignment.JUSTIFY).DrawOn(page);
        List<float[]> positions = Positions(TestSupport.Content(page));
        Assert.Equal(100f, positions[0][0], TestSupport.DELTA);
        Assert.Equal(300f, LastXOfFirstLine(positions) + font.StringWidth(font.GetSize(), "zeta"),
                TestSupport.DELTA);
    }

    [Fact]
    public void AWordWiderThanTheColumnLeavesNoBlankLineAboveIt() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        TextColumn wide = new TextColumn();
        wide.SetWidth(120f);
        wide.AddParagraph(new Paragraph(
                new TextLine(font, "Supercalifragilisticexpialidocious bbb ccc")));
        wide.SetLocation(0f, 0f);
        // The long word on a line of its own and the two short words on the next.
        Assert.Equal(2f * font.GetBodyHeight(font.GetSize()), wide.GetSize().GetHeight(),
                TestSupport.DELTA);
        Page page = new Page(pdf, Letter.PORTRAIT);
        wide.SetLocation(100f, 100f);
        wide.DrawOn(page);
        List<float[]> positions = Positions(TestSupport.Content(page));
        // The first token is drawn on the first line, at the ascent of the font.
        Assert.Equal(100f + font.GetAscent(font.GetSize()), 792f - positions[0][1],
                TestSupport.DELTA);
    }

    [Fact]
    public void TheUnderlineOfALineStopsAtItsTextAndRunsThroughIt() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine underlined = new TextLine(font, EIGHT_WORDS);
        underlined.SetUnderline(true);
        TextColumn column = new TextColumn();
        column.SetWidth(200f);
        column.AddParagraph(new Paragraph(underlined));
        column.SetLocation(100f, 100f);
        column.DrawOn(page);
        // The segments of a line follow each other without a gap, and the last
        // one stops at the text rather than after the space that follows it.
        List<float[]> segments = new List<float[]>();
        string[] rows = TestSupport.Content(page).Split('\n');
        for (int i = 0; i < rows.Length - 1; i++) {
            if (rows[i].EndsWith(" m") && rows[i + 1].EndsWith(" l")) {
                segments.Add(new float[] {
                        float.Parse(rows[i].Split(' ')[0]),
                        float.Parse(rows[i + 1].Split(' ')[0]),
                        float.Parse(rows[i].Split(' ')[1])});
            }
        }
        Assert.True(segments.Count > 2);
        for (int i = 1; i < segments.Count; i++) {
            if (segments[i][2] == segments[i - 1][2]) {      // the same line
                Assert.Equal(segments[i - 1][1], segments[i][0], TestSupport.DELTA);
            }
        }
        // The first line ends with "zeta", underlined up to its last character
        // and no further: the space after it is not text.
        float endOfFirstLine = 0f;
        foreach (float[] segment in segments) {
            if (segment[2] == segments[0][2]) {
                endOfFirstLine = segment[1];
            }
        }
        float lastTokenX = LastXOfFirstLine(Positions(TestSupport.Content(page)));
        Assert.Equal(lastTokenX + font.StringWidth(font.GetSize(), "zeta"), endOfFirstLine,
                TestSupport.DELTA);
    }
}
}   // End of namespace PDFjet.NET
