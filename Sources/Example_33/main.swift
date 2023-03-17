import Foundation
import PDFjet


/**
 *  Example_33.swift
 *
 */
public class Example_33 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_33.pdf", append: false) {

            let pdf = PDF(stream)
            let page = Page(pdf, Letter.PORTRAIT)

            let image = try Image(
                    pdf,
                    InputStream(fileAtPath: "images/photoshop.jpg")!,
                    ImageType.JPG)
            image.setLocation(10.0, 10.0)
            image.scaleBy(0.25)
            image.drawOn(page)

            var stream = InputStream(
                    fileAtPath: "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg")
            var icon = SVGImage(stream!)
            icon.setLocation(20.0, 670.0)
            var xy: [Float] = icon.drawOn(page)

            stream = InputStream(
                    fileAtPath: "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg")
            icon = SVGImage(stream!)
            icon.setLocation(xy[0], 670.0)
            xy = icon.drawOn(page)

            stream = InputStream(
                    fileAtPath: "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg")
            icon = SVGImage(stream!)
            icon.setLocation(xy[0], 670.0)
            xy = icon.drawOn(page)

            stream = InputStream(
                    fileAtPath: "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg")
            icon = SVGImage(stream!)
            icon.setLocation(xy[0], 670.0)
            xy = icon.drawOn(page)

            stream = InputStream(
                    fileAtPath: "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg")
            icon = SVGImage(stream!)
            // icon.setFillPath(false)
            icon.setLocation(xy[0], 670.0)
            xy = icon.drawOn(page)

            stream = InputStream(
                    fileAtPath: "images/svg-test/test-CS.svg")
            icon = SVGImage(stream!)
            icon.setLocation(xy[0], 670.0)
            xy = icon.drawOn(page)

            pdf.complete()
        }
    }

}   // End of Example_33.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_33()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_33 => \(time1 - time0)")
