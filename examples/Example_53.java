/*
 * Example_53.java
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
 * An invoice of a small design studio to a customer, on Letter paper: the
 * seller and the invoice number at the top, the customer, a table of the
 * items, the subtotal, the sales tax and the total, the payment terms, and
 * "Page 1 of 1" at the bottom.
 * <p>
 * The amounts are BigDecimal, never float or double, so that they add up to
 * the cent, and the totals are computed from the items rather than typed in.
 * The table of the items goes on to as many pages as it needs, with its
 * header row on each, so the same code prints an invoice of three items or of
 * three hundred; the totals go under the last row.
 * </p>
 * <p>
 * An invoice is kept for years by both sides, so the document is archival as
 * well as accessible, PDF/A-3a and PDF/UA-1 at once: the title is a heading,
 * the items a table that a screen reader reads by its column headers. The
 * layout is that of the invoice template of PDFjet Forms. The companies and
 * the numbers are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_53 {
    // A line of the invoice. The unit price is a BigDecimal, in dollars.
    static final class Item {
        final String description;
        final int quantity;
        final BigDecimal unitPrice;

        Item(String description, int quantity, String unitPrice) {
            this.description = description;
            this.quantity = quantity;
            this.unitPrice = new BigDecimal(unitPrice);
        }

        BigDecimal amount() {
            return unitPrice.multiply(BigDecimal.valueOf(quantity));
        }
    }

    // The data of the invoice. In an application it comes from the database.
    static final String NUMBER = "2026-042";
    static final LocalDate DATE = LocalDate.of(2026, 9, 25);
    static final int PAYMENT_DAYS = 30;
    static final String CURRENCY = "CAD";
    static final String TAX_NAME = "HST";               // Ontario's sales tax
    static final BigDecimal TAX_RATE = new BigDecimal("0.13");

    static final String SELLER = "Northwind Studio";
    static final String SELLER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    static final String CUSTOMER = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4";

    static final Item[] ITEMS = {
        new Item("Layout of the annual report, 48 pages", 1, "1200.00"),
        new Item("Accessible PDF/UA edition", 1, "400.00"),
        new Item("Charts redrawn from the spreadsheets", 6, "45.00"),
        new Item("Printed copies, saddle stitched", 50, "6.00"),
        new Item("Courier to Toronto", 1, "38.50"),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_53() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_53.pdf")));
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.setTitle("Invoice " + NUMBER);
        pdf.setAuthor(SELLER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The totals, from the items. The tax is rounded to the cent once, on
        // the subtotal, as a cash register does.
        BigDecimal subtotal = BigDecimal.ZERO;
        for (Item item : ITEMS) {
            subtotal = subtotal.add(item.amount());
        }
        BigDecimal tax = subtotal.multiply(TAX_RATE).setScale(2, RoundingMode.HALF_UP);
        BigDecimal total = subtotal.add(tax);

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;

        // The header: the seller on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).setLocation(left, 80f).setWidth(250f).drawOn(page);

        TextLine title = new TextLine(semiBold, "Invoice");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(BLUE);
        title.setLocation(right - title.getWidth(), 72f);
        title.drawOn(page);

        String numbers = "No. " + NUMBER
                + "\nDate: " + DATE
                + "\nDue: " + DATE.plusDays(PAYMENT_DAYS);
        new TextBlock(regular, numbers)
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200f, 94f)
                .setWidth(200f)
                .drawOn(page);

        // A thin rule under the header, decoration only, so an artifact that a
        // screen reader skips
        Line rule = new Line(left, 148f, right, 148f);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The customer
        TextLine billTo = new TextLine(semiBold, "Bill to");
        billTo.setStructureType(StructElem.H2);
        billTo.setFontSize(12f);
        billTo.setLocation(left, 182f);
        billTo.drawOn(page);
        new TextBlock(regular, CUSTOMER).setLocation(left, 192f).setWidth(250f).drawOn(page);

        // The items: a header row, repeated on every page the table goes on to
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Description", "Quantity", "Price", "Amount"));
        for (Item item : ITEMS) {
            rows.add(row(regular, item.description, String.valueOf(item.quantity),
                    money(item.unitPrice), money(item.amount())));
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
        // Under the customer on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.setLocation(left, MARGIN);
        table.setFirstPageTopMargin(260f);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The totals, under the last row. When they no longer fit on its page,
        // they go on a page of their own.
        float y = end[1] + 22f;
        if (y + 90f > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        String rate = TAX_RATE.movePointRight(2).stripTrailingZeros().toPlainString();
        drawRight(page, regular, 11f, "Subtotal: " + money(subtotal), right, y);
        drawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + money(tax), right, y + 15f);
        drawRight(page, semiBold, 14f, "Total: " + money(total) + " " + CURRENCY, right, y + 43f);

        new TextBlock(regular, "Payment by bank transfer within " + PAYMENT_DAYS
                + " days. Thank you for your business.")
                .setLocation(left, y + 64f)
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
    // 1,200.00. The locale is given, or a computer set to French writes
    // 1 200,00.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_53();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_53 => %4d ms%n", time1 - time0);
    }
}   // End of Example_53.java
