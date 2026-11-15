/*
 * Example_80.cs
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
 * Example_80.cs
 * A proposal of a design studio to a customer, on Letter paper: the studio
 * and the date, the project, who it is for and who wrote it, the goal, a
 * table of the phases of the work with their weeks, a table of the price, the
 * total, and how to go ahead.
 *
 * A proposal is a finished document that the customer receives, not a form.
 * It is read from top to bottom, so each part is placed under the one before
 * it, from where DrawOn says the last one ended: a longer goal or one more
 * phase moves everything below it down. Each section is a heading and what
 * follows it, drawn by two small helpers. The weeks and the price are
 * computed from the tables' rows, the price in decimal, so the totals
 * cannot disagree with the lines above them.
 *
 * The document is PDF/UA-1: the project is the heading of the document and
 * each section a heading under it, so that a screen reader can go from one
 * to the next; the plan and the price are tables that it reads by their
 * column headers. The layout is that of the proposal template of PDFjet
 * Forms. The people and the companies are made up.
 */
public class Example_80 {
    private const String STUDIO = "Northwind Studio";
    private const String STUDIO_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";
    private static readonly DateTime DATE = new DateTime(2026, 10, 19);
    private const int VALID_DAYS = 30;

    private const String PROJECT = "A new website for Contoso Books";
    private const String PREPARED_FOR = "Priya Raman\nContoso Books";
    private const String PREPARED_BY = "Dana Whitfield\nDesign Lead, " + STUDIO;
    private const String GOAL = "A website that shows the new titles each season, lets readers order "
            + "from any phone, and that the shop's team can keep up to date without help.";

    // The phases: the name, what the customer gets, and the weeks it takes
    private static readonly String[][] PHASES = {
        new String[] {"Discovery", "Interviews, a site map, the look in two directions", "2"},
        new String[] {"Design", "The pages designed for computers and phones", "3"},
        new String[] {"Build", "The site, the catalog and ordering, tested", "4"},
        new String[] {"Launch", "Training for your team, a month of support", "1"},
    };

    // The price, in two parts, in Canadian dollars before the sales tax
    private static readonly String[][] PRICES = {
        new String[] {"Discovery and design", "4800.00"},
        new String[] {"Build and launch", "7200.00"},
    };
    private const String CURRENCY = "CAD";

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float LEFT = 48f;
    private const float WIDTH = 516f;            // Letter, 612 wide, less the margins
    private const float RIGHT = LEFT + WIDTH;

    private Page page;
    private Font regular;
    private Font semiBold;

    public Example_80() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_80.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Proposal: " + PROJECT);
        pdf.SetAuthor(STUDIO);
        pdf.SetLanguage("en-CA");

        regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The weeks and the price, from the rows of the tables. The customer
        // pays half when the work starts, the rest at the launch.
        int weeks = 0;
        foreach (String[] phase in PHASES) {
            weeks += int.Parse(phase[2], CultureInfo.InvariantCulture);
        }
        decimal total = 0m;
        foreach (String[] price in PRICES) {
            total += decimal.Parse(price[1], CultureInfo.InvariantCulture);
        }
        decimal half = total / 2m;

        page = new Page(pdf, Letter.PORTRAIT);

        // The header: the studio on the left, what the document is and its
        // dates on the right
        new TextLine(semiBold, STUDIO).SetFontSize(16f).SetLocation(LEFT, 64f).DrawOn(page);
        new TextBlock(regular, STUDIO_ADDRESS).SetLocation(LEFT, 80f).SetWidth(250f).DrawOn(page);
        TextLine kind = new TextLine(semiBold, "Proposal");
        kind.SetFontSize(26f);
        kind.SetTextColor(BLUE);
        kind.SetLocation(RIGHT - kind.GetWidth(), 72f);
        kind.DrawOn(page);
        new TextBlock(regular, "Date: " + Iso(DATE) + "\nValid until: " + Iso(DATE.AddDays(VALID_DAYS)))
                .SetTextAlignment(Alignment.RIGHT)
                .SetLocation(RIGHT - 200f, 94f)
                .SetWidth(200f)
                .DrawOn(page);

