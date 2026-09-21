/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_06.swift
 * This example attaches two files to a page, and adds a note, a link and
 * polygon, square and circle annotations next to labels that describe them.
 */
public class Example_06 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_06.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Attachments and Annotations")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(12.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(14.0)

        let file1 = try EmbeddedFile(pdf, "images/linux-logo.png", false)
        let file2 = try EmbeddedFile(pdf, "Sources/Example_02/main.swift", true)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "Attachments and annotations")
        text.setStructureType(StructElem.H1)
        text.setFontSize(22.0)
        text.setLocation(70.0, 80.0)
        text.drawOn(page)

        text = TextLine(f1,
                "Open this page in a PDF viewer that shows annotations, and hover over the icons.")
        text.setTextColor(Color.gray)
        text.setLocation(70.0, 105.0)
        text.drawOn(page)

        // File attachments. The files are stored inside the PDF.
        TextLine(f2, "Attached files").setStructureType(StructElem.H2).setLocation(70.0, 160.0).drawOn(page)

        var attachment = FileAttachment(file1)
        attachment.setLocation(70.0, 175.0)
        attachment.setIconPushPin()
        attachment.setTitle("Attached File: " + file1.getFileName())
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.")
        attachment.drawOn(page)
        TextLine(f1, "linux-logo.png, an image, with a push pin icon")
                .setLocation(105.0, 192.0).drawOn(page)

        attachment = FileAttachment(file2)
        attachment.setLocation(70.0, 210.0)
        attachment.setIconPaperclip()
        attachment.setTitle("Attached File: " + file2.getFileName())
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.")
        attachment.drawOn(page)
        TextLine(f1, "The source code of Example_02, with a paperclip icon")
                .setLocation(105.0, 227.0).drawOn(page)

        // A note, and a link.
        TextLine(f2, "A note and a link").setStructureType(StructElem.H2).setLocation(70.0, 290.0).drawOn(page)

        let textAnnotation = TextAnnotation()
        textAnnotation.setLocation(70.0, 305.0)
        textAnnotation.setSize(24.0, 24.0)
        textAnnotation.setTitle("Reviewer")
        textAnnotation.setContents("Please check the figures on page 2.")
        _ = textAnnotation.drawOn(page)
        TextLine(f1, "A text annotation: click the note icon to read it")
                .setLocation(105.0, 322.0).drawOn(page)

        text = TextLine(f1, "Visit https://pdfjet.com")
        text.setTextColor(Color.blue)
        text.setUnderline(true)
        text.setURIAction("https://pdfjet.com")
        text.setLocation(105.0, 357.0)
        text.drawOn(page)

        // Shape annotations, drawn half transparent over the page.
        TextLine(f2, "Shape annotations").setLocation(70.0, 420.0).drawOn(page)

        let polygonAnnotation = PolygonAnnotation()
        polygonAnnotation.setLocation(70.0, 440.0)
        polygonAnnotation.setVertices([0.0, 60.0, 30.0, 0.0, 60.0, 60.0, 0.0, 60.0])
        polygonAnnotation.setFillColor(Color.red)
        polygonAnnotation.setOpacity(0.5)
        polygonAnnotation.setTitle("Polygon")
        polygonAnnotation.setContents("Polygon Annotation")
        _ = polygonAnnotation.drawOn(page)

        let squareAnnotation = SquareAnnotation()
        squareAnnotation.setLocation(170.0, 440.0)
        squareAnnotation.setSize(60.0, 60.0)
        squareAnnotation.setFillColor([0.0, 0.5, 0.0])
        squareAnnotation.setOpacity(0.5)
        squareAnnotation.setTitle("Square")
        squareAnnotation.setContents("Square Annotation")
        _ = squareAnnotation.drawOn(page)

        let circleAnnotation = CircleAnnotation()
        circleAnnotation.setLocation(270.0, 440.0)
        circleAnnotation.setSize(60.0, 60.0)
        circleAnnotation.setFillColor([0.0, 0.0, 1.0])
        circleAnnotation.setOpacity(0.5)
        circleAnnotation.setTitle("Circle")
        circleAnnotation.setContents("Circle Annotation")
        _ = circleAnnotation.drawOn(page)

        TextLine(f1, "Polygon").setLocation(78.0, 520.0).drawOn(page)
        TextLine(f1, "Square").setLocation(180.0, 520.0).drawOn(page)
        TextLine(f1, "Circle").setLocation(283.0, 520.0).drawOn(page)

        try pdf.complete()
    }
}   // End of Example_06.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_06()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_06 => \(String(format: "%4lld", time1 - time0)) ms")
