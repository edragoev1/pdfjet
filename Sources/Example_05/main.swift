import Foundation
import PDFjet

/**
 *  Example_05.swift
 */
public class Example_05 {
    public init() {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_05.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setItalic(true)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f1).setLocation(300.0, 300.0)
        var i = 0
        while i < 360 {
            text.setTextDirection(i)
            text.setUnderline(true)
            // text.setStrikeLine(true)
            text.setText("             Hello, World -- \(i) degrees.")
            text.drawOn(page)
            i += 15
        }

        text = TextLine(f1, "WAVE AWAY")
        text.setLocation(70.0, 50.0)
        text.drawOn(page)

        f1.setKernPairs(true)
        text = TextLine(f1, "WAVE AWAY")
        text.setLocation(70.0, 70.0)
        text.drawOn(page)

        f1.setKernPairs(false)
        text = TextLine(f1, "WAVE AWAY")
        text.setLocation(70.0, 90.0)
        text.drawOn(page)

        f1.setSize(8.0)
        text = TextLine(f1, "-- font.setKernPairs(false);")
        text.setLocation(150.0, 50.0)
        text.drawOn(page)
        text.setLocation(150.0, 90.0)
        text.drawOn(page)
        
        text = TextLine(f1, "-- font.setKernPairs(true);")
        text.setLocation(150.0, 70.0)
        text.drawOn(page)

        Point(300.0, 300.0)
                .setShape(Point.CIRCLE)
                .setFillShape(true)
                .setColor(Color.blue)
                .setRadius(37.0)
                .drawOn(page)

        Point(300.0, 300.0)
                .setShape(Point.CIRCLE)
                .setFillShape(true)
                .setColor(Color.white)
                .setRadius(25.0)
                .drawOn(page)

        page.setPenWidth(1.0)
        page.drawEllipse(300.0, 600.0, 100.0, 50.0)

        f1.setSize(14.0)        
        let unicode = "\u{20AC}\u{0020}\u{201A}\u{0192}\u{201E}\u{2026}\u{2020}\u{2021}\u{02C6}\u{2030}\u{0160}"
        text = TextLine(f1, unicode)
        text.setLocation(100.0, 700.0)
        text.drawOn(page)

        pdf.complete()
    }
}   // End of Example_05.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_05()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_05 => \(time1 - time0)")
