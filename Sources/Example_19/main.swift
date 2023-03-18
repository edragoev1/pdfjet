import Foundation
import PDFjet


/**
 *  Example_19.swift
 *
 */
public class Example_19 {

    public init() throws {

        let stream = OutputStream(toFileAtPath: "Example_19.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/OpenSans/OpenSans-Regular.ttf.stream")!,
                Font.STREAM)
        f1.setSize(10.0)

        let f2 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/Droid/DroidSansFallback.ttf.stream")!,
                Font.STREAM)
        f2.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        // Columns x coordinates
        let x1: Float = 50.0
        let y1: Float = 50.0

        let x2: Float = 300.0

        // Width of the second column:
        let w2: Float = 300.0

        let image1 = try Image(
                pdf,
                InputStream(fileAtPath: "images/fruit.jpg")!,
                ImageType.JPG)
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
        textBlock.setText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis.\n\n")
        textBlock.setWidth(w2)
        textBlock.setLocation(x2, xy[1] + 10.0)
        textBlock.setDrawBorder(true)
        textBlock.drawOn(page)

        textBlock = TextBlock(f1)
        textBlock.setFallbackFont(f2)
        textBlock.setText("保健所によると、女性は１３日に旅行先のタイから札幌に戻り、１６日午後５～８時ごろ同店を訪れ、帰宅後に発熱などの症状が出て、２３日に医療機関ではしかと診断された。はしかのウイルスは発症日の１日前から感染者の呼吸などから放出され、本人がいなくなっても、２時間程度空気中に漂い、空気感染する。保健所は１６日午後５～１１時に同店を訪れた人に、発熱などの異常が出た場合、早期にマスクをして医療機関を受診するよう呼びかけている。（本郷由美子）")
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
