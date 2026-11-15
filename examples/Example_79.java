/*
 * Example_79.java
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
import com.pdfjet.qrcode.*;

/**
 * A product catalog of two pages, on Letter paper: the logo and the title, a
 * grid of products with a picture, a name, a description, the code and the
 * price of each, the colors the products come in, the price list, how to
 * order, and a QR code of the shop online.
 * <p>
 * The products are one list, and both the grid and the price list are made
 * from it, so a price is changed in one place; the products without a
 * picture are in the price list only. The grid places three products to a
 * row, each in a column of the same width, and starts the next row under
 * the tallest product of the row, as a longer description pushes it down.
 * The codes and the prices are in Cascadia Mono, a monospace font, so that
 * the characters of the codes line up from row to row of the price list.
 * </p>
 * <p>
 * The pictures here are small drawings, SVG files in
 * resources/images/catalog that SVGImage draws as vectors, sharp at any
 * size, scaled to the width of a column. A real catalog has photos: new
 * Image(pdf, stream, ImageType.JPG) in place of SVGImage, with the same
 * setAltDescription. The logo is an SVG, as logos usually are.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title, the sections and the products are
 * headings of three levels, every picture and the QR code is a figure that
 * says what it shows, the price list is a table, and the running header of
 * each page and the color swatches are artifacts. The layout is that of the
 * product catalog template of PDFjet Forms. The studio and its products are
 * made up.
 * </p>
 *
 * @see SVGImage
 * @see Table
 * @see QRCode
 */
public class Example_79 {
    // A product: its code, name, description, the price in dollars, the
    // number of colors it comes in, and a picture, null for none.
    static final class Product {
        final String code;
        final String name;
        final String description;
        final BigDecimal price;
        final int colors;
        final String picture;
        final String alt;

        Product(String code, String name, String description, String price, int colors,
                String picture, String alt) {
            this.code = code;
            this.name = name;
            this.description = description;
            this.price = new BigDecimal(price);
            this.colors = colors;
            this.picture = picture;
            this.alt = alt;
        }
    }

    static final String STUDIO = "Northwind Studio";
    static final String TITLE = "Catalog 2026";
    static final String ORDER_URL = "https://example.com/order";

    // The pictures and the logo are SVG files, drawn as vectors, sharp at any size
    static final String IMAGES = "resources/images/catalog/";

    static final Product[] PRODUCTS = {
        new Product("NB-A5-L", "Lined notebook, A5", "160 pages of lined paper, 100 g/m², "
                + "a navy linen cover.", "24.00", 4, "notebook-lined.svg", "A navy notebook with a cream label"),
        new Product("NB-A5-D", "Dot grid notebook, A5", "160 pages with a 5 mm dot grid, "
                + "for notes and sketches.", "24.00", 4, "notebook-dots.svg", "An open notebook of dotted pages"),
        new Product("MG-350-B", "Stoneware mug", "350 ml, thrown by hand; dishwasher safe.",
                "32.00", 4, "mug.svg", "A brown stoneware mug"),
        new Product("NB-A4-S", "Sketchbook, A4", "80 sheets of 160 g/m² paper for pencil, "
                + "ink and washes.", "36.00", 2, "sketchbook.svg",
                "A sketchbook with a kraft cover and a spiral binding"),
        new Product("MG-250-E", "Espresso cups, two", "90 ml each, in a teal glaze, with their "
                + "saucers.", "38.00", 3, "espresso-cups.svg", "Two small teal cups on saucers"),
        new Product("BW-180-S", "Serving bowl, 18 cm", "Wide and shallow, for salads and fruit; "
                + "oven safe.", "58.00", 2, "serving-bowl.svg", "A wide sand-colored bowl with a teal rim"),
        new Product("NB-A6-P", "Pocket notebook, A6, three", "", "28.00", 4, null, null),
        new Product("GC-050", "Gift card", "", "50.00", 0, null, null),
    };

    // The colors the covers and the glazes come in, as on the swatches
    static final String[] COLOR_NAMES = {"Navy", "Teal", "Sand", "Clay"};
    static final int[] COLORS = {0x1d3557, 0x0c727d, 0xf4eee4, 0x6e4a2e};

    static final int NAVY = 0x1d3557;
    static final int INK = 0x333333;
    static final int SAND = 0xf4eee4;
    static final int CLAY = 0x6e4a2e;
    static final int RULE = 0xdde1e8;

