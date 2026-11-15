/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_63.swift
 * An appointment request form to print and fill in by hand, for the front
 * desk of a small accounting office: the name and how to reach the client,
 * what the appointment is for, two dates that suit, how to meet, and room
 * for a note; at the foot, on a shade, the part the office fills in when it
 * books the time.
 *
 * This is a form, not a finished document: the client fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box, and
 * the questions with a few answers are radio buttons. Those answers are laid
 * out by a helper that measures each button first, with drawOn(nil), and
 * starts a new line when the next one would pass the width of its field. So
 * the four answers of "What is it for?" wrap to two lines in half the page,
 * and the row under it starts below the taller of the two fields beside each
 * other.
 *
 * The document is PDF/UA-1: the title and the three sections are headings,
 * the labels and the answers are text, and the boxes are artifacts. The
 * layout is that of the appointment request template of PDFjet Forms. The
 * office is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_63 {
    static let OFFICE = "Maple Lane Accounting"
    static let CONTACT = "Maple Lane Accounting, 85 Mill Street, Elora, ON N0B 1S0. "
            + "Phone 519-555-0142, or email appointments@example.com."

    static let BLUE: Int32 = 0x1f58b0
    static let RED: Int32 = 0xb00020
    static let RULE: Int32 = 0xdde1e8
    static let BOX: Int32 = 0x4d4d4d
    static let SHADE: Int32 = 0xf1f3f6

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
        guard let output = OutputStream(toFileAtPath: "Example_63.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_63.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Appointment request")
        pdf.setAuthor(Example_63.OFFICE)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_63.LEFT
        let width = Example_63.WIDTH
        let half = Example_63.HALF
        let gap = Example_63.GAP

        // The title, what happens to the form, and a rule
        heading("Book an appointment", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Tell us when suits you, and we will confirm "
                + "by email or phone within one working day.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_63.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // Who you are and how to reach you
        y = heading("Your details", StructElem.H2, 14.0, y + 46.0)
        y = field("Full name *", left, y, width, 1, nil)
        field("Email *", left, y, half, 1, nil)
        y = field("Phone", left + half + gap, y, half, 1, nil)

        // The appointment. The two choices share a row, and the next row
        // starts under the one that took more lines.
        y = heading("The appointment", StructElem.H2, 14.0, y + 18.0)
        let end = choice("What is it for? *", left, y, half,
                "First consultation", "Follow-up", "Review of a document", "Other")
        y = max(end, choice("Preferred time", left + half + gap, y, half,
                "Morning", "Afternoon", "Evening"))
        field("Preferred date *", left, y, half, 1, nil)
        y = field("Another date that works", left + half + gap, y, half, 1, nil)
        y = choice("How would you like to meet?", left, y, width,
                "In person", "Video call", "Phone call")
        y = field("Anything we should know before we meet", left, y, width, 3, nil)

        CheckBox(textFont, "Send me a reminder the day before")
                .setBorderColor(Example_63.BOX)
                .setLocation(left, y)
                .drawOn(page)

        // The office fills in the rest when it books the time, so its part is
        // at the foot of the page, on a shade that sets it apart. The shade
        // is a Rect, and a Rect is drawn as an artifact.
        let office = page.getHeight() - 132.0
        Rect(left - 8.0, office - 8.0, width + 16.0, 84.0).setFillColor(Example_63.SHADE).drawOn(page)
        heading("For office use", StructElem.H2, 12.0, office + 8.0)
        let third = (width - 2.0 * gap) / 3.0
        field("Booked for", left, office + 18.0, third, 1, nil)
        field("With", left + third + gap, office + 18.0, third, 1, nil)
        field("Confirmed by, and how", left + 2.0 * (third + gap), office + 18.0, third, 1, nil)

        // How to reach the office, at the foot of the page
        let contact = TextLine(textFont, Example_63.CONTACT)
        contact.setFontSize(9.0)
        contact.setTextColor(Color.dimgray)
        contact.setLocation(left, page.getHeight() - 36.0)
        contact.drawOn(page)

        try pdf.complete()
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_63.BLUE)
        heading.setLocation(Example_63.LEFT, y)
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
        let height = Example_63.BOX_HEIGHT + Float(lines - 1) * Example_63.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_63.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_63.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // Draws a question of a few answers: its label at y and a radio button for
    // each answer under it, left to right. A button that would pass the width
    // goes to the start of the next line; drawOn(nil) measures it without
    // drawing it. Returns where the next row starts.
    private func choice(_ text: String, _ x: Float, _ y: Float, _ width: Float, _ answers: String...) -> Float {
        label(text, x, y)
        var top = y + 18.0
        var left = x
        for answer in answers {
            let button = RadioButton(textFont, answer)
            let right = button.setLocation(left, top).drawOn(nil)[0]
            if right > x + width && left > x {
                left = x
                top += Example_63.LINE_HEIGHT
                button.setLocation(left, top)
            }
            left = button.drawOn(page)[0] + 12.0
        }
        return top + Example_63.LINE_HEIGHT + 10.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_63.LEFT, y, Example_63.LEFT + Example_63.WIDTH, y)
        line.setStrokeColor(Example_63.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_63.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_63()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_63 => \(String(format: "%4lld", time1 - time0)) ms")
