import Foundation
import PDFjet

/**
 * Example_47.swift
 */
public class Example_47 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_47.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(14.0)

        let paragraphs = try String(
                contentsOfFile: "data/dostoevsky.txt", encoding: .utf8).components(separatedBy: "\n\n")

        var x: Float = 50.0
        var y: Float = 50.0
        let w: Float = 230.0
        let h: Float = 500.0
        let gap: Float = 20.0

        var page: Page? = nil
        let textFrame = TextFrame(f1, paragraphs)
        while textFrame.hasMoreText() {
            page = Page(pdf, Letter.LANDSCAPE)

            textFrame.setLocation(x, y)
            textFrame.setWidth(w)
            textFrame.setHeight(h)
            _ = textFrame.drawOn(page)

            if (textFrame.hasMoreText()) {
                x += w + gap
                textFrame.setLocation(x, y)
                textFrame.setWidth(w)
                textFrame.setHeight(h)
                _ = textFrame.drawOn(page)
            }

            if (textFrame.hasMoreText()) {
                x += w + gap
                textFrame.setLocation(x, y)
                textFrame.setWidth(w)
                textFrame.setHeight(h)
                _ = textFrame.drawOn(page)
            }

            x = 50.0
            y = 50.0
        }

        pdf.complete()
    }
}   // End of Example_47.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_47()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_47", time0, time1)
