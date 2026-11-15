/*
 * Example_84.java
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
 * A workshop registration form, to print and fill in by hand: who comes,
 * the sessions of the day with a column to tick the ones they attend, the
 * lunch and the photographs, and how to pay, with a QR code that opens the
 * payment page on a phone.
 * <p>
 * This is a form people fill in, not a document with data, though it carries
 * the program of the day. The fields are drawn as Example_72 draws the job
 * application, with the same field helper: the label above its box, the
 * boxes on a grid, and an asterisk on the label of a field that is required.
 * The sessions are a Table: the organizer prints the time, the title and the
 * room, and leaves the last column empty, a cell to tick.
 * </p>
 * <p>
 * The QR code holds the address of the payment page. It is a figure with a
 * description, which a screen reader reads, and the address is in the text
 * beside it too, for whoever cannot scan it. It is measured before it is placed,
 * with drawOn of a null page, so that it lines up with the right margin.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the sections are headings, the
 * sessions a table read by its column headers, and the boxes are artifacts.
 * The layout is that of the workshop sign-up template of PDFjet Forms. The
 * organizer, the workshop and the address are made up.
 * </p>
 *
 * @see QRCode
 * @see Table
 * @see CheckBox
 */
public class Example_84 {
    static final String ORGANIZER = "Lantern Hill Design School";
    static final String WORKSHOP = "Better PDFs: a hands-on workshop, Saturday, January 16, "
            + "2027, 9 a.m. to 4 p.m. Fill in this form and send it back to reserve your place. "
            + "Lunch and coffee are included.";
    static final String PAYMENT_PAGE = "https://example.com/workshops/2027-01-16/pay";

    // The sessions of the day: the time, the title and the room
    static final String[][] SESSIONS = {
        {"9 a.m.", "Layouts that print the same everywhere", "A"},
        {"10:30 a.m.", "Forms your clients fill in online", "A"},
        {"1 p.m.", "Barcodes and QR codes", "B"},
        {"2:30 p.m.", "Archival and accessible: PDF/A and PDF/UA", "B"},
    };

    static final int BLUE = 0x1f58b0;
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;
    static final int HEADER = 0xe3edf9;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static final float LEFT = 48f;
    static final float WIDTH = 516f;
    static final float GAP = 12f;
    static final float HALF = (WIDTH - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    private final Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_84() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_84.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Workshop registration");
        pdf.setAuthor(ORGANIZER);
        pdf.setLanguage("en-US");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, the workshop, and a rule
        heading("Workshop registration", StructElem.H1, 20f, 62f);
        float y = paragraph(WORKSHOP, LEFT, 74f, WIDTH) + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // Who comes
        y = heading("About you", StructElem.H2, 14f, y + 46f);
        field("Full name *", LEFT, y, HALF, 1, null);
        y = field("Email *", LEFT + HALF + GAP, y, HALF, 1, null);
        field("Company", LEFT, y, HALF, 1, null);
        y = field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The sessions, and an empty column to tick the ones they attend
        y = heading("Sessions", StructElem.H2, 14f, y + 18f);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Time", "Session", "Room", "I attend"));
        for (String[] session : SESSIONS) {
            rows.add(row(textFont, session[0], session[1], session[2], ""));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(5f, 5f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BOX);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(17f, 58f, 10f, 15f);
        table.setLocation(LEFT, y);
        y = table.drawOn(page)[1] + 16f;

        // The lunch and the photographs
        new CheckBox(textFont, "I need a vegetarian lunch")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        new CheckBox(textFont, "I agree to be photographed during the workshop")
                .setBorderColor(BOX)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);
        y += 50f;

        // The payment: the text on the left, the QR code of the payment page
        // on the right, its top at the top of the text
        y = heading("Payment", StructElem.H2, 14f, y + 18f);
        QRCode qr = new QRCode(PAYMENT_PAGE, ErrorCorrectionLevel.M);
        qr.setModuleLength(2f);
        qr.setAltDescription("QR code of the payment page, " + PAYMENT_PAGE);
        float qrSize = qr.drawOn(null)[0];      // Measured, at 0, 0, so its size
        qr.setLocation(LEFT + WIDTH - qrSize, y);
        qr.drawOn(page);
        float end = paragraph("The fee is $120, lunch included. Pay with your phone: scan the code. "
                + "Or open " + PAYMENT_PAGE + " on a computer. Your seat is kept once we have "
                + "your form and your payment.", LEFT, y, WIDTH - qrSize - 2 * GAP);
        y = Math.max(end, y + qrSize) + 16f;

        field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        pdf.complete();
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    // Draws the text wrapped to the width, its top at y, and returns where
    // it ended.
    private float paragraph(String text, float x, float y, float width) throws Exception {
        TextBlock block = new TextBlock(textFont, text);
        block.setLineSpacing(1.15f);
        block.setLocation(x, y);
        block.setWidth(width);
        return block.drawOn(page)[1];
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    private float heading(String text, StructElem level, float size, float y) throws Exception {
        TextLine heading = new TextLine(labelFont, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(BLUE);
        heading.setLocation(LEFT, y);
        heading.drawOn(page);
        return y + 16f;
    }

    // Draws the label of a field, its top at y.
    private void label(String text, float x, float y) throws Exception {
        TextLine label = new TextLine(labelFont, text);
        label.setLocation(x, y + labelFont.getAscent());
        label.drawOn(page);
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    private float field(String text, float x, float y, float width, int lines, String value)
            throws Exception {
        label(text, x, y);
        float top = y + 16f;
        float height = BOX_HEIGHT + (lines - 1) * LINE_HEIGHT;
        Rect box = new Rect(x, top, width, height);
        box.setBorderColor(BOX);
        box.setBorderWidth(0.5f);
        box.drawOn(page);
        if (value != null) {
            TextLine line = new TextLine(textFont, value);
            line.setTextColor(BLUE);
            line.setLocation(x + 6f, top + 16f);
            line.drawOn(page);
        }
        return top + height + 12f;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private void rule(float y) throws Exception {
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.setStrokeColor(RULE);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_84();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_84 => %4d ms%n", time1 - time0);
    }
}   // End of Example_84.java
