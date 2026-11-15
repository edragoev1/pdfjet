/*
 * Example_75.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.math.BigDecimal;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A membership application of a community garden, to print and fill in by
 * hand: the name, the address and how to reach the member, the kind of
 * membership with its fee, what they would like to take part in, the
 * newsletter and the rules to agree to, and a box to sign in.
 * <p>
 * This is a form people fill in, not a document with data: the boxes are
 * empty. It is drawn as Example_72 draws the job application, with the same
 * field helper: the label above its box, the boxes on a grid of columns so
 * that the rows line up, 24 points tall for a line of handwriting, and an
 * asterisk on the label of a field that is required.
 * </p>
 * <p>
 * The kinds of membership are radio buttons, one of three, and their fees
 * come from the data of the club, so that a change of fee is made in one
 * place and the form printed again. The options of a
 * choice are placed one after the other, each where the one before it ended,
 * so a longer option moves the next one along.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the sections are headings, the
 * labels and the options are text, and the boxes are artifacts. The layout
 * is that of the membership application template of PDFjet Forms. The garden
 * is made up.
 * </p>
 *
 * @see CheckBox
 * @see RadioButton
 * @see Rect
 */
public class Example_75 {
    static final String CLUB = "Riverbend Community Garden";
    static final String RETURN_TO = "Please bring the form to the garden shed on a Saturday "
            + "morning, or mail it to 85 Mill Road, Elora, ON N0B 1S0.";

    // The kinds of membership and their fees for a year, in dollars
    static final String[] KINDS = {"Individual", "Family", "Student"};
    static final BigDecimal[] FEES = {
        new BigDecimal("40"), new BigDecimal("70"), new BigDecimal("20"),
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

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    private final Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_75() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_75.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Membership application");
        pdf.setAuthor(CLUB);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what to do with the form, and a rule
        heading("Membership application", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Join " + CLUB + " for a year. Fill in this "
                + "form and send it back with your first payment.");
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
        field("First name *", LEFT, y, HALF, 1, null);
        y = field("Last name *", LEFT + HALF + GAP, y, HALF, 1, null);
        y = field("Street address", LEFT, y, WIDTH, 1, null);
        // City, province and postal code: 40, 30 and 30 percent of the row
        float city = 0.4f * (WIDTH - 2 * GAP);
        float rest = 0.3f * (WIDTH - 2 * GAP);
        field("City", LEFT, y, city, 1, null);
        field("Province or state", LEFT + city + GAP, y, rest, 1, null);
        y = field("Postal code", LEFT + city + GAP + rest + GAP, y, rest, 1, null);
        field("Email *", LEFT, y, HALF, 1, null);
        y = field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The membership: its kind, one of three, with the fee of each
        y = heading("Your membership", StructElem.H2, 14f, y + 18f);
        label("Membership *", LEFT, y);
        float x = LEFT + 96f;
        for (int i = 0; i < KINDS.length; i++) {
            String option = KINDS[i] + ", $" + FEES[i].toPlainString();
            x = new RadioButton(textFont, option).setLocation(x, y - 1f).drawOn(page)[0] + 24f;
        }
        y += 30f;
        y = field("Start date *", LEFT, y, HALF, 1, null);
        y = field("What would you like to take part in?", LEFT, y, WIDTH, 2, null);

        // The newsletter, the rules, and the signature under them
        new CheckBox(textFont, "Send me the monthly newsletter by email")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        new CheckBox(textFont, "I agree to the members' rules and sign below. *")
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
        new Example_75();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_75 => %4d ms%n", time1 - time0);
    }
}   // End of Example_75.java
