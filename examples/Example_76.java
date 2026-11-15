/*
 * Example_76.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * An order form of a small shop, to print and fill in by hand: who orders,
 * a table of the items with their prices and empty cells for the size, the
 * quantity and the amount, lines for the subtotal, the sales tax and the
 * total, where to deliver, and a box to sign in.
 * <p>
 * This is a form people fill in, not a document with data. The shop prints
 * what it knows, the items and their prices, from BigDecimal amounts written
 * with Locale.US, and leaves the rest empty. The rows of the table are about
 * as tall as a box, to write a number in by hand; the subtotal, the tax and
 * the total are rows of the same table whose label spans the first four
 * columns, so that their empty amounts line up under those of the items. The
 * other fields are drawn with the field helper of Example_72.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the sections are headings, the
 * items a table that a screen reader reads by its column headers, and the
 * boxes are artifacts. The layout is that of the order form template of
 * PDFjet Forms. The shop is made up.
 * </p>
 *
 * @see Table
 * @see CheckBox
 * @see Rect
 */
public class Example_76 {
    // An item of the price list: its name and its price, in dollars
    static final class Item {
        final String name;
        final BigDecimal price;

        Item(String name, String price) {
            this.name = name;
            this.price = new BigDecimal(price);
        }
    }

    static final String SHOP = "Tidewater Print Co.";
    static final String RETURN_TO = "Mail the form to Tidewater Print Co., 210 Front Street, "
            + "Beaufort, NC 28516, or scan it to orders@example.com.";
    static final String TAX = "Sales tax 8%";

    static final Item[] ITEMS = {
        new Item("T-shirt, cotton, printed on the front", "20.00"),
        new Item("Hoodie, printed on the back", "45.00"),
        new Item("Cap, embroidered", "15.00"),
        new Item("Tote bag, canvas", "12.00"),
    };

    static final int BLUE = 0x1f58b0;
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;
    static final int HEADER = 0xe3edf9;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static final float LEFT = 48f;
    static final float WIDTH = 516f;
    static final float GAP = 12f;
    static final float HALF = (WIDTH - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    private final Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_76() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_76.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Order form");
        pdf.setAuthor(SHOP);
        pdf.setLanguage("en-US");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what happens to the order, and a rule
        heading("Order form", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Write how many of each item you want, and "
                + "the amounts. Sign the form and send it to us: we confirm every order by "
                + "email within one working day.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // Your details
        y = heading("Your details", StructElem.H2, 14f, y + 46f);
        field("Name *", LEFT, y, HALF, 1, null);
        y = field("Email *", LEFT + HALF + GAP, y, HALF, 1, null);
        field("Company", LEFT, y, HALF, 1, null);
        y = field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The order: the items with their prices, and empty cells to write in
        y = heading("Your order", StructElem.H2, 14f, y + 18f);
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Item", "Price", "Size, color", "Qty", "Amount"));
        for (Item item : ITEMS) {
            rows.add(row(textFont, item.name, money(item.price), "", "", ""));
        }
        rows.add(totalRow(textFont, "Subtotal"));
        rows.add(totalRow(textFont, TAX));
        rows.add(totalRow(semiBold, "Total"));

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        // Rows about as tall as a box, to write in
        table.setCellPadding(5f, 6f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(BOX);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(42f, 13f, 20f, 10f, 15f);
        table.setTextAlignmentInColumn(1, Alignment.RIGHT);
        table.setLocation(LEFT, y);
        y = table.drawOn(page)[1] + 12f;

        // Where to deliver: the address beside the date
        y = heading("Delivery", StructElem.H2, 14f, y + 18f);
        float wide = 0.6f * WIDTH - GAP / 2f;
        field("Delivery date", LEFT + wide + GAP, y, WIDTH - wide - GAP, 1, null);
        y = field("Delivery address *", LEFT, y, wide, 3, null);

        // The confirmation, and the signature under it
        new CheckBox(textFont, "I confirm this order and sign below. *")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        y += 30f;
        field("Signature *", LEFT, y, wide, 1, null);
        field("Date", LEFT + wide + GAP, y, WIDTH - wide - GAP, 1, null);

        // Where the form goes, at the foot of the page
        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.setFontSize(9f);
        returnTo.setTextColor(Color.dimgray);
        returnTo.setLocation(LEFT, page.getHeight() - 36f);
        returnTo.drawOn(page);

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

    // A row of the totals: the label across the first four columns, on the
    // right, and an empty amount. The cells under the span stay in the row.
    private static List<Cell> totalRow(Font font, String label) {
        List<Cell> row = row(font, label, "", "", "", "");
        row.get(0).setColSpan(4);
        row.get(0).setTextAlignment(Alignment.RIGHT);
        return row;
    }

    // The price with two decimals, as 45.00. The locale is given, or a
    // computer set to French writes 45,00.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    private float heading(String text, StructElem level, float size, float y) throws Exception {
        TextLine heading = new TextLine(labelFont, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(BLUE);
        heading.setLocation(LEFT, y);
        heading.drawOn(page);
        return y + 16f;
    }

    // Draws the label of a field, its top at y.
    private void label(String text, float x, float y) throws Exception {
        TextLine label = new TextLine(labelFont, text);
        label.setLocation(x, y + labelFont.getAscent());
        label.drawOn(page);
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    private float field(String text, float x, float y, float width, int lines, String value)
            throws Exception {
        label(text, x, y);
        float top = y + 16f;
        float height = BOX_HEIGHT + (lines - 1) * LINE_HEIGHT;
        Rect box = new Rect(x, top, width, height);
        box.setBorderColor(BOX);
        box.setBorderWidth(0.5f);
        box.drawOn(page);
        if (value != null) {
            TextLine line = new TextLine(textFont, value);
            line.setTextColor(BLUE);
            line.setLocation(x + 6f, top + 16f);
            line.drawOn(page);
        }
        return top + height + 12f;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private void rule(float y) throws Exception {
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.setStrokeColor(RULE);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_76();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_76 => %4d ms%n", time1 - time0);
    }
}   // End of Example_76.java