    static final float LEFT = 40f;
    static final float WIDTH = 532f;            // Letter, 612 wide, less the margins
    static final float GAP = 12f;
    static final int PER_ROW = 3;
    static final float COLUMN = (WIDTH - (PER_ROW - 1) * GAP) / PER_ROW;

    private Font sans;
    private Font sansBold;
    private Font serif;
    private Font serifItalic;
    private Font mono;

    public Example_79() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_79.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle(TITLE);
        pdf.setAuthor(STUDIO);
        pdf.setLanguage("en-CA");

        sans = new Font(pdf, IBMPlexSans.Regular);
        sans.setSize(10f);
        sansBold = new Font(pdf, IBMPlexSans.SemiBold);
        sansBold.setSize(10f);                  // The headings set their own size
        serif = new Font(pdf, IBMPlexSerif.Regular);
        serif.setSize(11f);
        serifItalic = new Font(pdf, IBMPlexSerif.Italic);
        serifItalic.setSize(13f);
        mono = new Font(pdf, CascadiaMono.Regular);
        mono.setSize(10f);

        List<Page> pages = new ArrayList<Page>();

        // Page 1: the logo, the title and a line about the studio, then the
        // products with a picture, three to a row
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        SVGImage logo = new SVGImage(IMAGES + "northwind-logo.svg");
        logo.setAltDescription(STUDIO);
        logo.setLocation(LEFT, 52f);
        logo.drawOn(page);
        float x = LEFT + WIDTH / 4f + GAP;
        heading(page, TITLE, StructElem.H1, 26f, x, 50f);
        TextBlock tagline = new TextBlock(serifItalic,
                "Paper goods and ceramics, made by hand in our studio by the lake.");
        tagline.setTextColor(INK);
        tagline.setLocation(x, 86f);
        tagline.setWidth(LEFT + WIDTH - x);
        float y = tagline.drawOn(page)[1] + 18f;
        rule(page, y);

        y = heading(page, "Notebooks and mugs", StructElem.H2, 16f, LEFT, y + 20f);
        y = paragraph(page, "Every notebook is sewn by hand and opens flat, on paper heavy enough for "
                + "ink. Our mugs are thrown on the wheel and glazed in small batches, so no two are "
                + "quite alike. Each item has its code and its price in Canadian dollars.", LEFT, y, WIDTH);

        int column = 0;
        float rowTop = y + 18f;
        float rowEnd = rowTop;
        for (Product product : PRODUCTS) {
            if (product.picture == null) {
                continue;
            }
            float end = drawProduct(page, product, LEFT + column * (COLUMN + GAP), rowTop);
            rowEnd = Math.max(rowEnd, end);
            if (++column == PER_ROW) {
                column = 0;
                rowTop = rowEnd + 22f;
            }
        }

        // Page 2: the colors, the price list of all the products, and how to order
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        y = heading(page, "Colors", StructElem.H2, 16f, LEFT, 52f);
        y = paragraph(page, "Covers and glazes come in four colors; the price list says how many each "
                + "item has. Name the color with the code when you order.", LEFT, y, WIDTH);
        y = drawSwatches(page, y + 14f);

        y = heading(page, "Price list", StructElem.H2, 16f, LEFT, y + 30f);
        y = drawPriceList(page, y + 4f);

        // How to order on the left, the QR code of the shop online on the right
        float qrX = LEFT + 0.75f * WIDTH + GAP;
        float top = y + 30f;
        float end = heading(page, "Ordering", StructElem.H3, 12f, LEFT, top);
        paragraph(page, "Prices in Canadian dollars, taxes extra. Orders over $150 ship free within "
                + "Canada. Write to orders@example.com with the codes and colors you would like, and "
                + "we send an invoice within a day.", LEFT, end, 0.75f * WIDTH - GAP);
        end = heading(page, "Order online", StructElem.H3, 12f, qrX, top);
        QRCode qr = new QRCode(ORDER_URL, ErrorCorrectionLevel.M);
        qr.setModuleLength(2.5f);
        qr.setAltDescription("QR code of " + ORDER_URL);
        qr.setLocation(qrX, end + 4f);
        qr.drawOn(page);

