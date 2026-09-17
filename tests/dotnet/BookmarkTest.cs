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
