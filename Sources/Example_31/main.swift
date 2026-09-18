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
 * This example draws with transparency. A GraphicsState sets the alpha of the
 * fills and of the strokes: opaque shapes hide what is under them, transparent
 * ones mix with it, and the stroke of a shape can be more or less transparent
 * than its fill.
 */
public class Example_31 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_31.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "Transparency")
        text.setFontSize(22.0)
        text.setLocation(50.0, 80.0)
        text.drawOn(page)

        let textBlock = TextBlock(f1,
                "A GraphicsState sets the alpha of the fills and of the strokes that "
                + "follow it, until the graphics state is restored. Opaque shapes hide "
                + "what is under them, transparent ones mix with it, and the stroke of "
                + "a shape can be more or less transparent than its fill.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(512.0)
        let xy = textBlock.drawOn(page)

        var y: Float = xy[1] + 30.0
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
        var gs = GraphicsState()
        gs.setAlphaStroking(0.5)    // The stroking alpha constant
        gs.setAlphaNonStroking(0.5) // The non-stroking alpha constant
        page.setGraphicsState(gs)
        for i in 0..<colors.count {
            page.setBrushColor(colors[i])
            page.fillRect(320.0 + Float(i) * 60.0, y + 15.0 + Float(i) * 30.0, 120.0, 120.0)
        }
        page.restoreGraphicsState()

        // The same blue over a gray bar at four levels of alpha.
        y += 245.0
        TextLine(f2, "Fill alpha").setLocation(50.0, y).drawOn(page)
        page.setBrushColor(Color.gray)
        page.fillRect(50.0, y + 55.0, 506.0, 30.0)
        let alphas: [Float] = [0.25, 0.5, 0.75, 1.0]
        for i in 0..<alphas.count {
            let x: Float = 50.0 + Float(i) * 132.0
            page.saveGraphicsState()
            gs = GraphicsState()
            gs.setAlphaNonStroking(alphas[i])
            page.setGraphicsState(gs)
            page.setBrushColor(Color.blue)
            page.fillRect(x, y + 15.0, 110.0, 110.0)
            page.restoreGraphicsState()

            text = TextLine(f1, "\(Int((alphas[i] * 100.0).rounded()))%")
            text.setFontSize(10.0)
            text.setTextColor(Color.gray)
            text.setLocation(x, y + 140.0)
            text.drawOn(page)
        }

        // A thick stroke and a fill, each transparent on its own: the stroke
        // shows the fill through it, then the fill shows the stroke.
        y += 175.0
        TextLine(f2, "Stroke alpha and fill alpha").setLocation(50.0, y).drawOn(page)
        let labels = [
            "Stroke 25%, fill 100%",
            "Stroke 100%, fill 25%",
            "Stroke 50%, fill 50%",
        ]
        let strokeAndFill: [[Float]] = [[0.25, 1.0], [1.0, 0.25], [0.5, 0.5]]
        for i in 0..<labels.count {
            let x: Float = 100.0 + Float(i) * 180.0
            page.saveGraphicsState()
            gs = GraphicsState()
            gs.setAlphaStroking(strokeAndFill[i][0])
            gs.setAlphaNonStroking(strokeAndFill[i][1])
            page.setGraphicsState(gs)
            page.setBrushColor(Color.red)
            page.fillCircle(x, y + 65.0, 40.0)
            page.setPenColor(Color.blue)
            page.setPenWidth(16.0)
            page.drawCircle(x, y + 65.0, 40.0)
            page.restoreGraphicsState()

            text = TextLine(f1, labels[i])
            text.setFontSize(10.0)
            text.setTextColor(Color.gray)
            text.setLocation(x - text.getWidth() / 2.0, y + 130.0)
            text.drawOn(page)
        }

        try pdf.complete()
    }
}   // End of Example_31.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_31()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_31 => \(String(format: "%4lld", time1 - time0)) ms")
