/*
 * Example_58.java
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
 * A purchase order of a small design studio to its supplier, on Letter
 * paper: the studio and the order number at the top with the date to deliver
 * by, the vendor and where to ship, a table of the items with the vendor's
 * codes, the subtotal, the sales tax and the total, the terms, and who
 * approved the order.
 * <p>
 * Here the studio is the buyer: the layout of its invoice of Example_53,
 * the parties the other way around. A purchase order is a finished document
 * that the vendor receives, not a form. The amounts are BigDecimal, computed
 * from the items, and the table goes on to as many pages as it needs.
 * </p>
 * <p>
 * The vendor's invoice is checked against the purchase order, and both are
 * kept for the books, so the document is PDF/A-3a and PDF/UA-1 at once. The
 * layout is that of the purchase order template of PDFjet Forms. The
 * companies, the people and the numbers are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_58 {
    // A line of the order: the vendor's code of the item, so that the vendor
    // picks the right one, and the price the vendor quoted.
    static final class Item {
        final String description;
        final String code;
        final int quantity;
        final BigDecimal unitPrice;

        Item(String description, String code, int quantity, String unitPrice) {
            this.description = description;
            this.code = code;
            this.quantity = quantity;
            this.unitPrice = new BigDecimal(unitPrice);
        }

        BigDecimal amount() {
            return unitPrice.multiply(BigDecimal.valueOf(quantity));
        }
    }

    // The data of the order. In an application it comes from the database.
    static final String NUMBER = "2026-017";
    static final LocalDate DATE = LocalDate.of(2026, 10, 5);
    static final int DELIVERY_DAYS = 15;
    static final int PAYMENT_DAYS = 30;
    static final String CURRENCY = "CAD";
    static final String TAX_NAME = "HST";
    static final BigDecimal TAX_RATE = new BigDecimal("0.13");
    static final String APPROVED_BY = "Dana Whitfield, Design Lead";

    static final String BUYER = "Northwind Studio";
    static final String BUYER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0";
    static final String VENDOR = "Harbour Paper Supply\n88 Dock Road\nOwen Sound, ON N4K 3T2";

    static final Item[] ITEMS = {
        new Item("Recycled paper, letter, 500 sheets", "PP-2201", 20, "8.50"),
        new Item("Kraft envelopes, 9 x 12 in", "EN-0912", 200, "0.35"),
        new Item("Shipping boxes, medium", "BX-0400", 50, "1.20"),
        new Item("Packing tape, 48 mm x 50 m", "TP-4850", 12, "3.95"),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_58() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_58.pdf")));
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.setTitle("Purchase order " + NUMBER);
        pdf.setAuthor(BUYER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The totals, from the items, the tax rounded once on the subtotal
        BigDecimal subtotal = BigDecimal.ZERO;
        for (Item item : ITEMS) {
            subtotal = subtotal.add(item.amount());
        }
        BigDecimal tax = subtotal.multiply(TAX_RATE).setScale(2, RoundingMode.HALF_UP);
        BigDecimal total = subtotal.add(tax);

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;
        float middle = (left + right) / 2f;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, BUYER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        new TextBlock(regular, BUYER_ADDRESS + "\nhello@example.com")
                .setLocation(left, 80f).setWidth(250f).drawOn(page);

        TextLine title = new TextLine(semiBold, "Purchase order");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(BLUE);
        title.setLocation(right - title.getWidth(), 72f);
        title.drawOn(page);

        new TextBlock(regular, "PO No. " + NUMBER + "\nDate: " + DATE
                + "\nDeliver by: " + DATE.plusDays(DELIVERY_DAYS))
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

        // The vendor, and where to ship: side by side, the taller of the two
        // decides where the table starts
        heading(page, semiBold, "Vendor", left, 182f);
        float y1 = new TextBlock(regular, VENDOR).setLocation(left, 192f).setWidth(middle - left)
                .drawOn(page)[1];
        heading(page, semiBold, "Ship to", middle, 182f);
        float y2 = new TextBlock(regular, BUYER + "\n" + BUYER_ADDRESS).setLocation(middle, 192f)
                .setWidth(right - middle).drawOn(page)[1];
        float y = Math.max(y1, y2) + 24f;

        // The items: a header row, repeated on every page
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Item", "Code", "Quantity", "Price", "Amount"));
        for (Item item : ITEMS) {
            rows.add(row(regular, item.description, item.code, String.valueOf(item.quantity),
                    money(item.unitPrice), money(item.amount())));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BORDER);
        table.setWidth(right - left);
        table.setColumnWidthsInPercent(45f, 15f, 12f, 13f, 15f);
        for (int column = 2; column < 5; column++) {
            table.setTextAlignmentInColumn(column, Alignment.RIGHT);
        }
        table.setLocation(left, MARGIN);
        table.setFirstPageTopMargin(y);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The totals, the terms and the approval under the last row, or on a
        // page of their own when they no longer fit there
        y = end[1] + 22f;
        if (y + 130f > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        String rate = TAX_RATE.movePointRight(2).stripTrailingZeros().toPlainString();
        drawRight(page, regular, 11f, "Subtotal: " + money(subtotal), right, y);
        drawRight(page, regular, 11f, TAX_NAME + " " + rate + "%: " + money(tax), right, y + 15f);
        drawRight(page, semiBold, 14f, "Total: " + money(total) + " " + CURRENCY, right, y + 43f);

        y = new TextBlock(regular, "Please quote the PO number on your invoice and packing slip. "
                + "Payment within " + PAYMENT_DAYS + " days of delivery.")
                .setLineSpacing(1.15f)
                .setLocation(left, y + 64f)
                .setWidth(right - left)
                .drawOn(page)[1];
        new TextLine(regular, "Approved by: " + APPROVED_BY)
                .setLocation(left, y + 28f).drawOn(page);

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
        new Example_58();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_58 => %4d ms%n", time1 - time0);
    }
}   // End of Example_58.java
