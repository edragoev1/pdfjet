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
 * Embedded fonts: the same words in five weights of IBM Plex Sans, in a PDF/UA
 * document.
 *
 * An embedded font travels with the document, so the text looks the same in
 * every viewer, every character of the font can be drawn, and the document can
 * be PDF/UA and PDF/A. The fourteen core fonts, Helvetica, Times and Courier,
 * are in every viewer and make the smallest documents, but they draw only the
 * WinAnsi characters, and PDF/UA and PDF/A do not allow them.
 *
 * - SeeAlso: `Font`
 * - SeeAlso: `TextBlock`
 */
public class Example_05 {
    private static let SAMPLE = "Embedded fonts look the same in every viewer."

    private static let BACKGROUND: Int32 = 0xf1f4f8

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_05.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Embedded Fonts")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let bold = try Font(pdf, IBMPlexSans.Bold)
        bold.setSize(24.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let title = TextLine(bold, "Embedded Fonts")
        title.setStructureType(StructElem.H1)
        title.setLocation(50.0, 70.0)
        title.drawOn(page)

        let about = TextBlock(regular,
                "An embedded font travels with the document: the PDF carries the font program, "
                + "so the text looks the same in every viewer, every character of the font can be "
                + "drawn, and the document can be PDF/UA and PDF/A. PDFjet comes with the IBM Plex "
                + "fonts, Sans, Serif and Mono, with Arabic, Hebrew, Thai, Japanese, Korean and "
                + "Chinese, in their weights.\n\n"
                + "The fourteen core fonts, Helvetica, Times and Courier with their bold and italic, "
                + "Symbol and ZapfDingbats, are in every viewer, so the document carries no font "
                + "program and is the smallest it can be. But they draw only the characters of "
                + "Windows Latin 1, the viewer draws them with its own version of the font, and "
                + "PDF/UA and PDF/A do not allow them. The boxes below are the same words in five "
                + "weights of IBM Plex Sans.")
        about.setLineSpacing(1.4)
        about.setLocation(50.0, 90.0)
        about.setWidth(512.0)
        var y: Float = about.drawOn(page)[1] + 30.0

        let names = ["Light", "Regular", "Medium", "SemiBold", "Bold"]
        let weights = [
            IBMPlexSans.Light,
            IBMPlexSans.Regular,
            IBMPlexSans.Medium,
            IBMPlexSans.SemiBold,
            IBMPlexSans.Bold,
        ]
        for i in 0..<weights.count {
            let font = try Font(pdf, weights[i])
            font.setSize(20.0)
            y = Example_05.drawSample(page, regular, font, "IBM Plex Sans " + names[i], y) + 22.0
        }

        try pdf.complete()
    }

    // Draws the label, and under it the sample words in the font, in a text
    // block with a light background, and returns the bottom of the block.
    private static func drawSample(
            _ page: Page, _ labelFont: Font, _ font: Font, _ label: String, _ y: Float) -> Float {
        let caption = TextLine(labelFont, label)
        caption.setTextColor(Color.dimgray)
        caption.setLocation(50.0, y)
        caption.drawOn(page)

        let block = TextBlock(font, SAMPLE)
        block.setBackgroundColor(BACKGROUND)
        block.setPadding(10.0)
        block.setLineSpacing(1.2)
        block.setLocation(50.0, y + 8.0)
        block.setWidth(512.0)
        return block.drawOn(page)[1]
    }
}   // End of Example_05.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_05()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_05 => \(String(format: "%4lld", time1 - time0)) ms")
