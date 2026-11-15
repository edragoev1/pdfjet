/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_77.swift
 * A payment reminder, a letter to a customer whose invoice is past due: the
 * letterhead, the date, the address, the invoice as the subject, a friendly
 * paragraph, a small table of what was invoiced, credited and paid, the
 * amount still open, how to pay it, and the signature.
 *
 * A reminder is a finished document that the customer receives, not a form.
 * The letter is placed as the business letter of Example_59 is, each part
 * under the one before it, from where drawOn says the last one ended; the
 * table of the account is one more part, and the paragraphs after it move
 * down when it has more lines. The amount still open is computed from the
 * lines of the account, in Decimal, and is the same figure in the table
 * and in the text, as both come from one variable.
 *
 * The document is PDF/UA-1: the subject is the heading of the letter, the
 * account a table that a screen reader reads by its column headers, and the
 * rule under the letterhead is an artifact. The layout is that of the
 * payment reminder template of PDFjet Forms. The people and the companies
 * are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_77 {
    // A line of the customer's account: an invoice is positive, a credit
    // note and a payment negative.
    struct Entry {
        let date: Date
        let description: String
        let amount: Decimal

        init(_ date: String, _ description: String, _ amount: String) {
            self.date = Example_77.parse(date)
            self.description = description
            self.amount = Decimal(string: amount)!
        }
    }

    static let SENDER = "Northwind Studio"
    static let SENDER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"

    static let DATE = date(2026, 11, 9)
    static let RECIPIENT = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4"
    static let INVOICE = "2026-042"
    static let DUE = date(2026, 10, 25)
    static let CURRENCY = "CAD"
    static let ACCOUNT = "01234-567-8901234"

    static let ENTRIES = [
        Entry("2026-09-25", "Invoice 2026-042, the annual report", "2495.61"),
        Entry("2026-10-05", "Credit note CN-2026-007, ten copies damaged in delivery", "-67.80"),
        Entry("2026-10-20", "Your payment, thank you", "-1000.00"),
    ]

    static let SIGNER = "Dana Whitfield\nDesign Lead, Northwind Studio"

    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0            // Letter, 612 wide, less the margins
    static let GAP: Float = 14.0               // Between the parts of the letter

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_77.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_77.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Payment reminder, invoice " + Example_77.INVOICE)
        pdf.setAuthor(Example_77.SENDER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        // What is still open, from the lines of the account
        var open = Decimal(0)
        for entry in Example_77.ENTRIES {
            open += entry.amount
        }

        let page = Page(pdf, Letter.PORTRAIT)
        let longDate = "MMMM d, yyyy"
        let monthDay = "MMMM d"
        let left = Example_77.LEFT
        let width = Example_77.WIDTH
        let gap = Example_77.GAP

        // The letterhead, and a thin rule under it, decoration only
        TextLine(semiBold, Example_77.SENDER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        var y = Example_77.paragraph(page, regular, Example_77.SENDER_ADDRESS, 80.0) + gap
        let rule = Line(left, y, left + width, y)
        rule.setStrokeColor(Example_77.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The date, the recipient, and the subject, the heading of the letter
        y = Example_77.paragraph(page, regular, Example_77.format(Example_77.DATE, longDate), y + 2 * gap)
        y = Example_77.paragraph(page, regular, Example_77.RECIPIENT, y + gap)
        let subject = TextBlock(semiBold, "Invoice " + Example_77.INVOICE + ", due "
                + Example_77.format(Example_77.DUE, longDate))
        subject.setFontSize(12.0)
        subject.setStructureType(StructElem.H1)
        subject.setLocation(left, y + gap)
        subject.setWidth(width)
        y = subject.drawOn(page)[1]

        y = Example_77.paragraph(page, regular, "Dear Ms. Raman,", y + gap)
        y = Example_77.paragraph(page, regular, "This is a friendly reminder that invoice "
                + Example_77.INVOICE + " of "
                + Example_77.format(Example_77.ENTRIES[0].date, monthDay)
                + ", for the layout of your annual report, was due on "
                + Example_77.format(Example_77.DUE, monthDay) + ". The amount still open is "
                + Example_77.money(open) + " " + Example_77.CURRENCY
                + ", after the credit note and the payment below.", y + gap)

        // The account: what was invoiced, credited and paid, and what is open
        var rows = [[Cell]]()
        rows.append(Example_77.row(semiBold, "Date", "Description", "Amount"))
        for entry in Example_77.ENTRIES {
            rows.append(Example_77.row(regular, Example_77.format(entry.date, "yyyy-MM-dd"),
                    entry.description, Example_77.money(entry.amount)))
        }
        rows.append(Example_77.row(semiBold, "", "Amount open, " + Example_77.CURRENCY, Example_77.money(open)))

        let table = Table()
        table.setTableData(rows, 1)
        table.setNumberOfFooterRows(1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_77.HEADER)
        table.setFooterRowStyle(semiBold, Color.black, Example_77.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_77.BORDER)
        table.setWidth(width)
        table.setColumnWidthsInPercent(18.0, 62.0, 20.0)
        table.setTextAlignmentInColumn(2, Alignment.RIGHT)
        table.setLocation(left, y + gap)
        y = table.drawOn(page)[1]

        y = Example_77.paragraph(page, regular, "Invoice 2026-051 of October 28, also on our statement of "
                + "October 31, is not due until November 27 and is not part of this reminder.", y + gap)
        y = Example_77.paragraph(page, regular, "If you have paid it in the last few days, thank you, and please "
                + "disregard this letter. If anything in the invoice is unclear, just reply to this "
                + "letter and I will be glad to help.", y + gap)
        y = Example_77.paragraph(page, regular, "You can pay by bank transfer to " + Example_77.SENDER
                + ", account " + Example_77.ACCOUNT + ", with the invoice number in the message.", y + gap)

        // The closing, room to sign by hand, and the name under it
        y = Example_77.paragraph(page, regular, "Kind regards,", y + gap)
        Example_77.paragraph(page, regular, Example_77.SIGNER, y + 3 * gap)

        try pdf.complete()
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns the y where it ended, for the next part of the letter.
    @discardableResult
    private static func paragraph(_ page: Page, _ font: Font, _ text: String, _ y: Float) -> Float {
        let block = TextBlock(font, text)
        block.setLineSpacing(1.25)
        block.setLocation(LEFT, y)
        block.setWidth(WIDTH)
        return block.drawOn(page)[1]
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
    // 1,427.81, and a minus sign before a credit. It is put together here,
    // digit by digit, so that it is the same whatever the locale of the
    // computer, where one set to French writes 1 427,81.
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

    // The date of a text as 2026-09-25, as ISO 8601 writes it.
    private static func parse(_ text: String) -> Date {
        return formatter("yyyy-MM-dd").date(from: text)!
    }

    // The date in the pattern, in English whatever the locale of the
    // computer, as November 9, 2026.
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
}   // End of Example_77.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_77()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_77 => \(String(format: "%4lld", time1 - time0)) ms")
