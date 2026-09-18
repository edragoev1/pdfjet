/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_26.swift
 * This example draws a survey with check boxes and radio buttons.
 */
public class Example_26 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_26.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Customer Survey")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(11.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let x: Float = 70.0
        var y: Float = 90.0

        let text = TextLine(f2, "Customer Survey")
        text.setFontSize(22.0)
        text.setLocation(x, y)
        text.drawOn(page)

        // Check boxes, one below the other.
        y += 50.0
        TextLine(f2, "Which PDFjet ports do you use?").setLocation(x, y).drawOn(page)

        y += 15.0
        CheckBox(f1, "Java")
                .setLocation(x, y)
                .setCheckmarkColor(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page)

        y += 25.0
        CheckBox(f1, "C#")
                .setLocation(x, y)
                .setCheckmarkColor(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page)

        y += 25.0
        CheckBox(f1, "Swift")
                .setLocation(x, y)
                .drawOn(page)

        y += 25.0
        CheckBox(f1, "Go")
                .setLocation(x, y)
                .drawOn(page)

        // Radio buttons in a row. Each one starts where the one before it ends.
        y += 50.0
        TextLine(f2, "How did you hear about PDFjet?").setLocation(x, y).drawOn(page)

        y += 15.0
        var xy = RadioButton(f1, "Web search")
                .setLocation(x, y)
                .select(true)
                .drawOn(page)

        xy = RadioButton(f1, "A colleague")
                .setLocation(xy[0] + 20.0, y)
                .drawOn(page)

        RadioButton(f1, "Other")
                .setLocation(xy[0] + 20.0, y)
                .drawOn(page)

        y += 50.0
        TextLine(f2, "Would you recommend PDFjet?").setLocation(x, y).drawOn(page)

        y += 15.0
        xy = RadioButton(f1, "Yes")
                .setLocation(x, y)
                .select(true)
                .drawOn(page)

        RadioButton(f1, "No")
                .setLocation(xy[0] + 20.0, y)
                .drawOn(page)

        // A check box marked with an X, and one with a link.
        y += 50.0
        TextLine(f2, "Stay in touch").setLocation(x, y).drawOn(page)

        y += 15.0
        CheckBox(f1, "Send me news about new releases")
                .setLocation(x, y)
                .setCheckmarkColor(Color.red)
                .check(Mark.X)
                .drawOn(page)

        y += 25.0
        xy = CheckBox(f1, "Visit https://pdfjet.com")
                .setLocation(x, y)
                .setURIAction("https://pdfjet.com")
                .drawOn(page)

        // A border around the survey.
        let rect = Rect(50.0, 50.0, 512.0, xy[1] + 25.0 - 50.0)
        rect.setBorderColor(Color.lightgray)
        rect.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_26.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_26()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_26 => \(String(format: "%4lld", time1 - time0)) ms")
