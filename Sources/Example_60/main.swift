/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_60.swift
 * A certificate of completion of a workshop, on Letter paper turned on its
 * side: a band of color at the top and the bottom, the studio that gives it,
 * the title, the name of the participant on a line, what was completed, and
 * the lines of the instructor's signature and of the date.
 *
 * It is a document people receive, finished: everything on it is centered,
 * so each line is measured and placed half its width left of the middle of
 * the page, and the paragraph of what was completed is a TextBlock centered
 * in a column narrower than the page. The serif faces of IBM Plex give it
 * the look of a printed certificate; the name is the largest text after the
 * title, as it is what the reader looks for.
 *
 * The certificate is drawn by a method that takes the name, so that a
 * certificate for each person of a class is a loop over the list of names, a
 * page each in one document: the fonts are embedded once, however many
 * pages there are, and the file is printed in one go.
 *
 * The document is PDF/UA-1: the title is a heading, the bands and the lines
 * are artifacts, and the language is set. The layout is that of the
 * certificate template of PDFjet Forms. The people and the studio are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_60 {
    static let ISSUER = "Northwind Studio"
    static let TITLE = "Certificate of completion"
    static let NAME = "Robin Brook"
    static let WORKSHOP = "Design with PDF"
    static let DATE = date(2026, 11, 14)
    static let INSTRUCTOR = "Alex Morgan, instructor"

    static let NAVY: Int32 = 0x1d3557
    static let INK: Int32 = 0x333333
    static let GRAY: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let BAND: Float = 16.0              // The height of the bands

    private let sans: Font
    private let serif: Font
    private let serifBold: Font
    private let serifItalic: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_60.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_60.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle(Example_60.TITLE)
        pdf.setAuthor(Example_60.ISSUER)
        pdf.setLanguage("en-CA")

        sans = try Font(pdf, IBMPlexSans.SemiBold)
        serif = try Font(pdf, IBMPlexSerif.Regular)
        serifBold = try Font(pdf, IBMPlexSerif.SemiBold)
        serifItalic = try Font(pdf, IBMPlexSerif.Italic)

        // One certificate. For a class, a page each from the list of names:
        // for name in names {
        //     drawCertificate(Page(pdf, Letter.LANDSCAPE), name)
        // }
        drawCertificate(Page(pdf, Letter.LANDSCAPE), Example_60.NAME)

        try pdf.complete()
    }

    // Draws the certificate of the person on the page.
    private func drawCertificate(_ page: Page, _ name: String) {
        let margin = Example_60.MARGIN
        let width = page.getWidth()
        let inner = width - 2 * margin       // The width between the margins
        let middle = width / 2.0

        // The bands at the top and the bottom. A Rect is decoration, an
        // artifact, on its own.
        Rect(margin, margin, inner, Example_60.BAND).setFillColor(Example_60.NAVY).drawOn(page)
        Rect(margin, page.getHeight() - margin - Example_60.BAND, inner, Example_60.BAND)
                .setFillColor(Example_60.NAVY).drawOn(page)

        // Who gives it, and the title, the heading of the page
        Example_60.center(page, sans, 13.0, Example_60.ISSUER, Example_60.GRAY, 112.0).drawOn(page)
        let title = Example_60.center(page, serifBold, 38.0, Example_60.TITLE, Example_60.NAVY, 170.0)
        title.setStructureType(StructElem.H1)
        title.drawOn(page)

        // The name, on a line as wide as three fifths of the page
        Example_60.center(page, serifItalic, 15.0, "This certifies that", Example_60.INK, 214.0).drawOn(page)
        Example_60.center(page, serifBold, 30.0, name, Example_60.INK, 264.0).drawOn(page)
        let lineWidth = 0.6 * inner
        Example_60.rule(page, middle - lineWidth / 2.0, middle + lineWidth / 2.0, 286.0)

        // What was completed, in a column of seven tenths of the page, each
        // line centered in it
        let date = Example_60.format(Example_60.DATE, "MMMM d, yyyy")
        let columnWidth = 0.7 * inner
        let what = TextBlock(serif, "has completed " + Example_60.WORKSHOP + ", a hands-on workshop "
                + "of one day, on " + date + ", in Port Elgin, Ontario: layouts that print the "
                + "same everywhere, fillable forms, and accessible PDF.")
        what.setFontSize(14.0)
        what.setTextColor(Example_60.INK)
        what.setLineSpacing(1.15)
        what.setTextAlignment(Alignment.CENTER)
        what.setLocation(middle - columnWidth / 2.0, 306.0)
        what.setWidth(columnWidth)
        let y = what.drawOn(page)[1] + 64.0

        // The signature and the date, each on its line, a little over a third
        // of the page wide, in the left and the right half of the page
        let signWidth = 0.35 * inner
        let starts: [Float] = [margin + 0.1 * inner, margin + 0.55 * inner]
        let captions = [Example_60.INSTRUCTOR, date]
        for i in 0..<2 {
            let x = starts[i]
            Example_60.rule(page, x, x + signWidth, y)
            let caption = TextLine(serif, captions[i])
            caption.setFontSize(11.0)
            caption.setTextColor(Example_60.GRAY)
            caption.setLocation(x + (signWidth - caption.getWidth()) / 2.0, y + 22.0)
            caption.drawOn(page)
        }
    }

    // Returns a line of text centered on the page, its baseline at y, for the
    // caller to draw, after it has made it a heading where it is one.
    private static func center(
            _ page: Page, _ font: Font, _ size: Float, _ text: String, _ color: Int32, _ y: Float) -> TextLine {
        let line = TextLine(font, text)
        line.setFontSize(size)
        line.setTextColor(color)
        line.setLocation((page.getWidth() - line.getWidth()) / 2.0, y)
        return line
    }

    // A line to write or sign on, decoration only, so an artifact that a
    // screen reader skips.
    private static func rule(_ page: Page, _ x1: Float, _ x2: Float, _ y: Float) {
        let line = Line(x1, y, x2, y)
        line.setStrokeWidth(0.75)
        line.setStrokeColor(INK)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }

    // A date, a day of the calendar with no time zone, as a LocalDate is in Java.
    private static func date(_ year: Int, _ month: Int, _ day: Int) -> Date {
        return calendar().date(from: DateComponents(year: year, month: month, day: day))!
    }

    // The date in the pattern, in English whatever the locale of the
    // computer, as November 14, 2026.
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
}   // End of Example_60.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_60()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_60 => \(String(format: "%4lld", time1 - time0)) ms")
