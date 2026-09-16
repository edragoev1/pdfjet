/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_19.swift
 * Using the TextBlock component to draw text next to images.
 */
public class Example_19 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_19.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSansTC.Regular)
        f2.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)
        // Columns x coordinates
        let x1: Float = 50.0
        let y1: Float = 50.0
        let x2: Float = 300.0
        let w2: Float = 300.0   // Width of the second column

        let image1 = try Image(pdf, "images/ee-map.png")
        let image2 = try Image(pdf, "images/spain-admin.jpg")

        // Draw the first image
        image1.setLocation(x1, y1)
        image1.scaleBy(0.3)
        image1.drawOn(page)

        var textBlock = TextBlock(f1, try Content.ofTextFile("data/calculus-short.txt"))
        textBlock.setLocation(x2, y1)
        textBlock.setWidth(w2)
        textBlock.setBorderColor(Color.black)
        var xy = textBlock.drawOn(page)

        // Draw the second image
        image2.setLocation(x1, xy[1] + 10.0)
        image2.scaleBy(0.1)
        image2.drawOn(page)

        textBlock = TextBlock(f1, try Content.ofTextFile("data/physics.txt"))
        textBlock.setLocation(x2, xy[1] + 10.0)
        textBlock.setWidth(w2)
        textBlock.setBorderColor(Color.black)
        xy = textBlock.drawOn(page)

        let rect = Rect(xy[0], xy[1], 20.0, 20.0)
        rect.setBorderColor(Color.black)
        rect.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_19.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_19()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_19 => \(String(format: "%4lld", time1 - time0)) ms")
