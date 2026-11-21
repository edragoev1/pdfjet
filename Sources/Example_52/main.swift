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
 * its language. The Universal Declaration of Human Rights in Hindi, then in
 * Khmer: the paragraphs flow from page to page, a paragraph cut where a page
 * ends. Core Text is Apple's: on Linux the example says so and draws
 * nothing.
 */
#if canImport(CoreText)
public class Example_52 {
    private static let devanagari = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf"
    private static let khmer = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf"
    private static let latin = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

    private static let size: Float = 10.5
    private static let left: Float = 50.0
    private static let width: Float = 512.0
    private static let top: Float = 50.0        // The top margin
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

        let hindi = try Example_52.draw(pdf, heading, body, "Hindi", "hi", Example_52.devanagari,
                "resources/data/udhr/udhr_hin.txt")
        let khmer = try Example_52.draw(pdf, heading, body, "Khmer", "km", Example_52.khmer,
                "resources/data/udhr/udhr_khm.txt")

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        try pdf.attachText("udhr-hindi.txt", hindi)
        try pdf.attachText("udhr-khmer.txt", khmer)

        try pdf.complete()
    }

    // Draws the Declaration in the language, from a new page, on as many
    // pages as it takes, and returns its text.
    private static func draw(_ pdf: PDF, _ heading: Font, _ body: Font, _ name: String,
            _ language: String, _ scriptFile: String, _ textFile: String) throws -> String {
        var page = Page(pdf, Letter.PORTRAIT)
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

        let paragraphs = try String(contentsOfFile: textFile, encoding: .utf8)
                .components(separatedBy: "\n")
                .map { $0.trimmingCharacters(in: .whitespaces) }
                .filter { !$0.isEmpty }
        let text = ComplexText(script, paragraphs)
                .setFallbackFont(latinFont)
                .setLanguage(language)
                .setWidth(width)
                .setParagraphGap(0.5 * size)
        var y: Float = 96.0     // Below the heading, on the first page
        while true {
            text.setHeight(bottom - y).setLocation(left, y).drawOn(page)
            if !(try text.hasMoreText()) {
                break
            }
            page = Page(pdf, Letter.PORTRAIT)
            y = top
        }
        // A blank line between paragraphs, as plain text has them
        return paragraphs.map { $0 + "\n" }.joined(separator: "\n")
    }
}   // End of Example_52.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_52()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_52 => \(String(format: "%4lld", time1 - time0)) ms")
#else
print("Example_52 needs Core Text, Apple's: run it on a Mac.")
#endif
