/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_78.swift
 * A pro forma invoice of a small studio to a customer abroad, on Letter
 * paper: the seller, the number and the date until which the prices hold,
 * the address to bill and the address to ship to, a table of the goods, the
 * country of origin and the weight for customs, the total, and the terms.
 *
 * A pro forma invoice is a finished document that the customer receives, not
 * a form. It is sent before the sale: the customer pays it, or takes it to
 * the bank or to customs, and the goods are shipped with the real invoice
 * once the money is in. So it says what it is not, a tax invoice, and how
 * long its prices are good for. Goods exported from Canada carry no sales
 * tax, so the total is the sum of the lines.
 *
 * The amounts are Decimal, never Float or Double, and the total is
 * computed from the goods. The table of the goods goes on to as many pages
 * as it needs, with its header row on each; the total and the terms go
 * under the last row, and each page says "Page 1 of 1" at the bottom.
 *
 * Both sides keep the document, so it is archival as well as accessible,
 * PDF/A-3a and PDF/UA-1 at once. The layout is that of the pro forma
 * invoice template of PDFjet Forms. The companies and the numbers are made
 * up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_78 {
    // A line of the pro forma invoice. The unit price is a Decimal, in dollars.
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

    // The data of the pro forma invoice. In an application it comes from the database.
    static let NUMBER = "PF-2026-012"
    static let DATE = date(2026, 11, 2)
    static let VALID_DAYS = 30
    static let CURRENCY = "CAD"

    static let SELLER = "Northwind Studio"
    static let SELLER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
    static let BILL_TO = "Fabrikam Press\n18 Rue des Lilas\n75011 Paris, France"
    static let SHIP_TO =
            "Fabrikam Press, warehouse\n4 Avenue de l'Europe\n94200 Ivry-sur-Seine, France"
    static let CUSTOMS = "Country of origin: Canada\nWeight: 18 kg, in 2 parcels"

    static let ITEMS = [
        Item("Art prints, A3, numbered edition", 40, "35.00"),
        Item("Archival portfolio boxes", 4, "45.00"),
        Item("Shipping, insured", 1, "120.00"),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_78.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_78.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1)
        pdf.setTitle("Pro forma invoice " + Example_78.NUMBER)
        pdf.setAuthor(Example_78.SELLER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        // The total, from the goods. Exported, they carry no sales tax.
        var total = Decimal(0)
        for item in Example_78.ITEMS {
            total += item.amount()
        }

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let left = Example_78.MARGIN
        let right = page.getWidth() - Example_78.MARGIN
        let half = (right - left) / 2.0
        let validUntil = Example_78.isoDate(Example_78.plusDays(Example_78.DATE, Example_78.VALID_DAYS))

        // The header: the seller on the left, the title and the numbers on the right
        TextLine(semiBold, Example_78.SELLER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        TextBlock(regular, Example_78.SELLER_ADDRESS).setLocation(left, 80.0).setWidth(250.0).drawOn(page)

        let title = TextLine(semiBold, "Pro forma invoice")
        title.setStructureType(StructElem.H1)
        title.setFontSize(22.0)
        title.setTextColor(Example_78.BLUE)
        title.setLocation(right - title.getWidth(), 70.0)
        title.drawOn(page)

        let numbers = "No. " + Example_78.NUMBER
                + "\nDate: " + Example_78.isoDate(Example_78.DATE)
                + "\nValid until: " + validUntil
        TextBlock(regular, numbers)
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200.0, 94.0)
                .setWidth(200.0)
                .drawOn(page)

        // A thin rule under the header, decoration only, so an artifact that a
        // screen reader skips
        let rule = Line(left, 148.0, right, 148.0)
        rule.setStrokeColor(Example_78.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // Who pays, on the left, and where the goods go, on the right
        Example_78.address(page, semiBold, regular, "Bill to", Example_78.BILL_TO, left, half)
        Example_78.address(page, semiBold, regular, "Ship to", Example_78.SHIP_TO, left + half, half)

        // The goods: a header row, repeated on every page the table goes on to
        var rows = [[Cell]]()
        rows.append(Example_78.row(semiBold, "Description", "Quantity", "Price", "Amount"))
        for item in Example_78.ITEMS {
            rows.append(Example_78.row(regular, item.description, String(item.quantity),
                    Example_78.money(item.unitPrice), Example_78.money(item.amount())))
        }

        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_78.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_78.BORDER)
        table.setWidth(right - left)
        table.setColumnWidthsInPercent(55.0, 15.0, 15.0, 15.0)
        for column in 1..<4 {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT)
        }
        // Under the addresses on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.setLocation(left, Example_78.MARGIN)
        table.setFirstPageTopMargin(260.0)
        table.setBottomMargin(Example_78.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // What customs asks for on the left, the total on the right, and the
        // terms under them, all under the last row. When they no longer fit
        // on its page, they go on a page of their own.
        var y = end[1] + 22.0
        if y + 110.0 > page.getHeight() - Example_78.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 64.0
        }
        let totalLine = TextLine(semiBold, "Total: " + Example_78.money(total) + " " + Example_78.CURRENCY)
        totalLine.setFontSize(14.0)
        totalLine.setLocation(right - totalLine.getWidth(), y + 14.0)
        totalLine.drawOn(page)
        y = TextBlock(regular, Example_78.CUSTOMS).setLocation(left, y).setWidth(half).drawOn(page)[1]

        TextBlock(regular, "Not a tax invoice: the goods are shipped when this amount is received, "
                + "by bank transfer, and the invoice is sent with them. Prices are valid until "
                + validUntil + ".")
                .setLocation(left, y + 16.0)
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

    // Draws an address under its heading, in a column at x of the width,
    // under the rule of the header.
    private static func address(_ page: Page, _ headingFont: Font, _ font: Font, _ heading: String,
            _ text: String, _ x: Float, _ width: Float) {
        let line = TextLine(headingFont, heading)
        line.setStructureType(StructElem.H2)
        line.setFontSize(12.0)
        line.setLocation(x, 182.0)
        line.drawOn(page)
        TextBlock(font, text).setLocation(x, 192.0).setWidth(width - 12.0).drawOn(page)
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
    // 1,700.00. It is put together here, digit by digit, so that it is the
    // same whatever the locale of the computer, where one set to French
    // writes 1 700,00.
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

    // The date as 2026-11-02, as ISO 8601 writes it.
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
}   // End of Example_78.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_78()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_78 => \(String(format: "%4lld", time1 - time0)) ms")
