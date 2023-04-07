import Foundation
import PDFjet

/**
 *  Example_06.swift
 *  We will draw the American flag using Box, Line and Point objects.
 */
public class Example_06 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_06.pdf", append: false)!)
        let font = Font(pdf, CoreFont.HELVETICA);
        let file1 = try EmbeddedFile(pdf, "images/linux-logo.png", Compress.NO)
        let file2 = try EmbeddedFile(pdf, "examples/Example_02.cs", Compress.YES)

        let page = Page(pdf, Letter.PORTRAIT)

        let flag = Box()
        flag.setLocation(100.0, 100.0)
        flag.setSize(190.0, 100.0)
        flag.setColor(Color.white)
        flag.drawOn(page)

        let sw: Float = 7.69        // stripe width
        let stripe = Line(0.0, sw/2, 190.0, sw/2)
        stripe.setWidth(sw)
        stripe.setColor(Color.oldgloryred)
        for row in 0..<7 {
            stripe.placeIn(flag, 0.0, Float(row) * 2.0 * sw)
            stripe.drawOn(page)
        }

        let union = Box()
        union.setSize(76.0, 53.85)
        union.setColor(Color.oldgloryblue)
        union.setFillShape(true)
        union.placeIn(flag, 0.0, 0.0)
        union.drawOn(page)

        let h_si: Float = 12.6      // horizontal star interval
        let v_si: Float = 10.8      // vertical star interval
        let star = Point(h_si/2, v_si/2)
        star.setShape(Point.STAR)
        star.setRadius(3.0)
        star.setColor(Color.white)
        star.setFillShape(true)

        for row in 0..<6 {
            for col in 0..<5 {
                star.placeIn(union, Float(row) * h_si, Float(col) * v_si)
                star.drawOn(page)
            }
        }

        star.setLocation(h_si, v_si)
        for row in 0..<5 {
            for col in 0..<4 {
                star.placeIn(union, Float(row) * h_si, Float(col) * v_si)
                star.drawOn(page)
            }
        }

        font.setSize(Float(18))

        var text = TextLine(font, "WAVE AWAY")
        text.setLocation(Float(100), Float(250))
        text.drawOn(page)

        font.setKernPairs(true);
        text = TextLine(font, "WAVE AWAY")
        text.setLocation(Float(100), Float(270))
        text.drawOn(page)

        var attachment = FileAttachment(pdf, file1)
        attachment.setLocation(100.0, 300.0)
        attachment.setIconPushPin()
        attachment.setIconSize(24.0)
        attachment.setTitle("Attached File: " + file1.getFileName())
        attachment.setDescription(
                "Right mouse click on the icon to save the attached file.")
        attachment.drawOn(page)

        attachment = FileAttachment(pdf, file2)
        attachment.setLocation(200.0, 300.0)
        attachment.setIconPaperclip()
        attachment.setIconSize(24.0)
        attachment.setTitle("Attached File: " + file2.getFileName())
        attachment.setDescription(
                "Right mouse click on the icon to save the attached file.")
        attachment.drawOn(page)

        pdf.complete()
    }
}   // End of Example_06.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_06()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_06 => \(time1 - time0)")
