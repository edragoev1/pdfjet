import Foundation
import PDFjet

/**
 * Example_36.swift
 */
public class Example_36 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_36.pdf", append: false)!)
        let f1 = Font(pdf, CoreFont.HELVETICA)

        let image1 = try Image(pdf, "images/ee-map.png")
        let image2 = try Image(pdf, "images/spain-admin.jpg")

        let page1 = Page(pdf, A4.PORTRAIT, Page.DETACHED)

        let text = TextLine(f1,
                "The map below is an embedded PNG image")
        text.setLocation(90.0, 30.0)
        text.drawOn(page1)

        image1.setLocation(90.0, 40.0)
        image1.scaleBy(0.3)
        image1.drawOn(page1)

        let page2 = Page(pdf, A4.PORTRAIT, Page.DETACHED)

        text.setText("This page was created after the second one but it was drawn first!")
        text.setLocation(90.0, 30.0)
        let xy = text.drawOn(page2)

        image2.setLocation(90.0, xy[1] + 10.0)
        image2.scaleBy(0.1)
        image2.drawOn(page2)

        pdf.addPage(page2)
        pdf.addPage(page1)

        pdf.complete()
    }
}   // End of Example_36.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_36()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_36", time0, time1)
