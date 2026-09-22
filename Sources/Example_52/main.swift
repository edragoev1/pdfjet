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
 * A whole novel, "The Idiot" by Fyodor Dostoyevsky, set as a book: a title
 * page, and each chapter on new A5 pages that a TextFrame flows onto, with
 * the words in italics and a number on every page. The time it prints is the
 * time PDFjet takes to set the 241,527 words of data/the-idiot.txt.
 */
public class Example_52 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_52.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("The Idiot")
        pdf.setAuthor("Fyodor Dostoyevsky")

        let regular = try Font(pdf, SourceSerif4.Regular)
        let italic = try Font(pdf, SourceSerif4.Italic)
        let semiBold = try Font(pdf, SourceSerif4.SemiBold)
        regular.setSize(10.0)
        italic.setSize(10.0)

        var pages = [Page]()

        // The title page.
        let title = [
            Example_52.centered(TextLine(semiBold, "The Idiot").setFontSize(28.0), StructElem.H1),
            Example_52.centered(TextLine(regular, "Fyodor Dostoyevsky").setFontSize(14.0), StructElem.P),
            Example_52.centered(TextLine(italic, "Translated by Eva Martin"), StructElem.P),
        ]
        TextFrame(title).setLocation(54.0, 200.0).setWidth(312.0).setParagraphGap(16.0)
                .drawOn(pdf, &pages, A5.PORTRAIT)
        let titlePages = pages.count

        // The novel: parts begin with "PART I" and chapters with their number,
        // "I.", and the paragraphs are separated by an empty line.
        var blocks = [String]()
        var lines = [Substring]()
        for line in try Content.ofTextFile("data/the-idiot.txt").split(separator: "\n",
                omittingEmptySubsequences: false) {
            if line.trimmingCharacters(in: .whitespaces).isEmpty {
                if !lines.isEmpty {
                    blocks.append(lines.joined(separator: " "))
                    lines.removeAll()
                }
            } else {
                lines.append(line)
            }
        }
        if !lines.isEmpty {
            blocks.append(lines.joined(separator: " "))
        }
        var chapter: [Paragraph]? = nil
        for block in blocks {
            let text = block.split(whereSeparator: { $0.isWhitespace }).joined(separator: " ")
            if Example_52.isPart(text) {
                try Example_52.drawChapter(pdf, &pages, chapter)
                chapter = [Example_52.centered(TextLine(semiBold, text).setFontSize(16.0), StructElem.H1)]
            } else if Example_52.isChapter(text) {
                if let current = chapter, current.count > 1 {
                    try Example_52.drawChapter(pdf, &pages, current)
                    chapter = []
                }
                chapter!.append(Example_52.centered(TextLine(semiBold, text).setFontSize(14.0), StructElem.H2))
            } else if !text.isEmpty {
                chapter!.append(Example_52.paragraph(text, regular, italic))
            }
        }
        try Example_52.drawChapter(pdf, &pages, chapter)

        // A number at the foot of every page but the title page.
        for i in titlePages..<pages.count {
            let number = TextLine(regular, String(i - titlePages + 1))
            number.setFontSize(9.0)
            _ = pages[i].addFooter(number, 30.0)
        }
        pdf.addPages(pages)
        try pdf.complete()
    }

    private static let romanDigits = Set("IVXL")

    // "PART I" to "PART IV".
    private static func isPart(_ text: String) -> Bool {
        guard text.hasPrefix("PART ") else { return false }
        let number = text.dropFirst(5)
        return !number.isEmpty && number.allSatisfy { romanDigits.contains($0) }
    }

    // A chapter number, "I." to "L.".
    private static func isChapter(_ text: String) -> Bool {
        guard text.hasSuffix(".") else { return false }
        let number = text.dropLast()
        return !number.isEmpty && number.allSatisfy { romanDigits.contains($0) }
    }

    // Draws a chapter on new pages, which it starts at the top of.
    private static func drawChapter(_ pdf: PDF, _ pages: inout [Page], _ chapter: [Paragraph]?) throws {
        if let chapter = chapter {
            TextFrame(chapter).setLocation(54.0, 54.0).setWidth(312.0).setParagraphGap(4.0)
                    .drawOn(pdf, &pages, A5.PORTRAIT)
        }
    }

    private static func centered(_ textLine: TextLine, _ structureType: StructElem) -> Paragraph {
        return Paragraph(textLine).setTextAlignment(Alignment.CENTER).setStructureType(structureType)
    }

    // A justified paragraph of the text, in which _underscores_ mark the
    // words in italics. A word in italics is set in italics whole, with the
    // punctuation around it, since a paragraph puts a space between its text
    // lines.
    private static func paragraph(_ text: String, _ regular: Font, _ italic: Font) -> Paragraph {
        let paragraph = Paragraph().setTextAlignment(Alignment.JUSTIFY)
        var run = ""
        var runItalic = false
        var inItalic = false
        for word in text.split(separator: " ") {
            let wordItalic = inItalic || word.contains("_")
            for ch in word where ch == "_" {
                inItalic = !inItalic
            }
            if !run.isEmpty && wordItalic != runItalic {
                paragraph.add(TextLine(runItalic ? italic : regular, run))
                run = ""
            }
            if !run.isEmpty {
                run += " "
            }
            run += word.replacingOccurrences(of: "_", with: "")
            runItalic = wordItalic
        }
        if !run.isEmpty {
            paragraph.add(TextLine(runItalic ? italic : regular, run))
        }
        return paragraph
    }
}   // End of Example_52.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_52()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_52 => \(String(format: "%4lld", time1 - time0)) ms")
