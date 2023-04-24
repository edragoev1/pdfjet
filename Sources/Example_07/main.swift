import Foundation
import PDFjet

/**
 *  Example_07.swift
 */
public class Example_07 {
    public init() throws {
        // let pdf = PDF(OutputStream(toFileAtPath: "Example_07.pdf", append: false)!, Compliance.PDF_A_1B)
        // pdf.setTitle("PDF/A-1B compliant PDF")

        let pdf = PDF(OutputStream(toFileAtPath: "Example_07.pdf", append: false)!, Compliance.PDF_UA)
        pdf.setTitle("PDF/UA compliant PDF")

        let f1 = try Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")

        var page = Page(pdf, A4.LANDSCAPE)

        f1.setSize(72.0)
        try page.addWatermark(f1, "This is a Draft")
        f1.setSize(18.0)

        let xPos: Float = 20.0
        var yPos: Float = 20.0

        let textLine = TextLine(f1).setLocation(xPos, yPos)

        var buffer = String()
        var j = 0
        var i = 0x410
        while i < 0x46F {
            if j % 64 == 0 {
                textLine.setText(buffer)
                textLine.setLocation(xPos, yPos)
                textLine.drawOn(page)
                buffer = ""
                yPos += 24.0
            }
            buffer.append(Character(UnicodeScalar(i)!))
            i += 1
            j += 1
        }
        textLine.setText(buffer)
        textLine.setLocation(xPos, yPos)
        textLine.drawOn(page)

        yPos += 24.0
        buffer = String()
        j = 0
        i = 0x20
        while i < 0x7F {
            if j % 64 == 0 {
                textLine.setText(buffer)
                textLine.setLocation(xPos, yPos)
                textLine.drawOn(page)
                buffer = ""
                yPos += 24.0
            }
            buffer.append(Character(UnicodeScalar(i)!))
            i += 1
            j += 1
        }
        textLine.setText(buffer)
        textLine.setLocation(xPos, yPos)
        textLine.drawOn(page)

        page = Page(pdf, A4.LANDSCAPE)
        textLine.setText("Hello, World!")
        textLine.setUnderline(true)
        textLine.setLocation(xPos, 34.0)
        textLine.drawOn(page)

        pdf.complete()
    }
}   // End of Example_07.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_07()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_07", time0, time1)
