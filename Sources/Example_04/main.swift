import Foundation
import PDFjet

/**
 * Example_04.swift
 *
 * Draws Chinese, Japanese and Korean text with the CJK fonts, and Latin text
 * with Courier. None of these fonts is embedded: the PDF names them and the
 * viewer supplies them.
 *
 * The advantage is size and speed. A CJK font holds tens of thousands of
 * glyphs, and this document carries none of them, so it is a few kilobytes
 * and is written in a moment. The disadvantages: the viewer must have the
 * Adobe Asian font packs, or a substitute, and the text takes the shapes and
 * widths of whatever font it finds, so the document does not look the same
 * everywhere; the core font Courier is limited to the WinAnsi characters; and
 * a document with a font that is not embedded cannot claim PDF/A or PDF/UA
 * compliance. To ship the glyphs with the document, use an embedded font like
 * IBM Plex Sans JP, KR, SC or TC, as Example_02 and 19 do.
 *
 * - SeeAlso: `Font`
 * - SeeAlso: `CJKFont`
 */
public class Example_04 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_04.pdf", append: false)!)

        let f0 = Font(pdf, CoreFont.COURIER)
        f0.setSize(14.0)

        // Chinese (Traditional) font
        // Uses Adobe's Ming Standard Light font (明體)
        let f1 = Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT)
        f1.setSize(14.0)

        // Chinese (Simplified) font
        // Uses Adobe's Heiti SC Light font (黑体-简)
        let f2 = Font(pdf, CJKFont.ST_HEITI_SC_LIGHT)
        f2.setSize(14.0)

        // Japanese font
        // Uses Kozuka Mincho Pro VI Regular font (小塚明朝)
        let f3 = Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR)
        f3.setSize(14.0)

        // Korean font
        // Uses Adobe's Myungjo Standard Medium font (명조체)
        let f4 = Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM)
        f4.setSize(14.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let x_pos: Float = 100.0
        var y_pos: Float = 100.0

        let fileName = "data/happy-new-year.txt"
        var lines = (try String(contentsOfFile: fileName, encoding: .utf8))
                .replacingOccurrences(of: "\r\n", with: "\n")
                .components(separatedBy: .newlines)
        if lines.last == "" {
            lines.removeLast()  // The trailing newline is not a line, as in Java's readLine()
        }

        let text = TextLine(f0)
        for line in lines {
            text.setText(line)
            text.setLocation(x_pos, y_pos)
            text.drawOn(page)
            if line.contains("Traditional") {
                text.setFont(f1)
            } else if line.contains("Simplified") {
                text.setFont(f2)
            } else if line.contains("Japanese") {
                text.setFont(f3)
            } else if line.contains("Korean") {
                text.setFont(f4)
            } else {
                text.setFont(f0)
            }
            y_pos += Float(25.0)
        }

        try pdf.complete()
    }
}   // End of Example_04.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_04()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_04 => \(time1 - time0) ms")
