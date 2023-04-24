import Foundation
import PDFjet

/**
 *  Example_24.swift
 */
public class Example_24 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_24.pdf", append: false)
        let pdf = PDF(stream!)
        let font = Font(pdf, CoreFont.HELVETICA)

        let image_00 = try Image(pdf, "images/gr-map.jpg")
        let image_01 = try Image(pdf, "images/linux-logo.png.stream")
        let image_02 = try Image(pdf, "images/ee-map.png")
        let image_03 = try Image(pdf, "images/rgb24pal.bmp")

        var page = Page(pdf, Letter.PORTRAIT)
        let textline_00 = TextLine(font, "This is a JPEG image.")
        textline_00.setTextDirection(0)
        textline_00.setLocation(50.0, 50.0)
        var point = textline_00.drawOn(page)
        image_00.setLocation(50.0, point[1] + 10.0).scaleBy(0.25).drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        let textline_01 = TextLine(font, "This is a PNG_STREAM image.")
        textline_01.setTextDirection(0)
        textline_01.setLocation(50.0, 50.0)
        point = textline_01.drawOn(page)
        image_01.setLocation(50.0, point[1] + 10.0).drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        let textline_02 = TextLine(font, "This is a PNG image.")
        textline_02.setTextDirection(0)
        textline_02.setLocation(50.0, 50.0)
        point = textline_02.drawOn(page)
        image_02.setLocation(50.0, point[1] + 10.0).scaleBy(0.75).drawOn(page)

        let textline_03 = TextLine(font, "This is a BMP image.")
        textline_03.setTextDirection(0)
        textline_03.setLocation(50.0, 620.0)
        point = textline_03.drawOn(page)
        image_03.setLocation(50.0, point[1] + 10.0).scaleBy(0.75).drawOn(page)	

        pdf.complete()
    }
}   // End of Example_24.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_24()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_24", time0, time1)
