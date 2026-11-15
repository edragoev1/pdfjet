/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_65.swift
 * A photo and video consent form to print and sign, handed out at the door
 * of an event: how the organizer may use the photos a guest appears in,
 * whether the guest may be named, a line for a parent who signs for a child,
 * and the name, the date and the signature.
 *
 * This is a form, not a finished document: the guest fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * organizer prints it for one event, so the event is filled in already: the
 * field helper draws a value in its box when it is given one. The answers to
 * "How may we use them?" are long, so they are radio buttons one under the
 * other, and what changing one's mind means is a paragraph, a TextBlock that
 * wraps to the width and returns where it ended. The name and the date share
 * a row two thirds to one third, and the child's name is as wide as the
 * name, so the boxes line up.
 *
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels, the answers and the paragraph are text, and the boxes are
 * artifacts. The layout is that of the consent form template of PDFjet
 * Forms. The organization and the event are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_65 {
    static let ORGANIZER = "Lakeside Arts Collective"
    static let ADDRESS = "Lakeside Arts Collective, 18 Shore Road, Collingwood, "
            + "ON L9Y 1A1. Please hand the form in at the door."
    static let EVENT = "Winter concert, Saturday, December 12, 2026"
    static let CHANGE_OF_MIND = "You can change your mind at any time by writing to "
            + "photos@example.com. We then stop using the photos and videos you appear in, and "
            + "take them off our website and social media within two weeks; copies already "
            + "printed stay as they are."

    static let BLUE: Int32 = 0x1f58b0
    static let RED: Int32 = 0xb00020
    static let RULE: Int32 = 0xdde1e8
    static let BOX: Int32 = 0x4d4d4d

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row. This form's rows are split two
    // thirds to one third.
    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0
    static let GAP: Float = 12.0
    static let TWO_THIRDS: Float = (WIDTH - GAP) * 2.0 / 3.0
    static let ONE_THIRD: Float = (WIDTH - GAP) / 3.0

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    private let page: Page
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_65.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_65.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Photo and video consent")
        pdf.setAuthor(Example_65.ORGANIZER)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        page = Page(pdf, Letter.PORTRAIT)

        let left = Example_65.LEFT
        let width = Example_65.WIDTH
        let twoThirds = Example_65.TWO_THIRDS

        // The title, what the form is about, and a rule
        heading("Photo and video consent", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "During the event we take photos and short "
                + "videos. Tell us how we may use those you appear in.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_65.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // The consent: the event, printed in, and the use of the photos
        y = heading("Your consent", StructElem.H2, 14.0, y + 46.0)
        y = field("Event", left, y, twoThirds, 1, Example_65.EVENT)
        y = choice("How may we use them? *", left, y,
                "Anywhere: our website, social media and print",
                "Only inside our organization",
                "Not at all")
        y = checkBox("You may name me beside a photo of me", y)
        y = checkBox("I sign for a child in my care", y)
        y = field("Child's name, if you sign for one", left, y + 10.0, twoThirds, 1, nil)

        let change = TextBlock(textFont, Example_65.CHANGE_OF_MIND)
        change.setLineSpacing(1.15)
        change.setLocation(left, y)
        change.setWidth(width)
        y = change.drawOn(page)[1] + 12.0

        // The signature, under the confirmation it signs
        y = heading("Your signature", StructElem.H2, 14.0, y + 18.0)
        field("Full name *", left, y, twoThirds, 1, nil)
        y = field("Date *", left + twoThirds + Example_65.GAP, y, Example_65.ONE_THIRD, 1, nil)
        y = checkBox("I agree to the above and sign below. *", y)
        field("Signature *", left, y + 10.0, twoThirds, 1, nil)

        // Who the organizer is, at the foot of the page
        let organizer = TextLine(textFont, Example_65.ADDRESS)
        organizer.setFontSize(9.0)
        organizer.setTextColor(Color.dimgray)
        organizer.setLocation(left, page.getHeight() - 36.0)
        organizer.drawOn(page)

        try pdf.complete()
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_65.BLUE)
        heading.setLocation(Example_65.LEFT, y)
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
        let height = Example_65.BOX_HEIGHT + Float(lines - 1) * Example_65.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_65.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_65.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // Draws a question of several answers: its label at y and a radio button
    // for each answer under it, one a line. Returns where the next row starts.
    private func choice(_ text: String, _ x: Float, _ y: Float, _ answers: String...) -> Float {
        label(text, x, y)
        var top = y + 18.0
        for answer in answers {
            RadioButton(textFont, answer).setLocation(x, top).drawOn(page)
            top += Example_65.LINE_HEIGHT
        }
        return top + 10.0
    }

    // Draws a check box and its text, its top at y, and returns where the
    // next one starts.
    private func checkBox(_ text: String, _ y: Float) -> Float {
        CheckBox(textFont, text)
                .setBorderColor(Example_65.BOX)
                .setLocation(Example_65.LEFT, y)
                .drawOn(page)
        return y + Example_65.LINE_HEIGHT
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_65.LEFT, y, Example_65.LEFT + Example_65.WIDTH, y)
        line.setStrokeColor(Example_65.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_65.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_65()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_65 => \(String(format: "%4lld", time1 - time0)) ms")
