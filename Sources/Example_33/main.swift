/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_33.swift
 * Using the SVGImage component to draw a map of Europe, scaled to fit the page,
 * and a bar chart made with D3, its text in the fonts of the document.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_33 {
    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_33.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_33.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("SVG Images")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        var page = Page(pdf, A4.PORTRAIT)

        let text = TextLine(f2, "SVG Images")
        text.setStructureType(StructElem.H1)
        text.setFontSize(22.0)
        text.setLocation(50.0, 80.0)
        text.drawOn(page)

        var textBlock = TextBlock(f1,
                "SVGImage reads the paths of an SVG file and draws them as vector "
                + "graphics, which stay sharp at any zoom. The map is 865 points wide "
                + "in its file and is scaled by 0.5 to fit the page.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(495.0)
        var xy = textBlock.drawOn(page)

        let map = try loadSVG("images/svg-test/europe.svg")
        map.setAltDescription(
                "A map of Europe drawn from an SVG file, the countries filled in peachpuff and outlined in dim gray, Spain in aliceblue and Austria outlined in light gray.")
        map.scaleBy(0.5)
        // In the middle of the page, and the text under it as wide as it
        let mapX = (page.getWidth() - map.getWidth()) / 2.0
        map.setLocation(mapX, xy[1] + 20.0)
        xy = map.drawOn(page)

        textBlock = TextBlock(f1,
                "The colors are set in the SVG file itself. The svg element gives every "
                + "country a peachpuff fill and a dimgray outline; Spain overrides the fill "
                + "with aliceblue, and Austria the outline with lightgray.")
        textBlock.setFontSize(10.0)
        textBlock.setTextColor(Color.dimgray)
        textBlock.setLocation(mapX, xy[1] + 10.0)
        textBlock.setWidth(map.getWidth())
        xy = textBlock.drawOn(page)

        // A chart made with D3, as a browser draws it, by util/make-d3-chart.mjs:
        // its text, in sans-serif, is drawn in IBM Plex Sans, which PDFjet
        // carries and embeds, as PDF/UA asks; on a page of its own
        page = Page(pdf, A4.PORTRAIT)
        TextLine(f2, "A Chart Made with D3")
                .setStructureType(StructElem.H2)
                .setFontSize(22.0)
                .setLocation(50.0, 80.0)
                .drawOn(page)

        textBlock = TextBlock(f1,
                "SVGImage draws the charts of D3 and the like, their text in the fonts "
                + "PDFjet carries, embedded: IBM Plex Sans for "
                + "sans-serif, Source Serif 4 for serif and Cascadia Mono for monospace. "
                + "This chart was made by D3 in util/make-d3-chart.mjs.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(495.0)
        xy = textBlock.drawOn(page)

        let chart = try loadSVG("images/svg/d3-life-expectancy-by-region.svg")
        chart.setAltDescription("A bar chart of the life expectancy at birth of the World Bank's "
                + "regions in 2023: North America 78.7 years, Europe and Central Asia 78.4, "
                + "East Asia and Pacific 76.7, Latin America and the Caribbean 75.6, South Asia 72.3, "
                + "the Middle East, North Africa, Afghanistan and Pakistan 72.1, "
                + "and Sub-Saharan Africa 62.6; the world 73.3.")
        chart.scaleBy(495.0 / chart.getWidth())
        chart.setLocation(50.0, xy[1] + 20.0)
        chart.drawOn(page)

        try pdf.complete()
    }

    private func loadSVG(_ path: String) throws -> SVGImage {
        guard let image = try SVGImage(fileAtPath: path) else {
            throw ExampleError(message: "Cannot open SVG file: \(path)")
        }
        return image
    }
}   // End of Example_33.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_33()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_33 => \(String(format: "%4lld", time1 - time0)) ms")
