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
 */
public class Example_26 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_26.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let x: Float = 50.0
        var y: Float = 50.0

        CheckBox(f1, "Hello")
                .setLocation(x, y)
                .setCheckmarkColor(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page)

        y += 30.0
        CheckBox(f1, "World!")
                .setLocation(x, y)
                .setCheckmarkColor(Color.blue)
                .setURIAction("http://pdfjet.com")
                .check(Mark.CHECK)
                .drawOn(page)

        y += 30.0
        CheckBox(f1, "This is a test.")
                .setLocation(x, y)
                .setURIAction("http://pdfjet.com")
                .drawOn(page)

        y += 30.0
        RadioButton(f1, "Hello, World!")
                .setLocation(x, y)
                .select(true)
                .drawOn(page)

        var xy = (RadioButton(f1, "Yes"))
                .setLocation(x + 100.0, 50.0)
                .setURIAction("http://pdfjet.com")
                .select(true)
                .drawOn(page)

        xy = (RadioButton(f1, "No"))
                .setLocation(xy[0], 50.0)
                .drawOn(page)

        xy = (CheckBox(f1, "Hello"))
                .setLocation(xy[0], 50.0)
                .setCheckmarkColor(Color.blue)
                .check(Mark.X)
                .drawOn(page)

        xy = (CheckBox(f1, "Yahoo")
                .setLocation(xy[0], 50.0)
                .setCheckmarkColor(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page))

        let rect = Rect(xy[0], xy[1], 20.0, 20.0)
        rect.setBorderColor(Color.black)
        rect.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_26.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_26()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_26 => \(String(format: "%4lld", time1 - time0)) ms")
