/*
 * PDFTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
}
}
