/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_67.swift
 * A credit note of a small design studio to a customer, on Letter paper: the
 * studio and the credit note number at the top, the customer, the invoice
 * that the credit note corrects and why, a table of what is credited, the
 * subtotal, the sales tax and the total credit, and what happens to it.
 *
 * A credit note is a finished document that the customer receives, not a
 * form. It takes back part of an invoice, here part of the invoice of
 * Example_53, and refers to it by its number and its date, as the tax rules
 * ask, rather than changing an invoice that was already sent. The amounts are
 * positive: the title and the words "Total credit" say which way they go. The
 * sales tax is credited too, at the rate of the invoice, and computed from the
 * items the same way.
 *
 * Both sides keep a credit note with the invoice, so the document is PDF/A-3a
 * and PDF/UA-1 at once. The layout is that of the credit note template of
 * PDFjet Forms. The companies and the numbers are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_67 {
    // A line of the credit note. The unit price is a Decimal, in dollars.
    struct Item {
        let description: String
        let quantity: Int
        let unitPrice: Decimal

        init(_ description: String, _ quantity: Int, _ unitPrice: String) {
            self.description = description
            self.quantity = quantity
            self.unitPrice = Decimal(string: unitPrice)!
        }

        func amount() -> Decimal {
            return unitPrice * Decimal(quantity)
        }
    }

    // The data of the credit note. In an application it comes from the database.
    static let NUMBER = "CN-2026-007"
    static let DATE = date(2026, 10, 5)
    static let INVOICE = "2026-042"
    static let INVOICE_DATE = date(2026, 9, 25)
    static let REASON = "Ten printed copies were damaged in delivery and returned."
    static let CURRENCY = "CAD"
    static let TAX_NAME = "HST"
    static let TAX_RATE = Decimal(string: "0.13")!

    static let SELLER = "Northwind Studio"
    static let SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
    static let CUSTOMER =
            "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4"

    static let ITEMS = [
        Item("Printed copies returned, damaged in delivery", 10, "6.00"),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_67.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_67.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1)
        pdf.setTitle("Credit note " + Example_67.NUMBER)
        pdf.setAuthor(Example_67.SELLER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        // The credit, from the items, the tax rounded once on the subtotal
        var subtotal = Decimal(0)
        for item in Example_67.ITEMS {
            subtotal += item.amount()
        }
        let tax = Example_67.rounded(subtotal * Example_67.TAX_RATE)
        let credit = subtotal + tax

        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let left = Example_67.MARGIN
        let right = page.getWidth() - Example_67.MARGIN
        let middle = (left + right) / 2.0

        // The header: the studio on the left, the title and the numbers on the right
        TextLine(semiBold, Example_67.SELLER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        TextBlock(regular, Example_67.SELLER_ADDRESS).setLocation(left, 80.0).setWidth(250.0).drawOn(page)

        let title = TextLine(semiBold, "Credit note")
        title.setStructureType(StructElem.H1)
        title.setFontSize(26.0)
        title.setTextColor(Example_67.BLUE)
        title.setLocation(right - title.getWidth(), 72.0)
        title.drawOn(page)

        TextBlock(regular, "No. " + Example_67.NUMBER + "\nDate: " + Example_67.isoDate(Example_67.DATE)
                + "\nFor invoice " + Example_67.INVOICE)
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200.0, 94.0)
                .setWidth(200.0)
                .drawOn(page)

        // A thin rule under the header, decoration only, so an artifact
        let rule = Line(left, 148.0, right, 148.0)
        rule.setStrokeColor(Example_67.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The customer, and the invoice corrected and why: side by side, the
        // taller of the two decides where the table starts
        Example_67.heading(page, semiBold, "Credit to", left, 182.0)
        let y1 = TextBlock(regular, Example_67.CUSTOMER).setLocation(left, 192.0).setWidth(middle - left)
                .drawOn(page)[1]
        Example_67.heading(page, semiBold,
                "Invoice " + Example_67.INVOICE + " of " + Example_67.isoDate(Example_67.INVOICE_DATE),
                middle, 182.0)
        let y2 = TextBlock(regular, Example_67.REASON)
                .setLocation(middle, 192.0).setWidth(right - middle).drawOn(page)[1]
        var y = max(y1, y2) + 24.0

        // What is credited: a header row, repeated on every page
        var rows = [[Cell]]()
        rows.append(Example_67.row(semiBold, "Description", "Quantity", "Price", "Amount"))
        for item in Example_67.ITEMS {
            rows.append(Example_67.row(regular, item.description, String(item.quantity),
                    Example_67.money(item.unitPrice), Example_67.money(item.amount())))
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_67.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_67.BORDER)
        table.setWidth(right - left)
        table.setColumnWidthsInPercent(55.0, 15.0, 15.0, 15.0)
        for column in 1..<4 {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT)
        }
        table.setLocation(left, Example_67.MARGIN)
        table.setFirstPageTopMargin(y)
        table.setBottomMargin(Example_67.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // The amounts and what happens to them under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 22.0
        if y + 90.0 > page.getHeight() - Example_67.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 64.0
        }
        let rate = Example_67.percent(Example_67.TAX_RATE)
        Example_67.drawRight(page, regular, 11.0, "Subtotal: " + Example_67.money(subtotal), right, y)
        Example_67.drawRight(page, regular, 11.0,
                Example_67.TAX_NAME + " " + rate + "%: " + Example_67.money(tax), right, y + 15.0)
        Example_67.drawRight(page, semiBold, 14.0,
                "Total credit: " + Example_67.money(credit) + " " + Example_67.CURRENCY,
                right, y + 43.0)

        TextBlock(regular, "This amount is taken off your next invoice, or refunded to you "
                + "on request. We are sorry for the damaged copies.")
                .setLocation(left, y + 64.0)
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

    // Draws the heading of a part, its baseline at y.
    private static func heading(_ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float) {
        TextLine(font, text).setStructureType(StructElem.H2).setFontSize(12.0)
                .setLocation(x, y).drawOn(page)
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

    // The rate as a percent, with no zeros at its end, as 0.13 to 13.
    private static func percent(_ rate: Decimal) -> String {
        return (rate * 100).description
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
}   // End of Example_67.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_67()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_67 => \(String(format: "%4lld", time1 - time0)) ms")
