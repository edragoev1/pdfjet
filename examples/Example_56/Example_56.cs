/*
 * Example_56.cs
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
 * Example_56.cs
 * A statement of account of a small design studio to a customer, on Letter
 * paper: the studio, the date and the period at the top, the customer, a
 * table of the invoices, credit notes and payments of the period with the
 * balance after each, the balance due, and when to pay it.
 *
 * A statement is a finished document that the customer receives, not a
 * form. The balance of each line is computed from the one before it, the
 * charges added and the credits taken off, starting from the balance brought
 * forward from the period before; the balance due is the balance of the last
 * line. The amounts are decimal, so that they add up to the cent over any
 * number of lines, and the table goes on to as many pages as it needs, with
 * its header row on each.
 *
 * The lines are those of Example_53, the invoice, and Example_67, the credit
 * note against it, with a payment and a later invoice. Both sides keep a
 * statement with their books, so the document is PDF/A-3a and PDF/UA-1 at
 * once. The layout is that of the statement of account template of PDFjet
 * Forms. The companies and the numbers are made up.
 */
public class Example_56 {
    // A line of the statement: a charge, such as an invoice, or a credit,
    // such as a payment or a credit note. One of the two is zero.
    private sealed class Entry {
        internal readonly DateTime date;
        internal readonly String description;
        internal readonly decimal charge;
        internal readonly decimal credit;

        internal Entry(DateTime date, String description, String charge, String credit) {
            this.date = date;
            this.description = description;
            this.charge = decimal.Parse(charge, CultureInfo.InvariantCulture);
            this.credit = decimal.Parse(credit, CultureInfo.InvariantCulture);
        }
    }

    // The data of the statement. In an application it comes from the books.
    private static readonly DateTime PERIOD_START = new DateTime(2026, 9, 1);
    private static readonly DateTime DATE = new DateTime(2026, 10, 31);
    private const int PAYMENT_DAYS = 30;
    private const String CURRENCY = "CAD";
    private const decimal BALANCE_FORWARD = 0.00m;

    private const String SELLER = "Northwind Studio";
    private const String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    private const String CUSTOMER = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4";

    private static readonly Entry[] ENTRIES = {
        new Entry(new DateTime(2026, 9, 25), "Invoice 2026-042, annual report", "2495.61", "0"),
        new Entry(new DateTime(2026, 10, 5), "Credit note CN-2026-007", "0", "67.80"),
        new Entry(new DateTime(2026, 10, 20), "Payment, thank you", "0", "1000.00"),
        new Entry(new DateTime(2026, 10, 28), "Invoice 2026-051, display stand", "452.00", "0"),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_56() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_56.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.SetTitle("Statement of account, " + Iso(DATE));
        pdf.SetAuthor(SELLER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;

        // The header: the studio on the left, the title and the dates on the right
        new TextLine(semiBold, SELLER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).SetLocation(left, 80f).SetWidth(250f).DrawOn(page);

        TextLine title = new TextLine(semiBold, "Statement");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(BLUE);
        title.SetLocation(right - title.GetWidth(), 72f);
        title.DrawOn(page);

        new TextBlock(regular, "Date: " + Iso(DATE) + "\nPeriod: " + Iso(PERIOD_START) + " to " + Iso(DATE))
                .SetTextAlignment(Alignment.RIGHT)
                .SetLocation(right - 250f, 94f)
                .SetWidth(250f)
                .DrawOn(page);

        // A thin rule under the header, decoration only, so an artifact
        Line rule = new Line(left, 148f, right, 148f);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The customer
        new TextLine(semiBold, "Account").SetStructureType(StructElem.H2).SetFontSize(12f)
                .SetLocation(left, 182f).DrawOn(page);
        float y = new TextBlock(regular, CUSTOMER).SetLocation(left, 192f).SetWidth(250f)
                .DrawOn(page)[1] + 24f;

        // The lines, each with the balance after it. The first line is the
        // balance brought forward, so that the column adds up from the top.
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Date", "Description", "Charges", "Credits", "Balance"));
        decimal balance = BALANCE_FORWARD;
        rows.Add(Row(regular, Iso(PERIOD_START), "Balance forward", "", "", Money(balance)));
        foreach (Entry entry in ENTRIES) {
            balance = balance + entry.charge - entry.credit;
            rows.Add(Row(regular, Iso(entry.date), entry.description,
                    BlankIfZero(entry.charge), BlankIfZero(entry.credit), Money(balance)));
        }

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(right - left);
        table.SetColumnWidthsInPercent(16f, 36f, 16f, 16f, 16f);
        for (int column = 2; column < 5; column++) {
            table.SetTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.SetLocation(left, MARGIN);
        table.SetFirstPageTopMargin(y);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The balance due and when to pay it, under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 30f;
        if (y + 70f > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        TextLine due = new TextLine(semiBold, "Balance due: " + Money(balance) + " " + CURRENCY);
        due.SetFontSize(14f);
        due.SetLocation(right - due.GetWidth(), y);
        due.DrawOn(page);

        new TextBlock(regular, "Please pay the balance by "
                + Iso(DATE.AddDays(PAYMENT_DAYS))
                + ", by bank transfer. If a payment has crossed with this statement, thank you, "
                + "and please disregard it.")
                .SetLineSpacing(1.15f)
                .SetLocation(left, y + 22f)
                .SetWidth(right - left)
                .DrawOn(page);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.Count; i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.Count);
            footer.SetFontSize(10f);
            footer.SetLocation(right - footer.GetWidth(), page.GetHeight() - 52f);
            pages[i].AddArtifactBMC();
            footer.DrawOn(pages[i]);
            pages[i].AddEMC();
        }
        pdf.AddPages(pages);
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

    // A line has a charge or a credit, and the other column is left empty
    // rather than 0.00, so that the eye finds the amounts.
    private static String BlankIfZero(decimal amount) {
        return amount == 0m ? "" : Money(amount);
    }

    // The amount with two decimals and commas between the thousands, as
    // 2,495.61, whatever the culture of the computer.
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
        new Example_56();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_56 => {time1 - time0,4} ms");
    }
}   // End of Example_56.cs
