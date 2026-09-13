/*
 * BookmarkTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
}
}
