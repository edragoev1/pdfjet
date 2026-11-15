/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_82.swift
 * A time off request, to print and fill in by hand: the employee writes the
 * kind of leave, the first and the last day, and signs; the manager writes
 * the decision in a part of the form of its own, and signs too.
 *
 * This is a form people fill in, not a document with data. It is drawn as
 * Example_72 draws the job application, with the same field helper: the
 * label above its box, the boxes on a grid, and an asterisk on the label of
 * a field that is required. The kind of leave and the decision are radio
 * buttons, one of the options, under their label.
 *
 * Two people fill in the form, one after the other, so the part of the
 * manager is set apart: a rule across the page and a heading of its own.
 * Its fields have no asterisk, as the employee leaves them empty.
 *
 * The document is PDF/UA-1: the title and the two parts are headings,
 * the labels and the options are text, and the boxes are artifacts. The
 * layout is that of the time off request template of PDFjet Forms. The
 * company is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_82 {
    static let COMPANY = "Northwind Studio"
    static let KINDS_OF_LEAVE = [
        "Vacation", "Sick leave", "Personal day", "Parental leave", "Other",
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RED: Int32 = 0xb00020
    static let RULE: Int32 = 0xdde1e8
    static let BOX: Int32 = 0x4d4d4d

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0
    static let GAP: Float = 12.0
    static let HALF: Float = (WIDTH - GAP) / 2.0
    static let QUARTER: Float = (HALF - GAP) / 2.0

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    // The signature box and the date box beside it, 60 and 40 percent
    static let SIGNATURE: Float = 0.6 * WIDTH - GAP / 2.0
    static let DATE: Float = 0.4 * WIDTH - GAP / 2.0

    private let page: Page
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_82.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_82.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Time off request")
        pdf.setAuthor(Example_82.COMPANY)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_82.LEFT
        let width = Example_82.WIDTH
        let half = Example_82.HALF
        let quarter = Example_82.QUARTER
        let gap = Example_82.GAP

        // The title, when to send the form, and a rule
        heading("Time off request", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Fill in this form at least two weeks before "
                + "your time off, sign it and give it to your manager.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_82.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // The employee's part
        y = heading("Your request", StructElem.H2, 14.0, y + 46.0)
        field("Your name *", left, y, half, 1, nil)
        y = field("Department", left + half + gap, y, half, 1, nil)
        y = choice("Type of leave *", left, y, Example_82.KINDS_OF_LEAVE)
        field("First day *", left, y, quarter, 1, nil)
        field("Last day *", left + quarter + gap, y, quarter, 1, nil)
        field("Working days", left + half + gap, y, quarter, 1, nil)
        y = field("Phone while away", left + half + gap + quarter + gap, y, quarter, 1, nil)
        y = field("Notes for your manager", left, y, width, 2, nil)

        CheckBox(textFont, "I confirm this request and sign below. *")
                .setBorderColor(Example_82.BOX)
                .setLocation(left, y)
                .drawOn(page)
        y += 30.0
        field("Your signature *", left, y, Example_82.SIGNATURE, 1, nil)
        y = field("Date *", left + Example_82.SIGNATURE + gap, y, Example_82.DATE, 1, nil)

        // The manager's part, below a rule, filled in after the employee's
        rule(y + 12.0)
        y = heading("For your manager", StructElem.H2, 14.0, y + 42.0)
        y = choice("Decision", left, y, ["Approved", "Not approved"])
        y = field("Manager's name", left, y, half, 1, nil)
        CheckBox(textFont, "I confirm this decision and sign below.")
                .setBorderColor(Example_82.BOX)
                .setLocation(left, y)
                .drawOn(page)
        y += 30.0
        field("Manager's signature", left, y, Example_82.SIGNATURE, 1, nil)
        field("Date of decision", left + Example_82.SIGNATURE + gap, y, Example_82.DATE, 1, nil)

        try pdf.complete()
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_82.BLUE)
        heading.setLocation(Example_82.LEFT, y)
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
        let height = Example_82.BOX_HEIGHT + Float(lines - 1) * Example_82.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_82.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_82.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // Draws a question of a few answers: its label at y and a radio button
    // for each answer under it, where a box would be, each starting where the
    // one before it ended. drawOn returns the end of a button with some room
    // after its text already. Returns where the next row starts.
    private func choice(_ text: String, _ x: Float, _ y: Float, _ answers: [String]) -> Float {
        label(text, x, y)
        var x = x
        for answer in answers {
            x = RadioButton(textFont, answer).setLocation(x, y + 20.0).drawOn(page)[0] + 12.0
        }
        return y + 50.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_82.LEFT, y, Example_82.LEFT + Example_82.WIDTH, y)
        line.setStrokeColor(Example_82.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_82.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_82()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_82 => \(String(format: "%4lld", time1 - time0)) ms")
