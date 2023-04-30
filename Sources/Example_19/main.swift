import Foundation
import PDFjet

/**
 *  Example_19.swift
 */
public class Example_19 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_19.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(pdf, "fonts/Droid/DroidSans.ttf.stream")
        let f2 = try Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream")

        f1.setSize(10.0)
        f2.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        // Columns x coordinates
        let x1: Float = 50.0
        let y1: Float = 50.0

        let x2: Float = 300.0

        // Width of the second column:
        let w2: Float = 300.0

        let image1 = try Image(pdf, "images/fruit.jpg")
        image1.setLocation(x1, y1)
        image1.scaleBy(0.75)
        image1.drawOn(page)

        var textBox = TextBox(f1)
        textBox.setText("Geometry arose independently in a number of early cultures as a practical way for dealing with lengths, areas, and volumes.")
        textBox.setLocation(x2, y1)
        textBox.setWidth(w2)
        // textBox.setTextAlignment(Align.RIGHT)
        // textBox.setTextAlignment(Align.CENTER)
        textBox.setBorders(true)
        let xy = textBox.drawOn(page)

        // Draw the second row image and text:
        let image2 = try Image(pdf, "images/ee-map.png")
        image2.setLocation(x1, xy[1] + 10.0)
        image2.scaleBy(1.0/3.0)
        image2.drawOn(page)

        textBox = TextBox(f1)
        textBox.setText(try Contents.ofTextFile("data/latin.txt"))
        textBox.setWidth(w2)
        textBox.setLocation(x2, xy[1] + 10.0)
        textBox.setBorders(true)
        textBox.drawOn(page)

        textBox = TextBox(f1)
        textBox.setFallbackFont(f2)
        textBox.setText(try Contents.ofTextFile("data/chinese.txt"))
        textBox.setLocation(x1, 530.0)
        textBox.setWidth(350.0)
        textBox.setBorders(true)
        textBox.drawOn(page)

        pdf.complete()
    }
}   // End of Example_19.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_19()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_19", time0, time1)
