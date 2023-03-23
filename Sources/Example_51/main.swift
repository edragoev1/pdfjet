import Foundation
import PDFjet


///
/// Example_51.swift
///
public class Example_51 {

    public init(_ fileNumber: String, _ fileName: String) throws {

        if let stream = OutputStream(toFileAtPath: "Example_\(fileNumber).pdf", append: false) {

            var pdf = PDF(stream)
            var objects = try pdf.read(
                    from: InputStream(fileAtPath: "data/testPDFs/\(fileName)")!)

            let f1 = try Font(
                    &objects,
                    InputStream(fileAtPath: "fonts/Droid/DroidSans.ttf.stream")!,
                    Font.STREAM).setSize(12.0)

            let f2 = try Font(
                    &objects,
                    InputStream(fileAtPath: "fonts/Droid/DroidSans-Bold.ttf.stream")!,
                    Font.STREAM).setSize(12.0)

            let image = try Image(
                    &objects,
                    InputStream(fileAtPath: "images/qrcode.png")!,
                    ImageType.PNG)

            var pages = pdf.getPageObjects(from: &objects)
            let page = Page(&pdf, &pages[0])
            page.addResource(f1, &objects)
            page.addResource(f2, &objects)
            page.addResource(image, &objects)

            let f3 = page.addResource(CoreFont.HELVETICA, &objects).setSize(12.0)

            // page.setPenColor(Color.darkblue)
            // page.setPenWidth(2.0)
            // page.drawRect(0.0, 0.0, 200.0, 200.0)
            // page.moveTo(0.0, 0.0)
            // page.lineTo(200.0, 200.0)
            // page.strokePath()
            // page.drawString(f1, "Иван", 23.0, 185.0)

            image.setLocation(495.0, 65.0)
            image.scaleBy(0.40)
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
            page.drawString(f1, stripSpacesAndDashes("243-590-129"), x + Float(437.0), y, dx)

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
            page.drawString(f1, "Ontario", x + Float(365.0), y)

            // Postal Code
            page.drawString(f1, stripSpacesAndDashes("L7B 2E9"), x + Float(482.0), y, dx)

            // Home Address
            y += dy
            page.drawString(f1, "10 Oak Road", x, y)

            // City
            y += dy
            page.drawString(f1, "Toronto", x, y)

            // Previous Province or Territory
            page.drawString(f1, "Ontario", x + Float(365.0), y)

            // Postal Code
            page.drawString(f1, stripSpacesAndDashes("L7B 2E9"), x + Float(482.0), y, dx)

            // Home telephone number
            y += dy
            page.drawString(f1, "905-222-3333", x, y)

            // Work telephone number
            page.drawString(f1, "416-567-9903", x + Float(279.0), y)

            // Previous province or territory
            y += dy
            page.drawString(f1, "British Columbia", x + Float(452.0), y)

            // Move date from previous province or territory
            y += dy
            page.drawString(f1, stripSpacesAndDashes("2016-04-12"), x + 452.0, y, dx)

            // Date new marital status began
            page.drawString(f1, stripSpacesAndDashes("2014-11-02"), x + 452.0, 467.0, dx)

            // First name of spouse
            y = 521.0
            page.drawString(f1, "Melanie", x, y)

            // Last name of spouse
            page.drawString(f1, "Jones", x + Float(258.0), y)

            // Social Insurance number of spouse
            // page.drawString(f1, stripSpacesAndDashes("192-760-427"), x + Float(422.0), y, dx)

            // Spouse or common-law partner's address
            page.drawString(f1, "12 Smithfield Drive", x, Float(554.0))

            // Signature Date
            page.drawString(f1, "2016-08-07", x + Float(475.0), 615.0)

            // Signature Date of spouse
            page.drawString(f1, "2016-08-07", x + Float(475.0), 651.0)

            // Female Checkbox 1
            // xMarkCheckBox(page, 477.5, 197.5, 7.0)

            // Male Checkbox 1
            xMarkCheckBox(page, 534.5, 197.5, 7.0)

            // Married
            xMarkCheckBox(page, 34.5, 424.0, 7.0)

            // Living common-law
            // xMarkCheckBox(page, 121.5, 424.0, 7.0)

            // Widowed
            // xMarkCheckBox(page, 235.5, 424.0, 7.0)

            // Divorced
            // xMarkCheckBox(page, 325.5, 424.0, 7.0)

            // Separated
            // xMarkCheckBox(page, 415.5, 424.0, 7.0)

            // Single
            // xMarkCheckBox(page, 505.5, 424.0, 7.0)

            // Female Checkbox 2
            xMarkCheckBox(page, 478.5, 536.5, 7.0)

            // Male Checkbox 2
            // xMarkCheckBox(page, 535.5, 536.5, 7.0)

            page.complete(&objects)

            pdf.addObjects(&objects)

            pdf.complete()
        }
    }


    private func xMarkCheckBox(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ diagonal: Float) {
        page.setPenColor(Color.blue)
        page.setPenWidth(diagonal / 5)
        page.moveTo(x, y)
        page.lineTo(x + diagonal, y + diagonal)
        page.moveTo(x, y + diagonal)
        page.lineTo(x + diagonal, y)
        page.strokePath()
    }


    private func stripSpacesAndDashes(_ str: String) -> String {
        return str.filter({ $0 != " " && $0 != "-"})
    }

}   // End of Example_51.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_51("51", "rc65-16e.pdf")
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_51 => \(time1 - time0)")
