/*
 * ReviewReaderTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// Reading PDFs that are made to be slow to read, or to break what is made of
/// them. The time limits are generous: each of these took from five seconds to
/// more than a minute, or overflowed the stack, and takes a time in proportion
/// to its size now.
/// </summary>
public class ReviewReaderTest {
    private const string EARLIER = "The PDF was not completed because of an earlier error: ";

    // Reads the PDF, which must take less than five seconds, and returns its
    // objects, or null when it cannot be read.
    private static List<PDFobj> ReadQuickly(byte[] pdf) {
        Stopwatch watch = Stopwatch.StartNew();
        List<PDFobj> objects;
        try {
            objects = TestSupport.Read(pdf);
        } catch (Exception) {
            objects = null;
        }
        long elapsed = watch.ElapsedMilliseconds;
        Assert.True(elapsed < 5000L, "reading " + pdf.Length + " bytes took " + elapsed + " ms");
        return objects;
    }

    private static string Repeat(string text, int count) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++) {
            sb.Append(text);
        }
        return sb.ToString();
    }

    private static byte[] Latin1(string text) {
        return Encoding.Latin1.GetBytes(text);
    }

    private static byte[] PdfWithObjects(List<string> objects) {
        StringBuilder sb = new StringBuilder("%PDF-1.4\n");
        int[] offsets = new int[objects.Count];
        for (int i = 0; i < objects.Count; i++) {
            offsets[i] = sb.Length;
            sb.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }
        int xref = sb.Length;
        sb.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (int offset in offsets) {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append("trailer\n<< /Size ").Append(objects.Count + 1)
                .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        return Latin1(sb.ToString());
    }

    private static byte[] PdfWithObjects(params string[] objects) {
        return PdfWithObjects(new List<string>(objects));
    }

    private static string ReadError(string raw) {
        try {
            TestSupport.Read(Latin1(raw));
            return "(no error)";
        } catch (Exception e) {
            return e.Message;
        }
    }

    private static void AssertRefused(PDF pdf, string message) {
        InvalidOperationException e = Assert.Throws<InvalidOperationException>(() => pdf.Complete());
        Assert.Equal(EARLIER + message, e.Message);
    }

    // Returns the objects of a small PDF of one page, as Read returns them.
    private static List<PDFobj> ExistingObjects() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        new TextLine(font, "Existing").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        return TestSupport.Read(stream.ToArray());
    }

    // Returns a PDF with no cross-reference table, of a catalog, a page tree
    // of one page and then count objects "<< /A i >>", with or without their
    // endobj.
    private static byte[] NumberedObjects(int count, bool endobj) {
        List<string> objects = new List<string> {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        };
        for (int i = 0; i < count; i++) {
            objects.Add("<< /A " + i + " >>");
        }
        StringBuilder sb = new StringBuilder("%PDF-1.7\n");
        for (int i = 0; i < objects.Count; i++) {
            sb.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append('\n');
            if (endobj) {
                sb.Append("endobj\n");
            }
        }
        sb.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        return Latin1(sb.ToString());
    }

    [Fact]
    public void ARunOfWhiteSpaceIsScannedOnce() {
        // Every space looked at all the spaces after it for a number.
        ReadQuickly(Latin1("%PDF-1.7\n" + Repeat(" ", 400000)));
        ReadQuickly(Latin1("%PDF-1.7\n1" + Repeat("\n", 400000)));
    }

    [Fact]
    public void ObjectsWithNoEndobjAreReadOnce() {
        // Every object was read to the end of the PDF.
        ReadQuickly(Latin1(Repeat("1 0 obj\n", 31000)));

        // Each object ends where the next one starts.
        List<PDFobj> objects = ReadQuickly(NumberedObjects(20000, false));
        Assert.NotNull(objects);
        Assert.Equal(20003, objects.Count);
        Assert.Equal("7", objects[10].GetValue("/A"));
        Assert.Equal("[ 3 0 R ]", objects[1].GetValue("/Kids"));
        Assert.Single(new PDF().GetPageObjects(objects));
    }

    [Fact]
    public void ObjectsWithNoEndobjThatTheTableListsAreReadOnce() {
        // Every object that the cross-reference table lists was read to the
        // end of the PDF.
        List<string> objects = new List<string> {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        };
        for (int i = 0; i < 20000; i++) {
            objects.Add("<< /A " + i + " >>");
        }
        StringBuilder sb = new StringBuilder("%PDF-1.7\n");
        int[] offsets = new int[objects.Count];
        for (int i = 0; i < objects.Count; i++) {
            offsets[i] = sb.Length;
            sb.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append('\n');
        }
        int xref = sb.Length;
        sb.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (int offset in offsets) {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append("trailer\n<< /Size ").Append(objects.Count + 1)
                .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        List<PDFobj> read = ReadQuickly(Latin1(sb.ToString()));
        Assert.NotNull(read);
        Assert.Equal(20003, read.Count);
        Assert.Equal("7", read[10].GetValue("/A"));
        // The object is "11 0 obj << /A 7 >>", without the objects after it.
        Assert.Equal("11 0 obj << /A 7 >>", string.Join(" ", read[10].dict));
    }

    [Fact]
    public void ACrossReferenceSectionThatIsItsOwnPrevIsReadOnce() {
        // The section of a big table was read a thousand times: its /Prev is
        // itself. The PDF is then read by looking for its objects.
        string pdf = Encoding.Latin1.GetString(PdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"));
        int xref = pdf.IndexOf("xref", StringComparison.Ordinal);
        StringBuilder sb = new StringBuilder(pdf.Substring(0, xref));
        sb.Append("xref\n0 4\n0000000000 65535 f \n");
        for (int i = 1; i <= 3; i++) {
            sb.Append(pdf.IndexOf(i + " 0 obj", StringComparison.Ordinal).ToString("D10")).Append(" 00000 n \n");
        }
        // Free entries, which make the table big.
        sb.Append(Repeat("0000000000 65535 f \n", 100000));
        sb.Append("trailer\n<< /Size 4 /Root 1 0 R /Prev ").Append(xref)
                .Append(" >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        List<PDFobj> objects = ReadQuickly(Latin1(sb.ToString()));
        Assert.NotNull(objects);
        Assert.Single(new PDF().GetPageObjects(objects));
    }

    // Returns a PDF of count streams whose /Length is 1 and that have no
    // endstream, each followed by 1,000 bytes and its endobj, which the
    // cross-reference table lists.
    private static byte[] StreamsWithNoEndstream(int count) {
        StringBuilder sb = new StringBuilder("%PDF-1.7\n");
        string filler = Repeat("x", 1000);
        int[] offsets = new int[count];
        for (int i = 0; i < count; i++) {
            offsets[i] = sb.Length;
            sb.Append(i + 1).Append(" 0 obj\n<< /Length 1 >>\nstream\n").Append(filler).Append("\nendobj\n");
        }
        int xref = sb.Length;
        sb.Append("xref\n0 ").Append(count + 1).Append("\n0000000000 65535 f \n");
        foreach (int offset in offsets) {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append("trailer\n<< /Size ").Append(count + 1)
                .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        return Latin1(sb.ToString());
    }

    [Fact]
    public void StreamsWithAWrongLengthAndNoEndstreamAreSearchedOnce() {
        // The endstream of every stream was looked for to the end of the PDF:
        // 8,000 streams, 8.5 MB, took 25 seconds. It is looked for in the
        // object.
        List<PDFobj> objects = ReadQuickly(StreamsWithNoEndstream(8000));
        Assert.NotNull(objects);
        Assert.Equal(8000, objects.Count);
        Assert.Equal("1", objects[7999].GetValue("/Length"));
        Assert.Equal("x", TestSupport.Latin1(objects[7999].stream));
    }

    [Fact]
    public void AStreamThatTheTableListsManyTimesIsSearchedWithinABudget() {
        // Each entry of the stream is an object of its own, which ends at the
        // end of the PDF, and its endstream was looked for there 10,000 times.
        // What the reader may read in all is eight times the length of the
        // PDF: the streams past it keep their /Length.
        StringBuilder sb = new StringBuilder("%PDF-1.7\n1 0 obj\n<< /Length 1 >>\nstream\n");
        sb.Append(Repeat("x", 1000000));
        int xref = sb.Length;
        sb.Append("\nxref\n");
        sb.Append(Repeat("1 1\n0000000009 00000 n \n", 10000));
        sb.Append("trailer\n<< /Size 2 >>\nstartxref\n").Append(xref + 1).Append("\n%%EOF\n");
        List<PDFobj> objects = ReadQuickly(Latin1(sb.ToString()));
        Assert.NotNull(objects);
        Assert.Single(objects);
        Assert.Equal("1", objects[0].GetValue("/Length"));
    }

    [Fact]
    public void SectionsWithNoStartxrefAreReadWithinABudget() {
        // A thousand sections chained by /Prev, with no startxref after them,
        // were each read to the end of the PDF. The sections past the budget
        // are not read, and the PDF is read by looking for its objects.
        StringBuilder sb = new StringBuilder("%PDF-1.7\n");
        int prev = -1;
        for (int i = 0; i < 1000; i++) {
            int offset = sb.Length;
            if (prev == -1) {
                sb.Append("xref\n0 0\ntrailer\n<< /Size 1 >>\n");
            } else {
                sb.Append("xref\n0 0\ntrailer\n<< /Size 1 /Prev ").Append(prev).Append(" >>\n");
            }
            prev = offset;
        }
        sb.Append(Repeat("a ", 500000));
        sb.Append("\nstartxref\n").Append(prev).Append("\n%%EOF\n");
        ReadQuickly(Latin1(sb.ToString()));
    }

    [Fact]
    public void AnObjectThatTheTableListsManyTimesIsReadWithinABudget() {
        // The object with no endobj was read to the end of the PDF for each of
        // its 1,000 entries.
        StringBuilder sb = new StringBuilder("%PDF-1.7\n1 0 obj\n<< /A [");
        sb.Append(Repeat("1 ", 500000));
        sb.Append("] >>\n");
        int xref = sb.Length;
        sb.Append("xref\n");
        sb.Append(Repeat("1 1\n0000000009 00000 n \n", 1000));
        sb.Append("trailer\n<< /Size 2 >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        List<PDFobj> objects = ReadQuickly(Latin1(sb.ToString()));
        Assert.NotNull(objects);
        Assert.Single(objects);
        Assert.Equal("1 0 obj << /A [ 1", string.Join(" ", objects[0].dict.GetRange(0, 7)));
    }

    [Fact]
    public void AnObjectThatAnObjectStreamListsManyTimesIsReadWithinABudget() {
        // The objects of an object stream at offsets 0 and 1,000,000 by turns
        // were each read from 0 to 1,000,000, the next offset. Its objects are
        // read in no more than its length in all, and those past it have no
        // tokens.
        StringBuilder header = new StringBuilder();
        for (int i = 0; i < 1000; i++) {
            header.Append(i + 2).Append(' ').Append((i % 2) * 1000000).Append(' ');
        }
        string data = header + Repeat("1 ", 500000) + "<< >>";
        List<PDFobj> objects = ReadQuickly(PdfWithObjects("<< /Type /ObjStm /N 1000 /First " + header.Length
                + " /Length " + data.Length + " >>\nstream\n" + data + "\nendstream"));
        Assert.NotNull(objects);
        Assert.Equal(1001, objects.Count);
        Assert.Equal("2 0 obj 1 1", string.Join(" ", objects[1].dict.GetRange(0, 5)));
        Assert.Equal("1001 0 obj", string.Join(" ", objects[1000].dict));
    }

    [Fact]
    public void TheEndstreamOfAStreamIsLookedForInItsObject() {
        // A stream with a wrong /Length ends at the endstream in its object,
        // and one with none keeps its /Length: the endstream of the next
        // object, which the search found, made it the bytes up to there.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(
                "<< /Length 3 >>\nstream\nBT (Hello) Tj ET\nendstream",
                "<< /Length 5 >>\nstream\nhello world",
                "<< /Length 3 >>\nstream\nabc\nendstream"));
        Assert.Equal("BT (Hello) Tj ET", TestSupport.Latin1(objects[0].GetData()));
        Assert.Equal("16", objects[0].GetValue("/Length"));
        Assert.Equal("hello", TestSupport.Latin1(objects[1].GetData()));
        Assert.Equal("5", objects[1].GetValue("/Length"));
        Assert.Equal("abc", TestSupport.Latin1(objects[2].GetData()));

        // A stream whose /Length is right goes on past where its object ends
        // at the latest, which is the next "number generation obj" in a PDF
        // with no cross-reference table, and past an endstream in it, and the
        // object in it is not read.
        string data = "x\n2 0 obj\nendstream y";
        objects = TestSupport.Read(Latin1("%PDF-1.4\n1 0 obj\n<< /Length " + data.Length
                + " >>\nstream\n" + data + "\nendstream\nendobj\n"));
        Assert.Single(objects);
        Assert.Equal(data, TestSupport.Latin1(objects[0].GetData()));
    }

    [Fact]
    public void TheLengthOfAStreamIsFoundByItsNumber() {
        // Every stream looked for its /Length among all the objects.
        List<string> objects = new List<string> {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        };
        for (int i = 0; i < 60000; i++) {
            objects.Add("<< /Length " + (objects.Count + 2) + " 0 R >>\nstream\nq Q\nendstream");
            objects.Add("3");
        }
        List<PDFobj> read = ReadQuickly(PdfWithObjects(objects));
        Assert.NotNull(read);
        Assert.Equal(120003, read.Count);
        Assert.Equal("q Q", Encoding.Latin1.GetString(read[119999].GetData()));
    }

    [Fact]
    public void TheLengthOfAStreamIsItsNewestVersion() {
        // The /Length of the stream is updated from 2 to 15 at the end of the
        // PDF, and the stream holds the keyword that the first length ends at.
        string pdf = Encoding.Latin1.GetString(PdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>",
                "<< /Length 5 0 R >>\nstream\nAB endstream CD\nendstream",
                "2"));
        int prev = pdf.IndexOf("xref", StringComparison.Ordinal);
        StringBuilder sb = new StringBuilder(pdf);
        int offset = sb.Length;
        sb.Append("5 0 obj\n15\nendobj\n");
        int xref = sb.Length;
        sb.Append("xref\n0 1\n0000000000 65535 f \n5 1\n").Append(offset.ToString("D10")).Append(" 00000 n \n");
        sb.Append("trailer\n<< /Size 6 /Root 1 0 R /Prev ").Append(prev)
                .Append(" >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        List<PDFobj> objects = TestSupport.Read(Latin1(sb.ToString()));
        Assert.Equal("AB endstream CD", Encoding.Latin1.GetString(objects[3].GetData()));
    }

    // Returns a PDF whose page uses a form XObject that refers to the next
    // object, which refers to the next, count times, and whose page tree has
    // count nodes, one under the other.
    private static byte[] ChainOfObjects(int count) {
        List<string> objects = new List<string>();
        objects.Add("");    // The catalog, below
        objects.Add("<< /Type /Page /MediaBox [0 0 612 792] /Resources << /XObject << /X0 3 0 R >> >> >>");
        for (int i = 0; i < count; i++) {   // Objects 3 and on
            objects.Add("<< /Next " + (objects.Count + 2) + " 0 R >>");
        }
        objects.Add("<< >>");
        int first = objects.Count + 1;
        for (int i = 0; i < count; i++) {
            int kid = (i == count - 1) ? 2 : objects.Count + 2;    // The last is the page.
            objects.Add("<< /Type /Pages /Kids [" + kid + " 0 R] /Count 1 >>");
        }
        objects[0] = "<< /Type /Catalog /Pages " + first + " 0 R >>";
        return PdfWithObjects(objects);
    }

    [Fact]
    public void LongChainsOfObjectsAreFollowedWithoutRecursion() {
        // They overflowed the stack, which ended the process.
        int count = 100000;
        List<PDFobj> objects = TestSupport.Read(ChainOfObjects(count));
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Assert.Single(pdf.GetPageObjects(objects));
        pdf.AddResourceObjects(objects);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        List<PDFobj> written = TestSupport.Read(stream.ToArray());
        Assert.Equal(new List<string> {"<<", ">>"}, PDF.ValueOf(written[count + 2]));
    }

    [Fact]
    public void AddObjectsIsRefusedWhereItWouldLosePages() {
        List<PDFobj> objects = ExistingObjects();
        PDF pdf = TestSupport.NewPDF();
        new Page(pdf, Letter.PORTRAIT);
        Assert.Equal("The objects of an existing PDF cannot be added to a PDF that has pages of its own.",
                Assert.Throws<InvalidOperationException>(() => pdf.AddObjects(objects)).Message);

        // A page after the objects is not in their page tree.
        PDF pdf2 = TestSupport.NewPDF();
        pdf2.AddObjects(ExistingObjects());
        string message = "A page cannot be added to a PDF that AddObjects added the objects of an existing PDF to.";
        Assert.Equal(message,
                Assert.Throws<InvalidOperationException>(() => new Page(pdf2, Letter.PORTRAIT)).Message);
        AssertRefused(pdf2, message);

        // The pages were not made for the compliance of the document.
        PDF pdf3 = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        Assert.Equal("The objects of an existing PDF cannot be added to a PDF/UA or PDF/A document.",
                Assert.Throws<InvalidOperationException>(() => pdf3.AddObjects(objects)).Message);
    }

    [Fact]
    public void ANumberWithNoObjectIsAFreeEntry() {
        // Object 4 is not in the PDF, and was written as "4 0 obj endobj".
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
                "<< /Removed true >>",
                "<< /Kept true >>"));
        PDFobj empty = new PDFobj();    // As Read gives a number with no object
        empty.SetNumber(4);
        objects[3] = empty;
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.AddObjects(objects);
        pdf.Complete();
        string raw = Encoding.Latin1.GetString(stream.ToArray());
        Assert.DoesNotContain("\n4 0 obj", raw);
        string[] entries = raw.Substring(raw.LastIndexOf("\nxref\n", StringComparison.Ordinal)).Split('\n');
        Assert.Equal("0000000000 65535 f ", entries[7]);   // Object 4
        Assert.Equal("true", TestSupport.Read(stream.ToArray())[4].GetValue("/Kept"));
    }

    [Fact]
    public void TwoPagesThatNameDifferentResourcesAlikeAreRefused() {
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 6 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream",
                "<< /Subtype /Form /BBox [0 0 20 20] /Length 0 >>\nstream\n\nendstream"));
        PDF pdf = TestSupport.NewPDF();
        string message = "The pages of the PDF use the name /X0 for different resources, "
                + "and the pages of this document share one resources dictionary.";
        Assert.Equal(message, Assert.Throws<ArgumentException>(() => pdf.AddResourceObjects(objects)).Message);
        AssertRefused(pdf, message);

        // Pages that use the same resource under the same name share it.
        List<PDFobj> shared = TestSupport.Read(PdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream"));
        PDF pdf2 = TestSupport.NewPDF();
        pdf2.AddResourceObjects(shared);
        new Page(pdf2, Letter.PORTRAIT);
        pdf2.Complete();
    }

    [Fact]
    public void TheTypeOfAnObjectIsAnEntryOfItsOwn() {
        // The /Type of the /Group was taken for the type of the page, which a
        // form refers to, and the page was copied with the form.
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Group << /Type /Group /S /Transparency >> /Type /Page /Parent 2 0 R"
                        + " /Resources << /XObject << /X0 4 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Page 3 0 R /Length 0 >>\nstream\n\nendstream"));
        Assert.Equal("/Page", objects[2].GetValue("/Type"));
        Assert.Equal("", objects[2].GetValue("/S"));
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.AddResourceObjects(objects);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        Assert.DoesNotContain("/Transparency", Encoding.Latin1.GetString(stream.ToArray()));
    }

    [Fact]
    public void ANumberThatIsNotAnObjectNumberIsSkipped() {
        List<PDFobj> objects = TestSupport.Read(PdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject <<"
                        + " /Im1 abc 0 R /Im2 99999999999 0 R /Im3 4 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Ref 2147483648 0 R /Length 0 >>\nstream\n\nendstream"));
        PDF pdf = TestSupport.NewPDF();
        pdf.AddResourceObjects(objects);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
    }

    [Fact]
    public void TheNumbersOfAnObjectStreamAreDigits() {
        Assert.Equal("The object stream of the PDF is malformed: \"+5\" is not a number.",
                ReadError("1 0 obj<</Type/ObjStm/N 1/First 5/Length 9>>stream\n+5 0 <<>>\nendstream endobj"));
        Assert.Equal("The object stream of the PDF is malformed: \"2147483648\" is not a number.",
                ReadError("1 0 obj<</Type/ObjStm/N 1/First 2147483648/Length 9>>stream\n5 0 <<>>\nendstream endobj"));
        // An offset past the end of the stream, which overflowed an int.
        Assert.Equal("(no error)",
                ReadError("1 0 obj<</Type/ObjStm/N 2/First 2147483000/Length 13>>stream\n5 1000 6 1001\nendstream endobj"));
    }
}
}   // End of namespace PDFjet.NET
