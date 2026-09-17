/*
 * CellTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
public class CellTest {
    // A drawable that is 30 wide and 20 high, and remembers where it was
    // placed and how many times it was drawn.
    private sealed class Box : IDrawable {
        internal float x;
        internal float y;
        internal int draws;

        public float[] DrawOn(Page page) {
            if (page != null) {
                draws++;
            }
            return new float[] {x + 30f, y + 20f};
        }

        public IDrawable SetLocation(float x, float y) {
            this.x = x;
            this.y = y;
            return this;
        }
    }

    [Fact]
    public void TheLastContentSetterWins() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Cell cell = new Cell(font, "text");
        Barcode barcode = new Barcode(Barcode.CODE_128, "x");
        cell.SetTextBlock(new TextBlock(font, "block")).SetBarcode(barcode);
        Assert.Null(cell.GetTextBlock());
        Assert.Same(barcode, cell.GetBarcode());
        Assert.Same(barcode, cell.GetDrawable());
        Box box = new Box();
        cell.SetDrawable(box);
        Assert.Null(cell.GetBarcode());
        Assert.Null(cell.GetImage());
        Assert.Null(cell.GetTextColumn());
        Assert.Same(box, cell.GetDrawable());
        Assert.Null(cell.GetText());
    }

    [Fact]
    public void AnyDrawableIsMeasuredAndAlignedInTheCell() {
        PDF pdf = TestSupport.NewPDF();
        Box box = new Box();
        Cell cell = new Cell(TestSupport.Helvetica(pdf)).SetDrawable(box);
        TestSupport.AssertNear(24f, cell.GetHeight(100f), TestSupport.DELTA, "height");  // 20 and the paddings of 2
        Page page = new Page(pdf, Letter.PORTRAIT);
        cell.DrawOn(page, 10f, 50f, 100f, 24f);
        TestSupport.AssertXY(12f, 52f, new float[] {box.x, box.y});
        cell.SetTextAlignment(Alignment.CENTER).DrawOn(page, 10f, 50f, 100f, 24f);
        TestSupport.AssertXY(45f, 52f, new float[] {box.x, box.y});
        cell.SetTextAlignment(Alignment.RIGHT).DrawOn(page, 10f, 50f, 100f, 24f);
        TestSupport.AssertXY(78f, 52f, new float[] {box.x, box.y});
        Assert.Equal(3, box.draws);
    }

    [Fact]
    public void TextSetAfterTheDrawableIsDrawnAndMeasuredInstead() {
        PDF pdf = TestSupport.NewPDF();
        Box box = new Box();
        Cell cell = new Cell(TestSupport.Helvetica(pdf)).SetDrawable(box);
        cell.SetText("x");
        TestSupport.AssertNear(17.872f, cell.GetHeight(100f), TestSupport.DELTA, "height");
        cell.DrawOn(new Page(pdf, Letter.PORTRAIT), 10f, 50f, 100f, 24f);
        Assert.Equal(0, box.draws);
        Assert.Same(box, cell.GetDrawable());
    }

    [Fact]
    public void ATableFitsItsColumnsToAnyDrawable() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        List<List<Cell>> data = new List<List<Cell>> {
            new List<Cell> {new Cell(font, "a")},
            new List<Cell> {new Cell(font).SetDrawable(new Box())},
        };
        Table table = new Table().SetTableData(data, 1).AutoAdjustColumnWidths();
        TestSupport.AssertNear(34f, table.GetColumnWidth(0), TestSupport.DELTA, "width");
    }

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

    [Fact]
    public void TransparentLeavesTheTextColorUnchanged() {
        Cell cell = new Cell(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        cell.SetTextColor(Color.blue).SetTextColor(Color.transparent);
        TestSupport.AssertRGB(0f, 0f, 1f, cell.GetTextColor());
    }

    [Fact]
    public void ColorsAreKeptToTheNearestOf256Steps() {
        Cell cell = new Cell(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        TestSupport.AssertRGB(0f, 0f, 0f, cell.GetTextColor());         // black by default
        Assert.Null(cell.GetBackgroundColor());
        Assert.Null(cell.GetBorderColor());
        cell.SetTextColor(new float[] {0.5f, 0.25f, 1f});
        TestSupport.AssertRGB(128 / 255f, 64 / 255f, 1f, cell.GetTextColor());
        cell.SetBackgroundColor(new float[] {-1f, 2f, 0.1f});           // kept between 0 and 1
        TestSupport.AssertRGB(0f, 1f, 26 / 255f, cell.GetBackgroundColor());
        cell.SetBorderColor(0x336699);
        TestSupport.AssertRGB(0x33 / 255f, 0x66 / 255f, 0x99 / 255f, cell.GetBorderColor());
        cell.SetBackgroundColor(Color.transparent);
        Assert.Null(cell.GetBackgroundColor());
        cell.SetBorderColor((float[]) null);
        Assert.Null(cell.GetBorderColor());
    }

    [Fact]
    public void SetFontChangesTheFallbackFontUnlessAnotherWasSet() {
        PDF pdf = TestSupport.NewPDF();
        Font helvetica = TestSupport.Helvetica(pdf);
        Font courier = new Font(pdf, CoreFont.COURIER);
        Cell cell = new Cell(helvetica, "x").SetFont(courier);
        Assert.Same(courier, cell.GetFallbackFont());
        cell.SetFallbackFont(helvetica).SetFont(new Font(pdf, CoreFont.TIMES_ROMAN));
        Assert.Same(helvetica, cell.GetFallbackFont());
    }
}
}
