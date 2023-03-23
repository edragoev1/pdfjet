import Foundation
import PDFjet

///
/// Example_51.java
///  
/// This example shows how to add "Page X of N" footer to every page of
/// the PDF file. In this case we create new PDF and store it in a buffer.
///
public class Example_51 {
    public init(_ fileNumber: String) throws {
        let stream = OutputStream(toFileAtPath: "temp.pdf", append: false)!
        let pdf = PDF(stream)
        var page = Page(pdf, Letter.PORTRAIT)

        var box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.red)
        box.setFillShape(true)
        box.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.green)
        box.setFillShape(true)
        box.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)
        box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.blue)
        box.setFillShape(true)
        box.drawOn(page)

        pdf.complete()

        try AddFooterToPDF(fileNumber)
    }

    public func AddFooterToPDF(_ fileNumber: String) throws {
        let stream = OutputStream(toFileAtPath: "Example_\(fileNumber).pdf", append: false)!
        var pdf = PDF(stream)
        var objects = try pdf.read(
                from: InputStream(fileAtPath: "temp.pdf")!)

        let font = try Font(
                &objects,
                InputStream(fileAtPath: "fonts/Droid/DroidSans.ttf.stream")!,
                Font.STREAM).setSize(12.0)
        font.setSize(12.0)

        var pages = pdf.getPageObjects(from: &objects)
        var i = 0
        while i < pages.count {
            let footer = "Page " + String(i + 1) + " of " + String(pages.count)
            let page = Page(&pdf, &pages[i])
            page.addResource(font, &objects)
            page.setBrushColor(Color.transparent)   // Required!
            page.setBrushColor(Color.black)
            page.drawString(
                    font,
                    footer,
                    (page.getWidth() - font.stringWidth(footer))/2.0,
                    (page.getHeight() - 5.0))
            page.complete(&objects)
            i += 1
        }
        pdf.addObjects(&objects)
        pdf.complete()
    }
}   // End of Example_51.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_51("51")
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_51 => \(time1 - time0)")
