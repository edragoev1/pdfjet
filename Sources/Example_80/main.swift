/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_80.swift
 * A proposal of a design studio to a customer, on Letter paper: the studio
 * and the date, the project, who it is for and who wrote it, the goal, a
 * table of the phases of the work with their weeks, a table of the price, the
 * total, and how to go ahead.
 *
 * A proposal is a finished document that the customer receives, not a form.
 * It is read from top to bottom, so each part is placed under the one before
 * it, from where drawOn says the last one ended: a longer goal or one more
 * phase moves everything below it down. Each section is a heading and what
 * follows it, drawn by two small helpers. The weeks and the price are
 * computed from the tables' rows, the price in Decimal, so the totals
 * cannot disagree with the lines above them.
 *
 * The document is PDF/UA-1: the project is the heading of the document and
 * each section a heading under it, so that a screen reader can go from one
 * to the next; the plan and the price are tables that it reads by their
 * column headers. The layout is that of the proposal template of PDFjet
 * Forms. The people and the companies are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_80 {
    static let STUDIO = "Northwind Studio"
    static let STUDIO_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"
    static let DATE = date(2026, 10, 19)
    static let VALID_DAYS = 30

    static let PROJECT = "A new website for Contoso Books"
    static let PREPARED_FOR = "Priya Raman\nContoso Books"
    static let PREPARED_BY = "Dana Whitfield\nDesign Lead, " + STUDIO
    static let GOAL = "A website that shows the new titles each season, lets readers order "
            + "from any phone, and that the shop's team can keep up to date without help."

    // The phases: the name, what the customer gets, and the weeks it takes
    static let PHASES = [
        ["Discovery", "Interviews, a site map, the look in two directions", "2"],
        ["Design", "The pages designed for computers and phones", "3"],
        ["Build", "The site, the catalog and ordering, tested", "4"],
        ["Launch", "Training for your team, a month of support", "1"],
    ]

    // The price, in two parts, in Canadian dollars before the sales tax
    static let PRICES = [
        ["Discovery and design", "4800.00"],
        ["Build and launch", "7200.00"],
    ]
    static let CURRENCY = "CAD"

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0            // Letter, 612 wide, less the margins
    static let RIGHT: Float = LEFT + WIDTH

    private let page: Page
    private let regular: Font
    private let semiBold: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_80.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_80.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Proposal: " + Example_80.PROJECT)
        pdf.setAuthor(Example_80.STUDIO)
        pdf.setLanguage("en-CA")

        regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        // The weeks and the price, from the rows of the tables. The customer
        // pays half when the work starts, the rest at the launch.
        var weeks = 0
        for phase in Example_80.PHASES {
            weeks += Int(phase[2])!
        }
        var total = Decimal(0)
        for price in Example_80.PRICES {
            total += Decimal(string: price[1])!
        }
        let half = total / Decimal(string: "2")!

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_80.LEFT
        let width = Example_80.WIDTH
        let right = Example_80.RIGHT

        // The header: the studio on the left, what the document is and its
        // dates on the right
        TextLine(semiBold, Example_80.STUDIO).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        TextBlock(regular, Example_80.STUDIO_ADDRESS).setLocation(left, 80.0).setWidth(250.0).drawOn(page)
        let kind = TextLine(semiBold, "Proposal")
        kind.setFontSize(26.0)
        kind.setTextColor(Example_80.BLUE)
        kind.setLocation(right - kind.getWidth(), 72.0)
        kind.drawOn(page)
        TextBlock(regular, "Date: " + Example_80.isoDate(Example_80.DATE) + "\nValid until: "
                + Example_80.isoDate(Example_80.plusDays(Example_80.DATE, Example_80.VALID_DAYS)))
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200.0, 94.0)
                .setWidth(200.0)
                .drawOn(page)

        // A thin rule under the header, decoration only, so an artifact
        let rule = Line(left, 148.0, right, 148.0)
        rule.setStrokeColor(Example_80.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The project, the heading of the document, and the two sides of it
        var y = heading(Example_80.PROJECT, StructElem.H1, 18.0, Color.black, 186.0)
        TextLine(semiBold, "Prepared for").setLocation(left, y + 18.0).drawOn(page)
        TextLine(semiBold, "Prepared by").setLocation(left + width / 2.0, y + 18.0).drawOn(page)
        TextBlock(regular, Example_80.PREPARED_FOR).setLocation(left, y + 26.0).setWidth(width / 2.0).drawOn(page)
        y = TextBlock(regular, Example_80.PREPARED_BY)
                .setLocation(left + width / 2.0, y + 26.0)
                .setWidth(width / 2.0)
                .drawOn(page)[1]

        // The goal
        y = heading("The goal", StructElem.H2, 14.0, Example_80.BLUE, y + 30.0)
        y = paragraph(Example_80.GOAL, y)

        // The plan: the phases, and the weeks in all under them
        y = heading("The plan", StructElem.H2, 14.0, Example_80.BLUE, y + 26.0)
        var plan = [[Cell]]()
        plan.append(Example_80.row(semiBold, ["Phase", "What you get", "Weeks"]))
        for phase in Example_80.PHASES {
            plan.append(Example_80.row(regular, phase))
        }
        plan.append(Example_80.row(semiBold, ["In all", "", String(weeks)]))
        y = table(plan, y, 30.0, 55.0, 15.0)

        // The price: the parts, and the total under them
        y = heading("The price", StructElem.H2, 14.0, Example_80.BLUE, y + 26.0)
        var prices = [[Cell]]()
        prices.append(Example_80.row(semiBold, ["Item", "Price"]))
        for price in Example_80.PRICES {
            prices.append(Example_80.row(regular, [price[0], Example_80.money(Decimal(string: price[1])!)]))
        }
        prices.append(Example_80.row(semiBold,
                ["Total, " + Example_80.CURRENCY + ", plus HST", Example_80.money(total)]))
        y = table(prices, y, 70.0, 30.0)

        // How to go ahead
        paragraph("Half, " + Example_80.money(half) + " " + Example_80.CURRENCY
                + ", is invoiced when the work starts, "
                + "and half at the launch. To go ahead, reply to this proposal, and we will set a "
                + "date for the first interview.", y + 18.0)

        try pdf.complete()
    }

    // Draws a heading in the size and the color, its top at y, and returns
    // where the text under it starts.
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ color: Int32, _ y: Float) -> Float {
        let heading = TextLine(semiBold, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(color)
        heading.setLocation(Example_80.LEFT, y + heading.getAscent())
        heading.drawOn(page)
        return y + heading.getAscent() + 8.0
    }

    // Draws a paragraph across the page, its top at y, and returns where it ended.
    @discardableResult
    private func paragraph(_ text: String, _ y: Float) -> Float {
        let block = TextBlock(regular, text)
        block.setLineSpacing(1.15)
        block.setLocation(Example_80.LEFT, y)
        block.setWidth(Example_80.WIDTH)
        return block.drawOn(page)[1]
    }

    // Draws a table across the page, its top at y: a header row, the rows,
    // and a footer row of totals in the header's style, the last column
    // aligned on the right. Returns where it ended.
    private func table(_ rows: [[Cell]], _ y: Float, _ percents: Float...) -> Float {
        let table = Table()
        table.setTableData(rows, 1)
        table.setNumberOfFooterRows(1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_80.HEADER)
        table.setFooterRowStyle(semiBold, Color.black, Example_80.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_80.BORDER)
        table.setWidth(Example_80.WIDTH)
        // Swift passes the percents on to setColumnWidthsInPercent one by
        // one: a variadic parameter takes no array.
        if percents.count == 2 {
            table.setColumnWidthsInPercent(percents[0], percents[1])
        } else {
            table.setColumnWidthsInPercent(percents[0], percents[1], percents[2])
        }
        table.setTextAlignmentInColumn(percents.count - 1, Alignment.RIGHT)
        table.setLocation(Example_80.LEFT, y)
        return table.drawOn(page)[1]
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: [String]) -> [Cell] {
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
    // 12,000.00. It is put together here, digit by digit, so that it is the
    // same whatever the locale of the computer, where one set to French
    // writes 12 000,00.
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

    private static func plusDays(_ date: Date, _ days: Int) -> Date {
        return calendar().date(byAdding: .day, value: days, to: date)!
    }

    // The date as 2026-10-19, as ISO 8601 writes it.
    private static func isoDate(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = calendar()
        formatter.timeZone = calendar().timeZone
        formatter.dateFormat = "yyyy-MM-dd"
        return formatter.string(from: date)
    }

    // The Gregorian calendar in UTC, so that a day is a day wherever the
    // computer is.
    private static func calendar() -> Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(identifier: "UTC")!
        return calendar
    }
}   // End of Example_80.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_80()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_80 => \(String(format: "%4lld", time1 - time0)) ms")
