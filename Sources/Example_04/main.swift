import Foundation
import PDFjet


/**
 *  Example_04.swift
 *
 */
public class Example_04 {

    public init() throws {

        let fileName = "data/happy-new-year.txt"

        if let stream = OutputStream(toFileAtPath: "Example_04.pdf", append: false) {

            let pdf = PDF(stream)

            // Chinese (Traditional) font
            let f1 = Font(pdf, Font.AdobeMingStd_Light)
    
            // Chinese (Simplified) font
            let f2 = Font(pdf, Font.STHeitiSC_Light)
    
            // Japanese font
            let f3 = Font(pdf, Font.KozMinProVI_Regular)
    
            // Korean font
            let f4 = Font(pdf, Font.AdobeMyungjoStd_Medium)
    
            let page = Page(pdf, Letter.PORTRAIT)
    
            f1.setSize(14.0)
            f2.setSize(14.0)
            f3.setSize(14.0)
            f4.setSize(14.0)
    
            let x_pos: Float = 100.0
            var y_pos: Float = 100.0

            let lines = (try String(
                    contentsOfFile: fileName, encoding: .utf8)).components(separatedBy: .newlines)

            let text = TextLine(f1)
            for line in lines {
                if line.contains("Simplified") {
                    text.setFont(f2)
                }
                else if line.contains("Japanese") {
                    text.setFont(f3)
                }
                else if line.contains("Korean") {
                    text.setFont(f4)
                }
                text.setText(line)
                text.setLocation(x_pos, y_pos)
                text.drawOn(page)
                y_pos += Float(25.0)
            }

            pdf.complete()
        }
    }

}   // End of Example_04.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_04()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_04 => \(time1 - time0)")
