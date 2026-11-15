/*
 * Example_53.cs
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
 * Example_53.cs
 * An invoice of a small design studio to a customer, on Letter paper: the
 * seller and the invoice number at the top, the customer, a table of the
 * items, the subtotal, the sales tax and the total, the payment terms, and
 * "Page 1 of 1" at the bottom.
 *
 * The amounts are decimal, never float or double, so that they add up to
 * the cent, and the totals are computed from the items rather than typed in.
 * The table of the items goes on to as many pages as it needs, with its
 * header row on each, so the same code prints an invoice of three items or of
 * three hundred; the totals go under the last row.
 *
 * An invoice is kept for years by both sides, so the document is archival as
 * well as accessible, PDF/A-3a and PDF/UA-1 at once: the title is a heading,
 * the items a table that a screen reader reads by its column headers. The
 * layout is that of the invoice template of PDFjet Forms. The companies and
 * the numbers are made up.
 */
public class Example_53 {
    // A line of the invoice. The unit price is a decimal, in dollars.
    private sealed class Item {
        internal readonly String description;
        internal readonly int quantity;
        internal readonly decimal unitPrice;

        internal Item(String description, int quantity, String unitPrice) {
            this.description = description;
            this.quantity = quantity;
            this.unitPrice = decimal.Parse(unitPrice, CultureInfo.InvariantCulture);
        }

        internal decimal Amount() {
            return unitPrice * quantity;
        }
    }

    // The data of the invoice. In an application it comes from the database.
    private const String NUMBER = "2026-042";
    private static readonly DateTime DATE = new DateTime(2026, 9, 25);
    private const int PAYMENT_DAYS = 30;
    private const String CURRENCY = "CAD";
    private const String TAX_NAME = "HST";               // Ontario's sales tax
    private const decimal TAX_RATE = 0.13m;

    private const String SELLER = "Northwind Studio";
    private const String SELLER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    private const String CUSTOMER = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4";

    private static readonly Item[] ITEMS = {
        new Item("Layout of the annual report, 48 pages", 1, "1200.00"),
        new Item("Accessible PDF/UA edition", 1, "400.00"),
        new Item("Charts redrawn from the spreadsheets", 6, "45.00"),
        new Item("Printed copies, saddle stitched", 50, "6.00"),
        new Item("Courier to Toronto", 1, "38.50"),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_53() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_53.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.SetTitle("Invoice " + NUMBER);
        pdf.SetAuthor(SELLER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The totals, from the items. The tax is rounded to the cent once, on
        // the subtotal, as a cash register does.
        decimal subtotal = 0m;
        foreach (Item item in ITEMS) {
            subtotal += item.Amount();
        }
        decimal tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);
        decimal total = subtotal + tax;

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;

        // The header: the seller on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).SetLocation(left, 80f).SetWidth(250f).DrawOn(page);

        TextLine title = new TextLine(semiBold, "Invoice");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(BLUE);
        title.SetLocation(right - title.GetWidth(), 72f);
        title.DrawOn(page);

        String numbers = "No. " + NUMBER
                + "\nDate: " + Iso(DATE)
                + "\nDue: " + Iso(DATE.AddDays(PAYMENT_DAYS));
        new TextBlock(regular, numbers)
                .SetTextAlignment(Alignment.RIGHT)
                .SetLocation(right - 200f, 94f)
                .SetWidth(200f)
                .DrawOn(page);

        // A thin rule under the header, decoration only, so an artifact that a
        // screen reader skips
        Line rule = new Line(left, 148f, right, 148f);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The customer
        TextLine billTo = new TextLine(semiBold, "Bill to");
        billTo.SetStructureType(StructElem.H2);
        billTo.SetFontSize(12f);
        billTo.SetLocation(left, 182f);
        billTo.DrawOn(page);
        new TextBlock(regular, CUSTOMER).SetLocation(left, 192f).SetWidth(250f).DrawOn(page);

        // The items: a header row, repeated on every page the table goes on to
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Description", "Quantity", "Price", "Amount"));
        foreach (Item item in ITEMS) {
            rows.Add(Row(regular, item.description, item.quantity.ToString(),
                    Money(item.unitPrice), Money(item.Amount())));
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
        // Under the customer on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.SetLocation(left, MARGIN);
        table.SetFirstPageTopMargin(260f);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The totals, under the last row. When they no longer fit on its page,
        // they go on a page of their own.
        float y = end[1] + 22f;
        if (y + 90f > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        String rate = (TAX_RATE * 100).ToString("0.####", CultureInfo.InvariantCulture);
        DrawRight(page, regular, 11f, "Subtotal: " + Money(subtotal), right, y);
        DrawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + Money(tax), right, y + 15f);
        DrawRight(page, semiBold, 14f, "Total: " + Money(total) + " " + CURRENCY, right, y + 43f);

        new TextBlock(regular, "Payment by bank transfer within " + PAYMENT_DAYS
                + " days. Thank you for your business.")
                .SetLocation(left, y + 64f)
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

    // Draws a line of text in the font and the size, its baseline at y and
    // its end at x, as the amounts line up on their right.
    private static void DrawRight(Page page, Font font, float size, String text, float x, float y) {
        TextLine line = new TextLine(font, text);
        line.SetFontSize(size);
        line.SetLocation(x - line.GetWidth(), y);
        line.DrawOn(page);
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,200.00. The culture is given, or a computer set to French writes
    // 1 200,00.
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
        new Example_53();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_53 => {time1 - time0,4} ms");
    }
}   // End of Example_53.cs
