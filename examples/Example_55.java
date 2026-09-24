/*
 * Example_55.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.ArrayList;
import java.util.List;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.qrcode.*;

/**
 * An invoice, as a business sends one: a PDF/A-3 document that carries the
 * invoice as data too, in the file factur-x.xml, the way the e-invoice
 * standards Factur-X and ZUGFeRD ask, so that accounting software reads the
 * amounts instead of someone typing them in again.
 * <p>
 * The page has the logo of the seller, an SVG file written as a drawing
 * program exports one, with a style sheet, groups and shapes; the addresses
 * and the dates; a table of the items with a header row, striped rows and the
 * totals in its footer rows; and a QR code a banking app scans to pay, in the
 * format of the European Payments Council. The document is PDF/A-3a and
 * PDF/UA-1 at once, PDF_A_3A_UA_1, so it is kept as PDF/A and tagged for
 * screen readers: a heading, paragraphs, a table and a
 * figure.
 * </p>
 * <p>
 * The XML is attached with addAssociatedFile, with the relationship
 * Alternative, since it is the same invoice in another form, and
 * addMetadata says in the metadata of the document which file is the
 * invoice, with the extension schema PDF/A asks for properties of its own.
 * The companies, the addresses and the numbers are made up.
 * </p>
 *
 * @see EmbeddedFile
 * @see PDF#addAssociatedFile(EmbeddedFile)
 * @see PDF#addMetadata(String)
 * @see SVGImage
 * @see Table
 */
public class Example_55 {
    // The items of the invoice: the description, the quantity and the unit
    // price in cents. The amounts are in cents, so that they add up exactly.
    private static final String[] ITEMS = {
        "Letterpress business cards, 500",
        "A4 letterhead paper, box of 500",
        "DL envelopes, box of 250",
        "Logo design, hours",
        "Delivery",
    };
    private static final int[] QUANTITIES = {2, 3, 4, 6, 1};
    private static final long[] PRICES = {4500, 2850, 1275, 5500, 990};
    private static final int VAT_PERCENT = 19;

    private static final int NAVY = 0x1d3557;
    private static final int RED = 0xe63946;
    private static final int STRIPE = 0xf1f4f8;

    // The description of the invoice for the metadata of the document: the
    // file that holds it, its type and its profile, and the extension schema
    // that declares these properties, which PDF/A asks of properties of its own.
    private static final String FACTUR_X_METADATA =
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
            + "</rdf:Description>";

    private static String property(String name, String description) {
        return "            <rdf:li rdf:parseType=\"Resource\">\n"
                + "              <pdfaProperty:name>" + name + "</pdfaProperty:name>\n"
                + "              <pdfaProperty:valueType>Text</pdfaProperty:valueType>\n"
                + "              <pdfaProperty:category>external</pdfaProperty:category>\n"
                + "              <pdfaProperty:description>" + description + "</pdfaProperty:description>\n"
                + "            </rdf:li>\n";
    }

    public Example_55() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_55.pdf")));
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.setTitle("Invoice 2026-0147");
        pdf.setAuthor("Lindenberg Paper & Print GmbH");
        pdf.setLanguage("en-US");

