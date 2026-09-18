/*
 * MergeTest.cs
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
/// <summary>Merging the pages of documents that were read.</summary>
public class MergeTest {
    // Returns a document with one page for each text, drawn with Helvetica.
    private static byte[] Document(params string[] texts) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        foreach (string text in texts) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            new TextLine(font, text).SetLocation(50f, 50f).DrawOn(page);
        }
        pdf.Complete();
        return stream.ToArray();
    }

    // Returns the decoded content of each page, in the order of the pages.
    private static List<string> PageContents(List<PDFobj> objects) {
        List<string> contents = new List<string>();
        foreach (PDFobj page in new PDF().GetPageObjects(objects)) {
            contents.Add(TestSupport.Latin1(page.GetContentObject(objects).GetData()));
        }
        return contents;
    }

    // Checks that every reference of the document is to an object it has.
    private static void AssertReferencesResolve(List<PDFobj> objects) {
        Regex digits = new Regex("^[0-9]+$");
        foreach (PDFobj obj in objects) {
            List<string> dict = obj.dict;
            for (int i = 0; i + 2 < dict.Count; i++) {
                if (dict[i + 2] == "R" && digits.IsMatch(dict[i]) && digits.IsMatch(dict[i + 1])) {
                    int number = Int32.Parse(dict[i]);
                    Assert.True(number >= 1 && number <= objects.Count && objects[number - 1].dict.Count > 0,
                            "object " + obj.number + " refers to the missing object " + number);
                }
            }
        }
    }

    [Fact]
    public void MergesDocumentsInTheirOrder() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.Merge(TestSupport.Read(Document("A1", "A2")));
        pdf.Merge(TestSupport.Read(Document("B1")));
        pdf.Complete();
        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        List<string> contents = PageContents(objects);
        Assert.Equal(3, contents.Count);
        Assert.Contains(TestSupport.Hex("A1"), contents[0]);
        Assert.Contains(TestSupport.Hex("A2"), contents[1]);
        Assert.Contains(TestSupport.Hex("B1"), contents[2]);
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void DrawnPagesKeepTheirPlace() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        new TextLine(font, "G1").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Merge(TestSupport.Read(Document("B1")));
        new TextLine(font, "G2").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        List<string> contents = PageContents(objects);
        Assert.Equal(3, contents.Count);
        Assert.Contains(TestSupport.Hex("G1"), contents[0]);
        Assert.Contains(TestSupport.Hex("B1"), contents[1]);
        Assert.Contains(TestSupport.Hex("G2"), contents[2]);
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void AMergedPageInheritsFromThePageTree() {
        string content = "BT /F1 24 Tf 20 300 Td (Inherited) Tj ET";
        string source = "%PDF-1.4\n"
                + "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n"
                + "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 300 400] /Rotate 90"
                + " /Resources << /Font << /F1 4 0 R >> >> >> endobj\n"
                + "3 0 obj << /Type /Page /Parent 2 0 R /Contents 5 0 R >> endobj\n"
                + "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
                + "5 0 obj << /Length " + content.Length + " >>\nstream\n" + content + "\nendstream\nendobj\n"
                + "trailer << /Root 1 0 R >>\n%%EOF\n";
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.Merge(TestSupport.Read(Encoding.Latin1.GetBytes(source)));
        pdf.Complete();

        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        PDFobj page = new PDF().GetPageObjects(objects)[0];
        Assert.Equal(300f, page.GetPageSize().GetWidth());
        Assert.Equal(400f, page.GetPageSize().GetHeight());
        Assert.Equal("90", page.GetValue("/Rotate"));
        Assert.Contains("/F1", page.dict);
        int parent = page.GetObjectNumbers("/Parent")[0];
        Assert.Equal("/Pages", objects[parent - 1].GetValue("/Type"));
        Assert.Contains("(Inherited) Tj", PageContents(objects)[0]);
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void LinksPointAtTheMergedPages() {
        MemoryStream source = new MemoryStream();
        PDF pdf1 = new PDF(source);
        Font font1 = TestSupport.Helvetica(pdf1);
        Page page1 = new Page(pdf1, Letter.PORTRAIT);
        new TextLine(font1, "Go").SetGoToAction("there").SetLocation(50f, 50f).DrawOn(page1);
        Page page2 = new Page(pdf1, Letter.PORTRAIT);
        page2.AddDestination("there", 30f, 100f);
        pdf1.Complete();

        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        new TextLine(TestSupport.Helvetica(pdf), "Cover").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Merge(TestSupport.Read(source.ToArray()));
        pdf.Complete();

        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        Assert.Equal(3, pages.Count);
        PDFobj link = null;
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Subtype") == "/Link") {
                link = obj;
            }
        }
        Assert.NotNull(link);
        // The link on the second page points at the third page and is listed by the second.
        int dest = link.dict.IndexOf("/Dest");
        Assert.Equal("[", link.dict[dest + 1]);
        Assert.Equal(pages[2].number.ToString(), link.dict[dest + 2]);
        Assert.Equal("R", link.dict[dest + 4]);
        Assert.Contains(link.number, pages[1].GetObjectNumbers("/Annots"));
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void MergesTheListedPagesInTheirOrder() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.Merge(TestSupport.Read(Document("A1", "A2", "A3")), 3, 1);
        pdf.Complete();
        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        List<string> contents = PageContents(objects);
        Assert.Equal(2, contents.Count);
        Assert.Contains(TestSupport.Hex("A3"), contents[0]);
        Assert.Contains(TestSupport.Hex("A1"), contents[1]);
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void SplitsADocumentIntoOnePDFPerPage() {
        List<PDFobj> source = TestSupport.Read(Document("A1", "A2", "A3"));
        for (int i = 1; i <= 3; i++) {
            MemoryStream stream = new MemoryStream();
            PDF part = new PDF(stream);
            part.Merge(source, i);
            part.Complete();
            List<PDFobj> objects = TestSupport.Read(stream.ToArray());
            List<string> contents = PageContents(objects);
            Assert.Single(contents);
            Assert.Contains(TestSupport.Hex("A" + i), contents[0]);
            AssertReferencesResolve(objects);
        }
    }

    [Fact]
    public void ALinkToAPageThatIsNotMergedLeadsNowhere() {
        MemoryStream source = new MemoryStream();
        PDF pdf1 = new PDF(source);
        Page page1 = new Page(pdf1, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf1), "Go").SetGoToAction("there").SetLocation(50f, 50f).DrawOn(page1);
        Page page2 = new Page(pdf1, Letter.PORTRAIT);
        page2.AddDestination("there", 30f, 100f);
        pdf1.Complete();

        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.Merge(TestSupport.Read(source.ToArray()), 1);
        pdf.Complete();

        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        Assert.Single(new PDF().GetPageObjects(objects));
        PDFobj link = null;
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Subtype") == "/Link") {
                link = obj;
            }
        }
        Assert.NotNull(link);
        int dest = link.dict.IndexOf("/Dest");
        Assert.Equal("[", link.dict[dest + 1]);
        Assert.Equal("null", link.dict[dest + 2]);
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void AnEncryptedDocumentMergesThePagesEncrypted() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.SetEncryption(new Encryption(pdf, new Passwords(), new Permissions()));
        pdf.Merge(TestSupport.Read(Document("Secret A", "Secret B")));
        pdf.Complete();
        byte[] bytes = stream.ToArray();
        Assert.Contains("/Encrypt ", TestSupport.Latin1(bytes));
        List<PDFobj> objects = TestSupport.Read(bytes, "");
        List<string> contents = PageContents(objects);
        Assert.Equal(2, contents.Count);
        Assert.Contains(TestSupport.Hex("Secret A"), contents[0]);
        Assert.Contains(TestSupport.Hex("Secret B"), contents[1]);
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void AnEncryptedDocumentIsMergedDecrypted() {
        MemoryStream source = new MemoryStream();
        PDF pdf1 = new PDF(source);
        pdf1.SetEncryption(new Encryption(pdf1, new Passwords(), new Permissions()));
        new TextLine(TestSupport.Helvetica(pdf1), "Plain").SetLocation(50f, 50f).DrawOn(new Page(pdf1, Letter.PORTRAIT));
        pdf1.Complete();

        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.Merge(TestSupport.Read(source.ToArray(), ""));
        pdf.Complete();
        byte[] bytes = stream.ToArray();
        Assert.DoesNotContain("/Encrypt ", TestSupport.Latin1(bytes));
        List<PDFobj> objects = TestSupport.Read(bytes);
        Assert.Contains(TestSupport.Hex("Plain"), PageContents(objects)[0]);
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void MergesTheTestDocuments() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        foreach (string name in new string[] {"scanned-linearized.pdf", "rc65-16e.pdf", "PDFjetLogo.pdf"}) {
            pdf.Merge(new PDF().Read(TestSupport.Open("data/testPDFs/" + name)));
        }
        pdf.Complete();
        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        List<PDFobj> pages = new PDF().GetPageObjects(objects);
        Assert.Equal(8, pages.Count);
        foreach (PDFobj page in pages) {
            Assert.NotNull(page.GetContentObject(objects).GetData());
        }
        AssertReferencesResolve(objects);
    }

    [Fact]
    public void MergeIsRefusedWhereItWouldBreakTheDocument() {
        List<PDFobj> objects = TestSupport.Read(Document("A"));

        PDF ua = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        Assert.Equal("Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document.",
                Assert.Throws<InvalidOperationException>(() => ua.Merge(objects)).Message);

        PDF completed = new PDF(new MemoryStream());
        new Page(completed, Letter.PORTRAIT);
        completed.Complete();
        Assert.Equal("The PDF was already completed.",
                Assert.Throws<InvalidOperationException>(() => completed.Merge(objects)).Message);

        PDF rewritten = new PDF(new MemoryStream());
        rewritten.AddObjects(TestSupport.Read(Document("B")));
        Assert.Equal("Merge and AddObjects cannot be used on the same PDF.",
                Assert.Throws<InvalidOperationException>(() => rewritten.Merge(objects)).Message);

        PDF merged = new PDF(new MemoryStream());
        merged.Merge(objects);
        Assert.Equal("Merge and AddObjects cannot be used on the same PDF.",
                Assert.Throws<InvalidOperationException>(
                        () => merged.AddObjects(TestSupport.Read(Document("C")))).Message);

        PDF empty = new PDF(new MemoryStream());
        Assert.Equal("The objects have no root /Pages object.",
                Assert.Throws<ArgumentException>(() => empty.Merge(new List<PDFobj>())).Message);

        PDF split = new PDF(new MemoryStream());
        Assert.Equal("The document has no page 0.",
                Assert.Throws<ArgumentException>(() => split.Merge(objects, 0)).Message);
        Assert.Equal("The document has no page 2.",
                Assert.Throws<ArgumentException>(() => split.Merge(objects, 2)).Message);
        Assert.Equal("Page 1 is listed twice.",
                Assert.Throws<ArgumentException>(() => split.Merge(objects, 1, 1)).Message);
    }
}
}   // End of namespace PDFjet.NET
