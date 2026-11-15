/*
 * Example_66.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A contact details update form, mailed by a property manager to each of its
 * tenants: one page a tenant, with the name and the tenant number printed in,
 * the details on file shown above the boxes, and room for the new address,
 * email and phone, the date they apply from, and a signature.
 * <p>
 * This is a form, not a finished document: the tenant fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * forms come from a list of tenants, the way a mail merge does: the same
 * code draws a page for each one, and the field helper draws the name and
 * the number in their boxes. What is on file is printed in gray, so the
 * tenant fills in only what has changed. The address row is split 40, 30 and
 * 30 percent, the signature row 60 and 40, with a gap between the fields.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the section of each page are
 * headings, the labels and the details are text, and the boxes are
 * artifacts. The layout is that of the contact details update template of
 * PDFjet Forms. The company and the tenants are made up.
 * </p>
 *
 * @see Rect
 * @see RadioButton
 * @see CheckBox
 */
public class Example_66 {
    static final String COMPANY = "Oakridge Property Management";
    static final String RETURN_TO = "Please return the form in the envelope enclosed, or to the "
            + "office at 310 Queen Street, Fredericton, NB E3B 1B2.";

    // The tenants: the name, the tenant number, and the details on file. In
    // an application they come from the database.
    static final String[][] TENANTS = {
        {"Maria Alvarez", "T-1042", "Unit 4, 55 Charlotte Street, Fredericton, NB E3B 1L4\n"
                + "maria.alvarez@example.com, 506-555-0117"},
        {"Samuel Okafor", "T-1057", "Unit 12, 55 Charlotte Street, Fredericton, NB E3B 1L4\n"
                + "s.okafor@example.com, 506-555-0183"},
        {"Hannah Lindqvist", "T-1103", "88 Waterloo Row, Fredericton, NB E3B 1Y9\n"
                + "hannah.l@example.com, 506-555-0129"},
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

    private Page page;
    private final Font labelFont;
    private final Font textFont;
    private final Font italic;

    public Example_66() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_66.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Contact details update");
        pdf.setAuthor(COMPANY);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        for (String[] tenant : TENANTS) {
            page = new Page(pdf, Letter.PORTRAIT);
            drawForm(tenant[0], tenant[1], tenant[2]);
        }
        pdf.complete();
    }

    // Draws the form of one tenant on the page.
    private void drawForm(String name, String number, String onFile) throws Exception {
        // The title, what to fill in, and a rule
        heading("Update your contact details", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Tell us what has changed. "
                + "Fill in only the details that are new.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // Who the form is for, printed in, and what is on file now
        y += 40f;
        field("Full name *", LEFT, y, HALF, 1, name);
        y = field("Tenant number", LEFT + HALF + GAP, y, HALF, 1, number);
        label("On file now", LEFT, y);
        TextBlock current = new TextBlock(textFont, onFile);
        current.setLineSpacing(1.15f);
        current.setTextColor(Color.dimgray);
        current.setLocation(LEFT, y + 16f);
        current.setWidth(WIDTH);
        y = current.drawOn(page)[1] + 12f;

        // The new details. The address row is split 40, 30 and 30 percent.
        y = heading("Your new details", StructElem.H2, 14f, y + 18f);
        y = field("Street address", LEFT, y, WIDTH, 1, null);
        float rest = WIDTH - 2f * GAP;
        field("City", LEFT, y, 0.4f * rest, 1, null);
        field("Province or state", LEFT + 0.4f * rest + GAP, y, 0.3f * rest, 1, null);
        y = field("Postal code", LEFT + 0.7f * rest + 2f * GAP, y, 0.3f * rest, 1, null);
        field("Email", LEFT, y, HALF, 1, null);
        y = field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);
        field("These details apply from *", LEFT, y, HALF, 1, null);

        // The best way to reach the tenant, three answers in a row beside the date
        label("Best way to reach you", LEFT + HALF + GAP, y);
        float x = LEFT + HALF + GAP;
        for (String answer : new String[] {"Email", "Phone", "Mail"}) {
            x = new RadioButton(textFont, answer).setLocation(x, y + 21f).drawOn(page)[0] + 18f;
        }
        y += 64f;

        // The confirmation, and the signature under it
        new CheckBox(textFont, "I confirm these details and sign below. *")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        y += 30f;
        field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        // Where the form goes, at the foot of the page
        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.setFontSize(9f);
        returnTo.setTextColor(Color.dimgray);
        returnTo.setLocation(LEFT, page.getHeight() - 36f);
        returnTo.drawOn(page);
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
        new Example_66();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_66 => %4d ms%n", time1 - time0);
    }
}   // End of Example_66.java
