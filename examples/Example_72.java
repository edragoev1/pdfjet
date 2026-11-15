/*
 * Example_72.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A job application form to print and fill in by hand: a box under each
 * label, larger boxes for the address and the experience, radio buttons for
 * the hours, check boxes for the consent, and a box to sign in.
 * <p>
 * The form is drawn, not interactive: PDFjet draws text, lines and boxes, and
 * makes no fields that a viewer types into. That is the form most people
 * meet, on a clipboard at the front desk; it can also be filled in on screen
 * with the tools of a PDF viewer that type or sign on a page. So the boxes are
 * sized for handwriting, 24 points, about a third of an inch, tall, and each
 * label is above its box, where it stays readable once the box is filled. A
 * required field says so in its label, with an asterisk, as the note at the
 * top explains.
 * </p>
 * <p>
 * The employer prints the form for one opening, so the position is filled in
 * already: the field helper draws a value in its box when it is given one.
 * The fields are placed on a grid of two columns with a gap between them, so
 * that the boxes of the rows line up.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels and the options are text, and the boxes are artifacts. The
 * layout is that of the job application template of PDFjet Forms. The
 * company is made up.
 * </p>
 *
 * @see CheckBox
 * @see RadioButton
 * @see Rect
 */
public class Example_72 {
    static final String COMPANY = "Northwind Studio";
    static final String RETURN_TO = "Please return the form to Northwind Studio, "
            + "12 Harbour Street, Port Elgin, ON N0H 2C0, or scan it to jobs@example.com.";
    static final String POSITION = "Junior graphic designer (opening 2026-17)";

    static final int BLUE = 0x1f58b0;
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;

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

    public Example_72() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_72.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Job application");
        pdf.setAuthor(COMPANY);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what happens to the form, and a rule
        heading("Job application", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, COMPANY + " thanks you for your interest in working "
                + "with us. We read every application and answer within two weeks.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // Your contact details
        y = heading("Your contact details", StructElem.H2, 14f, y + 46f);
        field("First name *", LEFT, y, HALF, 1, null);
        y = field("Last name *", LEFT + HALF + GAP, y, HALF, 1, null);
        field("Email *", LEFT, y, HALF, 1, null);
        y = field("Phone *", LEFT + HALF + GAP, y, HALF, 1, null);
        y = field("Street address", LEFT, y, WIDTH, 2, null);
        float quarter = (HALF - GAP) / 2f;
        field("City", LEFT, y, HALF, 1, null);
        field("State or province", LEFT + HALF + GAP, y, quarter, 1, null);
        y = field("Postal code", LEFT + HALF + GAP + quarter + GAP, y, quarter, 1, null);

        // The position
        y = heading("The position", StructElem.H2, 14f, y + 18f);
        field("Position you apply for *", LEFT, y, HALF, 1, POSITION);
        y = field("Available from", LEFT + HALF + GAP, y, HALF, 1, null);

        // The hours, one of three, beside their label: each button starts
        // where the one before it ended
        label("Hours", LEFT, y);
        float x = LEFT + 70f;
        for (String option : new String[] {"Full time", "Part time", "Either"}) {
            x = new RadioButton(textFont, option).setLocation(x, y - 1f).drawOn(page)[0] + 24f;
        }
        y += 30f;

        y = field("Your experience for this position *", LEFT, y, WIDTH, 4, null);

        // The consent, and the signature under it
        new CheckBox(textFont, "You may keep my application for other positions for six months")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        new CheckBox(textFont, "I confirm the above is correct and sign below. *")
                .setBorderColor(BOX)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);
        y += 50f;
        field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        // Where the form goes, at the foot of the page
        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.setFontSize(9f);
        returnTo.setTextColor(Color.dimgray);
        returnTo.setLocation(LEFT, page.getHeight() - 36f);
        returnTo.drawOn(page);

        pdf.complete();
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
        new Example_72();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_72 => %4d ms%n", time1 - time0);
    }
}   // End of Example_72.java
