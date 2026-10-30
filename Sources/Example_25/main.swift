/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_25.swift
 *
 * Draws a SimpleLineChart of two lines over the same years: the vehicles of
 * the data file by model year, battery electric highlighted in navy, each
 * line labeled at its end with its last count, so that the chart needs no
 * legend, and a dot at each year so that each count can be read off its line.
 * The second page draws the plug-in hybrids alone as an area chart, the area
 * under the line filled and each count written above its year.
 */
public class Example_25 {
    // The vehicles of the Washington State Electric Vehicle Population Data
    // by model year and kind, summarized in
    // resources/data/Electric_Vehicles_by_Model_Year.csv: the rows of the data
    // whose "Electric Vehicle Type" begins with "Battery Electric" and with
    // "Plug-in Hybrid", counted by the "Model Year" column. The data holds
    // 124,716 rows, of which 124,419 are registered in Washington State, and
    // model years from 1997; the years before 2011 are a few dozen vehicles
    // in all and are left out here.
    private let firstYear = 2011

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_25.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicles by Model Year")

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        let f2 = try Font(pdf, IBMPlexSans.Regular)

        var page = Page(pdf, Letter.PORTRAIT)

        let title = TextLine(f1, "Electric Vehicles by Model Year")
        title.setStructureType(StructElem.H1)
        title.setFontSize(18.0)
        title.setLocation(50.0, 50.0)
        title.drawOn(page)

        let textBlock = TextBlock(f2,
                "A SimpleLineChart draws a few lines over the same categories, here " +
                "the model years, in their order left to right. Each line is labeled " +
                "at its end with its name and its last value, so the chart needs no " +
                "legend, and one line is highlighted in navy, the other gray. The dots " +
                "of setMarkers show the count for each model year. In a PDF/UA " +
                "document the chart describes itself from its data: each line's first " +
                "and last values and its highest.\n\n" +
                "The counts are of the Washington State Electric Vehicle Population " +
                "Data, summarized by model year and by kind in " +
                "resources/data/Electric_Vehicles_by_Model_Year.csv.")
        textBlock.setFontSize(11.0)
        textBlock.setLineSpacing(1.4)
        textBlock.setLocation(50.0, 72.0)
        textBlock.setWidth(512.0)
        let xy = textBlock.drawOn(page)

        f1.setSize(12.0)
        f2.setSize(9.0)

        // The model years from firstYear on, after the header line
        var years = [String]()
        var battery = [Float]()
        var hybrid = [Float]()
        let lines = try Content.linesOfTextFile("resources/data/Electric_Vehicles_by_Model_Year.csv")
        for line in lines[1...] {
            let field = line.components(separatedBy: ",")
            if (Int(field[0]) ?? 0) >= firstYear {
                years.append(field[0])
                battery.append(Float(field[1]) ?? 0)
                hybrid.append(Float(field[2]) ?? 0)
            }
        }

        SimpleLineChart(f1, f2)
                .setTitle("Battery electric and plug-in hybrid vehicles")
                .setSubtitle("By model year, in the Washington State data file")
                .setYAxisTitle("Vehicles")
                .setCategories(years)
                .addLine("Battery electric", battery)
                .addLine("Plug-in hybrid", hybrid)
                .setHighlight(0)
                .setMarkers(true)
                .setLocation(50.0, xy[1] + 30.0)
                .setSize(512.0, 340.0)
                .drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(f1, f2)
                .setTitle("Plug-in hybrid vehicles")
                .setSubtitle("By model year, the area under the line filled and each count written")
                .setYAxisTitle("Vehicles")
                .setCategories(years)
                .addLine("Plug-in hybrid", hybrid)
                .setHighlight(0)
                .setMarkers(true)
                .setAreaFilled(true)
                .setValueLabels(true)
                .setLocation(50.0, 50.0)
                .setSize(512.0, 300.0)
                .drawOn(page)

        try pdf.complete()
    }
}   // End of Example_25.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_25()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_25 => \(String(format: "%4lld", time1 - time0)) ms")
