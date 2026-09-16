/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_50.swift
 *
 * Fills in the fields of an existing PDF form: it reads the PDF, adds an image,
 * two fonts read from files and the core font Helvetica as resources of a page,
 * and writes the text on that page.
 *
 * The core font is added with addResource(CoreFont, objects), which writes a
 * font dictionary that names the font and nothing else. The advantage is that
 * the page grows by a few hundred bytes and needs no font file, which suits a
 * stamp or a field value added to a document that already exists. The
 * disadvantages are those of every font that is not embedded: the viewer draws
 * the text with its own Helvetica, only the WinAnsi characters can be drawn,
 * and the document cannot claim PDF/A or PDF/UA compliance. The two embedded
 * fonts of this example show the alternative.
 */
public class Example_50 {
    public init(_ fileNumber: String, _ fileName: String) throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_\(fileNumber).pdf", append: false)!)
        var objects = try pdf.read(from: InputStream(fileAtPath: fileName)!)

        let image = try Image(
                &objects,
                InputStream(fileAtPath: "images/qrcode.png")!)
        image.setLocation(495.0, 65.0)
        image.scaleBy(0.40)

        let f1 = try Font(
                &objects,
                InputStream(fileAtPath: IBMPlexSans.Regular)!).setSize(12.0)

        let f2 = try Font(
                &objects,
                InputStream(fileAtPath: IBMPlexSans.Bold)!).setSize(12.0)

        let pages = pdf.getPageObjects(from: objects)
        let page = Page(pdf, pages[0])
        // page.invertYAxis()

        page.addResource(image, &objects)
        page.addResource(f1, &objects)
        page.addResource(f2, &objects)
        let f3 = try page.addResource(CoreFont.HELVETICA, &objects).setSize(12.0)

        image.drawOn(page)

        let x: Float = 23.0
        var y: Float = 185.0
        let dx: Float = 15.0
        let dy: Float = 24.0

        page.setBrushColor(Color.blue)

        // First Name and Initial
        page.drawString(f2, f2.getSize(), "Иван", x, y)

        // Last Name
        page.drawString(f3, f3.getSize(), "Jones", x + 258.0, y)

        // Social Insurance Number
        page.drawString(f1, f1.getSize(), stripSpacesAndDashes("243-590-129"), x + 437.0, y, dx)

        // Last Name at Birth
        y += dy
        page.drawString(f1, f1.getSize(), "Culverton", x, y)

        // Mailing Address
        y += dy
        page.drawString(f1, f1.getSize(), "10 Elm Street", x, y)

        // City
        y += dy
        page.drawString(f1, f1.getSize(), "Toronto", x, y)

        // Province or Territory
        page.drawString(f1, f1.getSize(), "Ontario", x + 365.0, y)

        // Postal Code
        page.drawString(f1, f1.getSize(), stripSpacesAndDashes("L7B 2E9"), x + 482.0, y, dx)

        // Home Address
        y += dy
        page.drawString(f1, f1.getSize(), "10 Oak Road", x, y)

        // City
        y += dy
        page.drawString(f1, f1.getSize(), "Toronto", x, y)

        // Previous Province or Territory
        page.drawString(f1, f1.getSize(), "Ontario", x + 365.0, y)

        // Postal Code
        page.drawString(f1, f1.getSize(), stripSpacesAndDashes("L7B 2E9"), x + 482.0, y, dx)

        // Home telephone number
        y += dy
        page.drawString(f1, f1.getSize(), "905-222-3333", x, y)

        // Work telephone number
        page.drawString(f1, f1.getSize(), "416-567-9903", x + 279.0, y)

        // Previous province or territory
        y += dy
        page.drawString(f1, f1.getSize(), "British Columbia", x + 452.0, y)

        // Move date from previous province or territory
        y += dy
        page.drawString(f1, f1.getSize(), stripSpacesAndDashes("2016-04-12"), x + 452.0, y, dx)

        // Date new marital status began
        page.drawString(f1, f1.getSize(), stripSpacesAndDashes("2014-11-02"), x + 452.0, 467.0, dx)

        // First name of spouse
        y = 521.0
        page.drawString(f1, f1.getSize(), "Melanie", x, y)

        // Last name of spouse
        page.drawString(f1, f1.getSize(), "Jones", x + 258.0, y)

        // Social Insurance number of spouse
        page.drawString(f1, f1.getSize(), stripSpacesAndDashes("192-760-427"), x + 437.0, y, dx)

        // Spouse or common-law partner's address
        page.drawString(f1, f1.getSize(), "12 Smithfield Drive", x, 554.0)

        // Signature Date
        page.drawString(f1, f1.getSize(), "2016-08-07", x + 475.0, 615.0)

        // Signature Date of spouse
        page.drawString(f1, f1.getSize(), "2016-08-07", x + 475.0, 651.0)

        // Female Checkbox 1
        // CheckBox.drawXMark(page, 477.5, 197.5, 7.0)

        // Male Checkbox 1
        CheckBox.drawXMark(page, 534.5, 197.5, 7.0)

        // Married
        CheckBox.drawXMark(page, 34.5, 424.0, 7.0)

        // Living common-law
        // CheckBox.drawXMark(page, 121.5, 424.0, 7.0)

        // Widowed
        // CheckBox.drawXMark(page, 235.5, 424.0, 7.0)

        // Divorced
        // CheckBox.drawXMark(page, 325.5, 424.0, 7.0)

        // Separated
        // CheckBox.drawXMark(page, 415.5, 424.0, 7.0)

        // Single
        // CheckBox.drawXMark(page, 505.5, 424.0, 7.0)

        // Female Checkbox 2
        CheckBox.drawXMark(page, 478.5, 536.5, 7.0)

        // Male Checkbox 2
        // CheckBox.drawXMark(page, 535.5, 536.5, 7.0)
        page.complete(&objects)
        try pdf.addObjects(objects)

        try pdf.complete()
    }

    private func stripSpacesAndDashes(_ str: String) -> String {
        return str.filter({ $0 != " " && $0 != "-"})
    }
}   // End of Example_50.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_50("50", "data/testPDFs/rc65-16e.pdf")
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_50 => \(String(format: "%4lld", time1 - time0)) ms")
