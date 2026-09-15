import Foundation
import PDFjet

/**
 * Example_05.swift
 *
 * Draws text at every angle around a point, and the words "WAVE AWAY" with and
 * without kerning, in the core font Helvetica-Bold, which is not embedded.
 *
 * A core font is one of the fourteen fonts every PDF viewer has, so the
 * document carries no font program: it is small, it is written fast, and the
 * kerning pairs and the widths of the font are built into the library, which
 * is what setKernPairs shows. The disadvantages: the viewer draws the text with
 * its own version of the font, so the look differs a little between viewers;
 * only the WinAnsi characters can be drawn, so no Cyrillic, Greek or CJK text;
 * and a document with a font that is not embedded cannot claim PDF/A or PDF/UA
 * compliance. For those, use an embedded font like IBM Plex Sans, as the
 * other examples do.
 */
public class Example_05 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_05.pdf", append: false)!)

        let f1 = try Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setItalic(true)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f1)
        text.setLocation(300.0, 300.0)
        var i = 0
        while i < 360 {
            text.setTextRotation(-i)
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

        let point = Point(300.0, 300.0)
        _ = point.setShape(Shape.CIRCLE)
        _ = point.setFillColor(Color.blue)
        _ = point.setRadius(37.0)
        point.drawOn(page)
        _ = point.setRadius(25.0)
        _ = point.setFillColor(Color.white)
        point.drawOn(page)

        _ = Arc()
                .setLocation(300.0, 600.0)
                .setRadiusX(75.0)
                .setRadiusY(75.0)
                .setStartAngle(0.0)
                .setSweep(270.0)
                // .setSweep(-270.0)
                // .scaleBy(2.0)
                .setStrokeWidth(5.0)
                .setStrokeColor(Color.blue)
                .drawOn(page)

        Ellipse()
                .setLocation(300.0, 720.0)
                .setRadiusX(100.0)
                .setRadiusY(50.0)
                .setFillColor(Color.azure)
                .setStrokeWidth(1.5)
                .setStrokeColor(Color.blue)
                .scaleBy(0.5)
                .setRotation(45.0)
                .drawOn(page)

        try pdf.complete()
    }
}   // End of Example_05.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_05()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_05 => \(time1 - time0) ms")
