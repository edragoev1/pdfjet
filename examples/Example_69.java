/*
 * Example_69.java
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
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * An expense report of a business trip, filled in: who paid, what for and
 * when, a table of the expenses, the total of each category and the total to
 * pay back, how to pay it, and the lines where the employee and the manager
 * sign.
 * <p>
 * This is the report as the manager receives it, a finished document: the
 * expenses come from an application where each receipt was entered, and the
 * totals are computed from them, not typed in. The amounts are BigDecimal,
 * never float or double, so that they add up to the cent. The totals of the
 * categories are kept in a LinkedHashMap, in the order the categories first
 * appear, so the summary reads in the order of the trip.
 * </p>
 * <p>
 * The table of the expenses goes on to as many pages as it needs, with its
 * header row on each, as a month of receipts can take more than one page;
 * the totals go under its last row, or on a page of their own when they no
 * longer fit, and each page says "Page 1 of 2" at the bottom.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title is a heading, the expenses a table that
 * a screen reader reads by its column headers, and the rules and the lines to
 * sign on are artifacts. The layout is that of the expense report template of
 * PDFjet Forms. The people and the companies are made up.
 * </p>
 *
 * @see Table
 */
public class Example_69 {
    // An expense, as on its receipt. The amount is a BigDecimal, in dollars.
    static final class Expense {
        final LocalDate date;
        final String category;
        final String description;
        final BigDecimal amount;

        Expense(String date, String category, String description, String amount) {
            this.date = LocalDate.parse(date);
            this.category = category;
            this.description = description;
            this.amount = new BigDecimal(amount);
        }
    }

    // The data of the report. In an application it comes from the database.
    static final String COMPANY = "Northwind Studio";
    static final String EMPLOYEE = "Jordan Ellis";
    static final String PURPOSE = "Discovery interviews at Contoso Books, Toronto";
    static final LocalDate FROM = LocalDate.of(2026, 11, 2);
    static final LocalDate TO = LocalDate.of(2026, 11, 4);
    static final String CURRENCY = "CAD";
    static final String PAY_BACK_BY = "Bank transfer";

    static final Expense[] EXPENSES = {
        new Expense("2026-11-02", "Travel", "Bus, Port Elgin to Kitchener, return", "64.00"),
        new Expense("2026-11-02", "Travel", "Train, Kitchener to Toronto Union, return", "86.40"),
        new Expense("2026-11-02", "Lodging", "Hotel, two nights, taxes included", "412.30"),
        new Expense("2026-11-02", "Meals", "Dinner", "38.75"),
        new Expense("2026-11-03", "Meals", "Lunch with the shop's team, four people", "126.20"),
        new Expense("2026-11-03", "Supplies", "Sticky notes and markers for the workshop", "23.45"),
        new Expense("2026-11-03", "Meals", "Dinner", "41.10"),
        new Expense("2026-11-04", "Travel", "Subway fares, three days", "19.80"),
        new Expense("2026-11-04", "Meals", "Lunch at the station", "16.85"),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;
    static final int INK = 0x333333;

    static final float LEFT = 48f;
    static final float WIDTH = 516f;            // Letter, 612 wide, less the margins
    static final float RIGHT = LEFT + WIDTH;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_69() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_69.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Expense report, " + EMPLOYEE + ", " + FROM);
        pdf.setAuthor(EMPLOYEE);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The total of each category, and of all, from the expenses
        Map<String, BigDecimal> categories = new LinkedHashMap<String, BigDecimal>();
        BigDecimal total = BigDecimal.ZERO;
        for (Expense expense : EXPENSES) {
            BigDecimal sum = categories.get(expense.category);
            categories.put(expense.category, (sum == null) ? expense.amount : sum.add(expense.amount));
            total = total.add(expense.amount);
        }

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);

        // The title, the company, and a rule
        TextLine title = new TextLine(semiBold, "Expense report");
        title.setStructureType(StructElem.H1);
        title.setFontSize(20f);
        title.setTextColor(BLUE);
        title.setLocation(LEFT, 64f);
        title.drawOn(page);
        TextLine company = new TextLine(semiBold, COMPANY);
        company.setLocation(RIGHT - company.getWidth(), 64f);
        company.drawOn(page);
        rule(page, 80f);

