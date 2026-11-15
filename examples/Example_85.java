/*
 * Example_85.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.qrcode.*;

/**
 * Shipping labels, thirty to a sheet: the name and the address of each
 * customer, and a QR code of the order number that the packer scans to find
 * the order, on the common US sheet of 3 x 10 address labels.
 * <p>
 * The sheet is Letter, 8.5 x 11 inches, with labels of 2 5/8 x 1 inch in 3
 * columns and 10 rows. The labels start half an inch from the top and 3/16
 * inch from the left; the columns are 2 3/4 inches apart, so there is a gap
 * of 1/8 inch between them, and the rows 1 inch apart, with no gap. In
 * points, 72 to the inch, that is a label of 189 x 72, the first at 13.5,
 * 36, the next one to the right 198 further, the next one down 72 further.
 * Print at actual size, not "fit to page", or the labels miss the sheet.
 * For A4, sheets of 3 x 8 labels of 70 x 37 mm are common: change the
 * constants, in points, mm * 72 / 25.4.
 * </p>
 * <p>
 * Nothing is drawn closer than 9 points, 1/8 inch, to the edge of a label,
 * as printers place the page a little differently each time. A line too
 * long for its label is drawn smaller rather than cut off. The QR code has
 * room around it, the quiet zone of four modules that scanners need.
 * </p>
 * <p>
 * The document is PDF/UA-1: each label is text, and each QR code a figure
 * that says the order number it carries. The people and the addresses are
 * made up.
 * </p>
 *
 * @see QRCode
 * @see TextLine
 */
public class Example_85 {
    // The geometry of the sheet, in points
    static final float LABEL_WIDTH = 189f;          // 2 5/8 inch
    static final float LABEL_HEIGHT = 72f;          // 1 inch
    static final float FIRST_X = 13.5f;             // 3/16 inch
    static final float FIRST_Y = 36f;               // 1/2 inch
    static final float COLUMN_PITCH = 198f;         // 2 3/4 inch
    static final float ROW_PITCH = 72f;             // 1 inch
    static final int COLUMNS = 3;
    static final int ROWS = 10;

    // Inside a label: the margin kept clear, and the size of a QR code
    // module. A QR code of PDFjet is 33 modules a side or more, so 49.5
    // points here, which leaves 11 points above and below it.
    static final float PADDING = 9f;
    static final float MODULE = 1.5f;

    // The orders to ship: the order number, the name, and the address in two lines
    static final String[][] ORDERS = {
        {"NW-2026-0401", "Maria Alvarez", "218 Cedar Lane", "Springfield, IL 62704"},
        {"NW-2026-0402", "Thomas Becker", "45 Orchard Road, Unit 3", "Burlington, VT 05401"},
        {"NW-2026-0403", "Aiko Tanaka", "1290 Bay View Drive", "Monterey, CA 93940"},
        {"NW-2026-0404", "Samuel Okafor", "77 Willow Street", "Hamilton, ON L8P 4R5"},
        {"NW-2026-0405", "Hannah Lindqvist", "9 Birch Court", "Duluth, MN 55803"},
        {"NW-2026-0406", "Daniel Moreau", "350 Rue des Érables", "Gatineau, QC J8T 3K2"},
        {"NW-2026-0407", "Priya Raman", "400 King Street West", "Toronto, ON M5V 1K4"},
        {"NW-2026-0408", "Ethan Walsh", "61 Harbor Point Road", "Portland, ME 04101"},
        {"NW-2026-0409", "Grace Kim", "1824 Elm Avenue, Apt 5B", "Ann Arbor, MI 48104"},
        {"NW-2026-0410", "Luis Fernández", "502 Mesa Verde Way", "Santa Fe, NM 87501"},
        {"NW-2026-0411", "Olivia Chen", "88 Lakeshore Boulevard", "Kingston, ON K7L 2Z4"},
        {"NW-2026-0412", "Noah Patel", "16 Quarry Hill", "Ithaca, NY 14850"},
        {"NW-2026-0413", "Emma Novak", "735 Prairie Street", "Lincoln, NE 68508"},
        {"NW-2026-0414", "Liam O'Connor", "23 Copper Ridge Trail", "Bozeman, MT 59715"},
        {"NW-2026-0415", "Sofia Rossi", "140 Vine Street", "Napa, CA 94559"},
        {"NW-2026-0416", "Benjamin Ward", "4 Lighthouse Lane", "Halifax, NS B3H 1Y6"},
        {"NW-2026-0417", "Chloe Dubois", "1101 Magnolia Drive", "Savannah, GA 31401"},
        {"NW-2026-0418", "Jacob Nguyen", "59 Riverbend Road", "Boise, ID 83702"},
        {"NW-2026-0419", "Ava Johansson", "312 Aspen Way, Suite 200", "Boulder, CO 80302"},
        {"NW-2026-0420", "Mateo García", "27 Palm Court", "Tucson, AZ 85701"},
        {"NW-2026-0421", "Isabella Brooks", "640 Maple Avenue", "Madison, WI 53703"},
        {"NW-2026-0422", "Lucas Schmidt", "18 Old Mill Road", "Lancaster, PA 17602"},
        {"NW-2026-0423", "Mia Thompson", "905 Sunset Terrace", "Victoria, BC V8V 2L4"},
        {"NW-2026-0424", "Henry Adeyemi", "73 Granite Street", "Concord, NH 03301"},
        {"NW-2026-0425", "Zoe Martin", "211 Pinecrest Drive", "Asheville, NC 28801"},
        {"NW-2026-0426", "Oliver Singh", "1500 Prairie Rose Road", "Regina, SK S4P 3Y2"},
        {"NW-2026-0427", "Lily Andersen", "36 Harbour Street", "Charlottetown, PE C1A 4N3"},
        {"NW-2026-0428", "William Reyes", "820 Canyon Road", "Flagstaff, AZ 86001"},
        {"NW-2026-0429", "Ella Kowalski", "5 Chestnut Square", "Providence, RI 02903"},
        {"NW-2026-0430", "James Mitchell", "447 Bluebonnet Lane", "Austin, TX 78704"},
    };

