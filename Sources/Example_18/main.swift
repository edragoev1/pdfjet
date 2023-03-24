import Foundation
import PDFjet

/**
 *  Example_18.swift
 *  This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_18.pdf", append: false)!
        let pdf = PDF(stream)

        let font = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/RedHatText/RedHatText-Regular.ttf.stream")!,
                Font.STREAM)
        font.setSize(12.0)

        var pages = [Page]()
        var page = Page(pdf, A4.PORTRAIT)

        var box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.red)
        box.setFillShape(true)
        box.drawOn(page)
        pages.append(page)

        page = Page(pdf, A4.PORTRAIT)
        box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.green)
        box.setFillShape(true)
        box.drawOn(page)
        pages.append(page)

        page = Page(pdf, A4.PORTRAIT)
        box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.blue)
        box.setFillShape(true)
        box.drawOn(page)
        pages.append(page)

        var i = 0
        while i < pages.count {
            page = pages[i]
            let footer = "Page " + String(i + 1) + " of " + String(pages.count)
            page.setBrushColor(Color.black)
            page.drawString(
                    font,
                    footer,
                    (page.getWidth() - font.stringWidth(footer))/2.0,
                    (page.getHeight() - 5.0))
            i += 1
        }

        pdf.complete()
    }
}   // End of Example_18.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_18()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_18 => \(time1 - time0)")
