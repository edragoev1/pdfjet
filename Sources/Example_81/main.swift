/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_81.swift
 * A rental application, two pages to print and fill in by hand: the
 * property, the applicant and where they live now, who will live in the
 * property, their work and income, two references, and the agreement to
 * sign, with "Page 1 of 2" at the bottom of each page.
 *
 * This is a form people fill in, not a document with data. It is drawn as
 * Example_72 draws the job application, with the same field helper: the
 * label above its box, the boxes on a grid of two or three columns, and an
 * asterisk on the label of a field that is required. The choices, as the
 * length of the lease or the pets, are radio buttons under their label.
 *
 * The landlord prints the form for one property, so its address and its
 * rent are filled in already, in the dark red of the answers of the
 * template, apart from the black of the form; the rent is a Decimal
 * written the same whatever the locale. The second page starts with a
 * section: a form is broken between its sections, never inside one.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * labels and the options are text, and the boxes and the page numbers are
 * artifacts. The layout is that of the rental application template of
 * PDFjet Forms. The landlord and the address are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_81 {
    static let LANDLORD = "Cedar Row Rentals"
    static let RETURN_TO = "Please return both pages to Cedar Row Rentals, "
            + "40 Woolwich Street, Guelph, ON N1H 3V1, or scan them to rent@example.com."

    // The property the form is printed for
    static let PROPERTY = "Unit 4, 118 Elm Street, Guelph, ON N1H 3T2"
    static let RENT = Decimal(string: "1850.00")!

    static let INK: Int32 = 0x333333           // the headings
    static let MAROON: Int32 = 0x800000         // the answers printed on the form
    static let RED: Int32 = 0xb00020
    static let RULE: Int32 = 0xdde1e8
    static let BOX: Int32 = 0x4d4d4d

    // The grid: the margin, the width of the page between the margins, the
    // gap between two fields of a row, and the widths of two and three fields
    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0
    static let GAP: Float = 12.0
    static let HALF: Float = (WIDTH - GAP) / 2.0
    static let THIRD: Float = (WIDTH - 2 * GAP) / 3.0

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    static let PAGES = 2

    private var page: Page!
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_81.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_81.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Rental application")
        pdf.setAuthor(Example_81.LANDLORD)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        let left = Example_81.LEFT
        let width = Example_81.WIDTH
        let half = Example_81.HALF
        let third = Example_81.THIRD
        let gap = Example_81.GAP

        // Page 1: the title, the property and the applicant
        page = Page(pdf, Letter.PORTRAIT)
        heading("Rental application", StructElem.H1, 20.0, 62.0)
        var y = paragraph("Thank you for your interest in the property. Fill in both pages, "
                + "sign at the end and send the form back to us; we answer within three "
                + "working days.", 74.0) + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_81.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        y = heading("The property", StructElem.H2, 14.0, y + 46.0)
        y = field("Address of the property *", left, y, width, 1, Example_81.PROPERTY)
        field("Move-in date *", left, y, half, 1, nil)
        y = field("Monthly rent", left + half + gap, y, half, 1,
                "$" + Example_81.money(Example_81.RENT))
        y = choice("Length of the lease", left, y, "12 months", "6 months", "Month to month")

        y = heading("About you", StructElem.H2, 14.0, y + 18.0)
        field("First name *", left, y, half, 1, nil)
        y = field("Last name *", left + half + gap, y, half, 1, nil)
        field("Email *", left, y, half, 1, nil)
        y = field("Phone *", left + half + gap, y, half, 1, nil)
        y = field("Current address *", left, y, width, 2, nil)
        y = choice("How long have you lived there?", left, y,
                "Less than a year", "1 to 3 years", "More than 3 years")
        field("Current landlord's name", left, y, half, 1, nil)
        field("Current landlord's phone", left + half + gap, y, half, 1, nil)
        footer(1)

        // Page 2: the household, the income, the references and the agreement
        page = Page(pdf, Letter.PORTRAIT)
        y = heading("Who will live there", StructElem.H2, 14.0, 62.0)
        field("Adults *", left, y, third, 1, nil)
        y = field("Children", left + third + gap, y, third, 1, nil)
        y = choice("Pets", left, y, "No pets", "Cat", "Dog", "Other")
        y = field("About your pets, if any", left, y, width, 1, nil)

        y = heading("Work and income", StructElem.H2, 14.0, y + 18.0)
        field("Employer", left, y, half, 1, nil)
        y = field("Position", left + half + gap, y, half, 1, nil)
        field("Years there", left, y, third, 1, nil)
        field("Monthly income", left + third + gap, y, third, 1, nil)
        y = field("Employer's phone", left + 2 * (third + gap), y, third, 1, nil)

        y = heading("References", StructElem.H2, 14.0, y + 18.0)
        y = paragraph("Two people who know you, not related to you: a former landlord, "
                + "an employer or a colleague.", y) + 10.0
        for which in ["First", "Second"] {
            field(which + " reference", left, y, third, 1, nil)
            field("Phone", left + third + gap, y, third, 1, nil)
            y = field("How they know you", left + 2 * (third + gap), y, third, 1, nil)
        }

        y = heading("Agreement", StructElem.H2, 14.0, y + 18.0)
        y = paragraph("I confirm that what I have written in this application is true, and "
                + "I allow you to contact my landlord, my employer and my references to "
                + "check it. Sending this application does not commit either of us to a "
                + "lease.", y) + 12.0
        CheckBox(textFont, "I have read and agree to the above and sign below. *")
                .setBorderColor(Example_81.BOX)
                .setLocation(left, y)
                .drawOn(page)
        y += 30.0
        field("Signature *", left, y, 0.6 * width - gap / 2.0, 1, nil)
        field("Date *", left + 0.6 * width + gap / 2.0, y, 0.4 * width - gap / 2.0, 1, nil)

        let returnTo = TextLine(textFont, Example_81.RETURN_TO)
        returnTo.setFontSize(9.0)
        returnTo.setTextColor(Color.dimgray)
        returnTo.setLocation(left, page.getHeight() - 52.0)
        returnTo.drawOn(page)
        footer(2)

        try pdf.complete()
    }

    // Draws a heading in gray, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_81.INK)
        heading.setLocation(Example_81.LEFT, y)
        heading.drawOn(page)
        return y + 16.0
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns where it ended.
    private func paragraph(_ text: String, _ y: Float) -> Float {
        let block = TextBlock(textFont, text)
        block.setLineSpacing(1.15)
        block.setLocation(Example_81.LEFT, y)
        block.setWidth(Example_81.WIDTH)
        return block.drawOn(page)[1]
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
        let height = Example_81.BOX_HEIGHT + Float(lines - 1) * Example_81.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_81.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_81.MAROON)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // Draws a question of a few answers: its label at y and a radio button
    // for each answer under it, where a box would be, each starting where the
    // one before it ended. drawOn returns the end of a button with some room
    // after its text already. Returns where the next row starts.
    private func choice(_ text: String, _ x: Float, _ y: Float, _ answers: String...) -> Float {
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
        let line = Line(Example_81.LEFT, y, Example_81.LEFT + Example_81.WIDTH, y)
        line.setStrokeColor(Example_81.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }

    // The page number at the foot of the page, as an artifact: it is not
    // part of the form, and a screen reader skips it.
    private func footer(_ number: Int) {
        let footer = TextLine(textFont, "Page " + String(number) + " of " + String(Example_81.PAGES))
        footer.setFontSize(10.0)
        footer.setLocation(Example_81.LEFT + Example_81.WIDTH - footer.getWidth(), page.getHeight() - 36.0)
        page.addArtifactBMC()
        footer.drawOn(page)
        page.addEMC()
    }

    // The amount rounded to the cent, half up, as 0.125 to 0.13.
    private static func rounded(_ amount: Decimal) -> Decimal {
        var value = amount
        var result = Decimal()
        NSDecimalRound(&result, &value, 2, .plain)
        return result
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,850.00, whatever the locale of the computer: it is put together
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
}   // End of Example_81.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_81()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_81 => \(String(format: "%4lld", time1 - time0)) ms")
