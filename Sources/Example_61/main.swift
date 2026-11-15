/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_61.swift
 * A timesheet of one week, filled in: who worked, the week and the project,
 * a table of the seven days with the hours and the work done on each, the
 * total and the overtime, and the lines where the employee and the manager
 * sign.
 *
 * This is the timesheet as the manager receives it, a finished document: the
 * hours come from a time tracking application, and the total and the
 * overtime are computed from them, not typed in. The hours are Decimal:
 * a tenth of an hour, six minutes, is not exact in a Double, and a sum of
 * them can come out as 7.499999. The dates of the days are worked out from
 * the first day of the week.
 *
 * The days are a Table: a header row, a row for each day, and a footer row
 * with the total, in the header's style. Table wraps a long description of
 * the work in its column and makes the row as tall as it needs; a day of no
 * work keeps its row, empty, so that the week reads from Monday to Sunday.
 *
 * The document is PDF/UA-1: the title is a heading, the days a table that a
 * screen reader reads by its column headers, and the rules and the lines to
 * sign on are artifacts. The layout is that of the timesheet template of
 * PDFjet Forms. The people and the companies are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_61 {
    // The data of the timesheet. In an application it comes from the time
    // tracking: the hours and the work of each day, from Monday on.
    static let COMPANY = "Northwind Studio"
    static let EMPLOYEE = "Jordan Ellis"
    static let PROJECT = "Contoso Books, the new website"
    static let WEEK_START = date(2026, 11, 9)     // A Monday
    static let REGULAR_HOURS = Decimal(string: "40")!

    static let DAYS = [
        ["8", "The notes of the interviews at the shop written up for the team"],
        ["8.5", "The site map, first draft, from what the shop's team said: how the orders "
                + "come in today, which titles sell online, and what the catalog needs"],
        ["7.5", "The site map revised; wireframes of the home and title pages"],
        ["9", "Wireframes for phones; the order pages"],
        ["8", "Review with the client by video call, notes for the design phase"],
        ["2.5", "The list of photos of the new titles, for the shoot"],
        ["", ""],
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d
    static let INK: Int32 = 0x333333

    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0            // Letter, 612 wide, less the margins

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_61.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_61.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Timesheet, " + Example_61.EMPLOYEE + ", week of "
                + Example_61.format(Example_61.WEEK_START, "yyyy-MM-dd"))
        pdf.setAuthor(Example_61.EMPLOYEE)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        let left = Example_61.LEFT
        let width = Example_61.WIDTH

        // The total and the overtime, from the hours of the days
        var total = Decimal(0)
        for day in Example_61.DAYS {
            if !day[0].isEmpty {
                total += Decimal(string: day[0])!
            }
        }
        let overtime = max(total - Example_61.REGULAR_HOURS, Decimal(0))

        let page = Page(pdf, Letter.PORTRAIT)

        // The title, the company, and a rule
        let title = TextLine(semiBold, "Timesheet")
        title.setStructureType(StructElem.H1)
        title.setFontSize(20.0)
        title.setTextColor(Example_61.BLUE)
        title.setLocation(left, 64.0)
        title.drawOn(page)
        let company = TextLine(semiBold, Example_61.COMPANY)
        company.setLocation(left + width - company.getWidth(), 64.0)
        company.drawOn(page)
        Example_61.rule(page, 80.0)

        // Who, which week, and for what: a label and its value on each line
        let longDate = "MMMM d, yyyy"
        var y = Example_61.detail(page, semiBold, regular, "Employee", Example_61.EMPLOYEE, 106.0)
        y = Example_61.detail(page, semiBold, regular, "Week",
                Example_61.format(Example_61.WEEK_START, longDate)
                + " to " + Example_61.format(Example_61.plusDays(Example_61.WEEK_START, 6), longDate), y)
        y = Example_61.detail(page, semiBold, regular, "Project", Example_61.PROJECT, y)

        // The days: the name, the date and the hours of each, the work done,
        // and the total under them
        let dayName = "EEEE"
        let shortDate = "MMM d"
        var rows = [[Cell]]()
        rows.append(Example_61.row(semiBold, "Day", "Date", "Hours", "Work done"))
        for i in 0..<Example_61.DAYS.count {
            let date = Example_61.plusDays(Example_61.WEEK_START, i)
            rows.append(Example_61.row(regular, Example_61.format(date, dayName),
                    Example_61.format(date, shortDate),
                    Example_61.hours(Example_61.DAYS[i][0]), Example_61.DAYS[i][1]))
        }
        rows.append(Example_61.row(semiBold, "Total", "", Example_61.hours(total), ""))

        let table = Table()
        table.setTableData(rows, 1)
        table.setNumberOfFooterRows(1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_61.HEADER)
        table.setFooterRowStyle(semiBold, Color.black, Example_61.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_61.BORDER)
        table.setWidth(width)
        table.setColumnWidthsInPercent(16.0, 12.0, 10.0, 62.0)
        table.setTextAlignmentInColumn(2, Alignment.RIGHT)
        table.setLocation(left, y + 18.0)
        y = table.drawOn(page)[1]

        // The regular hours, and the overtime, the hours over them
        y = Example_61.detail(page, semiBold, regular, "Regular",
                Example_61.hours(min(total, Example_61.REGULAR_HOURS)) + " hours", y + 28.0)
        y = Example_61.detail(page, semiBold, regular, "Overtime", Example_61.hours(overtime) + " hours", y)

        // What the signatures confirm, and the lines to sign on, the
        // employee's and the manager's
        TextLine(regular, "I confirm these hours and sign below.")
                .setLocation(left, y + 28.0)
                .drawOn(page)
        y += 90.0
        let signWidth = 0.6 * width - 24.0
        let dateX = left + 0.6 * width
        Example_61.signature(page, regular, "Employee, " + Example_61.EMPLOYEE, left, signWidth, y)
        Example_61.signature(page, regular, "Date", dateX, 0.4 * width, y)
        y += 60.0
        Example_61.signature(page, regular, "Approved by the manager", left, signWidth, y)
        Example_61.signature(page, regular, "Date", dateX, 0.4 * width, y)

        try pdf.complete()
    }

    // Draws a label and its value beside it, the baseline at y, and returns
    // the baseline of the next line.
    private static func detail(
            _ page: Page, _ labelFont: Font, _ font: Font, _ label: String, _ value: String, _ y: Float) -> Float {
        TextLine(labelFont, label).setLocation(LEFT, y).drawOn(page)
        TextLine(font, value).setTextColor(INK).setLocation(LEFT + 80.0, y).drawOn(page)
        return y + 17.0
    }

    // A line to sign on at y, of the width, with what it is for under it. The line is an
    // artifact; its caption is text.
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

    // The hours with two decimals, as 7.50, so that they line up in their
    // column, or nothing for a day of no work.
    private static func hours(_ hours: String) -> String {
        return hours.isEmpty ? "" : Example_61.hours(Decimal(string: hours)!)
    }

    private static func hours(_ hours: Decimal) -> String {
        let hundredths = NSDecimalNumber(decimal: hours * 100).int64Value
        let rest = hundredths % 100
        return String(hundredths / 100) + (rest < 10 ? ".0" : ".") + String(rest)
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
        let line = Line(LEFT, y, LEFT + WIDTH, y)
        line.setStrokeColor(RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }

    // A date, a day of the calendar with no time zone, as a LocalDate is in Java.
    private static func date(_ year: Int, _ month: Int, _ day: Int) -> Date {
        return calendar().date(from: DateComponents(year: year, month: month, day: day))!
    }

    private static func plusDays(_ date: Date, _ days: Int) -> Date {
        return calendar().date(byAdding: .day, value: days, to: date)!
    }

    // The date in the pattern, in English whatever the locale of the
    // computer, as November 9, 2026, or Monday.
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
}   // End of Example_61.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_61()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_61 => \(String(format: "%4lld", time1 - time0)) ms")
