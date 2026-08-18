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

        let page = Page(pdf, Letter.LANDSCAPE)

        let paragraphs = try String(
                contentsOfFile: "data/dostoevsky.txt", encoding: .utf8).components(separatedBy: "\n\n")

        var x: Float = 50.0
        let y: Float = 50.0
        let w: Float = 230.0
        let h: Float = 500.0
        let gap: Float = 20.0

        let frame = TextFrame(f1, paragraphs)
        frame.setLocation(x, y)
        frame.setWidth(w)
        frame.setHeight(h)
        _ = frame.drawOn(page)

        if (frame.hasMoreText()) {
            x += w + gap
            frame.setLocation(x, y)
            frame.setWidth(w)
            frame.setHeight(h)
            _ = frame.drawOn(page)
        }

        if (frame.hasMoreText()) {
            x += w + gap
            frame.setLocation(x, y)
            frame.setWidth(w)
            frame.setHeight(h)
            _ = frame.drawOn(page)
        }

        pdf.complete()
    }
}   // End of Example_47.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_47()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_47", time0, time1)
