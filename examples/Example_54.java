/*
 * Example_54.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A quote of a small design studio to a prospective customer, on Letter
 * paper: the studio and the quote number at the top with the date it is
 * valid until, who it is for and the project, a table of the work in days,
 * the subtotal, the sales tax, the total and the deposit, the terms, and
 * lines for the customer to sign the quote and send it back.
 * <p>
 * A quote is a finished document that the customer receives, not a form.
 * The amounts are BigDecimal, computed from the lines of work, and the date
 * it is valid until and the deposit are computed too, so that a change of
 * the day rate changes every number that depends on it. The table goes on
 * to as many pages as it needs; what follows it moves to a new page when it
 * no longer fits. A signed quote is a contract that both sides keep, so the
 * document is PDF/A-3a and PDF/UA-1 at once, as the invoice of Example_53
 * is. The layout is that of the quote template of PDFjet Forms. The
 * companies and the numbers are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_54 {
    // A line of the work: so many days at the day rate.
    static final class Work {
        final String description;
        final int days;

        Work(String description, int days) {
            this.description = description;
            this.days = days;
        }

        BigDecimal amount() {
            return DAY_RATE.multiply(BigDecimal.valueOf(days));
        }
    }

    // The data of the quote. In an application it comes from the database.
    static final String NUMBER = "Q-2026-017";
    static final LocalDate DATE = LocalDate.of(2026, 10, 5);
    static final int VALID_DAYS = 30;
    static final String CURRENCY = "CAD";
    static final BigDecimal DAY_RATE = new BigDecimal("800.00");
    static final String TAX_NAME = "HST";
    static final BigDecimal TAX_RATE = new BigDecimal("0.13");
    static final BigDecimal DEPOSIT_RATE = new BigDecimal("0.30");

    static final String SELLER = "Northwind Studio";
    static final String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    static final String CUSTOMER =
            "Tamarack Hill Community Theatre\n27 Mill Street\nOwen Sound, ON N4K 2A1";
    static final String PROJECT = "The program book of the 2027 season: its design, "
            + "a layout of 40 pages, and an accessible PDF/UA edition.";

    static final Work[] WORK = {
        new Work("Discovery and a plan of the content", 1),
        new Work("Design of the layout and the cover", 3),
        new Work("Layout of 40 pages, two rounds of changes", 4),
        new Work("Accessible PDF/UA edition, checked page by page", 1),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;
    static final int GRAY = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"
    static final float GAP = 14f;

    public Example_54() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_54.pdf")));
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.setTitle("Quote " + NUMBER);
        pdf.setAuthor(SELLER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The totals, from the work. The tax is rounded to the cent once, on
        // the subtotal, and the deposit is a share of the total with tax.
        BigDecimal subtotal = BigDecimal.ZERO;
        for (Work work : WORK) {
            subtotal = subtotal.add(work.amount());
        }
        BigDecimal tax = subtotal.multiply(TAX_RATE).setScale(2, RoundingMode.HALF_UP);
        BigDecimal total = subtotal.add(tax);
        BigDecimal deposit = total.multiply(DEPOSIT_RATE).setScale(2, RoundingMode.HALF_UP);

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).setLocation(left, 80f).setWidth(250f).drawOn(page);

        TextLine title = new TextLine(semiBold, "Quote");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(BLUE);
        title.setLocation(right - title.getWidth(), 72f);
        title.drawOn(page);

        new TextBlock(regular, "No. " + NUMBER + "\nDate: " + DATE
                + "\nValid until: " + DATE.plusDays(VALID_DAYS))
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200f, 94f)
                .setWidth(200f)
                .drawOn(page);

        line(page, left, right, 148f, RULE, 1f);

        // Who the quote is for, and the project, side by side. The taller of
        // the two decides where the next part starts.
        heading(page, semiBold, "Prepared for", left, 182f);
        float middle = (left + right) / 2f;
        float y1 = paragraph(page, regular, CUSTOMER, left, 192f, middle - left - GAP);
        heading(page, semiBold, "Project", middle, 182f);
        float y = Math.max(y1, paragraph(page, regular, PROJECT, middle, 192f, right - middle));

        y = paragraph(page, regular, "Thank you for asking us. Below is what we propose, and "
                + "what it costs. Prices are in Canadian dollars.", left, y + GAP, right - left);

        // The work, in a table that goes on to more pages when it needs them
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Work", "Days", "Rate", "Amount"));
        for (Work work : WORK) {
            rows.add(row(regular, work.description, String.valueOf(work.days),
                    money(DAY_RATE), money(work.amount())));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(right - left);
        table.setColumnWidthsInPercent(55f, 15f, 15f, 15f);
        for (int column = 1; column < 4; column++) {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.setLocation(left, MARGIN);
        table.setFirstPageTopMargin(y + GAP);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The totals, the terms and the signature lines stay together: about
        // 300 points. When they no longer fit under the table, they go on a
        // page of their own.
        y = end[1] + 22f;
        if (y + 300f > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        String rate = TAX_RATE.movePointRight(2).stripTrailingZeros().toPlainString();
        String share = DEPOSIT_RATE.movePointRight(2).stripTrailingZeros().toPlainString();
        drawRight(page, regular, 11f, "Subtotal: " + money(subtotal), right, y);
        drawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + money(tax), right, y + 15f);
        drawRight(page, semiBold, 14f, "Total: " + money(total) + " " + CURRENCY, right, y + 43f);
        drawRight(page, regular, 11f, "Deposit, " + share + "%: " + money(deposit), right, y + 62f);

        y = heading(page, semiBold, "Terms", left, y + 100f);
        y = paragraph(page, regular, "This quote is valid for " + VALID_DAYS + " days. A deposit "
                + "of " + share + "% starts the work, and the rest is due on delivery. Two rounds "
                + "of changes are included; more are " + money(DAY_RATE) + " a day. The work takes "
                + "about four weeks from the deposit.", left, y, right - left);

        y = heading(page, semiBold, "Acceptance", left, y + 2 * GAP + 6f);
        y = paragraph(page, regular, "To accept this quote, sign below and send it back to us, "
                + "with the deposit.", left, y, right - left);

        // A line to sign on and one to date, each with its label under it
        float dateX = left + 0.6f * (right - left);
        y += 48f;
        line(page, left, dateX - GAP, y, GRAY, 0.75f);
        line(page, dateX, right, y, GRAY, 0.75f);
        new TextLine(regular, "Name and signature").setFontSize(10f).setTextColor(GRAY)
                .setLocation(left, y + 14f).drawOn(page);
        new TextLine(regular, "Date").setFontSize(10f).setTextColor(GRAY)
                .setLocation(dateX, y + 14f).drawOn(page);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.size());
            footer.setFontSize(10f);
            footer.setLocation(right - footer.getWidth(), page.getHeight() - 52f);
            pages.get(i).addArtifactBMC();
            footer.drawOn(pages.get(i));
            pages.get(i).addEMC();
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    // Draws a heading of a part, its baseline at y, and returns where the
    // text under it starts.
    private static float heading(Page page, Font font, String text, float x, float y)
            throws Exception {
        new TextLine(font, text).setStructureType(StructElem.H2).setFontSize(12f)
                .setLocation(x, y).drawOn(page);
        return y + 10f;
    }

    // Draws the text wrapped to the width, its top at y, and returns the y
    // where it ended.
    private static float paragraph(Page page, Font font, String text, float x, float y, float width)
            throws Exception {
        return new TextBlock(font, text).setLineSpacing(1.15f).setLocation(x, y).setWidth(width)
                .drawOn(page)[1];
    }

    // A line across, decoration or a line to sign on that its label names,
    // so an artifact that a screen reader skips.
    private static void line(Page page, float x1, float x2, float y, int color, float width)
            throws Exception {
        Line line = new Line(x1, y, x2, y);
        line.setStrokeColor(color);
        line.setStrokeWidth(width);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
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

    // The amount with two decimals and commas between the thousands, as
    // 2,400.00, whatever the locale of the computer.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_54();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_54 => %4d ms%n", time1 - time0);
    }
}   // End of Example_54.java
