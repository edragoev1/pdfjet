import Foundation
import PDFjet

/**
 *  Example_19.swift
 */
public class Example_19 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_19.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")
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

        var textBlock = TextBlock(f1)
        textBlock.setText("Geometry arose independently in a number of early cultures as a practical way for dealing with lengths, areas, and volumes.")
        textBlock.setLocation(x2, y1)
        textBlock.setWidth(w2)
        // textBlock.setTextAlignment(Align.RIGHT)
        // textBlock.setTextAlignment(Align.CENTER)
        textBlock.setDrawBorder(true)
        let xy = textBlock.drawOn(page)

        // Draw the second row image and text:
        let image2 = try Image(
                pdf,
                InputStream(fileAtPath: "images/ee-map.png")!,
                ImageType.PNG)
        image2.setLocation(x1, xy[1] + 10.0)
        image2.scaleBy(1.0/3.0)
        image2.drawOn(page)

        textBlock = TextBlock(f1)
        textBlock.setText(try Content.ofTextFile("data/latin.txt"))
        // textBlock.setText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis.\n\n")
        textBlock.setWidth(w2)
        textBlock.setLocation(x2, xy[1] + 10.0)
        textBlock.setDrawBorder(true)
        textBlock.drawOn(page)

        textBlock = TextBlock(f1)
        textBlock.setFallbackFont(f2)
        textBlock.setText(try Content.ofTextFile("data/chinese-text.txt"))
        textBlock.setLocation(x1, 600.0)
        textBlock.setWidth(350.0)
        textBlock.setDrawBorder(true)
        textBlock.drawOn(page)

        pdf.complete()
    }
}   // End of Example_19.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_19()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_19 => \(time1 - time0)")
