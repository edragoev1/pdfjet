import Foundation
import PDFjet

/**
 * Example_18.swift
 * This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_18.pdf", append: false)!)

        let font = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
        font.setSize(14.0)

        var pages = [Page]()
        var page = Page(pdf, A4.PORTRAIT, Page.DETACHED)

        var box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.red)
        box.setFillShape(true)
        box.drawOn(page)
        pages.append(page)

        page = Page(pdf, A4.PORTRAIT, Page.DETACHED)
        box = Box()
        box.setLocation(50.0, 50.0)
        box.setSize(100.0, 100.0)
        box.setColor(Color.green)
        box.setFillShape(true)
        box.drawOn(page)
        pages.append(page)

        page = Page(pdf, A4.PORTRAIT, Page.DETACHED)
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
                    font.getSize(),
                    footer,
                    (page.getWidth() - font.stringWidth(footer))/2.0,
                    (page.getHeight() - 5.0))
            i += 1
        }
        pdf.addPages(pages)

        pdf.complete()
    }
}   // End of Example_18.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_18()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_18", time0, time1)