    public Example_85() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_85.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Shipping labels");
        pdf.setLanguage("en-US");

        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        Font regular = new Font(pdf, IBMPlexSans.Regular);

        // A new sheet every thirty labels, filled row by row
        Page page = null;
        for (int i = 0; i < ORDERS.length; i++) {
            int place = i % (COLUMNS * ROWS);
            if (place == 0) {
                page = new Page(pdf, Letter.PORTRAIT);
            }
            float x = FIRST_X + (place % COLUMNS) * COLUMN_PITCH;
            float y = FIRST_Y + (place / COLUMNS) * ROW_PITCH;
            drawLabel(page, bold, regular, ORDERS[i], x, y);
        }

        pdf.complete();
    }

    // Draws a label with its top left corner at x, y: the QR code on the
    // right, centered from top to bottom, and the name and the address to the
    // left of it.
    private static void drawLabel(Page page, Font bold, Font regular, String[] order, float x, float y)
            throws Exception {
        QRCode qr = new QRCode(order[0], ErrorCorrectionLevel.M);
        qr.setModuleLength(MODULE);
        qr.setAltDescription("Order " + order[0]);
        float qrSize = qr.drawOn(null)[0];     // Measured, at 0, 0, so its size
        qr.setLocation(x + LABEL_WIDTH - PADDING - qrSize, y + (LABEL_HEIGHT - qrSize) / 2f);
        qr.drawOn(page);

        // The text ends a quiet zone of four modules before the QR code
        float textWidth = LABEL_WIDTH - 2 * PADDING - qrSize - 4 * MODULE;
        float baseline = y + 25f;
        drawFitted(page, bold, 10f, order[1], x + PADDING, baseline, textWidth);
        drawFitted(page, regular, 9f, order[2], x + PADDING, baseline + 13f, textWidth);
        drawFitted(page, regular, 9f, order[3], x + PADDING, baseline + 25f, textWidth);
    }

    // Draws a line of text in the size, or smaller, down to 7 points, when it
    // is wider than the room it has.
    private static void drawFitted(Page page, Font font, float size, String text,
            float x, float y, float width) throws Exception {
        TextLine line = new TextLine(font, text);
        line.setFontSize(size);
        while (line.getWidth() > width && size > 7f) {
            size -= 0.5f;
            line.setFontSize(size);
        }
        line.setLocation(x, y);
        line.drawOn(page);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_85();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_85 => %4d ms%n", time1 - time0);
    }
}   // End of Example_85.java
