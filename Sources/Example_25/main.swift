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
 */
public class Example_25 {
    // The vehicles of data/Electric_Vehicle_Population_Data.csv by model year
    // and kind, which Example_24 draws as a table: the rows of the file whose
    // "Electric Vehicle Type" begins with "Battery Electric" and with
    // "Plug-in Hybrid", counted by the "Model Year" column. The file holds
    // 124,716 rows, of which 124,419 are registered in Washington State, and
    // model years from 1997; the years before 2011 are a few dozen vehicles
    // in all and are left out here.
    private let years =
            [2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023]
    private let battery: [Float] =
            [ 753,  798, 2936, 1805, 3612, 3891, 4458, 9946, 8576, 9315, 14755, 23507, 11853]
    private let hybrid: [Float] =
            [  75,  870, 1645, 1804, 1323, 1811, 4100, 4278, 1874, 1611,  3541,  4015,  1500]

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_25.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicles by Model Year")

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        let f2 = try Font(pdf, IBMPlexSans.Regular)

        let page = Page(pdf, Letter.PORTRAIT)

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
                "The counts are the vehicles of " +
                "data/Electric_Vehicle_Population_Data.csv, the file Example_24 draws " +
                "as a table of 2,546 pages, by model year and by kind.")
        textBlock.setFontSize(11.0)
        textBlock.setLineSpacing(1.4)
        textBlock.setLocation(50.0, 72.0)
        textBlock.setWidth(512.0)
        let xy = textBlock.drawOn(page)

        f1.setSize(12.0)
        f2.setSize(9.0)

        SimpleLineChart(f1, f2)
                .setTitle("Battery electric and plug-in hybrid vehicles")
                .setSubtitle("By model year, in the Washington State data file")
                .setYAxisTitle("Vehicles")
                .setCategories(years.map { String($0) })
                .addLine("Battery electric", battery)
                .addLine("Plug-in hybrid", hybrid)
                .setHighlight(0)
                .setMarkers(true)
                .setLocation(50.0, xy[1] + 30.0)
                .setSize(512.0, 340.0)
                .drawOn(page)

        try pdf.complete()
    }
}   // End of Example_25.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_25()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_25 => \(String(format: "%4lld", time1 - time0)) ms")
