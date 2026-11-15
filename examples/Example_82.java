/*
 * Example_82.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A time off request, to print and fill in by hand: the employee writes the
 * kind of leave, the first and the last day, and signs; the manager writes
 * the decision in a part of the form of its own, and signs too.
 * <p>
 * This is a form people fill in, not a document with data. It is drawn as
 * Example_72 draws the job application, with the same field helper: the
 * label above its box, the boxes on a grid, and an asterisk on the label of
 * a field that is required. The kind of leave and the decision are radio
 * buttons, one of the options, under their label.
 * </p>
 * <p>
 * Two people fill in the form, one after the other, so the part of the
 * manager is set apart: a rule across the page and a heading of its own.
 * Its fields have no asterisk, as the employee leaves them empty.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the two parts are headings,
 * the labels and the options are text, and the boxes are artifacts. The
 * layout is that of the time off request template of PDFjet Forms. The
 * company is made up.
 * </p>
 *
 * @see RadioButton
 * @see CheckBox
 * @see Rect
 */
public class Example_82 {
    static final String COMPANY = "Northwind Studio";
    static final String[] KINDS_OF_LEAVE = {
        "Vacation", "Sick leave", "Personal day", "Parental leave", "Other",
    };

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
    static final float QUARTER = (HALF - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    // The signature box and the date box beside it, 60 and 40 percent
    static final float SIGNATURE = 0.6f * WIDTH - GAP / 2f;
    static final float DATE = 0.4f * WIDTH - GAP / 2f;

    private final Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_82() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_82.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Time off request");
        pdf.setAuthor(COMPANY);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, when to send the form, and a rule
        heading("Time off request", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Fill in this form at least two weeks before "
                + "your time off, sign it and give it to your manager.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // The employee's part
        y = heading("Your request", StructElem.H2, 14f, y + 46f);
        field("Your name *", LEFT, y, HALF, 1, null);
        y = field("Department", LEFT + HALF + GAP, y, HALF, 1, null);
        y = choice("Type of leave *", LEFT, y, KINDS_OF_LEAVE);
        field("First day *", LEFT, y, QUARTER, 1, null);
        field("Last day *", LEFT + QUARTER + GAP, y, QUARTER, 1, null);
        field("Working days", LEFT + HALF + GAP, y, QUARTER, 1, null);
        y = field("Phone while away", LEFT + HALF + GAP + QUARTER + GAP, y, QUARTER, 1, null);
        y = field("Notes for your manager", LEFT, y, WIDTH, 2, null);

        new CheckBox(textFont, "I confirm this request and sign below. *")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        y += 30f;
        field("Your signature *", LEFT, y, SIGNATURE, 1, null);
        y = field("Date *", LEFT + SIGNATURE + GAP, y, DATE, 1, null);

        // The manager's part, below a rule, filled in after the employee's
        rule(y + 12f);
        y = heading("For your manager", StructElem.H2, 14f, y + 42f);
        y = choice("Decision", LEFT, y, "Approved", "Not approved");
        y = field("Manager's name", LEFT, y, HALF, 1, null);
        new CheckBox(textFont, "I confirm this decision and sign below.")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        y += 30f;
        field("Manager's signature", LEFT, y, SIGNATURE, 1, null);
        field("Date of decision", LEFT + SIGNATURE + GAP, y, DATE, 1, null);

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

    // Draws a question of a few answers: its label at y and a radio button
    // for each answer under it, where a box would be, each starting where the
    // one before it ended. drawOn returns the end of a button with some room
    // after its text already. Returns where the next row starts.
    private float choice(String text, float x, float y, String... answers) throws Exception {
        label(text, x, y);
        for (String answer : answers) {
            x = new RadioButton(textFont, answer).setLocation(x, y + 20f).drawOn(page)[0] + 12f;
        }
        return y + 50f;
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
        new Example_82();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_82 => %4d ms%n", time1 - time0);
    }
}   // End of Example_82.java
