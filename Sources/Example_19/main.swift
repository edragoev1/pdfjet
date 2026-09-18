/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_19.swift
 * Using the TextBlock component to draw text next to images. The drawOn
 * methods of Image and TextBlock return the bottom of what they drew, so each
 * row starts below the taller of the image and the text next to it.
 */
public class Example_19 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_19.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "Text Next to Images")
        text.setFontSize(22.0)
        text.setLocation(50.0, 80.0)
        text.drawOn(page)

        var textBlock = TextBlock(f1,
                "Each map below is an Image with a TextBlock next to it. The drawOn "
                + "method of both returns the bottom of what it drew, so every row "
                + "starts below the taller of the two.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(512.0)
        let xy = textBlock.drawOn(page)

        let imageFiles = [
            "images/ee-map.png",
            "images/spain-admin.jpg",
        ]
        let titles = [
            "The European Union",
            "The Regions of Spain",
        ]
        let descriptions = [
            "A map of Europe with the member states of the European Union and "
                + "the countries that were candidates to join it when the map was "
                + "made. The image is a PNG file of 687 by 710 pixels, drawn 200 "
                + "points wide.",
            "A map of the 17 autonomous communities of Spain and its two "
                + "autonomous cities, Ceuta and Melilla, with their capitals. The "
                + "image is a JPEG file of 2,017 by 2,412 pixels, drawn 200 points "
                + "wide, which prints at more than 700 dots per inch.",
        ]

        let x1: Float = 50.0    // The images
        let x2: Float = 270.0   // The text next to them
        var y: Float = xy[1] + 25.0
        for i in 0..<imageFiles.count {
            let image = try Image(pdf, imageFiles[i])
            image.resizeWidth(200.0)
            image.setLocation(x1, y)
            let imageXY = image.drawOn(page)

            text = TextLine(f2, titles[i])
            text.setFontSize(14.0)
            text.setLocation(x2, y + f2.getAscent(14.0))
            text.drawOn(page)

            textBlock = TextBlock(f1, descriptions[i])
            textBlock.setFontSize(11.0)
            textBlock.setLineSpacing(1.5)
            textBlock.setLocation(x2, y + 25.0)
            textBlock.setWidth(292.0)
            let textXY = textBlock.drawOn(page)

            y = max(imageXY[1], textXY[1]) + 25.0
        }

        try pdf.complete()
    }
}   // End of Example_19.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_19()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_19 => \(String(format: "%4lld", time1 - time0)) ms")
