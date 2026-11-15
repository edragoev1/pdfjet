/*
 * Example_57.cs
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
 * Example_57.cs
 * A delivery note that goes in the box with the goods, on Letter paper: the
 * studio and the delivery note number at the top, where the goods go and how
 * they travel, a table of what was ordered and what is in this delivery, a
 * note on checking the goods, and a line for the receiver to sign.
 *
 * A delivery note is a finished document that the customer receives, not a
 * form; only its last line is filled in, by hand, at the door. It has no
 * prices, as it is read by whoever opens the box. The table says for each
 * item how many were ordered and how many are in the box, and an item that
 * is short says how many are to follow, computed from the two numbers; the
 * weight of the shipment is computed from the items too. The table
 * goes on to as many pages as it needs, with its header row on each.
 *
 * The goods are the printed copies of the invoice of Example_53, and a
 * display stand that follows them, invoiced when it ships. The document is
 * PDF/UA-1: the title is a heading, the items a table, and the rule and the
 * line to sign on are artifacts. The layout is that of the delivery note
 * template of PDFjet Forms. The companies and the numbers are made up.
 */
public class Example_57 {
    // A line of the delivery: how many were ordered, how many are in this
    // delivery, and the weight of one, in kilograms.
    private sealed class Item {
        internal readonly String description;
        internal readonly int ordered;
        internal readonly int delivered;
        internal readonly double kgEach;

        internal Item(String description, int ordered, int delivered, double kgEach) {
            this.description = description;
            this.ordered = ordered;
            this.delivered = delivered;
            this.kgEach = kgEach;
        }
    }

    // The data of the delivery. In an application it comes from the database.
    private const String NUMBER = "D-2026-064";
    private static readonly DateTime DATE = new DateTime(2026, 9, 24);
    private const String ORDER = "CB-PO-3310";
    private const int BOXES = 2;
    private const String CARRIER = "Harbourline Courier, tracking HC 4471 2290";
    private const int CHECK_DAYS = 5;

    private const String SELLER = "Northwind Studio";
    private const String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";
    private const String DELIVER_TO =
            "Contoso Books\nAttn: Receiving\n400 King Street West\nToronto, ON M5V 1K4";

    private static readonly Item[] ITEMS = {
        new Item("Annual report, printed copy, saddle stitched", 50, 50, 0.26),
        new Item("Display stand for the lobby", 1, 0, 3.8),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;
    private const int GRAY = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_57() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_57.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Delivery note " + NUMBER);
        pdf.SetAuthor(SELLER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The weight of the shipment, from the items in it, for the carrier
        double kg = 0.0;
        foreach (Item item in ITEMS) {
            kg += item.delivered * item.kgEach;
        }

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;
        float middle = (left + right) / 2f;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).SetLocation(left, 80f).SetWidth(250f).DrawOn(page);

        TextLine title = new TextLine(semiBold, "Delivery note");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(BLUE);
        title.SetLocation(right - title.GetWidth(), 72f);
        title.DrawOn(page);

        new TextBlock(regular, "No. " + NUMBER + "\nDate: " + Iso(DATE) + "\nYour order: " + ORDER)
                .SetTextAlignment(Alignment.RIGHT)
                .SetLocation(right - 200f, 94f)
                .SetWidth(200f)
                .DrawOn(page);

        Line(page, left, right, 148f, RULE, 1f);

        // Where the goods go, and how they travel: side by side, the taller
        // of the two decides where the table starts
        Heading(page, semiBold, "Deliver to", left, 182f);
        float y1 = new TextBlock(regular, DELIVER_TO).SetLocation(left, 192f)
                .SetWidth(middle - left).DrawOn(page)[1];
        Heading(page, semiBold, "Shipment", middle, 182f);
        String shipment = String.Format(CultureInfo.InvariantCulture, "{0} boxes, {1:F1} kg", BOXES, kg)
                + "\n" + CARRIER;
        float y2 = new TextBlock(regular, shipment).SetLocation(middle, 192f)
                .SetWidth(right - middle).DrawOn(page)[1];
        float y = Math.Max(y1, y2) + 24f;

        // The items: what was ordered, and what is in the box
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Item", "Ordered", "Delivered"));
        foreach (Item item in ITEMS) {
            String delivered = item.delivered.ToString();
            if (item.delivered < item.ordered) {
                delivered += " (" + (item.ordered - item.delivered) + " to follow)";
            }
            rows.Add(Row(regular, item.description, item.ordered.ToString(), delivered));
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(right - left);
        table.SetColumnWidthsInPercent(60f, 15f, 25f);
        table.SetTextAlignmentInColumn(1, Alignment.RIGHT);
        table.SetTextAlignmentInColumn(2, Alignment.RIGHT);
        table.SetLocation(left, MARGIN);
        table.SetFirstPageTopMargin(y);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The note and the line to sign stay together under the last row, or
        // go on a page of their own when they no longer fit there
        y = end[1] + 22f;
        if (y + 110f > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        y = new TextBlock(regular, "Please check the goods on arrival and tell us within "
                + CHECK_DAYS + " days if anything is missing or damaged.")
                .SetLineSpacing(1.15f)
                .SetLocation(left, y)
                .SetWidth(right - left)
                .DrawOn(page)[1];

        // Room to sign by hand above the line
        new TextLine(regular, "Received in good order by (name, signature and date):")
                .SetLocation(left, y + 30f).DrawOn(page);
        Line(page, left, left + 0.6f * (right - left), y + 74f, GRAY, 0.75f);

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

    // A line across, the rule under the header or the line to sign on, which
    // the text above it names: an artifact that a screen reader skips.
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

    // The date as 2026-09-25, in the form of ISO 8601, whatever the culture of
    // the computer.
    private static String Iso(DateTime date) {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_57();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_57 => {time1 - time0,4} ms");
    }
}   // End of Example_57.cs
