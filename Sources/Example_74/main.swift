/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_74.swift
 * The minutes of a team meeting, sent to the team after it: when and where
 * it was, who was there and who was not, what was said on each topic of the
 * agenda, the actions agreed, with who does each and by when, and the date
 * of the next meeting, with "Page 1 of 1" at the bottom.
 *
 * This is a document people receive, not a form. It is the record of the
 * meeting of Example_73, whose agenda it follows topic by topic. Minutes
 * have no fixed length, so each part goes on a new page when it no longer
 * fits on the one it is on: a paragraph is measured first, with drawOn of a
 * nil page, which returns where it would end without drawing it. The table
 * of the actions goes on to as many pages as it needs, its header row on
 * each. The pages are kept in a list and added to the document at the end,
 * when the footer can say how many there are.
 *
 * The document is PDF/UA-1: the title, the sections and the topics are
 * headings, and the page numbers are artifacts that a screen reader skips.
 * The layout is that of the meeting minutes template of PDFjet Forms. The
 * studio and the people are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_74 {
    // The data of the meeting. In an application it comes from the notes
    // taken during it.
    static let TEAM = "Northwind Studio"
    static let DATE = date(2026, 11, 12)
    static let TIME_AND_PLACE = "10:00 to 11:00\nMeeting room 2, and by video call"
    static let PEOPLE = "Present: Dana, Sam, Jordan · Absent: Lee · Notes: Sam"
    static let NEXT_MEETING = date(2026, 11, 26)

    // What was said, by topic of the agenda
    static let NOTES = [
        ["Contoso Books website", "Jordan wrote up the interviews at the shop, and the site "
                + "map and the wireframes for computers and phones are done. Contoso Books "
                + "reviews them with us tomorrow; the design phase starts after that."],
        ["New clients", "Two new clients this month, both from the website. A third has "
                + "asked for a quote for a catalog of 32 pages. Lee was away, so Dana calls "
                + "the two new clients in his place."],
        ["Holidays", "The studio is closed from December 24 to January 2. Work due in that "
                + "week is moved to the week before; Sam tells the clients."],
    ]

    // The actions agreed: what, who, and by when
    static let ACTIONS = [
        ["Send the notes of the review to the team", "Jordan", "2026-11-16"],
        ["Send the quote for the catalog", "Dana", "2026-11-14"],
        ["Call the two new clients", "Dana", "2026-11-19"],
        ["Tell clients of the holiday closing", "Sam", "2026-12-01"],
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0
    static let TOP: Float = 62.0
    // The text ends this far from the bottom of the page, above the footer
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    private let pdf: PDF
    private var pages = [Page]()
    private var page: Page!
    private let regular: Font
    private let semiBold: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_74.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_74.pdf for writing")
        }
        pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Team meeting, " + Example_74.format(Example_74.DATE, "yyyy-MM-dd") + ": minutes")
        pdf.setAuthor(Example_74.TEAM)
        pdf.setLanguage("en-CA")

        regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        newPage()

        let left = Example_74.LEFT
        let width = Example_74.WIDTH

        // The title, when and where, and a rule under them
        var y = heading("Team meeting: minutes", StructElem.H1, 18.0, Example_74.TOP)
        let date = Example_74.format(Example_74.DATE, "EEEE, MMMM d, yyyy")
        y = paragraph(date + " · " + Example_74.TIME_AND_PLACE, y) + 12.0
        let rule = Line(left, y, left + width, y)
        rule.setStrokeColor(Example_74.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        y = paragraph(Example_74.PEOPLE, y + 12.0)

        // What was said: a heading for each topic and the notes under it
        y = heading("What was said", StructElem.H2, 14.0, y + 30.0)
        for note in Example_74.NOTES {
            y = heading(note[0], StructElem.H3, 11.0, y + 18.0)
            y = paragraph(note[1], y)
        }

        // The actions, a table that goes on to the next pages when it must
        y = heading("Actions", StructElem.H2, 14.0, y + 30.0)
        var rows = [[Cell]]()
        rows.append(Example_74.row(semiBold, ["Action", "Who", "By"]))
        for action in Example_74.ACTIONS {
            rows.append(Example_74.row(regular, action))
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_74.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_74.BORDER)
        table.setWidth(width)
        table.setColumnWidthsInPercent(60.0, 20.0, 20.0)
        table.setLocation(left, Example_74.TOP)
        table.setFirstPageTopMargin(y)
        table.setBottomMargin(Example_74.FOOTER_ROOM)
        y = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)![1]
        page = pages[pages.count - 1]

        let next = Example_74.format(Example_74.NEXT_MEETING, "EEEE, MMMM d, yyyy")
        paragraph("Next meeting: " + next + ", at 10:00.", y + 18.0)

        // The footer, on every page, now that their number is known
        for i in 0..<pages.count {
            let footer = TextLine(regular, "Page " + String(i + 1) + " of " + String(pages.count))
            footer.setFontSize(10.0)
            footer.setLocation(left + width - footer.getWidth(), page.getHeight() - 52.0)
            pages[i].addArtifactBMC()
            footer.drawOn(pages[i])
            pages[i].addEMC()
        }
        pdf.addPages(pages)
        try pdf.complete()
    }

    // Starts a page, kept in the list until the end.
    private func newPage() {
        page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
    }

    // Draws a heading in blue, its baseline at y, and returns where the text
    // under it starts. A heading with no room for a line under it goes on to
    // a new page, so that it is never left alone at the bottom.
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        var y = y
        if y + 40.0 > page.getHeight() - Example_74.FOOTER_ROOM {
            newPage()
            y = Example_74.TOP
        }
        let heading = TextLine(semiBold, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_74.BLUE)
        heading.setLocation(Example_74.LEFT, y)
        heading.drawOn(page)
        return y + 8.0
    }

    // Draws the text wrapped to the width of the page, its top at y, on a new
    // page when it does not fit, and returns where it ended.
    @discardableResult
    private func paragraph(_ text: String, _ y: Float) -> Float {
        let block = TextBlock(regular, text)
        block.setLineSpacing(1.15)
        block.setWidth(Example_74.WIDTH)
        block.setLocation(Example_74.LEFT, y)
        if block.drawOn(nil)[1] > page.getHeight() - Example_74.FOOTER_ROOM {
            newPage()
            block.setLocation(Example_74.LEFT, Example_74.TOP)
        }
        return block.drawOn(page)[1]
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: [String]) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            row.append(Cell(font, text))
        }
        return row
    }

    // A date, a day of the calendar with no time zone, as a LocalDate is in Java.
    private static func date(_ year: Int, _ month: Int, _ day: Int) -> Date {
        return calendar().date(from: DateComponents(year: year, month: month, day: day))!
    }

    // The date in the pattern, in English whatever the locale of the
    // computer, as Thursday, November 12, 2026.
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
}   // End of Example_74.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_74()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_74 => \(String(format: "%4lld", time1 - time0)) ms")
