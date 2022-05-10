import Foundation
import PDFjet


/**
 *  Example_36.swift
 *
 */
public class Example_36 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_36.pdf", append: false) {

            let pdf = PDF(stream)

            let page1 = Page(pdf, A4.PORTRAIT, false)

            let f1 = Font(pdf, CoreFont.HELVETICA)

            let image1 = try Image(
                    pdf,
                    InputStream(fileAtPath: "images/ee-map.png")!,
                    ImageType.PNG)

            let image2 = try Image(
                    pdf,
                    InputStream(fileAtPath: "images/fruit.jpg")!,
                    ImageType.JPG)

            let image3 = try Image(
                    pdf,
                    InputStream(fileAtPath: "images/mt-map.bmp")!,
                    ImageType.BMP)

            let text = TextLine(f1,
                    "The map below is an embedded PNG image")
            text.setLocation(90.0, 30.0)
            text.drawOn(page1)

            image1.setLocation(90.0, 40.0)
            image1.scaleBy(2.0/3.0)
            image1.drawOn(page1)

            text.setText(
                    "JPG image file embedded once and drawn 3 times")
            text.setLocation(90.0, 550.0)
            text.drawOn(page1)

            image2.setLocation(90.0, 560.0)
            image2.scaleBy(0.5)
            image2.drawOn(page1)

            image2.setLocation(260.0, 560.0)
            image2.scaleBy(0.5)
            image2.setRotateCW90(true)
            image2.drawOn(page1)

            image2.setLocation(350.0, 560.0)
            image2.setRotateCW90(false)
            image2.scaleBy(0.5)
            image2.drawOn(page1)

            image3.setLocation(390.0, 630.0)
            image3.scaleBy(0.5)
            image3.drawOn(page1)

            let page2 = Page(pdf, A4.PORTRAIT, false)
            image1.drawOn(page2)

            text.setText("Hello, World!!")
            text.setLocation(90.0, 800.0)
            text.drawOn(page2)

            text.setText(
                    "The map on the right is an embedded BMP image")
            text.setUnderline(true)
            text.setStrikeout(true)
            text.setTextDirection(15)
            text.setLocation(90.0, 800.0)
            text.drawOn(page1)

            pdf.addPage(page2)
            pdf.addPage(page1)

            pdf.complete()
        }
    }

}   // End of Example_36.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_36()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_36 => \(time1 - time0)")