        // Who, what for, and when: a label and its value on each line
        DateTimeFormatter longDate = DateTimeFormatter.ofPattern("MMMM d, yyyy", Locale.US);
        float y = detail(page, semiBold, regular, "Employee", EMPLOYEE, 106f);
        y = detail(page, semiBold, regular, "Purpose", PURPOSE, y);
        detail(page, semiBold, regular, "Dates", FROM.format(longDate) + " to " + TO.format(longDate), y);

        // The expenses: a header row, repeated on every page the table goes on to
        DateTimeFormatter shortDate = DateTimeFormatter.ofPattern("MMM d", Locale.US);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Date", "Category", "What it was", "Amount"));
        for (Expense expense : EXPENSES) {
            rows.add(row(regular, expense.date.format(shortDate), expense.category,
                    expense.description, money(expense.amount)));
        }

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(12f, 16f, 56f, 16f);
        table.setTextAlignmentInColumn(3, Alignment.RIGHT);
        // Under the details on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.setLocation(LEFT, 48f);
        table.setFirstPageTopMargin(172f);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The totals and the signatures, under the last row. When they no
        // longer fit on its page, they go on a page of their own.
        y = end[1] + 24f;
        float height = 17f * categories.size() + 190f;
        if (y + height > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        for (Map.Entry<String, BigDecimal> category : categories.entrySet()) {
            drawRight(page, regular, 11f, category.getKey() + ": " + money(category.getValue()), RIGHT, y);
            y += 17f;
        }
        drawRight(page, semiBold, 14f, "To pay back: " + money(total) + " " + CURRENCY, RIGHT, y + 12f);
        detail(page, semiBold, regular, "Pay back by", PAY_BACK_BY, y + 12f);

        // What the signatures confirm, and the lines to sign on, the
        // employee's and the manager's
        y += 46f;
        new TextLine(regular, "I confirm these expenses were for the business and sign below.")
                .setLocation(LEFT, y)
                .drawOn(page);
        y += 52f;
        float signWidth = 0.6f * WIDTH - 24f;
        float dateX = LEFT + 0.6f * WIDTH;
        signature(page, regular, "Employee, " + EMPLOYEE, LEFT, signWidth, y);
        signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);
        y += 52f;
        signature(page, regular, "Approved by the manager", LEFT, signWidth, y);
        signature(page, regular, "Date", dateX, 0.4f * WIDTH, y);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.size());
            footer.setFontSize(10f);
            footer.setLocation(RIGHT - footer.getWidth(), page.getHeight() - 52f);
            pages.get(i).addArtifactBMC();
            footer.drawOn(pages.get(i));
            pages.get(i).addEMC();
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    // Draws a label and its value beside it, the baseline at y, and returns
    // the baseline of the next line.
    private static float detail(Page page, Font labelFont, Font font, String label, String value, float y)
            throws Exception {
        new TextLine(labelFont, label).setLocation(LEFT, y).drawOn(page);
        new TextLine(font, value).setTextColor(INK).setLocation(LEFT + 90f, y).drawOn(page);
        return y + 17f;
    }

    // Draws a line of text in the font and the size, its baseline at y and
    // its end at x, as the amounts line up on their right.
    private static void drawRight(Page page, Font font, float size, String text, float x, float y)
            throws Exception {
        TextLine line = new TextLine(font, text);
        line.setFontSize(size);
        line.setLocation(x - line.getWidth(), y);
        line.drawOn(page);
    }

    // A line to sign on at y, of the width, with what it is for under it.
    // The line is an artifact; its caption is text.
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
        Line line = new Line(LEFT, y, RIGHT, y);
        line.setStrokeColor(RULE);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,034.85. The locale is given, or a computer set to French writes
    // 1 034,85.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_69();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_69 => %4d ms%n", time1 - time0);
    }
}   // End of Example_69.java
