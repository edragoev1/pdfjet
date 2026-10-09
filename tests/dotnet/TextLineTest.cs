/*
 * TextLineTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class TextLineTest {
    [Fact]
    public void TheUnderlineAndTheStrikeoutOfTaggedTextAreArtifacts() {
        // The line is decoration: an element of its own, described as
        // "Underlined text: " and the text, is read after the text again.
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        PDF pdf = new PDF(output, Compliance.PDF_UA_1).SetTitle("Test");
        pdf.SetTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine line = new TextLine(TestSupport.Helvetica(pdf), "Hello");
        line.SetUnderline(true);
        line.SetStrikeout(true);
        line.SetLocation(10f, 20f);
        line.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal(2, content.Split("/Artifact BMC\n").Length - 1);
        pdf.Complete();
        string raw = TestSupport.Latin1(output.ToArray());
        Assert.Equal(1, raw.Split("/S /P\n").Length - 1);
        Assert.DoesNotContain("/Alt ", raw);
    }

    [Fact]
    public void DrawOnWritesTheTextAsHexAtTheFlippedY() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine line = new TextLine(TestSupport.Helvetica(pdf), "Hello (x)").SetLocation(10f, 20f);
        float[] xy = line.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("10 772 Td\n", content);
        Assert.Contains("[<" + TestSupport.Hex("Hello (x)") + ">] TJ\n", content);
        TestSupport.AssertNear(44.664f, line.GetWidth(), 0.001f);
        TestSupport.AssertNear(10f + line.GetWidth(), xy[0], TestSupport.DELTA);
    }

    [Fact]
    public void EmptyTextDrawsNothingAndReturnsTheLocation() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TestSupport.AssertXY(5f, 6f, new TextLine(TestSupport.Helvetica(pdf), "").SetLocation(5f, 6f).DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void GetLocationReturnsACopyOfTheLocation() {
        TextLine line = new TextLine(TestSupport.Helvetica(TestSupport.NewPDF()), "x").SetLocation(5f, 6f);
        line.GetLocation()[1] = 60f;
        TestSupport.AssertXY(5f, 6f, line.GetLocation());
    }

    [Fact]
    public void ColorSettersConvertAndCopy() {
        TextLine line = new TextLine(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        line.SetTextColor(0xFF8000);
        TestSupport.AssertRGB(1f, 128f / 255f, 0f, line.GetTextColor());

        float[] rgb = {0.1f, 0.2f, 0.3f};
        line.SetTextColor(rgb);
        rgb[0] = 0.9f;
        line.GetTextColor()[1] = 0.9f;
        TestSupport.AssertRGB(0.1f, 0.2f, 0.3f, line.GetTextColor());
    }

    [Fact]
    public void UnderlineAddsAStrokedLine() {
        PDF pdf = TestSupport.NewPDF();
        Page plain = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Hello").SetLocation(10f, 20f).DrawOn(plain);
        Assert.DoesNotContain("\nS\n", TestSupport.Content(plain));

        Page underlined = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Hello").SetLocation(10f, 20f).SetUnderline(true).DrawOn(underlined);
        Assert.Contains("\nS\n", TestSupport.Content(underlined));
    }

    [Fact]
    public void SetFontChangesTheFallbackFontUnlessAnotherWasSet() {
        PDF pdf = TestSupport.NewPDF();
        Font helvetica = TestSupport.Helvetica(pdf);
        Font courier = new Font(pdf, CoreFont.COURIER);
        TextLine line = new TextLine(helvetica, "x").SetFont(courier);
        Assert.Same(courier, line.GetFallbackFont());
        line.SetFallbackFont(helvetica).SetFont(new Font(pdf, CoreFont.TIMES_ROMAN));
        Assert.Same(helvetica, line.GetFallbackFont());
    }

    // The links of the page, each the width of the word it holds.
    private static void CheckLinks(string what, Page page, Font font, string[] words) {
        Assert.Equal(words.Length, page.annots.Count);
        for (int i = 0; i < words.Length; i++) {
            Annotation annot = page.annots[i];
            TestSupport.AssertNear(new TextLine(font, words[i]).GetWidth(), annot.x2 - annot.x1,
                    TestSupport.DELTA, what + " " + words[i]);
        }
    }

    [Fact]
    public void TheLinkOfAWordDrawnWithItsSpaceEndsAtTheWord() {
        // A word of a TextColumn, or of a justified TextFrame row, is drawn with
        // the space after it, and its link reached one space past the word. The
        // box now holds the text that shows.
        string[] words = {"Click", "here", "for", "the", "whole", "story", "of", "it"};
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(page.pdf);
        TextColumn column = new TextColumn();
        column.SetWidth(120f);
        column.SetLocation(50f, 50f);
        column.AddParagraph(new Paragraph().Add(
                new TextLine(font, string.Join(" ", words)).SetURIAction("https://pdfjet.com")));
        column.DrawOn(page);
        CheckLinks("TextColumn", page, font, words);

        // Rows of two words, justified and so drawn a word at a time, and a
        // last row of one, drawn as it is.
        words = new string[] {"word", "word", "word", "word", "word"};
        page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        font = TestSupport.Helvetica(page.pdf);
        Paragraph paragraph = new Paragraph().SetTextAlignment(Alignment.JUSTIFY);
        paragraph.Add(new TextLine(font, string.Join(" ", words)).SetURIAction("https://pdfjet.com"));
        TextFrame frame = new TextFrame(new System.Collections.Generic.List<Paragraph> { paragraph })
                .SetWidth(new TextLine(font, "word word").GetWidth() + 2f);
        frame.SetLocation(50f, 50f);
        frame.DrawOn(page);
        CheckLinks("justified TextFrame", page, font, words);

        // A text line of its own keeps the spaces it is given out of its box too.
        page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "  link  ").SetURIAction("https://pdfjet.com");
        line.SetLocation(100f, 100f);
        line.DrawOn(page);
        Annotation annot = page.annots[0];
        TestSupport.AssertNear(100f + new TextLine(font, "  ").GetWidth(), annot.x1, TestSupport.DELTA, "left");
        TestSupport.AssertNear(new TextLine(font, "link").GetWidth(), annot.x2 - annot.x1, TestSupport.DELTA, "width");
    }
}
}
