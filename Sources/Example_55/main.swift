/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_55.swift
 * An invoice, as a business sends one: a PDF/A-3 document that carries the
 * invoice as data too, in the file factur-x.xml, the way the e-invoice
 * standards Factur-X and ZUGFeRD ask, so that accounting software reads the
 * amounts instead of someone typing them in again.
 *
 * The page has the logo of the seller, an SVG file written as a drawing
 * program exports one, with a style sheet, groups and shapes; the addresses
 * and the dates; a table of the items with a header row, striped rows and the
 * totals in its footer rows; and a QR code a banking app scans to pay, in the
 * format of the European Payments Council. The document is PDF/A-3A, and so
 * it is tagged for screen readers too: a heading, paragraphs, a table and a
 * figure.
 *
 * The XML is attached with addAssociatedFile, with the relationship
 * Alternative, since it is the same invoice in another form, and
 * addMetadata says in the metadata of the document which file is the
 * invoice, with the extension schema PDF/A asks for properties of its own.
 * The companies, the addresses and the numbers are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_55 {
    // The items of the invoice: the description, the quantity and the unit
    // price in cents. The amounts are in cents, so that they add up exactly.
    private static let ITEMS = [
        "Letterpress business cards, 500",
        "A4 letterhead paper, box of 500",
        "DL envelopes, box of 250",
        "Logo design, hours",
        "Delivery",
    ]
    private static let QUANTITIES: [Int64] = [2, 3, 4, 6, 1]
    private static let PRICES: [Int64] = [4500, 2850, 1275, 5500, 990]
    private static let VAT_PERCENT: Int64 = 19

    private static let NAVY: Int32 = 0x1d3557
    private static let RED: Int32 = 0xe63946
    private static let STRIPE: Int32 = 0xf1f4f8

    // The description of the invoice for the metadata of the document: the
    // file that holds it, its type and its profile, and the extension schema
    // that declares these properties, which PDF/A asks of properties of its own.
    private static let FACTUR_X_METADATA =
            "<rdf:Description rdf:about=\"\"\n"
            + "    xmlns:fx=\"urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#\">\n"
            + "  <fx:DocumentType>INVOICE</fx:DocumentType>\n"
            + "  <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>\n"
            + "  <fx:Version>1.0</fx:Version>\n"
            + "  <fx:ConformanceLevel>BASIC</fx:ConformanceLevel>\n"
            + "</rdf:Description>\n"
            + "<rdf:Description rdf:about=\"\"\n"
            + "    xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\"\n"
            + "    xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\"\n"
            + "    xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">\n"
            + "  <pdfaExtension:schemas>\n"
            + "    <rdf:Bag>\n"
            + "      <rdf:li rdf:parseType=\"Resource\">\n"
            + "        <pdfaSchema:schema>Factur-X PDFA Extension Schema</pdfaSchema:schema>\n"
            + "        <pdfaSchema:namespaceURI>urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#</pdfaSchema:namespaceURI>\n"
            + "        <pdfaSchema:prefix>fx</pdfaSchema:prefix>\n"
            + "        <pdfaSchema:property>\n"
            + "          <rdf:Seq>\n"
            + property("DocumentFileName", "The name of the embedded XML document")
            + property("DocumentType", "The type of the hybrid document, in capital letters")
            + property("Version", "The version of the specification of the XML schema")
            + property("ConformanceLevel", "The profile of the XML document")
            + "          </rdf:Seq>\n"
            + "        </pdfaSchema:property>\n"
            + "      </rdf:li>\n"
            + "    </rdf:Bag>\n"
            + "  </pdfaExtension:schemas>\n"
            + "</rdf:Description>"

    private static func property(_ name: String, _ description: String) -> String {
        return "            <rdf:li rdf:parseType=\"Resource\">\n"
                + "              <pdfaProperty:name>" + name + "</pdfaProperty:name>\n"
                + "              <pdfaProperty:valueType>Text</pdfaProperty:valueType>\n"
                + "              <pdfaProperty:category>external</pdfaProperty:category>\n"
                + "              <pdfaProperty:description>" + description + "</pdfaProperty:description>\n"
                + "            </rdf:li>\n"
    }

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_55.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_55.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_A_3A)
        pdf.setTitle("Invoice 2026-0147")
        pdf.setAuthor("Lindenberg Paper & Print GmbH")
        pdf.setLanguage("en-US")

        // The invoice as data, which the document carries, and the description
        // of it in the metadata. Files are added before the first page.
        guard let xml = InputStream(fileAtPath: "data/invoice/factur-x.xml") else {
            throw ExampleError(message: "Cannot open data/invoice/factur-x.xml")
        }
        pdf.addAssociatedFile(try EmbeddedFile(
                pdf,
                "factur-x.xml",
                xml,
                true,
                "text/xml",
                Relationship.ALTERNATIVE,
                "The invoice as data, in the profile BASIC of Factur-X and ZUGFeRD."))
        pdf.addMetadata(Example_55.FACTUR_X_METADATA)

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(10.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(10.0)

        let page = Page(pdf, A4.PORTRAIT)
        let left: Float = 50.0
        let right: Float = page.getWidth() - 50.0
        let middle: Float = 345.0

        // The letterhead: the logo, and the address of the seller beside it
        guard let logo = try SVGImage(fileAtPath: "data/invoice/logo.svg") else {
            throw ExampleError(message: "Cannot open SVG file: data/invoice/logo.svg")
        }
        logo.setAltDescription("The logo of Lindenberg Paper & Print: a sheet of paper on a navy tile.")
        logo.setLocation(left, 50.0)
        logo.drawOn(page)

        var y: Float = 58.0
        Example_55.line(page, semiBold, "Lindenberg Paper & Print GmbH", middle, y, Color.black)
        for text in ["Hafenstraße 12", "20457 Hamburg, Germany", "VAT ID DE123456789"] {
            y += 14.0
            Example_55.line(page, regular, text, middle, y, Color.gray)
        }

        page.addArtifactBMC()
        page.setPenColor(Example_55.RED)
        page.setPenWidth(1.5)
        page.drawLine(left, 125.0, right, 125.0)
        page.addEMC()

        let title = TextLine(semiBold, "Invoice")
        title.setStructureType(StructElem.H1)
        title.setFontSize(26.0)
        title.setTextColor(Example_55.NAVY)
        title.setLocation(left, 172.0)
        title.drawOn(page)

        // The customer on the left, and the numbers of the invoice on the right
        y = 205.0
        let billTo = TextLine(regular, "BILL TO")
        billTo.setFontSize(8.0)
        billTo.setTextColor(Color.gray)
        billTo.setLocation(left, y)
        billTo.drawOn(page)
        Example_55.line(page, semiBold, "Kranich Design Studio", left, y + 16.0, Color.black)
        Example_55.line(page, regular, "Lindenallee 4", left, y + 30.0, Color.black)
        Example_55.line(page, regular, "50668 Köln, Germany", left, y + 44.0, Color.black)

        let facts = [
            ["Invoice number", "2026-0147"],
            ["Invoice date", "23 September 2026"],
            ["Due date", "23 October 2026"],
            ["Customer number", "K-3310"],
        ]
        for i in 0..<facts.count {
            let factY = y + 16.0 + Float(i) * 14.0
            Example_55.line(page, regular, facts[i][0], middle, factY, Color.gray)
            Example_55.line(page, semiBold, facts[i][1], right - semiBold.stringWidth(facts[i][1]), factY, Color.black)
        }

        // The items, and the totals in the footer rows of the table
        var net: Int64 = 0
        for i in 0..<Example_55.ITEMS.count {
            net += Example_55.QUANTITIES[i] * Example_55.PRICES[i]
        }
        let vat = (net * Example_55.VAT_PERCENT + 50) / 100    // Rounded to the cent
        let total = net + vat

        var rows = [[Cell]]()
        rows.append(Example_55.row(semiBold, "Description", "Quantity", "Unit price", "VAT", "Amount (EUR)"))
        for i in 0..<Example_55.ITEMS.count {
            rows.append(Example_55.row(regular, Example_55.ITEMS[i], String(Example_55.QUANTITIES[i]),
                    Example_55.money(Example_55.PRICES[i]),
                    String(Example_55.VAT_PERCENT) + "%",
                    Example_55.money(Example_55.QUANTITIES[i] * Example_55.PRICES[i])))
        }
        rows.append(Example_55.totalRow(regular, "Net amount", Example_55.money(net)))
        rows.append(Example_55.totalRow(regular, "VAT " + String(Example_55.VAT_PERCENT) + "%", Example_55.money(vat)))
        rows.append(Example_55.totalRow(semiBold, "Total due (EUR)", Example_55.money(total)))

        let table = Table()
        table.setTableData(rows, 1)
        table.setNumberOfFooterRows(3)
        table.setHeaderRowStyle(semiBold, Color.white, Example_55.NAVY)
        table.setAlternateRowColor(Example_55.STRIPE)
        table.setCellBorders(false)
        table.setWidth(right - left)
        table.setColumnWidthsInPercent(46.0, 12.0, 16.0, 8.0, 18.0)
        for column in 1..<5 {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT)
        }
        table.setLocation(left, 290.0)
        let xy = table.drawOn(page)

        // A line over the total due, an artifact
        page.addArtifactBMC()
        page.setPenColor(Example_55.NAVY)
        page.setPenWidth(1.0)
        page.drawLine(middle, xy[1] - 20.0, right, xy[1] - 20.0)
        page.addEMC()

        // How to pay, and the QR code of the payment, which a banking app reads
        let paymentY = xy[1] + 40.0
        let heading = TextLine(semiBold, "Payment")
        heading.setStructureType(StructElem.H2)
        heading.setFontSize(13.0)
        heading.setTextColor(Example_55.NAVY)
        heading.setLocation(left, paymentY)
        heading.drawOn(page)

        let payment = TextBlock(regular,
                "Please transfer EUR " + Example_55.money(total) + " by 23 October 2026 to Lindenberg Paper & Print GmbH, "
                + "IBAN DE89 3704 0044 0532 0130 00, with the reference 2026-0147.\n\n"
                + "Or scan the code with your banking app, which fills in the transfer.")
        payment.setLineSpacing(1.4)
        payment.setLocation(left, paymentY + 10.0)
        payment.setWidth(330.0)
        payment.drawOn(page)

        // An EPC QR code, which banking apps in Europe read as a SEPA credit
        // transfer: the version, the character set, the name, the IBAN, the
        // amount and the reference, one to a line, with error correction M.
        let epc = "BCD\n002\n1\nSCT\n\nLindenberg Paper & Print GmbH\nDE89370400440532013000\n"
                + "EUR" + Example_55.money(total) + "\n\n\nInvoice 2026-0147"
        let qr = try QRCode(epc, ErrorCorrectionLevel.M)
        qr.setModuleLength(2.0)
        qr.setLocation(right - 90.0, paymentY - 8.0)
        let qrXY = qr.drawOn(page)
        let caption = TextLine(regular, "Scan to pay")
        caption.setFontSize(8.0)
        caption.setTextColor(Color.gray)
        caption.setLocation(right - 90.0, qrXY[1] + 12.0)
        caption.drawOn(page)

        // What makes this invoice data as well
        let note = TextBlock(regular,
                "This invoice is also data. It is a PDF/A-3 document that carries its content as "
                + "factur-x.xml, in the profile BASIC of Factur-X and ZUGFeRD, so that accounting "
                + "software reads the items and the amounts instead of someone typing them in. "
                + "Open the attachments of your PDF viewer to see it.")
        note.setFontSize(9.0)
        note.setLineSpacing(1.4)
        note.setTextColor(Example_55.NAVY)
        note.setBackgroundColor(Example_55.STRIPE)
        note.setCornerRadius(6.0)
        note.setPadding(12.0)
        note.setLocation(left, qrXY[1] + 45.0)
        note.setWidth(right - left)
        note.drawOn(page)

        let footer = TextLine(regular,
                "Lindenberg Paper & Print GmbH  ·  Hafenstraße 12, 20457 Hamburg  ·  Registered in Hamburg, HRB 000000")
        footer.setFontSize(8.0)
        footer.setTextColor(Color.gray)
        footer.setLocation((page.getWidth() - footer.getWidth()) / 2.0, page.getHeight() - 40.0)
        footer.drawOn(page)

        try pdf.complete()
    }

    // Draws a line of text in the font and the color.
    private static func line(
            _ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float, _ color: Int32) {
        let line = TextLine(font, text)
        line.setTextColor(color)
        line.setLocation(x, y)
        line.drawOn(page)
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: String...) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            let cell = Cell(font, text)
            cell.setPadding(6.0)
            row.append(cell)
        }
        return row
    }

    // A row of the totals: its label over the first four columns, right
    // aligned, and the amount. The cells the label spans are there and empty.
    private static func totalRow(_ font: Font, _ label: String, _ amount: String) -> [Cell] {
        let row = row(font, label, "", "", "", amount)
        row[0].setColSpan(4)
        row[0].setTextAlignment(Alignment.RIGHT)
        return row
    }

    // The amount of cents as euros and cents, with a comma between the thousands.
    private static func money(_ cents: Int64) -> String {
        let euros = Array(String(cents / 100))
        var sb = ""
        for i in 0..<euros.count {
            if i > 0 && (euros.count - i) % 3 == 0 {
                sb += ","
            }
            sb.append(euros[i])
        }
        let rest = cents % 100
        return sb + (rest < 10 ? ".0" : ".") + String(rest)
    }
}   // End of Example_55.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_55()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_55 => \(String(format: "%4lld", time1 - time0)) ms")
