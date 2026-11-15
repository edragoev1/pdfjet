/*
 * Example_73.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.time.LocalDate;
import java.time.LocalTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * The agenda of a team meeting, sent to the people attending before it: when
 * and where it is, who comes and who takes the notes, the topics with the
 * time each one starts and who leads it, and the actions from the last
 * meeting, to be checked at the start.
 * <p>
 * This is a document people receive, not a form: every part has its data.
 * The topics are given with the minutes each one takes, and the start time
 * of each, and the end of the meeting, are worked out from them, so a topic
 * that is added or made longer moves the times after it. The two tables are
 * placed one under the other from where drawOn says the one before ended.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the two sections are headings, and
 * the tables are read by a screen reader by their column headers. The layout
 * is that of the meeting agenda template of PDFjet Forms, and Example_74 is
 * the minutes of the same meeting. The studio and the people are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_73 {
    // A topic of the meeting: what it is, who leads it, and how long it takes
    static final class Topic {
        final String title;
        final String leader;
        final int minutes;

        Topic(String title, String leader, int minutes) {
            this.title = title;
            this.leader = leader;
            this.minutes = minutes;
        }
    }

    // The data of the meeting. In an application it comes from the calendar.
    static final String TEAM = "Northwind Studio";
    static final LocalDate DATE = LocalDate.of(2026, 11, 12);
    static final LocalTime START = LocalTime.of(10, 0);
    static final String PLACE = "Meeting room 2, or by video call";
    static final String PEOPLE = "Attending: Dana, Sam, Jordan, Lee · Notes: Sam";

    static final Topic[] TOPICS = {
        new Topic("Welcome and the notes of the last meeting", "Dana", 5),
        new Topic("Contoso Books website: the wireframes", "Jordan", 20),
        new Topic("New clients this month", "Lee", 20),
        new Topic("Holiday schedule", "Sam", 10),
        new Topic("Anything else, and the next meeting", "All", 5),
    };

    // The actions agreed at the last meeting: what, who, and by when
    static final String[][] OPEN_ACTIONS = {
        {"Write up the interviews at Contoso Books", "Jordan", "2026-11-09"},
        {"Call the two new clients", "Lee", "2026-11-19"},
        {"Post the holiday schedule", "Sam", "2026-11-13"},
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float LEFT = 48f;
    static final float WIDTH = 516f;

    // A time of day as the team writes it, 10:05
    static final DateTimeFormatter TIME = DateTimeFormatter.ofPattern("H:mm", Locale.US);

    private final Page page;
    private final Font regular;
    private final Font semiBold;

    public Example_73() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_73.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Team meeting, " + DATE);
        pdf.setAuthor(TEAM);
        pdf.setLanguage("en-CA");

        regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The topics, each starting when the one before it ends. The meeting
        // ends when the last one does.
        List<List<Cell>> topics = new ArrayList<List<Cell>>();
        topics.add(row(semiBold, "Time", "Topic", "Who"));
        LocalTime time = START;
        for (Topic topic : TOPICS) {
            topics.add(row(regular, time.format(TIME), topic.title, topic.leader));
            time = time.plusMinutes(topic.minutes);
        }
        LocalTime end = time;

        // The title, when and where, and a rule under them
        float y = heading("Team meeting", StructElem.H1, 18f, 62f);
        String when = DATE.format(DateTimeFormatter.ofPattern("EEEE, MMMM d, yyyy", Locale.US))
                + " · " + START.format(TIME) + " to " + end.format(TIME) + "\n" + PLACE;
        y = paragraph(when, y) + 12f;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // Who comes, and who takes the notes
        y = paragraph(PEOPLE, y + 12f);

        // The agenda
        y = heading("Agenda", StructElem.H2, 14f, y + 30f);
        y = table(topics, y, 15f, 60f, 25f);

        // The actions of the last meeting, checked at its start
        List<List<Cell>> actions = new ArrayList<List<Cell>>();
        actions.add(row(semiBold, "Action", "Who", "By"));
        for (String[] action : OPEN_ACTIONS) {
            actions.add(row(regular, action));
        }
        y = heading("Actions from the last meeting", StructElem.H2, 14f, y + 30f);
        table(actions, y, 60f, 20f, 20f);

        pdf.complete();
    }

    // Draws a heading in blue, its baseline at y, and returns where the part
    // under it starts.
    private float heading(String text, StructElem level, float size, float y) throws Exception {
        TextLine heading = new TextLine(semiBold, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(BLUE);
        heading.setLocation(LEFT, y);
        heading.drawOn(page);
        return y + 10f;
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns where it ended.
    private float paragraph(String text, float y) throws Exception {
        TextBlock block = new TextBlock(regular, text);
        block.setLineSpacing(1.15f);
        block.setLocation(LEFT, y);
        block.setWidth(WIDTH);
        return block.drawOn(page)[1];
    }

    // Draws a table across the page, its first row the header, the columns
    // in percent of the width, and returns where it ended.
    private float table(List<List<Cell>> rows, float y, float... percents) throws Exception {
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(percents);
        table.setLocation(LEFT, y);
        return table.drawOn(page)[1];
    }

    // A row of a table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_73();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_73 => %4d ms%n", time1 - time0);
    }
}   // End of Example_73.java
