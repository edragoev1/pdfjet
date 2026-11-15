/*
 * Example_80.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A proposal of a design studio to a customer, on Letter paper: the studio
 * and the date, the project, who it is for and who wrote it, the goal, a
 * table of the phases of the work with their weeks, a table of the price, the
 * total, and how to go ahead.
 * <p>
 * A proposal is a finished document that the customer receives, not a form.
 * It is read from top to bottom, so each part is placed under the one before
 * it, from where drawOn says the last one ended: a longer goal or one more
 * phase moves everything below it down. Each section is a heading and what
 * follows it, drawn by two small helpers. The weeks and the price are
 * computed from the tables' rows, the price in BigDecimal, so the totals
 * cannot disagree with the lines above them.
 * </p>
 * <p>
 * The document is PDF/UA-1: the project is the heading of the document and
 * each section a heading under it, so that a screen reader can go from one
 * to the next; the plan and the price are tables that it reads by their
 * column headers. The layout is that of the proposal template of PDFjet
 * Forms. The people and the companies are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_80 {
    static final String STUDIO = "Northwind Studio";
    static final String STUDIO_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";
    static final LocalDate DATE = LocalDate.of(2026, 10, 19);
    static final int VALID_DAYS = 30;

    static final String PROJECT = "A new website for Contoso Books";
    static final String PREPARED_FOR = "Priya Raman\nContoso Books";
    static final String PREPARED_BY = "Dana Whitfield\nDesign Lead, " + STUDIO;
    static final String GOAL = "A website that shows the new titles each season, lets readers order "
            + "from any phone, and that the shop's team can keep up to date without help.";

    // The phases: the name, what the customer gets, and the weeks it takes
    static final String[][] PHASES = {
        {"Discovery", "Interviews, a site map, the look in two directions", "2"},
        {"Design", "The pages designed for computers and phones", "3"},
        {"Build", "The site, the catalog and ordering, tested", "4"},
        {"Launch", "Training for your team, a month of support", "1"},
    };

    // The price, in two parts, in Canadian dollars before the sales tax
    static final String[][] PRICES = {
        {"Discovery and design", "4800.00"},
        {"Build and launch", "7200.00"},
    };
    static final String CURRENCY = "CAD";

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float LEFT = 48f;
    static final float WIDTH = 516f;            // Letter, 612 wide, less the margins
    static final float RIGHT = LEFT + WIDTH;

    private Page page;
    private Font regular;
    private Font semiBold;

    public Example_80() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_80.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Proposal: " + PROJECT);
        pdf.setAuthor(STUDIO);
        pdf.setLanguage("en-CA");

        regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The weeks and the price, from the rows of the tables. The customer
        // pays half when the work starts, the rest at the launch.
        int weeks = 0;
        for (String[] phase : PHASES) {
            weeks += Integer.parseInt(phase[2]);
        }
        BigDecimal total = BigDecimal.ZERO;
        for (String[] price : PRICES) {
            total = total.add(new BigDecimal(price[1]));
        }
        BigDecimal half = total.divide(new BigDecimal("2"));

        page = new Page(pdf, Letter.PORTRAIT);

        // The header: the studio on the left, what the document is and its
        // dates on the right
        new TextLine(semiBold, STUDIO).setFontSize(16f).setLocation(LEFT, 64f).drawOn(page);
        new TextBlock(regular, STUDIO_ADDRESS).setLocation(LEFT, 80f).setWidth(250f).drawOn(page);
        TextLine kind = new TextLine(semiBold, "Proposal");
        kind.setFontSize(26f);
        kind.setTextColor(BLUE);
        kind.setLocation(RIGHT - kind.getWidth(), 72f);
        kind.drawOn(page);
        new TextBlock(regular, "Date: " + DATE + "\nValid until: " + DATE.plusDays(VALID_DAYS))
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(RIGHT - 200f, 94f)
                .setWidth(200f)
                .drawOn(page);

        // A thin rule under the header, decoration only, so an artifact
        Line rule = new Line(LEFT, 148f, RIGHT, 148f);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The project, the heading of the document, and the two sides of it
        float y = heading(PROJECT, StructElem.H1, 18f, Color.black, 186f);
        new TextLine(semiBold, "Prepared for").setLocation(LEFT, y + 18f).drawOn(page);
        new TextLine(semiBold, "Prepared by").setLocation(LEFT + WIDTH / 2f, y + 18f).drawOn(page);
        new TextBlock(regular, PREPARED_FOR).setLocation(LEFT, y + 26f).setWidth(WIDTH / 2f).drawOn(page);
        y = new TextBlock(regular, PREPARED_BY)
                .setLocation(LEFT + WIDTH / 2f, y + 26f)
                .setWidth(WIDTH / 2f)
                .drawOn(page)[1];

        // The goal
        y = heading("The goal", StructElem.H2, 14f, BLUE, y + 30f);
        y = paragraph(GOAL, y);

        // The plan: the phases, and the weeks in all under them
        y = heading("The plan", StructElem.H2, 14f, BLUE, y + 26f);
        List<List<Cell>> plan = new ArrayList<List<Cell>>();
        plan.add(row(semiBold, "Phase", "What you get", "Weeks"));
        for (String[] phase : PHASES) {
            plan.add(row(regular, phase));
        }
        plan.add(row(semiBold, "In all", "", String.valueOf(weeks)));
        y = table(plan, y, 30f, 55f, 15f);

        // The price: the parts, and the total under them
        y = heading("The price", StructElem.H2, 14f, BLUE, y + 26f);
        List<List<Cell>> prices = new ArrayList<List<Cell>>();
        prices.add(row(semiBold, "Item", "Price"));
        for (String[] price : PRICES) {
            prices.add(row(regular, price[0], money(new BigDecimal(price[1]))));
        }
        prices.add(row(semiBold, "Total, " + CURRENCY + ", plus HST", money(total)));
        y = table(prices, y, 70f, 30f);

        // How to go ahead
        paragraph("Half, " + money(half) + " " + CURRENCY + ", is invoiced when the work starts, "
                + "and half at the launch. To go ahead, reply to this proposal, and we will set a "
                + "date for the first interview.", y + 18f);

        pdf.complete();
    }

    // Draws a heading in the size and the color, its top at y, and returns
    // where the text under it starts.
    private float heading(String text, StructElem level, float size, int color, float y)
            throws Exception {
        TextLine heading = new TextLine(semiBold, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(color);
        heading.setLocation(LEFT, y + heading.getAscent());
        heading.drawOn(page);
        return y + heading.getAscent() + 8f;
    }

    // Draws a paragraph across the page, its top at y, and returns where it ended.
    private float paragraph(String text, float y) throws Exception {
        TextBlock block = new TextBlock(regular, text);
        block.setLineSpacing(1.15f);
        block.setLocation(LEFT, y);
        block.setWidth(WIDTH);
        return block.drawOn(page)[1];
    }

    // Draws a table across the page, its top at y: a header row, the rows,
    // and a footer row of totals in the header's style, the last column
    // aligned on the right. Returns where it ended.
    private float table(List<List<Cell>> rows, float y, float... percents) throws Exception {
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setNumberOfFooterRows(1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setFooterRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(percents);
        table.setTextAlignmentInColumn(percents.length - 1, Alignment.RIGHT);
        table.setLocation(LEFT, y);
        return table.drawOn(page)[1];
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    // The amount with two decimals and commas between the thousands, as
    // 12,000.00. The locale is given, or a computer set to French writes
    // 12 000,00.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_80();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_80 => %4d ms%n", time1 - time0);
    }
}   // End of Example_80.java
