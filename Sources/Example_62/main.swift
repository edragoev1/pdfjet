/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_62.swift
 * The price list of a small design studio, on Letter paper: a centered
 * title with the date the prices start, then a section for each kind of
 * work, a heading and a table of the services and their prices, and a note
 * on taxes and quotes at the end.
 *
 * A price list is a finished document that customers receive, not a form.
 * It is several tables, one under the other: each is drawn with
 * drawOn(pdf, page, &pages, ...) starting where the one before it ended, and
 * goes on to the next page when it needs to, with its header row repeated.
 * A heading is not left alone at the foot of a page: when there is no room
 * for it and two rows under it, the section starts on a new page.
 *
 * The document is PDF/UA-1: the title is the H1 and the sections H2, so a
 * screen reader can jump from one section to the next, and the rule and the
 * page numbers are artifacts. The layout is that of the price list template
 * of PDFjet Forms. The company and the prices are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_62 {
    // A kind of work: its heading, and its services with their prices, in
    // dollars before tax.
    struct Section {
        let title: String
        let services: [[String]]

        init(_ title: String, _ services: [String]...) {
            self.title = title
            self.services = services
        }
    }

    static let SELLER = "Northwind Studio"
    static let PRICES_FROM = date(2026, 11, 1)

    static let SECTIONS = [
        Section("Design",
                ["Logo and business card", "450.00"],
                ["Brochure, up to 8 pages", "900.00"],
                ["Poster or flyer", "250.00"],
                ["Design and layout of larger work, per day", "800.00"]),
        Section("Print",
                ["Business cards, 250", "60.00"],
                ["Flyers, 500, color, both sides", "180.00"],
                ["Posters, 11 x 17 in, 25", "95.00"],
                ["Booklets up to 48 pages, saddle stitched, per copy", "6.00"]),
        Section("Accessible PDFs",
                ["PDF/UA edition of a printed piece, per page", "25.00"],
                ["Fillable PDF form, per page", "60.00"]),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let TOP: Float = 64.0           // where the next pages start
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_62.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_62.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Price list, " + Example_62.SELLER)
        pdf.setAuthor(Example_62.SELLER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let left = Example_62.MARGIN
        let right = page.getWidth() - Example_62.MARGIN
        let bottom = page.getHeight() - Example_62.FOOTER_ROOM

        // The title and the date the prices start, centered, and a rule
        let title = TextLine(semiBold, "Price list")
        title.setStructureType(StructElem.H1)
        title.setFontSize(26.0)
        title.setTextColor(Example_62.BLUE)
        title.setLocation((page.getWidth() - title.getWidth()) / 2.0, 72.0)
        title.drawOn(page)

        let from = Example_62.format(Example_62.PRICES_FROM, "MMMM d, yyyy")
        let subtitle = TextLine(regular, Example_62.SELLER + " · Prices from " + from)
        subtitle.setFontSize(12.0)
        subtitle.setLocation((page.getWidth() - subtitle.getWidth()) / 2.0, 96.0)
        subtitle.drawOn(page)

        let rule = Line(left, 116.0, right, 116.0)
        rule.setStrokeColor(Example_62.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The sections, each under the one before it
        var y: Float = 116.0
        for section in Example_62.SECTIONS {
            // The heading and the header row and two rows of the table, about
            // 90 points, or the section starts on a new page
            if y + 90.0 > bottom {
                page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
                pages.append(page)
                y = Example_62.TOP - 36.0
            }
            let heading = TextLine(semiBold, section.title)
            heading.setStructureType(StructElem.H2)
            heading.setFontSize(14.0)
            heading.setTextColor(Example_62.BLUE)
            heading.setLocation(left, y + 36.0)
            heading.drawOn(page)

            var rows = [[Cell]]()
            rows.append(Example_62.row(semiBold, "Service", "Price"))
            for service in section.services {
                rows.append(Example_62.row(regular, service[0], Example_62.money(Decimal(string: service[1])!)))
            }
            let table = Table()
            table.setTableData(rows, 1)
            table.setHeaderRowStyle(semiBold, Color.black, Example_62.HEADER)
            table.setCellPadding(4.0, 3.0)
            table.setCellBorderWidth(0.5)
            table.setCellBorderColor(Example_62.BORDER)
            table.setWidth(right - left)
            table.setColumnWidthsInPercent(70.0, 30.0)
            table.setTextAlignmentInColumn(1, Alignment.RIGHT)
            table.setLocation(left, Example_62.TOP - 16.0)
            table.setFirstPageTopMargin(y + 46.0)
            table.setBottomMargin(Example_62.FOOTER_ROOM)
            y = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)![1]
            page = pages[pages.count - 1]
        }

        // The note on taxes and quotes, under the last table
        if y + 40.0 > bottom {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = Example_62.TOP - 20.0
        }
        TextBlock(regular, "Prices in Canadian dollars, before tax. Ask us for a quote "
                + "for larger work.")
                .setLocation(left, y + 20.0)
                .setWidth(right - left)
                .drawOn(page)

        // The footer, on every page, now that their number is known
        for i in 0..<pages.count {
            let footer = TextLine(regular, "Page " + String(i + 1) + " of " + String(pages.count))
            footer.setFontSize(10.0)
            footer.setLocation(right - footer.getWidth(), page.getHeight() - 52.0)
            pages[i].addArtifactBMC()
            footer.drawOn(pages[i])
            pages[i].addEMC()
        }
        pdf.addPages(pages)
        try pdf.complete()
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: String...) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            row.append(Cell(font, text))
        }
        return row
    }

    // The amount rounded to the cent, half up, as 0.125 to 0.13.
    private static func rounded(_ amount: Decimal) -> Decimal {
        var value = amount
        var result = Decimal()
        NSDecimalRound(&result, &value, 2, .plain)
        return result
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,200.00, whatever the locale of the computer: it is put together
    // here, digit by digit.
    private static func money(_ amount: Decimal) -> String {
        let cents = NSDecimalNumber(decimal: rounded(amount) * 100).int64Value
        let digits = Array(String(abs(cents) / 100))
        var text = cents < 0 ? "-" : ""
        for i in 0..<digits.count {
            if i > 0 && (digits.count - i) % 3 == 0 {
                text += ","
            }
            text.append(digits[i])
        }
        let rest = abs(cents) % 100
        return text + (rest < 10 ? ".0" : ".") + String(rest)
    }

    // A date, a day of the calendar with no time zone, as a LocalDate is in Java.
    private static func date(_ year: Int, _ month: Int, _ day: Int) -> Date {
        return calendar().date(from: DateComponents(year: year, month: month, day: day))!
    }

    // The date in the pattern, in English whatever the locale of the
    // computer, as November 1, 2026.
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
}   // End of Example_62.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_62()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_62 => \(String(format: "%4lld", time1 - time0)) ms")
