import Foundation
import PDFjet

/**
 * Example_46.swift
 */
public class Example_46 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_46.pdf", append: false)!)

        let font = Font(pdf, CoreFont.HELVETICA)

        let image1 = try Image(pdf, "images/map407.png")
        image1.setLocation(10.0, 100.0)

        let image2 = try Image(pdf, "images/qrcode.png")
        image2.setLocation(10.0, 100.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var textLine = TextLine(font)
        textLine.setText("© OpenStreetMap contributors")
        textLine.setLocation(10.0, 655.0)
        let xy = textLine.drawOn(page)

        textLine = TextLine(font, "http://www.openstreetmap.org/copyright")
        textLine.setURIAction("http://www.openstreetmap.org/copyright")
        textLine.setLocation(10.0, xy[1] + font.getBodyHeight())
        textLine.drawOn(page)

        var group = OptionalContentGroup(pdf, "Open Source Map")
        group.add(image1)
        group.setVisible(true)
        group.setPrintable(false)
        group.drawOn(page)

        let textBox = TextBox(font)
        textBox.setText("Blue Layer Text")
        textBox.setLocation(10.0, 130.0)

        var line = Line()
        line.setPointA(300.0, 150.0)
        line.setPointB(500.0, 150.0)
        line.setWidth(2.0)
        line.setColor(Color.blue)

        group = OptionalContentGroup(pdf, "Blue Layer")
        group.add(textBox)
        group.add(line)
        group.setVisible(true)
        group.drawOn(page)

        line = Line()
        line.setPointA(350.0, 160.0)
        line.setPointB(550.0, 160.0)
        line.setWidth(2.0)
        line.setColor(Color.red)
        line.drawOn(page)

        group = OptionalContentGroup(pdf, "Barcode")
        group.add(image2)
        group.add(line)
        group.setPrintable(true)
        group.drawOn(page)

        pdf.complete()
    }
}   // End of Example_46.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_46()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_46", time0, time1)
