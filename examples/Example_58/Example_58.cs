/*
 * Example_58.cs
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
 * Example_58.cs
 * A purchase order of a small design studio to its supplier, on Letter
 * paper: the studio and the order number at the top with the date to deliver
 * by, the vendor and where to ship, a table of the items with the vendor's
 * codes, the subtotal, the sales tax and the total, the terms, and who
 * approved the order.
 *
 * Here the studio is the buyer: the layout of its invoice of Example_53,
 * the parties the other way around. A purchase order is a finished document
 * that the vendor receives, not a form. The amounts are decimal, computed
 * from the items, and the table goes on to as many pages as it needs.
 *
 * The vendor's invoice is checked against the purchase order, and both are
 * kept for the books, so the document is PDF/A-3a and PDF/UA-1 at once. The
 * layout is that of the purchase order template of PDFjet Forms. The
 * companies, the people and the numbers are made up.
 */
public class Example_58 {
    // A line of the order: the vendor's code of the item, so that the vendor
    // picks the right one, and the price the vendor quoted.
    private sealed class Item {
        internal readonly String description;
        internal readonly String code;
        internal readonly int quantity;
        internal readonly decimal unitPrice;

        internal Item(String description, String code, int quantity, String unitPrice) {
            this.description = description;
            this.code = code;
            this.quantity = quantity;
            this.unitPrice = decimal.Parse(unitPrice, CultureInfo.InvariantCulture);
        }

        internal decimal Amount() {
            return unitPrice * quantity;
        }
    }

    // The data of the order. In an application it comes from the database.
    private const String NUMBER = "2026-017";
    private static readonly DateTime DATE = new DateTime(2026, 10, 5);
    private const int DELIVERY_DAYS = 15;
    private const int PAYMENT_DAYS = 30;
    private const String CURRENCY = "CAD";
    private const String TAX_NAME = "HST";
    private const decimal TAX_RATE = 0.13m;
    private const String APPROVED_BY = "Dana Whitfield, Design Lead";

    private const String BUYER = "Northwind Studio";
    private const String BUYER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0";
    private const String VENDOR = "Harbour Paper Supply\n88 Dock Road\nOwen Sound, ON N4K 3T2";

    private static readonly Item[] ITEMS = {
        new Item("Recycled paper, letter, 500 sheets", "PP-2201", 20, "8.50"),
        new Item("Kraft envelopes, 9 x 12 in", "EN-0912", 200, "0.35"),
        new Item("Shipping boxes, medium", "BX-0400", 50, "1.20"),
        new Item("Packing tape, 48 mm x 50 m", "TP-4850", 12, "3.95"),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_58() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_58.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.SetTitle("Purchase order " + NUMBER);
        pdf.SetAuthor(BUYER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The totals, from the items, the tax rounded once on the subtotal
        decimal subtotal = 0m;
        foreach (Item item in ITEMS) {
            subtotal += item.Amount();
        }
        decimal tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);
        decimal total = subtotal + tax;

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;
        float middle = (left + right) / 2f;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, BUYER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        new TextBlock(regular, BUYER_ADDRESS + "\nhello@example.com")
                .SetLocation(left, 80f).SetWidth(250f).DrawOn(page);

        TextLine title = new TextLine(semiBold, "Purchase order");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(BLUE);
        title.SetLocation(right - title.GetWidth(), 72f);
        title.DrawOn(page);

        new TextBlock(regular, "PO No. " + NUMBER + "\nDate: " + Iso(DATE)
                + "\nDeliver by: " + Iso(DATE.AddDays(DELIVERY_DAYS)))
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

        // The vendor, and where to ship: side by side, the taller of the two
        // decides where the table starts
        Heading(page, semiBold, "Vendor", left, 182f);
        float y1 = new TextBlock(regular, VENDOR).SetLocation(left, 192f).SetWidth(middle - left)
                .DrawOn(page)[1];
        Heading(page, semiBold, "Ship to", middle, 182f);
        float y2 = new TextBlock(regular, BUYER + "\n" + BUYER_ADDRESS).SetLocation(middle, 192f)
                .SetWidth(right - middle).DrawOn(page)[1];
        float y = Math.Max(y1, y2) + 24f;

        // The items: a header row, repeated on every page
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Item", "Code", "Quantity", "Price", "Amount"));
        foreach (Item item in ITEMS) {
            rows.Add(Row(regular, item.description, item.code, item.quantity.ToString(),
                    Money(item.unitPrice), Money(item.Amount())));
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(right - left);
        table.SetColumnWidthsInPercent(45f, 15f, 12f, 13f, 15f);
        for (int column = 2; column < 5; column++) {
            table.SetTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.SetLocation(left, MARGIN);
        table.SetFirstPageTopMargin(y);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The totals, the terms and the approval under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 22f;
        if (y + 130f > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        String rate = (TAX_RATE * 100).ToString("0.####", CultureInfo.InvariantCulture);
        DrawRight(page, regular, 11f, "Subtotal: " + Money(subtotal), right, y);
        DrawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + Money(tax), right, y + 15f);
        DrawRight(page, semiBold, 14f, "Total: " + Money(total) + " " + CURRENCY, right, y + 43f);

        y = new TextBlock(regular, "Please quote the PO number on your invoice and packing slip. "
                + "Payment within " + PAYMENT_DAYS + " days of delivery.")
                .SetLineSpacing(1.15f)
                .SetLocation(left, y + 64f)
                .SetWidth(right - left)
                .DrawOn(page)[1];
        new TextLine(regular, "Approved by: " + APPROVED_BY)
                .SetLocation(left, y + 28f).DrawOn(page);

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
        new Example_58();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_58 => {time1 - time0,4} ms");
    }
}   // End of Example_58.cs
