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

            let f3 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/Noto/NotoSansSymbols-Regular-Subsetted.ttf.stream")!,
                    Font.STREAM)

            f1.setSize(11.0)
            f2.setSize(11.0)
            f3.setSize(11.0)

            let page = Page(pdf, Letter.LANDSCAPE)

            let str = (try String(contentsOfFile:
                    "data/report.csv", encoding: .utf8)).trimmingCharacters(in: .newlines)
            let lines = str.components(separatedBy: "\n")

            let x: Float = 50.0
            var y: Float = 40.0
            for line in lines {
                y += 20.0
                TextLine(f1, line)
                        .setFallbackFont(f2)
                        .setLocation(50.0, y)
                        .drawOn(page)
            }

            y = 210.0
            let dy: Float = 22.0

            let text = TextLine(f3)
            var buf = String()
            var count: Int = 0
            var i: Int = 0x2200
            while i <= 0x22FF {
                // Draw the Math Symbols
                if count % 80 == 0 {
                    y += dy
                    text.setText(buf)
                    text.setLocation(x, y)
                    text.drawOn(page)
                    buf = ""
                }
                buf.append(Character(UnicodeScalar(i)!))
                count += 1
                i += 1
            }
            y += dy
            text.setText(buf)
            text.setLocation(x, y)
            text.drawOn(page)

            buf = ""
            count = 0
            i = 0x25A0
            while i <= 0x25FF {
                // Draw the Geometric Shapes
                if count % 80 == 0 {
                    y += dy
                    text.setText(buf)
                    text.setLocation(x, y)
                    text.drawOn(page)
                    buf = ""
                }
                buf.append(Character(UnicodeScalar(i)!))
                count += 1
                i += 1
            }

            y += dy
            text.setText(buf)
            text.setLocation(x, y)
            text.drawOn(page)

            buf = ""
            count = 0
            i = 0x2701
            while i <= 0x27ff {
                // Draw the Dingbats
                if count % 80 == 0 {
                    y += dy
                    text.setText(buf)
                    text.setLocation(x, y)
                    text.drawOn(page)
                    buf = ""
                }
                buf.append(Character(UnicodeScalar(i)!))
                count += 1
                i += 1
            }

            y += dy
            text.setText(buf)
            text.setLocation(x, y)
            text.drawOn(page)

            buf = ""
            count = 0
            i = 0x2800
            while i <= 0x28FF {
                // Draw the Braille Patterns
                if count % 80 == 0 {
                    y += dy
                    text.setText(buf)
                    text.setLocation(x, y)
                    text.drawOn(page)
                    buf = ""
                }
                buf.append(Character(UnicodeScalar(i)!))
                count += 1
                i += 1
            }
            text.setText(buf)
            text.setLocation(x, y)
            text.drawOn(page)

            pdf.complete()
        }
    }

}   // End of Example_28.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_28()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_28 => \(time1 - time0)")
