/*
 * Example_64.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A new client form to print and fill in by hand, that a design studio hands
 * to a client before the first meeting: who the client is, what the project
 * needs, the budget, the dates, and what the project should achieve.
 * <p>
 * This is a form, not a finished document: the client fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * two questions of several answers, the kind of work and the budget, are
 * lists of radio buttons, one answer a line, side by side; the list of five
 * answers is longer than the list of four, so the row under them starts
 * where the longer one ended. The last answer, "Something else", has a line
 * after it to write what, drawn from where the button ended to the edge of
 * its column.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels and the answers are text, and the boxes and the write-in line
 * are artifacts. The layout is that of the client intake template of PDFjet
 * Forms. The studio is made up.
 * </p>
 *
 * @see RadioButton
 * @see CheckBox
 * @see Rect
 */
public class Example_64 {
    static final String STUDIO = "Tidewater Studio";
    static final String RETURN_TO = "Please bring the form to our first meeting at 240 Front "
            + "Street, Kingston, or scan it to hello@example.com.";

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

    public Example_64() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_64.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("New client form");
        pdf.setAuthor(STUDIO);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, why the studio asks, and a rule
        heading("New client form", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Tell us about you and your project, so that "
                + "we can prepare for our first meeting. It takes about five minutes.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // About you
        y = heading("About you", StructElem.H2, 14f, y + 46f);
        field("Full name *", LEFT, y, HALF, 1, null);
        y = field("Company", LEFT + HALF + GAP, y, HALF, 1, null);
        field("Email *", LEFT, y, HALF, 1, null);
        y = field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // Your project. The two lists are side by side, and the row under
        // them starts below the longer one.
        y = heading("Your project", StructElem.H2, 14f, y + 18f);
        float end = choice("What do you need? *", LEFT, y, HALF,
                "Brand identity", "Website", "Print design", "Photography", "Something else");
        y = Math.max(end, choice("Budget", LEFT + HALF + GAP, y, HALF,
                "Under $1,000", "$1,000 to $5,000", "$5,000 to $15,000", "Over $15,000"));
        field("Ideal start date", LEFT, y, HALF, 1, null);
        y = field("Deadline", LEFT + HALF + GAP, y, HALF, 1, null);
        y = field("What should the project achieve? *", LEFT, y, WIDTH, 4, null);
        y = field("How did you hear about us?", LEFT, y, WIDTH, 1, null);

        new CheckBox(textFont, "Send me occasional news about our work")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);

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

    // Draws a question of several answers: its label at y and a radio button
    // for each answer under it, one a line. An answer that starts with
    // "Something else" gets a line after it, to write what. Returns where the
    // next row starts.
    private float choice(String text, float x, float y, float width, String... answers)
            throws Exception {
        label(text, x, y);
        float top = y + 18f;
        for (String answer : answers) {
            float[] end = new RadioButton(textFont, answer).setLocation(x, top).drawOn(page);
            if (answer.startsWith("Something else")) {
                Line line = new Line(end[0], end[1], x + width, end[1]);
                line.setStrokeColor(BOX);
                line.setStrokeWidth(0.5f);
                page.addArtifactBMC();
                line.drawOn(page);
                page.addEMC();
            }
            top += LINE_HEIGHT;
        }
        return top + 10f;
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
        new Example_64();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_64 => %4d ms%n", time1 - time0);
    }
}   // End of Example_64.java
