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
 * and a set of icons, each labeled with its name.
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

        let page = Page(pdf, A4.PORTRAIT)

        var text = TextLine(f2, "SVG Images")
        text.setFontSize(22.0)
        text.setLocation(50.0, 80.0)
        text.drawOn(page)

        var textBlock = TextBlock(f1,
                "SVGImage reads the paths of an SVG file and draws them as vector "
                + "graphics, which stay sharp at any zoom. The map is 1,000 points wide "
                + "in its file and is scaled by 0.5 to fit the page.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(495.0)
        var xy = textBlock.drawOn(page)

        let map = try loadSVG("images/svg-test/europe.svg")
        map.scaleBy(0.5)
        map.setLocation((page.getWidth() - map.getWidth()) / 2.0, xy[1] + 20.0)
        xy = map.drawOn(page)

        textBlock = TextBlock(f1,
                "The colors come from the file: the peachpuff fill of the svg element "
                + "for most countries, an aliceblue fill for Spain and an olive "
                + "outline for Austria.")
        textBlock.setFontSize(10.0)
        textBlock.setTextColor(Color.gray)
        textBlock.setLocation(50.0, xy[1] + 10.0)
        textBlock.setWidth(495.0)
        xy = textBlock.drawOn(page)

        let iconFiles = [
            "images/svg/home_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/search_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/settings_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg-test/menu-icon.svg",
            "images/svg-test/menu-icon-close.svg",
            "images/svg-test/test-CS.svg",
            "images/svg-test/test-QQ1.svg",
        ]
        let iconNames = [
            "home",
            "search",
            "checkout",
            "palette",
            "stories",
            "add",
            "star",
            "settings",
            "menu",
            "close",
            "C and S curves",
            "Q curves",
        ]

        // Two rows of six icons, 48 by 48 points each.
        let y: Float = xy[1] + 30.0
        for i in 0..<iconFiles.count {
            let x: Float = 50.0 + Float(i % 6) * 85.0
            let yIcon: Float = y + Float(i / 6) * 90.0

            let icon = try loadSVG(iconFiles[i])
            icon.setLocation(x, yIcon)
            let iconXY = icon.drawOn(page)

            text = TextLine(f1, iconNames[i])
            text.setFontSize(9.0)
            text.setTextColor(Color.gray)
            text.setLocation(x, iconXY[1] + 15.0)
            text.drawOn(page)
        }

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
