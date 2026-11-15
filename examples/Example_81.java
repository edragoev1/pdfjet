/*
 * Example_81.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.math.BigDecimal;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A rental application, two pages to print and fill in by hand: the
 * property, the applicant and where they live now, who will live in the
 * property, their work and income, two references, and the agreement to
 * sign, with "Page 1 of 2" at the bottom of each page.
 * <p>
 * This is a form people fill in, not a document with data. It is drawn as
 * Example_72 draws the job application, with the same field helper: the
 * label above its box, the boxes on a grid of two or three columns, and an
 * asterisk on the label of a field that is required. The choices, as the
 * length of the lease or the pets, are radio buttons under their label.
 * </p>
 * <p>
 * The landlord prints the form for one property, so its address and its
 * rent are filled in already, in the dark red of the answers of the
 * template, apart from the black of the form; the rent is a BigDecimal
 * written with Locale.US. The second page starts with a section: a form is
 * broken between its sections, never inside one.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the sections are headings, the
 * labels and the options are text, and the boxes and the page numbers are
 * artifacts. The layout is that of the rental application template of
 * PDFjet Forms. The landlord and the address are made up.
 * </p>
 *
 * @see RadioButton
 * @see CheckBox
 * @see Rect
 */
public class Example_81 {
    static final String LANDLORD = "Cedar Row Rentals";
    static final String RETURN_TO = "Please return both pages to Cedar Row Rentals, "
            + "40 Woolwich Street, Guelph, ON N1H 3V1, or scan them to rent@example.com.";

    // The property the form is printed for
    static final String PROPERTY = "Unit 4, 118 Elm Street, Guelph, ON N1H 3T2";
    static final BigDecimal RENT = new BigDecimal("1850.00");

    static final int GRAY = 0x333333;           // the headings
    static final int MAROON = 0x800000;         // the answers printed on the form
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;

    // The grid: the margin, the width of the page between the margins, the
    // gap between two fields of a row, and the widths of two and three fields
    static final float LEFT = 48f;
    static final float WIDTH = 516f;
    static final float GAP = 12f;
    static final float HALF = (WIDTH - GAP) / 2f;
    static final float THIRD = (WIDTH - 2 * GAP) / 3f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    static final int PAGES = 2;

    private Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_81() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_81.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Rental application");
        pdf.setAuthor(LANDLORD);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        // Page 1: the title, the property and the applicant
        page = new Page(pdf, Letter.PORTRAIT);
        heading("Rental application", StructElem.H1, 20f, 62f);
        float y = paragraph("Thank you for your interest in the property. Fill in both pages, "
                + "sign at the end and send the form back to us; we answer within three "
                + "working days.", 74f) + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        y = heading("The property", StructElem.H2, 14f, y + 46f);
        y = field("Address of the property *", LEFT, y, WIDTH, 1, PROPERTY);
        field("Move-in date *", LEFT, y, HALF, 1, null);
        y = field("Monthly rent", LEFT + HALF + GAP, y, HALF, 1,
                "$" + String.format(Locale.US, "%,.2f", RENT));
        y = choice("Length of the lease", new String[] {"12 months", "6 months", "Month to month"}, y);

        y = heading("About you", StructElem.H2, 14f, y + 18f);
        field("First name *", LEFT, y, HALF, 1, null);
        y = field("Last name *", LEFT + HALF + GAP, y, HALF, 1, null);
        field("Email *", LEFT, y, HALF, 1, null);
        y = field("Phone *", LEFT + HALF + GAP, y, HALF, 1, null);
        y = field("Current address *", LEFT, y, WIDTH, 2, null);
        y = choice("How long have you lived there?",
                new String[] {"Less than a year", "1 to 3 years", "More than 3 years"}, y);
        field("Current landlord's name", LEFT, y, HALF, 1, null);
        field("Current landlord's phone", LEFT + HALF + GAP, y, HALF, 1, null);
        footer(1);

