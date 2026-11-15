/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_68.swift
 * A customer feedback card to print and fill in by hand, left on the counter
 * of a bicycle shop: the date of the visit and what it was for, a rating of
 * 1 to 5 for the quality, the service, the value and the visit overall,
 * whether the customer would recommend the shop, and two boxes for comments.
 *
 * This is a form, not a finished document: the customer fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * ratings are a grid: a row for each question, with five radio buttons
 * labeled 1 to 5 in columns of the same width, under a line that says what
 * 1 and 5 mean. The columns are placed by a constant, not by the width of
 * the labels, so the buttons of all the rows line up, and a thin line
 * between the rows, an artifact, keeps the eye on the row.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * questions and the answers are text, and the boxes and the lines are
 * artifacts. The layout is that of the customer feedback template of PDFjet
 * Forms. The shop is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_68 {
    static let SHOP = "Harbor Lane Bicycles"
    static let RETURN_TO = "Leave the card at the counter, or mail it to Harbor Lane "
            + "Bicycles, 72 Harbor Lane, Mystic, CT 06355. Thank you."

    // The questions of the rating grid, each rated from 1 to 5
    static let RATINGS = [
        "Quality of the work or the product",
        "Service: friendly, clear, on time",
        "Value for money",
        "Your visit overall *",
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RED: Int32 = 0xb00020
    static let RULE: Int32 = 0xdde1e8
    static let BOX: Int32 = 0x4d4d4d

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static let LEFT: Float = 44.0
    static let WIDTH: Float = 524.0
    static let GAP: Float = 12.0
    static let HALF: Float = (WIDTH - GAP) / 2.0

    // The rating grid: where its first column of buttons starts, and how
    // far apart the columns are
    static let SCALE_LEFT: Float = LEFT + 250.0
    static let SCALE_STEP: Float = 54.0

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    private let page: Page
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_68.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_68.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Customer feedback")
        pdf.setAuthor(Example_68.SHOP)
        pdf.setLanguage("en-US")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_68.LEFT
        let width = Example_68.WIDTH
        let half = Example_68.HALF
        let gap = Example_68.GAP

        // The title, a line on what it is for, and a rule
        heading("How did we do?", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Two minutes of your time help us do better. "
                + "Every card is read by the owner.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(left, y, width)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_68.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // Your visit: the date, and what it was for, any of three
        y = heading("Your visit", StructElem.H2, 14.0, y + 46.0)
        let third = (width - 2.0 * gap) / 3.0
        field("Date of your visit", left, y, third, 1, nil)
        var x = left + third + gap
        label("What did you come for?", x, y)
        for reason in ["A purchase", "A repair or a service", "Advice"] {
            x = CheckBox(textFont, reason)
                    .setBorderColor(Example_68.BOX)
                    .setLocation(x, y + 21.0)
                    .drawOn(page)[0] + 12.0
        }
        y += 64.0

        // Your ratings: what 1 and 5 mean, then a row for each question
        y = heading("Your ratings", StructElem.H2, 14.0, y + 18.0)
        TextLine(italic, "1 is poor, 5 is excellent.")
                .setLocation(left, y + italic.getAscent())
                .drawOn(page)
        y += 22.0
        for question in Example_68.RATINGS {
            TextLine(textFont, question)
                    .setLocation(left, y + textFont.getAscent())
                    .drawOn(page)
            for i in 1...5 {
                RadioButton(textFont, String(i))
                        .setLocation(Example_68.SCALE_LEFT + Float(i - 1) * Example_68.SCALE_STEP, y)
                        .drawOn(page)
            }
            rule(left, y + 21.0, width)
            y += 28.0
        }

        // Would you recommend us, in a row
        label("Would you recommend us to a friend?", left, y + 6.0)
        x = left
        for answer in ["Yes, gladly", "Probably", "Not sure", "No"] {
            x = RadioButton(textFont, answer).setLocation(x, y + 24.0).drawOn(page)[0] + 18.0
        }
        y += 56.0

        // In the customer's words
        y = field("What did we do well?", left, y, width, 2, nil)
        y = field("What could we do better?", left, y, width, 2, nil)
        CheckBox(textFont, "You may quote me, without my name")
                .setBorderColor(Example_68.BOX)
                .setLocation(left, y)
                .drawOn(page)
        CheckBox(textFont, "Please contact me about my answers")
                .setBorderColor(Example_68.BOX)
                .setLocation(left + half + gap, y)
                .drawOn(page)
        y += 30.0
        field("Name", left, y, half, 1, nil)
        field("Email or phone", left + half + gap, y, half, 1, nil)

        // Where the card goes, at the foot of the page
        let returnTo = TextLine(textFont, Example_68.RETURN_TO)
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
        heading.setTextColor(Example_68.BLUE)
        heading.setLocation(Example_68.LEFT, y)
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
        let height = Example_68.BOX_HEIGHT + Float(lines - 1) * Example_68.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_68.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_68.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // A thin line, decoration only, so an artifact that a screen reader skips.
    private func rule(_ x: Float, _ y: Float, _ width: Float) {
        let line = Line(x, y, x + width, y)
        line.setStrokeColor(Example_68.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_68.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_68()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_68 => \(String(format: "%4lld", time1 - time0)) ms")
