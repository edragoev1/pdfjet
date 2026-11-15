/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_75.swift
 * A membership application of a community garden, to print and fill in by
 * hand: the name, the address and how to reach the member, the kind of
 * membership with its fee, what they would like to take part in, the
 * newsletter and the rules to agree to, and a box to sign in.
 *
 * This is a form people fill in, not a document with data: the boxes are
 * empty. It is drawn as Example_72 draws the job application, with the same
 * field helper: the label above its box, the boxes on a grid of columns so
 * that the rows line up, 24 points tall for a line of handwriting, and an
 * asterisk on the label of a field that is required.
 *
 * The kinds of membership are radio buttons, one of three, and their fees
 * come from the data of the club, so that a change of fee is made in one
 * place and the form printed again. The options of a
 * choice are placed one after the other, each where the one before it ended,
 * so a longer option moves the next one along.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * labels and the options are text, and the boxes are artifacts. The layout
 * is that of the membership application template of PDFjet Forms. The garden
 * is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_75 {
    static let CLUB = "Riverbend Community Garden"
    static let RETURN_TO = "Please bring the form to the garden shed on a Saturday "
            + "morning, or mail it to 85 Mill Road, Elora, ON N0B 1S0."

    // The kinds of membership and their fees for a year, in dollars
    static let KINDS = ["Individual", "Family", "Student"]
    static let FEES = [
        Decimal(string: "40")!, Decimal(string: "70")!, Decimal(string: "20")!,
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

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    private let page: Page
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_75.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_75.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Membership application")
        pdf.setAuthor(Example_75.CLUB)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_75.LEFT
        let width = Example_75.WIDTH
        let half = Example_75.HALF
        let gap = Example_75.GAP

        // The title, what to do with the form, and a rule
        heading("Membership application", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Join " + Example_75.CLUB + " for a year. Fill in this "
                + "form and send it back with your first payment.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_75.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // About you
        y = heading("About you", StructElem.H2, 14.0, y + 46.0)
        field("First name *", left, y, half, 1, nil)
        y = field("Last name *", left + half + gap, y, half, 1, nil)
        y = field("Street address", left, y, width, 1, nil)
        // City, province and postal code: 40, 30 and 30 percent of the row
        let city = 0.4 * (width - 2 * gap)
        let rest = 0.3 * (width - 2 * gap)
        field("City", left, y, city, 1, nil)
        field("Province or state", left + city + gap, y, rest, 1, nil)
        y = field("Postal code", left + city + gap + rest + gap, y, rest, 1, nil)
        field("Email *", left, y, half, 1, nil)
        y = field("Phone", left + half + gap, y, half, 1, nil)

        // The membership: its kind, one of three, with the fee of each
        y = heading("Your membership", StructElem.H2, 14.0, y + 18.0)
        label("Membership *", left, y)
        var x = left + 96.0
        for i in 0..<Example_75.KINDS.count {
            let option = Example_75.KINDS[i] + ", $" + Example_75.FEES[i].description
            x = RadioButton(textFont, option).setLocation(x, y - 1.0).drawOn(page)[0] + 24.0
        }
        y += 30.0
        y = field("Start date *", left, y, half, 1, nil)
        y = field("What would you like to take part in?", left, y, width, 2, nil)

        // The newsletter, the rules, and the signature under them
        CheckBox(textFont, "Send me the monthly newsletter by email")
                .setBorderColor(Example_75.BOX)
                .setLocation(left, y)
                .drawOn(page)
        CheckBox(textFont, "I agree to the members' rules and sign below. *")
                .setBorderColor(Example_75.BOX)
                .setLocation(left, y + 20.0)
                .drawOn(page)
        y += 50.0
        field("Signature *", left, y, 0.6 * width - gap / 2.0, 1, nil)
        field("Date *", left + 0.6 * width + gap / 2.0, y, 0.4 * width - gap / 2.0, 1, nil)

        // Where the form goes, at the foot of the page
        let returnTo = TextLine(textFont, Example_75.RETURN_TO)
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
        heading.setTextColor(Example_75.BLUE)
        heading.setLocation(Example_75.LEFT, y)
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
        let height = Example_75.BOX_HEIGHT + Float(lines - 1) * Example_75.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_75.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_75.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_75.LEFT, y, Example_75.LEFT + Example_75.WIDTH, y)
        line.setStrokeColor(Example_75.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_75.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_75()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_75 => \(String(format: "%4lld", time1 - time0)) ms")
