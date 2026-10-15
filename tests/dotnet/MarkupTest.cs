/*
 * MarkupTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public class MarkupTest {
    private Font regular;
    private Font bold;
    private Font italic;
    private Font boldItalic;
    private Font code;

    private Markup NewMarkup() {
        return NewMarkup(TestSupport.NewPDF());
    }

    private Markup NewMarkup(PDF pdf) {
        regular = new Font(pdf, CoreFont.HELVETICA);
        bold = new Font(pdf, CoreFont.HELVETICA_BOLD);
        italic = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);
        boldItalic = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);
        code = new Font(pdf, CoreFont.COURIER);
        return new Markup(regular, bold, italic, boldItalic, code);
    }

    // The text lines of the paragraph, each as its text and its font: R, B, I,
    // X for bold italic, or C for code, then ~ when it is struck through, with
    // the link after a space, and a + before the text of one that is joined to
    // the text line before it.
    private List<String> Describe(Paragraph paragraph) {
        List<String> list = new List<String>();
        for (int i = 0; i < paragraph.lines.Count; i++) {
            TextLine line = paragraph.lines[i];
            Font font = line.GetFont();
            String style = (font == bold) ? "B" : (font == italic) ? "I"
                    : (font == boldItalic) ? "X" : (font == code) ? "C" : "R";
            if (line.GetStrikeout()) {
                style += "~";
            }
            String link = (line.GetURIAction() == null) ? "" : " " + line.GetURIAction();
            list.Add((paragraph.JoinsPrevious(i) ? "+" : "") + line.GetText() + "|" + style + link);
        }
        return list;
    }

    private List<String> Parse(String text) {
        return Describe(NewMarkup().Paragraph(text));
    }

    private static List<String> Lines(params String[] lines) {
        return new List<String>(lines);
    }

    [Fact]
    public void PlainTextIsOneTextLine() {
        Assert.Equal(Lines("Just text, no marks.|R"), Parse("Just text, no marks."));
        Assert.Equal(Lines("one two|R"), Parse("one\ntwo"));
    }

    [Fact]
    public void BoldItalicAndBoth() {
        Assert.Equal(Lines("a |R", "b|B", " c|R"), Parse("a **b** c"));
        Assert.Equal(Lines("a |R", "b|I", " c|R"), Parse("a *b* c"));
        Assert.Equal(Lines("a |R", "b|X", " c|R"), Parse("a ***b*** c"));
        Assert.Equal(Lines("a |B", "b|X", " c|B"), Parse("**a *b* c**"));
    }

    [Fact]
    public void PunctuationAfterAStyleIsJoinedToTheWord() {
        Assert.Equal(Lines("Hello, |R", "world|B", "+!|R"), Parse("Hello, **world**!"));
        Assert.Equal(Lines("un|R", "+believ|I", "+able|R"), Parse("un*believ*able"));
    }

    [Fact]
    public void CodeKeepsItsTextAsItIs() {
        Assert.Equal(Lines("Call |R", "a*b*c|C", "+.|R"), Parse("Call `a*b*c`."));
        Assert.Equal(Lines("a `b` c|C"), Parse("`` a `b` c ``"));
        Assert.Equal(Lines("x|B", "+y|C", "+z|B"), Parse("**x`y`z**"));
    }

    [Fact]
    public void Links() {
        Assert.Equal(Lines("See |R", "PDFjet|R https://pdfjet.com", "+.|R"),
                Parse("See [PDFjet](https://pdfjet.com)."));
        Assert.Equal(Lines("a |R https://x", "b|B https://x"), Parse("[a **b**](https://x)"));
        Assert.Equal(Lines("go|B u"), Parse("**[go](u)**"));
    }

    [Fact]
    public void ALinkNeedsItsBracketsAParenthesisAndAURLWithoutSpaces() {
        Assert.Equal(Lines("[a] (b)|R"), Parse("[a] (b)"));
        Assert.Equal(Lines("[a](b c)|R"), Parse("[a](b c)"));
        Assert.Equal(Lines("[a]()|R"), Parse("[a]()"));
        Assert.Equal(Lines("[a](b|R"), Parse("[a](b"));
        // A link is not in a link.
        Assert.Equal(Lines("[b](u) c|R v"), Parse("[[b](u) c](v)"));
    }

    [Fact]
    public void MarksWithNoMatchAreText() {
        Assert.Equal(Lines("2 * 3 = 6|R"), Parse("2 * 3 = 6"));
        Assert.Equal(Lines("**a|R"), Parse("**a"));
        Assert.Equal(Lines("a*|R"), Parse("a*"));
        Assert.Equal(Lines("*|R", "+a|I"), Parse("**a*"));
        Assert.Equal(Lines("`not code|R"), Parse("`not code"));
        Assert.Equal(Lines("[not a link|R"), Parse("[not a link"));
    }

    [Fact]
    public void ABackslashMakesAMarkText() {
        Assert.Equal(Lines("*not italic*|R"), Parse("\\*not italic\\*"));
        Assert.Equal(Lines("[a](b)|R"), Parse("\\[a](b)"));
        Assert.Equal(Lines("a\\b \\|R"), Parse("a\\b \\"));
    }

    [Fact]
    public void ParagraphsAreSeparatedByEmptyLines() {
        List<Paragraph> paragraphs = NewMarkup().Paragraphs("One **two**\nthree.\n\n  \nFour.\r\n\r\n");
        Assert.Equal(2, paragraphs.Count);
        Assert.Equal(Lines("One |R", "two|B", " three. |R"), Describe(paragraphs[0]));
        Assert.Equal(Lines("Four. |R"), Describe(paragraphs[1]));
        Assert.Empty(NewMarkup().Paragraphs(" \n\n"));
    }

    [Fact]
    public void ALinkIsColoredAndUnderlined() {
        Paragraph paragraph = NewMarkup().SetLinkColor(Color.red).Paragraph("[a](u)");
        TextLine line = paragraph.lines[0];
        Assert.True(line.GetUnderline());
        TestSupport.AssertRGB(1f, 0f, 0f, line.GetTextColor());
        // The underline in the color of the link, not black
        TestSupport.AssertRGB(1f, 0f, 0f, line.GetDecorationColor());
    }

    [Fact]
    public void LongInputsAreReadInLinearTime() {
        // Unmatched brackets, backticks of every length and runs of * would
        // each take quadratic time with a naive search.
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 20000; i++) {
            text.Append("[a](b *c `").Append(i % 50 == 0 ? "``" : "").Append(" [[");
        }
        Paragraph paragraph = null;
        long milliseconds = TestSupport.ShortestMillis(2000, () => paragraph = NewMarkup().Paragraph(text.ToString()));
        Assert.NotEmpty(paragraph.lines);
        Assert.True(milliseconds < 2000, milliseconds + " ms");
    }

    // GitHub's Markdown: _ emphasizes as * does, but only at the edge of a word,
    // so that snake_case and file names are text.
    [Fact]
    public void UnderscoresEmphasizeAtTheEdgesOfWords() {
        Assert.Equal(Lines("italic|I", " and |R", "bold|B"), Parse("_italic_ and __bold__"));
        Assert.Equal(Lines("snake_case_name stays|R"), Parse("snake_case_name stays"));
        Assert.Equal(Lines("file_name and |R", "x|I"), Parse("file_name and _x_"));
        Assert.Equal(Lines("a |B", "b|X", " c|B"), Parse("__a _b_ c__"));
        Assert.Equal(Lines("(|R", "+x|I", "+)|R"), Parse("(_x_)"));
        Assert.Equal(Lines("_open only|R"), Parse("_open only"));
        // The * closes first, and the _ inside it can no longer match.
        Assert.Equal(Lines("a _b|I", " c_|R"), Parse("*a _b* c_"));
    }

    // Two ~ strike through, as in GitHub's Markdown; one or three are text.
    [Fact]
    public void TwoTildesStrikeThrough() {
        Assert.Equal(Lines("gone|R~", " here|R"), Parse("~~gone~~ here"));
        Assert.Equal(Lines("a |R", "b |R~", "c|I~", " d|R"), Parse("a ~~b *c*~~ d"));
        Assert.Equal(Lines("~5 and ~~~x~~~|R"), Parse("~5 and ~~~x~~~"));
        Assert.Equal(Lines("~~ a ~~|R"), Parse("~~ a ~~"));
        // They nest, and do not cross.
        Assert.Equal(Lines("a *b|R~", " c*|R"), Parse("~~a *b~~ c*"));
        Assert.Equal(Lines("a ~~b|I", " c~~|R"), Parse("*a ~~b* c~~"));
        Assert.Equal(Lines("a |B", "b|B~", " c|B"), Parse("**a ~~b~~ c**"));
    }

    // An address of http:// or https:// is a link: in < and >, or alone,
    // without the punctuation at its end and a ) that closes no ( of its own.
    [Fact]
    public void AnAddressIsALink() {
        Assert.Equal(Lines("see |R", "https://pdfjet.com|R https://pdfjet.com", "+.|R"),
                Parse("see https://pdfjet.com."));
        Assert.Equal(Lines("(|R", "+https://pdfjet.com|R https://pdfjet.com", "+)|R"),
                Parse("(https://pdfjet.com)"));
        Assert.Equal(Lines("https://en.wikipedia.org/wiki/Foo_(bar)|R https://en.wikipedia.org/wiki/Foo_(bar)"),
                Parse("https://en.wikipedia.org/wiki/Foo_(bar)"));
        Assert.Equal(Lines("go to |R", "http://a.b/c?d=1&e=2|R http://a.b/c?d=1&e=2", "+, then|R"),
                Parse("go to http://a.b/c?d=1&e=2, then"));
        Assert.Equal(Lines("https://pdfjet.com/a_b|R https://pdfjet.com/a_b"), Parse("<https://pdfjet.com/a_b>"));
        Assert.Equal(Lines("https://pdfjet.com|I https://pdfjet.com"), Parse("*https://pdfjet.com*"));
        Assert.Equal(Lines("text|R https://x.com", "https://y.com|R https://y.com"),
                Parse("[text](https://x.com) https://y.com"));
        Assert.Equal(Lines("xhttps://a.b|R"), Parse("xhttps://a.b"));
        Assert.Equal(Lines("HTTPS://A.B/x|R HTTPS://A.B/x"), Parse("HTTPS://A.B/x"));
        Assert.Equal(Lines("Http://a.b|R Http://a.b"), Parse("<Http://a.b>"));
        // Only the ASCII letters are folded: ſ is not an s.
        Assert.Equal(Lines("httpſ://a.b|R"), Parse("httpſ://a.b"));
        Assert.Equal(Lines("http:// alone|R"), Parse("http:// alone"));
        Assert.Equal(Lines("<http://a b>|R"), Parse("<http://a b>"));
    }

    [Fact]
    public void LongAddressesAreReadInLinearTime() {
        // The ) at the end of an address, and < that no > closes, would each
        // take quadratic time with a naive search.
        StringBuilder text = new StringBuilder();
        text.Append("http://a");
        for (int i = 0; i < 40000; i++) {
            text.Append(')');
        }
        for (int i = 0; i < 20000; i++) {
            text.Append("<http://a_~");
        }
        Paragraph paragraph = null;
        long milliseconds = TestSupport.ShortestMillis(2000, () => paragraph = NewMarkup().Paragraph(text.ToString()));
        Assert.NotEmpty(paragraph.lines);
        Assert.True(milliseconds < 2000, milliseconds + " ms");
    }

    [Fact]
    public void TheParagraphDrawsWithNoSpaceBeforeJoinedPunctuation() {
        PDF pdf = TestSupport.NewPDF();
        Markup markup = NewMarkup(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextFrame(new List<Paragraph> {markup.Paragraph("one **two**, three")})
                .SetLocation(10f, 10f).SetWidth(300f).DrawOn(page);
        String content = TestSupport.Content(page);
        float[] two = TestSupport.PositionOf(content, "two");
        float[] comma = TestSupport.PositionOf(content, ",");
        TestSupport.AssertNear(two[0] + bold.StringWidth("two"), comma[0], TestSupport.DELTA);
    }
}
}   // End of namespace PDFjet.NET
