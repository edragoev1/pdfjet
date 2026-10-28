/**
 * BarChartReviewTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The findings of the review of BarChart, DonutChart and ChartScale, of 28
/// October 2026.
@Suite struct BarChartReviewTests {
    private func chart(_ pdf: PDF) -> BarChart {
        let font = TestSupport.helvetica(pdf)
        return BarChart(font, font).setLocation(50, 50).setSize(300, 200)
    }

    private func draw(_ chart: BarChart, _ page: Page) -> String {
        TestSupport.expectXY(350, 250, chart.drawOn(page))
        return TestSupport.content(page)
    }

    /// Returns how many times the text is drawn in a core font.
    private func count(_ content: String, _ text: String) -> Int {
        return content.components(separatedBy: "<" + TestSupport.hex(text) + ">").count - 1
    }

    // The total of a stack below a range that is set is not written: it was
    // written under the plot, or left of it by horizontal bars.
    @Test func aTotalBelowARangeThatIsSetIsNotWritten() {
        let pdf = TestSupport.newPDF()
        for horizontal in [false, true] {
            let page = Page(pdf, Letter.PORTRAIT)
            let chart = chart(pdf).setCategories("A", "B").setStacked(true).setValueLabels(true)
                    .setHorizontal(horizontal).setValueAxisRange(50, 100)
                    .addSeries("x", [10, 30]).addSeries("y", [10, 30])
            let content = draw(chart, page)
            #expect(count(content, "20") == 0, "\(content)")
            #expect(count(content, "30") == 1, "\(content)")     // B's second segment
        }
    }

    // Values under 0.01 are written with the digits of the step of the axis,
    // and described with theirs; the values that had their digits keep them.
    @Test func smallValuesAreNotWrittenAsZero() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        var page = Page(memory.pdf, Letter.PORTRAIT)
        var chart = chart(memory.pdf).setTitle("Rates").setCategories("A", "B").setValueLabels(true)
                .addSeries("s", [0.00012, 0.00034])
        var content = draw(chart, page)
        #expect(count(content, "0.00012") == 1, "\(content)")
        #expect(count(content, "0.00034") == 1, "\(content)")
        #expect(page.structures[0].altDescription == "Rates: s: A 0.00012, B 0.00034.")

        page = Page(memory.pdf, Letter.PORTRAIT)
        chart = self.chart(memory.pdf).setCategories("a", "b", "c").setValueLabels(true)
                .addSeries("", [12.5, 1234, 0.012])
        content = draw(chart, page)
        for label in ["12.5", "1,234", "0.01"] {
            #expect(count(content, label) == 1, "\(label)")
        }
        #expect(page.structures[0].altDescription == "Bar chart: a 12.5, b 1,234, c 0.012.")
    }

    // A label wider than its bar is not written inside it: a stacked one is
    // left out, and one inside a grouped bar is written past its end. The
    // labels past the ends of narrow bars, and the totals over narrow stacks,
    // are written where they leave the one before them room.
    @Test func labelsKeepToTheWidthOfTheirBars() {
        let pdf = TestSupport.newPDF()
        let twelve = [Float](repeating: 12345, count: 12)
        var page = Page(pdf, Letter.PORTRAIT)
        var content = draw(chart(pdf).setStacked(true).setValueLabels(true)
                .addSeries("", twelve).addSeries("", twelve), page)
        #expect(count(content, "12,345") == 0, "\(content)")
        let totals = count(content, "24,690")
        #expect(totals > 0 && totals < 12, "\(totals) totals of 12")

        page = Page(pdf, Letter.PORTRAIT)
        content = draw(chart(pdf).setValueLabels(true).setValueLabelsInside(true).addSeries("", twelve), page)
        #expect(!content.contains("1 1 1 rg"), "\(content)")
        let labels = count(content, "12,345")
        #expect(labels > 0 && labels < 12, "\(labels) labels of 12")
    }

    // By horizontal bars a category is written where it leaves the one before
    // it room: forty of them ran into each other.
    @Test func horizontalCategoriesThatDoNotFitAreSkipped() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let names = (0..<40).map { "Category \($0)" }
        let values = (0..<40).map { Float($0 + 1) }
        let content = draw(chart(pdf).setHorizontal(true).setCategories(names).addSeries("", values), page)
        #expect(count(content, "Category 0") == 1, "\(content)")
        #expect(count(content, "Category 1") == 0, "\(content)")
    }

    // The numbers of an axis written with a power of ten have the significant
    // digits that write them apart.
    @Test func largeNumbersOfAnAxisAreApart() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let content = draw(chart(pdf).setCategories("a").setValueAxisRange(4.84e15, 4.85e15)
                .addSeries("", [4.845e15]), page)
        #expect(count(content, "4.84e15") == 1, "\(content)")
        #expect(count(content, "4.841e15") == 1, "\(content)")
        #expect(ChartScale.format(4.841e15, 0, 0) == "4.84e15")
    }

    // A percentage is written inside its slice only when the slice is as long
    // across the middle of the ring as the percentage is wide.
    @Test func aPercentageWiderThanItsSliceIsNotWritten() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        let chart = DonutChart(font, font).setLocation(100, 100).setRadii(20, 0).add("a", 1).add("b", 17)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(count(content, "6%") == 0, "\(content)")
        #expect(count(content, "94%") == 1, "\(content)")
    }

    // A NaN is kept by the smaller and the larger of two numbers, as Go's min
    // and max keep it: Swift.min and Swift.max dropped one that was not first.
    @Test func aNaNIsKeptAsGoKeepsIt() {
        #expect(ChartScale.flatDataSpan(Float.nan).isNaN)
        #expect(!ChartScale.spanFits([1, Float.nan]))
        #expect(ChartScale.held(Float.nan, ChartScale.Axis(min: 0, max: 10, lines: 5)).isNaN)
        #expect(ChartScale.placed(5, 2, 0, Float.nan).isNaN)
    }
}
