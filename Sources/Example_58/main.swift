/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_58.swift
 * A purchase order of a small design studio to its supplier, on Letter
 * paper: the studio and the order number at the top with the date to deliver
 * by, the vendor and where to ship, a table of the items with the vendor's
 * codes, the subtotal, the sales tax and the total, the terms, and who
 * approved the order.
 *
 * Here the studio is the buyer: the layout of its invoice of Example_53,
 * the parties the other way around. A purchase order is a finished document
 * that the vendor receives, not a form. The amounts are Decimal, computed
 * from the items, and the table goes on to as many pages as it needs.
 *
 * The vendor's invoice is checked against the purchase order, and both are
 * kept for the books, so the document is PDF/A-3a and PDF/UA-1 at once. The
 * layout is that of the purchase order template of PDFjet Forms. The
 * companies, the people and the numbers are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_58 {
    // A line of the order: the vendor's code of the item, so that the vendor
    // picks the right one, and the price the vendor quoted.
    struct Item {
        let description: String
        let code: String
        let quantity: Int
        let unitPrice: Decimal

        init(_ description: String, _ code: String, _ quantity: Int, _ unitPrice: String) {
            self.description = description
            self.code = code
            self.quantity = quantity
            self.unitPrice = Decimal(string: unitPrice)!
        }

        func amount() -> Decimal {
            return unitPrice * Decimal(quantity)
        }
    }

    // The data of the order. In an application it comes from the database.
    static let NUMBER = "2026-017"
    static let DATE = date(2026, 10, 5)
    static let DELIVERY_DAYS = 15
    static let PAYMENT_DAYS = 30
    static let CURRENCY = "CAD"
    static let TAX_NAME = "HST"
    static let TAX_RATE = Decimal(string: "0.13")!
    static let APPROVED_BY = "Dana Whitfield, Design Lead"

    static let BUYER = "Northwind Studio"
    static let BUYER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0"
    static let VENDOR = "Harbour Paper Supply\n88 Dock Road\nOwen Sound, ON N4K 3T2"

    static let ITEMS = [
        Item("Recycled paper, letter, 500 sheets", "PP-2201", 20, "8.50"),
        Item("Kraft envelopes, 9 x 12 in", "EN-0912", 200, "0.35"),
        Item("Shipping boxes, medium", "BX-0400", 50, "1.20"),
        Item("Packing tape, 48 mm x 50 m", "TP-4850", 12, "3.95"),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_58.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_58.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1)
        pdf.setTitle("Purchase order " + Example_58.NUMBER)
        pdf.setAuthor(Example_58.BUYER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        // The totals, from the items, the tax rounded once on the subtotal
        var subtotal = Decimal(0)
        for item in Example_58.ITEMS {
            subtotal += item.amount()
        }
        let tax = Example_58.rounded(subtotal * Example_58.TAX_RATE)
        let total = subtotal + tax

        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let left = Example_58.MARGIN
        let right = page.getWidth() - Example_58.MARGIN
        let middle = (left + right) / 2.0

        // The header: the studio on the left, the title and the numbers on the right
        TextLine(semiBold, Example_58.BUYER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        TextBlock(regular, Example_58.BUYER_ADDRESS + "\nhello@example.com")
                .setLocation(left, 80.0).setWidth(250.0).drawOn(page)

        let title = TextLine(semiBold, "Purchase order")
        title.setStructureType(StructElem.H1)
        title.setFontSize(26.0)
        title.setTextColor(Example_58.BLUE)
        title.setLocation(right - title.getWidth(), 72.0)
        title.drawOn(page)

        TextBlock(regular, "PO No. " + Example_58.NUMBER + "\nDate: " + Example_58.isoDate(Example_58.DATE)
                + "\nDeliver by: "
                + Example_58.isoDate(Example_58.plusDays(Example_58.DATE, Example_58.DELIVERY_DAYS)))
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200.0, 94.0)
                .setWidth(200.0)
                .drawOn(page)

        // A thin rule under the header, decoration only, so an artifact
        let rule = Line(left, 148.0, right, 148.0)
        rule.setStrokeColor(Example_58.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The vendor, and where to ship: side by side, the taller of the two
        // decides where the table starts
        Example_58.heading(page, semiBold, "Vendor", left, 182.0)
        let y1 = TextBlock(regular, Example_58.VENDOR).setLocation(left, 192.0).setWidth(middle - left)
                .drawOn(page)[1]
        Example_58.heading(page, semiBold, "Ship to", middle, 182.0)
        let y2 = TextBlock(regular, Example_58.BUYER + "\n" + Example_58.BUYER_ADDRESS)
                .setLocation(middle, 192.0)
                .setWidth(right - middle).drawOn(page)[1]
        var y = max(y1, y2) + 24.0

        // The items: a header row, repeated on every page
        var rows = [[Cell]]()
        rows.append(Example_58.row(semiBold, "Item", "Code", "Quantity", "Price", "Amount"))
        for item in Example_58.ITEMS {
            rows.append(Example_58.row(regular, item.description, item.code, String(item.quantity),
                    Example_58.money(item.unitPrice), Example_58.money(item.amount())))
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_58.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_58.BORDER)
        table.setWidth(right - left)
        table.setColumnWidthsInPercent(45.0, 15.0, 12.0, 13.0, 15.0)
        for column in 2..<5 {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT)
        }
        table.setLocation(left, Example_58.MARGIN)
        table.setFirstPageTopMargin(y)
        table.setBottomMargin(Example_58.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // The totals, the terms and the approval under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 22.0
        if y + 130.0 > page.getHeight() - Example_58.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 64.0
        }
        let rate = Example_58.percent(Example_58.TAX_RATE)
        Example_58.drawRight(page, regular, 11.0, "Subtotal: " + Example_58.money(subtotal), right, y)
        Example_58.drawRight(page, regular, 11.0,
                Example_58.TAX_NAME + " " + rate + "%: " + Example_58.money(tax), right, y + 15.0)
        Example_58.drawRight(page, semiBold, 14.0,
                "Total: " + Example_58.money(total) + " " + Example_58.CURRENCY, right, y + 43.0)

        y = TextBlock(regular, "Please quote the PO number on your invoice and packing slip. "
                + "Payment within " + String(Example_58.PAYMENT_DAYS) + " days of delivery.")
                .setLineSpacing(1.15)
                .setLocation(left, y + 64.0)
                .setWidth(right - left)
                .drawOn(page)[1]
        TextLine(regular, "Approved by: " + Example_58.APPROVED_BY)
                .setLocation(left, y + 28.0).drawOn(page)

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
}   // End of Example_58.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_58()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_58 => \(String(format: "%4lld", time1 - time0)) ms")
