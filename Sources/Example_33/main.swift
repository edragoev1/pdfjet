import Foundation
import PDFjet

/**
 *  Example_33.swift
 */
public class Example_33 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_33.pdf", append: false)!)
        let page = Page(pdf, Letter.PORTRAIT)

        var image = SVGImage(stream: InputStream(fileAtPath: "images/svg-test/europe.svg")!)
        image.setLocation(-150.0, 0.0)
        var xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(20.0, 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg-test/test-CS.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg-test/test-QQ.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg-test/menu-image.svg")
        image.setLocation(xy[0], 670.0)
        xy = image.drawOn(page)

        image = SVGImage(fileAtPath: "images/svg-test/menu-image-close.svg")
        image.setLocation(xy[0], 670.0)
        image.scaleBy(2.0)
        image.drawOn(page)

        pdf.complete()
    }
}   // End of Example_33.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_33()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_33 => \(time1 - time0)")
