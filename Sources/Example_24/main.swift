import Foundation
import PDFjet

/**
 * Example_24.swift
 */
public class Example_24 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_24.pdf", append: false)!)
        let font = Font(pdf, CoreFont.HELVETICA)

        let image1 = try Image(pdf, "images/gr-map.jpg")
        let image2 = try Image(pdf, "images/ee-map.png")
        let image3 = try Image(pdf, "images/rgb24pal.bmp")

        var page = Page(pdf, Letter.PORTRAIT)
        let textLine1 = TextLine(font, "This is a JPEG image.")
        textLine1.setTextDirection(0)
        textLine1.setLocation(50.0, 50.0)
        var point = textLine1.drawOn(page)
        image1.setLocation(50.0, point[1] + 5.0).scaleBy(0.25).drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        let textLine2 = TextLine(font, "This is a PNG image.")
        textLine2.setTextDirection(0)
        textLine2.setLocation(50.0, 50.0)
        point = textLine2.drawOn(page)
        image2.setLocation(50.0, point[1] + 5.0).scaleBy(0.75).drawOn(page)

        let textLine3 = TextLine(font, "This is a BMP image.")
        textLine3.setTextDirection(0)
        textLine3.setLocation(50.0, 620.0)
        point = textLine3.drawOn(page)
        image3.setLocation(50.0, point[1] + 5.0).scaleBy(0.75).drawOn(page)

        pdf.complete()
    }
}   // End of Example_24.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_24()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_24", time0, time1)