        // Page 2: the household, the income, the references and the agreement
        page = new Page(pdf, Letter.PORTRAIT);
        y = heading("Who will live there", StructElem.H2, 14f, 62f);
        field("Adults *", LEFT, y, THIRD, 1, null);
        y = field("Children", LEFT + THIRD + GAP, y, THIRD, 1, null);
        y = choice("Pets", new String[] {"No pets", "Cat", "Dog", "Other"}, y);
        y = field("About your pets, if any", LEFT, y, WIDTH, 1, null);

        y = heading("Work and income", StructElem.H2, 14f, y + 18f);
        field("Employer", LEFT, y, HALF, 1, null);
        y = field("Position", LEFT + HALF + GAP, y, HALF, 1, null);
        field("Years there", LEFT, y, THIRD, 1, null);
        field("Monthly income", LEFT + THIRD + GAP, y, THIRD, 1, null);
        y = field("Employer's phone", LEFT + 2 * (THIRD + GAP), y, THIRD, 1, null);

        y = heading("References", StructElem.H2, 14f, y + 18f);
        y = paragraph("Two people who know you, not related to you: a former landlord, "
                + "an employer or a colleague.", y) + 10f;
        for (String which : new String[] {"First", "Second"}) {
            field(which + " reference", LEFT, y, THIRD, 1, null);
            field("Phone", LEFT + THIRD + GAP, y, THIRD, 1, null);
            y = field("How they know you", LEFT + 2 * (THIRD + GAP), y, THIRD, 1, null);
        }

        y = heading("Agreement", StructElem.H2, 14f, y + 18f);
        y = paragraph("I confirm that what I have written in this application is true, and "
                + "I allow you to contact my landlord, my employer and my references to "
                + "check it. Sending this application does not commit either of us to a "
                + "lease.", y) + 12f;
        new CheckBox(textFont, "I have read and agree to the above and sign below. *")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        y += 30f;
        field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.setFontSize(9f);
        returnTo.setTextColor(Color.dimgray);
        returnTo.setLocation(LEFT, page.getHeight() - 52f);
        returnTo.drawOn(page);
        footer(2);

        pdf.complete();
    }

    // Draws a heading in gray, its baseline at y, and returns where the field
    // under it starts.
    private float heading(String text, StructElem level, float size, float y) throws Exception {
        TextLine heading = new TextLine(labelFont, text);
        heading.setStructureType(level);
        heading.setFontSize(size);
        heading.setTextColor(GRAY);
        heading.setLocation(LEFT, y);
        heading.drawOn(page);
        return y + 16f;
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns where it ended.
    private float paragraph(String text, float y) throws Exception {
        TextBlock block = new TextBlock(textFont, text);
        block.setLineSpacing(1.15f);
        block.setLocation(LEFT, y);
        block.setWidth(WIDTH);
        return block.drawOn(page)[1];
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
            line.setTextColor(MAROON);
            line.setLocation(x + 6f, top + 16f);
            line.drawOn(page);
        }
        return top + height + 12f;
    }

    // Draws a choice of one of the options: its label, and the radio buttons
    // under it, where a box would be, each starting where the one before it
    // ended. drawOn returns the end of a button with some room after its
    // text already. Returns where the next row starts.
    private float choice(String text, String[] options, float y) throws Exception {
        label(text, LEFT, y);
        float x = LEFT;
        for (String option : options) {
            x = new RadioButton(textFont, option).setLocation(x, y + 20f).drawOn(page)[0] + 12f;
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

    // The page number at the foot of the page, as an artifact: it is not
    // part of the form, and a screen reader skips it.
    private void footer(int number) throws Exception {
        TextLine footer = new TextLine(textFont, "Page " + number + " of " + PAGES);
        footer.setFontSize(10f);
        footer.setLocation(LEFT + WIDTH - footer.getWidth(), page.getHeight() - 36f);
        page.addArtifactBMC();
        footer.drawOn(page);
        page.addEMC();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_81();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_81 => %4d ms%n", time1 - time0);
    }
}   // End of Example_81.java
