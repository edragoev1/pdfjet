import Foundation
import PDFjet

/**
 * Example_19.swift
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

        var textBox = TextBox(f1, try Content.ofTextFile("data/calculus-short.txt"))
        textBox.setLocation(x2, y1)
        textBox.setWidth(w2)
        textBox.setBorders(true)
        var xy = textBox.drawOn(page)

        // Draw the second image
        image2.setLocation(x1, xy[1] + 10.0)
        image2.scaleBy(0.1)
        image2.drawOn(page)

        textBox = TextBox(f1)
        textBox.setText(try Content.ofTextFile("data/physics.txt"))
        textBox.setLocation(x2, xy[1] + 10.0)
        textBox.setWidth(w2)
        textBox.setBorders(true)
        xy = textBox.drawOn(page)

        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        pdf.complete()
    }
}   // End of Example_19.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_19()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_19", time0, time1)
