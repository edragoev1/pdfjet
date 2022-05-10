import Foundation
import PDFjet


/**
 *  Example_29.swift
 *
 */
public class Example_29 {

    public init() {

        if let stream = OutputStream(toFileAtPath: "Example_29.pdf", append: false) {

            let pdf = PDF(stream)
            let page = Page(pdf, Letter.PORTRAIT)

            let font = Font(pdf, CoreFont.HELVETICA)
            font.setSize(16.0)

            var paragraph = Paragraph()
            paragraph.add(TextLine(font, "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis."))

            let column = TextColumn()
            column.setLocation(50.0, 50.0)
            column.setSize(540.0, 0.0)
            // column.SetLineBetweenParagraphs(true)
            column.setLineBetweenParagraphs(false)
            column.addParagraph(paragraph)
/*
            let dim0 = column.getSize()
*/
            let _ /* point1 */ = column.drawOn(page)
            let point2 = column.drawOn(nil)
/*
            let dim1 = column.getSize()
            let dim2 = column.getSize()
            let dim3 = column.getSize()
            print("height0: \(dim0.getHeight()))
            print("point1.x: \(point1[0])    point1.y \(point1[1]))
            print("point2.x: \(point2[0])    point2.y \(point2[1]))
            print("height1: \(dim1.getHeight()))
            print("height2: \(dim2.getHeight()))
            print("height3: \(dim3.getHeight()))
            print()
*/
            column.removeLastParagraph()
            column.setLocation(50.0, point2[1])
            paragraph = Paragraph()
            paragraph.add(TextLine(font, "Peter Blood, bachelor of medicine and several other things besides, smoked a pipe and tended the geraniums boxed on the sill of his window above Water Lane in the town of Bridgewater."))
            column.addParagraph(paragraph)

/*
            let dim4 = column.getSize()
*/
            let point = column.drawOn(page)     // Draw the updated text column
/*
            print("point.x: \(point[0]))
            print("point.y: \(point[1]))
            print("height4: \(dim4.getHeight()))
*/
            let box = Box()
            box.setLocation(point[0], point[1])
            box.setSize(540.0, 25.0)
            box.setLineWidth(2.0)
            box.setColor(Color.darkblue)
            box.drawOn(page)

            pdf.complete()
        }
    }

}   // End of Example_29.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_29()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_29 => \(time1 - time0)")
