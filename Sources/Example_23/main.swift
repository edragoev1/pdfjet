import Foundation
import PDFjet

/**
 * Example_23.swift
 */
public class Example_23 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_23.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(72.0)

        let f2 = Font(pdf, CoreFont.HELVETICA)
        f2.setSize(24.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let x1: Float = 90.0
        let y1: Float = 50.0

        let textLine = TextLine(f2, "(x1, y1)")
        textLine.setLocation(x1, y1 - 15.0)
        textLine.drawOn(page)

        let textBlock = TextBlock(f1,
            "Hello, World! This example shows the functionality of the TextBlock.")
        textBlock.setLocation(x1, y1)
        textBlock.setWidth(500.0)
        textBlock.setBorderColor(Color.lightgreen)
        textBlock.setFillColor(Color.lightgreen)
        textBlock.setTextColor(Color.black)
        let xy = textBlock.drawOn(page)

        // Text on the left
        let ascentText = TextLine(f2, "Ascent")
        ascentText.setFontSize(18.0)
        ascentText.setLocation(x1 - 85.0, y1 + 40.0)
        ascentText.drawOn(page)

        let descentText = TextLine(f2, "Descent")
        descentText.setFontSize(18.0)
        descentText.setLocation(x1 - 85.0, y1 + f1.getAscent() + 15.0)
        descentText.drawOn(page)

        let blueLine = Line(
            x1 - 10.0,
            y1,
            x1 - 10.0,
            y1 + f1.getAscent())
        blueLine.setColor(Color.blue)
        blueLine.setWidth(3.0)
        blueLine.drawOn(page)

        let redLine = Line(
            x1 - 10.0,
            y1 + f1.getAscent(),
            x1 - 10.0,
            y1 + f1.getAscent() + f1.getDescent())
        redLine.setColor(Color.red)
        redLine.setWidth(3.0)
        redLine.drawOn(page)

        let textLine1 = Line(
                x1,
                y1 + f1.getAscent(),
                xy[0],
                y1 + f1.getAscent())
        textLine1.drawOn(page)

        let descentLine1 = Line(
                x1,
                y1 + f1.getAscent() + f1.getDescent(),
                xy[0],
                y1 + f1.getAscent() + f1.getDescent())
        descentLine1.drawOn(page)

        let ascentLine = Line(
                x1,
                y1 + f1.getBodyHeight() + f1.getAscent(),
                xy[0],
                y1 + f1.getBodyHeight() + f1.getAscent())
        ascentLine.drawOn(page)

        let p1 = Point(x1, y1)
        p1.setRadius(5.0)
        p1.drawOn(page)

        let p2 = Point(xy[0], xy[1])
        p2.setRadius(5.0)
        p2.drawOn(page)

        let textLine3 = TextLine(f2, "(x2, y2)")
        textLine3.setFontSize(24.0)
        textLine3.setLocation(xy[0] - 80.0, xy[1] + 30.0)
        textLine3.drawOn(page)

        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        pdf.complete()
    }
}   // End of Example_23.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_23()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_23", time0, time1)
