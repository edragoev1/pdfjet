import Foundation
import PDFjet

/**
 * Example_05.swift
 */
public class Example_05 {
    public init() {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_05.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setItalic(true)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f1)
        text.setLocation(300.0, 300.0)
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

        let point = Point(300.0, 300.0)
        _ = point.setShape(Point.CIRCLE)
        _ = point.setFillColor(Color.blue)
        _ = point.setRadius(37.0)
        point.drawOn(page)
        _ = point.setRadius(25.0)
        _ = point.setFillColor(Color.white)
        point.drawOn(page)

        _ = Arc()
                .setCenterXY(300.0, 600.0)
                .setRadiusX(75.0)
                .setRadiusY(75.0)
                .setStartAngle(0.0)
                .setSweepDegreesCW(270.0)
                // .setSweepDegreesCCW(270.0)
                // .setScaleFactor(2.0)
                // .setRotateDegreesCW(90.0)
                // .setRotateDegreesCCW(90.0)
                .setStrokeWidth(5.0)
                .setStrokeColor(Color.blue)
                .drawOn(page)

        Ellipse()
                .setCenterXY(300.0, 720.0)
                .setRadiusX(100.0)
                .setRadiusY(50.0)
                .setFillColor(Color.azure)
                .setStrokeWidth(1.5)
                .setStrokeColor(Color.blue)
                .setScaleFactor(0.5)
                .setRotateDegreesCW(45.0)
                // .setRotateDegreesCCW(45.0)
                .drawOn(page)

        pdf.complete()
    }
}   // End of Example_05.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_05()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_05", time0, time1)
