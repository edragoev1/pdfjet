/*
 * Example_54.cs
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
 * Example_54.cs
 * A quote of a small design studio to a prospective customer, on Letter
 * paper: the studio and the quote number at the top with the date it is
 * valid until, who it is for and the project, a table of the work in days,
 * the subtotal, the sales tax, the total and the deposit, the terms, and
 * lines for the customer to sign the quote and send it back.
 *
 * A quote is a finished document that the customer receives, not a form.
 * The amounts are decimal, computed from the lines of work, and the date
 * it is valid until and the deposit are computed too, so that a change of
 * the day rate changes every number that depends on it. The table goes on
 * to as many pages as it needs; what follows it moves to a new page when it
 * no longer fits. A signed quote is a contract that both sides keep, so the
 * document is PDF/A-3a and PDF/UA-1 at once, as the invoice of Example_53
 * is. The layout is that of the quote template of PDFjet Forms. The
 * companies and the numbers are made up.
 */
public class Example_54 {
    // A line of the work: so many days at the day rate.
    private sealed class Work {
        internal readonly String description;
        internal readonly int days;

        internal Work(String description, int days) {
            this.description = description;
            this.days = days;
        }

        internal decimal Amount() {
            return DAY_RATE * days;
        }
    }

    // The data of the quote. In an application it comes from the database.
    private const String NUMBER = "Q-2026-017";
    private static readonly DateTime DATE = new DateTime(2026, 10, 5);
    private const int VALID_DAYS = 30;
    private const String CURRENCY = "CAD";
    private const decimal DAY_RATE = 800.00m;
    private const String TAX_NAME = "HST";
    private const decimal TAX_RATE = 0.13m;
    private const decimal DEPOSIT_RATE = 0.30m;

    private const String SELLER = "Northwind Studio";
    private const String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    private const String CUSTOMER =
            "Tamarack Hill Community Theatre\n27 Mill Street\nOwen Sound, ON N4K 2A1";
    private const String PROJECT = "The program book of the 2027 season: its design, "
            + "a layout of 40 pages, and an accessible PDF/UA edition.";

    private static readonly Work[] WORK = {
        new Work("Discovery and a plan of the content", 1),
        new Work("Design of the layout and the cover", 3),
        new Work("Layout of 40 pages, two rounds of changes", 4),
        new Work("Accessible PDF/UA edition, checked page by page", 1),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;
    private const int GRAY = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"
    private const float GAP = 14f;

    public Example_54() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_54.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.SetTitle("Quote " + NUMBER);
        pdf.SetAuthor(SELLER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The totals, from the work. The tax is rounded to the cent once, on
        // the subtotal, and the deposit is a share of the total with tax.
        decimal subtotal = 0m;
        foreach (Work work in WORK) {
            subtotal += work.Amount();
        }
        decimal tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);
        decimal total = subtotal + tax;
        decimal deposit = Math.Round(total * DEPOSIT_RATE, 2, MidpointRounding.AwayFromZero);

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).SetLocation(left, 80f).SetWidth(250f).DrawOn(page);

        TextLine title = new TextLine(semiBold, "Quote");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(BLUE);
        title.SetLocation(right - title.GetWidth(), 72f);
        title.DrawOn(page);

        new TextBlock(regular, "No. " + NUMBER + "\nDate: " + Iso(DATE)
                + "\nValid until: " + Iso(DATE.AddDays(VALID_DAYS)))
                .SetTextAlignment(Alignment.RIGHT)
                .SetLocation(right - 200f, 94f)
                .SetWidth(200f)
                .DrawOn(page);

        Line(page, left, right, 148f, RULE, 1f);

        // Who the quote is for, and the project, side by side. The taller of
        // the two decides where the next part starts.
        Heading(page, semiBold, "Prepared for", left, 182f);
        float middle = (left + right) / 2f;
        float y1 = Paragraph(page, regular, CUSTOMER, left, 192f, middle - left - GAP);
        Heading(page, semiBold, "Project", middle, 182f);
        float y = Math.Max(y1, Paragraph(page, regular, PROJECT, middle, 192f, right - middle));

        y = Paragraph(page, regular, "Thank you for asking us. Below is what we propose, and "
                + "what it costs. Prices are in Canadian dollars.", left, y + GAP, right - left);

