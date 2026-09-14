/**
 * CellTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct CellTests {
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
