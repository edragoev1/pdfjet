/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_56.swift
 * A statement of account of a small design studio to a customer, on Letter
 * paper: the studio, the date and the period at the top, the customer, a
 * table of the invoices, credit notes and payments of the period with the
 * balance after each, the balance due, and when to pay it.
 *
 * A statement is a finished document that the customer receives, not a
 * form. The balance of each line is computed from the one before it, the
 * charges added and the credits taken off, starting from the balance brought
 * forward from the period before; the balance due is the balance of the last
 * line. The amounts are Decimal, so that they add up to the cent over any
 * number of lines, and the table goes on to as many pages as it needs, with
 * its header row on each.
 *
 * The lines are those of Example_53, the invoice, and Example_67, the credit
 * note against it, with a payment and a later invoice. Both sides keep a
 * statement with their books, so the document is PDF/A-3a and PDF/UA-1 at
 * once. The layout is that of the statement of account template of PDFjet
 * Forms. The companies and the numbers are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_56 {
    // A line of the statement: a charge, such as an invoice, or a credit,
    // such as a payment or a credit note. One of the two is zero.
    struct Entry {
        let date: Date
        let description: String
        let charge: Decimal
        let credit: Decimal

        init(_ date: Date, _ description: String, _ charge: String, _ credit: String) {
            self.date = date
            self.description = description
            self.charge = Decimal(string: charge)!
            self.credit = Decimal(string: credit)!
        }
    }

    // The data of the statement. In an application it comes from the books.
    static let PERIOD_START = date(2026, 9, 1)
    static let DATE = date(2026, 10, 31)
    static let PAYMENT_DAYS = 30
    static let CURRENCY = "CAD"
    static let BALANCE_FORWARD = Decimal(string: "0.00")!

    static let SELLER = "Northwind Studio"
    static let SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
    static let CUSTOMER = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4"

    static let ENTRIES = [
        Entry(date(2026, 9, 25), "Invoice 2026-042, annual report", "2495.61", "0"),
        Entry(date(2026, 10, 5), "Credit note CN-2026-007", "0", "67.80"),
        Entry(date(2026, 10, 20), "Payment, thank you", "0", "1000.00"),
        Entry(date(2026, 10, 28), "Invoice 2026-051, display stand", "452.00", "0"),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_56.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_56.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1)
        pdf.setTitle("Statement of account, " + Example_56.isoDate(Example_56.DATE))
        pdf.setAuthor(Example_56.SELLER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let left = Example_56.MARGIN
        let right = page.getWidth() - Example_56.MARGIN

        // The header: the studio on the left, the title and the dates on the right
        TextLine(semiBold, Example_56.SELLER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        TextBlock(regular, Example_56.SELLER_ADDRESS).setLocation(left, 80.0).setWidth(250.0).drawOn(page)

        let title = TextLine(semiBold, "Statement")
        title.setStructureType(StructElem.H1)
        title.setFontSize(26.0)
        title.setTextColor(Example_56.BLUE)
        title.setLocation(right - title.getWidth(), 72.0)
        title.drawOn(page)

        TextBlock(regular, "Date: " + Example_56.isoDate(Example_56.DATE)
                + "\nPeriod: " + Example_56.isoDate(Example_56.PERIOD_START)
                + " to " + Example_56.isoDate(Example_56.DATE))
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 250.0, 94.0)
                .setWidth(250.0)
                .drawOn(page)

        // A thin rule under the header, decoration only, so an artifact
        let rule = Line(left, 148.0, right, 148.0)
        rule.setStrokeColor(Example_56.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The customer
        TextLine(semiBold, "Account").setStructureType(StructElem.H2).setFontSize(12.0)
                .setLocation(left, 182.0).drawOn(page)
        var y = TextBlock(regular, Example_56.CUSTOMER).setLocation(left, 192.0).setWidth(250.0)
                .drawOn(page)[1] + 24.0

        // The lines, each with the balance after it. The first line is the
        // balance brought forward, so that the column adds up from the top.
        var rows = [[Cell]]()
        rows.append(Example_56.row(semiBold, "Date", "Description", "Charges", "Credits", "Balance"))
        var balance = Example_56.BALANCE_FORWARD
        rows.append(Example_56.row(regular, Example_56.isoDate(Example_56.PERIOD_START),
                "Balance forward", "", "", Example_56.money(balance)))
        for entry in Example_56.ENTRIES {
            balance = balance + entry.charge - entry.credit
            rows.append(Example_56.row(regular, Example_56.isoDate(entry.date), entry.description,
                    Example_56.blankIfZero(entry.charge), Example_56.blankIfZero(entry.credit),
                    Example_56.money(balance)))
        }

        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_56.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_56.BORDER)
        table.setWidth(right - left)
        table.setColumnWidthsInPercent(16.0, 36.0, 16.0, 16.0, 16.0)
        for column in 2..<5 {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT)
        }
        table.setLocation(left, Example_56.MARGIN)
        table.setFirstPageTopMargin(y)
        table.setBottomMargin(Example_56.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // The balance due and when to pay it, under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 30.0
        if y + 70.0 > page.getHeight() - Example_56.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 64.0
        }
        let due = TextLine(semiBold, "Balance due: " + Example_56.money(balance) + " " + Example_56.CURRENCY)
        due.setFontSize(14.0)
        due.setLocation(right - due.getWidth(), y)
        due.drawOn(page)

        TextBlock(regular, "Please pay the balance by "
                + Example_56.isoDate(Example_56.plusDays(Example_56.DATE, Example_56.PAYMENT_DAYS))
                + ", by bank transfer. If a payment has crossed with this statement, thank you, "
                + "and please disregard it.")
                .setLineSpacing(1.15)
                .setLocation(left, y + 22.0)
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

    // A line has a charge or a credit, and the other column is left empty
    // rather than 0.00, so that the eye finds the amounts.
    private static func blankIfZero(_ amount: Decimal) -> String {
        return amount.isZero ? "" : money(amount)
    }

    // The amount rounded to the cent, half up, as 0.125 to 0.13.
    private static func rounded(_ amount: Decimal) -> Decimal {
        var value = amount
        var result = Decimal()
        NSDecimalRound(&result, &value, 2, .plain)
        return result
    }

    // The amount with two decimals and commas between the thousands, as
    // 2,495.61, whatever the locale of the computer: it is put together
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

    // The date as 2026-10-31, as ISO 8601 writes it.
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
}   // End of Example_56.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_56()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_56 => \(String(format: "%4lld", time1 - time0)) ms")
