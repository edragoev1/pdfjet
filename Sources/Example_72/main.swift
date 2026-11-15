/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_72.swift
 * A job application form to print and fill in by hand: a box under each
 * label, larger boxes for the address and the experience, radio buttons for
 * the hours, check boxes for the consent, and a box to sign in.
 *
 * The form is drawn, not interactive: PDFjet draws text, lines and boxes, and
 * makes no fields that a viewer types into. That is the form most people
 * meet, on a clipboard at the front desk; it can also be filled in on screen
 * with the tools of a PDF viewer that type or sign on a page. So the boxes are
 * sized for handwriting, 24 points, about a third of an inch, tall, and each
 * label is above its box, where it stays readable once the box is filled. A
 * required field says so in its label, with an asterisk, as the note at the
 * top explains.
 *
 * The employer prints the form for one opening, so the position is filled in
 * already: the field helper draws a value in its box when it is given one.
 * The fields are placed on a grid of two columns with a gap between them, so
 * that the boxes of the rows line up.
 *
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels and the options are text, and the boxes are artifacts. The
 * layout is that of the job application template of PDFjet Forms. The
 * company is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_72 {
    static let COMPANY = "Northwind Studio"
    static let RETURN_TO = "Please return the form to Northwind Studio, "
            + "12 Harbour Street, Port Elgin, ON N0H 2C0, or scan it to jobs@example.com."
    static let POSITION = "Junior graphic designer (opening 2026-17)"

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

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    private let page: Page
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_72.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_72.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Job application")
        pdf.setAuthor(Example_72.COMPANY)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_72.LEFT
        let width = Example_72.WIDTH
        let half = Example_72.HALF
        let gap = Example_72.GAP

        // The title, what happens to the form, and a rule
        heading("Job application", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, Example_72.COMPANY + " thanks you for your interest in working "
                + "with us. We read every application and answer within two weeks.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_72.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // Your contact details
        y = heading("Your contact details", StructElem.H2, 14.0, y + 46.0)
        field("First name *", left, y, half, 1, nil)
        y = field("Last name *", left + half + gap, y, half, 1, nil)
        field("Email *", left, y, half, 1, nil)
        y = field("Phone *", left + half + gap, y, half, 1, nil)
        y = field("Street address", left, y, width, 2, nil)
        let quarter = (half - gap) / 2.0
        field("City", left, y, half, 1, nil)
        field("State or province", left + half + gap, y, quarter, 1, nil)
        y = field("Postal code", left + half + gap + quarter + gap, y, quarter, 1, nil)

        // The position
        y = heading("The position", StructElem.H2, 14.0, y + 18.0)
        field("Position you apply for *", left, y, half, 1, Example_72.POSITION)
        y = field("Available from", left + half + gap, y, half, 1, nil)

        // The hours, one of three, beside their label: each button starts
        // where the one before it ended
        label("Hours", left, y)
        var x = left + 70.0
        for option in ["Full time", "Part time", "Either"] {
            x = RadioButton(textFont, option).setLocation(x, y - 1.0).drawOn(page)[0] + 24.0
        }
        y += 30.0

        y = field("Your experience for this position *", left, y, width, 4, nil)

        // The consent, and the signature under it
        CheckBox(textFont, "You may keep my application for other positions for six months")
                .setBorderColor(Example_72.BOX)
                .setLocation(left, y)
                .drawOn(page)
        CheckBox(textFont, "I confirm the above is correct and sign below. *")
                .setBorderColor(Example_72.BOX)
                .setLocation(left, y + 20.0)
                .drawOn(page)
        y += 50.0
        field("Signature *", left, y, 0.6 * width - gap / 2.0, 1, nil)
        field("Date *", left + 0.6 * width + gap / 2.0, y, 0.4 * width - gap / 2.0, 1, nil)

        // Where the form goes, at the foot of the page
        let returnTo = TextLine(textFont, Example_72.RETURN_TO)
        returnTo.setFontSize(9.0)
        returnTo.setTextColor(Color.dimgray)
        returnTo.setLocation(left, page.getHeight() - 36.0)
        returnTo.drawOn(page)

        try pdf.complete()
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_72.BLUE)
        heading.setLocation(Example_72.LEFT, y)
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
        let height = Example_72.BOX_HEIGHT + Float(lines - 1) * Example_72.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_72.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_72.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_72.LEFT, y, Example_72.LEFT + Example_72.WIDTH, y)
        line.setStrokeColor(Example_72.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_72.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_72()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_72 => \(String(format: "%4lld", time1 - time0)) ms")
