/*
 * Example_61.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A timesheet of one week, filled in: who worked, the week and the project,
 * a table of the seven days with the hours and the work done on each, the
 * total and the overtime, and the lines where the employee and the manager
 * sign.
 * <p>
 * This is the timesheet as the manager receives it, a finished document: the
 * hours come from a time tracking application, and the total and the
 * overtime are computed from them, not typed in. The hours are BigDecimal:
 * a tenth of an hour, six minutes, is not exact in a double, and a sum of
 * them can come out as 7.499999. The dates of the days are worked out from
 * the first day of the week.
 * </p>
 * <p>
 * The days are a Table: a header row, a row for each day, and a footer row
 * with the total, in the header's style. Table wraps a long description of
 * the work in its column and makes the row as tall as it needs; a day of no
 * work keeps its row, empty, so that the week reads from Monday to Sunday.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title is a heading, the days a table that a
 * screen reader reads by its column headers, and the rules and the lines to
 * sign on are artifacts. The layout is that of the timesheet template of
 * PDFjet Forms. The people and the companies are made up.
 * </p>
 *
 * @see Table
 */
public class Example_61 {
    // The data of the timesheet. In an application it comes from the time
    // tracking: the hours and the work of each day, from Monday on.
    static final String COMPANY = "Northwind Studio";
    static final String EMPLOYEE = "Jordan Ellis";
    static final String PROJECT = "Contoso Books, the new website";
    static final LocalDate WEEK_START = LocalDate.of(2026, 11, 9);     // A Monday
    static final BigDecimal REGULAR_HOURS = new BigDecimal("40");

    static final String[][] DAYS = {
        {"8", "The notes of the interviews at the shop written up for the team"},
        {"8.5", "The site map, first draft, from what the shop's team said: how the orders "
                + "come in today, which titles sell online, and what the catalog needs"},
        {"7.5", "The site map revised; wireframes of the home and title pages"},
        {"9", "Wireframes for phones; the order pages"},
        {"8", "Review with the client by video call, notes for the design phase"},
        {"2.5", "The list of photos of the new titles, for the shoot"},
        {"", ""},
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;
    static final int INK = 0x333333;

    static final float LEFT = 48f;
    static final float WIDTH = 516f;            // Letter, 612 wide, less the margins

    public Example_61() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_61.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Timesheet, " + EMPLOYEE + ", week of " + WEEK_START);
        pdf.setAuthor(EMPLOYEE);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The total and the overtime, from the hours of the days
        BigDecimal total = BigDecimal.ZERO;
        for (String[] day : DAYS) {
            if (!day[0].isEmpty()) {
                total = total.add(new BigDecimal(day[0]));
            }
        }
        BigDecimal overtime = total.subtract(REGULAR_HOURS).max(BigDecimal.ZERO);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The title, the company, and a rule
        TextLine title = new TextLine(semiBold, "Timesheet");
        title.setStructureType(StructElem.H1);
        title.setFontSize(20f);
        title.setTextColor(BLUE);
        title.setLocation(LEFT, 64f);
        title.drawOn(page);
        TextLine company = new TextLine(semiBold, COMPANY);
        company.setLocation(LEFT + WIDTH - company.getWidth(), 64f);
        company.drawOn(page);
        rule(page, 80f);

        // Who, which week, and for what: a label and its value on each line
        DateTimeFormatter longDate = DateTimeFormatter.ofPattern("MMMM d, yyyy", Locale.US);
        float y = detail(page, semiBold, regular, "Employee", EMPLOYEE, 106f);
        y = detail(page, semiBold, regular, "Week", WEEK_START.format(longDate)
                + " to " + WEEK_START.plusDays(6).format(longDate), y);
        y = detail(page, semiBold, regular, "Project", PROJECT, y);

        // The days: the name, the date and the hours of each, the work done,
        // and the total under them
        DateTimeFormatter dayName = DateTimeFormatter.ofPattern("EEEE", Locale.US);
        DateTimeFormatter shortDate = DateTimeFormatter.ofPattern("MMM d", Locale.US);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Day", "Date", "Hours", "Work done"));
        for (int i = 0; i < DAYS.length; i++) {
            LocalDate date = WEEK_START.plusDays(i);
            rows.add(row(regular, date.format(dayName), date.format(shortDate),
                    hours(DAYS[i][0]), DAYS[i][1]));
        }
        rows.add(row(semiBold, "Total", "", hours(total), ""));

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setNumberOfFooterRows(1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setFooterRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(16f, 12f, 10f, 62f);
        table.setTextAlignmentInColumn(2, Alignment.RIGHT);
        table.setLocation(LEFT, y + 18f);
        y = table.drawOn(page)[1];

        // The regular hours, and the overtime, the hours over them
        y = detail(page, semiBold, regular, "Regular", hours(total.min(REGULAR_HOURS)) + " hours", y + 28f);
        y = detail(page, semiBold, regular, "Overtime", hours(overtime) + " hours", y);

        // What the signatures confirm, and the lines to sign on, the
        // employee's and the manager's
        new TextLine(regular, "I confirm these hours and sign below.")
                .setLocation(LEFT, y + 28f)
                .drawOn(page);
        y += 90f;
        float signWidth = 0.6f * WIDTH - 24f;
        float dateX = LEFT + 0.6f * WIDTH;
        signature(page, regular, "Employee, " + EMPLOYEE, LEFT, signWidth, y);
        signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);
        y += 60f;
        signature(page, regular, "Approved by the manager", LEFT, signWidth, y);
        signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);

        pdf.complete();
    }

    // Draws a label and its value beside it, the baseline at y, and returns
    // the baseline of the next line.
    private static float detail(Page page, Font labelFont, Font font, String label, String value, float y)
            throws Exception {
        new TextLine(labelFont, label).setLocation(LEFT, y).drawOn(page);
        new TextLine(font, value).setTextColor(INK).setLocation(LEFT + 80f, y).drawOn(page);
        return y + 17f;
    }

    // A line to sign on at y, of the width, with what it is for under it. The line is an
    // artifact; its caption is text.
    private static void signature(Page page, Font font, String caption, float x, float width, float y)
            throws Exception {
        Line line = new Line(x, y, x + width, y);
        line.setStrokeWidth(0.75f);
        line.setStrokeColor(INK);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
        new TextLine(font, caption)
                .setFontSize(9f)
                .setTextColor(Color.dimgray)
                .setLocation(x, y + 13f)
                .drawOn(page);
    }

    // The hours with two decimals, as 7.50, so that they line up in their
    // column, or nothing for a day of no work.
    private static String hours(String hours) {
        return hours.isEmpty() ? "" : hours(new BigDecimal(hours));
    }

    private static String hours(BigDecimal hours) {
        return hours.setScale(2).toPlainString();
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private static void rule(Page page, float y) throws Exception {
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.setStrokeColor(RULE);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_61();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_61 => %4d ms%n", time1 - time0);
    }
}   // End of Example_61.java
