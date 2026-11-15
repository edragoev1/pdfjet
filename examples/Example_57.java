/*
 * Example_57.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A delivery note that goes in the box with the goods, on Letter paper: the
 * studio and the delivery note number at the top, where the goods go and how
 * they travel, a table of what was ordered and what is in this delivery, a
 * note on checking the goods, and a line for the receiver to sign.
 * <p>
 * A delivery note is a finished document that the customer receives, not a
 * form; only its last line is filled in, by hand, at the door. It has no
 * prices, as it is read by whoever opens the box. The table says for each
 * item how many were ordered and how many are in the box, and an item that
 * is short says how many are to follow, computed from the two numbers; the
 * weight of the shipment is computed from the items too. The table
 * goes on to as many pages as it needs, with its header row on each.
 * </p>
 * <p>
 * The goods are those of the invoice of Example_53. The document is
 * PDF/UA-1: the title is a heading, the items a table, and the rule and the
 * line to sign on are artifacts. The layout is that of the delivery note
 * template of PDFjet Forms. The companies and the numbers are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_57 {
    // A line of the delivery: how many were ordered, how many are in this
    // delivery, and the weight of one, in kilograms.
    static final class Item {
        final String description;
        final int ordered;
        final int delivered;
        final double kgEach;

        Item(String description, int ordered, int delivered, double kgEach) {
            this.description = description;
            this.ordered = ordered;
            this.delivered = delivered;
            this.kgEach = kgEach;
        }
    }

    // The data of the delivery. In an application it comes from the database.
    static final String NUMBER = "D-2026-064";
    static final LocalDate DATE = LocalDate.of(2026, 9, 24);
    static final String ORDER = "CB-PO-3310";
    static final int BOXES = 2;
    static final String CARRIER = "Harbourline Courier, tracking HC 4471 2290";
    static final int CHECK_DAYS = 5;

    static final String SELLER = "Northwind Studio";
    static final String SELLER_ADDRESS =
            "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";
    static final String DELIVER_TO =
            "Contoso Books\nAttn: Receiving\n400 King Street West\nToronto, ON M5V 1K4";

    static final Item[] ITEMS = {
        new Item("Annual report, printed copy, saddle stitched", 50, 50, 0.26),
        new Item("Accessible PDF/UA edition, USB key", 2, 2, 0.02),
        new Item("Display stand for the lobby", 1, 0, 3.8),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int GRAY = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_57() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_57.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Delivery note " + NUMBER);
        pdf.setAuthor(SELLER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // The weight of the shipment, from the items in it, for the carrier
        double kg = 0.0;
        for (Item item : ITEMS) {
            kg += item.delivered * item.kgEach;
        }

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;
        float middle = (left + right) / 2f;

        // The header: the studio on the left, the title and the numbers on the right
        new TextLine(semiBold, SELLER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        new TextBlock(regular, SELLER_ADDRESS).setLocation(left, 80f).setWidth(250f).drawOn(page);

        TextLine title = new TextLine(semiBold, "Delivery note");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(BLUE);
        title.setLocation(right - title.getWidth(), 72f);
        title.drawOn(page);

        new TextBlock(regular, "No. " + NUMBER + "\nDate: " + DATE + "\nYour order: " + ORDER)
                .setTextAlignment(Alignment.RIGHT)
                .setLocation(right - 200f, 94f)
                .setWidth(200f)
                .drawOn(page);

        line(page, left, right, 148f, RULE, 1f);

        // Where the goods go, and how they travel: side by side, the taller
        // of the two decides where the table starts
        heading(page, semiBold, "Deliver to", left, 182f);
        float y1 = new TextBlock(regular, DELIVER_TO).setLocation(left, 192f)
                .setWidth(middle - left).drawOn(page)[1];
        heading(page, semiBold, "Shipment", middle, 182f);
        String shipment = String.format(Locale.US, "%d boxes, %.1f kg", BOXES, kg) + "\n" + CARRIER;
        float y2 = new TextBlock(regular, shipment).setLocation(middle, 192f)
                .setWidth(right - middle).drawOn(page)[1];
        float y = Math.max(y1, y2) + 24f;

        // The items: what was ordered, and what is in the box
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Item", "Ordered", "Delivered"));
        for (Item item : ITEMS) {
            String delivered = String.valueOf(item.delivered);
            if (item.delivered < item.ordered) {
                delivered += " (" + (item.ordered - item.delivered) + " to follow)";
            }
            rows.add(row(regular, item.description, String.valueOf(item.ordered), delivered));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(GRAY);
        table.setWidth(right - left);
        table.setColumnWidthsInPercent(60f, 15f, 25f);
        table.setTextAlignmentInColumn(1, Alignment.RIGHT);
        table.setTextAlignmentInColumn(2, Alignment.RIGHT);
        table.setLocation(left, MARGIN);
        table.setFirstPageTopMargin(y);
        table.setBottomMargin(FOOTER_ROOM);
        float[] end = table.drawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages.get(pages.size() - 1);

        // The note and the line to sign stay together under the last row, or
        // go on a page of their own when they no longer fit there
        y = end[1] + 22f;
        if (y + 110f > page.getHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = 64f;
        }
        y = new TextBlock(regular, "Please check the goods on arrival and tell us within "
                + CHECK_DAYS + " days if anything is missing or damaged.")
                .setLineSpacing(1.15f)
                .setLocation(left, y)
                .setWidth(right - left)
                .drawOn(page)[1];

        // Room to sign by hand above the line
        new TextLine(regular, "Received in good order by (name, signature and date):")
                .setLocation(left, y + 30f).drawOn(page);
        line(page, left, left + 0.6f * (right - left), y + 74f, GRAY, 0.75f);

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

    // A line across, the rule under the header or the line to sign on, which
    // the text above it names: an artifact that a screen reader skips.
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

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_57();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_57 => %4d ms%n", time1 - time0);
    }
}   // End of Example_57.java
