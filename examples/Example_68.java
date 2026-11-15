/*
 * Example_68.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A customer feedback card to print and fill in by hand, left on the counter
 * of a bicycle shop: the date of the visit and what it was for, a rating of
 * 1 to 5 for the quality, the service, the value and the visit overall,
 * whether the customer would recommend the shop, and two boxes for comments.
 * <p>
 * This is a form, not a finished document: the customer fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * ratings are a grid: a row for each question, with five radio buttons
 * labeled 1 to 5 in columns of the same width, under a line that says what
 * 1 and 5 mean. The columns are placed by a constant, not by the width of
 * the labels, so the buttons of all the rows line up, and a thin line
 * between the rows, an artifact, keeps the eye on the row.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the sections are headings, the
 * questions and the answers are text, and the boxes and the lines are
 * artifacts. The layout is that of the customer feedback template of PDFjet
 * Forms. The shop is made up.
 * </p>
 *
 * @see RadioButton
 * @see CheckBox
 * @see Rect
 */
public class Example_68 {
    static final String SHOP = "Harbor Lane Bicycles";
    static final String RETURN_TO = "Leave the card at the counter, or mail it to Harbor Lane "
            + "Bicycles, 72 Harbor Lane, Mystic, CT 06355. Thank you.";

    // The questions of the rating grid, each rated from 1 to 5
    static final String[] RATINGS = {
        "Quality of the work or the product",
        "Service: friendly, clear, on time",
        "Value for money",
        "Your visit overall *",
    };

    static final int BLUE = 0x1f58b0;
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static final float LEFT = 44f;
    static final float WIDTH = 524f;
    static final float GAP = 12f;
    static final float HALF = (WIDTH - GAP) / 2f;

    // The rating grid: where its first column of buttons starts, and how
    // far apart the columns are
    static final float SCALE_LEFT = LEFT + 250f;
    static final float SCALE_STEP = 54f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    private final Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_68() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_68.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Customer feedback");
        pdf.setAuthor(SHOP);
        pdf.setLanguage("en-US");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, a line on what it is for, and a rule
        heading("How did we do?", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Two minutes of your time help us do better. "
                + "Every card is read by the owner.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(LEFT, y, WIDTH);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // Your visit: the date, and what it was for, any of three
        y = heading("Your visit", StructElem.H2, 14f, y + 46f);
        float third = (WIDTH - 2f * GAP) / 3f;
        field("Date of your visit", LEFT, y, third, 1, null);
        float x = LEFT + third + GAP;
        label("What did you come for?", x, y);
        for (String reason : new String[] {"A purchase", "A repair or a service", "Advice"}) {
            x = new CheckBox(textFont, reason)
                    .setBorderColor(BOX)
                    .setLocation(x, y + 21f)
                    .drawOn(page)[0] + 12f;
        }
        y += 64f;

        // Your ratings: what 1 and 5 mean, then a row for each question
        y = heading("Your ratings", StructElem.H2, 14f, y + 18f);
        new TextLine(italic, "1 is poor, 5 is excellent.")
                .setLocation(LEFT, y + italic.getAscent())
                .drawOn(page);
        y += 22f;
        for (String question : RATINGS) {
            new TextLine(textFont, question)
                    .setLocation(LEFT, y + textFont.getAscent())
                    .drawOn(page);
            for (int i = 1; i <= 5; i++) {
                new RadioButton(textFont, String.valueOf(i))
                        .setLocation(SCALE_LEFT + (i - 1) * SCALE_STEP, y)
                        .drawOn(page);
            }
            rule(LEFT, y + 21f, WIDTH);
            y += 28f;
        }

        // Would you recommend us, in a row
        label("Would you recommend us to a friend?", LEFT, y + 6f);
        x = LEFT;
        for (String answer : new String[] {"Yes, gladly", "Probably", "Not sure", "No"}) {
            x = new RadioButton(textFont, answer).setLocation(x, y + 24f).drawOn(page)[0] + 18f;
        }
        y += 56f;

        // In the customer's words
        y = field("What did we do well?", LEFT, y, WIDTH, 2, null);
        y = field("What could we do better?", LEFT, y, WIDTH, 2, null);
        new CheckBox(textFont, "You may quote me, without my name")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        new CheckBox(textFont, "Please contact me about my answers")
                .setBorderColor(BOX)
                .setLocation(LEFT + HALF + GAP, y)
                .drawOn(page);
        y += 30f;
        field("Name", LEFT, y, HALF, 1, null);
        field("Email or phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // Where the card goes, at the foot of the page
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

    // A thin line, decoration only, so an artifact that a screen reader skips.
    private void rule(float x, float y, float width) throws Exception {
        Line line = new Line(x, y, x + width, y);
        line.setStrokeColor(RULE);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_68();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_68 => %4d ms%n", time1 - time0);
    }
}   // End of Example_68.java
