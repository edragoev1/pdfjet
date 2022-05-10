import Foundation
import PDFjet


/**
 *  Example_12.swift
 *  We will draw the American flag using Box, Line and Point objects.
 */
public class Example_12 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_12.pdf", append: false) {

            let pdf = PDF(stream)
            let font = Font(pdf, CoreFont.HELVETICA)

            let page = Page(pdf, Letter.PORTRAIT)

            var buf = String()
            let text = try String(contentsOfFile: "Sources/Example_12/main.swift", encoding: .utf8)
            let lines = text.components(separatedBy: "\n")
            for line in lines {
                if line == "\r" {
                    continue
                }
                buf.append(line)
                // Both CR and LF are required by the scanner!
                buf.append("\r\n")
            }

            let code2D = BarCode2D(buf)
            code2D.setModuleWidth(0.5)
            code2D.setLocation(100.0, 60.0)
            code2D.drawOn(page)
/*
            let box = Box()
            box.setLocation(xy[0], xy[1])
            box.setSize(20.0, 20.0)
            box.drawOn(page)
*/
            let textLine = TextLine(font, "PDF417 barcode containing the program that created it.")
            textLine.setLocation(100.0, 40.0)
            textLine.drawOn(page)
    
            pdf.complete()
        }
    }

}   // End of Example_12.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_12()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_12 => \(time1 - time0)")
