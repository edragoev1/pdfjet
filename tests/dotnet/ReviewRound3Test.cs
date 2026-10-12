/*
 * ReviewRound3Test.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
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

    // Two characters copied as the same text, as a letter and its
    // presentation form, need no actual text for the second: Arabic text had
    // three times the spans it needed, its content 27% larger (T2).
    [Fact]
    public void ACharacterCopiedAsTheFirstHasNoActualText() {
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.RepoPath("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        Assert.Equal(font.unicodeToGID.Get(0x0644), font.unicodeToGID.Get(0xFEDD));
        font.UseChar(0xFEDD, font.unicodeToGID.Get(0xFEDD));
        Assert.False(font.DrawnAsAnother(0x0644));
        font.UseChar(0x0661, font.unicodeToGID.Get(0x0661));
        Assert.True(font.DrawnAsAnother(0x06F1));
    }

    // A glyph drawn for another character stays in the span of a word with
    // moved marks, whose actual text gives the character: in a span of its
    // own, the word's span began with a moved mark and the space that stands
    // for it, and "ةِمى" was copied as "ىمِ ة" (T1).
    [Fact]
    public void AWordWithMarksIsCopiedWithoutASpaceInside() {
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.RepoPath("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "\u0628").SetLocation(50f, 50f).DrawOn(page);
        // The glyph of ة as drawn first for another character, which no two
        // characters of this font copied as different text are
        font.program.chars[font.unicodeToGID.Get(0x0629)] = 'X';
        new TextLine(font, "\u0629\u0650\u0645\u0649").SetLocation(50f, 100f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain("/ActualText <FEFF0020", content);
        Assert.Contains("/ActualText <" + Page.ToUTF16Hex("\u0629\u0650\u0645\u0649") + ">", content);
    }

    // A cell's word broken between its characters breaks again at a soft
    // hyphen, as TextBlock and TextFrame do: "1234<SHY>5678" was
    // 123 / 4<SHY>56 / 78 (T4).
    [Fact]
    public void ACellBreaksAtASoftHyphenAfterACharacterBreak() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Cell cell = new Cell(font, "1234\u00AD5678");
        cell.SetWidth(font.StringWidth("123"));
        cell.SetLeftPadding(0f);
        cell.SetRightPadding(0f);
        List<string> lines = new List<string>();
        foreach (string line in new Table().WrapCellText(new List<Cell> {cell}, 0)) {
            lines.Add(line.Replace("\u00AD", ""));
        }
        Assert.Equal(new List<string> {"123", "4-", "567", "8"}, lines);
        Cell wide = new Cell(font, "W");
        wide.SetWidth(1f);
        Assert.Equal(new List<string> {"W"}, new Table().WrapCellText(new List<Cell> {wide}, 0));
    }

    // A CJK font fits all of a text in a width of more characters than it
    // has, as 1e30 or an infinity, in every port: Go and C# fit none (T6).
    [Fact]
    public void ACJKFontFitsAllOfATextInAHugeWidth() {
#pragma warning disable CS0618 // The CJK fonts are deprecated, and still tested
        Font font = new Font(TestSupport.NewPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
#pragma warning restore CS0618
        string text = "\u4E2D\u6587\u5B57";
        foreach (float width in new float[] {1e30f, float.PositiveInfinity, float.MaxValue}) {
            Assert.Equal(3, font.GetFitChars(text, width));
        }
        foreach (float width in new float[] {float.NaN, -1f, 0f}) {
            Assert.Equal(0, font.GetFitChars(text, width));
        }
        Assert.Equal(2, font.GetFitChars(text, 2f * font.GetSize()));
    }

    // A zero width joiner the font has no glyph for, in left to right text
    // between two LRMs, is in the actual text of the run, as the stamp keeps
    // it: it was left out (T7).
    [Fact]
    public void AJoinerInARunIsInItsActualText() {
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.RepoPath("fonts/NotoSansJP/NotoSansJP-Regular.ttf"));
        Assert.Equal(0, font.unicodeToGID.Get(0x200D));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "d\u200D\u200E\u200Ce\u200D").SetLocation(50f, 50f).DrawOn(page);
        Assert.Contains("/ActualText <" + Page.ToUTF16Hex("\u200Ee\u200D\u200E") + ">", TestSupport.Content(page));
    }

    // A lone surrogate in the text of a stamp is drawn as a character the font
    // does not have, as in Java and Go: it threw (T8).
    [Fact]
    public void ALoneSurrogateInAStampIsDrawn() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.PlexSans(pdf);
        Stamp stamp = new Stamp(pdf);
        stamp.DrawText(font, 12f, 0f, 12f, "a\uD800b\uDC00c\uD800");
        MemoryStream buf = (MemoryStream) typeof(Stamp).GetField("buf",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(stamp);
        string content = TestSupport.Latin1(buf.ToArray());
        Assert.Contains("/ActualText <" + Page.ToUTF16Hex("\uD800") + ">", content);
    }

    // A /MediaBox that refers to an object of more than a box is not read:
    // 1,000 pages over an array of a million numbers, a PDF of 2 MB, copied
    // the array for each page, 15 GB in Go (N1).
    [Fact]
    public void AMediaBoxOfAnotherObjectIsReadOnlyWhenItIsABox() {
        int pages = 200;
        var kids = new System.Text.StringBuilder();
        for (int i = 0; i < pages; i++) {
            kids.Append(5 + i).Append(" 0 R ");
        }
        var numbers = new System.Text.StringBuilder("[");
        for (int i = 0; i < 100000; i++) {
            numbers.Append("0 ");
        }
        var objects = new List<string> {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [" + kids + "] /Count " + pages + " >>",
            numbers.Append("]").ToString(),
            "[0 0 612 792]"};
        for (int i = 0; i < pages; i++) {
            objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox " + ((i == 0) ? "4" : "3") + " 0 R >>");
        }
        PDF pdf = TestSupport.NewPDF();
        List<PDFobj> read = TestSupport.Read(PDFTest.PdfWithObjects(objects.ToArray()));
        long before = GC.GetAllocatedBytesForCurrentThread();
        List<PDFobj> found = pdf.GetPageObjects(read);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated <= 64 << 20, (allocated >> 20) + " MB allocated");
        Assert.Equal(pages, found.Count);
        Assert.Equal("[ 0 0 612 792 ]", found[0].GetValue("/MediaBox"));
        Assert.Equal("3", found[1].GetValue("/MediaBox"));
    }

    // A /Font dictionary that is an object of its own, which small resources
    // of each page refer to, is added once: 4,000 pages over one of 4,000
    // fonts, a PDF of 1 MB, copied it for each page, 4.6 GB in Go (N2).
    [Fact]
    public void AFontDictionaryOfItsOwnIsAddedOnce() {
        int pages = 2000;
        int fonts = 2000;
        var kids = new System.Text.StringBuilder();
        for (int i = 0; i < pages; i++) {
            kids.Append(5 + i).Append(" 0 R ");
        }
        var entries = new System.Text.StringBuilder("<< ");
        for (int i = 0; i < fonts; i++) {
            entries.Append("/F").Append(i).Append(" 4 0 R ");
        }
        var objects = new List<string> {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [" + kids + "] /Count " + pages + " >>",
            entries.Append(">>").ToString(),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"};
        for (int i = 0; i < pages; i++) {
            objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font 3 0 R >> >>");
        }
        PDF pdf = TestSupport.NewPDF();
        List<PDFobj> read = TestSupport.Read(PDFTest.PdfWithObjects(objects.ToArray()));
        long before = GC.GetAllocatedBytesForCurrentThread();
        pdf.AddResourceObjects(read);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated <= 64 << 20, (allocated >> 20) + " MB allocated");
        var imported = (List<string>) typeof(PDF).GetField("importedFonts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(pdf);
        Assert.Equal(4 * fonts, imported.Count);
    }

    // A token of an object stream's header longer than any number is copied
    // no further than 20 bytes, and the TIFF predictor is undone in the data
    // the reader decoded: each copied a stream, uncharged (N3).
    [Fact]
    public void TheReaderMakesNoUnchargedCopyOfAStream() {
        string digits = new string('7', 100000);
        byte[] data = System.Text.Encoding.Latin1.GetBytes(digits + " 5");
        List<string> header = PDF.ObjectStreamHeader(data, data.Length, 2);
        Assert.Equal(new List<string> {digits.Substring(0, 20) + "...", "5"}, header);
        byte[] samples = {1, 1, 1, 5, 0, 0};
        Assert.Same(samples, Decompressor.ApplyPredictor(samples, 2, 1, 8, 3));
    }

    // A lone surrogate in a password is EF BF BD in its UTF-8, in every port:
    // Java's UTF-8 made it '?' (S5).
    [Fact]
    public void ALoneSurrogateInAPasswordIsTheReplacementCharacter() {
        Assert.Equal(new byte[] {(byte) 'a', 0xEF, 0xBF, 0xBD, (byte) 'b'}, SASLprep.Truncate("a\uD800b"));
        Assert.Equal(new byte[] {0xF0, 0x9F, 0x98, 0x80, 0xEF, 0xBF, 0xBD}, SASLprep.Truncate("\uD83D\uDE00\uDC00"));
    }

    // An image of a PDF whose /Width is past an int takes the largest or the
    // smallest, in every port: Go took the smallest int64, and C# the
    // smallest int (C8).
    [Fact]
    public void AWidthPastAnIntTakesTheLargestInt() {
        string[] widths = {"1e19", "1e400", "-1e19", "NaN", "12.7"};
        float[] pixels = {int.MaxValue, int.MaxValue, int.MinValue, 0f, 12f};
        for (int i = 0; i < widths.Length; i++) {
            PDFobj obj = new PDFobj();
            obj.dict = new List<string> {
                "<<", "/Type", "/XObject", "/Subtype", "/Image", "/Width", widths[i], "/Height", "1", ">>"};
            obj.stream = new byte[0];
            Assert.Equal(pixels[i], new Image(TestSupport.NewPDF(), obj).GetWidth());
        }
    }
}
}
