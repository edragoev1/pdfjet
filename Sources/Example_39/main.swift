import Foundation
import PDFjet

/**
 * Example_39.swift
 *
 * Draws a horizontal bar chart of the ten longest rivers, each bar in its own
 * color with its length written inside it, under a title and a subtitle, and
 * a color key and a source note under the chart.
 */
public class Example_39 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_39.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(15.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(9.0)

        let f3 = try Font(pdf, IBMPlexSans.Bold)
        f3.setSize(9.0)

        let f4 = try Font(pdf, IBMPlexSans.Regular)
        f4.setSize(8.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let rivers = [
                "Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
                "Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"]
        let lengths: [Float] = [6650.0, 6400.0, 6300.0, 5971.0, 5540.0, 5464.0, 5410.0, 4880.0, 4700.0, 4444.0]
        let colors: [Int32] = [
                0x5b9bd5, 0x6b8e6b, 0x8b6f47, 0x9b7b5a, 0x6fa8dc,
                0xd4a017, 0x8fa98f, 0xa08060, 0x5c5c5c, 0x8b7355]

        let chart = BarChart(f1, f2)
        chart.setLocation(36.0, 40.0)
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
        chart.setDrawValueLabels(true)
        chart.setValueLabelsInside(true)
        chart.setGroupingUsed(true)
        chart.drawOn(page)

        // The color key under the chart
        let gray: Int32 = 0x444444
        TextLine(f3, "Color key (illustrative):")
                .setTextColor(gray).setLocation(171.0, 466.0).drawOn(page)
        let keyColors: [Int32] = [0x5b9bd5, 0x6b8e6b, 0xa08060, 0xd4a017, 0x5c5c5c]
        let keyTexts = [
                "Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
                "Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"]
        let keyX: [Float] = [171.0, 262.0, 398.0, 171.0, 313.0]
        let keyY: [Float] = [482.0, 482.0, 482.0, 497.0, 497.0]
        for i in 0..<keyColors.count {
            page.setBrushColor(keyColors[i])
            page.fillRect(keyX[i], keyY[i] - 8.5, 10.5, 10.5)
            TextLine(f4, keyTexts[i])
                    .setTextColor(gray).setLocation(keyX[i] + 15.0, keyY[i]).drawOn(page)
        }

        let note = "Color mapping is illustrative; lengths and conditions vary by source and season."
        TextLine(f4, note)
                .setTextColor(0x999999).setLocation(576.0 - f4.stringWidth(note), 520.0).drawOn(page)

        try pdf.complete()
    }
}   // End of Example_39.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_39()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_39 => \(time1 - time0) ms")
