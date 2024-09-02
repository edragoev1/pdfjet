import Foundation
import PDFjet

/**
 *  Example_53.swift
 */
public class Example_53 {
    public init(_ fileName: String) throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_53.pdf", append: false)!)
        var objects = try pdf.read(from: InputStream(fileAtPath: fileName)!)
        let pages = pdf.getPageObjects(from: objects)
        for pageObj in pages {
            let page = Page(pdf, pageObj)
            page.drawLine(0.0, 0.0, 200.0, 200.0)
            page.complete(&objects) // The graphics stack is unwinded automatically
        }
        pdf.addObjects(&objects)
        pdf.complete()
    }
}   // End of Example_53.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_53("../testPDFs/cairo-graphics-1.pdf")
// _ = try Example_53("../testPDFs/cairo-graphics-2.pdf")
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_53", time0, time1)
