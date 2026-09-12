import Foundation
import PDFjet

/**
 * Example_50.swift
 */
public class Example_50 {
    public init(_ fileNumber: String, _ fileName: String) throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_\(fileNumber).pdf", append: false)!)
        var objects = try pdf.read(from: InputStream(fileAtPath: fileName)!)

        let image = try Image(
                &objects,
                InputStream(fileAtPath: "images/qrcode.png")!,
                ImageType.PNG)
        image.setLocation(495.0, 65.0)
        image.scaleBy(0.40)

        let f1 = try Font(
                &objects,
                InputStream(fileAtPath: IBMPlexSans.Regular)!,
                Font.STREAM).setSize(12.0)

        let f2 = try Font(
                &objects,
                InputStream(fileAtPath: IBMPlexSans.Bold)!,
                Font.STREAM).setSize(12.0)

        let pages = pdf.getPageObjects(from: objects)
        let page = Page(pdf, pages[0])
        // page.invertYAxis()

        page.addResource(image, &objects)
        page.addResource(f1, &objects)
        page.addResource(f2, &objects)
        let f3 = page.addResource(CoreFont.HELVETICA, &objects).setSize(12.0)

        image.drawOn(page)

        let x: Float = 23.0
        var y: Float = 185.0
        let dx: Float = 15.0
        let dy: Float = 24.0

        page.setBrushColor(Color.blue)

        // First Name and Initial
        page.drawString(f2, "Иван", x, y)

        // Last Name
        page.drawString(f3, "Jones", x + 258.0, y)

        // Social Insurance Number
        page.drawString(f1, stripSpacesAndDashes("243-590-129"), x + 437.0, y, dx)

        // Last Name at Birth
        y += dy
        page.drawString(f1, "Culverton", x, y)

        // Mailing Address
        y += dy
        page.drawString(f1, "10 Elm Street", x, y)

        // City
        y += dy
        page.drawString(f1, "Toronto", x, y)

        // Province or Territory
        page.drawString(f1, "Ontario", x + 365.0, y)

        // Postal Code
        page.drawString(f1, stripSpacesAndDashes("L7B 2E9"), x + 482.0, y, dx)

        // Home Address
        y += dy
        page.drawString(f1, "10 Oak Road", x, y)

        // City
        y += dy
        page.drawString(f1, "Toronto", x, y)

        // Previous Province or Territory
        page.drawString(f1, "Ontario", x + 365.0, y)

        // Postal Code
        page.drawString(f1, stripSpacesAndDashes("L7B 2E9"), x + 482.0, y, dx)

        // Home telephone number
        y += dy
        page.drawString(f1, "905-222-3333", x, y)

        // Work telephone number
        page.drawString(f1, "416-567-9903", x + 279.0, y)

        // Previous province or territory
        y += dy
        page.drawString(f1, "British Columbia", x + 452.0, y)

        // Move date from previous province or territory
        y += dy
        page.drawString(f1, stripSpacesAndDashes("2016-04-12"), x + 452.0, y, dx)

        // Date new marital status began
        page.drawString(f1, stripSpacesAndDashes("2014-11-02"), x + 452.0, 467.0, dx)

        // First name of spouse
        y = 521.0
        page.drawString(f1, "Melanie", x, y)

        // Last name of spouse
        page.drawString(f1, "Jones", x + 258.0, y)

        // Social Insurance number of spouse
        page.drawString(f1, stripSpacesAndDashes("192-760-427"), x + 437.0, y, dx)

        // Spouse or common-law partner's address
        page.drawString(f1, "12 Smithfield Drive", x, 554.0)

        // Signature Date
        page.drawString(f1, "2016-08-07", x + 475.0, 615.0)

        // Signature Date of spouse
        page.drawString(f1, "2016-08-07", x + 475.0, 651.0)

        // Female Checkbox 1
        // CheckBox.xMark(page, 477.5, 197.5, 7.0)

        // Male Checkbox 1
        CheckBox.xMark(page, 534.5, 197.5, 7.0)

        // Married
        CheckBox.xMark(page, 34.5, 424.0, 7.0)

        // Living common-law
        // CheckBox.xMark(page, 121.5, 424.0, 7.0)

        // Widowed
        // CheckBox.xMark(page, 235.5, 424.0, 7.0)

        // Divorced
        // CheckBox.xMark(page, 325.5, 424.0, 7.0)

        // Separated
        // CheckBox.xMark(page, 415.5, 424.0, 7.0)

        // Single
        // CheckBox.xMark(page, 505.5, 424.0, 7.0)

        // Female Checkbox 2
        CheckBox.xMark(page, 478.5, 536.5, 7.0)

        // Male Checkbox 2
        // CheckBox.xMark(page, 535.5, 536.5, 7.0)
        page.complete(&objects)
        pdf.addObjects(&objects)

        pdf.complete()
    }

    private func stripSpacesAndDashes(_ str: String) -> String {
        return str.filter({ $0 != " " && $0 != "-"})
    }
}   // End of Example_50.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_50("50", "data/testPDFs/rc65-16e.pdf")
// _ = try Example_50("50", "data/testPDFs/NoPredictor.pdf")
// _ = try Example_50("50", "../../eBooks/UniversityPhysicsVolume1.pdf")
// _ = try Example_50("50", "../specifications/ISO_32000-2_2017(en).PDF")
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_50", time0, time1)
