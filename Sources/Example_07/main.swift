import Foundation
import PDFjet


/**
 *  Example_07.swift
 *
 */
public class Example_07 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_07.pdf", append: false) {

            let pdf = PDF(stream)
            let page = Page(pdf, Letter.PORTRAIT)
            // pdf.setPageLayout(PageLayout.SINGLE_PAGE)
            // pdf.setPageMode(PageMode.FULL_SCREEN)

            var f1 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/OpenSans/OpenSans-Regular.ttf.stream")!,
                    Font.STREAM)
            f1.setSize(72.0)

            var f2 = try Font(
                    pdf,
                    InputStream(fileAtPath: "fonts/Droid/DroidSerif-Italic.ttf.stream")!,
                    Font.STREAM)
            f2.setSize(15.0)

            try page.addWatermark(f1!, "This is a Draft")
            f1!.setSize(15.0)

            var x_pos: Float = 70.0
            var y_pos: Float = 70.0

            var text = TextLine(f1!).setLocation(x_pos, y_pos)

            var buffer = String()
            var i = 0x20
            while i < 0x7F {
                if i % 16 == 0 {
                    y_pos += 24.0
                    text.setText(buffer)
                    text.setLocation(x_pos, y_pos)
                    text.drawOn(page)
                    buffer = ""
                }
                buffer.append(Character(UnicodeScalar(i)!))
                i += 1
            }

            y_pos += 24.0
            text.setText(buffer)
            text.setLocation(x_pos, y_pos)
            text.drawOn(page);

            y_pos += 24.0
            buffer = ""
            i = 0x390
            while i < 0x3EF {
                if i % 16 == 0 {
                    y_pos += 24.0
                    text.setText(buffer)
                    text.setLocation(x_pos, y_pos)
                    text.drawOn(page)
                    buffer = ""
                }
                if i == 0x3A2
                        || i == 0x3CF
                        || i == 0x3D0
                        || i == 0x3D3
                        || i == 0x3D4
                        || i == 0x3D5
                        || i == 0x3D7
                        || i == 0x3D8
                        || i == 0x3D9
                        || i == 0x3DA
                        || i == 0x3DB
                        || i == 0x3DC
                        || i == 0x3DD
                        || i == 0x3DE
                        || i == 0x3DF
                        || i == 0x3E0
                        || i == 0x3EA
                        || i == 0x3EB
                        || i == 0x3EC
                        || i == 0x3ED
                        || i == 0x3EF {
                    // Replace .notdef with space to generate PDF/A compliant PDF
                    buffer.append(Character(UnicodeScalar(0x0020)!))
                }
                else {
                    buffer.append(Character(UnicodeScalar(i)!))
                }
                i += 1
            }

            y_pos += 24.0
            buffer = ""
            i = 0x410
            while i <= 0x46F {
                if i % 16 == 0 {
                    y_pos += 24.0
                    text.setText(buffer)
                    text.setLocation(x_pos, y_pos)
                    text.drawOn(page)
                    buffer = ""
                }
                buffer.append(Character(UnicodeScalar(i)!))
                i += 1
            }

            x_pos = 370.0
            y_pos = 70.0
            text = TextLine(f2!)
            text.setLocation(x_pos, y_pos)
            buffer = ""
            i = 0x20
            while i < 0x7F {
                if i % 16 == 0 {
                    y_pos += 24.0
                    text.setText(buffer)
                    text.setLocation(x_pos, y_pos)
                    text.drawOn(page)
                    buffer = ""
                }
                buffer.append(Character(UnicodeScalar(i)!))
                i += 1
            }

            y_pos += 24.0
            text.setText(buffer)
            text.setLocation(x_pos, y_pos)
            text.drawOn(page)

            y_pos += 24.0
            buffer = ""
            i = 0x390
            while i < 0x3EF {
                if i % 16 == 0 {
                    y_pos += 24.0
                    text.setText(buffer)
                    text.setLocation(x_pos, y_pos)
                    text.drawOn(page)
                    buffer = ""
                }
                if i == 0x3A2
                        || i == 0x3CF
                        || i == 0x3D0
                        || i == 0x3D3
                        || i == 0x3D4
                        || i == 0x3D5
                        || i == 0x3D7
                        || i == 0x3D8
                        || i == 0x3D9
                        || i == 0x3DA
                        || i == 0x3DB
                        || i == 0x3DC
                        || i == 0x3DD
                        || i == 0x3DE
                        || i == 0x3DF
                        || i == 0x3E0
                        || i == 0x3EA
                        || i == 0x3EB
                        || i == 0x3EC
                        || i == 0x3ED
                        || i == 0x3EF {
                    // Replace .notdef with space to generate PDF/A compliant PDF
                    buffer.append(Character(UnicodeScalar(0x0020)!))
                }
                else {
                    buffer.append(Character(UnicodeScalar(i)!))
                }
                i += 1
            }

            y_pos += 24.0
            buffer = ""
            i = 0x410
            while i < 0x46F {
                if i % 16 == 0 {
                    y_pos += 24.0
                    text.setText(buffer)
                    text.setLocation(x_pos, y_pos)
                    text.drawOn(page)
                    buffer = ""
                }
                buffer.append(Character(UnicodeScalar(i)!))
                i += 1
            }

            pdf.complete()
        }
    }

}   // End of Example_07.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_07()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_07 => \(time1 - time0)")
