/*
 * Example_83.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PDFjet.NET;

/**
 * Example_83.cs
 * A volunteer sign-up sheet, to print and put on a clipboard at the front
 * desk: the event at the top, and a table of numbered empty rows in which
 * each volunteer writes their name and how to reach them, ticks the times
 * they are free, and says what they would like to help with.
 *
 * This is a form people fill in, not a document with data. The template of
 * PDFjet Forms is a form for one volunteer, filled in on screen; on paper
 * the same questions go on one sheet for many people, a row for each, which
 * is how a sign-up is done at a table or a door. So the form is a Table
 * whose rows are empty: they are padded to about the height of a box, room
 * enough for handwriting, and the columns for the times are narrow, for a
 * tick. The page is Letter landscape, for the width of the columns.
 *
 * The table is drawn with DrawOn(pdf, page, pages, Letter.LANDSCAPE), so a
 * sheet of more rows than a page holds goes on to the next pages, with its
 * header row on each, and every page says "Page i of N". The pages are kept
 * in a list and added to the document at the end, when their number is
 * known.
 *
 * The document is PDF/UA-1: the title is a heading, and the sheet a table
 * that a screen reader reads by its column headers. The layout follows the
 * volunteer sign-up template of PDFjet Forms. The food bank is made up.
 */
public class Example_83 {
    private const String ORGANIZATION = "Harbourview Food Bank";
    private const String EVENT = "Holiday food drive, December 5 to 19, 2026";
    private const String INTRO = "Thank you for offering your time. Write your name and how "
            + "to reach you, and tick the times you are free: the mornings, afternoons and "
            + "evenings are those of weekdays. Under \"Help with\", write what you would like "
            + "to do: sorting, the front desk, driving, or anything. Our coordinator calls you "
            + "within a week.";

    // The rows of the sheet, as many as fit on the page. More go on to the
    // next page.
    private const int ROWS = 13;

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float LEFT = 48f;
    private const float TOP = 48f;
    private const float WIDTH = 696f;            // 792 less two margins of 48
    private const float FOOTER_ROOM = 56f;   // the bottom margin, clear of "Page i of N"

    public Example_83() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_83.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Volunteer sign-up");
        pdf.SetAuthor(ORGANIZATION);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(10f);

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.LANDSCAPE, Page.DETACHED);
        pages.Add(page);

        // The title and the event on one line, what to write, and a rule
        TextLine title = new TextLine(semiBold, "Volunteer sign-up");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(20f);
        title.SetTextColor(BLUE);
        title.SetLocation(LEFT, 62f);
        title.DrawOn(page);
        TextLine eventLine = new TextLine(regular, ORGANIZATION + " · " + EVENT);
        eventLine.SetLocation(LEFT + WIDTH - eventLine.GetWidth(), 62f);
        eventLine.DrawOn(page);

        TextBlock intro = new TextBlock(regular, INTRO);
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The sheet: a header row, and numbered empty rows to write in
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "No.", "Name", "Phone or email",
                "Mornings", "Afternoons", "Evenings", "Weekends", "Help with"));
        for (int i = 1; i <= ROWS; i++) {
            rows.Add(Row(regular, i.ToString(), "", "", "", "", "", "", ""));
        }

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        // The rows about as tall as a box of a form, the header row less so
        table.SetCellPadding(5f, 7f);
        foreach (Cell cell in table.GetRow(0)) {
            cell.SetTopPadding(4f);
            cell.SetBottomPadding(4f);
        }
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(5f, 22f, 22f, 9f, 9f, 9f, 9f, 15f);
        table.SetTextAlignmentInColumn(0, Alignment.RIGHT);
        table.SetTextColorInColumn(0, Color.dimgray);
        table.SetLocation(LEFT, TOP);
        table.SetFirstPageTopMargin(y + 16f);
        table.SetBottomMargin(FOOTER_ROOM);
        table.DrawOn(pdf, page, pages, Letter.LANDSCAPE);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.Count; i++) {
            Page p = pages[i];
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.Count);
            footer.SetFontSize(10f);
            footer.SetLocation(LEFT + WIDTH - footer.GetWidth(), p.GetHeight() - 30f);
            p.AddArtifactBMC();
            footer.DrawOn(p);
            p.AddEMC();
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

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_83();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_83 => {time1 - time0,4} ms");
    }
}   // End of Example_83.cs
