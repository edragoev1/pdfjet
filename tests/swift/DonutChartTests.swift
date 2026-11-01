/**
 * DonutChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct DonutChartTests {
    private func chart(_ pdf: PDF) -> DonutChart {
        let font = TestSupport.helvetica(pdf)
        return DonutChart(font, font).setLocation(100, 100).setRadii(100, 50)
    }

    @Test func aChartWithoutValuesDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).add("none", 0, Color.red)
        TestSupport.expectXY(300, 300, chart.drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func slicesArePercentagesOfTheSumOfTheValues() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.add("a", 1, Color.red)
        chart.add("b", 1, Color.green)
        chart.add("c", 2, Color.blue)
        TestSupport.expectXY(300, 300, chart.drawOn(page))
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("25%")), "\(content)")
        #expect(content.contains(TestSupport.hex("50%")), "\(content)")
        #expect(!content.contains(TestSupport.hex("100%")), "\(content)")
    }

    @Test func aPieChartHasAnInnerRadiusOfZero() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setRadii(100, 0)
        chart.add("a", 3, Color.red).add("b", 1, Color.blue)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("75%")), "\(content)")
        #expect(content.contains("200 592 l"), "\(content)")   // the center, in PDF coordinates
    }

    @Test func aChartIsAFigureThatListsItsSlices() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let donut = chart(memory.pdf).add("Apples", 25, Color.red)
                .add("Oranges", 75, Color.blue)
        donut.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\n"), "\(content)")
        #expect(content.hasSuffix("EMC\n"), "\(content)")
        #expect(TestSupport.fillColorBefore(content, "Apples") == "0.13 0.15 0.17 rg")
        #expect(TestSupport.fillColorBefore(content, "25%") == "0.07 0.07 0.07 rg")    // On red, which white contrasts 4:1
        chart(memory.pdf).setRadii(100, 0).add("", 1, Color.red).drawOn(page)
        chart(memory.pdf).setAltDescription("Most are oranges.")
                .add("Apples", 1, Color.red).drawOn(page)
        #expect(page.structures[0].altDescription == "Donut chart: Apples 25%, Oranges 75%")
        #expect(page.structures[1].altDescription == "Pie chart: 100%")
        #expect(page.structures[2].altDescription == "Most are oranges.")
    }

    // Slices in the palette, blue first; thin white lines between them; thin
    // gray leader lines; the percentage white on blue, ink on orange; and no
    // black line or text.
    @Test func theDefaultLookIsQuiet() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).add("a", 60).add("b", 40).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("0.31 0.47 0.65 rg"), "\(content)")       // blue
        #expect(content.contains("0.95 0.56 0.17 rg"), "\(content)")       // orange
        #expect(content.contains("1 1 1 RG\n1.5 w\n"), "\(content)")       // the white lines between
        #expect(content.contains("0.55 0.59 0.64 RG\n0.75 w\n"), "\(content)")   // the leader lines
        #expect(!content.contains("0 0 0 RG") && !content.contains("0 0 0 rg"), "\(content)")
        #expect(TestSupport.fillColorBefore(content, "60%") == "1 1 1 rg")
        #expect(TestSupport.fillColorBefore(content, "40%") == "0.13 0.15 0.17 rg")
    }

    // The title at the location, the subtitle under it, the circle under
    // both; a chart without them has the circle at its location, as before.
    @Test func aTitleIsDrawnAboveTheCircle() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        // The circle, and its label 15 under it
        TestSupport.expectXY(300, 315, chart(memory.pdf).add("a", 1).drawOn(page))
        let xy = chart(memory.pdf).setTitle("Fruit").setSubtitle("This week")
                .add("a", 1).drawOn(page)
        #expect(abs(xy[0] - 300) < 0.01)
        #expect(xy[1] > 315, "the circle is under the title: \(xy[1])")
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("Fruit")) && content.contains(TestSupport.hex("This week")))
        #expect(TestSupport.fillColorBefore(content, "This week") == "0.37 0.41 0.45 rg")
        #expect(page.structures[1].altDescription == "Fruit. This week: a 100%")
    }
    // A slice that is not a number or an infinity is not drawn, and the
    // others are: one made the arc's loop endless (review of 11 October 2026,
    // #2). A value whose 360ths are past the largest Float is drawn, as it
    // compares with the others (review of 26 October 2026).
    @Test func aSliceThatIsNotANumberIsNotDrawn() {
        for bad: Float in [Float.nan, Float.infinity] {
            let pdf = TestSupport.newPDF()
            let page = Page(pdf, Letter.PORTRAIT)
            let chart = chart(pdf)
            chart.add("bad", bad, Color.red)
            chart.add("a", 1, Color.green)
            chart.add("b", 3, Color.blue)
            chart.drawOn(page)
            let content = TestSupport.content(page)
            #expect(content.contains(TestSupport.hex("25%")) && content.contains(TestSupport.hex("75%")),
                    "\(bad): the other slices are not 25% and 75% in \(content)")
        }
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).add("a", 3e38).add("b", 3e38).drawOn(page)
        #expect(TestSupport.content(page).contains(TestSupport.hex("50%")), "\(TestSupport.content(page))")
    }

    // A slice is computed with the precision of Go's port, float or double
    // step by step, so the ports draw the same curves: the cosine and the
    // sine in float drew 21 of 300 random charts a hundredth apart (review of
    // 11 October 2026, #25). The expected curves are Go's.
    @Test func aSliceIsDrawnAsTheGoPortDrawsIt() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        var chart = DonutChart(nil, nil).setRadii(163.36272, 132.87044)
        for value: Float in [736.5481, 275.45578, 860.18787, 464.1474] {
            chart.add("", value, 0x123456)
        }
        chart.setLocation(88.48386, 140.68524).drawOn(page)
        #expect(TestSupport.content(page).contains("115.28 498.52 116.96 464.15 130.52 433.78 c\n"))

        page = Page(pdf, Letter.PORTRAIT)
        chart = DonutChart(nil, nil).setRadii(59.125828, 19.265127)
        for value: Float in [363.0059, 966.9588, 317.46417, 795.0797, 18.464327] {
            chart.add("", value, 0x123456)
        }
        chart.setLocation(262.18466, 51.85658).drawOn(page)
        #expect(TestSupport.content(page).contains("321.01 700.28 320.71 700.28 320.4 700.26 c\n"))
    }

    // The labels of thin slices side by side are moved apart, a line apart at
    // least; the percentages add up to 100; the white line by a thin slice is
    // narrower than it; and drawOn returns the bottom right corner of the
    // labels too, measured as drawn (review of 26 October 2026).
    @Test func thinSlicesKeepTheirLabelsApart() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let chart = chart(memory.pdf).add("Big", 90)
        for i in 0..<6 {
            chart.add("Small \(i)", 1.5)
        }
        let measured = chart.drawOn(nil)
        let xy = chart.drawOn(page)
        TestSupport.expectXY(measured[0], measured[1], xy)
        let content = TestSupport.content(page)
        let font = TestSupport.helvetica(memory.pdf)
        for i in 1..<6 {
            let above = TestSupport.positionOf(content, "Small \(i - 1)")[1]
            let below = TestSupport.positionOf(content, "Small \(i)")[1]
            #expect(abs(above - below) >= font.getBodyHeight() - 0.01, "\(above) \(below)")
        }
        #expect(page.structures[0].altDescription
                == "Donut chart: Big 91%, Small 0 2%, Small 1 2%, Small 2 2%, Small 3 1%, Small 4 1%, Small 5 1%")
        #expect(content.contains("1.5 w\n"), "\(content)")                        // By the big slice
        #expect(content.range(of: "\n0\\.\\d+ w\n", options: .regularExpression) != nil,
                "\(content)")                                                      // Between two thin ones
    }

    // With a legend the chart has no leader lines: each slice's label and
    // percentage is in rows under the circle, which the returned corner
    // holds. The text in the hole is drawn and described; a pie chart has no
    // hole for it.
    @Test func aLegendAndATextInTheHole() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        var page = Page(memory.pdf, Letter.PORTRAIT)
        let chart = chart(memory.pdf).setLegend(true).setCenterText("100 crates")
                .add("a", 60).add("b", 40)
        let xy = chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains("0.55 0.59 0.64 RG"), "\(content)")      // No leader lines
        #expect(content.contains(TestSupport.hex("a 60%")) && content.contains(TestSupport.hex("b 40%")), "\(content)")
        #expect(content.contains(TestSupport.hex("100 crates")), "\(content)")
        #expect(xy[1] > 300 + 15, "the legend is under the circle: \(xy[1])")
        TestSupport.expectXY(xy[0], xy[1], chart.drawOn(nil))
        #expect(page.structures[0].altDescription == "100 crates: a 60%, b 40%")
        page = Page(memory.pdf, Letter.PORTRAIT)
        self.chart(memory.pdf).setRadii(100, 0).setCenterText("none").add("a", 1).drawOn(page)
        #expect(!TestSupport.content(page).contains(TestSupport.hex("none")))
    }

    // The review of 26 October 2026, the second: labels moved apart stay
    // outside the ring; long labels, legend items and texts in the hole are
    // shortened; the caller's dash pattern does not dash the lines; an inner
    // radius past the outer one is the outer one; and the last slice, by the
    // first around the circle, is not in the first's color.
    @Test func theSecondReviewOfTheCharts() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var page = Page(pdf, Letter.PORTRAIT)
        let thin = chart(pdf).add("Big", 90)    // At (100, 100), radii 100 and 50
        for i in 0..<8 {
            thin.add("Small \(i)", 1.2)
        }
        thin.drawOn(page)
        var content = TestSupport.content(page)
        for i in 0..<8 {
            let at = TestSupport.positionOf(content, "Small \(i)")   // In PDF space, y up
            let dy = (792 - at[1]) - 200
            let ring: Float = abs(dy) < 100 ? (100 * 100 - dy * dy).squareRoot() : 0
            #expect(at[0] + font.stringWidth("Small \(i)") <= 200 - ring + 0.01,
                    "Small \(i) is on the ring: \(at[0]), \(at[1])")
        }

        page = Page(pdf, Letter.PORTRAIT)
        page.setStrokeDashPattern("[3 3] 0")
        let name = "A label much too long to be drawn whole beside the circle"
        chart(pdf).setCenterText("A text in the hole far too long to be written in it at any size")
                .add(name, 60).add("b", 40).drawOn(page)
        content = TestSupport.content(page)
        // The chart's own solid lines after the caller's dashes
        let dash = content.range(of: "[] 0 d\n")
        let dashed = content.range(of: "[3 3] 0 d\n")
        #expect(dash != nil && dashed != nil && dash!.lowerBound > dashed!.lowerBound, "\(content)")
        #expect(!content.contains(TestSupport.hex(name)), "\(content)")
        #expect(content.contains(" 9 Tf\n"), "\(content)")    // The text in the hole at 9, three quarters of 12
        page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setLegend(true).add(name, 60).add("b", 40).drawOn(page)
        content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex(name)), "\(content)")
        #expect(content.contains(TestSupport.hex(" 60%")), "\(content)")   // The percentage kept

        page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).add("a", 1).drawOn(page)
        let outer = TestSupport.content(page)
        page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setRadii(50, 100).add("a", 1).drawOn(page)
        // The same but for the names of the fonts, a new one for each chart
        #expect(outer.replacingOccurrences(of: "/F\\d+ ", with: "/F ", options: .regularExpression)
                == TestSupport.content(page).replacingOccurrences(of: "/F\\d+ ", with: "/F ", options: .regularExpression))

        page = Page(pdf, Letter.PORTRAIT)
        let eleven = chart(pdf)
        for _ in 0..<11 {
            eleven.add("", 1)
        }
        eleven.drawOn(page)
        content = TestSupport.content(page)
        #expect(content.components(separatedBy: "0.31 0.47 0.65 rg").count <= 2, "\(content)")
    }

    // The third review of 26 October 2026: a radius that is not a number is
    // 0, in the four ports alike; and the percentages are not written in a
    // ring too thin for them.
    @Test func theThirdReviewOfTheCharts() {
        let pdf = TestSupport.newPDF()
        func fonts(_ content: String) -> String {
            return content.replacingOccurrences(of: "/F\\d+ ", with: "/F ", options: .regularExpression)
        }
        var page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setRadii(100, Float.nan).add("a", 1).add("b", 3).drawOn(page)
        let nan = TestSupport.content(page)
        page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setRadii(100, 0).add("a", 1).add("b", 3).drawOn(page)
        #expect(fonts(TestSupport.content(page)) == fonts(nan))
        page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setRadii(200, 196).add("a", 1).add("b", 3).drawOn(page)
        #expect(!TestSupport.content(page).contains(TestSupport.hex("75%")), "\(TestSupport.content(page))")
    }
}
