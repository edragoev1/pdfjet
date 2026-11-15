/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_69.swift
 * An expense report of a business trip, filled in: who paid, what for and
 * when, a table of the expenses, the total of each category and the total to
 * pay back, how to pay it, and the lines where the employee and the manager
 * sign.
 *
 * This is the report as the manager receives it, a finished document: the
 * expenses come from an application where each receipt was entered, and the
 * totals are computed from them, not typed in. The amounts are Decimal,
 * never Float or Double, so that they add up to the cent. The totals of the
 * categories are kept with the list of the categories, in the order they
 * first appear, so the summary reads in the order of the trip.
 *
 * The table of the expenses goes on to as many pages as it needs, with its
 * header row on each, as a month of receipts can take more than one page;
 * the totals go under its last row, or on a page of their own when they no
 * longer fit, and each page says "Page 1 of 2" at the bottom.
 *
 * The document is PDF/UA-1: the title is a heading, the expenses a table that
 * a screen reader reads by its column headers, and the rules and the lines to
 * sign on are artifacts. The layout is that of the expense report template of
 * PDFjet Forms. The people and the companies are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_69 {
    // An expense, as on its receipt. The amount is a Decimal, in dollars.
    struct Expense {
        let date: Date
        let category: String
        let description: String
        let amount: Decimal

        init(_ date: String, _ category: String, _ description: String, _ amount: String) {
            self.date = Example_69.parse(date)
            self.category = category
            self.description = description
            self.amount = Decimal(string: amount)!
        }
    }

    // The data of the report. In an application it comes from the database.
    static let COMPANY = "Northwind Studio"
    static let EMPLOYEE = "Jordan Ellis"
    static let PURPOSE = "Discovery interviews at Contoso Books, Toronto"
    static let FROM = date(2026, 11, 2)
    static let TO = date(2026, 11, 4)
    static let CURRENCY = "CAD"
    static let PAY_BACK_BY = "Bank transfer"

    static let EXPENSES = [
        Expense("2026-11-02", "Travel", "Bus, Port Elgin to Kitchener, return", "64.00"),
        Expense("2026-11-02", "Travel", "Train, Kitchener to Toronto Union, return", "86.40"),
        Expense("2026-11-02", "Lodging", "Hotel, two nights, taxes included", "412.30"),
        Expense("2026-11-02", "Meals", "Dinner", "38.75"),
        Expense("2026-11-03", "Meals", "Lunch with the shop's team, four people", "126.20"),
        Expense("2026-11-03", "Supplies", "Sticky notes and markers for the workshop", "23.45"),
        Expense("2026-11-03", "Meals", "Dinner", "41.10"),
        Expense("2026-11-04", "Travel", "Subway fares, three days", "19.80"),
        Expense("2026-11-04", "Meals", "Lunch at the station", "16.85"),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d
    static let INK: Int32 = 0x333333

    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0            // Letter, 612 wide, less the margins
    static let RIGHT: Float = LEFT + WIDTH
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_69.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_69.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Expense report, " + Example_69.EMPLOYEE + ", "
                + Example_69.format(Example_69.FROM, "yyyy-MM-dd"))
        pdf.setAuthor(Example_69.EMPLOYEE)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        let left = Example_69.LEFT
        let width = Example_69.WIDTH
        let right = Example_69.RIGHT

        // The total of each category, and of all, from the expenses. The
        // names of the categories are kept in a list too, in the order they
        // first appear.
        var names = [String]()
        var categories = [String: Decimal]()
        var total = Decimal(0)
        for expense in Example_69.EXPENSES {
            if let sum = categories[expense.category] {
                categories[expense.category] = sum + expense.amount
            } else {
                names.append(expense.category)
                categories[expense.category] = expense.amount
            }
            total += expense.amount
        }

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)

        // The title, the company, and a rule
        let title = TextLine(semiBold, "Expense report")
        title.setStructureType(StructElem.H1)
        title.setFontSize(20.0)
        title.setTextColor(Example_69.BLUE)
        title.setLocation(left, 64.0)
        title.drawOn(page)
        let company = TextLine(semiBold, Example_69.COMPANY)
        company.setLocation(right - company.getWidth(), 64.0)
        company.drawOn(page)
        Example_69.rule(page, 80.0)

        // Who, what for, and when: a label and its value on each line
        let longDate = "MMMM d, yyyy"
        var y = Example_69.detail(page, semiBold, regular, "Employee", Example_69.EMPLOYEE, 106.0)
        y = Example_69.detail(page, semiBold, regular, "Purpose", Example_69.PURPOSE, y)
        Example_69.detail(page, semiBold, regular, "Dates", Example_69.format(Example_69.FROM, longDate)
                + " to " + Example_69.format(Example_69.TO, longDate), y)

        // The expenses: a header row, repeated on every page the table goes on to
        let shortDate = "MMM d"
        var rows = [[Cell]]()
        rows.append(Example_69.row(semiBold, "Date", "Category", "What it was", "Amount"))
        for expense in Example_69.EXPENSES {
            rows.append(Example_69.row(regular, Example_69.format(expense.date, shortDate), expense.category,
                    expense.description, Example_69.money(expense.amount)))
        }

        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_69.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_69.BORDER)
        table.setWidth(width)
        table.setColumnWidthsInPercent(12.0, 16.0, 56.0, 16.0)
        table.setTextAlignmentInColumn(3, Alignment.RIGHT)
        // Under the details on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.setLocation(left, 48.0)
        table.setFirstPageTopMargin(172.0)
        table.setBottomMargin(Example_69.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // The totals and the signatures, under the last row. When they no
        // longer fit on its page, they go on a page of their own.
        y = end[1] + 24.0
        let height = 17.0 * Float(categories.count) + 190.0
        if y + height > page.getHeight() - Example_69.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 64.0
        }
        for name in names {
            Example_69.drawRight(page, regular, 11.0, name + ": " + Example_69.money(categories[name]!), right, y)
            y += 17.0
        }
        Example_69.drawRight(page, semiBold, 14.0,
                "To pay back: " + Example_69.money(total) + " " + Example_69.CURRENCY, right, y + 12.0)
        Example_69.detail(page, semiBold, regular, "Pay back by", Example_69.PAY_BACK_BY, y + 12.0)

        // What the signatures confirm, and the lines to sign on, the
        // employee's and the manager's
        y += 46.0
        TextLine(regular, "I confirm these expenses were for the business and sign below.")
                .setLocation(left, y)
                .drawOn(page)
        y += 52.0
        let signWidth = 0.6 * width - 24.0
        let dateX = left + 0.6 * width
        Example_69.signature(page, regular, "Employee, " + Example_69.EMPLOYEE, left, signWidth, y)
        Example_69.signature(page, regular, "Date", dateX, 0.4 * width, y)
        y += 52.0
        Example_69.signature(page, regular, "Approved by the manager", left, signWidth, y)
        Example_69.signature(page, regular, "Date", dateX, 0.4 * width, y)

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

    // Draws a label and its value beside it, the baseline at y, and returns
    // the baseline of the next line.
    @discardableResult
    private static func detail(
            _ page: Page, _ labelFont: Font, _ font: Font, _ label: String, _ value: String, _ y: Float) -> Float {
        TextLine(labelFont, label).setLocation(LEFT, y).drawOn(page)
        TextLine(font, value).setTextColor(INK).setLocation(LEFT + 90.0, y).drawOn(page)
        return y + 17.0
    }

    // Draws a line of text in the font and the size, its baseline at y and
    // its end at x, as the amounts line up on their right.
    private static func drawRight(
            _ page: Page, _ font: Font, _ size: Float, _ text: String, _ x: Float, _ y: Float) {
        let line = TextLine(font, text)
        line.setFontSize(size)
        line.setLocation(x - line.getWidth(), y)
        line.drawOn(page)
    }

    // A line to sign on at y, of the width, with what it is for under it.
    // The line is an artifact; its caption is text.
    private static func signature(
            _ page: Page, _ font: Font, _ caption: String, _ x: Float, _ width: Float, _ y: Float) {
        let line = Line(x, y, x + width, y)
        line.setStrokeWidth(0.75)
        line.setStrokeColor(INK)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
        TextLine(font, caption)
                .setFontSize(9.0)
                .setTextColor(Color.dimgray)
                .setLocation(x, y + 13.0)
                .drawOn(page)
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: String...) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            row.append(Cell(font, text))
        }
        return row
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private static func rule(_ page: Page, _ y: Float) {
        let line = Line(LEFT, y, RIGHT, y)
        line.setStrokeColor(RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }

    // The amount rounded to the cent, half up, as 0.125 to 0.13.
    private static func rounded(_ amount: Decimal) -> Decimal {
        var value = amount
        var result = Decimal()
        NSDecimalRound(&result, &value, 2, .plain)
        return result
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,034.85. It is put together here, digit by digit, so that it is the
    // same whatever the locale of the computer, where one set to French
    // writes 1 034,85.
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

    // The date of a text as 2026-11-02, as ISO 8601 writes it.
    private static func parse(_ text: String) -> Date {
        return formatter("yyyy-MM-dd").date(from: text)!
    }

    // The date in the pattern, in English whatever the locale of the
    // computer, as November 2, 2026.
    private static func format(_ date: Date, _ pattern: String) -> String {
        return formatter(pattern).string(from: date)
    }

    private static func formatter(_ pattern: String) -> DateFormatter {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = calendar()
        formatter.timeZone = calendar().timeZone
        formatter.dateFormat = pattern
        return formatter
    }

    // The Gregorian calendar in UTC, so that a day is a day wherever the
    // computer is.
    private static func calendar() -> Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(identifier: "UTC")!
        return calendar
    }
}   // End of Example_69.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_69()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_69 => \(String(format: "%4lld", time1 - time0)) ms")
