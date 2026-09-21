/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_20.swift
 * This example draws a letterhead: a logo read from a PDF file, a maple leaf
 * drawn as a path with curves, and a QR code with the address of a web site.
 */
public class Example_20 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_20.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("PDFjet Software Letterhead")

        // Read the logo from a PDF file, and add the resources of its pages, its
        // fonts and images, to this PDF. The logo itself is drawn with paths and
        // has none.
        let objects = try pdf.read(
                from: InputStream(fileAtPath: "data/testPDFs/PDFjetLogo.pdf")!)

        pdf.addResourceObjects(from: objects)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(11.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(11.0)

        let pages = pdf.getPageObjects(from: objects)
        let content = pages[0].getContentObject(objects)!

        let page = Page(pdf, Letter.PORTRAIT)

        // Draw the content of the first page of the logo PDF, at half its size.
        // drawContents anchors the content by the point that is height points
        // above the origin of its page, which it puts at x and y here. The
        // logo is in the top left corner of its page, so that point is the top
        // of the page and the height to give is the height of the page.
        let height: Float = Letter.PORTRAIT.getHeight()
        let x: Float = 60.0
        let y: Float = 40.0
        let xScale: Float = 0.5
        let yScale: Float = 0.5

        // The logo is a figure, with an alternate description for screen readers.
        page.addBDC(StructElem.FIGURE, nil, "The PDFjet logo")
        page.drawContents(
                content.getData(),
                height,
                x,
                y,
                xScale,
                yScale)
        page.addEMC()

        TextLine(f2, "PDFjet Software").setLocation(390.0, 60.0).drawOn(page)
        TextLine(f1, "Unionville, Ontario, Canada").setLocation(390.0, 76.0).drawOn(page)
        TextLine(f1, "https://pdfjet.com").setLocation(390.0, 92.0).drawOn(page)

        // A thin rule under the letterhead, an artifact.
        page.addArtifactBMC()
        page.setPenColor(Color.darkred)
        page.setPenWidth(1.0)
        page.drawLine(60.0, 115.0, 552.0, 115.0)
        page.addEMC()

        let text = TextLine(f2, "The logo on this page was read from a PDF file.")
        text.setFontSize(16.0)
        text.setLocation(60.0, 170.0)
        text.drawOn(page)

        let textBlock = TextBlock(f1,
                "The logo is the content of the first page of data/testPDFs/PDFjetLogo.pdf, "
                + "drawn here at half its size with page.drawContents. It stays sharp "
                + "at any zoom, because it is drawn as vector graphics and not as an image.\n\n"
                + "The maple leaf below is a Path with curves, and the QR code "
                + "holds the address of the PDFjet web site.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(60.0, 185.0)
        textBlock.setWidth(490.0)
        textBlock.drawOn(page)

        // A maple leaf, drawn with lines and with curves from control points.
        let path = Path()

        path.add(Point(13.0,  0.0))
        path.add(Point(15.5,  4.5))

        path.add(Point(18.0,  3.5))
        path.add(Point(15.5, 13.5, Point.CONTROL_POINT_C))
        path.add(Point(15.5, 13.5, Point.CONTROL_POINT_C))
        path.add(Point(20.5,  7.5))

        path.add(Point(21.0,  9.5))
        path.add(Point(25.0,  9.0))
        path.add(Point(24.0, 13.0))
        path.add(Point(25.5, 14.0))
        path.add(Point(19.0, 19.0))
        path.add(Point(20.0, 21.5))
        path.add(Point(13.5, 20.5))
        path.add(Point(13.5, 27.0))
        path.add(Point(12.5, 27.0))
        path.add(Point(12.5, 20.5))
        path.add(Point( 6.0, 21.5))
        path.add(Point( 7.0, 19.0))
        path.add(Point( 0.5, 14.0))
        path.add(Point( 2.0, 13.0))
        path.add(Point( 1.0,  9.0))
        path.add(Point( 5.0,  9.5))

        path.add(Point( 5.5,  7.5))
        path.add(Point(10.5, 13.5, Point.CONTROL_POINT_C))
        path.add(Point(10.5, 13.5, Point.CONTROL_POINT_C))
        path.add(Point( 8.0,  3.5))

        path.add(Point(10.5,  4.5))
        path.setClosed(true)
        path.setStrokeColor(Color.red)
        path.setFillShape(true)
        path.setLocation(60.0, 330.0)
        path.scaleBy(6.0)
        path.drawOn(page)

        let qr = try QRCode(
                "https://pdfjet.com",
                ErrorCorrectionLevel.M)    // Medium
        qr.setModuleLength(5.0)
        qr.setLocation(300.0, 340.0)
        let xy = qr.drawOn(page)

        // A frame around the QR code, an artifact.
        page.addArtifactBMC()
        page.setPenColor(Color.lightgray)
        page.setPenWidth(0.5)
        page.drawRect(290.0, 330.0, xy[0] - 280.0, xy[1] - 320.0)
        page.addEMC()

        var caption = TextLine(f1, "A Path with curves")
        caption.setTextColor(Color.gray)
        caption.setLocation(60.0, xy[1] + 35.0)
        caption.drawOn(page)

        caption = TextLine(f1, "Scan to visit https://pdfjet.com")
        caption.setTextColor(Color.gray)
        caption.setLocation(290.0, xy[1] + 35.0)
        caption.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_20.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_20()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_20 => \(String(format: "%4lld", time1 - time0)) ms")
