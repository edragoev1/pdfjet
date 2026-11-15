/*
 * Example_84.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PDFjet.NET;

/**
 * Example_84.cs
 * A workshop registration form, to print and fill in by hand: who comes,
 * the sessions of the day with a column to tick the ones they attend, the
 * lunch and the photographs, and how to pay, with a QR code that opens the
 * payment page on a phone.
 *
 * This is a form people fill in, not a document with data, though it carries
 * the program of the day. The fields are drawn as Example_72 draws the job
 * application, with the same field helper: the label above its box, the
 * boxes on a grid, and an asterisk on the label of a field that is required.
 * The sessions are a Table: the organizer prints the time, the title and the
 * room, and leaves the last column empty, a cell to tick.
 *
 * The QR code holds the address of the payment page. It is a figure with a
 * description, which a screen reader reads, and the address is in the text
 * beside it too, for whoever cannot scan it. It is measured before it is placed,
 * with DrawOn of a null page, so that it lines up with the right margin.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * sessions a table read by its column headers, and the boxes are artifacts.
 * The layout is that of the workshop sign-up template of PDFjet Forms. The
 * organizer, the workshop and the address are made up.
 */
public class Example_84 {
    private const String ORGANIZER = "Lantern Hill Design School";
    private const String WORKSHOP = "Better PDFs: a hands-on workshop, Saturday, January 16, "
            + "2027, 9 a.m. to 4 p.m. Fill in this form and send it back to reserve your place. "
            + "Lunch and coffee are included.";
    private const String PAYMENT_PAGE = "https://example.com/workshops/2027-01-16/pay";

    // The sessions of the day: the time, the title and the room
    private static readonly String[][] SESSIONS = {
        new String[] {"9 a.m.", "Layouts that print the same everywhere", "A"},
        new String[] {"10:30 a.m.", "Forms your clients fill in online", "A"},
        new String[] {"1 p.m.", "Barcodes and QR codes", "B"},
        new String[] {"2:30 p.m.", "Archival and accessible: PDF/A and PDF/UA", "B"},
    };

    private const int BLUE = 0x1f58b0;
    private const int RED = 0xb00020;
    private const int RULE = 0xdde1e8;
    private const int BOX = 0x4d4d4d;
    private const int HEADER = 0xe3edf9;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    private const float LEFT = 48f;
    private const float WIDTH = 516f;
    private const float GAP = 12f;
    private const float HALF = (WIDTH - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    private readonly Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_84() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_84.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Workshop registration");
        pdf.SetAuthor(ORGANIZER);
        pdf.SetLanguage("en-US");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, the workshop, and a rule
        Heading("Workshop registration", StructElem.H1, 20f, 62f);
        float y = Paragraph(WORKSHOP, LEFT, 74f, WIDTH) + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // Who comes
        y = Heading("About you", StructElem.H2, 14f, y + 46f);
        Field("Full name *", LEFT, y, HALF, 1, null);
        y = Field("Email *", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("Company", LEFT, y, HALF, 1, null);
        y = Field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The sessions, and an empty column to tick the ones they attend
        y = Heading("Sessions", StructElem.H2, 14f, y + 18f);
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Time", "Session", "Room", "I attend"));
        foreach (String[] session in SESSIONS) {
            rows.Add(Row(textFont, session[0], session[1], session[2], ""));
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(5f, 5f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BOX);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(17f, 58f, 10f, 15f);
        table.SetLocation(LEFT, y);
        y = table.DrawOn(page)[1] + 16f;

        // The lunch and the photographs
        new CheckBox(textFont, "I need a vegetarian lunch")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        new CheckBox(textFont, "I agree to be photographed during the workshop")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);
        y += 50f;

        // The payment: the text on the left, the QR code of the payment page
        // on the right, its top at the top of the text
        y = Heading("Payment", StructElem.H2, 14f, y + 18f);
        QRCode qr = new QRCode(PAYMENT_PAGE, ErrorCorrectionLevel.M);
        qr.SetModuleLength(2f);
        qr.SetAltDescription("QR code of the payment page, " + PAYMENT_PAGE);
        float qrSize = qr.DrawOn(null)[0];      // Measured, at 0, 0, so its size
        qr.SetLocation(LEFT + WIDTH - qrSize, y);
        qr.DrawOn(page);
        float end = Paragraph("The fee is $120, lunch included. Pay with your phone: scan the code. "
                + "Or open " + PAYMENT_PAGE + " on a computer. Your seat is kept once we have "
                + "your form and your payment.", LEFT, y, WIDTH - qrSize - 2 * GAP);
        y = Math.Max(end, y + qrSize) + 16f;

        Field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        Field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        pdf.Complete();
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> Row(Font font, params String[] texts) {
        List<Cell> row = new List<Cell>();
        foreach (String text in texts) {
            row.Add(new Cell(font, text));
        }
        return row;
    }

    // Draws the text wrapped to the width, its top at y, and returns where
    // it ended.
    private float Paragraph(String text, float x, float y, float width) {
        TextBlock block = new TextBlock(textFont, text);
        block.SetLineSpacing(1.15f);
        block.SetLocation(x, y);
        block.SetWidth(width);
        return block.DrawOn(page)[1];
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    private float Heading(String text, StructElem level, float size, float y) {
        TextLine heading = new TextLine(labelFont, text);
        heading.SetStructureType(level);
        heading.SetFontSize(size);
        heading.SetTextColor(BLUE);
        heading.SetLocation(LEFT, y);
        heading.DrawOn(page);
        return y + 16f;
    }

    // Draws the label of a field, its top at y.
    private void Label(String text, float x, float y) {
        TextLine label = new TextLine(labelFont, text);
        label.SetLocation(x, y + labelFont.GetAscent());
        label.DrawOn(page);
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    private float Field(String text, float x, float y, float width, int lines, String value) {
        Label(text, x, y);
        float top = y + 16f;
        float height = BOX_HEIGHT + (lines - 1) * LINE_HEIGHT;
        Rect box = new Rect(x, top, width, height);
        box.SetBorderColor(BOX);
        box.SetBorderWidth(0.5f);
        box.DrawOn(page);
        if (value != null) {
            TextLine line = new TextLine(textFont, value);
            line.SetTextColor(BLUE);
            line.SetLocation(x + 6f, top + 16f);
            line.DrawOn(page);
        }
        return top + height + 12f;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private void Rule(float y) {
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_84();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_84 => {time1 - time0,4} ms");
    }
}   // End of Example_84.cs
