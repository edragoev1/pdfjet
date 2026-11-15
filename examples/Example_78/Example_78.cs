/*
 * Example_78.cs
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
 * Example_78.cs
 * A pro forma invoice of a small studio to a customer abroad, on Letter
 * paper: the seller, the number and the date until which the prices hold,
 * the address to bill and the address to ship to, a table of the goods, the
 * country of origin and the weight for customs, the total, and the terms.
 *
 * A pro forma invoice is a finished document that the customer receives, not
 * a form. It is sent before the sale: the customer pays it, or takes it to
 * the bank or to customs, and the goods are shipped with the real invoice
 * once the money is in. So it says what it is not, a tax invoice, and how
 * long its prices are good for. Goods exported from Canada carry no sales
 * tax, so the total is the sum of the lines.
 *
 * The amounts are decimal, never float or double, and the total is
 * computed from the goods. The table of the goods goes on to as many pages
 * as it needs, with its header row on each; the total and the terms go
 * under the last row, and each page says "Page 1 of 1" at the bottom.
 *
 * Both sides keep the document, so it is archival as well as accessible,
 * PDF/A-3a and PDF/UA-1 at once. The layout is that of the pro forma
 * invoice template of PDFjet Forms. The companies and the numbers are made
 * up.
 */
public class Example_78 {
    // A line of the pro forma invoice. The unit price is a decimal, in dollars.
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

    // The data of the pro forma invoice. In an application it comes from the database.
    private const String NUMBER = "PF-2026-012";
    private static readonly DateTime DATE = new DateTime(2026, 11, 2);
    private const int VALID_DAYS = 30;
    private const String CURRENCY = "CAD";

    private const String SELLER = "Northwind Studio";
    private const String SELLER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    private const String BILL_TO = "Fabrikam Press\n18 Rue des Lilas\n75011 Paris, France";
    private const String SHIP_TO =
            "Fabrikam Press, warehouse\n4 Avenue de l'Europe\n94200 Ivry-sur-Seine, France";
    private const String CUSTOMS = "Country of origin: Canada\nWeight: 18 kg, in 2 parcels";

    private static readonly Item[] ITEMS = {
        new Item("Art prints, A3, numbered edition", 40, "35.00"),
        new Item("Archival portfolio boxes", 4, "45.00"),
        new Item("Shipping, insured", 1, "120.00"),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_78() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_78.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.SetTitle("Pro forma invoice " + NUMBER);
        pdf.SetAuthor(SELLER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The total, from the goods. Exported, they carry no sales tax.
        decimal total = 0m;
        foreach (Item item in ITEMS) {
            total += item.Amount();
        }

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;
        float half = (right - left) / 2f;

        // The header: the seller on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).SetLocation(left, 80f).SetWidth(250f).DrawOn(page);

        TextLine title = new TextLine(semiBold, "Pro forma invoice");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(22f);
        title.SetTextColor(BLUE);
        title.SetLocation(right - title.GetWidth(), 70f);
        title.DrawOn(page);

        String numbers = "No. " + NUMBER
                + "\nDate: " + Iso(DATE)
                + "\nValid until: " + Iso(DATE.AddDays(VALID_DAYS));
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

        // Who pays, on the left, and where the goods go, on the right
        Address(page, semiBold, regular, "Bill to", BILL_TO, left, half);
        Address(page, semiBold, regular, "Ship to", SHIP_TO, left + half, half);

        // The goods: a header row, repeated on every page the table goes on to
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
        // Under the addresses on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.SetLocation(left, MARGIN);
        table.SetFirstPageTopMargin(260f);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // What customs asks for on the left, the total on the right, and the
        // terms under them, all under the last row. When they no longer fit
        // on its page, they go on a page of their own.
        float y = end[1] + 22f;
        if (y + 110f > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        TextLine totalLine = new TextLine(semiBold, "Total: " + Money(total) + " " + CURRENCY);
        totalLine.SetFontSize(14f);
        totalLine.SetLocation(right - totalLine.GetWidth(), y + 14f);
        totalLine.DrawOn(page);
        y = new TextBlock(regular, CUSTOMS).SetLocation(left, y).SetWidth(half).DrawOn(page)[1];

        new TextBlock(regular, "Not a tax invoice: the goods are shipped when this amount is received, "
                + "by bank transfer, and the invoice is sent with them. Prices are valid until "
                + Iso(DATE.AddDays(VALID_DAYS)) + ".")
                .SetLocation(left, y + 16f)
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

    // Draws an address under its heading, in a column at x of the width,
    // under the rule of the header.
    private static void Address(Page page, Font headingFont, Font font, String heading, String text,
            float x, float width) {
        TextLine line = new TextLine(headingFont, heading);
        line.SetStructureType(StructElem.H2);
        line.SetFontSize(12f);
        line.SetLocation(x, 182f);
        line.DrawOn(page);
        new TextBlock(font, text).SetLocation(x, 192f).SetWidth(width - 12f).DrawOn(page);
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
    // 1,700.00. The culture is given, or a computer set to French writes
    // 1 700,00.
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
        new Example_78();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_78 => {time1 - time0,4} ms");
    }
}   // End of Example_78.cs
