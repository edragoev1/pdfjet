/**
 * CellTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// A drawable that is 30 wide and 20 high, and remembers where it was placed
/// and how many times it was drawn.
private final class Box: Drawable {
    var x: Float = 0
    var y: Float = 0
    var draws = 0

    func drawOn(_ page: Page?) -> [Float] {
        if page != nil {
            draws += 1
        }
        return [x + 30, y + 20]
    }

    func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }
}

@Suite struct CellTests {
    @Test func theLastContentSetterWins() throws {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let cell = Cell(font, "text")
        let barcode = try Barcode(Barcode.CODE_128, "x")
        cell.setTextBlock(TextBlock(font, "block")).setBarcode(barcode)
        #expect(cell.getTextBlock() == nil)
        #expect(cell.getBarcode() === barcode)
        #expect(cell.getDrawable() === barcode)
        let box = Box()
        cell.setDrawable(box)
        #expect(cell.getBarcode() == nil && cell.getImage() == nil && cell.getTextColumn() == nil)
        #expect(cell.getDrawable() === box)
        #expect(cell.getText() == nil)
    }

    @Test func anyDrawableIsMeasuredAndAlignedInTheCell() {
        let pdf = TestSupport.newPDF()
        let box = Box()
        let cell = Cell(TestSupport.helvetica(pdf)).setDrawable(box)
        TestSupport.expectNear(24, cell.getHeight(100))     // 20 and the paddings of 2
        let page = Page(pdf, Letter.PORTRAIT)
        cell.drawOn(page, 10, 50, 100, 24)
        TestSupport.expectXY(12, 52, [box.x, box.y])
        cell.setTextAlignment(Alignment.CENTER).drawOn(page, 10, 50, 100, 24)
        TestSupport.expectXY(45, 52, [box.x, box.y])
        cell.setTextAlignment(Alignment.RIGHT).drawOn(page, 10, 50, 100, 24)
        TestSupport.expectXY(78, 52, [box.x, box.y])
        #expect(box.draws == 3)
    }

    @Test func textSetAfterTheDrawableIsDrawnAndMeasuredInstead() {
        let pdf = TestSupport.newPDF()
        let box = Box()
        let cell = Cell(TestSupport.helvetica(pdf)).setDrawable(box)
        cell.setText("x")
        TestSupport.expectNear(17.872, cell.getHeight(100))
        cell.drawOn(Page(pdf, Letter.PORTRAIT), 10, 50, 100, 24)
        #expect(box.draws == 0)
        #expect(cell.getDrawable() === box)
    }

    @Test func aTableFitsItsColumnsToAnyDrawable() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let data = [[Cell(font, "a")], [Cell(font).setDrawable(Box())]]
        let table = Table().setTableData(data, 1).autoAdjustColumnWidths()
        TestSupport.expectNear(34, table.getColumnWidth(0))
    }

    @Test func aCellWithoutTextHasNoHeight() {
        #expect(Cell(TestSupport.helvetica(TestSupport.newPDF())).getHeight(100) == 0)
    }

    @Test func emptyTextIsOneLineTallWithThePaddings() {
        let cell = Cell(TestSupport.helvetica(TestSupport.newPDF()), "")
        #expect(cell.getTopPadding() == 2)
        #expect(cell.getBottomPadding() == 2)
        TestSupport.expectNear(17.872, cell.getHeight(100))
    }

    @Test func theCellFontSizeSetsTheHeight() {
        let cell = Cell(TestSupport.helvetica(TestSupport.newPDF()), "x")
        _ = cell.setFontSize(24)
        TestSupport.expectNear(31.744, cell.getHeight(100))
    }

    @Test func settingATextBlockClearsTheText() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let withBlock = Cell(font, "text")
        _ = withBlock.setTextBlock(TextBlock(font, "block"))
        #expect(withBlock.getText() == nil)
    }

    @Test func aNewCellHasTopAndLeftBordersAndSpansOneColumn() {
        // A table adds the right border of its last column and the bottom border of its last row.
        let cell = Cell(TestSupport.helvetica(TestSupport.newPDF()), "x")
        #expect(cell.getBorder(Border.TOP))
        #expect(cell.getBorder(Border.LEFT))
        #expect(!cell.getBorder(Border.RIGHT))
        #expect(!cell.getBorder(Border.BOTTOM))
        #expect(cell.getColSpan() == 1)
    }

    @Test func setBorderChangesOneBorderAndKeepsTheColumnSpan() {
        let cell = Cell(TestSupport.helvetica(TestSupport.newPDF()), "x")
        _ = cell.setColSpan(3)
        _ = cell.setBorder(Border.TOP, false).setBorder(Border.BOTTOM, true)
        #expect(!cell.getBorder(Border.TOP))
        #expect(cell.getBorder(Border.LEFT))
        #expect(cell.getBorder(Border.BOTTOM))
        #expect(cell.getColSpan() == 3)
        _ = cell.setBorders(false)
        #expect(!(cell.getBorder(Border.LEFT) || cell.getBorder(Border.BOTTOM)))
        #expect(cell.getColSpan() == 3)
    }

    @Test func transparentLeavesTheTextColorUnchanged() {
        let cell = Cell(TestSupport.helvetica(TestSupport.newPDF()), "x")
        cell.setTextColor(Color.blue).setTextColor(Color.transparent)
        TestSupport.expectRGB(0, 0, 1, cell.getTextColor())
    }

    @Test func setFontChangesTheFallbackFontUnlessAnotherWasSet() throws {
        let pdf = TestSupport.newPDF()
        let helvetica = TestSupport.helvetica(pdf)
        let courier = try Font(pdf, CoreFont.COURIER)
        let cell = Cell(helvetica, "x").setFont(courier)
        #expect(cell.getFallbackFont() === courier)
        cell.setFallbackFont(helvetica).setFont(try Font(pdf, CoreFont.TIMES_ROMAN))
        #expect(cell.getFallbackFont() === helvetica)
    }
}
