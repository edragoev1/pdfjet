/**
 * ChartReviewFourTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The findings of the fourth read-only review of the charts, of 26 October
/// 2026.
@Suite struct ChartReviewFourTests {
    private func count(_ content: String, _ text: String) -> Int {
        return content.components(separatedBy: text).count - 1
    }

    // The right end of the text, drawn in the font at its size.
    private func endOf(_ content: String, _ font: Font, _ text: String) -> Float {
        return TestSupport.positionOf(content, text)[0] + font.stringWidth(text)
    }

    @Test func barChartAxisKeepsTheDigitsOfItsStepWithNoFractionDigits() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var page = Page(pdf, Letter.PORTRAIT)
        var chart = BarChart(font, font).setLocation(50, 50).setSize(300, 300)
                .setCategories("a", "b").setFractionDigits(0)
        chart.addSeries("", [1, 2])
        chart.drawOn(page)
        var content = TestSupport.content(page)
        #expect(content.contains("<" + TestSupport.hex("0.25") + ">"))     // Not 0, 0, 1, 1

        page = Page(pdf, Letter.PORTRAIT)
        chart = BarChart(font, font).setLocation(50, 50).setSize(300, 300).setCategories("a", "b")
        chart.addSeries("", [0.002, 0.004])
        chart.drawOn(page)
        content = TestSupport.content(page)
        #expect(content.contains("<" + TestSupport.hex("0.0020") + ">"))   // The digits of 0.0005
        #expect(!content.contains("<" + TestSupport.hex("0.00") + ">"))
    }

    @Test func barChartOfASpanPastTheLargestFloatDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = BarChart(font, font).setLocation(50, 50).setSize(300, 200).setCategories("a", "b")
        chart.addSeries("", [-3e38, 3e38])
        TestSupport.expectXY(350, 250, chart.drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func dataAtTheLargestFloatHasAnAxisOfItsOwn() {
        let axis = ChartScale.axisOf([Float.greatestFiniteMagnitude], 0, 0, false)
        #expect(ChartScale.drawable(axis))
        #expect(axis.min > 1e38 && axis.max == Float.greatestFiniteMagnitude, "\(axis.min) to \(axis.max)")
        let flat = ChartScale.flatRange(5)
        #expect(flat.lo == 5)
        #expect(flat.hi == 6)
    }

    @Test func groupDigitsLeavesWordsAsTheyAre() {
        #expect(ChartScale.groupDigits("Infinity") == "Infinity")
        #expect(ChartScale.groupDigits("-Infinity") == "-Infinity")
        #expect(ChartScale.groupDigits("NaN") == "NaN")
        #expect(ChartScale.groupDigits("3e38") == "3e38")
        #expect(ChartScale.groupDigits("-1234567.25") == "-1,234,567.25")
    }

    @Test func simpleBarChartWritesNoValueThatIsNotANumber() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let chart = SimpleBarChart(font, font).setTitle("Sizes")
                .add("one", 1200).add("two", Float.infinity).add("three", Float.nan)
        #expect(chart.label(Float.infinity) == "")
        #expect(chart.label(Float.nan) == "")
        #expect(chart.getAltDescription() == "Sizes: one 1,200.")
        let page = Page(pdf, Letter.PORTRAIT)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex("fin")))
        #expect(!content.contains(TestSupport.hex("NaN")))
    }

    @Test func stepDigitsGoPastSix() {
        #expect(ChartScale.stepDigits(2.5e-8) == 9)
        #expect(ChartScale.digitsOf(2.5e-8) == 9)
        #expect(ChartScale.digitsOf(0.25) == 2)
        #expect(ChartScale.digitsOf(10) == 2)
        #expect(ChartScale.stepDigits(1.0 / 3.0) == 6)
        #expect(ChartScale.digitsOf(Float.nan) == 2)
    }

    @Test func lineChartCategoriesStayInsideTheChart() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).setLocation(50, 50).setSize(300, 200)
                .setCategories("January 2026", "February 2026", "March 2026 and on")
                .addLine("a", [1, 2, 3]).drawOn(page)
        let content = TestSupport.content(page)
        #expect(TestSupport.positionOf(content, "January 2026")[0] >= 50)
        #expect(endOf(content, font, "March 2026 and on") <= 350)
    }

    @Test func scatterChartNumbersStayInsideTheChart() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).setLocation(50, 50).setSize(400, 200)
                .add(0, 1).add(1000000, 5).drawOn(page)
        let content = TestSupport.content(page)
        #expect(endOf(content, font, "1,000,000") <= 450)
    }

    @Test func barChartCategoriesStayInsideTheChart() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = BarChart(font, font).setLocation(50, 50).setSize(300, 200)
                .setCategories("a", "b", "c", "d", "e", "f", "the last one")
        chart.addSeries("", [1, 2, 3, 4, 5, 6, 7])
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(endOf(content, font, "the last one") <= 350)
    }

    @Test func scatterDescriptionTellsWhatIsDrawn() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        // A trend line that misses the plot is not drawn, nor described
        var chart = SimpleScatterChart(font, font).setTitle("Flat")
                .add(1, 1).add(2, 1).add(3, 1).setTrendLine(true)
        #expect(chart.getAltDescription() == "Flat: 3 points; the trend is flat.")
        chart.setYAxisRange(100, 200)
        #expect(chart.getAltDescription() == "Flat: 0 points.")
        // The points outside a range that is set are not counted
        chart = SimpleScatterChart(font, font).setTitle("Some")
                .add(1, 5).add(2, 6).add("far", 3, 50).setYAxisRange(0, 10)
        #expect(chart.getAltDescription() == "Some: 2 points.")
    }

    @Test func lineChartDescriptionOfOneCategory() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let chart = SimpleLineChart(font, font).setTitle("Sales")
                .setCategories("2019").addLine("a", [4])
        #expect(chart.getAltDescription().hasPrefix("Sales, 2019: a 4."), "\(chart.getAltDescription())")
        chart.setCategories("2019", "2020", "")
        #expect(chart.getAltDescription().hasPrefix("Sales, 2019 to 2020: a 4."), "\(chart.getAltDescription())")
    }

    @Test func lineChartDrawsAValueAloneBetweenGaps() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let joined = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).setLocation(50, 50).setSize(300, 200)
                .addLine("a", [1, 2, 3]).drawOn(joined)
        let alone = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).setLocation(50, 50).setSize(300, 200)
                .addLine("a", [1, Float.nan, 3]).drawOn(alone)
        #expect(count(TestSupport.content(joined), " c\n") == 0)
        #expect(count(TestSupport.content(alone), " c\n") > 0)    // The dots of the two values
    }

    @Test func barChartMakesNoRoomForLabelsItDoesNotWrite() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var at = [Float](repeating: 0, count: 2)
        let values: [[Float]] = [[5, 7], [5, -1000000]]     // The second wholly outside
        for k in 0..<2 {
            let page = Page(pdf, Letter.PORTRAIT)
            let chart = BarChart(font, font).setLocation(50, 50).setSize(300, 200)
                    .setCategories("a", "b").setHorizontal(true).setValueLabels(true).setValueAxisRange(0, 10)
            chart.addSeries("", values[k])
            chart.drawOn(page)
            at[k] = TestSupport.positionOf(TestSupport.content(page), "10")[0]
        }
        #expect(abs(at[0] - at[1]) <= 0.01)
    }
}
