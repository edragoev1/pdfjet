/*
 * Example_74.cs
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
 * Example_74.cs
 * The minutes of a team meeting, sent to the team after it: when and where
 * it was, who was there and who was not, what was said on each topic of the
 * agenda, the actions agreed, with who does each and by when, and the date
 * of the next meeting, with "Page 1 of 1" at the bottom.
 *
 * This is a document people receive, not a form. It is the record of the
 * meeting of Example_73, whose agenda it follows topic by topic. Minutes
 * have no fixed length, so each part goes on a new page when it no longer
 * fits on the one it is on: a paragraph is measured first, with DrawOn of a
 * null page, which returns where it would end without drawing it. The table
 * of the actions goes on to as many pages as it needs, its header row on
 * each. The pages are kept in a list and added to the document at the end,
 * when the footer can say how many there are.
 *
 * The document is PDF/UA-1: the title, the sections and the topics are
 * headings, and the page numbers are artifacts that a screen reader skips.
 * The layout is that of the meeting minutes template of PDFjet Forms. The
 * studio and the people are made up.
 */
public class Example_74 {
    // The data of the meeting. In an application it comes from the notes
    // taken during it.
    private const String TEAM = "Northwind Studio";
    private static readonly DateTime DATE = new DateTime(2026, 11, 12);
    private const String TIME_AND_PLACE = "10:00 to 11:00\nMeeting room 2, and by video call";
    private const String PEOPLE = "Present: Dana, Sam, Jordan · Absent: Lee · Notes: Sam";
    private static readonly DateTime NEXT_MEETING = new DateTime(2026, 11, 26);

    // What was said, by topic of the agenda
    private static readonly String[][] NOTES = {
        new String[] {"Contoso Books website", "Jordan wrote up the interviews at the shop, and the site "
                + "map and the wireframes for computers and phones are done. Contoso Books "
                + "reviews them with us tomorrow; the design phase starts after that."},
        new String[] {"New clients", "Two new clients this month, both from the website. A third has "
                + "asked for a quote for a catalog of 32 pages. Lee was away, so Dana calls "
                + "the two new clients in his place."},
        new String[] {"Holidays", "The studio is closed from December 24 to January 2. Work due in that "
                + "week is moved to the week before; Sam tells the clients."},
    };

    // The actions agreed: what, who, and by when
    private static readonly String[][] ACTIONS = {
        new String[] {"Send the notes of the review to the team", "Jordan", "2026-11-16"},
        new String[] {"Send the quote for the catalog", "Dana", "2026-11-14"},
        new String[] {"Call the two new clients", "Dana", "2026-11-19"},
        new String[] {"Tell clients of the holiday closing", "Sam", "2026-12-01"},
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float LEFT = 48f;
    private const float WIDTH = 516f;
    private const float TOP = 62f;
    // The text ends this far from the bottom of the page, above the footer
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    private readonly PDF pdf;
    private readonly List<Page> pages = new List<Page>();
    private Page page;
    private readonly Font regular;
    private readonly Font semiBold;

    public Example_74() {
        pdf = new PDF(new BufferedStream(
                new FileStream("Example_74.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Team meeting, " + Iso(DATE) + ": minutes");
        pdf.SetAuthor(TEAM);
        pdf.SetLanguage("en-CA");

        regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        NewPage();

        // The title, when and where, and a rule under them
        float y = Heading("Team meeting: minutes", StructElem.H1, 18f, TOP);
        String date = DATE.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        y = Paragraph(date + " · " + TIME_AND_PLACE, y) + 12f;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        y = Paragraph(PEOPLE, y + 12f);

        // What was said: a heading for each topic and the notes under it
        y = Heading("What was said", StructElem.H2, 14f, y + 30f);
        foreach (String[] note in NOTES) {
            y = Heading(note[0], StructElem.H3, 11f, y + 18f);
            y = Paragraph(note[1], y);
        }

        // The actions, a table that goes on to the next pages when it must
        y = Heading("Actions", StructElem.H2, 14f, y + 30f);
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(Row(semiBold, "Action", "Who", "By"));
        foreach (String[] action in ACTIONS) {
            rows.Add(Row(regular, action));
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(60f, 20f, 20f);
        table.SetLocation(LEFT, TOP);
        table.SetFirstPageTopMargin(y);
        table.SetBottomMargin(FOOTER_ROOM);
        y = table.DrawOn(pdf, page, pages, Letter.PORTRAIT)[1];
        page = pages[pages.Count - 1];

        String next = NEXT_MEETING.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        Paragraph("Next meeting: " + next + ", at 10:00.", y + 18f);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.Count; i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.Count);
            footer.SetFontSize(10f);
            footer.SetLocation(LEFT + WIDTH - footer.GetWidth(), page.GetHeight() - 52f);
            pages[i].AddArtifactBMC();
            footer.DrawOn(pages[i]);
            pages[i].AddEMC();
        }
        pdf.AddPages(pages);
        pdf.Complete();
    }

    // Starts a page, kept in the list until the end.
    private void NewPage() {
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
    }

    // Draws a heading in blue, its baseline at y, and returns where the text
    // under it starts. A heading with no room for a line under it goes on to
    // a new page, so that it is never left alone at the bottom.
    private float Heading(String text, StructElem level, float size, float y) {
        if (y + 40f > page.GetHeight() - FOOTER_ROOM) {
            NewPage();
            y = TOP;
        }
        TextLine heading = new TextLine(semiBold, text);
        heading.SetStructureType(level);
        heading.SetFontSize(size);
        heading.SetTextColor(BLUE);
        heading.SetLocation(LEFT, y);
        heading.DrawOn(page);
        return y + 8f;
    }

    // Draws the text wrapped to the width of the page, its top at y, on a new
    // page when it does not fit, and returns where it ended.
    private float Paragraph(String text, float y) {
        TextBlock block = new TextBlock(regular, text);
        block.SetLineSpacing(1.15f);
        block.SetWidth(WIDTH);
        block.SetLocation(LEFT, y);
        if (block.DrawOn(null)[1] > page.GetHeight() - FOOTER_ROOM) {
            NewPage();
            block.SetLocation(LEFT, TOP);
        }
        return block.DrawOn(page)[1];
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
        new Example_74();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_74 => {time1 - time0,4} ms");
    }
}   // End of Example_74.cs
