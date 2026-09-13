/*
 * PageSizeTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class PageSizeTest {
    private static void AssertSize(float width, float height, PageSize portrait, PageSize landscape) {
        Assert.Equal(width, portrait.GetWidth());
        Assert.Equal(height, portrait.GetHeight());
        Assert.Equal(height, landscape.GetWidth());
        Assert.Equal(width, landscape.GetHeight());
    }

    [Fact]
    public void IsoSizesInPoints() {
        AssertSize(842f, 1191f, A3.PORTRAIT, A3.LANDSCAPE);
        AssertSize(595f, 842f, A4.PORTRAIT, A4.LANDSCAPE);
        AssertSize(420f, 595f, A5.PORTRAIT, A5.LANDSCAPE);
        AssertSize(499f, 709f, B5.PORTRAIT, B5.LANDSCAPE);
    }

    [Fact]
    public void JapaneseAndNorthAmericanSizesInPoints() {
        AssertSize(516f, 729f, JISB5.PORTRAIT, JISB5.LANDSCAPE);
        AssertSize(612f, 792f, Letter.PORTRAIT, Letter.LANDSCAPE);
        AssertSize(612f, 1008f, Legal.PORTRAIT, Legal.LANDSCAPE);
        AssertSize(522f, 756f, Executive.PORTRAIT, Executive.LANDSCAPE);
        AssertSize(792f, 1224f, Tabloid.PORTRAIT, Tabloid.LANDSCAPE);
    }

    [Fact]
    public void APageTakesItsSizeFromThePageSize() {
        Page page = new Page(TestSupport.NewPDF(), A4.LANDSCAPE);
        Assert.Equal(842f, page.GetWidth());
        Assert.Equal(595f, page.GetHeight());
        PageSize custom = new PageSize(100f, 200f);
        Assert.Equal(100f, custom.GetWidth());
        Assert.Equal(200f, custom.GetHeight());
    }
}
}
