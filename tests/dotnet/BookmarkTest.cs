/*
 * BookmarkTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PDFjet.NET {
public class BookmarkTest {
    [Fact]
    public void TheRootHasNoTitleAndNoDestination() {
        Bookmark root = new Bookmark(TestSupport.NewPDF());
        Assert.Null(root.GetTitle());
        Assert.Null(root.GetDestinationName());
    }

    [Fact]
    public void ATitleHasItsWhitespaceCollapsed() {
        PDF pdf = TestSupport.NewPDF();
        Bookmark root = new Bookmark(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Bookmark child = root.AddBookmark(page, new Title(TestSupport.Helvetica(pdf), "Chapter\t one\n  intro", 10f, 10f));
        Assert.Equal("Chapter one intro", child.GetTitle());
        Assert.NotNull(child.GetDestinationName());
        Assert.Same(root, child.GetParent());
    }

    private static PDFobj Item(List<PDFobj> objects, string title) {
        foreach (PDFobj obj in objects) {
            string value = obj.GetValue("/Title");
            if (value.Length > 0 && obj.GetValue("/Producer").Length == 0
                    && TestSupport.Utf16Hex(value) == title) {
                return obj;
            }
        }
        throw new Xunit.Sdk.XunitException("no outline item " + title);
    }

    [Fact]
    public void NestedBookmarksMakeATreeThatReadersNeedNotRepair() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Bookmark root = new Bookmark(pdf);
        root.AddBookmark(page, new Title(font, "A", 10f, 10f));
        Bookmark b = root.AddBookmark(page, new Title(font, "B", 10f, 30f));
        b.AddBookmark(page, new Title(font, "B1", 10f, 50f));
        Bookmark b2 = b.AddBookmark(page, new Title(font, "B2", 10f, 70f));
        b2.AddBookmark(page, new Title(font, "B2a", 10f, 90f));
        root.AddBookmark(page, new Title(font, "C", 10f, 110f));
        pdf.Complete();

        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        PDFobj outlines = null;
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Type") == "/Outlines") {
                outlines = obj;
            }
        }
        Assert.NotNull(outlines);
        string root0 = outlines.GetNumber().ToString();
        // The outline dictionary has the items of the first level.
        Assert.Equal(Item(objects, "A").GetNumber().ToString(), outlines.GetValue("/First"));
        Assert.Equal(Item(objects, "C").GetNumber().ToString(), outlines.GetValue("/Last"));
        Assert.Equal("3", outlines.GetValue("/Count"));
        Assert.Equal(root0, Item(objects, "A").GetValue("/Parent"));
        Assert.Equal(root0, Item(objects, "C").GetValue("/Parent"));

        // A nested item has the item above it as its parent, and a closed item
        // counts the items that opening it shows.
        PDFobj itemB = Item(objects, "B");
        PDFobj itemB2 = Item(objects, "B2");
        Assert.Equal("-2", itemB.GetValue("/Count"));
        Assert.Equal(Item(objects, "B1").GetNumber().ToString(), itemB.GetValue("/First"));
        Assert.Equal(itemB2.GetNumber().ToString(), itemB.GetValue("/Last"));
        Assert.Equal(itemB.GetNumber().ToString(), Item(objects, "B1").GetValue("/Parent"));
        Assert.Equal(itemB.GetNumber().ToString(), itemB2.GetValue("/Parent"));
        Assert.Equal(itemB2.GetNumber().ToString(), Item(objects, "B1").GetValue("/Next"));
        Assert.Equal(Item(objects, "B1").GetNumber().ToString(), itemB2.GetValue("/Prev"));
        Assert.Equal("-1", itemB2.GetValue("/Count"));
        Assert.Equal(itemB2.GetNumber().ToString(), Item(objects, "B2a").GetValue("/Parent"));
        Assert.Equal("", Item(objects, "B2a").GetValue("/Count"));
    }

    // A document with the headings, as text lines of their structure types, H1
    // on the first page and the others on the second.
    private static MemoryStream HeadingsDoc(bool tagged, List<string[]> headings, out PDF pdf) {
        MemoryStream stream = new MemoryStream();
        pdf = new PDF(stream);
        if (tagged) {
            pdf.SetCompliance(Compliance.PDF_UA_1).SetTitle("Test");
            pdf.SetTitle("Title");
        }
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = null;
        for (int i = 0; i < headings.Count; i++) {
            if (page == null || (headings[i][0] == "H1" && i > 0)) {
                page = new Page(pdf, Letter.PORTRAIT);
            }
            StructElem structure = (StructElem) System.Enum.Parse(typeof(StructElem), headings[i][0]);
            new TextLine(font, headings[i][1]).SetStructureType(structure)
                    .SetLocation(70f, 100f + 40f * i).DrawOn(page);
        }
        return stream;
    }

    // A tagged document with headings and no bookmarks of its own has the
    // bookmarks of its headings, each under the heading of a higher level
    // before it, and each at the top of its heading on its page, as PAC asks.
    [Fact]
    public void ATaggedDocumentHasTheBookmarksOfItsHeadings() {
        MemoryStream stream = HeadingsDoc(true, new List<string[]> {
                new[] {"H1", "Intro"}, new[] {"H2", "What  it is"}, new[] {"H3", "In short"},
                new[] {"H2", "Why"}, new[] {"H1", "Use"}}, out PDF pdf);
        pdf.Complete();
        List<PDFobj> objects = TestSupport.Read(stream.ToArray());
        PDFobj outlines = null;
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Type") == "/Outlines") {
                outlines = obj;
            }
        }
        Assert.NotNull(outlines);
        string Number(string title) => Item(objects, title).GetNumber().ToString();
        Assert.Equal(Number("Intro"), outlines.GetValue("/First"));
        Assert.Equal(Number("Use"), outlines.GetValue("/Last"));
        // The whitespace of a title is collapsed, as AddBookmark does
        Assert.Equal(Number("Intro"), Item(objects, "What it is").GetValue("/Parent"));
        Assert.Equal(Number("What it is"), Item(objects, "In short").GetValue("/Parent"));
        Assert.Equal(Number("Intro"), Item(objects, "Why").GetValue("/Parent"));
        Assert.Equal(Number("Why"), Item(objects, "What it is").GetValue("/Next"));
        // The destination: the page of the heading, and the top of its text
        string intro = Item(objects, "Intro").GetValue("/Dest");
        string use = Item(objects, "Use").GetValue("/Dest");
        Assert.Contains("/XYZ", intro);
        Assert.Contains("/XYZ", use);
        string[] introFields = intro.Split(new[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
        string[] useFields = use.Split(new[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEqual(introFields[1], useFields[1]);
        // 792 - (100 - 12): the top of a line of 12 points at 100
        Assert.Contains("/XYZ 0 704 0", intro);
    }

    // A document that is not tagged has no headings, and one with bookmarks
    // of its own keeps them.
    [Fact]
    public void OfHeadingsOnlyInATaggedDocumentWithoutBookmarks() {
        MemoryStream untagged = HeadingsDoc(false, new List<string[]> {new[] {"H1", "Intro"}}, out PDF pdf1);
        pdf1.Complete();
        Assert.DoesNotContain("/Outlines", System.Text.Encoding.Latin1.GetString(untagged.ToArray()));

        MemoryStream own = HeadingsDoc(true, new List<string[]> {new[] {"H1", "Intro"}}, out PDF pdf2);
        Page page = new Page(pdf2, Letter.PORTRAIT);
        Font font = new Font(pdf2, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        new Bookmark(pdf2).AddBookmark(page, new Title(font, "Mine", 10f, 10f));
        pdf2.Complete();
        List<PDFobj> objects = TestSupport.Read(own.ToArray());
        Item(objects, "Mine");
        foreach (PDFobj obj in objects) {
            string value = obj.GetValue("/Title");
            Assert.False(value.Length > 0 && obj.GetValue("/Producer").Length == 0
                    && TestSupport.Utf16Hex(value) == "Intro",
                    "the bookmarks of the document were replaced by those of its headings");
        }
    }

    [Fact]
    public void ATitleIsATextStringThatEveryReaderDecodes() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Bookmark root = new Bookmark(pdf);
        root.AddBookmark(page, new Title(TestSupport.Helvetica(pdf), "\u00dcbersicht \u2013 r\u00e9sum\u00e9", 10f, 10f));
        pdf.Complete();
        PDFobj obj = Item(TestSupport.Read(stream.ToArray()), "\u00dcbersicht \u2013 r\u00e9sum\u00e9");
        Assert.StartsWith("<feff00dc", obj.GetValue("/Title").ToLowerInvariant());
    }
}
}
