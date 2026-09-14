/*
 * CellTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class CellTest {
    [Fact]
    public void ACellWithoutTextHasNoHeight() {
        Assert.Equal(0f, new Cell(TestSupport.Helvetica(TestSupport.NewPDF())).GetHeight(100f));
    }

    [Fact]
    public void EmptyTextIsOneLineTallWithThePaddings() {
        Cell cell = new Cell(TestSupport.Helvetica(TestSupport.NewPDF()), "");
        Assert.Equal(2f, cell.GetTopPadding());
        Assert.Equal(2f, cell.GetBottomPadding());
        TestSupport.AssertNear(17.872f, cell.GetHeight(100f), TestSupport.DELTA);
    }

    [Fact]
    public void TheCellFontSizeSetsTheHeight() {
        Cell cell = new Cell(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        cell.SetFontSize(24f);
        TestSupport.AssertNear(31.744f, cell.GetHeight(100f), TestSupport.DELTA);
    }

    [Fact]
    public void SettingATextBlockClearsTheText() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Cell withBlock = new Cell(font, "text");
        withBlock.SetTextBlock(new TextBlock(font, "block"));
        Assert.Null(withBlock.GetText());
    }

    [Fact]
    public void ANewCellHasTopAndLeftBordersAndSpansOneColumn() {
        // A table adds the right border of its last column and the bottom border of its last row.
        Cell cell = new Cell(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        Assert.True(cell.GetBorder(Border.TOP));
        Assert.True(cell.GetBorder(Border.LEFT));
        Assert.False(cell.GetBorder(Border.RIGHT));
        Assert.False(cell.GetBorder(Border.BOTTOM));
        Assert.Equal(1, cell.GetColSpan());
    }

    [Fact]
    public void SetBorderChangesOneBorderAndKeepsTheColumnSpan() {
        Cell cell = new Cell(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        cell.SetColSpan(3);
        cell.SetBorder(Border.TOP, false).SetBorder(Border.BOTTOM, true);
        Assert.False(cell.GetBorder(Border.TOP));
        Assert.True(cell.GetBorder(Border.LEFT));
        Assert.True(cell.GetBorder(Border.BOTTOM));
        Assert.Equal(3, cell.GetColSpan());
        cell.SetBorders(false);
        Assert.False(cell.GetBorder(Border.LEFT) || cell.GetBorder(Border.BOTTOM));
        Assert.Equal(3, cell.GetColSpan());
    }
}
}
