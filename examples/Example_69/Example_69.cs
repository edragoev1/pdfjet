/*
 * Example_69.cs
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
 * Example_69.cs
 * An expense report of a business trip, filled in: who paid, what for and
 * when, a table of the expenses, the total of each category and the total to
 * pay back, how to pay it, and the lines where the employee and the manager
 * sign.
 *
 * This is the report as the manager receives it, a finished document: the
 * expenses come from an application where each receipt was entered, and the
 * totals are computed from them, not typed in. The amounts are decimal,
 * never float or double, so that they add up to the cent. The totals of the
 * categories are kept in a Dictionary, and their names in a List, in the
 * order the categories first appear, so the summary reads in the order of
 * the trip.
 *
 * The table of the expenses goes on to as many pages as it needs, with its
 * header row on each, as a month of receipts can take more than one page;
 * the totals go under its last row, or on a page of their own when they no
 * longer fit, and each page says "Page 1 of 2" at the bottom.
 *
 * The document is PDF/UA-1: the title is a heading, the expenses a table that
 * a screen reader reads by its column headers, and the rules and the lines to
 * sign on are artifacts. The layout is that of the expense report template of
 * PDFjet Forms. The people and the companies are made up.
 */
public class Example_69 {
    // An expense, as on its receipt. The amount is a decimal, in dollars.
    private sealed class Expense {
        internal readonly DateTime date;
        internal readonly String category;
        internal readonly String description;
        internal readonly decimal amount;

        internal Expense(String date, String category, String description, String amount) {
            this.date = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            this.category = category;
            this.description = description;
            this.amount = decimal.Parse(amount, CultureInfo.InvariantCulture);
        }
    }

    // The data of the report. In an application it comes from the database.
    private const String COMPANY = "Northwind Studio";
    private const String EMPLOYEE = "Jordan Ellis";
    private const String PURPOSE = "Discovery interviews at Contoso Books, Toronto";
    private static readonly DateTime FROM = new DateTime(2026, 11, 2);
    private static readonly DateTime TO = new DateTime(2026, 11, 4);
    private const String CURRENCY = "CAD";
    private const String PAY_BACK_BY = "Bank transfer";

    private static readonly Expense[] EXPENSES = {
        new Expense("2026-11-02", "Travel", "Bus, Port Elgin to Kitchener, return", "64.00"),
        new Expense("2026-11-02", "Travel", "Train, Kitchener to Toronto Union, return", "86.40"),
        new Expense("2026-11-02", "Lodging", "Hotel, two nights, taxes included", "412.30"),
        new Expense("2026-11-02", "Meals", "Dinner", "38.75"),
        new Expense("2026-11-03", "Meals", "Lunch with the shop's team, four people", "126.20"),
        new Expense("2026-11-03", "Supplies", "Sticky notes and markers for the workshop", "23.45"),
        new Expense("2026-11-03", "Meals", "Dinner", "41.10"),
        new Expense("2026-11-04", "Travel", "Subway fares, three days", "19.80"),
        new Expense("2026-11-04", "Meals", "Lunch at the station", "16.85"),
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;
    private const int INK = 0x333333;

    private const float LEFT = 48f;
    private const float WIDTH = 516f;            // Letter, 612 wide, less the margins
    private const float RIGHT = LEFT + WIDTH;
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_69() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_69.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Expense report, " + EMPLOYEE + ", " + Iso(FROM));
        pdf.SetAuthor(EMPLOYEE);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The total of each category, and of all, from the expenses
        Dictionary<String, decimal> categories = new Dictionary<String, decimal>();
        List<String> names = new List<String>();
        decimal total = 0m;
        foreach (Expense expense in EXPENSES) {
            if (categories.TryGetValue(expense.category, out decimal sum)) {
                categories[expense.category] = sum + expense.amount;
            } else {
                categories[expense.category] = expense.amount;
                names.Add(expense.category);
            }
            total += expense.amount;
        }

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);

        // The title, the company, and a rule
        TextLine title = new TextLine(semiBold, "Expense report");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(20f);
        title.SetTextColor(BLUE);
        title.SetLocation(LEFT, 64f);
        title.DrawOn(page);
        TextLine company = new TextLine(semiBold, COMPANY);
        company.SetLocation(RIGHT - company.GetWidth(), 64f);
        company.DrawOn(page);
        Rule(page, 80f);

