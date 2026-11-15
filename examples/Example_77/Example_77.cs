/*
 * Example_77.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using PDFjet.NET;

/**
 * Example_77.cs
 * A payment reminder, a letter to a customer whose invoice is past due: the
 * letterhead, the date, the address, the invoice as the subject, a friendly
 * paragraph, a small table of what was invoiced, credited and paid, the
 * amount still open, how to pay it, and the signature.
 *
 * A reminder is a finished document that the customer receives, not a form.
 * The letter is placed as the business letter of Example_59 is, each part
 * under the one before it, from where DrawOn says the last one ended; the
 * table of the account is one more part, and the paragraphs after it move
 * down when it has more lines. The amount still open is computed from the
 * lines of the account, in decimal, and is the same figure in the table
 * and in the text, as both come from one variable.
 *
 * The document is PDF/UA-1: the subject is the heading of the letter, the
 * account a table that a screen reader reads by its column headers, and the
 * rule under the letterhead is an artifact. The layout is that of the
 * payment reminder template of PDFjet Forms. The people and the companies
 * are made up.
 */
public class Example_77 {
    // A line of the customer's account: an invoice is positive, a credit
    // note and a payment negative.
    private sealed class Entry {
        internal readonly DateTime date;
        internal readonly String description;
        internal readonly decimal amount;

        internal Entry(String date, String description, String amount) {
            this.date = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            this.description = description;
            this.amount = decimal.Parse(amount, CultureInfo.InvariantCulture);
        }
    }

    private const String SENDER = "Northwind Studio";
    private const String SENDER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";

    private static readonly DateTime DATE = new DateTime(2026, 11, 9);
    private const String RECIPIENT = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4";
    private const String INVOICE = "2026-042";
    private static readonly DateTime DUE = new DateTime(2026, 10, 25);
    private const String CURRENCY = "CAD";
    private const String ACCOUNT = "01234-567-8901234";

    private static readonly Entry[] ENTRIES = {
        new Entry("2026-09-25", "Invoice 2026-042, the annual report", "2495.61"),
        new Entry("2026-10-05", "Credit note CN-2026-007, ten copies damaged in delivery", "-67.80"),
        new Entry("2026-10-20", "Your payment, thank you", "-1000.00"),
    };

    private const String SIGNER = "Dana Whitfield\nDesign Lead, Northwind Studio";

    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float LEFT = 48f;
    private const float WIDTH = 516f;            // Letter, 612 wide, less the margins
    private const float GAP = 14f;               // Between the parts of the letter

    public Example_77() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_77.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Payment reminder, invoice " + INVOICE);
        pdf.SetAuthor(SENDER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // What is still open, from the lines of the account
        decimal open = 0m;
        foreach (Entry entry in ENTRIES) {
            open += entry.amount;
        }

        Page page = new Page(pdf, Letter.PORTRAIT);
        String longDate = "MMMM d, yyyy";
        String monthDay = "MMMM d";

        // The letterhead, and a thin rule under it, decoration only
        new TextLine(semiBold, SENDER).SetFontSize(16f).SetLocation(LEFT, 64f).DrawOn(page);
        float y = Paragraph(page, regular, SENDER_ADDRESS, 80f) + GAP;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The date, the recipient, and the subject, the heading of the letter
        y = Paragraph(page, regular, DATE.ToString(longDate, CultureInfo.InvariantCulture), y + 2 * GAP);
        y = Paragraph(page, regular, RECIPIENT, y + GAP);
        TextBlock subject = new TextBlock(semiBold,
                "Invoice " + INVOICE + ", due " + DUE.ToString(longDate, CultureInfo.InvariantCulture));
        subject.SetFontSize(12f);
        subject.SetStructureType(StructElem.H1);
        subject.SetLocation(LEFT, y + GAP);
        subject.SetWidth(WIDTH);
        y = subject.DrawOn(page)[1];

        y = Paragraph(page, regular, "Dear Ms. Raman,", y + GAP);
        y = Paragraph(page, regular, "This is a friendly reminder that invoice " + INVOICE + " of "
                + ENTRIES[0].date.ToString(monthDay, CultureInfo.InvariantCulture)
                + ", for the layout of your annual report, was due on "
                + DUE.ToString(monthDay, CultureInfo.InvariantCulture)
                + ". The amount still open is " + Money(open) + " " + CURRENCY
                + ", after the credit note and the payment below.", y + GAP);

        // The account: what was invoiced, credited and paid, and what is open
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Date", "Description", "Amount"));
        foreach (Entry entry in ENTRIES) {
            rows.Add(Row(regular, Iso(entry.date), entry.description, Money(entry.amount)));
        }
        rows.Add(Row(semiBold, "", "Amount open, " + CURRENCY, Money(open)));

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetNumberOfFooterRows(1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetFooterRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(18f, 62f, 20f);
        table.SetTextAlignmentInColumn(2, Alignment.RIGHT);
        table.SetLocation(LEFT, y + GAP);
        y = table.DrawOn(page)[1];

        y = Paragraph(page, regular, "Invoice 2026-051 of October 28, also on our statement of "
                + "October 31, is not due until November 27 and is not part of this reminder.", y + GAP);
        y = Paragraph(page, regular, "If you have paid it in the last few days, thank you, and please "
                + "disregard this letter. If anything in the invoice is unclear, just reply to this "
                + "letter and I will be glad to help.", y + GAP);
        y = Paragraph(page, regular, "You can pay by bank transfer to " + SENDER + ", account "
                + ACCOUNT + ", with the invoice number in the message.", y + GAP);

        // The closing, room to sign by hand, and the name under it
        y = Paragraph(page, regular, "Kind regards,", y + GAP);
        Paragraph(page, regular, SIGNER, y + 3 * GAP);

        pdf.Complete();
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns the y where it ended, for the next part of the letter.
    private static float Paragraph(Page page, Font font, String text, float y) {
        TextBlock block = new TextBlock(font, text);
        block.SetLineSpacing(1.25f);
        block.SetLocation(LEFT, y);
        block.SetWidth(WIDTH);
        return block.DrawOn(page)[1];
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> Row(Font font, params String[] texts) {
        List<Cell> row = new List<Cell>();
        foreach (String text in texts) {
            row.Add(new Cell(font, text));
        }
        return row;
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,427.81, and a minus sign before a credit. The culture is given, or a
    // computer set to French writes 1 427,81.
    private static String Money(decimal amount) {
        return amount.ToString("N2", CultureInfo.InvariantCulture);
    }

    // The date as 2026-09-25, in the form of ISO 8601, whatever the culture of
    // the computer.
    private static String Iso(DateTime date) {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_77();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_77 => {time1 - time0,4} ms");
    }
}   // End of Example_77.cs
