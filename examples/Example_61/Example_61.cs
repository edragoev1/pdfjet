/*
 * Example_61.cs
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
 * Example_61.cs
 * A timesheet of one week, filled in: who worked, the week and the project,
 * a table of the seven days with the hours and the work done on each, the
 * total and the overtime, and the lines where the employee and the manager
 * sign.
 *
 * This is the timesheet as the manager receives it, a finished document: the
 * hours come from a time tracking application, and the total and the
 * overtime are computed from them, not typed in. The hours are decimal:
 * a tenth of an hour, six minutes, is not exact in a double, and a sum of
 * them can come out as 7.499999. The dates of the days are worked out from
 * the first day of the week.
 *
 * The days are a Table: a header row, a row for each day, and a footer row
 * with the total, in the header's style. Table wraps a long description of
 * the work in its column and makes the row as tall as it needs; a day of no
 * work keeps its row, empty, so that the week reads from Monday to Sunday.
 *
 * The document is PDF/UA-1: the title is a heading, the days a table that a
 * screen reader reads by its column headers, and the rules and the lines to
 * sign on are artifacts. The layout is that of the timesheet template of
 * PDFjet Forms. The people and the companies are made up.
 */
public class Example_61 {
    // The data of the timesheet. In an application it comes from the time
    // tracking: the hours and the work of each day, from Monday on.
    private const String COMPANY = "Northwind Studio";
    private const String EMPLOYEE = "Jordan Ellis";
    private const String PROJECT = "Contoso Books, the new website";
    private static readonly DateTime WEEK_START = new DateTime(2026, 11, 9);     // A Monday
    private const decimal REGULAR_HOURS = 40m;

    private static readonly String[][] DAYS = {
        new String[] {"8", "The notes of the interviews at the shop written up for the team"},
        new String[] {"8.5", "The site map, first draft, from what the shop's team said: how the orders "
                + "come in today, which titles sell online, and what the catalog needs"},
        new String[] {"7.5", "The site map revised; wireframes of the home and title pages"},
        new String[] {"9", "Wireframes for phones; the order pages"},
        new String[] {"8", "Review with the client by video call, notes for the design phase"},
        new String[] {"2.5", "The list of photos of the new titles, for the shoot"},
        new String[] {"", ""},
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;
    private const int INK = 0x333333;

    private const float LEFT = 48f;
    private const float WIDTH = 516f;            // Letter, 612 wide, less the margins

    public Example_61() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_61.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Timesheet, " + EMPLOYEE + ", week of " + Iso(WEEK_START));
        pdf.SetAuthor(EMPLOYEE);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        // The total and the overtime, from the hours of the days
        decimal total = 0m;
        foreach (String[] day in DAYS) {
            if (day[0].Length != 0) {
                total += decimal.Parse(day[0], CultureInfo.InvariantCulture);
            }
        }
        decimal overtime = Math.Max(total - REGULAR_HOURS, 0m);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The title, the company, and a rule
        TextLine title = new TextLine(semiBold, "Timesheet");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(20f);
        title.SetTextColor(BLUE);
        title.SetLocation(LEFT, 64f);
        title.DrawOn(page);
        TextLine company = new TextLine(semiBold, COMPANY);
        company.SetLocation(LEFT + WIDTH - company.GetWidth(), 64f);
        company.DrawOn(page);
        Rule(page, 80f);

        // Who, which week, and for what: a label and its value on each line
        String longDate = "MMMM d, yyyy";
        float y = Detail(page, semiBold, regular, "Employee", EMPLOYEE, 106f);
        y = Detail(page, semiBold, regular, "Week", WEEK_START.ToString(longDate, CultureInfo.InvariantCulture)
                + " to " + WEEK_START.AddDays(6).ToString(longDate, CultureInfo.InvariantCulture), y);
        y = Detail(page, semiBold, regular, "Project", PROJECT, y);

        // The days: the name, the date and the hours of each, the work done,
        // and the total under them
        String dayName = "dddd";
        String shortDate = "MMM d";
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Day", "Date", "Hours", "Work done"));
        for (int i = 0; i < DAYS.Length; i++) {
            DateTime date = WEEK_START.AddDays(i);
            rows.Add(Row(regular, date.ToString(dayName, CultureInfo.InvariantCulture),
                    date.ToString(shortDate, CultureInfo.InvariantCulture),
                    Hours(DAYS[i][0]), DAYS[i][1]));
        }
        rows.Add(Row(semiBold, "Total", "", Hours(total), ""));

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetNumberOfFooterRows(1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetFooterRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(16f, 12f, 10f, 62f);
        table.SetTextAlignmentInColumn(2, Alignment.RIGHT);
        table.SetLocation(LEFT, y + 18f);
        y = table.DrawOn(page)[1];

        // The regular hours, and the overtime, the hours over them
        y = Detail(page, semiBold, regular, "Regular", Hours(Math.Min(total, REGULAR_HOURS)) + " hours", y + 28f);
        y = Detail(page, semiBold, regular, "Overtime", Hours(overtime) + " hours", y);

        // What the signatures confirm, and the lines to sign on, the
        // employee's and the manager's
        new TextLine(regular, "I confirm these hours and sign below.")
                .SetLocation(LEFT, y + 28f)
                .DrawOn(page);
        y += 90f;
        float signWidth = 0.6f * WIDTH - 24f;
        float dateX = LEFT + 0.6f * WIDTH;
        Signature(page, regular, "Employee, " + EMPLOYEE, LEFT, signWidth, y);
        Signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);
        y += 60f;
        Signature(page, regular, "Approved by the manager", LEFT, signWidth, y);
        Signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);

        pdf.Complete();
    }

    // Draws a label and its value beside it, the baseline at y, and returns
    // the baseline of the next line.
    private static float Detail(Page page, Font labelFont, Font font, String label, String value, float y) {
        new TextLine(labelFont, label).SetLocation(LEFT, y).DrawOn(page);
        new TextLine(font, value).SetTextColor(INK).SetLocation(LEFT + 80f, y).DrawOn(page);
        return y + 17f;
    }

    // A line to sign on at y, of the width, with what it is for under it. The line is an
    // artifact; its caption is text.
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

    // The hours with two decimals, as 7.50, so that they line up in their
    // column, or nothing for a day of no work.
    private static String Hours(String hours) {
        return hours.Length == 0 ? "" : Hours(decimal.Parse(hours, CultureInfo.InvariantCulture));
    }

    private static String Hours(decimal hours) {
        return hours.ToString("0.00", CultureInfo.InvariantCulture);
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
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    // The date as 2026-09-25, in the form of ISO 8601, whatever the culture of
    // the computer.
    private static String Iso(DateTime date) {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_61();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_61 => {time1 - time0,4} ms");
    }
}   // End of Example_61.cs
