/*
 * Example_83.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.ArrayList;
import java.util.List;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A volunteer sign-up sheet, to print and put on a clipboard at the front
 * desk: the event at the top, and a table of numbered empty rows in which
 * each volunteer writes their name and how to reach them, ticks the times
 * they are free, and says what they would like to help with.
 * <p>
 * This is a form people fill in, not a document with data. The template of
 * PDFjet Forms is a form for one volunteer, filled in on screen; on paper
 * the same questions go on one sheet for many people, a row for each, which
 * is how a sign-up is done at a table or a door. So the form is a Table
 * whose rows are empty: they are padded to about the height of a box, room
 * enough for handwriting, and the columns for the times are narrow, for a
 * tick. The page is Letter landscape, for the width of the columns.
 * </p>
 * <p>
 * The table is drawn with drawOn(pdf, page, pages, Letter.LANDSCAPE), so a
 * sheet of more rows than a page holds goes on to the next pages, with its
 * header row on each, and every page says "Page i of N". The pages are kept
 * in a list and added to the document at the end, when their number is
 * known.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title is a heading, and the sheet a table
 * that a screen reader reads by its column headers. The layout follows the
 * volunteer sign-up template of PDFjet Forms. The food bank is made up.
 * </p>
 *
 * @see Table
 * @see Cell
 */
public class Example_83 {
    static final String ORGANIZATION = "Harbourview Food Bank";
    static final String EVENT = "Holiday food drive, December 5 to 19, 2026";
    static final String INTRO = "Thank you for offering your time. Write your name and how "
            + "to reach you, and tick the times you are free: the mornings, afternoons and "
            + "evenings are those of weekdays. Under \"Help with\", write what you would like "
            + "to do: sorting, the front desk, driving, or anything. Our coordinator calls you "
            + "within a week.";

    // The rows of the sheet, as many as fit on the page. More go on to the
    // next page.
    static final int ROWS = 13;

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float LEFT = 48f;
    static final float TOP = 48f;
    static final float WIDTH = 696f;            // 792 less two margins of 48
    static final float FOOTER_ROOM = 56f;   // the bottom margin, clear of "Page i of N"

    public Example_83() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_83.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Volunteer sign-up");
        pdf.setAuthor(ORGANIZATION);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(10f);

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.LANDSCAPE, Page.DETACHED);
        pages.add(page);

        // The title and the event on one line, what to write, and a rule
        TextLine title = new TextLine(semiBold, "Volunteer sign-up");
        title.setStructureType(StructElem.H1);
        title.setFontSize(20f);
        title.setTextColor(BLUE);
        title.setLocation(LEFT, 62f);
        title.drawOn(page);
        TextLine event = new TextLine(regular, ORGANIZATION + " · " + EVENT);
        event.setLocation(LEFT + WIDTH - event.getWidth(), 62f);
        event.drawOn(page);

        TextBlock intro = new TextBlock(regular, INTRO);
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The sheet: a header row, and numbered empty rows to write in
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "No.", "Name", "Phone or email",
                "Mornings", "Afternoons", "Evenings", "Weekends", "Help with"));
        for (int i = 1; i <= ROWS; i++) {
            rows.add(row(regular, String.valueOf(i), "", "", "", "", "", "", ""));
        }

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        // The rows about as tall as a box of a form, the header row less so
        table.setCellPadding(5f, 7f);
        for (Cell cell : table.getRow(0)) {
            cell.setTopPadding(4f);
            cell.setBottomPadding(4f);
        }
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(5f, 22f, 22f, 9f, 9f, 9f, 9f, 15f);
        table.setTextAlignmentInColumn(0, Alignment.RIGHT);
        table.setTextColorInColumn(0, Color.dimgray);
        table.setLocation(LEFT, TOP);
        table.setFirstPageTopMargin(y + 16f);
        table.setBottomMargin(FOOTER_ROOM);
        table.drawOn(pdf, page, pages, Letter.LANDSCAPE);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            Page p = pages.get(i);
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.size());
            footer.setFontSize(10f);
            footer.setLocation(LEFT + WIDTH - footer.getWidth(), p.getHeight() - 30f);
            p.addArtifactBMC();
            footer.drawOn(p);
            p.addEMC();
        }
        pdf.addPages(pages);
        pdf.complete();
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
        new Example_83();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_83 => %4d ms%n", time1 - time0);
    }
}   // End of Example_83.java
