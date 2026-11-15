/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_57.swift
 * A delivery note that goes in the box with the goods, on Letter paper: the
 * studio and the delivery note number at the top, where the goods go and how
 * they travel, a table of what was ordered and what is in this delivery, a
 * note on checking the goods, and a line for the receiver to sign.
 *
 * A delivery note is a finished document that the customer receives, not a
 * form; only its last line is filled in, by hand, at the door. It has no
 * prices, as it is read by whoever opens the box. The table says for each
 * item how many were ordered and how many are in the box, and an item that
 * is short says how many are to follow, computed from the two numbers; the
 * weight of the shipment is computed from the items too. The table
 * goes on to as many pages as it needs, with its header row on each.
 *
 * The goods are the printed copies of the invoice of Example_53, and a
 * display stand that follows them, invoiced when it ships. The document is
 * PDF/UA-1: the title is a heading, the items a table, and the rule and the
 * line to sign on are artifacts. The layout is that of the delivery note
 * template of PDFjet Forms. The companies and the numbers are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_57 {
    // A line of the delivery: how many were ordered, how many are in this
    // delivery, and the weight of one, in kilograms.
    struct Item {
        let description: String
        let ordered: Int
        let delivered: Int
        let kgEach: Double

        init(_ description: String, _ ordered: Int, _ delivered: Int, _ kgEach: Double) {
            self.description = description
            self.ordered = ordered
            self.delivered = delivered
            self.kgEach = kgEach
        }
    }

    // The data of the delivery. In an application it comes from the database.
    static let NUMBER = "D-2026-064"
    static let DATE = date(2026, 9, 24)
    static let ORDER = "CB-PO-3310"
    static let BOXES = 2
    static let CARRIER = "Harbourline Courier, tracking HC 4471 2290"
    static let CHECK_DAYS = 5

    static let SELLER = "Northwind Studio"
    static let SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"
    static let DELIVER_TO =
            "Contoso Books\nAttn: Receiving\n400 King Street West\nToronto, ON M5V 1K4"

    static let ITEMS = [
        Item("Annual report, printed copy, saddle stitched", 50, 50, 0.26),
        Item("Display stand for the lobby", 1, 0, 3.8),
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d
    static let GRAY: Int32 = 0x4d4d4d

    static let MARGIN: Float = 48.0
    static let FOOTER_ROOM: Float = 80.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_57.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_57.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Delivery note " + Example_57.NUMBER)
        pdf.setAuthor(Example_57.SELLER)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        // The weight of the shipment, from the items in it, for the carrier
        var kg = 0.0
        for item in Example_57.ITEMS {
            kg += Double(item.delivered) * item.kgEach
        }

        var pages = [Page]()
        var page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        let left = Example_57.MARGIN
        let right = page.getWidth() - Example_57.MARGIN
        let middle = (left + right) / 2.0

        // The header: the studio on the left, the title and the numbers on the right
        TextLine(semiBold, Example_57.SELLER).setFontSize(16.0).setLocation(left, 64.0).drawOn(page)
        TextBlock(regular, Example_57.SELLER_ADDRESS).setLocation(left, 80.0).setWidth(250.0).drawOn(page)

        let title = TextLine(semiBold, "Delivery note")
        title.setStructureType(StructElem.H1)
        title.setFontSize(26.0)
        title.setTextColor(Example_57.BLUE)
        title.setLocation(right - title.getWidth(), 72.0)
        title.drawOn(page)

        TextBlock(regular, "No. " + Example_57.NUMBER + "\nDate: " + Example_57.isoDate(Example_57.DATE)
                + "\nYour order: " + Example_57.ORDER)
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200.0, 94.0)
                .setWidth(200.0)
                .drawOn(page)

        Example_57.line(page, left, right, 148.0, Example_57.RULE, 1.0)

        // Where the goods go, and how they travel: side by side, the taller
        // of the two decides where the table starts
        Example_57.heading(page, semiBold, "Deliver to", left, 182.0)
        let y1 = TextBlock(regular, Example_57.DELIVER_TO).setLocation(left, 192.0)
                .setWidth(middle - left).drawOn(page)[1]
        Example_57.heading(page, semiBold, "Shipment", middle, 182.0)
        let shipment = String(Example_57.BOXES) + " boxes, "
                + String(format: "%.1f", kg) + " kg\n" + Example_57.CARRIER
        let y2 = TextBlock(regular, shipment).setLocation(middle, 192.0)
                .setWidth(right - middle).drawOn(page)[1]
        var y = max(y1, y2) + 24.0

        // The items: what was ordered, and what is in the box
        var rows = [[Cell]]()
        rows.append(Example_57.row(semiBold, "Item", "Ordered", "Delivered"))
        for item in Example_57.ITEMS {
            var delivered = String(item.delivered)
            if item.delivered < item.ordered {
                delivered += " (" + String(item.ordered - item.delivered) + " to follow)"
            }
            rows.append(Example_57.row(regular, item.description, String(item.ordered), delivered))
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_57.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_57.BORDER)
        table.setWidth(right - left)
        table.setColumnWidthsInPercent(60.0, 15.0, 25.0)
        table.setTextAlignmentInColumn(1, Alignment.RIGHT)
        table.setTextAlignmentInColumn(2, Alignment.RIGHT)
        table.setLocation(left, Example_57.MARGIN)
        table.setFirstPageTopMargin(y)
        table.setBottomMargin(Example_57.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // The note and the line to sign stay together under the last row, or
        // go on a page of their own when they no longer fit there
        y = end[1] + 22.0
        if y + 110.0 > page.getHeight() - Example_57.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 64.0
        }
        y = TextBlock(regular, "Please check the goods on arrival and tell us within "
                + String(Example_57.CHECK_DAYS) + " days if anything is missing or damaged.")
                .setLineSpacing(1.15)
                .setLocation(left, y)
                .setWidth(right - left)
                .drawOn(page)[1]

        // Room to sign by hand above the line
        TextLine(regular, "Received in good order by (name, signature and date):")
                .setLocation(left, y + 30.0).drawOn(page)
        Example_57.line(page, left, left + 0.6 * (right - left), y + 74.0, Example_57.GRAY, 0.75)

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

    // A line across, the rule under the header or the line to sign on, which
    // the text above it names: an artifact that a screen reader skips.
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

    // A date, a day of the calendar with no time zone, as a LocalDate is in Java.
    private static func date(_ year: Int, _ month: Int, _ day: Int) -> Date {
        return calendar().date(from: DateComponents(year: year, month: month, day: day))!
    }

    // The date as 2026-09-24, as ISO 8601 writes it.
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
}   // End of Example_57.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_57()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_57 => \(String(format: "%4lld", time1 - time0)) ms")
