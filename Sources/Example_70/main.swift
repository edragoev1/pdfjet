/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_70.swift
 * An incident report form to print and fill in by hand, kept in the first
 * aid room of a warehouse: on the first page, who reports, when and where it
 * happened, what happened, who was hurt, the witnesses and what was done
 * right away, and the signature; on the second, the follow-up that the
 * supervisor fills in.
 *
 * This is a form, not a finished document: the employee and then the
 * supervisor fill it in. It is drawn as the job application of Example_72
 * is, a label above each box, with larger boxes where the answer takes a
 * few lines. The form is two pages, each with its form number and "Page 1
 * of 2" at the foot. The pages are made detached and kept in a list, then
 * added to the document at the end, once their number is known; a third
 * page added to the form needs no change to the footers.
 *
 * The document is PDF/UA-1: the title and the supervisor's section are
 * headings, the labels and the answers are text, and the boxes and the
 * footers are artifacts. The layout is that of the incident report template of PDFjet
 * Forms. The company is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_70 {
    static let COMPANY = "Granite Bay Logistics"
    static let FORM_NUMBER = "Form HS-04, revised September 2026"

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
    static let THIRD: Float = (WIDTH - 2.0 * GAP) / 3.0

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    private var page: Page!
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_70.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_70.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Incident report")
        pdf.setAuthor(Example_70.COMPANY)
        pdf.setLanguage("en-CA")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        let left = Example_70.LEFT
        let width = Example_70.WIDTH
        let half = Example_70.HALF
        let third = Example_70.THIRD
        let gap = Example_70.GAP

        var pages = [Page]()

        // The first page, for the person who reports
        page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        heading("Incident report", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Report any accident, injury, damage or near "
                + "miss as soon as you can, while the details are fresh.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_70.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        y += 40.0
        field("Your name *", left, y, half, 1, nil)
        y = field("Phone", left + half + gap, y, half, 1, nil)
        field("Date of the incident *", left, y, third, 1, nil)
        field("Time", left + third + gap, y, third, 1, nil)
        y = field("Where it happened *", left + 2.0 * (third + gap), y, third, 1, nil)

        // The kind of incident, one of four, in a row under its label
        label("What happened? *", left, y)
        var x = left
        for kind in ["Injury", "Damage to property", "Near miss", "Other"] {
            x = RadioButton(textFont, kind).setLocation(x, y + 18.0).drawOn(page)[0] + 18.0
        }
        y += 48.0

        y = field("Describe what happened, step by step *", left, y, width, 4, nil)
        y = field("Was anyone hurt? Who, and how?", left, y, width, 2, nil)
        y = checkBox("First aid or medical care was given", y)
        y = field("Witnesses: names and phone numbers", left, y + 10.0, width, 2, nil)
        y = field("What was done right away?", left, y, width, 2, nil)
        y = checkBox("I confirm this report is true to the best of my knowledge "
                + "and sign below. *", y)
        signature("Signature *", "Date *", y + 10.0)

        // The second page, for the supervisor
        page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)
        y = heading("For the supervisor", StructElem.H2, 14.0, 62.0)
        field("Received by", left, y, half, 1, nil)
        y = field("Date received", left + half + gap, y, half, 1, nil)
        y = field("What caused it?", left, y, width, 3, nil)
        y = field("What will be done so that it does not happen again, by whom and by when?",
                left, y, width, 4, nil)
        label("Reported to", left, y)
        y += 18.0
        for to in ["The health and safety committee", "Our insurer",
                "The owner of the building"] {
            y = checkBox(to, y)
        }
        y = field("Closed on", left, y + 10.0, third, 1, nil)
        signature("Supervisor's signature", "Date", y)

        // The footer of each page, now that their number is known
        for i in 0..<pages.count {
            let p = pages[i]
            let form = TextLine(textFont, Example_70.COMPANY + ", " + Example_70.FORM_NUMBER)
            form.setFontSize(9.0)
            form.setTextColor(Color.dimgray)
            form.setLocation(left, p.getHeight() - 36.0)
            let number = TextLine(textFont, "Page " + String(i + 1) + " of " + String(pages.count))
            number.setFontSize(9.0)
            number.setTextColor(Color.dimgray)
            number.setLocation(left + width - number.getWidth(), p.getHeight() - 36.0)
            p.addArtifactBMC()
            form.drawOn(p)
            number.drawOn(p)
            p.addEMC()
        }
        pdf.addPages(pages)
        try pdf.complete()
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_70.BLUE)
        heading.setLocation(Example_70.LEFT, y)
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
        let height = Example_70.BOX_HEIGHT + Float(lines - 1) * Example_70.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_70.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_70.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // Draws a check box and its text, its top at y, and returns where the
    // next one starts.
    private func checkBox(_ text: String, _ y: Float) -> Float {
        CheckBox(textFont, text)
                .setBorderColor(Example_70.BOX)
                .setLocation(Example_70.LEFT, y)
                .drawOn(page)
        return y + Example_70.LINE_HEIGHT
    }

    // Draws the signature and the date beside it, 60 and 40 percent of the row.
    private func signature(_ text: String, _ date: String, _ y: Float) {
        let left = Example_70.LEFT
        let width = Example_70.WIDTH
        let gap = Example_70.GAP
        field(text, left, y, 0.6 * width - gap / 2.0, 1, nil)
        field(date, left + 0.6 * width + gap / 2.0, y, 0.4 * width - gap / 2.0, 1, nil)
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_70.LEFT, y, Example_70.LEFT + Example_70.WIDTH, y)
        line.setStrokeColor(Example_70.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_70.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_70()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_70 => \(String(format: "%4lld", time1 - time0)) ms")
