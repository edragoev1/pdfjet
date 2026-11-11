/**
 * DefaultWidthTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct DefaultWidthTests {
    // Every line PDFjet draws by default is 0.5 points wide (9.1.9), as Word's
    // table borders and TextBlock's and TextFrame's are: 0, the thinnest line
    // of the device, is a pixel on a screen and faint or gone in print, and the
    // PDF standard advises against it (ISO 32000, 8.4.3.2). 0 is still there
    // when set.
    @Test func everyLineIsHalfAPointUnlessSet() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        #expect(Cell(font, "x").getBorderWidth() == 0.5, "a cell's border")
        // The width a rect, a line and a path draw with, on pages of their own
        var page = Page(pdf, Letter.PORTRAIT)
        _ = Rect(0, 0, 10, 10).setBorderColor(Color.black).drawOn(page)
        #expect(TestSupport.content(page).contains("0.5 w\n"), "a rect's border")
        page = Page(pdf, Letter.PORTRAIT)
        _ = Line(0, 0, 10, 10).drawOn(page)
        #expect(TestSupport.content(page).contains("0.5 w\n"), "a line")
        page = Page(pdf, Letter.PORTRAIT)
        _ = Path().add(Point(0, 0)).add(Point(10, 10)).drawOn(page)
        #expect(TestSupport.content(page).contains("0.5 w\n"), "a path")
        page = Page(pdf, Letter.PORTRAIT)
        page.setDefaultPenWidth()
        #expect(TestSupport.content(page).contains("0.5 w\n"), "the default pen")

        let table = BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(1)
        table.setTableData(["Name"], [["a"]])
        table.setLocation(20, 20)
        try table.complete()
        let content = TestSupport.content(table.getPages()[0])
        #expect(content.contains("0.5 w\n") && !content.contains("\n0 w\n"), "a big table's lines: \(content)")
        // Set to 0, the thinnest line, as before
        let zero = BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(1).setBorderWidth(0)
        zero.setTableData(["Name"], [["a"]])
        zero.setLocation(20, 20)
        try zero.complete()
        #expect(TestSupport.content(zero.getPages()[0]).contains("0 w\n"), "a big table set to 0")
    }
}
