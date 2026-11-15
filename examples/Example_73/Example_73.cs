/*
 * Example_73.cs
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
 * Example_73.cs
 * The agenda of a team meeting, sent to the people attending before it: when
 * and where it is, who comes and who takes the notes, the topics with the
 * time each one starts and who leads it, and the actions from the last
 * meeting, to be checked at the start.
 *
 * This is a document people receive, not a form: every part has its data.
 * The topics are given with the minutes each one takes, and the start time
 * of each, and the end of the meeting, are worked out from them, so a topic
 * that is added or made longer moves the times after it. The two tables are
 * placed one under the other from where DrawOn says the one before ended.
 *
 * The document is PDF/UA-1: the title and the two sections are headings, and
 * the tables are read by a screen reader by their column headers. The layout
 * is that of the meeting agenda template of PDFjet Forms, and Example_74 is
 * the minutes of the same meeting. The studio and the people are made up.
 */
public class Example_73 {
    // A topic of the meeting: what it is, who leads it, and how long it takes
    private sealed class Topic {
        internal readonly String title;
        internal readonly String leader;
        internal readonly int minutes;

        internal Topic(String title, String leader, int minutes) {
            this.title = title;
            this.leader = leader;
            this.minutes = minutes;
        }
    }

    // The data of the meeting. In an application it comes from the calendar.
    private const String TEAM = "Northwind Studio";
    private static readonly DateTime DATE = new DateTime(2026, 11, 12);
    private static readonly TimeOnly START = new TimeOnly(10, 0);
    private const String PLACE = "Meeting room 2, or by video call";
    private const String PEOPLE = "Attending: Dana, Sam, Jordan, Lee · Notes: Sam";

    private static readonly Topic[] TOPICS = {
        new Topic("Welcome and the notes of the last meeting", "Dana", 5),
        new Topic("Contoso Books website: the wireframes", "Jordan", 20),
        new Topic("New clients this month", "Lee", 20),
        new Topic("Holiday schedule", "Sam", 10),
        new Topic("Anything else, and the next meeting", "All", 5),
    };

    // The actions agreed at the last meeting: what, who, and by when
    private static readonly String[][] OPEN_ACTIONS = {
        new String[] {"Write up the interviews at Contoso Books", "Jordan", "2026-11-09"},
        new String[] {"Call the two new clients", "Lee", "2026-11-19"},
        new String[] {"Post the holiday schedule", "Sam", "2026-11-13"},
    };

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float LEFT = 48f;
    private const float WIDTH = 516f;

    // A time of day as the team writes it, 10:05
    private const String TIME = "H:mm";

    private readonly Page page;
    private readonly Font regular;
    private readonly Font semiBold;

    public Example_73() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_73.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Team meeting, " + Iso(DATE));
        pdf.SetAuthor(TEAM);
        pdf.SetLanguage("en-CA");

        regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The topics, each starting when the one before it ends. The meeting
        // ends when the last one does.
        List<List<Cell>> topics = new List<List<Cell>>();
        topics.Add(Row(semiBold, "Time", "Topic", "Who"));
        TimeOnly time = START;
        foreach (Topic topic in TOPICS) {
            topics.Add(Row(regular, time.ToString(TIME, CultureInfo.InvariantCulture), topic.title, topic.leader));
            time = time.AddMinutes(topic.minutes);
        }
        TimeOnly end = time;

        // The title, when and where, and a rule under them
        float y = Heading("Team meeting", StructElem.H1, 18f, 62f);
        String when = DATE.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture)
                + " · " + START.ToString(TIME, CultureInfo.InvariantCulture) + " to "
                + end.ToString(TIME, CultureInfo.InvariantCulture) + "\n" + PLACE;
        y = Paragraph(when, y) + 12f;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // Who comes, and who takes the notes
        y = Paragraph(PEOPLE, y + 12f);

        // The agenda
        y = Heading("Agenda", StructElem.H2, 14f, y + 30f);
        y = Table(topics, y, 15f, 60f, 25f);

        // The actions of the last meeting, checked at its start
        List<List<Cell>> actions = new List<List<Cell>>();
        actions.Add(Row(semiBold, "Action", "Who", "By"));
        foreach (String[] action in OPEN_ACTIONS) {
            actions.Add(Row(regular, action));
        }
        y = Heading("Actions from the last meeting", StructElem.H2, 14f, y + 30f);
        Table(actions, y, 60f, 20f, 20f);

        pdf.Complete();
    }

    // Draws a heading in blue, its baseline at y, and returns where the part
    // under it starts.
    private float Heading(String text, StructElem level, float size, float y) {
        TextLine heading = new TextLine(semiBold, text);
        heading.SetStructureType(level);
        heading.SetFontSize(size);
        heading.SetTextColor(BLUE);
        heading.SetLocation(LEFT, y);
        heading.DrawOn(page);
        return y + 10f;
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns where it ended.
    private float Paragraph(String text, float y) {
        TextBlock block = new TextBlock(regular, text);
        block.SetLineSpacing(1.15f);
        block.SetLocation(LEFT, y);
        block.SetWidth(WIDTH);
        return block.DrawOn(page)[1];
    }

    // Draws a table across the page, its first row the header, the columns
    // in percent of the width, and returns where it ended.
    private float Table(List<List<Cell>> rows, float y, params float[] percents) {
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
        table.SetCellPadding(4f, 3f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BORDER);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(percents);
        table.SetLocation(LEFT, y);
        return table.DrawOn(page)[1];
    }

    // A row of a table, a cell in the font for each text.
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
        new Example_73();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_73 => {time1 - time0,4} ms");
    }
}   // End of Example_73.cs
