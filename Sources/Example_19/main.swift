import Foundation
import PDFjet

/**
 * Example_19.swift
 */
public class Example_19 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_19.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(pdf, NotoSans.Regular)
        f1.setSize(10.0)

        let f2 = try Font(pdf, "fonts/NotoSansTC/NotoSansTC-Regular.ttf.stream")
        f2.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)
        // Columns x coordinates
        let x1: Float = 50.0
        let y1: Float = 50.0
        let x2: Float = 300.0
        let w2: Float = 300.0

        let image1 = try Image(pdf, "images/fruit.jpg")
        let image2 = try Image(pdf, "images/ee-map.png")

        image1.setLocation(x1, y1)
        image1.scaleBy(0.75)
        image1.drawOn(page)

        var textBlock = TextBlock(f1, try Content.ofTextFile("data/calculus-short.txt"))
        textBlock.setLocation(x2, y1)
        textBlock.setWidth(w2)
        textBlock.setBorderColor(Color.black)
        var xy = textBlock.drawOn(page)

        // Draw the second row image and text:
        image2.setLocation(x1, xy[1] + 10.0)
        image2.scaleBy(1.0/3.0)
        image2.drawOn(page)

        textBlock = TextBlock(f1, try Content.ofTextFile("data/latin.txt"))
        textBlock.setWidth(w2)
        textBlock.setLocation(x2, xy[1] + 10.0)
        textBlock.setBorderColor(Color.black)
        xy = textBlock.drawOn(page)

        textBlock = TextBlock(f2, try Content.ofTextFile("data/chinese.txt"))
        textBlock.setLocation(x1, 570.0)
        textBlock.setWidth(350.0)
        textBlock.setBorderColor(Color.blue)
        textBlock.drawOn(page)

        pdf.complete()
    }
}   // End of Example_19.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_19()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_19", time0, time1)
