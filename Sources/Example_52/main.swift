/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_52.swift
 *
 * Draws Hindi and Khmer, scripts PDFjet does not shape itself, with
 * ComplexText: Apple's Core Text shapes them -- conjuncts, vowel signs,
 * stacked consonants -- and PDFjet draws the glyphs it found, at their
 * places, as outlines read from the font file: sharp on screen and in print,
 * and read by screen readers, as each paragraph is tagged with its text and
 * its language. A page of each, filled with the Universal Declaration of
 * Human Rights from its beginning: Hindi on the first page, Khmer on the
 * second. Core Text is Apple's: on Linux the example says so and draws
 * nothing.
 */
#if canImport(CoreText)
public class Example_52 {
    private static let devanagari = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf"
    private static let khmer = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.otf"
    private static let latin = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

    private static let size: Float = 10.5
    private static let left: Float = 50.0
    private static let width: Float = 512.0
    private static let bottom: Float = 742.0    // The page's 792 points less the margin

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_52.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("The Universal Declaration of Human Rights in Hindi and Khmer")    // Required for PDF/UA !
        pdf.setLanguage("en")

        let heading = try Font(pdf, IBMPlexSans.SemiBold)
        heading.setSize(16.0)
        let body = try Font(pdf, IBMPlexSans.Regular)
        body.setSize(9.0)

        let hindi = try Example_52.drawPage(pdf, heading, body, "Hindi", "hi", Example_52.devanagari,
                "resources/data/udhr/udhr_hin.txt")
        let khmer = try Example_52.drawPage(pdf, heading, body, "Khmer", "km", Example_52.khmer,
                "resources/data/udhr/udhr_khm.txt")

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        try pdf.attachText("udhr-hindi.txt", hindi)
        try pdf.attachText("udhr-khmer.txt", khmer)

        try pdf.complete()
    }

    // Draws a page of the Declaration in the language, its paragraphs from
    // the beginning until the page is full, and returns the text drawn.
    private static func drawPage(_ pdf: PDF, _ heading: Font, _ body: Font, _ name: String,
            _ language: String, _ scriptFile: String, _ textFile: String) throws -> String {
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(heading, "Universal Declaration of Human Rights, in " + name)
                .setStructureType(StructElem.H1)
                .setLocation(left, 60.0)
                .drawOn(page)
        TextLine(body, "Shaped by Core Text, drawn by PDFjet's ComplexText as outlines;"
                + " the text is attached to the document.")
                .setLocation(left, 80.0)
                .drawOn(page)

        // The script's font, and IBM Plex Sans for the characters it does
        // not have, such as digits and punctuation of the Latin script
        let script = try Font(pdf, scriptFile).setSize(size)
        let latinFont = try Font(pdf, latin)

        var drawn = ""
        var y: Float = 96.0     // The top of the next paragraph
        let paragraphs = try String(contentsOfFile: textFile, encoding: .utf8)
                .components(separatedBy: "\n")
                .map { $0.trimmingCharacters(in: .whitespaces) }
                .filter { !$0.isEmpty }
        for paragraph in paragraphs {
            let text = ComplexText(script, paragraph)
                    .setFallbackFont(latinFont)
                    .setLanguage(language)
                    .setLocation(left, y)
                    .setWidth(width)
            if y + (try text.getHeight()) > bottom {
                break       // The page is full
            }
            y = text.drawOn(page)[1] + 0.5 * size
            // A blank line between paragraphs, as plain text has them
            if !drawn.isEmpty {
                drawn += "\n"
            }
            drawn += paragraph + "\n"
        }
        return drawn
    }
}   // End of Example_52.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_52()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_52 => \(String(format: "%4lld", time1 - time0)) ms")
#else
print("Example_52 needs Core Text, Apple's: run it on a Mac.")
#endif
