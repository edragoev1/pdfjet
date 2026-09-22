/*
 * PDFTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
/// <summary>Writing a document and reading it back.</summary>
public class PDFTest {
    private static byte[] Document(params PageSize[] sizes) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        foreach (PageSize size in sizes) {
            Page page = new Page(pdf, size);
            new TextLine(font, "Page").SetLocation(50f, 50f).DrawOn(page);
        }
        pdf.Complete();
        return stream.ToArray();
    }

    /// <summary>A stream that remembers that it was closed.</summary>
    private sealed class ClosingStream : MemoryStream {
        public bool Closed;

        protected override void Dispose(bool disposing) {
            Closed = true;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void StartsWithTheHeaderAndEndsWithEof() {
        string raw = TestSupport.Latin1(Document(Letter.PORTRAIT));
        Assert.StartsWith("%PDF-1.7\n%", raw);
        Assert.EndsWith("%%EOF\n", raw);
    }

    [Fact]
    public void TheCrossReferenceTablePointsAtEveryObject() {
        string raw = TestSupport.Latin1(Document(Letter.PORTRAIT, A4.PORTRAIT));
        Match header = Regex.Match(raw, "xref\n0 (\\d+)\n");
        Assert.True(header.Success);
        int count = int.Parse(header.Groups[1].Value);
        int entries = header.Index + header.Length;
        for (int number = 1; number < count; number++) {
            string entry = raw.Substring(entries + 20 * number, 20);
            if (entry[17] == 'n') {
                int offset = int.Parse(entry.Substring(0, 10));
                Assert.True(string.CompareOrdinal(raw, offset, number + " 0 obj", 0, (number + " 0 obj").Length) == 0,
                        "object " + number + " at " + offset);
            }
        }
        Match startxref = Regex.Match(raw, "startxref\n(\\d+)\n%%EOF\n$");
        Assert.True(startxref.Success);
        Assert.Equal("xref\n", raw.Substring(int.Parse(startxref.Groups[1].Value), 5));
    }

    [Fact]
    public void DocumentIdsAreRandomAndDifferent() {
        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < 100; i++) {
            MemoryStream stream = new MemoryStream();
            PDF pdf = new PDF(stream);
            new Page(pdf, A4.PORTRAIT);
            pdf.Complete();
            string id = TestSupport.TrailerID(stream.ToArray());
            Assert.Matches("^[0-9a-f]{32}$", id);
            ids.Add(id);
        }
        Assert.Equal(100, ids.Count);
    }

    [Fact]
    public void TheXmpDocumentIdIsTheTrailerId() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        string id = TestSupport.TrailerID(stream.ToArray());
        Assert.Contains("<xapMM:DocumentID>uuid:" + id + "</xapMM:DocumentID>", TestSupport.Latin1(stream.ToArray()));
    }

    [Fact]
    public void TheInfoDictionaryHasTheTitleInUtf16() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.SetTitle("Grüße (x)").SetAuthor("Author");
        new TextLine(TestSupport.Helvetica(pdf), "x").SetLocation(10f, 10f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        PDFobj info = TestSupport.FindObject(TestSupport.Read(stream.ToArray()), "/Producer");
        Assert.NotNull(info);
        Assert.Equal("Grüße (x)", TestSupport.Utf16Hex(info.GetValue("/Title")));
        Assert.Equal("Author", TestSupport.Utf16Hex(info.GetValue("/Author")));
    }

    [Fact]
    public void PageSizesSurviveReadingBack() {
        List<PDFobj> objects = TestSupport.Read(Document(Letter.PORTRAIT, A4.LANDSCAPE, Letter.PORTRAIT));
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        Assert.Equal(3, pages.Count);
        Assert.Equal(612f, pages[0].GetPageSize().GetWidth());
        Assert.Equal(842f, pages[1].GetPageSize().GetWidth());
        Assert.Equal(595f, pages[1].GetPageSize().GetHeight());
        Assert.Equal(792f, pages[2].GetPageSize().GetHeight());
    }

    [Fact]
    public void ReadsAPdfWhoseCrossReferenceOffsetIsWrong() {
        string raw = TestSupport.Latin1(Document(Letter.PORTRAIT, Letter.PORTRAIT));
        string damaged = new Regex("startxref\n\\d+\n").Replace(raw, "startxref\n12\n", 1);
        List<PDFobj> objects = TestSupport.Read(Encoding.Latin1.GetBytes(damaged));
        Assert.Equal(2, new PDF().GetPageObjects(objects).Count);
    }

    [Fact]
    public void CrossReferenceOffsetsAreTenDigits() {
        Assert.Equal("0000000017", PDF.XrefOffset(17));
        Assert.Equal("9999999999", PDF.XrefOffset(9999999999L));
        IOException e = Assert.Throws<IOException>(() => PDF.XrefOffset(10000000000L));
        Assert.Equal("The PDF is too large for a cross-reference table: an object starts at byte 10000000000.",
                e.Message);
    }

    [Fact]
    public void CompleteClosesTheStream() {
        ClosingStream stream = new ClosingStream();
        PDF pdf = new PDF(stream);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        Assert.True(stream.Closed);
    }

    [Fact]
    public void ReadsAPdfWithABlankPage() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        Assert.Single(new PDF().GetPageObjects(TestSupport.Read(stream.ToArray())));
    }

    // A PDF with one stream whose data starts with a line feed, the byte that
    // ends the stream keyword. Every stream of an encrypted PDF starts with a
    // random IV, so one in 256 of them starts that way.
    private static byte[] PdfWithStream(string data) {
        string o1 = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
        string o2 = "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n";
        string o3 = "3 0 obj\n<< /Length " + data.Length + " >>\nstream\n" + data + "\nendstream\nendobj\n";
        string header = "%PDF-1.4\n";
        int off1 = header.Length;
        int off2 = off1 + o1.Length;
        int off3 = off2 + o2.Length;
        int xref = off3 + o3.Length;
        string body = header + o1 + o2 + o3
                + "xref\n0 4\n0000000000 65535 f \n"
                + off1.ToString("D10") + " 00000 n \n" + off2.ToString("D10") + " 00000 n \n" + off3.ToString("D10") + " 00000 n \n"
                + "trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n";
        return Encoding.Latin1.GetBytes(body);
    }

    [Fact]
    public void AStreamThatStartsWithALineFeedKeepsIt() {
        List<PDFobj> objects = TestSupport.Read(PdfWithStream("\nHELLO"));
        foreach (PDFobj obj in objects) {
            if (obj.GetNumber() == 3) {
                Assert.Equal("\nHELLO", Encoding.Latin1.GetString(obj.GetData()));
                return;
            }
        }
        Assert.Fail("object 3 not read");
    }

    [Fact]
    public void AnEmptyDocumentPropertyIsNotWritten() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.SetTitle("").SetAuthor("").SetSubject("").SetKeywords("").SetCreator("");
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        foreach (string key in new string[] {"/Title", "/Author", "/Subject", "/Keywords", "/Creator"}) {
            Assert.DoesNotContain(key, raw);
        }
    }

    [Fact]
    public void AShapeWithoutADescriptionWritesNoAltText() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(10f, 10f, 100f, 10f).DrawOn(page);
        new Line(10f, 20f, 100f, 20f).SetAltDescription("A rule").DrawOn(page);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, raw.Split("/Alt <").Length - 1);
        Assert.DoesNotContain("/ActualText", raw);
    }

    [Fact]
    public void PointsAndTwoDimensionalBarcodesAreArtifacts() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        IDrawable[] drawables = {
                new Point(50f, 50f),
                new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M),
                new DataMatrix("PDFjet")};
        foreach (IDrawable drawable in drawables) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            drawable.DrawOn(page);
            string content = TestSupport.Content(page);
            Assert.StartsWith("/Artifact BMC\n", content);
            Assert.EndsWith("EMC\n", content);
        }
    }

    [Fact]
    public void ALinkIsInALinkElementAndAnyOtherAnnotationInAnAnnotElement() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "PDFjet").SetURIAction("https://pdfjet.com").SetLocation(70f, 80f).DrawOn(page);
        TextAnnotation note = new TextAnnotation();
        note.SetLocation(70f, 100f);
        note.SetContents("A note");
        note.DrawOn(page);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, raw.Split("/S /Link\n").Length - 1);
        Assert.Equal(1, raw.Split("/S /Annot\n").Length - 1);
    }

    // The numbers of the page objects, in page order.
    private static string[] PageNumbers(byte[] pdf) {
        List<PDFobj> pages = new PDF().GetPageObjects(TestSupport.Read(pdf));
        string[] numbers = new string[pages.Count];
        for (int i = 0; i < numbers.Length; i++) {
            numbers[i] = pages[i].GetNumber().ToString();
        }
        return numbers;
    }

    [Fact]
    public void ADetachedPageThatIsNeverAddedLeavesNoTrace() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        // A dry run, like one that measures the text, on a page that is never added.
        Page dry = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new TextLine(font, "PDFjet").SetURIAction("https://pdfjet.com").SetLocation(70f, 80f).DrawOn(dry);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go to page 2").SetGoToAction("dest2").SetLocation(70f, 80f).DrawOn(page1);
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        page2.AddDestination("dest2", 100f);
        pdf.Complete();

        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.DoesNotContain("/Pg 0 0 R", raw);
        Assert.Equal(1, raw.Split("/Type /Annot\n").Length - 1);
        // The link leads to the second page, not to the object before it.
        Match dest = Regex.Match(raw, @"/Dest \[(\d+) 0 R");
        Assert.True(dest.Success);
        Assert.Equal(PageNumbers(stream.ToArray())[1], dest.Groups[1].Value);
    }

    [Fact]
    public void TheStructureTreeFollowsThePagesNotTheOrderTheyWereDrawnIn() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page second = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new TextLine(font, "Second").SetLocation(70f, 80f).DrawOn(second);
        Page first = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new TextLine(font, "First").SetLocation(70f, 80f).DrawOn(first);
        pdf.AddPage(first);
        pdf.AddPage(second);
        pdf.Complete();

        // The structure elements are written, and listed by the document
        // element, in the order of their pages.
        string[] pages = PageNumbers(stream.ToArray());
        MatchCollection pg = Regex.Matches(TestSupport.Latin1(stream.ToArray()), @"/Pg (\d+) 0 R");
        Assert.True(pg.Count >= 2);
        Assert.Equal(pages[0], pg[0].Groups[1].Value);
        Assert.Equal(pages[1], pg[1].Groups[1].Value);
    }

    [Fact]
    public void TextStringsAreUtf16SoThatEveryReaderDecodesThem() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(10f, 20f, 100f, 20f).SetAltDescription("Gr\u00fc\u00dfe \u2013 \u7dda").DrawOn(page);
        pdf.Complete();
        PDFobj element = TestSupport.FindObject(TestSupport.Read(stream.ToArray()), "/Alt");
        Assert.NotNull(element);
        Assert.StartsWith("<feff", element.GetValue("/Alt").ToLowerInvariant());
        Assert.Equal("Gr\u00fc\u00dfe \u2013 \u7dda", TestSupport.Utf16Hex(element.GetValue("/Alt")));
    }

    // A PDF whose font has its widths and its encoding in objects of their own,
    // as the PDFs that Word makes do.
    private static byte[] PdfWithIndirectWidths() {
        return PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792]"
                    + " /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            "<< /Length 32 >>\nstream\nBT /F1 24 Tf 72 700 Td (H) Tj ET\nendstream",
            "<< /Type /Font /Subtype /TrueType /BaseFont /Helvetica /FirstChar 72 /LastChar 72"
                    + " /Widths 6 0 R /Encoding 7 0 R >>",
            "[ 722 ]",
            "<< /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [ 72 /H ] >>",
        });
    }

    private static byte[] PdfWithObjects(string[] objects) {
        StringBuilder sb = new StringBuilder("%PDF-1.4\n");
        int[] offsets = new int[objects.Length];
        for (int i = 0; i < objects.Length; i++) {
            offsets[i] = sb.Length;
            sb.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }
        int xref = sb.Length;
        sb.Append("xref\n0 ").Append(objects.Length + 1).Append("\n0000000000 65535 f \n");
        foreach (int offset in offsets) {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append("trailer\n<< /Size ").Append(objects.Length + 1)
                .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    [Fact]
    public void AFontIsImportedWithTheObjectsItRefersTo() {
        List<PDFobj> source = TestSupport.Read(PdfWithIndirectWidths());
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.AddResourceObjects(source);
        Page page = new Page(pdf, Letter.PORTRAIT);
        PDFobj content = pdf.GetPageObjects(source)[0].GetContentObject(source);
        page.DrawContents(content.GetData(), 792f, 0f, 0f, 1f, 1f);
        pdf.Complete();

        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        Assert.Equal("/Font", objects[4].GetValue("/Type"));
        Assert.Contains("722", objects[5].GetDict());
        Assert.Equal("/Encoding", objects[6].GetValue("/Type"));
    }

    [Fact]
    public void APageTreeThatLoopsIsReadOnce() {
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R 99 0 R] /Count 1 >>",
            "<< /Type /Pages /Parent 2 0 R /Kids [3 0 R 2 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        }));
        Assert.Single(new PDF().GetPageObjects(objects));
        PDF pdf = new PDF(new MemoryStream());
        pdf.Merge(objects);
        pdf.Complete();
    }

    [Fact]
    public void APageTreeNodeWithManyKidsIsReadOnce() {
        // A node of the page tree with thousands of kids, from which every
        // page inherits its /MediaBox, as veraPDF tests it with 10,000 pages
        // in isartor-6-1-12-t01-fail-a: the node was read again for each
        // entry of each page, which took time that grew as the square of the
        // number of pages, and more than a minute for that file.
        const int count = 10000;
        StringBuilder kids = new StringBuilder();
        string[] objects = new string[count + 2];
        objects[0] = "<< /Type /Catalog /Pages 2 0 R >>";
        for (int i = 0; i < count; i++) {
            kids.Append(i + 3).Append(" 0 R ");
            objects[i + 2] = "<< /Type /Page /Parent 2 0 R >>";
        }
        objects[1] = "<< /Type /Pages /MediaBox [0 0 595 842] /Kids [" + kids + "] /Count " + count + " >>";
        List<PDFobj> read = TestSupport.Read(PdfWithObjects(objects));
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        List<PDFobj> pages = new PDF().GetPageObjects(read);
        PDF pdf = new PDF(new MemoryStream());
        pdf.Merge(read);
        pdf.Complete();
        Assert.Equal(count, pages.Count);
        Assert.Equal(842f, pages[count - 1].GetPageSize().GetHeight());
        Assert.True(watch.ElapsedMilliseconds < 5000, "It took " + watch.ElapsedMilliseconds + " ms.");
    }

    [Fact]
    public void ObjectsWithoutAPageTreeHaveNoPages() {
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {"<< /Type /Catalog >>"}));
        Assert.Empty(new PDF().GetPageObjects(objects));
    }

    [Fact]
    public void TheNameOfAnEmbeddedFileIsATextStringInFAndUF() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        EmbeddedFile file = new EmbeddedFile(pdf, "\u00dcbersicht \u2013 r\u00e9sum\u00e9.txt",
                new MemoryStream(Encoding.ASCII.GetBytes("Hello")), false);
        new FileAttachment(file).SetLocation(100f, 100f).DrawOn(page);
        pdf.Complete();
        PDFobj spec = TestSupport.FindObject(TestSupport.Read(stream.ToArray()), "/UF");
        Assert.NotNull(spec);
        Assert.Equal("/Filespec", spec.GetValue("/Type"));
        Assert.Equal("\u00dcbersicht \u2013 r\u00e9sum\u00e9.txt", TestSupport.Utf16Hex(spec.GetValue("/UF")));
        Assert.Equal(spec.GetValue("/UF"), spec.GetValue("/F"));
    }
    // The PDFs that are not valid, as the Go fuzz target of the reader found
    // them: each fails with a message or reads what it can, and none reads
    // past the file, allocates what the file does not have or traps in Swift.

    // The message that reading the PDF fails with, or "(no error)" when it
    // is read.
    private static string ReadError(string raw) {
        try {
            TestSupport.Read(Encoding.Latin1.GetBytes(raw));
            return "(no error)";
        } catch (Exception e) {
            return e.Message;
        }
    }

    [Fact]
    public void AnObjectNumberedHigherThanTheFileHasBytesIsRefused() {
        // One object of every number up to the one it says would take
        // gigabytes of memory for a file of 31 bytes.
        Assert.Equal("The PDF of 31 bytes cannot hold an object numbered 44444441.",
                ReadError("44444441 0 obj/Filter/Fl streil"));
    }

    [Fact]
    public void AStreamLongerThanTheFileIsRefused() {
        // The bytes of the stream are counted before it is made, so that a
        // file of a few bytes that says its stream is a gigabyte takes no
        // memory. A stream that has its endstream ends there, whatever its
        // /Length says.
        Assert.Equal("The stream of an object is not in the PDF.",
                ReadError("1 0 obj<</Length 1000000000>>stream\nx"));
        Assert.Equal("(no error)",
                ReadError("1 0 obj<</Length 1000000000>>stream\nx\nendstream endobj"));
    }

    [Fact]
    public void AStreamWhoseLengthIsWrongEndsAtItsEndstream() {
        // A /Length that is too short, too long or missing, as pdf.js tests it
        // in issue6108, issue6069 and issue1293r: the stream ends at the end of
        // line before endstream, as MuPDF and pdf.js read it, and not at the
        // /Length, which cut the page's content when it was merged. The /Length
        // is set to it, so that the stream is written whole.
        foreach (string dict in new string[] {"<< /Length 3 >>", "<< /Length 300 >>", "<< >>", "<< /Length 16 >>"}) {
            List<PDFobj> read = TestSupport.Read(PdfWithObjects(new string[] {
                dict + "\nstream\nBT (Hello) Tj ET\nendstream",
            }));
            Assert.Equal("BT (Hello) Tj ET", TestSupport.Latin1(read[0].GetData()));
            Assert.Equal("16", read[0].GetValue("/Length"));
        }
        // A /Length that is right is kept, though the stream holds "endstream".
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Length 11 >>\nstream\nendstream x\nendstream",
        }));
        Assert.Equal("endstream x", TestSupport.Latin1(objects[0].GetData()));
    }

    [Fact]
    public void AStreamThatCannotBeDecodedHasNoData() {
        // A Flate stream cut short or with a wrong checksum, as pdf.js tests them
        // in comments.pdf and bug1050040: the rest of the PDF is read, and a
        // merge copies the stream as it is. It made the whole PDF unreadable.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Length 5 /Filter /FlateDecode >>\nstream\nabcde\nendstream",
        }));
        Assert.Null(objects[0].GetData());
        Assert.Equal("abcde", TestSupport.Latin1(objects[0].stream));
        // The objects of an object stream that cannot be decoded cannot be read.
        Assert.Equal("The archive entry was compressed using an unsupported compression method.",
                ReadError("1 0 obj<</Type/ObjStm/N 1/First 4/Length 5/Filter/FlateDecode>>stream\nabcde\nendstream endobj"));
    }

    [Fact]
    public void AnObjectNumberedHigherThanTheFileHasBytesIsRead() {
        // A PDF cut from a larger document can keep its object numbers, as pdf.js
        // tests it in issue16091: 41 objects numbered up to 156341 in a file of
        // 107,355 bytes, which MuPDF reads.
        List<PDFobj> objects = TestSupport.Read(
                Encoding.Latin1.GetBytes("156337 0 obj<</Type/Catalog>>endobj\n"));
        Assert.Equal(156337, objects.Count);
        Assert.Equal("/Catalog", objects[156336].GetValue("/Type"));
    }

    [Fact]
    public void AnObjectStreamThatIsNotANumberIsRefused() {
        Assert.Equal("The object stream of the PDF is malformed: \"x\" is not a number.",
                ReadError("1 0 obj<</Type/ObjStm/First x>>stream\n\nendstream endobj"));
        // An object stream with no stream of its own has no objects.
        Assert.Equal("(no error)", ReadError("1 0 obj 1 0 obj/Type/ObjStm/First 0"));
    }

    [Fact]
    public void AReferenceToAnObjectThatIsNotThereHasNoContents() {
        // A page whose /Contents names an object the PDF does not have, and
        // one whose dictionary ends where a value belongs.
        List<PDFobj> objects = TestSupport.Read(Encoding.Latin1.GetBytes(
                "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n"
                + "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n"
                + "3 0 obj<</Type/Page/Parent 2 0 R/Contents 99 0 R>>endobj\n"));
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        Assert.Single(pages);
        Assert.Null(pages[0].GetContentObject(objects));
        Assert.Null(pages[0].GetResourcesObject(objects));
        Assert.Equal("", pages[0].GetValue("/Contents2"));
    }

    [Fact]
    public void ALengthThatIsNotANumberIsAnError() {
        // The /Length is read for every stream, so a PDF that writes anything
        // there, or that ends before it, has to fail with a message: it read
        // past the tokens in the Java, C# and Go ports.
        Assert.Equal("The /Length of a stream is not a number.",
                ReadError("1 0 obj<</Length stream\nx\nendstream endobj"));
        Assert.Equal("The stream of an object is not in the PDF.",
                ReadError("1 0 obj<</Length 1 stream"));
        // A /Length that names an object with no length of its own.
        Assert.Equal("The /Length of a stream is not a number.",
                ReadError("1 0 obj<</Length 2 0 R>>stream\nx\nendstream endobj\n2 0 obj endobj"));
    }

    [Fact]
    public void AMediaBoxThatIsNotFourNumbersIsLetterSize() {
        // The size of a page is read from its /MediaBox, which a PDF that was
        // read can write as anything: it was four tokens past the key, which
        // trapped in Swift and read past the tokens in the other ports.
        foreach (string box in new string[] {"[0 0 612", "[a b c d]", "5 0 R", "[]", "", "[0 0 0 0]", "[0 0 612 0]"}) {
            List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox " + box + " >>",
            }));
            PageSize size = new PDF().GetPageObjects(objects)[0].GetPageSize();
            Assert.Equal(612f, size.GetWidth());
            Assert.Equal(792f, size.GetHeight());
        }
    }

    [Fact]
    public void AMediaBoxThatIsAnObjectOrRefersToItsNumbersIsRead() {
        // A box that is an object of its own, inherited here, and one whose
        // numbers are, as pdf.js tests them in bug852992_reduced and issue7872:
        // the size of both pages was letter size.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 /MediaBox 5 0 R >>",
            "<< /Type /Page /Parent 2 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 6 0 R 7 0 R] >>",
            "[0 0 540 190]",
            "250",
            "50",
        }));
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        float[][] sizes = {new float[] {540f, 190f}, new float[] {250f, 50f}};
        for (int i = 0; i < sizes.Length; i++) {
            PageSize size = pages[i].GetPageSize();
            Assert.Equal(sizes[i][0], size.GetWidth());
            Assert.Equal(sizes[i][1], size.GetHeight());
        }
        Assert.Equal("[ 0 0 250 50 ]", pages[1].GetValue("/MediaBox"));

        // A box that refers to the page itself is left as it is, as the fuzz
        // target found it: the page, listed three times, grew each time it was
        // read, to 600 MB.
        objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 3 0 R 3 0 R] /Count 3 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [3 0 R 0 612 792] >>",
        }));
        pages = new PDF().GetPageObjects(objects);
        Assert.Equal("[ 3 0 R 0 612 792 ]", pages[2].GetValue("/MediaBox"));
    }

    [Fact]
    public void ThePageSizeIsTheDistanceBetweenTheCornersOfTheMediaBox() {
        // The box is a rectangle of two opposite corners, in either order, and
        // its origin is not always 0 0: the size is what lies between them.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [9 9 621 801] >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [612 792 0 0] >>",
        }));
        foreach (PDFobj page in new PDF().GetPageObjects(objects)) {
            Assert.Equal(612f, page.GetPageSize().GetWidth());
            Assert.Equal(792f, page.GetPageSize().GetHeight());
        }
    }

    // The pages of a PDF that a stamp is drawn on, each with a dictionary that
    // ends where a value belongs or that names an object the file does not have.
    private static readonly string[] BrokenPages = {
        "<< /Type /Page /Parent 2 0 R /Resources",
        "<< /Type /Page /Parent 2 0 R /Resources 99 0 R /Contents 99 0 R >>",
        "<< /Type /Page /Parent 2 0 R /Resources << /Font 99 0 R >> /Contents",
        "<< /Type /Page /Parent 2 0 R /Resources << /XObject 99 0 R >> /Contents 4 0 R >>",
        "<< /Type /Page /Parent 2 0 R /Resources << >> /Contents 4 0 R /MediaBox [0 0 612",
        "<< /Type /Page /Parent 2 0 R /Contents [ 4 0 R",
        "<< /Type /Page /Parent 2 0 R /Contents 4",
        "<< /Type /Page /Parent 2 0 R /Resources << /ExtGState 99 0 R >> /Contents 4 0 R >>",
    };

    [Fact]
    public void APageHoldsTheEntriesItInheritsFromThePageTree() {
        // /Resources, /MediaBox, /CropBox and /Rotate can be written once on
        // a node above the pages, and a page of another program's PDF often
        // carries none of them: such a page read as letter size whatever its
        // size was, and had no resources, so its fonts and images were not
        // copied. The page that has one of its own keeps it.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 /MediaBox [0 0 595 842]"
                    + " /Resources << /Font << /F1 5 0 R >> >> /Rotate 90 >>",
            "<< /Type /Page /Parent 2 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        }));
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        Assert.Equal(595f, pages[0].GetPageSize().GetWidth());
        Assert.Equal(842f, pages[0].GetPageSize().GetHeight());
        Assert.NotNull(pages[0].GetResourcesObject(objects));
        Assert.Equal("90", pages[0].GetValue("/Rotate"));
        Assert.Equal(612f, pages[1].GetPageSize().GetWidth());
        // The entries are added once, however often the pages are returned.
        int size = pages[0].GetDict().Count;
        Assert.Equal(size, new PDF().GetPageObjects(objects)[0].GetDict().Count);
    }

    [Fact]
    public void AResourcesObjectThatNamesItsOwnPageIsAddedToOnce() {
        // The font was added to the page for every "/Resources" left in its
        // dictionary, and adding it grew that dictionary, so a resources
        // object whose /Font names the page itself never ended.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /Resources 4 0 R /Contents 5 0 R >>",
            "<< /Font 3 0 R >>",
            "<< /Length 5 >>\nstream\nHELLO\nendstream",
        }));
        PDFobj page = new PDF().GetPageObjects(objects)[0];
        page.AddResource(CoreFont.HELVETICA, objects);
        Assert.True(page.GetDict().Count <= 32, String.Join(" ", page.GetDict()));
    }

    [Fact]
    public void AStampOnAPageWhoseDictionaryIsBrokenDrawsNothing() {
        // Every one of these crashed a port: the methods that add a font, an
        // image, a content stream or a graphics state to a page that was read
        // indexed its dictionary and the objects of the PDF unchecked.
        foreach (string page in BrokenPages) {
            List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                page,
                "<< /Length 5 >>\nstream\nHELLO\nendstream",
            }));
            List<PDFobj> pages = new PDF().GetPageObjects(objects);
            Assert.Single(pages);
            PDFobj obj = pages[0];
            obj.AddResource(CoreFont.HELVETICA, objects);
            obj.AddContent(Encoding.ASCII.GetBytes("BT ET\n"), objects);
            obj.AddPrefixContent(Encoding.ASCII.GetBytes("q Q\n"), objects);
            obj.SetGraphicsState(new GraphicsState().SetAlphaStroking(0.5f), objects);
            // The objects that were read are written as they are.
            PDF stamped = new PDF(new MemoryStream());
            stamped.AddObjects(objects);
            stamped.Complete();
            // The fonts and the images of the pages are copied into a new PDF.
            PDF imported = new PDF(new MemoryStream());
            imported.AddResourceObjects(objects);
            new Page(imported, Letter.PORTRAIT);
            imported.Complete();
        }
    }

    [Fact]
    public void ADictionaryThatEndsInTheMiddleOfAValueIsClosedThere() {
        List<PDFobj> objects = TestSupport.Read(
                Encoding.Latin1.GetBytes("1 0 obj<</Type/Catalog/Kids[3 0 R\n"));
        Assert.Single(objects);
        Assert.Equal("[ 3 0 R ]", objects[0].GetValue("/Kids"));
        Assert.Equal("", objects[0].GetValue("/Nothing"));
    }

    [Fact]
    public void ThePagesAreTheTreeThatTheCatalogOfTheTrailerNames() {
        // A second page tree with no /Parent, before the one that the catalog
        // names, as pdf.js tests it in issue19281 (a tree of one page left
        // from an earlier version) and in xfa_issue13556 (the pages of an XFA
        // form behind a tree of one page): MuPDF and pdf.js find the pages
        // through the trailer's /Root, and the first tree found was taken.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
            "<< /Type /Pages /Kids [5 0 R 6 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 4 0 R /MediaBox [0 0 200 200] >>",
            "<< /Type /Page /Parent 4 0 R /MediaBox [0 0 300 300] >>",
        }));
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        Assert.Equal(2, pages.Count);
        Assert.Equal(5, pages[0].GetNumber());
        Assert.Equal(6, pages[1].GetNumber());
        // A catalog that the trailer does not name, like that of an older
        // version of the document, is not the one whose pages are read.
        string raw = Encoding.Latin1.GetString(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R >>",
            "<< /Type /Pages /Kids [5 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 4 0 R >>",
            "<< /Type /Catalog /Pages 4 0 R >>",
        })).Replace("/Root 1 0 R", "/Root 6 0 R");
        pages = new PDF().GetPageObjects(TestSupport.Read(Encoding.Latin1.GetBytes(raw)));
        Assert.Single(pages);
        Assert.Equal(5, pages[0].GetNumber());
        // Objects with no trailer find the tree that has no /Parent, as before.
        objects = TestSupport.Read(Encoding.Latin1.GetBytes(
                "1 0 obj<</Type/Pages/Kids[2 0 R]/Count 1>>endobj\n"
                + "2 0 obj<</Type/Page/Parent 1 0 R>>endobj\n"));
        Assert.Single(new PDF().GetPageObjects(objects));
    }

    [Fact]
    public void AnEntryForObjectZeroThatIsInUseIsSkipped() {
        // Object 0 heads the list of free objects, and a table that marks it
        // in use, as pdf.js tests it in issue10004, is read as MuPDF reads it:
        // the whole table was refused, and the objects were looked for by
        // scanning.
        string raw = Encoding.Latin1.GetString(PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R >>",
        })).Replace("0000000000 65535 f ", "0000000009 00000 n ");
        // The objects follow each other with no white space between them, as
        // there, so that scanning for them does not find them either.
        raw = raw.Replace("\nendobj\n", "\n endobj");
        List<PDFobj> objects = TestSupport.Read(Encoding.Latin1.GetBytes(raw));
        Assert.Single(new PDF().GetPageObjects(objects));
    }

    [Fact]
    public void TheLengthOfAStreamIsTheEntryOfItsDictionary() {
        // A /Length that is the value of another entry, "/Height/Length", as
        // pdf.js tests it in issue19611, is not the /Length of the stream.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(new string[] {
            "<< /Type /XObject /Height /Length /Length 5 >>\nstream\nabcde\nendstream",
        }));
        Assert.Equal("abcde", TestSupport.Latin1(objects[0].GetData()));
    }

}
}
