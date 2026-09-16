/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_37.swift
 */
public class Example_37 {
    public init(_ fileName: String) throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_37.pdf", append: false)!)
        var objects = try pdf.read(from: InputStream(fileAtPath: fileName)!)

        let f1 = try Font(
                &objects,
                InputStream(fileAtPath: IBMPlexSans.Regular)!)
        f1.setSize(72.0)

        let text = TextLine(f1, "This is a test!")
        text.setLocation(150.0, 350.0)
        text.setTextColor(Color.peru)

        let pages = pdf.getPageObjects(from: objects)
        for pageObj in pages {
            let gs = GraphicsState()
            gs.setAlphaStroking(0.75)       // Stroking alpha
            gs.setAlphaNonStroking(0.75)    // Non-stroking alpha
            pageObj.setGraphicsState(gs, &objects)

            let page = Page(pdf, pageObj)
            page.addResource(f1, &objects)
            page.setBrushColor(Color.blue)
            // page.drawString(f1, "Hello, World!", 50.0, 200.0)
            text.drawOn(page)

            page.complete(&objects) // The graphics stack is unwinded automatically
        }
        try pdf.addObjects(objects)

        try pdf.complete()
    }
}   // End of Example_37.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_37("data/testPDFs/wirth.pdf")
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_37 => \(String(format: "%4lld", time1 - time0)) ms")
