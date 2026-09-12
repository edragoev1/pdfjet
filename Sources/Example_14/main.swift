import Foundation
import PDFjet

/**
 * Example_14.swift
 * Drawing Data Matrix barcodes.
 */
public class Example_14 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_14.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var barcode = DataMatrix("https://github.com/edragoev1/pdfjet")
        barcode.setLocation(50.0, 50.0)
        barcode.setModuleLength(3.0)
        var xy = barcode.drawOn(page)
        var caption = TextLine(f1, "A web address")
        caption.setLocation(50.0, xy[1] + 20.0)
        caption.drawOn(page)

        barcode = DataMatrix("Grüße aus München! こんにちは 😀")
        barcode.setLocation(300.0, 50.0)
        barcode.setModuleLength(3.0)
        xy = barcode.drawOn(page)
        caption = TextLine(f1, "Text in UTF-8")
        caption.setLocation(300.0, xy[1] + 20.0)
        caption.drawOn(page)

        barcode = DataMatrix("PDFjet 9.0.0", DataMatrix.RECTANGLE)
        barcode.setLocation(50.0, 250.0)
        barcode.setModuleLength(4.0)
        barcode.setColor(Color.blue)
        xy = barcode.drawOn(page)
        caption = TextLine(f1, "A rectangular symbol")
        caption.setLocation(50.0, xy[1] + 20.0)
        caption.drawOn(page)

        var sb = ""
        for i in 1...20 {
            sb += "Line \(i) of a longer text in a larger symbol.\n"
        }
        barcode = DataMatrix(sb)
        barcode.setLocation(300.0, 250.0)
        barcode.setModuleLength(2.0)
        xy = barcode.drawOn(page)
        caption = TextLine(f1, "A larger symbol")
        caption.setLocation(300.0, xy[1] + 20.0)
        caption.drawOn(page)

        pdf.complete()
    }
}   // End of Example_14.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_14()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_14", time0, time1)
