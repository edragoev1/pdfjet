/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_66.swift
 * A contact details update form, mailed by a property manager to each of its
 * tenants: one page a tenant, with the name and the tenant number printed in,
 * the details on file shown above the boxes, and room for the new address,
 * email and phone, the date they apply from, and a signature.
 *
 * This is a form, not a finished document: the tenant fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * forms come from a list of tenants, the way a mail merge does: the same
 * code draws a page for each one, and the field helper draws the name and
 * the number in their boxes. What is on file is printed in gray, so the
 * tenant fills in only what has changed. The address row is split 40, 30 and
 * 30 percent, the signature row 60 and 40, with a gap between the fields.
 *
 * The document is PDF/UA-1: the title and the section of each page are
 * headings, the labels and the details are text, and the boxes are
 * artifacts. The layout is that of the contact details update template of
 * PDFjet Forms. The company and the tenants are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_66 {
    static let COMPANY = "Oakridge Property Management"
    static let RETURN_TO = "Please return the form in the envelope enclosed, or to the "
            + "office at 310 Queen Street, Fredericton, NB E3B 1B2."

    // The tenants: the name, the tenant number, and the details on file. In
    // an application they come from the database.
    static let TENANTS = [
        ["Maria Alvarez", "T-1042", "Unit 4, 55 Charlotte Street, Fredericton, NB E3B 1L4\n"
                + "maria.alvarez@example.com, 506-555-0117"],
        ["Samuel Okafor", "T-1057", "Unit 12, 55 Charlotte Street, Fredericton, NB E3B 1L4\n"
                + "s.okafor@example.com, 506-555-0183"],
        ["Hannah Lindqvist", "T-1103", "88 Waterloo Row, Fredericton, NB E3B 1Y9\n"
                + "hannah.l@example.com, 506-555-0129"],
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

    private var page: Page!
    private let labelFont: Font
    private let textFont: Font
    private let italic: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_66.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_66.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Contact details update")
        pdf.setAuthor(Example_66.COMPANY)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        for tenant in Example_66.TENANTS {
            page = Page(pdf, Letter.PORTRAIT)
            drawForm(tenant[0], tenant[1], tenant[2])
        }
        try pdf.complete()
    }

    // Draws the form of one tenant on the page.
    private func drawForm(_ name: String, _ number: String, _ onFile: String) {
        let left = Example_66.LEFT
        let width = Example_66.WIDTH
        let half = Example_66.HALF
        let gap = Example_66.GAP

        // The title, what to fill in, and a rule
        heading("Update your contact details", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Tell us what has changed. "
                + "Fill in only the details that are new.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_66.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // Who the form is for, printed in, and what is on file now
        y += 40.0
        field("Full name *", left, y, half, 1, name)
        y = field("Tenant number", left + half + gap, y, half, 1, number)
        label("On file now", left, y)
        let current = TextBlock(textFont, onFile)
        current.setLineSpacing(1.15)
        current.setTextColor(Color.dimgray)
        current.setLocation(left, y + 16.0)
        current.setWidth(width)
        y = current.drawOn(page)[1] + 12.0

        // The new details. The address row is split 40, 30 and 30 percent.
        y = heading("Your new details", StructElem.H2, 14.0, y + 18.0)
        y = field("Street address", left, y, width, 1, nil)
        let rest = width - 2.0 * gap
        field("City", left, y, 0.4 * rest, 1, nil)
        field("Province or state", left + 0.4 * rest + gap, y, 0.3 * rest, 1, nil)
        y = field("Postal code", left + 0.7 * rest + 2.0 * gap, y, 0.3 * rest, 1, nil)
        field("Email", left, y, half, 1, nil)
        y = field("Phone", left + half + gap, y, half, 1, nil)
        field("These details apply from *", left, y, half, 1, nil)

        // The best way to reach the tenant, three answers in a row beside the date
        label("Best way to reach you", left + half + gap, y)
        var x = left + half + gap
        for answer in ["Email", "Phone", "Mail"] {
            x = RadioButton(textFont, answer).setLocation(x, y + 21.0).drawOn(page)[0] + 18.0
        }
        y += 64.0

        // The confirmation, and the signature under it
        CheckBox(textFont, "I confirm these details and sign below. *")
                .setBorderColor(Example_66.BOX)
                .setLocation(left, y)
                .drawOn(page)
        y += 30.0
        field("Signature *", left, y, 0.6 * width - gap / 2.0, 1, nil)
        field("Date *", left + 0.6 * width + gap / 2.0, y, 0.4 * width - gap / 2.0, 1, nil)

        // Where the form goes, at the foot of the page
        let returnTo = TextLine(textFont, Example_66.RETURN_TO)
        returnTo.setFontSize(9.0)
        returnTo.setTextColor(Color.dimgray)
        returnTo.setLocation(left, page.getHeight() - 36.0)
        returnTo.drawOn(page)
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_66.BLUE)
        heading.setLocation(Example_66.LEFT, y)
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
        let height = Example_66.BOX_HEIGHT + Float(lines - 1) * Example_66.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_66.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_66.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_66.LEFT, y, Example_66.LEFT + Example_66.WIDTH, y)
        line.setStrokeColor(Example_66.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_66.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_66()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_66 => \(String(format: "%4lld", time1 - time0)) ms")
