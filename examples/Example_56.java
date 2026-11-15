/*
 * Example_56.java
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
 * A statement of account of a small design studio to a customer, on Letter
 * paper: the studio, the date and the period at the top, the customer, a
 * table of the invoices, credit notes and payments of the period with the
 * balance after each, the balance due, and when to pay it.
 * <p>
 * A statement is a finished document that the customer receives, not a
 * form. The balance of each line is computed from the one before it, the
 * charges added and the credits taken off, starting from the balance brought
 * forward from the period before; the balance due is the balance of the last
 * line. The amounts are BigDecimal, so that they add up to the cent over any
 * number of lines, and the table goes on to as many pages as it needs, with
 * its header row on each.
 * </p>
 * <p>
 * The lines are those of Example_53, the invoice, and Example_67, the credit
 * note against it, with a payment and a later invoice. Both sides keep a
 * statement with their books, so the document is PDF/A-3a and PDF/UA-1 at
 * once. The layout is that of the statement of account template of PDFjet
 * Forms. The companies and the numbers are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_56 {
    // A line of the statement: a charge, such as an invoice, or a credit,
    // such as a payment or a credit note. One of the two is zero.
    static final class Entry {
        final LocalDate date;
        final String description;
        final BigDecimal charge;
        final BigDecimal credit;

        Entry(LocalDate date, String description, String charge, String credit) {
            this.date = date;
            this.description = description;
            this.charge = new BigDecimal(charge);
            this.credit = new BigDecimal(credit);
        }
    }

    // The data of the statement. In an application it comes from the books.
    static final LocalDate PERIOD_START = LocalDate.of(2026, 9, 1);
    static final LocalDate DATE = LocalDate.of(2026, 10, 31);
    static final int PAYMENT_DAYS = 30;
    static final String CURRENCY = "CAD";
    static final BigDecimal BALANCE_FORWARD = new BigDecimal("0.00");

    static final String SELLER = "Northwind Studio";
    static final String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";
    static final String CUSTOMER = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4";

    static final Entry[] ENTRIES = {
        new Entry(LocalDate.of(2026, 9, 25), "Invoice 2026-042, annual report", "2495.61", "0"),
        new Entry(LocalDate.of(2026, 10, 5), "Credit note CN-2026-007", "0", "67.80"),
        new Entry(LocalDate.of(2026, 10, 20), "Payment, thank you", "0", "1000.00"),
        new Entry(LocalDate.of(2026, 10, 28), "Invoice 2026-051, display stand", "452.00", "0"),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_56() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_56.pdf")));
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.setTitle("Statement of account, " + DATE);
        pdf.setAuthor(SELLER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;

        // The header: the studio on the left, the title and the dates on the right
        new TextLine(semiBold, SELLER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).setLocation(left, 80f).setWidth(250f).drawOn(page);

        TextLine title = new TextLine(semiBold, "Statement");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(BLUE);
        title.setLocation(right - title.getWidth(), 72f);
        title.drawOn(page);

        new TextBlock(regular, "Date: " + DATE + "\nPeriod: " + PERIOD_START + " to " + DATE)
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 250f, 94f)
                .setWidth(250f)
                .drawOn(page);

        // A thin rule under the header, decoration only, so an artifact
        Line rule = new Line(left, 148f, right, 148f);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The customer
        new TextLine(semiBold, "Account").setStructureType(StructElem.H2).setFontSize(12f)
                .setLocation(left, 182f).drawOn(page);
        float y = new TextBlock(regular, CUSTOMER).setLocation(left, 192f).setWidth(250f)
                .drawOn(page)[1] + 24f;

        // The lines, each with the balance after it. The first line is the
        // balance brought forward, so that the column adds up from the top.
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Date", "Description", "Charges", "Credits", "Balance"));
        BigDecimal balance = BALANCE_FORWARD;
        rows.add(row(regular, PERIOD_START.toString(), "Balance forward", "", "", money(balance)));
        for (Entry entry : ENTRIES) {
            balance = balance.add(entry.charge).subtract(entry.credit);
            rows.add(row(regular, entry.date.toString(), entry.description,
                    blankIfZero(entry.charge), blankIfZero(entry.credit), money(balance)));
        }

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(right - left);
        table.setColumnWidthsInPercent(16f, 36f, 16f, 16f, 16f);
        for (int column = 2; column < 5; column++) {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.setLocation(left, MARGIN);
        table.setFirstPageTopMargin(y);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The balance due and when to pay it, under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 30f;
        if (y + 70f > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        TextLine due = new TextLine(semiBold, "Balance due: " + money(balance) + " " + CURRENCY);
        due.setFontSize(14f);
        due.setLocation(right - due.getWidth(), y);
        due.drawOn(page);

        new TextBlock(regular, "Please pay the balance by " + DATE.plusDays(PAYMENT_DAYS)
                + ", by bank transfer. If a payment has crossed with this statement, thank you, "
                + "and please disregard it.")
                .setLineSpacing(1.15f)
                .setLocation(left, y + 22f)
                .setWidth(right - left)
                .drawOn(page);

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

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    // A line has a charge or a credit, and the other column is left empty
    // rather than 0.00, so that the eye finds the amounts.
    private static String blankIfZero(BigDecimal amount) {
        return amount.signum() == 0 ? "" : money(amount);
    }

    // The amount with two decimals and commas between the thousands, as
    // 2,495.61, whatever the locale of the computer.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_56();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_56 => %4d ms%n", time1 - time0);
    }
}   // End of Example_56.java
