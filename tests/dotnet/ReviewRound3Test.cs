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
        font.program.FirstChar(font.unicodeToGID.Get(0x0629), 'X');
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
        Font font = new Font(TestSupport.NewPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
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
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
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
        string[] widths = {"1e19", "1e400", "-1e19", "12.7"};
        float[] pixels = {int.MaxValue, int.MaxValue, int.MinValue, 12f};
        for (int i = 0; i < widths.Length; i++) {
            PDFobj obj = new PDFobj();
            obj.dict = new List<string> {
                "<<", "/Type", "/XObject", "/Subtype", "/Image", "/Width", widths[i], "/Height", "1", ">>"};
            obj.stream = new byte[0];
            Assert.Equal(pixels[i], new Image(TestSupport.NewPDF(), obj).GetWidth());
        }
        // Only a number in decimal, in the four ports (review of 17 October 2026)
        foreach (string width in new[] {"NaN", "nan", "100f", "0x10", "inf", " 12", "5 0 R"}) {
            PDFobj obj = new PDFobj();
            obj.dict = new List<string> {
                "<<", "/Type", "/XObject", "/Subtype", "/Image", "/Width", width, "/Height", "1", ">>"};
            obj.stream = new byte[0];
            Assert.Equal("The /Width of the image object is not a number: " + width,
                    Assert.Throws<InvalidDataException>(() => new Image(TestSupport.NewPDF(), obj)).Message);
        }
    }

    // An image turned a quarter of the way is as wide as it was tall: it gave
    // its corner unturned, and in a table cell ran over the rows after it (F3).
    [Fact]
    public void AnImageTurnedAQuarterHasItsDrawnSize() {
        PDF pdf = TestSupport.NewPDF();
        Image image = new Image(pdf, TestSupport.Open("PngSuite/BASN2C08.PNG"));   // 32 by 32
        image.ScaleBy(0.75f, 0.375f);                                               // 24 by 12
        image.SetLocation(50f, 50f);
        foreach (int degrees in new int[] {90, 270}) {
            image.SetRotation(degrees);
            TestSupport.AssertXY(62f, 74f, image.DrawOn(null));
            TestSupport.AssertNear(12f, image.GetWidth(), 0.01f);
            TestSupport.AssertNear(24f, image.GetHeight(), 0.01f);
            TestSupport.AssertXY(62f, 74f, image.DrawOn(new Page(pdf, Letter.PORTRAIT)));
        }
        image.SetRotation(0);
        TestSupport.AssertXY(74f, 62f, image.DrawOn(null));
        // Turned, the image fits the page as it is drawn
        image.SetRotation(90);
        image.ResizeToFit(new Page(pdf, Letter.PORTRAIT), false);
        TestSupport.AssertXY(612f, 792f, image.DrawOn(null));
    }

    // A text line in a text column is raised by its vertical offset once: the
    // column added it, and drawing the line added it again, so a superscript
    // raised 13 points rose 26 (F6).
    [Fact]
    public void ATextColumnRaisesASuperscriptOnce() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(font, "Base")).Add(new TextLine(font, "sup").SetVerticalOffset(-13f));
        TextColumn column = new TextColumn();
        column.SetLocation(50f, 100f);
        column.SetWidth(400f);
        column.AddParagraph(paragraph);
        column.DrawOn(page);
        string content = TestSupport.Content(page);
        TestSupport.AssertNear(13f, TestSupport.PositionOf(content, "sup")[1] - TestSupport.PositionOf(content, "Base")[1], 0.01f);
    }

    // A destination the document names keeps its name, and a bookmark whose
    // name it is, dest#1, is named again: a link to the document's dest#1 went
    // to the bookmark (F7).
    [Fact]
    public void ADestinationNameOfTheDocumentComesFirst() {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.AddDestination("dest#1", 500f);
        new TextLine(font, "go").SetGoToAction("dest#1").SetLocation(50f, 50f).DrawOn(page);
        Bookmark bookmark = new Bookmark(pdf).AddBookmark(page, new Title(font, "A", 10f, 100f));
        pdf.Complete();
        var ys = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                TestSupport.Latin1(output.ToArray()), @"/Dest \[\d+ 0 R /XYZ 0 ([\d.]+) 0\]")) {
            ys.Add(m.Groups[1].Value);
        }
        Assert.True(ys.Count == 2 && ys[0] == "292" && ys[1] != "292", string.Join(" ", ys));
        Assert.NotEqual("dest#1", bookmark.GetDestinationName());
    }

    // A bookmark on a page that is not added fails the document: it went to
    // /Dest [0 0 R], and readers refused the document (F8).
    [Fact]
    public void ABookmarkOnAPageNotAddedFailsTheDocument() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        new Page(pdf, Letter.PORTRAIT);
        Page detached = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new Bookmark(pdf).AddBookmark(detached, new Title(font, "Lost", 10f, 100f));
        Exception e = Assert.Throws<InvalidOperationException>(() => pdf.Complete());
        Assert.Equal("The bookmark Lost is on a page that is not added to the PDF.", e.Message);
    }

    // An empty dash pattern of an arc is refused, with a stroke color and
    // without, in every port: Go took it for none with a stroke color (S3, C7).
    [Fact]
    public void AnArcRefusesAnEmptyDashPattern() {
        foreach (bool colored in new bool[] {false, true}) {
            Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
            Arc arc = new Arc().SetRadius(50f).SetSweep(90f).SetStrokeDashPattern("");
            arc.SetLocation(100f, 100f);
            if (colored) {
                arc.SetStrokeColor(Color.red);
            }
            Exception e = Assert.Throws<ArgumentException>(() => arc.DrawOn(page));
            Assert.Equal("The dash pattern \"\" is not an array of non-negative numbers, not all zero, followed by a phase, such as \"[3 3] 0\".", e.Message);
        }
    }

    // A NaN or infinite angle fails the document, as a NaN coordinate does: it
    // wrote 0 0 0 0 0 0 cm, and the arc or the container vanished (F12).
    [Fact]
    public void ANaNRotationFailsTheDocument() {
        foreach (float degrees in new float[] {float.NaN, float.PositiveInfinity}) {
            Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
            Arc arc = new Arc().SetRadius(50f).SetSweep(90f).SetRotation(degrees);
            arc.SetLocation(100f, 100f);
            Assert.Equal("A coordinate, size or width is NaN, infinite or too large for a PDF.", Assert.Throws<ArgumentException>(() => arc.DrawOn(page)).Message);

            Page page2 = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
            Container container = new Container(100f, 100f).SetRotation(degrees);
            container.Add(new Arc().SetRadius(10f).SetSweep(90f));
            Assert.Equal("A coordinate, size or width is NaN, infinite or too large for a PDF.", Assert.Throws<ArgumentException>(() => container.DrawOn(page2)).Message);
        }
    }

    // A curve has two "c" control points, or one "v" or "y" one: a "c" curve
    // of one control point wrote "c" with four numbers, not six, which readers
    // refused (F11).
    [Fact]
    public void ACurveOfTheWrongControlPointsIsRefused() {
        Point start = new Point(10f, 10f);
        Point end = new Point(100f, 100f);
        var paths = new List<List<Point>> {
            new List<Point> {start, new Point(20f, 20f, Point.CONTROL_POINT_C), end},
            new List<Point> {start, new Point(20f, 20f, Point.CONTROL_POINT_V), new Point(30f, 30f, Point.CONTROL_POINT_V), end},
            new List<Point> {start, new Point(20f, 20f, Point.CONTROL_POINT_C), new Point(30f, 30f, Point.CONTROL_POINT_Y), end}};
        foreach (List<Point> path in paths) {
            Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
            Assert.Equal("A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.", Assert.Throws<ArgumentException>(() => page.DrawPath(path, PathOperator.STROKE)).Message);
            Stamp stamp = new Stamp(TestSupport.NewPDF()).SetSize(200f, 200f);
            Assert.Equal("A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.", Assert.Throws<ArgumentException>(() => stamp.DrawPath(path, PathOperator.STROKE)).Message);
        }
        new Page(TestSupport.NewPDF(), Letter.PORTRAIT).DrawPath(new List<Point> {start,
                new Point(20f, 20f, Point.CONTROL_POINT_C), new Point(30f, 30f, Point.CONTROL_POINT_C), end},
                PathOperator.STROKE);
    }

    // A point a path lists twice is scaled once: (10, 10) listed twice and
    // scaled by 2 became (40, 40), not (20, 20) (F9).
    [Fact]
    public void APointListedTwiceIsScaledOnce() {
        Point shared = new Point(10f, 10f);
        Point other = new Point(30f, 5f);
        new Path().Add(shared).Add(other).Add(shared).ScaleBy(2f);
        TestSupport.AssertNear(20f, shared.GetX(), 0.01f);
        TestSupport.AssertNear(20f, shared.GetY(), 0.01f);
        TestSupport.AssertNear(60f, other.GetX(), 0.01f);
    }

    private static string RoundedRect(float x, float y, float w, float h) {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        new Rect(x, y, w, h).SetCornerRadius(10f).SetFillColor(Color.red).DrawOn(page);
        return TestSupport.Content(page);
    }

    // A rounded rectangle of a negative width and height is the same
    // rectangle the other way round: its corners were turned inside out (F10).
    [Fact]
    public void ARoundedRectangleOfANegativeSizeIsTheSame() {
        Assert.Equal(RoundedRect(50f, 60f, 50f, 40f), RoundedRect(100f, 100f, -50f, -40f));
    }

    // A stamp is given its size before anything is drawn on it, as its y runs
    // down from its top: drawn before, the content was below the stamp, and
    // was not seen (F13).
    [Fact]
    public void AStampIsSizedBeforeItIsDrawnOn() {
        Stamp stamp = new Stamp(TestSupport.NewPDF());
        Assert.Equal("Set the size of the stamp before drawing on it: its y runs down from its top.", Assert.Throws<InvalidOperationException>(() => stamp.FillRect(0f, 0f, 20f, 10f)).Message);
        Stamp sized = new Stamp(TestSupport.NewPDF()).SetSize(20f, 10f);
        sized.FillRect(0f, 0f, 20f, 10f);
        Assert.Equal("Set the size of the stamp before drawing on it: its y runs down from its top.", Assert.Throws<InvalidOperationException>(() => sized.SetSize(40f, 20f)).Message);
        new Stamp(TestSupport.NewPDF()).SetSize(20f, 10f).FillRect(0f, 0f, 20f, 10f).SetSize(40f, 10f);
    }

    // A point that is a link, drawn on its own, makes its link, and a marker
    // of lines is stroked in the point's color: the link was not made, and a
    // plus was drawn in the pen's color (F15).
    [Fact]
    public void APointDrawnOnItsOwnHasItsLinkAndColor() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        new Point(100f, 100f).SetURIAction("https://pdfjet.com").DrawOn(page);
        Assert.Single(page.annots);
        Page page2 = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        new Point(100f, 100f).SetShape(Shape.PLUS).SetFillColor(Color.red).DrawOn(page2);
        Assert.Contains("1 0 0 RG\n", TestSupport.Content(page2));
    }

    // A paragraph a text column draws has its coordinates, as one a text
    // frame draws does: they stayed 0 (F17).
    [Fact]
    public void AParagraphOfATextColumnHasItsCoordinates() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Paragraph paragraph = new Paragraph().Add(new TextLine(font, "Hello, world"));
        TextColumn column = new TextColumn();
        column.SetLocation(50f, 100f);
        column.SetWidth(400f);
        column.AddParagraph(paragraph);
        column.DrawOn(new Page(pdf, Letter.PORTRAIT));
        TestSupport.AssertNear(50f, paragraph.GetX1(), 0.01f);
        TestSupport.AssertNear(100f, paragraph.GetY1(), 0.01f);
        TestSupport.AssertNear(100f + font.GetAscent(), paragraph.GetTextY(), 0.01f);
        Assert.True(paragraph.GetY2() > paragraph.GetTextY() && paragraph.GetX2() > 50f);
    }

    // Drawing on a page of a PDF that was read: the page's own content is
    // drawn in a q and a Q of its own; a page with no /Contents gets the
    // drawing, which was lost; and the drawing starts at the corner of the
    // /MediaBox (F2).
    [Fact]
    public void DrawingOnAPageOfAPDFThatWasRead() {
        List<PDFobj> objects = TestSupport.Read(PDFTest.PdfWithObjects(new string[] {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [100 100 712 892] /Contents 4 0 R >>",
                "<< /Length 18 >>\nstream\n0.5 0 0 0.5 0 0 cm\nendstream",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"}));
        PDF pdf = TestSupport.NewPDF();
        List<PDFobj> pages = pdf.GetPageObjects(objects);
        foreach (PDFobj pageObj in pages) {
            Page page = new Page(pdf, pageObj);
            page.FillRect(0f, 0f, 30f, 30f);
            page.Complete(objects);
        }
        List<string> wrapped = pages[0].dict;
        int i = wrapped.IndexOf("/Contents");
        Assert.Equal("[ 6 0 R 4 0 R 7 0 R ]", string.Join(" ", wrapped.GetRange(i + 1, 11)));
        Assert.Equal("q\n", TestSupport.Latin1(objects[5].stream));
        Assert.StartsWith("Q\nq\n1 0 0 1 100 100 cm\n", TestSupport.Latin1(objects[6].stream));
        List<string> added = pages[1].dict;
        int j = added.IndexOf("/Contents");
        Assert.Equal("[ 8 0 R ]", string.Join(" ", added.GetRange(j + 1, 5)));
    }

    // A document of PDF/A-1 of more than 8191 pages, the most entries of an
    // array of PDF/A-1, is refused, and a text of PDF/A of more than 16383
    // characters, a string longer than 32767 bytes: veraPDF failed both (W1).
    [Fact]
    public void TheSizeLimitsOfPDFAAreChecked() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_A_1B);
        for (int i = 0; i < 8192; i++) {
            new Page(pdf, Letter.PORTRAIT);
        }
        Assert.Equal("PDF/A-1 limits an array to 8191 entries, and the page tree has 8192: make the document PDF/A-2 or PDF/A-3.", Assert.Throws<InvalidOperationException>(() => pdf.Complete()).Message);

        string text = new string('a', 16383);
        PDF fits = new PDF(new MemoryStream(), Compliance.PDF_A_2B);
        new TextLine(TestSupport.PlexSans(fits), text).SetLocation(10f, 20f).DrawOn(new Page(fits, Letter.PORTRAIT));
        PDF tooLong = new PDF(new MemoryStream(), Compliance.PDF_A_2B);
        TextLine line = new TextLine(TestSupport.PlexSans(tooLong), text + "a");
        line.SetLocation(10f, 20f);
        Page page = new Page(tooLong, Letter.PORTRAIT);
        Assert.Equal("A text of more than 16383 characters is a string longer than PDF/A allows, 32767 bytes: draw it as several lines.", Assert.Throws<ArgumentException>(() => line.DrawOn(page)).Message);
    }

    // An element is tagged with a language of its own only when it has one,
    // as in the other ports: every table, row, cell and list element was
    // tagged en-US, in a German document too (K4).
    [Fact]
    public void AnElementIsTaggedWithTheLanguageOfTheDocument() {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output, Compliance.PDF_UA_1);
        pdf.SetTitle("Tabelle");
        pdf.SetLanguage("de-DE");
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Table table = new Table();
        table.SetTableData(new List<List<Cell>> {
            new List<Cell> {new Cell(font, "Name"), new Cell(font, "Stadt")},
            new List<Cell> {new Cell(font, "Anna"), new Cell(font, "Berlin")}}, 1);
        table.SetLocation(50f, 50f);
        table.DrawOn(page);
        pdf.Complete();
        Assert.DoesNotContain("/Lang <" + Util.ToHexString(System.Text.Encoding.UTF8.GetBytes("en-US")) + ">",
                TestSupport.Latin1(output.ToArray()));
    }

    // A /MediaBox of a NaN or infinite size is no size, and the page is
    // Letter, as of one of 0: Swift stopped where such a page was watermarked
    // (K1); net8 and net48 read the huge numbers alike (K5).
    [Fact]
    public void AMediaBoxOfANaNSizeIsLetter() {
        string huge = new string('9', 40);
        foreach (string[] box in new string[][] {
                new string[] {"0", "0", "NaN", "792"}, new string[] {"0", "0", "Infinity", "792"},
                new string[] {huge, "0", "-" + huge, "792"}}) {
            PDFobj obj = new PDFobj();
            obj.dict = new List<string> {"<<", "/Type", "/Page", "/MediaBox", "[", box[0], box[1], box[2], box[3], "]", ">>"};
            PageSize size = obj.GetPageSize();
            Assert.Equal(612f, size.GetWidth());
            Assert.Equal(792f, size.GetHeight());
        }
    }

    // A cell's vertical alignment that is not top, center or bottom gives the
    // same message in the four ports: Java and C# threw a bare "Invalid
    // vertical text alignment option."
    [Fact]
    public void ACellsVerticalAlignmentOfLeftHasTheMessageOfTheFourPorts() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Table table = new Table();
        table.SetTableData(new List<List<Cell>> {new List<Cell> {new Cell(font, "text").SetVerticalAlignment(Alignment.LEFT)}}, 0);
        table.SetLocation(50f, 50f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Assert.Equal("The vertical alignment of a cell is top, center or bottom.", Assert.Throws<ArgumentException>(() => table.DrawOn(page)).Message);
    }

    // A word of a text column too wide for its line breaks at a soft hyphen,
    // the line ending with a hyphen, as TextBlock and TextFrame break it:
    // TextColumn never broke at one.
    [Fact]
    public void ATextColumnBreaksAWordAtASoftHyphen() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        TextColumn column = new TextColumn();
        column.SetLocation(50f, 100f);
        column.SetWidth(TextBlockTest.SoftHyphenWidth(font));
        column.AddParagraph(new Paragraph().Add(new TextLine(font, "Ab Donau\u00addampf\u00adschiff\u00adfahrt")));
        Page page = new Page(pdf, Letter.PORTRAIT);
        column.DrawOn(page);
        // Ab Donau- / dampfschiff- / fahrt
        string content = TestSupport.Content(page);
        float[] donau = TestSupport.PositionOf(content, "Donau-");
        float[] dampf = TestSupport.PositionOf(content, "dampfschiff-");
        float[] fahrt = TestSupport.PositionOf(content, "fahrt");
        Assert.True(dampf[1] < donau[1] && fahrt[1] < dampf[1], content);
    }

    // The number that tells a font program apart, and that the tag of its
    // subset is made of, folds the blocks of 256 characters the font has
    // glyphs in, and leaves out the rest: it folded all 65,536 characters,
    // 236 empty blocks of IBM Plex Sans's 256. The number is the same in the
    // four ports.
    [Fact]
    public void AFontChecksumLeavesOutEmptyBlocks() {
        Font font = new Font(TestSupport.NewPDF(), TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Assert.Equal(0xABF1C2C88C082202UL, font.checksum);
    }

    // A word too wide for the column whose first piece does not fit the rest
    // of its line breaks at its soft hyphens on the next lines: it was put on
    // the next line whole, past the column's edge.
    [Fact]
    public void ATextColumnBreaksAWordThatStartsANewLine() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        font.SetSize(10f);
        TextColumn column = new TextColumn();
        column.SetLocation(50f, 100f);
        column.SetWidth(100f);
        column.AddParagraph(new Paragraph().Add(new TextLine(font, "aaaaaaaaaaaaaaaaa bbbbbbbbbb\u00ADbbbbbbbbbb\u00ADbbbbbbbbbb\u00ADbbbbbbbbbb")));
        Page page = new Page(pdf, Letter.PORTRAIT);
        column.DrawOn(page);
        // aaaaaaaaaaaaaaaaa / bbbbbbbbbb- / bbbbbbbbbb- / bbbbbbbbbb- / bbbbbbbbbb
        string content = TestSupport.Content(page);
        float[] a = TestSupport.PositionOf(content, "aaaaaaaaaaaaaaaaa");
        float[] b = TestSupport.PositionOf(content, "bbbbbbbbbb-");
        Assert.True(b[1] < a[1], content);
    }
}
}
