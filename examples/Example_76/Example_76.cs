/*
 * Example_76.cs
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
 * Example_76.cs
 * An order form of a small shop, to print and fill in by hand: who orders,
 * a table of the items with their prices and empty cells for the size, the
 * quantity and the amount, lines for the subtotal, the sales tax and the
 * total, where to deliver, and a box to sign in.
 *
 * This is a form people fill in, not a document with data. The shop prints
 * what it knows, the items and their prices, from decimal amounts written
 * with CultureInfo.InvariantCulture, and leaves the rest empty. The rows of
 * the table are about as tall as a box, to write a number in by hand; the
 * subtotal, the tax and the total are rows of the same table whose label
 * spans the first four columns, so that their empty amounts line up under
 * those of the items. The other fields are drawn with the field helper of
 * Example_72.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * items a table that a screen reader reads by its column headers, and the
 * boxes are artifacts. The layout is that of the order form template of
 * PDFjet Forms. The shop is made up.
 */
public class Example_76 {
    // An item of the price list: its name and its price, in dollars
    private sealed class Item {
        internal readonly String name;
        internal readonly decimal price;

        internal Item(String name, String price) {
            this.name = name;
            this.price = decimal.Parse(price, CultureInfo.InvariantCulture);
        }
    }

    private const String SHOP = "Tidewater Print Co.";
    private const String RETURN_TO = "Mail the form to Tidewater Print Co., 210 Front Street, "
            + "Beaufort, NC 28516, or scan it to orders@example.com.";
    private const String TAX = "Sales tax 8%";

    private static readonly Item[] ITEMS = {
        new Item("T-shirt, cotton, printed on the front", "20.00"),
        new Item("Hoodie, printed on the back", "45.00"),
        new Item("Cap, embroidered", "15.00"),
        new Item("Tote bag, canvas", "12.00"),
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

    public Example_76() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_76.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Order form");
        pdf.SetAuthor(SHOP);
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

        // The title, what happens to the order, and a rule
        Heading("Order form", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Write how many of each item you want, and "
                + "the amounts. Sign the form and send it to us: we confirm every order by "
                + "email within one working day.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // Your details
        y = Heading("Your details", StructElem.H2, 14f, y + 46f);
        Field("Name *", LEFT, y, HALF, 1, null);
        y = Field("Email *", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("Company", LEFT, y, HALF, 1, null);
        y = Field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The order: the items with their prices, and empty cells to write in
        y = Heading("Your order", StructElem.H2, 14f, y + 18f);
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Item", "Price", "Size, color", "Qty", "Amount"));
        foreach (Item item in ITEMS) {
            rows.Add(Row(textFont, item.name, Money(item.price), "", "", ""));
        }
        rows.Add(TotalRow(textFont, "Subtotal"));
        rows.Add(TotalRow(textFont, TAX));
        rows.Add(TotalRow(semiBold, "Total"));

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        // Rows about as tall as a box, to write in
        table.SetCellPadding(5f, 6f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BOX);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(42f, 13f, 20f, 10f, 15f);
        table.SetTextAlignmentInColumn(1, Alignment.RIGHT);
        table.SetLocation(LEFT, y);
        y = table.DrawOn(page)[1] + 12f;

        // Where to deliver: the address beside the date
        y = Heading("Delivery", StructElem.H2, 14f, y + 18f);
        float wide = 0.6f * WIDTH - GAP / 2f;
        Field("Delivery date", LEFT + wide + GAP, y, WIDTH - wide - GAP, 1, null);
        y = Field("Delivery address *", LEFT, y, wide, 3, null);

        // The confirmation, and the signature under it
        new CheckBox(textFont, "I confirm this order and sign below. *")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        y += 30f;
        Field("Signature *", LEFT, y, wide, 1, null);
        Field("Date", LEFT + wide + GAP, y, WIDTH - wide - GAP, 1, null);

        // Where the form goes, at the foot of the page
        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.SetFontSize(9f);
        returnTo.SetTextColor(Color.dimgray);
        returnTo.SetLocation(LEFT, page.GetHeight() - 36f);
        returnTo.DrawOn(page);

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

    // A row of the totals: the label across the first four columns, on the
    // right, and an empty amount. The cells under the span stay in the row.
    private static List<Cell> TotalRow(Font font, String label) {
        List<Cell> row = Row(font, label, "", "", "", "");
        row[0].SetColSpan(4);
        row[0].SetTextAlignment(Alignment.RIGHT);
        return row;
    }

    // The price with two decimals, as 45.00. The culture is given, or a
    // computer set to French writes 45,00.
    private static String Money(decimal amount) {
        return amount.ToString("N2", CultureInfo.InvariantCulture);
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
        new Example_76();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_76 => {time1 - time0,4} ms");
    }
}   // End of Example_76.cs
