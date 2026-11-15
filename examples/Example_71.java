/*
 * Example_71.java
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
 * A monthly safety inspection checklist to print and take on a walk through
 * a building: who inspects, when and where, then for each area a row for
 * each item to check, with a box to tick when it is in order, a box when it
 * needs fixing, and room for a note; under the list, the problems found, the
 * date of the next inspection, and the signature.
 * <p>
 * This is a form, not a finished document: the inspector fills it in. The
 * fields above and below the list are drawn as in the job application of
 * Example_72, a label above each box. The list is a Table: the items come
 * from an array, each area is a row that spans the four columns, and each
 * box to tick is a square, a Rect placed in its cell with setDrawable. The
 * table goes on to as many pages as it needs, with its header row on each,
 * so a building with sixty items prints on two pages with the same code;
 * the fields under it follow the last row, or go to a page of their own
 * when they no longer fit.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title is a heading, the list is a table
 * that a screen reader reads by its column headers, and the boxes, of the
 * fields and to tick, are artifacts. The layout is that of the inspection
 * checklist template of PDFjet Forms. The building is made up.
 * </p>
 *
 * @see Table
 * @see Cell#setDrawable
 * @see Rect
 */
public class Example_71 {
    static final String SITE = "Pinecrest Community Center";

    // The areas to inspect, each its name and then its items
    static final String[][] AREAS = {
        {"Exits and fire safety",
            "Exits and hallways are clear",
            "Exit signs are lit",
            "Fire extinguishers are in place and charged",
            "Smoke alarms were tested"},
        {"Work areas",
            "Floors are dry and free of trip hazards",
            "Electrical cords are in good shape",
            "Shelves and storage are stable",
            "The first aid kit is stocked"},
    };

    static final int BLUE = 0x1f58b0;
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;
    static final int HEADER = 0xe3edf9;
    static final int AREA = 0xf1f3f6;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static final float LEFT = 48f;
    static final float WIDTH = 516f;
    static final float GAP = 12f;
    static final float HALF = (WIDTH - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    // The side of a box to tick, about the height of the capital letters
    static final float TICK = 11f;

    // Room at the foot of each page for the footer, and the height of the
    // fields under the list
    static final float BOTTOM = 72f;
    static final float FIELDS_HEIGHT = 170f;

    private Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_71() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_71.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Monthly safety inspection");
        pdf.setAuthor(SITE);
        pdf.setLanguage("en-US");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new ArrayList<Page>();
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);

        // The title, how to use the list, and a rule
        heading("Monthly safety inspection", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Walk through each area, tick what is in "
                + "order, and note anything that needs fixing.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // Where, who and when, a row of a half and two quarters. The list is
        // printed for one building, so its name is filled in already.
        y += 40f;
        float quarter = (HALF - GAP) / 2f;
        field("Location *", LEFT, y, HALF, 1, SITE);
        field("Inspected by *", LEFT + HALF + GAP, y, quarter, 1, null);
        y = field("Date *", LEFT + HALF + GAP + quarter + GAP, y, quarter, 1, null);

        // The list: a header row, then each area and its items. The row of an
        // area is kept on the page of its first item.
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        List<Integer> areaRows = new ArrayList<Integer>();
        rows.add(row(new Cell(labelFont, "Item"), new Cell(labelFont, "OK"),
                new Cell(labelFont, "To fix"), new Cell(labelFont, "Notes")));
        for (String[] area : AREAS) {
            // The name of the area across the row; a cell that spans columns
            // is followed by empty ones that it covers
            Cell name = new Cell(labelFont, area[0]).setColSpan(4).setBackgroundColor(AREA);
            areaRows.add(rows.size());
            rows.add(row(name, new Cell(textFont), new Cell(textFont), new Cell(textFont)));
            for (int i = 1; i < area.length; i++) {
                rows.add(row(new Cell(textFont, area[i]), tick(textFont), tick(textFont),
                        new Cell(textFont)));
            }
        }

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(labelFont, Color.black, HEADER);
        table.setCellPadding(5f, 6f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BOX);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(50f, 9f, 9f, 32f);
        table.setTextAlignmentInColumn(1, Alignment.CENTER);
        table.setTextAlignmentInColumn(2, Alignment.CENTER);
        for (int index : areaRows) {
            table.keepRowWithNext(index);
        }
        // Under the fields on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.setLocation(LEFT, 48f);
        table.setFirstPageTopMargin(y + 6f);
        table.setBottomMargin(BOTTOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The fields under the last row. When they no longer fit on its page,
        // they go on a page of their own.
        y = end[1] + 18f;
        if (y + FIELDS_HEIGHT > page.getHeight() - BOTTOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 48f;
        }
        y = field("Problems found, and who will fix them", LEFT, y, WIDTH, 3, null);
        new CheckBox(textFont, "I confirm this inspection and sign below. *")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        y += 30f;
        field("Inspector's signature *", LEFT, y, HALF, 1, null);
        field("Date *", LEFT + HALF + GAP, y, quarter, 1, null);
        field("Next inspection", LEFT + HALF + GAP + quarter + GAP, y, quarter, 1, null);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            Page p = pages.get(i);
            TextLine footer = new TextLine(textFont, SITE + ", monthly safety inspection, page "
                    + (i + 1) + " of " + pages.size());
            footer.setFontSize(9f);
            footer.setTextColor(Color.dimgray);
            footer.setLocation(LEFT, p.getHeight() - 36f);
            p.addArtifactBMC();
            footer.drawOn(p);
            p.addEMC();
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    // A cell with a box to tick: a square, placed in the cell with
    // setDrawable and centered as the column is. A Rect is drawn as an
    // artifact; the column header says what a tick in it means.
    private static Cell tick(Font font) {
        Rect box = new Rect(0f, 0f, TICK, TICK);
        box.setBorderColor(BOX);
        box.setBorderWidth(0.75f);
        return new Cell(font).setDrawable(box);
    }

    // A row of the table, of the cells.
    private static List<Cell> row(Cell... cells) {
        List<Cell> row = new ArrayList<Cell>();
        for (Cell cell : cells) {
            row.add(cell);
        }
        return row;
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    private float heading(String text, StructElem level, float size, float y) throws Exception {
        TextLine heading = new TextLine(labelFont, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(BLUE);
        heading.setLocation(LEFT, y);
        heading.drawOn(page);
        return y + 16f;
    }

    // Draws the label of a field, its top at y.
    private void label(String text, float x, float y) throws Exception {
        TextLine label = new TextLine(labelFont, text);
        label.setLocation(x, y + labelFont.getAscent());
        label.drawOn(page);
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    private float field(String text, float x, float y, float width, int lines, String value)
            throws Exception {
        label(text, x, y);
        float top = y + 16f;
        float height = BOX_HEIGHT + (lines - 1) * LINE_HEIGHT;
        Rect box = new Rect(x, top, width, height);
        box.setBorderColor(BOX);
        box.setBorderWidth(0.5f);
        box.drawOn(page);
        if (value != null) {
            TextLine line = new TextLine(textFont, value);
            line.setTextColor(BLUE);
            line.setLocation(x + 6f, top + 16f);
            line.drawOn(page);
        }
        return top + height + 12f;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private void rule(float y) throws Exception {
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.setStrokeColor(RULE);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_71();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_71 => %4d ms%n", time1 - time0);
    }
}   // End of Example_71.java
