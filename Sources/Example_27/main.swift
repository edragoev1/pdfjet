/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_27.swift
 *
 * Draws bar charts. The first page is a horizontal bar chart of the ten
 * longest rivers, each bar in its own color with its length inside it, and a
 * color key under the chart. The second page draws two charts with vertical
 * bars from the same data: the two series grouped by month, and the same
 * series stacked. The third, the units of 2026 with a line over the bars for
 * the target of each month.
 */
public class Example_27 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_27.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Bar Charts")

        try drawHorizontal(pdf)
        try drawVertical(pdf)
        try drawTarget(pdf)

        try pdf.complete()
    }

    private func drawHorizontal(_ pdf: PDF) throws {
        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(15.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(9.0)

        let f3 = try Font(pdf, IBMPlexSans.Bold)
        f3.setSize(9.0)

        let f4 = try Font(pdf, IBMPlexSans.Regular)
        f4.setSize(8.0)

        let page = Page(pdf, Letter.PORTRAIT)

        // The heading of the page, above the chart
        TextLine(f1, "Rivers of the World")
                .setStructureType(StructElem.H1)
                .setFontSize(18.0)
                .setLocation(36.0, 40.0)
                .drawOn(page)

        let rivers = [
                "Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
                "Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"]
        let lengths: [Float] = [6650.0, 6400.0, 6300.0, 5971.0, 5540.0, 5464.0, 5410.0, 4880.0, 4700.0, 4444.0]
        let colors: [Int32] = [
                0x477aa8, 0x5f7e5f, 0x8b6f47, 0x8e7152, 0x517aa0,
                0x967110, 0x687b68, 0x8e7155, 0x5c5c5c, 0x897154]

        let chart = BarChart(f1, f2)
        chart.setLocation(36.0, 70.0)
        chart.setSize(540.0, 400.0)
        chart.setTitle("10 Longest Rivers in the World")
        chart.setSubtitle("Length in kilometers · Color reflects typical sediment / pollution character")
        chart.setCategories(rivers)
        chart.addSeries("", lengths, colors)
        chart.setHorizontal(true)
        chart.setGroupGap(0.4)
        chart.setValueAxisMinMax(0.0, 8000.0, 4)
        chart.setGridLineWidth(0.75)
        chart.setGridLineColor(0xe0e0e0)
        chart.setGridLineDashPattern("[] 0")
        chart.setAxisLineWidth(0.0)
        chart.setValueLabels(true)
        chart.setValueLabelsInside(true)
        chart.setGroupingUsed(true)
        chart.setAltDescription(
                "The ten longest rivers in the world, in kilometers: Nile 6,650, Amazon 6,400, Yangtze 6,300, Mississippi-Missouri 5,971, Yenisey-Baikal-Selenga 5,540, Huang He (Yellow) 5,464, Ob-Irtysh 5,410, Paraná 4,880, Congo 4,700, Amur 4,444.")
        chart.drawOn(page)

        // The color key under the chart
        let gray: Int32 = 0x444444
        TextLine(f3, "Color key (illustrative):")
                .setTextColor(gray).setLocation(171.0, 496.0).drawOn(page)
        let keyColors: [Int32] = [0x477aa8, 0x5f7e5f, 0x8e7155, 0x967110, 0x5c5c5c]
        let keyTexts = [
                "Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
                "Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"]
        // Three on the first row and two on the second, each after the one
        // before it by the width of its text
        var x: Float = 171.0
        var y: Float = 512.0
        for i in 0..<keyColors.count {
            if i == 3 {
                x = 171.0
                y = 527.0
            }
            // The swatches are rectangles, drawn as artifacts next to their text.
            Rect(x, y - 8.5, 10.5, 10.5).setFillColor(keyColors[i]).drawOn(page)
            TextLine(f4, keyTexts[i])
                    .setTextColor(gray).setLocation(x + 15.0, y).drawOn(page)
            x += 15.0 + f4.stringWidth(keyTexts[i]) + 12.0
        }

        let note = "Color mapping is illustrative; lengths and conditions vary by source and season."
        TextLine(f4, note)
                .setTextColor(0x696969).setLocation(576.0 - f4.stringWidth(note), 550.0).drawOn(page)

        // The data of the chart in a table under it
        let lengthTexts = [
                "6,650", "6,400", "6,300", "5,971", "5,540", "5,464", "5,410", "4,880", "4,700", "4,444"]
        let characters = [
                keyTexts[0], keyTexts[1], keyTexts[2], keyTexts[2], keyTexts[0],
                keyTexts[3], keyTexts[1], keyTexts[2], keyTexts[4], keyTexts[2]]
        var rows = [[Cell]]()
        rows.append([
                Cell(f3, "River").setWidth(180.0),
                Cell(f3, "Length, km").setWidth(90.0).setTextAlignment(Alignment.RIGHT),
                Cell(f3, "Character").setWidth(270.0)])
        for i in 0..<rivers.count {
            rows.append([
                    Cell(f2, rivers[i]).setWidth(180.0),
                    Cell(f2, lengthTexts[i]).setWidth(90.0).setTextAlignment(Alignment.RIGHT),
                    Cell(f2, characters[i]).setWidth(270.0)])
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(f3, 0x000000, 0xEEF0F3)
        table.setCellBorderColor(0xC8CCD2)
        table.setLocation(36.0, 572.0)
        table.drawOn(page)
    }

    private func drawVertical(_ pdf: PDF) throws {
        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(8.0)

        // The heading of the document, above the charts
        TextLine(f1, "Sales in 2025 and 2026")
                .setStructureType(StructElem.H1)
                .setFontSize(18.0)
                .setLocation(70.0, 45.0)
                .drawOn(page)

        let months = [
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
        let units2025: [Float] = [45.0, 65.0, 31.0, 45.0, 65.0, 31.0, 38.0, 52.0, 47.0, 59.0, 66.0, 72.0]
        let units2026: [Float] = [75.0, 20.0, 73.0, 75.0, 20.0, 73.0, 61.0, 58.0, 69.0, 64.0, 77.0, 80.0]

        let chart = BarChart(f1, f2)
        chart.setLocation(70.0, 70.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("Units sold by month")
        chart.setXAxisTitle("Month")
        chart.setYAxisTitle("Units")
        chart.setCategories(months)
        chart.addSeries("2025", units2025)
        chart.addSeries("2026", units2026)
        chart.setGroupGap(0.4)
        chart.setBarGap(0.1)
        chart.setAltDescription("Units sold by month in 2025 and 2026, side by side: from 31 to 72 a month in 2025 and from 20 to 80 in 2026, the most in December in both years.")
        chart.drawOn(page)

        let stacked = BarChart(f1, f2)
        stacked.setLocation(70.0, 420.0)
        stacked.setSize(500.0, 300.0)
        stacked.setTitle("Units sold by month, stacked")
        stacked.setXAxisTitle("Month")
        stacked.setYAxisTitle("Units")
        stacked.setCategories(months)
        stacked.addSeries("2025", units2025)
        stacked.addSeries("2026", units2026)
        stacked.setStacked(true)
        stacked.setValueLabels(true)
        stacked.setAltDescription("Units sold by month in 2025 and 2026, stacked: from 85 a month, in February and May, to 152 in December.")
        stacked.drawOn(page)
    }

    private func drawTarget(_ pdf: PDF) throws {
        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(8.0)

        let months = [
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
        let units2026: [Float] = [75.0, 20.0, 73.0, 75.0, 20.0, 73.0, 61.0, 58.0, 69.0, 64.0, 77.0, 80.0]
        let target: [Float] = [60.0, 60.0, 62.0, 64.0, 66.0, 68.0, 68.0, 68.0, 70.0, 72.0, 75.0, 80.0]

        // The description of the chart is made from its data
        let chart = BarChart(f1, f2)
        chart.setLocation(70.0, 70.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("Units sold by month in 2026, and the target")
        chart.setXAxisTitle("Month")
        chart.setYAxisTitle("Units")
        chart.setCategories(months)
        chart.addSeries("2026", units2026)
        chart.addLineSeries("Target", target)
        chart.drawOn(page)
    }

}   // End of Example_27.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_27()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_27 => \(String(format: "%4lld", time1 - time0)) ms")
