/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_59.swift
 * A business letter on Letter paper: the letterhead of the sender, the date,
 * the address of the recipient, the subject, the paragraphs of the letter,
 * the closing and the signature block, and a line for the enclosure.
 *
 * Each part is placed under the one before it: drawOn returns where a
 * TextBlock ended, so a longer address or one more paragraph moves
 * everything below it down, and nothing is placed by hand twice. The
 * paragraphs are TextBlocks, which wrap the text to the width of the page,
 * with a blank line between them and no indent.
 *
 * The document is PDF/UA-1: the subject is the heading of the letter, the
 * rule under the letterhead is an artifact, and the language is set, so that
 * a screen reader reads the letter as it is written. The layout is that of
 * the business letter template of PDFjet Forms. The people and the companies
 * are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_59 {
    static let SENDER = "Northwind Studio"
    static let SENDER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"

    static let DATE = date(2026, 9, 25)
    static let RECIPIENT = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4"
    static let SUBJECT = "Your annual report, accessible edition"
    static let SALUTATION = "Dear Ms. Raman,"

    static let PARAGRAPHS = [
        "Thank you for choosing us for this year's annual report. The printed copies are on "
        + "their way, and the accessible PDF edition is on the USB drive enclosed.",

        "The PDF meets the PDF/UA standard, so screen readers read it in the right order, "
        + "with every picture described. If any of your readers has trouble with it, tell us "
        + "and we will fix it at no charge.",

        "Our invoice for the work is enclosed. We look forward to working with you again.",
    ]

    static let CLOSING = "Sincerely,"
    static let SIGNER = "Dana Whitfield\nDesign Lead, Northwind Studio"
    static let ENCLOSURE = "Enclosures: Invoice 2026-042, USB drive"

    static let RULE: Int32 = 0xdde1e8

    // The space between the parts of the letter, and between its paragraphs
    static let GAP: Float = 14.0

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_59.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_59.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle(Example_59.SUBJECT)
        pdf.setAuthor(Example_59.SENDER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)
        let left: Float = 48.0
        let width = page.getWidth() - 2 * left
        let gap = Example_59.GAP

        // The letterhead
        TextLine(semiBold, Example_59.SENDER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        var y = Example_59.paragraph(page, regular, Example_59.SENDER_ADDRESS, left, 80.0, width)

        // A thin rule under the letterhead, decoration only
        y += gap
        let rule = Line(left, y, left + width, y)
        rule.setStrokeColor(Example_59.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The date, written out in words, so that 10/12 is not read as the
        // 10th of December
        let date = Example_59.format(Example_59.DATE, "MMMM d, yyyy")
        y = Example_59.paragraph(page, regular, date, left, y + 2 * gap, width)

        // The recipient, as on the envelope
        y = Example_59.paragraph(page, regular, Example_59.RECIPIENT, left, y + gap, width)

        // The subject, the heading of the letter for a screen reader
        let subject = TextBlock(semiBold, Example_59.SUBJECT)
        subject.setStructureType(StructElem.H1)
        subject.setLocation(left, y + gap)
        subject.setWidth(width)
        y = subject.drawOn(page)[1]

        // The body: the salutation and the paragraphs, a blank line between them
        y = Example_59.paragraph(page, regular, Example_59.SALUTATION, left, y + gap, width)
        for text in Example_59.PARAGRAPHS {
            y = Example_59.paragraph(page, regular, text, left, y + gap, width)
        }

        // The closing, room to sign by hand, and the name under it
        y = Example_59.paragraph(page, regular, Example_59.CLOSING, left, y + gap, width)
        y = Example_59.paragraph(page, regular, Example_59.SIGNER, left, y + 3 * gap, width)

        Example_59.paragraph(page, regular, Example_59.ENCLOSURE, left, y + 2 * gap, width)

        try pdf.complete()
    }

    // Draws the text wrapped to the width, its top at y, and returns the y
    // where it ended, for the next part of the letter.
    @discardableResult
    private static func paragraph(
            _ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float, _ width: Float) -> Float {
        let block = TextBlock(font, text)
        block.setLineSpacing(1.25)
        block.setLocation(x, y)
        block.setWidth(width)
        return block.drawOn(page)[1]
    }

    // A date, a day of the calendar with no time zone, as a LocalDate is in Java.
    private static func date(_ year: Int, _ month: Int, _ day: Int) -> Date {
        return calendar().date(from: DateComponents(year: year, month: month, day: day))!
    }

    // The date in the pattern, in English whatever the locale of the
    // computer, as September 25, 2026.
    private static func format(_ date: Date, _ pattern: String) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = calendar()
        formatter.timeZone = calendar().timeZone
        formatter.dateFormat = pattern
        return formatter.string(from: date)
    }

    // The Gregorian calendar in UTC, so that a day is a day wherever the
    // computer is.
    private static func calendar() -> Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(identifier: "UTC")!
        return calendar
    }
}   // End of Example_59.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_59()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_59 => \(String(format: "%4lld", time1 - time0)) ms")
