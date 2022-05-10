import Foundation
import PDFjet


/**
 *  Example_72.swift
 *
 */
public class Example_72 {

    public init() throws {

        let stream = OutputStream(toFileAtPath: "Example_72.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = Font(pdf, CoreFont.HELVETICA)
        f1.setSize(72.0)

        let f2 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/Droid/DroidSans.ttf.stream")!,
                Font.STREAM)
        f2.setSize(24.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var buf = String()
        buf.append("Heya, World! This is a test to show the functionality of a TextBox.")

        let x1: Float = 90.0
        let y1: Float = 150.0

        let p1 = Point(x1, y1)
        p1.setRadius(5.0)
        p1.setFillShape(true)
        p1.drawOn(page)

        let textline = TextLine(f2, "(x1, y1)")
        textline.setLocation(x1, y1 - 15.0)
        textline.drawOn(page)

        let width: Float = 500.0
        let height: Float = 450.0
        let textBox = TextBox(f1, buf)
        textBox.setLocation(x1, y1)
        textBox.setWidth(width)
        textBox.setHeight(height)
        textBox.setMargin(0.0)
        textBox.setSpacing(0.0)
        let xy = textBox.drawOn(page)

        let x2 = x1 + width
        let y2 = y1 + textBox.getHeight()

        f2.setSize(18.0)

        // Text on the left
        let ascent_text = TextLine(f2, "Ascent")
        ascent_text.setLocation(x1 - 85.0, y1 + 40.0) //(y1 + f1.getAscent()) / 2)
        ascent_text.drawOn(page)

        let descent_text = TextLine(f2, "Descent")
        descent_text.setLocation(x1 - 85.0, y1 + f1.getAscent() + 15.0)
        descent_text.drawOn(page)

        // Lines beside the text
        let arrow_line1 = Line(x1 - 10.0, y1, x1 - 10.0, y1 + f1.getAscent())
        arrow_line1.setColor(Color.blue)
        arrow_line1.setWidth(3.0)
        arrow_line1.drawOn(page)

        let arrow_line2 = Line(x1 - 10.0, y1 + f1.getAscent(),
                            x1 - 10.0, y1 + f1.getAscent() + f1.getDescent())
        arrow_line2.setColor(Color.red)
        arrow_line2.setWidth(3.0)
        arrow_line2.drawOn(page)


        // Lines for first line of text
        let text_line1 = Line(x1, y1 + f1.getAscent(), x2, y1 + f1.getAscent())
        text_line1.drawOn(page)

        let descent_line1 = Line(x1, y1 + (f1.getAscent() + f1.getDescent()),
                                x2, y1 + (f1.getAscent() + f1.getDescent()))
        descent_line1.drawOn(page)


        // Lines for second line of text
        let curr_y = y1 + f1.getBodyHeight()

        let text_line2 = Line(x1, curr_y + f1.getAscent(), x2, curr_y + f1.getAscent())
        text_line2.drawOn(page)

        let descent_line2 = Line(x1, curr_y + f1.getAscent() + f1.getDescent(),
                                x2, curr_y + f1.getAscent() + f1.getDescent())
        descent_line2.drawOn(page)


        let p2 = Point(x2, y2)
        p2.setRadius(5.0)
        p2.setFillShape(true)
        p2.drawOn(page)

        f2.setSize(24.0)
        let textline2 = TextLine(f2, "(x2, y2)")
        textline2.setLocation(x2 - 80.0, y2 + 30.0)
        textline2.drawOn(page)

        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        pdf.complete()
    }

}   // End of Example_72.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_72()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_72 => \(time1 - time0)")
