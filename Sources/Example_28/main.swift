import Foundation
import PDFjet


///
/// Example_28.swift
///
public class Example_28 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_28.pdf", append: false) {

            let pdf = PDF(stream)

            let f1 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/Droid/DroidSans.ttf.stream")!,
                    Font.STREAM)


            let f2 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/Droid/DroidSansFallback.ttf.stream")!,
                    Font.STREAM)

            f1.setSize(11.0)
            f2.setSize(11.0)

            let page = Page(pdf, Letter.LANDSCAPE)

            let str = (try String(contentsOfFile:
                    "data/report.csv", encoding: .utf8)).trimmingCharacters(in: .newlines)
            let lines = str.components(separatedBy: "\n")

            var y: Float = 40.0
            for line in lines {
                y += 20.0
                TextLine(f1, line).setFallbackFont(f2).setLocation(50.0, y).drawOn(page)
            }

            pdf.complete()
        }
    }

}   // End of Example_28.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_28()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_28 => \(time1 - time0)")
