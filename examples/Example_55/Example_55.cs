/*
 * Example_55.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_55.cs
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
 * The XML is attached with AddAssociatedFile, with the relationship
 * Alternative, since it is the same invoice in another form, and
 * AddMetadata says in the metadata of the document which file is the
 * invoice, with the extension schema PDF/A asks for properties of its own.
 * The companies, the addresses and the numbers are made up.
 */
public class Example_55 {
    // The items of the invoice: the description, the quantity and the unit
    // price in cents. The amounts are in cents, so that they add up exactly.
    private static readonly String[] ITEMS = {
        "Letterpress business cards, 500",
        "A4 letterhead paper, box of 500",
        "DL envelopes, box of 250",
        "Logo design, hours",
        "Delivery",
    };
    private static readonly int[] QUANTITIES = {2, 3, 4, 6, 1};
    private static readonly long[] PRICES = {4500, 2850, 1275, 5500, 990};
    private const int VAT_PERCENT = 19;

    private const int NAVY = 0x1d3557;
    private const int RED = 0xe63946;
    private const int STRIPE = 0xf1f4f8;

    // The description of the invoice for the metadata of the document: the
    // file that holds it, its type and its profile, and the extension schema
    // that declares these properties, which PDF/A asks of properties of its own.
    private static readonly String FACTUR_X_METADATA =
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
            + Property("DocumentFileName", "The name of the embedded XML document")
            + Property("DocumentType", "The type of the hybrid document, in capital letters")
            + Property("Version", "The version of the specification of the XML schema")
            + Property("ConformanceLevel", "The profile of the XML document")
            + "          </rdf:Seq>\n"
            + "        </pdfaSchema:property>\n"
            + "      </rdf:li>\n"
            + "    </rdf:Bag>\n"
            + "  </pdfaExtension:schemas>\n"
            + "</rdf:Description>";

    private static String Property(String name, String description) {
        return "            <rdf:li rdf:parseType=\"Resource\">\n"
                + "              <pdfaProperty:name>" + name + "</pdfaProperty:name>\n"
                + "              <pdfaProperty:valueType>Text</pdfaProperty:valueType>\n"
                + "              <pdfaProperty:category>external</pdfaProperty:category>\n"
                + "              <pdfaProperty:description>" + description + "</pdfaProperty:description>\n"
                + "            </rdf:li>\n";
    }

