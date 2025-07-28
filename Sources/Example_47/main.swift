import Foundation
import PDFjet

/**
 *  Example_47.swift
 */
public class Example_47 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_47.pdf", append: false)!)

        let f1 = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
        let f2 = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Italic.ttf.stream")

        f1.setSize(12.0)
        f2.setSize(12.0)

        let image1 = try Image(pdf, "images/AU-map.png")
        let image2 = try Image(pdf, "images/HU-map.png")

        let page = Page(pdf, Letter.PORTRAIT)

        image1.scaleBy(0.5)
        image1.setLocation(20.0, 20.0)
        image1.drawOn(page)

        image2.scaleBy(0.5)
        image2.setLocation(
                page.getWidth() - (image2.getWidth() + 20.0),
                page.getHeight() - (image2.getHeight() + 20.0));
        image2.drawOn(page)

        var paragraphs = [TextLine]()
        let str = try String(contentsOfFile: "data/austria_hungary.txt", encoding: .utf8)        
        let lines = str.components(separatedBy: "\n\n")
        for line in lines {
            paragraphs.append(TextLine(f1, String(line)))
        }

        var xPos: Float = 20.0
        let yPos: Float = 250.0

        let width: Float = 180.0
        let height: Float = 315.0

        let frame = TextFrame(paragraphs)
        frame.setLocation(xPos, yPos)
        frame.setWidth(width)
        frame.setHeight(height)
        frame.setDrawBorder(true)
        frame.drawOn(page)

        xPos += 200.0;
        if (frame.isNotEmpty()) {
            frame.setLocation(xPos, yPos)
            frame.setWidth(width)
            frame.setHeight(height)
            frame.setDrawBorder(false)
            frame.drawOn(page)
        }

        xPos += 200.0;
        if (frame.isNotEmpty()) {
            frame.setLocation(xPos, yPos)
            frame.setWidth(width)
            frame.setHeight(height)
            frame.setDrawBorder(true)
            frame.drawOn(page)
        }

        pdf.complete();
    }
}   // End of Example_47.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_47()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_47", time0, time1)
