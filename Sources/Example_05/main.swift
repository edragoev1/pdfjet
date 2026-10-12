/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_05.swift
 *
 * Fonts that are not embedded: the twelve text faces of the core fonts, and
 * Chinese, Japanese and Korean in the CJK fonts.
 *
 * The PDF names each font and the viewer supplies it, so the document carries
 * no glyphs: it is a few kilobytes, where an embedded CJK font adds hundreds,
 * and it is written in a moment. The cost: the text takes the shapes of the
 * font the viewer has, so it does not look the same everywhere; the core fonts
 * draw only the characters of Western European languages; the CJK fonts need
 * the Adobe Asian font pack, and draw Latin letters and spaces full width; and
 * a document with a font that is not embedded cannot be PDF/A or PDF/UA. To
 * carry the glyphs with the document, use an embedded font, as Example_02 and
 * Example_03 do.
 *
 * See CoreFont and CJKFont.
 */
public class Example_05 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_05.pdf", append: false)!)

        let title = try Font(pdf, CoreFont.HELVETICA_BOLD)
        title.setSize(22.0)
        let heading = try Font(pdf, CoreFont.HELVETICA_BOLD)
        heading.setSize(13.0)
        let body = try Font(pdf, CoreFont.HELVETICA)
        body.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(title, "Fonts that are not embedded")
        text.setLocation(50.0, 60.0)
        text.drawOn(page)

        var block = TextBlock(body,
                "None of the fonts of this document is embedded: the PDF names each font, "
                + "and the viewer supplies it. That keeps the file very small, a few kilobytes, "
                + "where an embedded Chinese, Japanese or Korean font adds hundreds, and it is "
                + "written in a moment. What it costs is at the bottom of the page.")
        block.setLocation(50.0, 75.0)
        block.setWidth(512.0)
        var end = block.drawOn(page)

        // The twelve text faces of the core fonts, each drawn in itself
        text = TextLine(heading, "The core fonts")
        text.setLocation(50.0, end[1] + 30.0)
        text.drawOn(page)
        let coreFonts = [
            CoreFont.HELVETICA, CoreFont.HELVETICA_BOLD,
            CoreFont.HELVETICA_OBLIQUE, CoreFont.HELVETICA_BOLD_OBLIQUE,
            CoreFont.TIMES_ROMAN, CoreFont.TIMES_BOLD,
            CoreFont.TIMES_ITALIC, CoreFont.TIMES_BOLD_ITALIC,
            CoreFont.COURIER, CoreFont.COURIER_BOLD,
            CoreFont.COURIER_OBLIQUE, CoreFont.COURIER_BOLD_OBLIQUE,
        ]
        let coreNames = [
            "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
            "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
            "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
        ]
        var y = end[1] + 52.0
        for i in 0..<coreFonts.count {
            let font = try Font(pdf, coreFonts[i])
            font.setSize(11.0)
            text = TextLine(font, coreNames[i])
            text.setLocation(50.0 + Float(i % 2) * 256.0, y)
            text.drawOn(page)
            if i % 2 == 1 {
                y += 17.0
            }
        }

        // A greeting in each of the four CJK fonts
        text = TextLine(heading, "The CJK fonts")
        text.setLocation(50.0, y + 20.0)
        text.drawOn(page)
        let cjkFonts = [
            CJKFont.ADOBE_MING_STD_LIGHT,
            CJKFont.ST_HEITI_SC_LIGHT,
            CJKFont.KOZ_MIN_PRO_VI_REGULAR,
            CJKFont.ADOBE_MYUNGJO_STD_MEDIUM,
        ]
        let languages = [
            "Chinese (Traditional), Adobe Ming Std Light",
            "Chinese (Simplified), STHeiti SC Light",
            "Japanese, Kozuka Mincho Pro VI Regular",
            "Korean, Adobe Myungjo Std Medium",
        ]
        let greetings = [
            "新年快樂!",
            "新年快乐!",
            "明けましておめでとう!",
            "새해 복 많이 받으세요!",
        ]
        y += 42.0
        for i in 0..<cjkFonts.count {
            text = TextLine(body, languages[i])
            text.setTextColor(Color.dimgray)
            text.setLocation(50.0, y)
            text.drawOn(page)
            let font = Font(pdf, cjkFonts[i])
            font.setSize(20.0)
            text = TextLine(font, greetings[i])
            text.setLocation(50.0, y + 26.0)
            text.drawOn(page)
            y += 48.0
        }

        // What it gains, and what it costs
        text = TextLine(heading, "Small, at a cost")
        text.setLocation(50.0, y + 8.0)
        text.drawOn(page)
        let mark = try Font(pdf, CoreFont.HELVETICA_BOLD)
        mark.setSize(10.0)
        let marks = ["+", "\u{2013}", "\u{2013}", "\u{2013}", "\u{2013}"]
        let items = [
            "Very small and fast: the document carries no glyphs, where one CJK font holds "
                    + "tens of thousands.",
            "The text takes the shapes and widths of the font the viewer has, so the document "
                    + "does not look the same everywhere.",
            "The core fonts draw only the characters of Western European languages.",
            "The CJK fonts need the Adobe Asian font pack, which Acrobat and Reader offer to "
                    + "download, and draw Latin letters and spaces full width.",
            "A document with a font that is not embedded cannot be PDF/A or PDF/UA, so it is "
                    + "neither archival nor accessible.",
        ]
        y += 26.0
        for i in 0..<items.count {
            text = TextLine(mark, marks[i])
            text.setLocation(52.0, y + 8.0)
            text.drawOn(page)
            block = TextBlock(body, items[i])
            block.setLocation(66.0, y)
            block.setWidth(496.0)
            end = block.drawOn(page)
            y = end[1] + 6.0
        }
        block = TextBlock(body, "To carry its glyphs with the document, use an embedded "
                + "font, as Example_02 and Example_03 do.")
        block.setLocation(50.0, y + 6.0)
        block.setWidth(512.0)
        block.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_05.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_05()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_05 => \(String(format: "%4lld", time1 - time0)) ms")