        // The running header of each page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            drawHeader(pages.get(i), i + 1, pages.size());
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    // Draws a product in its column, its top at y: the picture, the name, a
    // heading under the section, the description, and the code and the
    // price. Returns where it ended.
    private float drawProduct(Page page, Product product, float x, float y) throws Exception {
        // The drawings are 300 wide, scaled to the width of the column
        SVGImage picture = new SVGImage(IMAGES + product.picture);
        picture.scaleBy(COLUMN / picture.getWidth());
        picture.setAltDescription(product.alt);
        picture.setLocation(x, y);
        y = picture.drawOn(page)[1];
        y = heading(page, product.name, StructElem.H3, 12f, x, y + 14f);
        y = paragraph(page, product.description, x, y, COLUMN);
        // The code padded to ten characters, so the prices line up from
        // product to product in the monospace font
        String text = String.format(Locale.US, "%-10s $%s", product.code, money(product.price));
        TextLine price = new TextLine(mono, text);
        price.setTextColor(INK);
        price.setLocation(x, y + 18f);
        price.drawOn(page);
        return y + 18f;
    }

    // Draws a swatch of each color with its name under it, in four columns,
    // its top at y, and returns where the names end. The swatches are
    // decoration; the names say the colors.
    private float drawSwatches(Page page, float y) throws Exception {
        float width = (WIDTH - 3 * GAP) / 4f;
        for (int i = 0; i < COLORS.length; i++) {
            float x = LEFT + i * (width + GAP);
            Rect swatch = new Rect(x, y, width, 40f);
            swatch.setCornerRadius(6f);
            swatch.setFillColor(COLORS[i]);
            // The sand swatch is near the color of the paper, so it has a border
            swatch.setBorderColor(COLORS[i] == SAND ? CLAY : COLORS[i]);
            swatch.drawOn(page);
            TextLine name = new TextLine(sans, COLOR_NAMES[i]);
            name.setLocation(x + (width - name.getWidth()) / 2f, y + 58f);
            name.drawOn(page);
        }
        return y + 58f;
    }

    // Draws the price list of all the products, its top at y, and returns
    // where it ended: the header in white on navy, the rows on sand, no
    // borders, the codes and the prices in the monospace font.
    private float drawPriceList(Page page, float y) throws Exception {
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(sansBold, "Code", "Item", "Colors", "Price"));
        for (Product product : PRODUCTS) {
            rows.add(row(mono, product.code, product.name,
                    (product.colors == 0) ? "" : String.valueOf(product.colors),
                    "$" + money(product.price)));
        }
        for (int i = 1; i < rows.size(); i++) {
            for (Cell cell : rows.get(i)) {
                cell.setBackgroundColor(SAND);
            }
        }

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(sansBold, Color.white, NAVY);
        table.setCellBorders(false);
        table.setCellPadding(4f, 3f);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(20f, 50f, 15f, 15f);
        table.setTextAlignmentInColumn(2, Alignment.CENTER);
        table.setTextAlignmentInColumn(3, Alignment.RIGHT);
        table.setLocation(LEFT, y);
        return table.drawOn(page)[1];
    }

    // The studio and the catalog on the left and the page number on the
    // right, above the content, in small gray type. It repeats on every
    // page, so it is an artifact.
    private void drawHeader(Page page, int number, int count) throws Exception {
        TextLine left = new TextLine(sans, STUDIO + " · " + TITLE);
        left.setFontSize(9f);
        left.setTextColor(Color.dimgray);
        left.setLocation(LEFT, 26f);
        TextLine right = new TextLine(sans, "Page " + number + " of " + count);
        right.setFontSize(9f);
        right.setTextColor(Color.dimgray);
        right.setLocation(LEFT + WIDTH - right.getWidth(), 26f);
        page.addArtifactBMC();
        left.drawOn(page);
        right.drawOn(page);
        page.addEMC();
    }

    // Draws a heading in navy, its top at y, and returns where the text
    // under it starts.
    private float heading(Page page, String text, StructElem level, float size, float x, float y)
            throws Exception {
        TextLine heading = new TextLine(sansBold, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(NAVY);
        heading.setLocation(x, y + heading.getAscent());
        heading.drawOn(page);
        return y + heading.getAscent() + 10f;
    }

    // Draws a paragraph in the serif font, its top at y, of the width, and
    // returns where it ended.
    private float paragraph(Page page, String text, float x, float y, float width) throws Exception {
        TextBlock block = new TextBlock(serif, text);
        block.setTextColor(INK);
        block.setLineSpacing(1.15f);
        block.setLocation(x, y);
        block.setWidth(width);
        return block.drawOn(page)[1];
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

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,200.00. The locale is given, or a computer set to French writes
    // 1 200,00.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_79();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_79 => %4d ms%n", time1 - time0);
    }
}   // End of Example_79.java
