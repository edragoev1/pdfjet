/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_64.swift
 * A new client form to print and fill in by hand, that a design studio hands
 * to a client before the first meeting: who the client is, what the project
 * needs, the budget, the dates, and what the project should achieve.
 *
 * This is a form, not a finished document: the client fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * two questions of several answers, the kind of work and the budget, are
 * lists of radio buttons, one answer a line, side by side; the list of five
 * answers is longer than the list of four, so the row under them starts
 * where the longer one ended. The last answer, "Something else", has a line
 * after it to write what, drawn from where the button ended to the edge of
 * its column.
 *
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels and the answers are text, and the boxes and the write-in line
 * are artifacts. The layout is that of the client intake template of PDFjet
 * Forms. The studio is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_64 {
    static let STUDIO = "Tidewater Studio"
    static let RETURN_TO = "Please bring the form to our first meeting at 240 Front "
            + "Street, Kingston, or scan it to hello@example.com."

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
        guard let output = OutputStream(toFileAtPath: "Example_64.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_64.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("New client form")
        pdf.setAuthor(Example_64.STUDIO)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_64.LEFT
        let width = Example_64.WIDTH
        let half = Example_64.HALF
        let gap = Example_64.GAP

        // The title, why the studio asks, and a rule
        heading("New client form", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Tell us about you and your project, so that "
                + "we can prepare for our first meeting. It takes about five minutes.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_64.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // About you
        y = heading("About you", StructElem.H2, 14.0, y + 46.0)
        field("Full name *", left, y, half, 1, nil)
        y = field("Company", left + half + gap, y, half, 1, nil)
        field("Email *", left, y, half, 1, nil)
        y = field("Phone", left + half + gap, y, half, 1, nil)

        // Your project. The two lists are side by side, and the row under
        // them starts below the longer one.
        y = heading("Your project", StructElem.H2, 14.0, y + 18.0)
        let end = choice("What do you need? *", left, y, half,
                "Brand identity", "Website", "Print design", "Photography", "Something else")
        y = max(end, choice("Budget", left + half + gap, y, half,
                "Under $1,000", "$1,000 to $5,000", "$5,000 to $15,000", "Over $15,000"))
        field("Ideal start date", left, y, half, 1, nil)
        y = field("Deadline", left + half + gap, y, half, 1, nil)
        y = field("What should the project achieve? *", left, y, width, 4, nil)
        y = field("How did you hear about us?", left, y, width, 1, nil)

        CheckBox(textFont, "Send me occasional news about our work")
                .setBorderColor(Example_64.BOX)
                .setLocation(left, y)
                .drawOn(page)

        // Where the form goes, at the foot of the page
        let returnTo = TextLine(textFont, Example_64.RETURN_TO)
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
        heading.setTextColor(Example_64.BLUE)
        heading.setLocation(Example_64.LEFT, y)
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
        let height = Example_64.BOX_HEIGHT + Float(lines - 1) * Example_64.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_64.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_64.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // Draws a question of several answers: its label at y and a radio button
    // for each answer under it, one a line. An answer that starts with
    // "Something else" gets a line after it, to write what. Returns where the
    // next row starts.
    private func choice(_ text: String, _ x: Float, _ y: Float, _ width: Float, _ answers: String...) -> Float {
        label(text, x, y)
        var top = y + 18.0
        for answer in answers {
            let end = RadioButton(textFont, answer).setLocation(x, top).drawOn(page)
            if answer.hasPrefix("Something else") {
                let line = Line(end[0], end[1], x + width, end[1])
                line.setStrokeColor(Example_64.BOX)
                line.setStrokeWidth(0.5)
                page.addArtifactBMC()
                line.drawOn(page)
                page.addEMC()
            }
            top += Example_64.LINE_HEIGHT
        }
        return top + 10.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_64.LEFT, y, Example_64.LEFT + Example_64.WIDTH, y)
        line.setStrokeColor(Example_64.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_64.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_64()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_64 => \(String(format: "%4lld", time1 - time0)) ms")
