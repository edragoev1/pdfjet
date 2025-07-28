import Foundation
import PDFjet

/**
 *  Example_11.swift
 *
 */
public class Example_11 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_11.pdf", append: false)!)
        let f1 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")!,
                Font.STREAM)

        let page = Page(pdf, Letter.PORTRAIT)

        var code = Barcode(Barcode.CODE128, "Hellö, World!")
        code.setLocation(170.0, 70.0)
        code.setModuleLength(0.75)
        code.setFont(f1)
        code.drawOn(page)

        code = Barcode(Barcode.CODE128, "G86513JVW0C")
        code.setLocation(170.0, 170.0)
        code.setModuleLength(0.75)
        code.setDirection(Barcode.TOP_TO_BOTTOM)
        code.setFont(f1)
        code.drawOn(page)

        code = Barcode(Barcode.CODE39, "WIKIPEDIA")
        code.setLocation(270.0, 370.0)
        code.setModuleLength(0.75)
        code.setFont(f1)
        code.drawOn(page)

        code = Barcode(Barcode.CODE39, "CODE39")
        code.setLocation(400.0, 70.0)
        code.setModuleLength(0.75)
        code.setDirection(Barcode.TOP_TO_BOTTOM)
        code.setFont(f1)
        code.drawOn(page)

        code = Barcode(Barcode.CODE39, "CODE39")
        code.setLocation(450.0, 70.0)
        code.setModuleLength(0.75)
        code.setDirection(Barcode.BOTTOM_TO_TOP)
        code.setFont(f1)
        code.drawOn(page)

        code = Barcode(Barcode.UPC, "712345678904")
        code.setLocation(450.0, 270.0)
        code.setModuleLength(0.75)
        code.setDirection(Barcode.BOTTOM_TO_TOP)
        code.setFont(f1)
        code.drawOn(page)

        pdf.complete()
    }
}   // End of Example_11.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_11()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_11", time0, time1)
