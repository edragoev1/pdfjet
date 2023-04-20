import Foundation
import PDFjet

/**
 *  Example_33.swift
 */
public class Example_33 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_33.pdf", append: false)!)
        let page = Page(pdf, Letter.PORTRAIT)

        var icon = SVGImage(stream: InputStream(fileAtPath: "images/svg-test/europe.svg")!)
        icon.setLocation(-150.0, 0.0)
        var xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg")
        icon.setLocation(20.0, 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg")
        icon.setLocation(xy[0], 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg")
        icon.setLocation(xy[0], 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg")
        icon.setLocation(xy[0], 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg")
        icon.setLocation(xy[0], 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg-test/test-CS.svg")
        icon.setLocation(xy[0], 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg-test/test-QQ.svg")
        icon.setLocation(xy[0], 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg-test/menu-icon.svg")
        icon.setLocation(xy[0], 670.0)
        xy = icon.drawOn(page)

        icon = SVGImage(fileAtPath: "images/svg-test/menu-icon-close.svg")
        icon.setLocation(xy[0], 670.0)
        icon.scaleBy(2.0)
        icon.drawOn(page)

        pdf.complete()
    }
}   // End of Example_33.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_33()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_33 => \(time1 - time0)")
