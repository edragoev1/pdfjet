/*
 * Example_67.java
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
 * A credit note of a small design studio to a customer, on Letter paper: the
 * studio and the credit note number at the top, the customer, the invoice
 * that the credit note corrects and why, a table of what is credited, the
 * subtotal, the sales tax and the total credit, and what happens to it.
 * <p>
 * A credit note is a finished document that the customer receives, not a
 * form. It takes back part of an invoice, here part of the invoice of
 * Example_53, and refers to it by its number and its date, as the tax rules
 * ask, rather than changing an invoice that was already sent. The amounts are
 * positive: the title and the words "Total credit" say which way they go. The
 * sales tax is credited too, at the rate of the invoice, and computed from the
 * items the same way.
 * </p>
 * <p>
 * Both sides keep a credit note with the invoice, so the document is PDF/A-3a
 * and PDF/UA-1 at once. The layout is that of the credit note template of
 * PDFjet Forms. The companies and the numbers are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_67 {
    // A line of the credit note. The unit price is a BigDecimal, in dollars.
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

    // The data of the credit note. In an application it comes from the database.
    static final String NUMBER = "CN-2026-007";
    static final LocalDate DATE = LocalDate.of(2026, 10, 5);
    static final String INVOICE = "2026-042";
    static final LocalDate INVOICE_DATE = LocalDate.of(2026, 9, 25);
    static final String REASON = "Ten printed copies were damaged in delivery and returned.";
    static final String CURRENCY = "CAD";
    static final String TAX_NAME = "HST";
    static final BigDecimal TAX_RATE = new BigDecimal("0.13");

    static final String SELLER = "Northwind Studio";
    static final String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";
    static final String CUSTOMER =
            "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4";

    static final Item[] ITEMS = {
        new Item("Printed copies returned, damaged in delivery", 10, "6.00"),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_67() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_67.pdf")));
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.setTitle("Credit note " + NUMBER);
        pdf.setAuthor(SELLER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The credit, from the items, the tax rounded once on the subtotal
        BigDecimal subtotal = BigDecimal.ZERO;
        for (Item item : ITEMS) {
            subtotal = subtotal.add(item.amount());
        }
        BigDecimal tax = subtotal.multiply(TAX_RATE).setScale(2, RoundingMode.HALF_UP);
        BigDecimal credit = subtotal.add(tax);

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;
        float middle = (left + right) / 2f;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).setLocation(left, 80f).setWidth(250f).drawOn(page);

        TextLine title = new TextLine(semiBold, "Credit note");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(BLUE);
        title.setLocation(right - title.getWidth(), 72f);
        title.drawOn(page);

        new TextBlock(regular, "No. " + NUMBER + "\nDate: " + DATE + "\nFor invoice " + INVOICE)
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200f, 94f)
                .setWidth(200f)
                .drawOn(page);

        // A thin rule under the header, decoration only, so an artifact
        Line rule = new Line(left, 148f, right, 148f);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The customer, and the invoice corrected and why: side by side, the
        // taller of the two decides where the table starts
        heading(page, semiBold, "Credit to", left, 182f);
        float y1 = new TextBlock(regular, CUSTOMER).setLocation(left, 192f).setWidth(middle - left)
                .drawOn(page)[1];
        heading(page, semiBold, "Invoice " + INVOICE + " of " + INVOICE_DATE, middle, 182f);
        float y2 = new TextBlock(regular, REASON)
                .setLocation(middle, 192f).setWidth(right - middle).drawOn(page)[1];
        float y = Math.max(y1, y2) + 24f;

        // What is credited: a header row, repeated on every page
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
        table.setLocation(left, MARGIN);
        table.setFirstPageTopMargin(y);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The amounts and what happens to them under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 22f;
        if (y + 90f > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        String rate = TAX_RATE.movePointRight(2).stripTrailingZeros().toPlainString();
        drawRight(page, regular, 11f, "Subtotal: " + money(subtotal), right, y);
        drawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + money(tax), right, y + 15f);
        drawRight(page, semiBold, 14f, "Total credit: " + money(credit) + " " + CURRENCY,
                right, y + 43f);

        new TextBlock(regular, "This amount is taken off your next invoice, or refunded to you "
                + "on request. We are sorry for the damaged copies.")
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

    // Draws the heading of a part, its baseline at y.
    private static void heading(Page page, Font font, String text, float x, float y)
            throws Exception {
        new TextLine(font, text).setStructureType(StructElem.H2).setFontSize(12f)
                .setLocation(x, y).drawOn(page);
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
    // 1,200.00, whatever the locale of the computer.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_67();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_67 => %4d ms%n", time1 - time0);
    }
}   // End of Example_67.java
