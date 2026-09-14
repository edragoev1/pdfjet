/**
 * ChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct ChartTests {
    private func series(_ ys: Float...) -> [[Point]] {
        var points = [Point]()
        for (i, y) in ys.enumerated() {
            points.append(Point(Float(i) + 1, y))
        }
        return [points]
    }

    private func draw(_ data: [[Point]]) -> String {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        _ = Chart(font, font).setLocation(50, 50).setSize(300, 200).setData(data).drawOn(page)
        return TestSupport.content(page)
    }

    @Test func aChartWithoutPointsDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        let chart = Chart(font, font).setLocation(50, 50).setSize(300, 200)
        _ = chart.setData([[Point]]())
        TestSupport.expectXY(350, 250, chart.drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func allNegativeDataGetsNegativeAxisLabels() {
        let content = draw(series(-5, -2.5, -1))
        #expect(content.contains(TestSupport.hex("-5.0")), "\(content)")
        #expect(content.contains(TestSupport.hex("-1.0")), "\(content)")
        #expect(!content.contains("NaN"))
    }

    @Test func flatDataIsDrawnWithoutNaN() {
        let content = draw(series(3, 3, 3))
        #expect(!content.contains("NaN"))
        #expect(content.contains(TestSupport.hex("3.0")), "\(content)")
        #expect(content.contains(TestSupport.hex("4.0")), "\(content)")
    }

    @Test func wholeNumberStepsGetWholeNumberLabels() {
        let content = draw(series(10, 60, 35))
        #expect(content.contains(TestSupport.hex("60")), "\(content)")
        #expect(!content.contains(TestSupport.hex("60.00")), "\(content)")
    }

    @Test func aPathSeriesKeepsItsStrokeWidthAndWritesItsText() {
        let p1 = Point(1, 2).setDrawPath(true).setShape(Point.INVISIBLE)
        p1.setStrokeWidth(20).setStrokeColor(Color.blue).setText("label")
        let p2 = Point(3, 2).setShape(Point.INVISIBLE)
        let content = draw([[p1, p2]])
        #expect(content.contains("20 w"), "\(content)")
        #expect(content.contains(TestSupport.hex("label")), "\(content)")
    }

    @Test func labelsUseAPeriodWhateverTheDefaultLocale() {
        // Java sets the default locale to German first; a Swift process cannot
        // change its current locale, so this checks the labels as they are.
        let content = draw(series(1, 2, 3))
        #expect(content.contains(TestSupport.hex("1.25")), "\(content)")
        #expect(!content.contains(TestSupport.hex("1,25")))
    }
}
