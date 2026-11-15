/*
 * Example_67.cs
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
 * Example_67.cs
 * A credit note of a small design studio to a customer, on Letter paper: the
 * studio and the credit note number at the top, the customer, the invoice
 * that the credit note corrects and why, a table of what is credited, the
 * subtotal, the sales tax and the total credit, and what happens to it.
 *
 * A credit note is a finished document that the customer receives, not a
 * form. It takes back part of an invoice, here part of the invoice of
 * Example_53, and refers to it by its number and its date, as the tax rules
 * ask, rather than changing an invoice that was already sent. The amounts are
 * positive: the title and the words "Total credit" say which way they go. The
 * sales tax is credited too, at the rate of the invoice, and computed from the
 * items the same way.
 *
 * Both sides keep a credit note with the invoice, so the document is PDF/A-3a
 * and PDF/UA-1 at once. The layout is that of the credit note template of
 * PDFjet Forms. The companies and the numbers are made up.
 */
public class Example_67 {
    // A line of the credit note. The unit price is a decimal, in dollars.
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

    // The data of the credit note. In an application it comes from the database.
    private const String NUMBER = "CN-2026-007";
    private static readonly DateTime DATE = new DateTime(2026, 10, 5);
    private const String INVOICE = "2026-042";
    private static readonly DateTime INVOICE_DATE = new DateTime(2026, 9, 25);
    private const String REASON = "Ten printed copies were damaged in delivery and returned.";
    private const String CURRENCY = "CAD";
    private const String TAX_NAME = "HST";
    private const decimal TAX_RATE = 0.13m;

    private const String SELLER = "Northwind Studio";
    private const String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    private const String CUSTOMER =
            "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4";

    private static readonly Item[] ITEMS = {
        new Item("Printed copies returned, damaged in delivery", 10, "6.00"),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_67() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_67.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.SetTitle("Credit note " + NUMBER);
        pdf.SetAuthor(SELLER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The credit, from the items, the tax rounded once on the subtotal
        decimal subtotal = 0m;
        foreach (Item item in ITEMS) {
            subtotal += item.Amount();
        }
        decimal tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);
        decimal credit = subtotal + tax;

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;
        float middle = (left + right) / 2f;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).SetLocation(left, 80f).SetWidth(250f).DrawOn(page);

        TextLine title = new TextLine(semiBold, "Credit note");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(BLUE);
        title.SetLocation(right - title.GetWidth(), 72f);
        title.DrawOn(page);

        new TextBlock(regular, "No. " + NUMBER + "\nDate: " + Iso(DATE) + "\nFor invoice " + INVOICE)
                .SetTextAlignment(Alignment.RIGHT)
                .SetLocation(right - 200f, 94f)
                .SetWidth(200f)
                .DrawOn(page);

        // A thin rule under the header, decoration only, so an artifact
        Line rule = new Line(left, 148f, right, 148f);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The customer, and the invoice corrected and why: side by side, the
        // taller of the two decides where the table starts
        Heading(page, semiBold, "Credit to", left, 182f);
        float y1 = new TextBlock(regular, CUSTOMER).SetLocation(left, 192f).SetWidth(middle - left)
                .DrawOn(page)[1];
        Heading(page, semiBold, "Invoice " + INVOICE + " of " + Iso(INVOICE_DATE), middle, 182f);
        float y2 = new TextBlock(regular, REASON)
                .SetLocation(middle, 192f).SetWidth(right - middle).DrawOn(page)[1];
        float y = Math.Max(y1, y2) + 24f;

        // What is credited: a header row, repeated on every page
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
        table.SetLocation(left, MARGIN);
        table.SetFirstPageTopMargin(y);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The amounts and what happens to them under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 22f;
        if (y + 90f > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        String rate = (TAX_RATE * 100).ToString("0.####", CultureInfo.InvariantCulture);
        DrawRight(page, regular, 11f, "Subtotal: " + Money(subtotal), right, y);
        DrawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + Money(tax), right, y + 15f);
        DrawRight(page, semiBold, 14f, "Total credit: " + Money(credit) + " " + CURRENCY,
                right, y + 43f);

        new TextBlock(regular, "This amount is taken off your next invoice, or refunded to you "
                + "on request. We are sorry for the damaged copies.")
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

    // Draws the heading of a part, its baseline at y.
    private static void Heading(Page page, Font font, String text, float x, float y) {
        new TextLine(font, text).SetStructureType(StructElem.H2).SetFontSize(12f)
                .SetLocation(x, y).DrawOn(page);
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
    // 1,200.00, whatever the culture of the computer.
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
        new Example_67();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_67 => {time1 - time0,4} ms");
    }
}   // End of Example_67.cs
