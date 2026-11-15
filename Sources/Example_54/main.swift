/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_54.swift
 * A quote of a small design studio to a prospective customer, on Letter
 * paper: the studio and the quote number at the top with the date it is
 * valid until, who it is for and the project, a table of the work in days,
 * the subtotal, the sales tax, the total and the deposit, the terms, and
 * lines for the customer to sign the quote and send it back.
 *
 * A quote is a finished document that the customer receives, not a form.
 * The amounts are Decimal, computed from the lines of work, and the date
 * it is valid until and the deposit are computed too, so that a change of
 * the day rate changes every number that depends on it. The table goes on
 * to as many pages as it needs; what follows it moves to a new page when it
 * no longer fits. A signed quote is a contract that both sides keep, so the
 * document is PDF/A-3a and PDF/UA-1 at once, as the invoice of Example_53
 * is. The layout is that of the quote template of PDFjet Forms. The
 * companies and the numbers are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_54 {
    // A line of the work: so many days at the day rate.
    struct Work {
        let description: String
        let days: Int

        init(_ description: String, _ days: Int) {
            self.description = description
            self.days = days
        }

        func amount() -> Decimal {
            return Example_54.DAY_RATE * Decimal(days)
        }
    }

    // The data of the quote. In an application it comes from the database.
    static let NUMBER = "Q-2026-017"
    static let DATE = date(2026, 10, 5)
    static let VALID_DAYS = 30
    static let CURRENCY = "CAD"
    static let DAY_RATE = Decimal(string: "800.00")!
    static let TAX_NAME = "HST"
    static let TAX_RATE = Decimal(string: "0.13")!
    static let DEPOSIT_RATE = Decimal(string: "0.30")!

    static let SELLER = "Northwind Studio"
    static let SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
    static let CUSTOMER =
            "Tamarack Hill Community Theatre\n27 Mill Street\nOwen Sound, ON N4K 2A1"
    static let PROJECT = "The program book of the 2027 season: its design, "
            + "a layout of 40 pages, and an accessible PDF/UA edition."

    static let WORK = [
        Work("Discovery and a plan of the content", 1),
        Work("Design of the layout and the cover", 3),
        Work("Layout of 40 pages, two rounds of changes", 4),
        Work("Accessible PDF/UA edition, checked page by page", 1),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d
    static let GRAY: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"
    static let GAP: Float = 14.0

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_54.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_54.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1)
        pdf.setTitle("Quote " + Example_54.NUMBER)
        pdf.setAuthor(Example_54.SELLER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        // The totals, from the work. The tax is rounded to the cent once, on
        // the subtotal, and the deposit is a share of the total with tax.
        var subtotal = Decimal(0)
        for work in Example_54.WORK {
            subtotal += work.amount()
        }
        let tax = Example_54.rounded(subtotal * Example_54.TAX_RATE)
        let total = subtotal + tax
        let deposit = Example_54.rounded(total * Example_54.DEPOSIT_RATE)

        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let left = Example_54.MARGIN
        let right = page.getWidth() - Example_54.MARGIN
        let gap = Example_54.GAP

        // The header: the studio on the left, the title and the numbers on the right
        TextLine(semiBold, Example_54.SELLER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        TextBlock(regular, Example_54.SELLER_ADDRESS).setLocation(left, 80.0).setWidth(250.0).drawOn(page)

        let title = TextLine(semiBold, "Quote")
        title.setStructureType(StructElem.H1)
        title.setFontSize(26.0)
        title.setTextColor(Example_54.BLUE)
        title.setLocation(right - title.getWidth(), 72.0)
        title.drawOn(page)

        TextBlock(regular, "No. " + Example_54.NUMBER + "\nDate: " + Example_54.isoDate(Example_54.DATE)
                + "\nValid until: "
                + Example_54.isoDate(Example_54.plusDays(Example_54.DATE, Example_54.VALID_DAYS)))
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200.0, 94.0)
                .setWidth(200.0)
                .drawOn(page)

        Example_54.line(page, left, right, 148.0, Example_54.RULE, 1.0)

        // Who the quote is for, and the project, side by side. The taller of
        // the two decides where the next part starts.
        Example_54.heading(page, semiBold, "Prepared for", left, 182.0)
        let middle = (left + right) / 2.0
        let y1 = Example_54.paragraph(page, regular, Example_54.CUSTOMER, left, 192.0, middle - left - gap)
        Example_54.heading(page, semiBold, "Project", middle, 182.0)
        var y = max(y1, Example_54.paragraph(page, regular, Example_54.PROJECT, middle, 192.0, right - middle))

        y = Example_54.paragraph(page, regular, "Thank you for asking us. Below is what we propose, and "
                + "what it costs. Prices are in Canadian dollars.", left, y + gap, right - left)

        // The work, in a table that goes on to more pages when it needs them
        var rows = [[Cell]]()
        rows.append(Example_54.row(semiBold, "Work", "Days", "Rate", "Amount"))
        for work in Example_54.WORK {
            rows.append(Example_54.row(regular, work.description, String(work.days),
                    Example_54.money(Example_54.DAY_RATE), Example_54.money(work.amount())))
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_54.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_54.BORDER)
        table.setWidth(right - left)
        table.setColumnWidthsInPercent(55.0, 15.0, 15.0, 15.0)
        for column in 1..<4 {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT)
        }
        table.setLocation(left, Example_54.MARGIN)
        table.setFirstPageTopMargin(y + gap)
        table.setBottomMargin(Example_54.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // The totals, the terms and the signature lines stay together: about
        // 300 points. When they no longer fit under the table, they go on a
        // page of their own.
        y = end[1] + 22.0
        if y + 300.0 > page.getHeight() - Example_54.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 64.0
        }
        let rate = Example_54.percent(Example_54.TAX_RATE)
        let share = Example_54.percent(Example_54.DEPOSIT_RATE)
        Example_54.drawRight(page, regular, 11.0, "Subtotal: " + Example_54.money(subtotal), right, y)
        Example_54.drawRight(page, regular, 11.0,
                Example_54.TAX_NAME + " " + rate + "%: " + Example_54.money(tax), right, y + 15.0)
        Example_54.drawRight(page, semiBold, 14.0,
                "Total: " + Example_54.money(total) + " " + Example_54.CURRENCY, right, y + 43.0)
        Example_54.drawRight(page, regular, 11.0,
                "Deposit, " + share + "%: " + Example_54.money(deposit), right, y + 62.0)

        y = Example_54.heading(page, semiBold, "Terms", left, y + 100.0)
        y = Example_54.paragraph(page, regular, "This quote is valid for " + String(Example_54.VALID_DAYS)
                + " days. A deposit "
                + "of " + share + "% starts the work, and the rest is due on delivery. Two rounds "
                + "of changes are included; more are " + Example_54.money(Example_54.DAY_RATE)
                + " a day. The work takes "
                + "about four weeks from the deposit.", left, y, right - left)

        y = Example_54.heading(page, semiBold, "Acceptance", left, y + 2 * gap + 6.0)
        y = Example_54.paragraph(page, regular, "To accept this quote, sign below and send it back to us, "
                + "with the deposit.", left, y, right - left)

        // A line to sign on and one to date, each with its label under it
        let dateX = left + 0.6 * (right - left)
        y += 48.0
        Example_54.line(page, left, dateX - gap, y, Example_54.GRAY, 0.75)
        Example_54.line(page, dateX, right, y, Example_54.GRAY, 0.75)
        TextLine(regular, "Name and signature").setFontSize(10.0).setTextColor(Example_54.GRAY)
                .setLocation(left, y + 14.0).drawOn(page)
        TextLine(regular, "Date").setFontSize(10.0).setTextColor(Example_54.GRAY)
                .setLocation(dateX, y + 14.0).drawOn(page)

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

    // Draws a heading of a part, its baseline at y, and returns where the
    // text under it starts.
    @discardableResult
    private static func heading(
            _ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float) -> Float {
        TextLine(font, text).setStructureType(StructElem.H2).setFontSize(12.0)
                .setLocation(x, y).drawOn(page)
        return y + 10.0
    }

    // Draws the text wrapped to the width, its top at y, and returns the y
    // where it ended.
    private static func paragraph(
            _ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float, _ width: Float) -> Float {
        return TextBlock(font, text).setLineSpacing(1.15).setLocation(x, y).setWidth(width)
                .drawOn(page)[1]
    }

    // A line across, decoration or a line to sign on that its label names,
    // so an artifact that a screen reader skips.
    private static func line(
            _ page: Page, _ x1: Float, _ x2: Float, _ y: Float, _ color: Int32, _ width: Float) {
        let line = Line(x1, y, x2, y)
        line.setStrokeColor(color)
        line.setStrokeWidth(width)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: String...) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            row.append(Cell(font, text))
        }
        return row
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

    // The amount rounded to the cent, half up, as 0.125 to 0.13.
    private static func rounded(_ amount: Decimal) -> Decimal {
        var value = amount
        var result = Decimal()
        NSDecimalRound(&result, &value, 2, .plain)
        return result
    }

    // The rate as a percent, with no zeros at its end, as 0.30 to 30.
    private static func percent(_ rate: Decimal) -> String {
        return (rate * 100).description
    }

    // The amount with two decimals and commas between the thousands, as
    // 2,400.00, whatever the locale of the computer: it is put together
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

    private static func plusDays(_ date: Date, _ days: Int) -> Date {
        return calendar().date(byAdding: .day, value: days, to: date)!
    }

    // The date as 2026-10-05, as ISO 8601 writes it.
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
}   // End of Example_54.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_54()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_54 => \(String(format: "%4lld", time1 - time0)) ms")
