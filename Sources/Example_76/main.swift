/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_76.swift
 * An order form of a small shop, to print and fill in by hand: who orders,
 * a table of the items with their prices and empty cells for the size, the
 * quantity and the amount, lines for the subtotal, the sales tax and the
 * total, where to deliver, and a box to sign in.
 *
 * This is a form people fill in, not a document with data. The shop prints
 * what it knows, the items and their prices, from Decimal amounts written
 * the same whatever the locale, and leaves the rest empty. The rows of the
 * table are about as tall as a box, to write a number in by hand; the
 * subtotal, the tax and the total are rows of the same table whose label
 * spans the first four columns, so that their empty amounts line up under
 * those of the items. The other fields are drawn with the field helper of
 * Example_72.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * items a table that a screen reader reads by its column headers, and the
 * boxes are artifacts. The layout is that of the order form template of
 * PDFjet Forms. The shop is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_76 {
    // An item of the price list: its name and its price, in dollars
    struct Item {
        let name: String
        let price: Decimal

        init(_ name: String, _ price: String) {
            self.name = name
            self.price = Decimal(string: price)!
        }
    }

    static let SHOP = "Tidewater Print Co."
    static let RETURN_TO = "Mail the form to Tidewater Print Co., 210 Front Street, "
            + "Beaufort, NC 28516, or scan it to orders@example.com."
    static let TAX = "Sales tax 8%"

    static let ITEMS = [
        Item("T-shirt, cotton, printed on the front", "20.00"),
        Item("Hoodie, printed on the back", "45.00"),
        Item("Cap, embroidered", "15.00"),
        Item("Tote bag, canvas", "12.00"),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RED: Int32 = 0xb00020
    static let RULE: Int32 = 0xdde1e8
    static let BOX: Int32 = 0x4d4d4d
    static let HEADER: Int32 = 0xe3edf9

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0
    static let GAP: Float = 12.0
    static let HALF: Float = (WIDTH - GAP) / 2.0

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    private let page: Page
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_76.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_76.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Order form")
        pdf.setAuthor(Example_76.SHOP)
        pdf.setLanguage("en-US")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_76.LEFT
        let width = Example_76.WIDTH
        let half = Example_76.HALF
        let gap = Example_76.GAP

        // The title, what happens to the order, and a rule
        heading("Order form", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Write how many of each item you want, and "
                + "the amounts. Sign the form and send it to us: we confirm every order by "
                + "email within one working day.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_76.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // Your details
        y = heading("Your details", StructElem.H2, 14.0, y + 46.0)
        field("Name *", left, y, half, 1, nil)
        y = field("Email *", left + half + gap, y, half, 1, nil)
        field("Company", left, y, half, 1, nil)
        y = field("Phone", left + half + gap, y, half, 1, nil)

        // The order: the items with their prices, and empty cells to write in
        y = heading("Your order", StructElem.H2, 14.0, y + 18.0)
        var rows = [[Cell]]()
        rows.append(Example_76.row(semiBold, "Item", "Price", "Size, color", "Qty", "Amount"))
        for item in Example_76.ITEMS {
            rows.append(Example_76.row(textFont, item.name, Example_76.money(item.price), "", "", ""))
        }
        rows.append(Example_76.totalRow(textFont, "Subtotal"))
        rows.append(Example_76.totalRow(textFont, Example_76.TAX))
        rows.append(Example_76.totalRow(semiBold, "Total"))

        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_76.HEADER)
        // Rows about as tall as a box, to write in
        table.setCellPadding(5.0, 6.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_76.BOX)
        table.setWidth(width)
        table.setColumnWidthsInPercent(42.0, 13.0, 20.0, 10.0, 15.0)
        table.setTextAlignmentInColumn(1, Alignment.RIGHT)
        table.setLocation(left, y)
        y = table.drawOn(page)[1] + 12.0

        // Where to deliver: the address beside the date
        y = heading("Delivery", StructElem.H2, 14.0, y + 18.0)
        let wide = 0.6 * width - gap / 2.0
        field("Delivery date", left + wide + gap, y, width - wide - gap, 1, nil)
        y = field("Delivery address *", left, y, wide, 3, nil)

        // The confirmation, and the signature under it
        CheckBox(textFont, "I confirm this order and sign below. *")
                .setBorderColor(Example_76.BOX)
                .setLocation(left, y)
                .drawOn(page)
        y += 30.0
        field("Signature *", left, y, wide, 1, nil)
        field("Date", left + wide + gap, y, width - wide - gap, 1, nil)

        // Where the form goes, at the foot of the page
        let returnTo = TextLine(textFont, Example_76.RETURN_TO)
        returnTo.setFontSize(9.0)
        returnTo.setTextColor(Color.dimgray)
        returnTo.setLocation(left, page.getHeight() - 36.0)
        returnTo.drawOn(page)

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

    // A row of the totals: the label across the first four columns, on the
    // right, and an empty amount. The cells under the span stay in the row.
    private static func totalRow(_ font: Font, _ label: String) -> [Cell] {
        let row = row(font, label, "", "", "", "")
        row[0].setColSpan(4)
        row[0].setTextAlignment(Alignment.RIGHT)
        return row
    }

    // The amount rounded to the cent, half up, as 0.125 to 0.13.
    private static func rounded(_ amount: Decimal) -> Decimal {
        var value = amount
        var result = Decimal()
        NSDecimalRound(&result, &value, 2, .plain)
        return result
    }

    // The price with two decimals, as 45.00. It is put together here, digit
    // by digit, so that it is the same whatever the locale of the computer,
    // where one set to French writes 45,00.
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

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_76.BLUE)
        heading.setLocation(Example_76.LEFT, y)
        heading.drawOn(page)
        return y + 16.0
    }

    // Draws the label of a field, its top at y.
    private func label(_ text: String, _ x: Float, _ y: Float) {
        let label = TextLine(labelFont, text)
        label.setLocation(x, y + labelFont.getAscent())
        label.drawOn(page)
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    @discardableResult
    private func field(
            _ text: String, _ x: Float, _ y: Float, _ width: Float, _ lines: Int, _ value: String?) -> Float {
        label(text, x, y)
        let top = y + 16.0
        let height = Example_76.BOX_HEIGHT + Float(lines - 1) * Example_76.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_76.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_76.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_76.LEFT, y, Example_76.LEFT + Example_76.WIDTH, y)
        line.setStrokeColor(Example_76.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_76.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_76()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_76 => \(String(format: "%4lld", time1 - time0)) ms")