        // Who, what for, and when: a label and its value on each line
        String longDate = "MMMM d, yyyy";
        float y = Detail(page, semiBold, regular, "Employee", EMPLOYEE, 106f);
        y = Detail(page, semiBold, regular, "Purpose", PURPOSE, y);
        Detail(page, semiBold, regular, "Dates",
                FROM.ToString(longDate, CultureInfo.InvariantCulture) + " to " + TO.ToString(longDate, CultureInfo.InvariantCulture), y);

        // The expenses: a header row, repeated on every page the table goes on to
        String shortDate = "MMM d";
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Date", "Category", "What it was", "Amount"));
        foreach (Expense expense in EXPENSES) {
            rows.Add(Row(regular, expense.date.ToString(shortDate, CultureInfo.InvariantCulture), expense.category,
                    expense.description, Money(expense.amount)));
        }

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(12f, 16f, 56f, 16f);
        table.SetTextAlignmentInColumn(3, Alignment.RIGHT);
        // Under the details on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.SetLocation(LEFT, 48f);
        table.SetFirstPageTopMargin(172f);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The totals and the signatures, under the last row. When they no
        // longer fit on its page, they go on a page of their own.
        y = end[1] + 24f;
        float height = 17f * categories.Count + 190f;
        if (y + height > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 64f;
        }
        foreach (String name in names) {
            DrawRight(page, regular, 11f, name + ": " + Money(categories[name]), RIGHT, y);
            y += 17f;
        }
        DrawRight(page, semiBold, 14f, "To pay back: " + Money(total) + " " + CURRENCY, RIGHT, y + 12f);
        Detail(page, semiBold, regular, "Pay back by", PAY_BACK_BY, y + 12f);

        // What the signatures confirm, and the lines to sign on, the
        // employee's and the manager's
        y += 46f;
        new TextLine(regular, "I confirm these expenses were for the business and sign below.")
                .SetLocation(LEFT, y)
                .DrawOn(page);
        y += 52f;
        float signWidth = 0.6f * WIDTH - 24f;
        float dateX = LEFT + 0.6f * WIDTH;
        Signature(page, regular, "Employee, " + EMPLOYEE, LEFT, signWidth, y);
        Signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);
        y += 52f;
        Signature(page, regular, "Approved by the manager", LEFT, signWidth, y);
        Signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.Count; i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.Count);
            footer.SetFontSize(10f);
            footer.SetLocation(RIGHT - footer.GetWidth(), page.GetHeight() - 52f);
            pages[i].AddArtifactBMC();
            footer.DrawOn(pages[i]);
            pages[i].AddEMC();
        }
        pdf.AddPages(pages);
        pdf.Complete();
    }

    // Draws a label and its value beside it, the baseline at y, and returns
    // the baseline of the next line.
    private static float Detail(Page page, Font labelFont, Font font, String label, String value, float y) {
        new TextLine(labelFont, label).SetLocation(LEFT, y).DrawOn(page);
        new TextLine(font, value).SetTextColor(INK).SetLocation(LEFT + 90f, y).DrawOn(page);
        return y + 17f;
    }

    // Draws a line of text in the font and the size, its baseline at y and
    // its end at x, as the amounts line up on their right.
    private static void DrawRight(Page page, Font font, float size, String text, float x, float y) {
        TextLine line = new TextLine(font, text);
        line.SetFontSize(size);
        line.SetLocation(x - line.GetWidth(), y);
        line.DrawOn(page);
    }

    // A line to sign on at y, of the width, with what it is for under it.
    // The line is an artifact; its caption is text.
    private static void Signature(Page page, Font font, String caption, float x, float width, float y) {
        Line line = new Line(x, y, x + width, y);
        line.SetStrokeWidth(0.75f);
        line.SetStrokeColor(INK);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
        new TextLine(font, caption)
                .SetFontSize(9f)
                .SetTextColor(Color.dimgray)
                .SetLocation(x, y + 13f)
                .DrawOn(page);
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> Row(Font font, params String[] texts) {
        List<Cell> row = new List<Cell>();
        foreach (String text in texts) {
            row.Add(new Cell(font, text));
        }
        return row;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private static void Rule(Page page, float y) {
        Line line = new Line(LEFT, y, RIGHT, y);
        line.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,034.85. The culture is given, or a computer set to French writes
    // 1 034,85.
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
        new Example_69();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_69 => {time1 - time0,4} ms");
    }
}   // End of Example_69.cs
