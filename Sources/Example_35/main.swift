import Foundation
import PDFjet


/**
 *  Example_35.swift
 *
 */
public class Example_35 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_35.pdf", append: false) {

            let pdf = PDF(stream)
            let page = Page(pdf, A4.PORTRAIT)

            let text = try String(contentsOfFile: "data/chinese-english.txt", encoding: .utf8)

            let mainFont = Font(pdf, "AdobeMingStd")
            let fallbackFont = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/OpenSans/OpenSans-Regular.ttf")!)

            var textLine = TextLine(mainFont)
            textLine.setText(text)
            textLine.setLocation(50.0, 50.0)
            textLine.drawOn(page)

            textLine = TextLine(mainFont)
            textLine.setFallbackFont(fallbackFont)
            textLine.setText(text)
            textLine.setLocation(50.0, 80.0)
            textLine.drawOn(page)

            pdf.complete()
        }
    }

}   // End of Example_35.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_35()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_35 => \(time1 - time0)")