        // A thin rule under the header, decoration only, so an artifact
        Line rule = new Line(LEFT, 148f, RIGHT, 148f);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The project, the heading of the document, and the two sides of it
        float y = Heading(PROJECT, StructElem.H1, 18f, Color.black, 186f);
        new TextLine(semiBold, "Prepared for").SetLocation(LEFT, y + 18f).DrawOn(page);
        new TextLine(semiBold, "Prepared by").SetLocation(LEFT + WIDTH / 2f, y + 18f).DrawOn(page);
        new TextBlock(regular, PREPARED_FOR).SetLocation(LEFT, y + 26f).SetWidth(WIDTH / 2f).DrawOn(page);
        y = new TextBlock(regular, PREPARED_BY)
                .SetLocation(LEFT + WIDTH / 2f, y + 26f)
                .SetWidth(WIDTH / 2f)
                .DrawOn(page)[1];

        // The goal
        y = Heading("The goal", StructElem.H2, 14f, BLUE, y + 30f);
        y = Paragraph(GOAL, y);

        // The plan: the phases, and the weeks in all under them
        y = Heading("The plan", StructElem.H2, 14f, BLUE, y + 26f);
        List<List<Cell>> plan = new List<List<Cell>>();
        plan.Add(Row(semiBold, "Phase", "What you get", "Weeks"));
        foreach (String[] phase in PHASES) {
            plan.Add(Row(regular, phase));
        }
        plan.Add(Row(semiBold, "In all", "", weeks.ToString()));
        y = Table(plan, y, 30f, 55f, 15f);

        // The price: the parts, and the total under them
        y = Heading("The price", StructElem.H2, 14f, BLUE, y + 26f);
        List<List<Cell>> prices = new List<List<Cell>>();
        prices.Add(Row(semiBold, "Item", "Price"));
        foreach (String[] price in PRICES) {
            prices.Add(Row(regular, price[0], Money(decimal.Parse(price[1], CultureInfo.InvariantCulture))));
        }
        prices.Add(Row(semiBold, "Total, " + CURRENCY + ", plus HST", Money(total)));
        y = Table(prices, y, 70f, 30f);

        // How to go ahead
        Paragraph("Half, " + Money(half) + " " + CURRENCY + ", is invoiced when the work starts, "
                + "and half at the launch. To go ahead, reply to this proposal, and we will set a "
                + "date for the first interview.", y + 18f);

        pdf.Complete();
    }

    // Draws a heading in the size and the color, its top at y, and returns
    // where the text under it starts.
    private float Heading(String text, StructElem level, float size, int color, float y) {
        TextLine heading = new TextLine(semiBold, text);
        heading.SetStructureType(level);
        heading.SetFontSize(size);
        heading.SetTextColor(color);
        heading.SetLocation(LEFT, y + heading.GetAscent());
        heading.DrawOn(page);
        return y + heading.GetAscent() + 8f;
    }

    // Draws a paragraph across the page, its top at y, and returns where it ended.
    private float Paragraph(String text, float y) {
        TextBlock block = new TextBlock(regular, text);
        block.SetLineSpacing(1.15f);
        block.SetLocation(LEFT, y);
        block.SetWidth(WIDTH);
        return block.DrawOn(page)[1];
    }

    // Draws a table across the page, its top at y: a header row, the rows,
    // and a footer row of totals in the header's style, the last column
    // aligned on the right. Returns where it ended.
    private float Table(List<List<Cell>> rows, float y, params float[] percents) {
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetNumberOfFooterRows(1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetFooterRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(percents);
        table.SetTextAlignmentInColumn(percents.Length - 1, Alignment.RIGHT);
        table.SetLocation(LEFT, y);
        return table.DrawOn(page)[1];
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
    // 12,000.00. The culture is given, or a computer set to French writes
    // 12 000,00.
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
        new Example_80();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_80 => {time1 - time0,4} ms");
    }
}   // End of Example_80.cs
