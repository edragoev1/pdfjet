/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_53.swift
 * Paragraphs written with the inline markup of Markdown: **bold**, *italic*,
 * `code` and [links](url), each drawn in its own font by Markup, with the
 * punctuation after a word in another style next to it. The paragraphs flow
 * in a text frame, and the list is a list of paragraphs with labels.
 */
public class Example_53 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_53.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Inline markup")

        let regular = try Font(pdf, IBMPlexSans.Regular).setSize(11.0)
        let bold = try Font(pdf, IBMPlexSans.Bold).setSize(11.0)
        let italic = try Font(pdf, IBMPlexSans.Italic).setSize(11.0)
        let boldItalic = try Font(pdf, IBMPlexSans.BoldItalic).setSize(11.0)
        let code = try Font(pdf, IBMPlexMono.Regular).setSize(10.0)
        let heading = try Font(pdf, IBMPlexSans.SemiBold)
        let markup = Markup(regular, bold, italic, boldItalic, code)

        var paragraphs = [Paragraph]()
        paragraphs.append(Paragraph(TextLine(heading, "Inline markup").setFontSize(22.0))
                .setStructureType(StructElem.H1))
        paragraphs.append(contentsOf: markup.paragraphs(
                "**Markup** reads the inline markup of Markdown and draws each part in its own "
                + "font: **bold**, *italic*, ***bold italic***, `code` and "
                + "[links](https://pdfjet.com). A word keeps the punctuation after it, as in "
                + "*this*, and a mark with no match, such as the one in 2 * 3, is text.\n"
                + "\n"
                + "A backslash makes a mark text too: \\*not italic\\*. Code keeps its marks "
                + "as they are, as in `a*b*c`, and a link can have emphasis in it: "
                + "[the **PDFjet** repository](https://github.com/edragoev1/pdfjet)."))

        let items = [
            "`Markup.paragraph` makes one paragraph of a text.",
            "`Markup.paragraphs` makes one of each part between the empty lines.",
            "The paragraphs go in a **TextFrame** or a **TextColumn**, as any others do.",
        ]
        for (i, item) in items.enumerated() {
            paragraphs.append(markup.paragraph(item)
                    .setListLabel(TextLine(regular, "\(i + 1)."), 16.0))
        }

        let frame = TextFrame(paragraphs)
        frame.setLocation(70.0, 70.0)
        frame.setWidth(470.0)
        frame.setParagraphGap(8.0)
        var pages = [Page]()
        frame.drawOn(pdf, &pages, Letter.PORTRAIT)
        pdf.addPages(pages)
        try pdf.complete()
    }
}

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_53()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_53 => \(String(format: "%4lld", time1 - time0)) ms")
