import Foundation
import PDFjet

/**
 * Example_06.swift
 * We will draw the American flag using Box, Line and Point objects.
 */
public class Example_06 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_06.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)

        let file1 = try EmbeddedFile(pdf, "images/linux-logo.png", Compress.NO)
        let file2 = try EmbeddedFile(pdf, "examples/Example_02.cs", Compress.YES)

        let page = Page(pdf, Letter.PORTRAIT)

        // File attachment functionality
        var attachment = FileAttachment(pdf, file1)
        attachment.setLocation(100.0, 600.0)
        attachment.setIconPushPin()
        attachment.setTitle("Attached File: " + file1.getFileName())
        attachment.setDescription(
                "Right mouse click on the icon to save the attached file.")
        attachment.drawOn(page)

        attachment = FileAttachment(pdf, file2)
        attachment.setLocation(200.0, 600.0)
        attachment.setIconPaperclip()
        attachment.setTitle("Attached File: " + file2.getFileName())
        attachment.setDescription(
                "Right mouse click on the icon to save the attached file.")
        attachment.drawOn(page)

        let textLine = TextLine(f1, "pdfjet.com")
        textLine.setLocation(300.0, 618.0)
        textLine.setURIAction("https://pdfjet.com")
        textLine.drawOn(page)

        let textAnnotation = TextAnnotation()
        textAnnotation.setLocation(400.0, 600.0)
        textAnnotation.setSize(25.0, 25.0)
        textAnnotation.setTitle("Hello")
        textAnnotation.setContents("World")
        _ = textAnnotation.drawOn(page)

        let container = Container(400.0, 400.0)
        container.setLocation(100.0, 100.0)
        container.setBorderColor(Color.black)
        container.setRotationClockwise(90)

        let rect = Rect(0.0, 0.0, 25.0, 25.0)
        rect.setBorderColor(Color.black)
        rect.setBorderWidth(1.0)
        container.add(rect)

        let polygonAnnotation = PolygonAnnotation()
        polygonAnnotation.setLocation(0.0, 0.0)
        polygonAnnotation.setVertices([0.0, 0.0, 50.0, 0.0, 0.0, 50.0, 0.0, 0.0])
        polygonAnnotation.setFillColor(Color.red)
        polygonAnnotation.setTransparency(0.5)
        polygonAnnotation.setTitle("This is a test ...")
        polygonAnnotation.setContents("The quick brown cat caught the lazy mouse.")
        container.add(polygonAnnotation)

        let squareAnnotation = SquareAnnotation()
        squareAnnotation.setLocation(25.0, 0.0)
        squareAnnotation.setSize(50.0, 50.0)
        squareAnnotation.setFillColor([0.0, 0.0, 1.0])
        squareAnnotation.setTransparency(0.5)
        squareAnnotation.setTitle("Hello, World!")
        squareAnnotation.setContents("The quick brown fox jumps over the lazy dog.")
        container.add(squareAnnotation)

        let circleAnnotation = CircleAnnotation()
        circleAnnotation.setLocation(50.0, 0.0)
        circleAnnotation.setSize(50.0, 50.0)
        circleAnnotation.setFillColor([0.0, 0.0, 1.0])
        circleAnnotation.setTransparency(0.5)
        circleAnnotation.setTitle("Circle");
        circleAnnotation.setContents("Annotation")
        container.add(circleAnnotation)

        _ = container.drawOn(page)

        pdf.complete()
    }
}   // End of Example_06.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_06()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_06", time0, time1)
