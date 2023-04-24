import Foundation
import PDFjet

/**
 *  Example_03.swift
 *
 */
public class Example_03 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_03.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA)

        let page = Page(pdf, A4.PORTRAIT)

        var xy = try page.addHeader(TextLine(f1, "This is a header!"))
        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(30.0, 30.0)
        box.drawOn(page)
        try page.addFooter(TextLine(f1, "And this is a footer."))

        let image1 = try Image(pdf, "images/ee-map.png.stream")
        // let image1 = try Image(pdf, "images/ee-map.png")
        let image2 = try Image(pdf, "images/fruit.jpg")
        let image3 = try Image(pdf, "images/mt-map.bmp")

        let textline1 = TextLine(f1, "The map below is an embedded PNG image")
        textline1.setLocation(90.0, 30.0)
        var point = textline1.drawOn(page)

        image1.setLocation(90.0, point[1] + f1.getDescent())
                .scaleBy(2.0/3.0)
                .drawOn(page)

        let textline2 = TextLine(f1, "JPG image file embedded once and drawn 3 times")
        textline2.setLocation(90.0, 550.0)
        textline2.setURIAction("https://en.wikipedia.org/wiki/European_Union")
        point = textline2.drawOn(page)

        image2.setLocation(90.0, point[1] + f1.getDescent())
                .scaleBy(0.5)
                .drawOn(page)

        image2.setLocation(260.0, point[1] + f1.getDescent())
                .scaleBy(0.5)
                .setRotate(ClockWise._90_degrees)
                // .setRotate(ClockWise._180_degrees)
                // .setRotate(ClockWise._270_degrees)
                .drawOn(page)

        image2.setLocation(350.0, point[1] + f1.getDescent())
                .setRotate(ClockWise._0_degrees)
                .scaleBy(0.5)
                .drawOn(page)

        TextLine(f1,
                "The map on the right is an embedded BMP image")
                .setUnderline(true)
                .setVerticalOffset(3.0)
                .setStrikeout(true)
                .setTextDirection(15)
                .setLocation(90.0, 800.0)
                .drawOn(page)

        image3.setLocation(390.0, 630.0)
                .scaleBy(0.5)
                .drawOn(page)

        let page2 = Page(pdf, A4.PORTRAIT)
        xy = image1.drawOn(page2)

        Box().setLocation(xy[0], xy[1]).setSize(20.0, 20.0).drawOn(page2)

        pdf.complete()
    }
}   // End of Example_03.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_03()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_03", time0, time1)
