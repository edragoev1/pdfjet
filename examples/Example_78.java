/*
 * Example_78.java
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
 * A pro forma invoice of a small studio to a customer abroad, on Letter
 * paper: the seller, the number and the date until which the prices hold,
 * the address to bill and the address to ship to, a table of the goods, the
 * country of origin and the weight for customs, the total, and the terms.
 * <p>
 * A pro forma invoice is a finished document that the customer receives, not
 * a form. It is sent before the sale: the customer pays it, or takes it to
 * the bank or to customs, and the goods are shipped with the real invoice
 * once the money is in. So it says what it is not, a tax invoice, and how
 * long its prices are good for. Goods exported from Canada carry no sales
 * tax, so the total is the sum of the lines.
 * </p>
 * <p>
 * The amounts are BigDecimal, never float or double, and the total is
 * computed from the goods. The table of the goods goes on to as many pages
 * as it needs, with its header row on each; the total and the terms go
 * under the last row, and each page says "Page 1 of 1" at the bottom.
 * </p>
 * <p>
 * Both sides keep the document, so it is archival as well as accessible,
 * PDF/A-3a and PDF/UA-1 at once. The layout is that of the pro forma
 * invoice template of PDFjet Forms. The companies and the numbers are made
 * up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_78 {
    // A line of the pro forma invoice. The unit price is a BigDecimal, in dollars.
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

    // The data of the pro forma invoice. In an application it comes from the database.
    static final String NUMBER = "PF-2026-012";
    static final LocalDate DATE = LocalDate.of(2026, 11, 2);
    static final int VALID_DAYS = 30;
    static final String CURRENCY = "CAD";

    static final String SELLER = "Northwind Studio";
    static final String SELLER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001";
    static final String BILL_TO = "Fabrikam Press\n18 Rue des Lilas\n75011 Paris, France";
    static final String SHIP_TO =
            "Fabrikam Press, warehouse\n4 Avenue de l'Europe\n94200 Ivry-sur-Seine, France";
    static final String CUSTOMS = "Country of origin: Canada\nWeight: 18 kg, in 2 parcels";

    static final Item[] ITEMS = {
        new Item("Art prints, A3, numbered edition", 40, "35.00"),
        new Item("Archival portfolio boxes", 4, "45.00"),
        new Item("Shipping, insured", 1, "120.00"),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_78() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_78.pdf")));
        pdf.setCompliance(Compliance.PDF_A_3A_UA_1);
        pdf.setTitle("Pro forma invoice " + NUMBER);
        pdf.setAuthor(SELLER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The total, from the goods. Exported, they carry no sales tax.
        BigDecimal total = BigDecimal.ZERO;
        for (Item item : ITEMS) {
            total = total.add(item.amount());
        }

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;
        float half = (right - left) / 2f;

        // The header: the seller on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).setLocation(left, 80f).setWidth(250f).drawOn(page);

        TextLine title = new TextLine(semiBold, "Pro forma invoice");
        title.setStructureType(StructElem.H1);
        title.setFontSize(22f);
        title.setTextColor(BLUE);
        title.setLocation(right - title.getWidth(), 70f);
        title.drawOn(page);

        String numbers = "No. " + NUMBER
                + "\nDate: " + DATE
                + "\nValid until: " + DATE.plusDays(VALID_DAYS);
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

        // Who pays, on the left, and where the goods go, on the right
        address(page, semiBold, regular, "Bill to", BILL_TO, left, half);
        address(page, semiBold, regular, "Ship to", SHIP_TO, left + half, half);

        // The goods: a header row, repeated on every page the table goes on to
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
        // Under the addresses on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.setLocation(left, MARGIN);
        table.setFirstPageTopMargin(260f);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // What customs asks for on the left, the total on the right, and the
        // terms under them, all under the last row. When they no longer fit
        // on its page, they go on a page of their own.
        float y = end[1] + 22f;
        if (y + 110f > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        TextLine totalLine = new TextLine(semiBold, "Total: " + money(total) + " " + CURRENCY);
        totalLine.setFontSize(14f);
        totalLine.setLocation(right - totalLine.getWidth(), y + 14f);
        totalLine.drawOn(page);
        y = new TextBlock(regular, CUSTOMS).setLocation(left, y).setWidth(half).drawOn(page)[1];

        new TextBlock(regular, "Not a tax invoice: the goods are shipped when this amount is received, "
                + "by bank transfer, and the invoice is sent with them. Prices are valid until "
                + DATE.plusDays(VALID_DAYS) + ".")
                .setLocation(left, y + 16f)
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

    // Draws an address under its heading, in a column at x of the width,
    // under the rule of the header.
    private static void address(Page page, Font headingFont, Font font, String heading, String text,
            float x, float width) throws Exception {
        TextLine line = new TextLine(headingFont, heading);
        line.setStructureType(StructElem.H2);
        line.setFontSize(12f);
        line.setLocation(x, 182f);
        line.drawOn(page);
        new TextBlock(font, text).setLocation(x, 192f).setWidth(width - 12f).drawOn(page);
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
    // 1,700.00. The locale is given, or a computer set to French writes
    // 1 700,00.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_78();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_78 => %4d ms%n", time1 - time0);
    }
}   // End of Example_78.java
