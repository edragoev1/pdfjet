import Foundation
import PDFjet


/**
 *  Example_11.swift
 *
 */
public class Example_11 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_11.pdf", append: false) {

            let pdf = PDF(stream)

            let page = Page(pdf, Letter.PORTRAIT)

            let f1 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/OpenSans/OpenSans-Regular.ttf.stream")!,
                    Font.STREAM)

            var code = BarCode(BarCode.CODE128, "Hellö, World!")
            code.setLocation(170.0, 70.0)
            code.setModuleLength(0.75)
            code.setFont(f1)
            code.drawOn(page)

            code = BarCode(BarCode.CODE128, "G86513JVW0C")
            code.setLocation(170.0, 170.0)
            code.setModuleLength(0.75)
            code.setDirection(BarCode.TOP_TO_BOTTOM)
            code.setFont(f1)
            code.drawOn(page)

            code = BarCode(BarCode.CODE39, "WIKIPEDIA")
            code.setLocation(270.0, 370.0)
            code.setModuleLength(0.75)
            code.setFont(f1)
            code.drawOn(page)

            code = BarCode(BarCode.CODE39, "CODE39")
            code.setLocation(400.0, 70.0)
            code.setModuleLength(0.75)
            code.setDirection(BarCode.TOP_TO_BOTTOM)
            code.setFont(f1)
            code.drawOn(page)

            code = BarCode(BarCode.CODE39, "CODE39")
            code.setLocation(450.0, 70.0)
            code.setModuleLength(0.75)
            code.setDirection(BarCode.BOTTOM_TO_TOP)
            code.setFont(f1)
            code.drawOn(page)

            code = BarCode(BarCode.UPC, "712345678904")
            code.setLocation(450.0, 270.0)
            code.setModuleLength(0.75)
            code.setDirection(BarCode.BOTTOM_TO_TOP)
            code.setFont(f1)
            code.drawOn(page)

            pdf.complete()
        }
    }

}   // End of Example_11.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_11()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_11 => \(time1 - time0)")
