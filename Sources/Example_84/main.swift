/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_84.swift
 * A workshop registration form, to print and fill in by hand: who comes,
 * the sessions of the day with a column to tick the ones they attend, the
 * lunch and the photographs, and how to pay, with a QR code that opens the
 * payment page on a phone.
 *
 * This is a form people fill in, not a document with data, though it carries
 * the program of the day. The fields are drawn as Example_72 draws the job
 * application, with the same field helper: the label above its box, the
 * boxes on a grid, and an asterisk on the label of a field that is required.
 * The sessions are a Table: the organizer prints the time, the title and the
 * room, and leaves the last column empty, a cell to tick.
 *
 * The QR code holds the address of the payment page. It is a figure with a
 * description, which a screen reader reads, and the address is in the text
 * beside it too, for whoever cannot scan it. It is measured before it is placed,
 * with drawOn of a nil page, so that it lines up with the right margin.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * sessions a table read by its column headers, and the boxes are artifacts.
 * The layout is that of the workshop sign-up template of PDFjet Forms. The
 * organizer, the workshop and the address are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_84 {
    static let ORGANIZER = "Lantern Hill Design School"
    static let WORKSHOP = "Better PDFs: a hands-on workshop, Saturday, January 16, "
            + "2027, 9 a.m. to 4 p.m. Fill in this form and send it back to reserve your place. "
            + "Lunch and coffee are included."
    static let PAYMENT_PAGE = "https://example.com/workshops/2027-01-16/pay"

    // The sessions of the day: the time, the title and the room
    static let SESSIONS = [
        ["9 a.m.", "Layouts that print the same everywhere", "A"],
        ["10:30 a.m.", "Forms your clients fill in online", "A"],
        ["1 p.m.", "Barcodes and QR codes", "B"],
        ["2:30 p.m.", "Archival and accessible: PDF/A and PDF/UA", "B"],
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
        guard let output = OutputStream(toFileAtPath: "Example_84.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_84.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Workshop registration")
        pdf.setAuthor(Example_84.ORGANIZER)
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

        let left = Example_84.LEFT
        let width = Example_84.WIDTH
        let half = Example_84.HALF
        let gap = Example_84.GAP

        // The title, the workshop, and a rule
        heading("Workshop registration", StructElem.H1, 20.0, 62.0)
        var y = paragraph(Example_84.WORKSHOP, left, 74.0, width) + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_84.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // Who comes
        y = heading("About you", StructElem.H2, 14.0, y + 46.0)
        field("Full name *", left, y, half, 1, nil)
        y = field("Email *", left + half + gap, y, half, 1, nil)
        field("Company", left, y, half, 1, nil)
        y = field("Phone", left + half + gap, y, half, 1, nil)

        // The sessions, and an empty column to tick the ones they attend
        y = heading("Sessions", StructElem.H2, 14.0, y + 18.0)
        var rows = [[Cell]]()
        rows.append(Example_84.row(semiBold, "Time", "Session", "Room", "I attend"))
        for session in Example_84.SESSIONS {
            rows.append(Example_84.row(textFont, session[0], session[1], session[2], ""))
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_84.HEADER)
        table.setCellPadding(5.0, 5.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_84.BOX)
        table.setWidth(width)
        table.setColumnWidthsInPercent(17.0, 58.0, 10.0, 15.0)
        table.setLocation(left, y)
        y = table.drawOn(page)[1] + 16.0

        // The lunch and the photographs
        CheckBox(textFont, "I need a vegetarian lunch")
                .setBorderColor(Example_84.BOX)
                .setLocation(left, y)
                .drawOn(page)
        CheckBox(textFont, "I agree to be photographed during the workshop")
                .setBorderColor(Example_84.BOX)
                .setLocation(left, y + 20.0)
                .drawOn(page)
        y += 50.0

        // The payment: the text on the left, the QR code of the payment page
        // on the right, its top at the top of the text
        y = heading("Payment", StructElem.H2, 14.0, y + 18.0)
        let qr = try QRCode(Example_84.PAYMENT_PAGE, ErrorCorrectionLevel.M)
        qr.setModuleLength(2.0)
        qr.setAltDescription("QR code of the payment page, " + Example_84.PAYMENT_PAGE)
        let qrSize = qr.drawOn(nil)[0]      // Measured, at 0, 0, so its size
        qr.setLocation(left + width - qrSize, y)
        qr.drawOn(page)
        let end = paragraph("The fee is $120, lunch included. Pay with your phone: scan the code. "
                + "Or open " + Example_84.PAYMENT_PAGE + " on a computer. Your seat is kept once we have "
                + "your form and your payment.", left, y, width - qrSize - 2 * gap)
        y = max(end, y + qrSize) + 16.0

        field("Signature *", left, y, 0.6 * width - gap / 2.0, 1, nil)
        field("Date *", left + 0.6 * width + gap / 2.0, y, 0.4 * width - gap / 2.0, 1, nil)

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

    // Draws the text wrapped to the width, its top at y, and returns where
    // it ended.
    private func paragraph(_ text: String, _ x: Float, _ y: Float, _ width: Float) -> Float {
        let block = TextBlock(textFont, text)
        block.setLineSpacing(1.15)
        block.setLocation(x, y)
        block.setWidth(width)
        return block.drawOn(page)[1]
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_84.BLUE)
        heading.setLocation(Example_84.LEFT, y)
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
        let height = Example_84.BOX_HEIGHT + Float(lines - 1) * Example_84.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_84.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_84.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_84.LEFT, y, Example_84.LEFT + Example_84.WIDTH, y)
        line.setStrokeColor(Example_84.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_84.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_84()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_84 => \(String(format: "%4lld", time1 - time0)) ms")