        // The invoice as data, which the document carries, and the description
        // of it in the metadata. Files are added before the first page.
        pdf.addAssociatedFile(new EmbeddedFile(
                pdf,
                "factur-x.xml",
                new FileInputStream("data/invoice/factur-x.xml"),
                true,
                "text/xml",
                Relationship.ALTERNATIVE,
                "The invoice as data, in the profile BASIC of Factur-X and ZUGFeRD."));
        pdf.addMetadata(FACTUR_X_METADATA);

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(10f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(10f);

        Page page = new Page(pdf, A4.PORTRAIT);
        float left = 50f;
        float right = page.getWidth() - 50f;
        float middle = 345f;

        // The letterhead: the logo, and the address of the seller beside it
        SVGImage logo = new SVGImage("data/invoice/logo.svg");
        logo.setAltDescription("The logo of Lindenberg Paper & Print: a sheet of paper on a navy tile.");
        logo.setLocation(left, 50f);
        logo.drawOn(page);

        float y = 58f;
        line(page, semiBold, "Lindenberg Paper & Print GmbH", middle, y, Color.black);
        for (String text : new String[] {"Hafenstraße 12", "20457 Hamburg, Germany", "VAT ID DE123456789"}) {
            y += 14f;
            line(page, regular, text, middle, y, Color.gray);
        }

        page.addArtifactBMC();
        page.setPenColor(RED);
        page.setPenWidth(1.5f);
        page.drawLine(left, 125f, right, 125f);
        page.addEMC();

        TextLine title = new TextLine(semiBold, "Invoice");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(NAVY);
        title.setLocation(left, 172f);
        title.drawOn(page);

        // The customer on the left, and the numbers of the invoice on the right
        y = 205f;
        TextLine billTo = new TextLine(regular, "BILL TO");
        billTo.setFontSize(8f);
        billTo.setTextColor(Color.gray);
        billTo.setLocation(left, y);
        billTo.drawOn(page);
        line(page, semiBold, "Kranich Design Studio", left, y + 16f, Color.black);
        line(page, regular, "Lindenallee 4", left, y + 30f, Color.black);
        line(page, regular, "50668 Köln, Germany", left, y + 44f, Color.black);

        String[][] facts = {
            {"Invoice number", "2026-0147"},
            {"Invoice date", "23 September 2026"},
            {"Due date", "23 October 2026"},
            {"Customer number", "K-3310"},
        };
        for (int i = 0; i < facts.length; i++) {
            float factY = y + 16f + i * 14f;
            line(page, regular, facts[i][0], middle, factY, Color.gray);
            line(page, semiBold, facts[i][1], right - semiBold.stringWidth(facts[i][1]), factY, Color.black);
        }

        // The items, and the totals in the footer rows of the table
        long net = 0;
        for (int i = 0; i < ITEMS.length; i++) {
            net += QUANTITIES[i] * PRICES[i];
        }
        long vat = (net * VAT_PERCENT + 50) / 100;    // Rounded to the cent
        long total = net + vat;

        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Description", "Quantity", "Unit price", "VAT", "Amount (EUR)"));
        for (int i = 0; i < ITEMS.length; i++) {
            rows.add(row(regular, ITEMS[i], String.valueOf(QUANTITIES[i]), money(PRICES[i]),
                    VAT_PERCENT + "%", money(QUANTITIES[i] * PRICES[i])));
        }
        rows.add(totalRow(regular, "Net amount", money(net)));
        rows.add(totalRow(regular, "VAT " + VAT_PERCENT + "%", money(vat)));
        rows.add(totalRow(semiBold, "Total due (EUR)", money(total)));

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setNumberOfFooterRows(3);
        table.setHeaderRowStyle(semiBold, Color.white, NAVY);
        table.setAlternateRowColor(STRIPE);
        table.setCellBorders(false);
        table.setWidth(right - left);
        table.setColumnWidthsInPercent(46f, 12f, 16f, 8f, 18f);
        for (int column = 1; column < 5; column++) {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.setLocation(left, 290f);
        float[] xy = table.drawOn(page);

        // A line over the total due, an artifact
        page.addArtifactBMC();
        page.setPenColor(NAVY);
        page.setPenWidth(1f);
        page.drawLine(middle, xy[1] - 20f, right, xy[1] - 20f);
        page.addEMC();

        // How to pay, and the QR code of the payment, which a banking app reads
        float paymentY = xy[1] + 40f;
        TextLine heading = new TextLine(semiBold, "Payment");
        heading.setStructureType(StructElem.H2);
        heading.setFontSize(13f);
        heading.setTextColor(NAVY);
        heading.setLocation(left, paymentY);
        heading.drawOn(page);

        TextBlock payment = new TextBlock(regular,
                "Please transfer EUR " + money(total) + " by 23 October 2026 to Lindenberg Paper & Print GmbH, "
                + "IBAN DE89 3704 0044 0532 0130 00, with the reference 2026-0147.\n\n"
                + "Or scan the code with your banking app, which fills in the transfer.");
        payment.setLineSpacing(1.4f);
        payment.setLocation(left, paymentY + 10f);
        payment.setWidth(330f);
        payment.drawOn(page);

        // An EPC QR code, which banking apps in Europe read as a SEPA credit
        // transfer: the version, the character set, the name, the IBAN, the
        // amount and the reference, one to a line, with error correction M.
        String epc = "BCD\n002\n1\nSCT\n\nLindenberg Paper & Print GmbH\nDE89370400440532013000\n"
                + "EUR" + money(total) + "\n\n\nInvoice 2026-0147";
        QRCode qr = new QRCode(epc, ErrorCorrectionLevel.M);
        qr.setModuleLength(2f);
        qr.setLocation(right - 90f, paymentY - 8f);
        float[] qrXY = qr.drawOn(page);
        TextLine caption = new TextLine(regular, "Scan to pay");
        caption.setFontSize(8f);
        caption.setTextColor(Color.gray);
        caption.setLocation(right - 90f, qrXY[1] + 12f);
        caption.drawOn(page);

        // What makes this invoice data as well
        TextBlock note = new TextBlock(regular,
                "This invoice is also data. It is a PDF/A-3 document that carries its content as "
                + "factur-x.xml, in the profile BASIC of Factur-X and ZUGFeRD, so that accounting "
                + "software reads the items and the amounts instead of someone typing them in. "
                + "Open the attachments of your PDF viewer to see it.");
        note.setFontSize(9f);
        note.setLineSpacing(1.4f);
        note.setTextColor(NAVY);
        note.setBackgroundColor(STRIPE);
        note.setCornerRadius(6f);
        note.setPadding(12f);
        note.setLocation(left, qrXY[1] + 45f);
        note.setWidth(right - left);
        note.drawOn(page);

        TextLine footer = new TextLine(regular,
                "Lindenberg Paper & Print GmbH  ·  Hafenstraße 12, 20457 Hamburg  ·  Registered in Hamburg, HRB 000000");
        footer.setFontSize(8f);
        footer.setTextColor(Color.gray);
        footer.setLocation((page.getWidth() - footer.getWidth()) / 2f, page.getHeight() - 40f);
        footer.drawOn(page);

        pdf.complete();
    }

    // Draws a line of text in the font and the color.
    private static void line(Page page, Font font, String text, float x, float y, int color)
            throws Exception {
        TextLine line = new TextLine(font, text);
        line.setTextColor(color);
        line.setLocation(x, y);
        line.drawOn(page);
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            Cell cell = new Cell(font, text);
            cell.setPadding(6f);
            row.add(cell);
        }
        return row;
    }

    // A row of the totals: its label over the first four columns, right
    // aligned, and the amount. The cells the label spans are there and empty.
    private static List<Cell> totalRow(Font font, String label, String amount) {
        List<Cell> row = row(font, label, "", "", "", amount);
        row.get(0).setColSpan(4);
        row.get(0).setTextAlignment(Alignment.RIGHT);
        return row;
    }

    // The amount of cents as euros and cents, with a comma between the thousands.
    private static String money(long cents) {
        String euros = String.valueOf(cents / 100);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < euros.length(); i++) {
            if (i > 0 && (euros.length() - i) % 3 == 0) {
                sb.append(',');
            }
            sb.append(euros.charAt(i));
        }
        long rest = cents % 100;
        return sb.append(rest < 10 ? ".0" : ".").append(rest).toString();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_55();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_55 => %4d ms%n", time1 - time0);
    }
}   // End of Example_55.java