    public Example_55() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_55.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_3A);
        pdf.SetTitle("Invoice 2026-0147");
        pdf.SetAuthor("Lindenberg Paper & Print GmbH");
        pdf.SetLanguage("en-US");

        // The invoice as data, which the document carries, and the description
        // of it in the metadata. Files are added before the first page.
        pdf.AddAssociatedFile(new EmbeddedFile(
                pdf,
                "factur-x.xml",
                new FileStream("data/invoice/factur-x.xml", FileMode.Open, FileAccess.Read),
                true,
                "text/xml",
                Relationship.ALTERNATIVE,
                "The invoice as data, in the profile BASIC of Factur-X and ZUGFeRD."));
        pdf.AddMetadata(FACTUR_X_METADATA);

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(10f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(10f);

        Page page = new Page(pdf, A4.PORTRAIT);
        float left = 50f;
        float right = page.GetWidth() - 50f;
        float middle = 345f;

        // The letterhead: the logo, and the address of the seller beside it
        SVGImage logo = new SVGImage("data/invoice/logo.svg");
        logo.SetAltDescription("The logo of Lindenberg Paper & Print: a sheet of paper on a navy tile.");
        logo.SetLocation(left, 50f);
        logo.DrawOn(page);

        float y = 58f;
        Line(page, semiBold, "Lindenberg Paper & Print GmbH", middle, y, Color.black);
        foreach (String text in new String[] {"Hafenstraße 12", "20457 Hamburg, Germany", "VAT ID DE123456789"}) {
            y += 14f;
            Line(page, regular, text, middle, y, Color.gray);
        }

        page.AddArtifactBMC();
        page.SetPenColor(RED);
        page.SetPenWidth(1.5f);
        page.DrawLine(left, 125f, right, 125f);
        page.AddEMC();

        TextLine title = new TextLine(semiBold, "Invoice");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(NAVY);
        title.SetLocation(left, 172f);
        title.DrawOn(page);

        // The customer on the left, and the numbers of the invoice on the right
        y = 205f;
        TextLine billTo = new TextLine(regular, "BILL TO");
        billTo.SetFontSize(8f);
        billTo.SetTextColor(Color.gray);
        billTo.SetLocation(left, y);
        billTo.DrawOn(page);
        Line(page, semiBold, "Kranich Design Studio", left, y + 16f, Color.black);
        Line(page, regular, "Lindenallee 4", left, y + 30f, Color.black);
        Line(page, regular, "50668 Köln, Germany", left, y + 44f, Color.black);

        String[][] facts = {
            new String[] {"Invoice number", "2026-0147"},
            new String[] {"Invoice date", "23 September 2026"},
            new String[] {"Due date", "23 October 2026"},
            new String[] {"Customer number", "K-3310"},
        };
        for (int i = 0; i < facts.Length; i++) {
            float factY = y + 16f + i * 14f;
            Line(page, regular, facts[i][0], middle, factY, Color.gray);
            Line(page, semiBold, facts[i][1], right - semiBold.StringWidth(facts[i][1]), factY, Color.black);
        }

        // The items, and the totals in the footer rows of the table
        long net = 0;
        for (int i = 0; i < ITEMS.Length; i++) {
            net += QUANTITIES[i] * PRICES[i];
        }
        long vat = (net * VAT_PERCENT + 50) / 100;    // Rounded to the cent
        long total = net + vat;

        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Description", "Quantity", "Unit price", "VAT", "Amount (EUR)"));
        for (int i = 0; i < ITEMS.Length; i++) {
            rows.Add(Row(regular, ITEMS[i], QUANTITIES[i].ToString(), Money(PRICES[i]),
                    VAT_PERCENT + "%", Money(QUANTITIES[i] * PRICES[i])));
        }
        rows.Add(TotalRow(regular, "Net amount", Money(net)));
        rows.Add(TotalRow(regular, "VAT " + VAT_PERCENT + "%", Money(vat)));
        rows.Add(TotalRow(semiBold, "Total due (EUR)", Money(total)));

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetNumberOfFooterRows(3);
        table.SetHeaderRowStyle(semiBold, Color.white, NAVY);
        table.SetAlternateRowColor(STRIPE);
        table.SetCellBorders(false);
        table.SetWidth(right - left);
        table.SetColumnWidthsInPercent(46f, 12f, 16f, 8f, 18f);
        for (int column = 1; column < 5; column++) {
            table.SetTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.SetLocation(left, 290f);
        float[] xy = table.DrawOn(page);

        // A line over the total due, an artifact
        page.AddArtifactBMC();
        page.SetPenColor(NAVY);
        page.SetPenWidth(1f);
        page.DrawLine(middle, xy[1] - 20f, right, xy[1] - 20f);
        page.AddEMC();

        // How to pay, and the QR code of the payment, which a banking app reads
        float paymentY = xy[1] + 40f;
        TextLine heading = new TextLine(semiBold, "Payment");
        heading.SetStructureType(StructElem.H2);
        heading.SetFontSize(13f);
        heading.SetTextColor(NAVY);
        heading.SetLocation(left, paymentY);
        heading.DrawOn(page);

        TextBlock payment = new TextBlock(regular,
                "Please transfer EUR " + Money(total) + " by 23 October 2026 to Lindenberg Paper & Print GmbH, "
                + "IBAN DE89 3704 0044 0532 0130 00, with the reference 2026-0147.\n\n"
                + "Or scan the code with your banking app, which fills in the transfer.");
        payment.SetLineSpacing(1.4f);
        payment.SetLocation(left, paymentY + 10f);
        payment.SetWidth(330f);
        payment.DrawOn(page);

        // An EPC QR code, which banking apps in Europe read as a SEPA credit
        // transfer: the version, the character set, the name, the IBAN, the
        // amount and the reference, one to a line, with error correction M.
        String epc = "BCD\n002\n1\nSCT\n\nLindenberg Paper & Print GmbH\nDE89370400440532013000\n"
                + "EUR" + Money(total) + "\n\n\nInvoice 2026-0147";
        QRCode qr = new QRCode(epc, ErrorCorrectionLevel.M);
        qr.SetModuleLength(2f);
        qr.SetLocation(right - 90f, paymentY - 8f);
        float[] qrXY = qr.DrawOn(page);
        TextLine caption = new TextLine(regular, "Scan to pay");
        caption.SetFontSize(8f);
        caption.SetTextColor(Color.gray);
        caption.SetLocation(right - 90f, qrXY[1] + 12f);
        caption.DrawOn(page);

        // What makes this invoice data as well
        TextBlock note = new TextBlock(regular,
                "This invoice is also data. It is a PDF/A-3 document that carries its content as "
                + "factur-x.xml, in the profile BASIC of Factur-X and ZUGFeRD, so that accounting "
                + "software reads the items and the amounts instead of someone typing them in. "
                + "Open the attachments of your PDF viewer to see it.");
        note.SetFontSize(9f);
        note.SetLineSpacing(1.4f);
        note.SetTextColor(NAVY);
        note.SetBackgroundColor(STRIPE);
        note.SetCornerRadius(6f);
        note.SetPadding(12f);
        note.SetLocation(left, qrXY[1] + 45f);
        note.SetWidth(right - left);
        note.DrawOn(page);

        TextLine footer = new TextLine(regular,
                "Lindenberg Paper & Print GmbH  ·  Hafenstraße 12, 20457 Hamburg  ·  Registered in Hamburg, HRB 000000");
        footer.SetFontSize(8f);
        footer.SetTextColor(Color.gray);
        footer.SetLocation((page.GetWidth() - footer.GetWidth()) / 2f, page.GetHeight() - 40f);
        footer.DrawOn(page);

        pdf.Complete();
    }

    // Draws a line of text in the font and the color.
    private static void Line(Page page, Font font, String text, float x, float y, int color) {
        TextLine line = new TextLine(font, text);
        line.SetTextColor(color);
        line.SetLocation(x, y);
        line.DrawOn(page);
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> Row(Font font, params String[] texts) {
        List<Cell> row = new List<Cell>();
        foreach (String text in texts) {
            Cell cell = new Cell(font, text);
            cell.SetPadding(6f);
            row.Add(cell);
        }
        return row;
    }

    // A row of the totals: its label over the first four columns, right
    // aligned, and the amount. The cells the label spans are there and empty.
    private static List<Cell> TotalRow(Font font, String label, String amount) {
        List<Cell> row = Row(font, label, "", "", "", amount);
        row[0].SetColSpan(4);
        row[0].SetTextAlignment(Alignment.RIGHT);
        return row;
    }

    // The amount of cents as euros and cents, with a comma between the thousands.
    private static String Money(long cents) {
        String euros = (cents / 100).ToString();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < euros.Length; i++) {
            if (i > 0 && (euros.Length - i) % 3 == 0) {
                sb.Append(',');
            }
            sb.Append(euros[i]);
        }
        long rest = cents % 100;
        return sb.Append(rest < 10 ? ".0" : ".").Append(rest).ToString();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_55();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_55 => {time1 - time0,4} ms");
    }
}   // End of Example_55.cs
