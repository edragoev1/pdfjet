/**
 * DonutChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
        let chart = chart(pdf).addSlice(Slice(0, Color.red, "none"))
        TestSupport.expectXY(300, 300, chart.drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func slicesArePercentagesOfTheSumOfTheValues() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSlice(Slice(1, Color.red, "a"))
        chart.addSlice(Slice(1, Color.green, "b"))
        chart.addSlice(Slice(2, Color.blue, "c"))
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
        chart.addSlice(Slice(3, Color.red, "a")).addSlice(Slice(1, Color.blue, "b"))
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("75%")), "\(content)")
        #expect(content.contains("200 592 l"), "\(content)")   // the center, in PDF coordinates
    }

    @Test func aChartIsAFigureThatListsItsSlices() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let donut = chart(memory.pdf).addSlice(Slice(25, Color.red, "Apples"))
                .addSlice(Slice(75, Color.blue, "Oranges"))
        donut.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\n"), "\(content)")
        #expect(content.hasSuffix("EMC\n"), "\(content)")
        #expect(TestSupport.fillColorBefore(content, "Apples") == "0.13 0.15 0.17 rg")
        #expect(TestSupport.fillColorBefore(content, "25%") == "1 1 1 rg")
        chart(memory.pdf).setRadii(100, 0).addSlice(Slice(1, Color.red, "")).drawOn(page)
        chart(memory.pdf).setAltDescription("Most are oranges.")
                .addSlice(Slice(1, Color.red, "Apples")).drawOn(page)
        #expect(page.structures[0].altDescription == "Donut chart: Apples 25%, Oranges 75%")
        #expect(page.structures[1].altDescription == "Pie chart: 100%")
        #expect(page.structures[2].altDescription == "Most are oranges.")
    }

    // Slices in the palette, navy first; thin white lines between them; thin
    // gray leader lines; the percentage white on navy, ink on the light steel
    // blue; and no black line or text.
    @Test func theDefaultLookIsQuiet() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).addSlice(Slice(60, "a")).addSlice(Slice(40, "b")).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("0.12 0.23 0.37 rg"), "\(content)")       // navy
        #expect(content.contains("0.44 0.58 0.76 rg"), "\(content)")       // steel blue
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
        TestSupport.expectXY(300, 315, chart(memory.pdf).addSlice(Slice(1, "a")).drawOn(page))
        let xy = chart(memory.pdf).setTitle("Fruit").setSubtitle("This week")
                .addSlice(Slice(1, "a")).drawOn(page)
        #expect(abs(xy[0] - 300) < 0.01)
        #expect(xy[1] > 315, "the circle is under the title: \(xy[1])")
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("Fruit")) && content.contains(TestSupport.hex("This week")))
        #expect(TestSupport.fillColorBefore(content, "This week") == "0.37 0.41 0.45 rg")
        #expect(page.structures[1].altDescription == "Fruit, This week: a 100%")
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
            chart.addSlice(Slice(bad, Color.red, "bad"))
            chart.addSlice(Slice(1, Color.green, "a"))
            chart.addSlice(Slice(3, Color.blue, "b"))
            chart.drawOn(page)
            let content = TestSupport.content(page)
            #expect(content.contains(TestSupport.hex("25%")) && content.contains(TestSupport.hex("75%")),
                    "\(bad): the other slices are not 25% and 75% in \(content)")
        }
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).addSlice(Slice(3e38, "a")).addSlice(Slice(3e38, "b")).drawOn(page)
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
            chart.addSlice(Slice(value, 0x123456, ""))
        }
        chart.setLocation(88.48386, 140.68524).drawOn(page)
        #expect(TestSupport.content(page).contains("115.28 498.52 116.96 464.15 130.52 433.78 c\n"))

        page = Page(pdf, Letter.PORTRAIT)
        chart = DonutChart(nil, nil).setRadii(59.125828, 19.265127)
        for value: Float in [363.0059, 966.9588, 317.46417, 795.0797, 18.464327] {
            chart.addSlice(Slice(value, 0x123456, ""))
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
        let chart = chart(memory.pdf).addSlice(Slice(90, "Big"))
        for i in 0..<6 {
            chart.addSlice(Slice(1.5, "Small \(i)"))
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
}
