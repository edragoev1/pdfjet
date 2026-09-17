/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_31.swift
 * This example draws Hindi and Marathi text, and filled rectangles, first
 * opaque and then half transparent, so the colors mix where they overlap.
 */
public class Example_31 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_31.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSansDevanagari.Regular)
        f1.setSize(13.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(14.0)

        let page = Page(pdf, Letter.PORTRAIT)

        // Hindi: the second line of the file, after its label.
        TextLine(f2, "Hindi").setLocation(50.0, 60.0).drawOn(page)
        let lines = try Content.linesOfTextFile("data/languages/devanagari.txt")
        var textBlock = TextBlock(f1, lines[1])
        textBlock.setLineSpacing(1.3)
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(510.0)
        var xy = textBlock.drawOn(page)

        TextLine(f2, "Marathi").setLocation(50.0, xy[1] + 35.0).drawOn(page)
        textBlock = TextBlock(f1, try Content.ofTextFile("data/languages/marathi.txt"))
        textBlock.setLineSpacing(1.3)
        textBlock.setLocation(50.0, xy[1] + 45.0)
        textBlock.setWidth(510.0)
        xy = textBlock.drawOn(page)

        let y: Float = xy[1] + 50.0
        let colors = [Color.blue, Color.green, Color.red]

        // Opaque rectangles: each one hides the one under it.
        TextLine(f2, "Opaque").setLocation(50.0, y).drawOn(page)
        for i in 0..<colors.count {
            page.setBrushColor(colors[i])
            page.fillRect(50.0 + Float(i) * 60.0, y + 15.0 + Float(i) * 30.0, 120.0, 120.0)
        }

        // Half transparent rectangles: the colors mix where they overlap.
        TextLine(f2, "50% transparent").setLocation(320.0, y).drawOn(page)
        page.saveGraphicsState()
        let gs = GraphicsState()
        gs.setAlphaStroking(0.5)    // The stroking alpha constant
        gs.setAlphaNonStroking(0.5) // The non-stroking alpha constant
        page.setGraphicsState(gs)
        for i in 0..<colors.count {
            page.setBrushColor(colors[i])
            page.fillRect(320.0 + Float(i) * 60.0, y + 15.0 + Float(i) * 30.0, 120.0, 120.0)
        }
        page.restoreGraphicsState()

        try pdf.complete()
    }
}   // End of Example_31.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_31()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_31 => \(String(format: "%4lld", time1 - time0)) ms")
