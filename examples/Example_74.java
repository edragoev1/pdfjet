/*
 * Example_74.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * The minutes of a team meeting, sent to the team after it: when and where
 * it was, who was there and who was not, what was said on each topic of the
 * agenda, the actions agreed, with who does each and by when, and the date
 * of the next meeting, with "Page 1 of 1" at the bottom.
 * <p>
 * This is a document people receive, not a form. It is the record of the
 * meeting of Example_73, whose agenda it follows topic by topic. Minutes
 * have no fixed length, so each part goes on a new page when it no longer
 * fits on the one it is on: a paragraph is measured first, with drawOn of a
 * null page, which returns where it would end without drawing it. The table
 * of the actions goes on to as many pages as it needs, its header row on
 * each. The pages are kept in a list and added to the document at the end,
 * when the footer can say how many there are.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title, the sections and the topics are
 * headings, and the page numbers are artifacts that a screen reader skips.
 * The layout is that of the meeting minutes template of PDFjet Forms. The
 * studio and the people are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_74 {
    // The data of the meeting. In an application it comes from the notes
    // taken during it.
    static final String TEAM = "Northwind Studio";
    static final LocalDate DATE = LocalDate.of(2026, 11, 12);
    static final String TIME_AND_PLACE = "10:00 to 11:00\nMeeting room 2, and by video call";
    static final String PEOPLE = "Present: Dana, Sam, Priya · Absent: Lee · Notes: Sam";
    static final LocalDate NEXT_MEETING = LocalDate.of(2026, 11, 26);

    // What was said, by topic of the agenda
    static final String[][] NOTES = {
        {"Annual report", "The printer delivers the 50 copies on November 20. Priya checks "
                + "the proofs on the 16th; the accessible PDF edition is done and was sent "
                + "to Contoso Books for review."},
        {"New clients", "Two new clients this month, both from the website. A third has "
                + "asked for a quote for a catalog of 32 pages. Lee was away, so Dana calls "
                + "the two new clients in his place."},
        {"Holidays", "The studio is closed from December 24 to January 2. Work due in that "
                + "week is moved to the week before; Sam tells the clients."},
    };

    // The actions agreed: what, who, and by when
    static final String[][] ACTIONS = {
        {"Check the printer's proofs", "Priya", "2026-11-16"},
        {"Send the quote for the catalog", "Dana", "2026-11-14"},
        {"Call the two new clients", "Dana", "2026-11-19"},
        {"Tell clients of the holiday closing", "Sam", "2026-12-01"},
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float LEFT = 48f;
    static final float WIDTH = 516f;
    static final float TOP = 62f;
    // The text ends this far from the bottom of the page, above the footer
    static final float BOTTOM = 80f;

    private final PDF pdf;
    private final List<Page> pages = new ArrayList<Page>();
    private Page page;
    private final Font regular;
    private final Font semiBold;

    public Example_74() throws Exception {
        pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_74.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Team meeting, " + DATE + ": minutes");
        pdf.setAuthor(TEAM);
        pdf.setLanguage("en-CA");

        regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        newPage();

        // The title, when and where, and a rule under them
        float y = heading("Team meeting: minutes", StructElem.H1, 18f, TOP);
        String date = DATE.format(DateTimeFormatter.ofPattern("EEEE, MMMM d, yyyy", Locale.US));
        y = paragraph(date + " · " + TIME_AND_PLACE, y) + 12f;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        y = paragraph(PEOPLE, y + 12f);

        // What was said: a heading for each topic and the notes under it
        y = heading("What was said", StructElem.H2, 14f, y + 30f);
        for (String[] note : NOTES) {
            y = heading(note[0], StructElem.H3, 11f, y + 18f);
            y = paragraph(note[1], y);
        }

        // The actions, a table that goes on to the next pages when it must
        y = heading("Actions", StructElem.H2, 14f, y + 30f);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Action", "Who", "By"));
        for (String[] action : ACTIONS) {
            rows.add(row(regular, action));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(60f, 20f, 20f);
        table.setLocation(LEFT, TOP);
        table.setFirstPageTopMargin(y);
        table.setBottomMargin(BOTTOM);
        y = table.drawOn(pdf, page, pages, Letter.PORTRAIT)[1];
        page = pages.get(pages.size() - 1);

        String next = NEXT_MEETING.format(DateTimeFormatter.ofPattern("EEEE, MMMM d, yyyy", Locale.US));
        paragraph("Next meeting: " + next + ", at 10:00.", y + 18f);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.size());
            footer.setFontSize(10f);
            footer.setLocation(LEFT + WIDTH - footer.getWidth(), page.getHeight() - 52f);
            pages.get(i).addArtifactBMC();
            footer.drawOn(pages.get(i));
            pages.get(i).addEMC();
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    // Starts a page, kept in the list until the end.
    private void newPage() throws Exception {
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
    }

    // Draws a heading in blue, its baseline at y, and returns where the text
    // under it starts. A heading with no room for a line under it goes on to
    // a new page, so that it is never left alone at the bottom.
    private float heading(String text, StructElem level, float size, float y) throws Exception {
        if (y + 40f > page.getHeight() - BOTTOM) {
            newPage();
            y = TOP;
        }
        TextLine heading = new TextLine(semiBold, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(BLUE);
        heading.setLocation(LEFT, y);
        heading.drawOn(page);
        return y + 8f;
    }

    // Draws the text wrapped to the width of the page, its top at y, on a new
    // page when it does not fit, and returns where it ended.
    private float paragraph(String text, float y) throws Exception {
        TextBlock block = new TextBlock(regular, text);
        block.setLineSpacing(1.15f);
        block.setWidth(WIDTH);
        block.setLocation(LEFT, y);
        if (block.drawOn(null)[1] > page.getHeight() - BOTTOM) {
            newPage();
            block.setLocation(LEFT, TOP);
        }
        return block.drawOn(page)[1];
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_74();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_74 => %4d ms%n", time1 - time0);
    }
}   // End of Example_74.java
