/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_24.swift
 */
public class Example_24 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_24.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("JPEG, PNG and BMP Images")
        let font = try Font(pdf, IBMPlexSans.Regular)

        let image1 = try Image(pdf, "images/gr-map.jpg")
        image1.setAltDescription(
                "A map of Greece with its cities, roads and airports, the Ionian Sea to the west, the Aegean Sea to the east and Crete to the south.")
        let image2 = try Image(pdf, "images/ee-map.png")
        image2.setAltDescription(
                "A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.")
        let image3 = try Image(pdf, "images/rgb24pal.bmp")
        image3.setAltDescription(
                "The letters BMP in white over bars of red, green, blue, yellow, magenta and cyan.")
        let image4 = try Image(pdf, "images/cmyk.jpg")
        image4.setAltDescription(
                "A CMYK test chart: rows of cyan, magenta, yellow and black from 0 to 100 percent in steps of 10, and bars of red, green, blue and rich black.")

        var page = Page(pdf, Letter.PORTRAIT)
        let textLine1 = TextLine(font, "This is a JPEG image.")
        textLine1.setTextRotation(0)
        textLine1.setLocation(50.0, 50.0)
        var point = textLine1.drawOn(page)
        image1.setLocation(50.0, point[1] + 5.0).scaleBy(0.25).drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        let textLine2 = TextLine(font, "This is a PNG image.")
        textLine2.setTextRotation(0)
        textLine2.setLocation(50.0, 50.0)
        point = textLine2.drawOn(page)
        image2.setLocation(50.0, point[1] + 5.0).scaleBy(0.75).drawOn(page)

        let textLine3 = TextLine(font, "This is a BMP image.")
        textLine3.setTextRotation(0)
        textLine3.setLocation(50.0, 620.0)
        point = textLine3.drawOn(page)
        image3.setLocation(50.0, point[1] + 5.0).scaleBy(0.75).drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        let textLine4 = TextLine(font, "This is a CMYK JPEG image, with its inks stored inverted, as Photoshop saves them.")
        textLine4.setLocation(50.0, 50.0)
        point = textLine4.drawOn(page)
        // The image is 300 DPI, so its size is 72/300 of its pixels.
        image4.setLocation(50.0, point[1] + 5.0).scaleBy(0.425 * 300.0 / 72.0).drawOn(page)

        try pdf.complete()
    }
}   // End of Example_24.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_24()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_24 => \(String(format: "%4lld", time1 - time0)) ms")
