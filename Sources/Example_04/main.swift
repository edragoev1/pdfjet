/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_04.swift
 *
 * Draws Chinese, Japanese and Korean text with the CJK fonts, and Latin text
 * with Helvetica. None of these fonts is embedded: the PDF names them and the
 * viewer supplies them.
 *
 * The advantage is size and speed. A CJK font holds tens of thousands of
 * glyphs, and this document carries none of them, so it is a few kilobytes
 * and is written in a moment. The disadvantages: the viewer must have the
 * Adobe Asian font packs, or a substitute, and the text takes the shapes and
 * widths of whatever font it finds, so the document does not look the same
 * everywhere; the core font Helvetica is limited to the WinAnsi characters; and
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

        // Core fonts for the Latin text
        let f0 = try Font(pdf, CoreFont.HELVETICA_BOLD)
        let f5 = try Font(pdf, CoreFont.HELVETICA)

        // Chinese (Traditional) font
        // Uses Adobe's Ming Standard Light font (明體)
        let f1 = Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT)

        // Chinese (Simplified) font
        // Uses Adobe's Heiti SC Light font (黑体-简)
        let f2 = Font(pdf, CJKFont.ST_HEITI_SC_LIGHT)

        // Japanese font
        // Uses Kozuka Mincho Pro VI Regular font (小塚明朝)
        let f3 = Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR)

        // Korean font
        // Uses Adobe's Myungjo Standard Medium font (명조체)
        let f4 = Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f0, "Happy New Year!")
        text.setFontSize(26.0)
        text.setLocation(70.0, 90.0)
        text.drawOn(page)

        text = TextLine(f5, "In four languages, with CJK fonts that are not embedded in this PDF.")
        text.setFontSize(11.0)
        text.setTextColor(Color.gray)
        text.setLocation(70.0, 112.0)
        text.drawOn(page)

        let languages = [
            "Chinese (Traditional)",
            "Chinese (Simplified)",
            "Japanese",
            "Korean",
        ]
        let fontNames = [
            "Adobe Ming Std Light",
            "STHeiti SC Light",
            "Kozuka Mincho Pro VI Regular",
            "Adobe Myungjo Std Medium",
        ]
        let greetings = [
            "新年快樂!",
            "新年快乐!",
            "明けましておめでとう!",
            "새해 복 많이 받으세요!",
        ]
        let fonts = [f1, f2, f3, f4]

        var y: Float = 170.0
        for i in 0..<languages.count {
            text = TextLine(f0, languages[i])
            text.setFontSize(12.0)
            text.setLocation(70.0, y)
            text.drawOn(page)

            text = TextLine(f5, fontNames[i])
            text.setFontSize(10.0)
            text.setTextColor(Color.gray)
            text.setLocation(70.0, y + 15.0)
            text.drawOn(page)

            text = TextLine(fonts[i], greetings[i])
            text.setFontSize(32.0)
            text.setLocation(70.0, y + 60.0)
            text.drawOn(page)

            let line = Line(70.0, y + 80.0, 540.0, y + 80.0)
            line.setStrokeColor(Color.lightgray)
            line.drawOn(page)

            y += 115.0
        }

        try pdf.complete()
    }
}   // End of Example_04.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_04()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_04 => \(String(format: "%4lld", time1 - time0)) ms")