        // The work, in a table that goes on to more pages when it needs them
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Work", "Days", "Rate", "Amount"));
        foreach (Work work in WORK) {
            rows.Add(Row(regular, work.description, work.days.ToString(),
                    Money(DAY_RATE), Money(work.Amount())));
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(right - left);
        table.SetColumnWidthsInPercent(55f, 15f, 15f, 15f);
        for (int column = 1; column < 4; column++) {
            table.SetTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.SetLocation(left, MARGIN);
        table.SetFirstPageTopMargin(y + GAP);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The totals, the terms and the signature lines stay together: about
        // 300 points. When they no longer fit under the table, they go on a
        // page of their own.
        y = end[1] + 22f;
        if (y + 300f > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        String rate = (TAX_RATE * 100).ToString("0.####", CultureInfo.InvariantCulture);
        String share = (DEPOSIT_RATE * 100).ToString("0.####", CultureInfo.InvariantCulture);
        DrawRight(page, regular, 11f, "Subtotal: " + Money(subtotal), right, y);
        DrawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + Money(tax), right, y + 15f);
        DrawRight(page, semiBold, 14f, "Total: " + Money(total) + " " + CURRENCY, right, y + 43f);
        DrawRight(page, regular, 11f, "Deposit, " + share + "%: " + Money(deposit), right, y + 62f);

        y = Heading(page, semiBold, "Terms", left, y + 100f);
        y = Paragraph(page, regular, "This quote is valid for " + VALID_DAYS + " days. A deposit "
                + "of " + share + "% starts the work, and the rest is due on delivery. Two rounds "
                + "of changes are included; more are " + Money(DAY_RATE) + " a day. The work takes "
                + "about four weeks from the deposit.", left, y, right - left);

        y = Heading(page, semiBold, "Acceptance", left, y + 2 * GAP + 6f);
        y = Paragraph(page, regular, "To accept this quote, sign below and send it back to us, "
                + "with the deposit.", left, y, right - left);

        // A line to sign on and one to date, each with its label under it
        float dateX = left + 0.6f * (right - left);
        y += 48f;
        Line(page, left, dateX - GAP, y, GRAY, 0.75f);
        Line(page, dateX, right, y, GRAY, 0.75f);
        new TextLine(regular, "Name and signature").SetFontSize(10f).SetTextColor(GRAY)
                .SetLocation(left, y + 14f).DrawOn(page);
        new TextLine(regular, "Date").SetFontSize(10f).SetTextColor(GRAY)
                .SetLocation(dateX, y + 14f).DrawOn(page);

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

    // Draws a heading of a part, its baseline at y, and returns where the
    // text under it starts.
    private static float Heading(Page page, Font font, String text, float x, float y) {
        new TextLine(font, text).SetStructureType(StructElem.H2).SetFontSize(12f)
                .SetLocation(x, y).DrawOn(page);
        return y + 10f;
    }

    // Draws the text wrapped to the width, its top at y, and returns the y
    // where it ended.
    private static float Paragraph(Page page, Font font, String text, float x, float y, float width) {
        return new TextBlock(font, text).SetLineSpacing(1.15f).SetLocation(x, y).SetWidth(width)
                .DrawOn(page)[1];
    }

    // A line across, decoration or a line to sign on that its label names,
    // so an artifact that a screen reader skips.
    private static void Line(Page page, float x1, float x2, float y, int color, float width) {
        Line line = new Line(x1, y, x2, y);
        line.SetStrokeColor(color);
        line.SetStrokeWidth(width);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> Row(Font font, params String[] texts) {
        List<Cell> row = new List<Cell>();
        foreach (String text in texts) {
            row.Add(new Cell(font, text));
        }
        return row;
    }

    // Draws a line of text in the font and the size, its baseline at y and
    // its end at x, as the amounts line up on their right.
    private static void DrawRight(Page page, Font font, float size, String text, float x, float y) {
        TextLine line = new TextLine(font, text);
        line.SetFontSize(size);
        line.SetLocation(x - line.GetWidth(), y);
        line.DrawOn(page);
    }

    // The amount with two decimals and commas between the thousands, as
    // 2,400.00, whatever the culture of the computer.
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
        new Example_54();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_54 => {time1 - time0,4} ms");
    }
}   // End of Example_54.cs
