import Foundation
import PDFjet

/**
 * Example_33.swift
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

        let page = Page(pdf, Letter.PORTRAIT)

        var image = try loadSVG("images/svg-test/europe.svg")
        image.setLocation(-150.0, 0.0)
        var xy = image.drawOn(page)

        image = try loadSVG("images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(20.0, 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg-test/test-CS.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg-test/test-QQ1.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg-test/menu-icon.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = try loadSVG("images/svg-test/menu-icon-close.svg")
        image.setLocation(xy[0], 670.0)
        image.drawOn(page)

        pdf.complete()
    }

    private func loadSVG(_ path: String) throws -> SVGImage {
        guard let image = SVGImage(fileAtPath: path) else {
            throw ExampleError(message: "Cannot open SVG file: \(path)")
        }
        return image
    }
}   // End of Example_33.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_33()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_33", time0, time1)
