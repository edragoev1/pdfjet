/*
 * Example_63.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * An appointment request form to print and fill in by hand, for the front
 * desk of a small accounting office: the name and how to reach the client,
 * what the appointment is for, two dates that suit, how to meet, and room
 * for a note; at the foot, on a shade, the part the office fills in when it
 * books the time.
 * <p>
 * This is a form, not a finished document: the client fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box, and
 * the questions with a few answers are radio buttons. Those answers are laid
 * out by a helper that measures each button first, with drawOn(null), and
 * starts a new line when the next one would pass the width of its field. So
 * the four answers of "What is it for?" wrap to two lines in half the page,
 * and the row under it starts below the taller of the two fields beside each
 * other.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the three sections are headings,
 * the labels and the answers are text, and the boxes are artifacts. The
 * layout is that of the appointment request template of PDFjet Forms. The
 * office is made up.
 * </p>
 *
 * @see RadioButton
 * @see CheckBox
 * @see Rect
 */
public class Example_63 {
    static final String OFFICE = "Maple Lane Accounting";
    static final String CONTACT = "Maple Lane Accounting, 85 Mill Street, Elora, ON N0B 1S0. "
            + "Phone 519-555-0142, or email appointments@example.com.";

    static final int BLUE = 0x1f58b0;
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;
    static final int SHADE = 0xf1f3f6;

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

    public Example_63() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_63.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Appointment request");
        pdf.setAuthor(OFFICE);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what happens to the form, and a rule
        heading("Book an appointment", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Tell us when suits you, and we will confirm "
                + "by email or phone within one working day.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // Who you are and how to reach you
        y = heading("Your details", StructElem.H2, 14f, y + 46f);
        y = field("Full name *", LEFT, y, WIDTH, 1, null);
        field("Email *", LEFT, y, HALF, 1, null);
        y = field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The appointment. The two choices share a row, and the next row
        // starts under the one that took more lines.
        y = heading("The appointment", StructElem.H2, 14f, y + 18f);
        float end = choice("What is it for? *", LEFT, y, HALF,
                "First consultation", "Follow-up", "Review of a document", "Other");
        y = Math.max(end, choice("Preferred time", LEFT + HALF + GAP, y, HALF,
                "Morning", "Afternoon", "Evening"));
        field("Preferred date *", LEFT, y, HALF, 1, null);
        y = field("Another date that works", LEFT + HALF + GAP, y, HALF, 1, null);
        y = choice("How would you like to meet?", LEFT, y, WIDTH,
                "In person", "Video call", "Phone call");
        y = field("Anything we should know before we meet", LEFT, y, WIDTH, 3, null);

        new CheckBox(textFont, "Send me a reminder the day before")
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);

        // The office fills in the rest when it books the time, so its part is
        // at the foot of the page, on a shade that sets it apart. The shade
        // is a Rect, and a Rect is drawn as an artifact.
        float office = page.getHeight() - 132f;
        new Rect(LEFT - 8f, office - 8f, WIDTH + 16f, 84f).setFillColor(SHADE).drawOn(page);
        heading("For office use", StructElem.H2, 12f, office + 8f);
        float third = (WIDTH - 2f * GAP) / 3f;
        field("Booked for", LEFT, office + 18f, third, 1, null);
        field("With", LEFT + third + GAP, office + 18f, third, 1, null);
        field("Confirmed by, and how", LEFT + 2f * (third + GAP), office + 18f, third, 1, null);

        // How to reach the office, at the foot of the page
        TextLine contact = new TextLine(textFont, CONTACT);
        contact.setFontSize(9f);
        contact.setTextColor(Color.dimgray);
        contact.setLocation(LEFT, page.getHeight() - 36f);
        contact.drawOn(page);

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

    // Draws a question of a few answers: its label at y and a radio button for
    // each answer under it, left to right. A button that would pass the width
    // goes to the start of the next line; drawOn(null) measures it without
    // drawing it. Returns where the next row starts.
    private float choice(String text, float x, float y, float width, String... answers)
            throws Exception {
        label(text, x, y);
        float top = y + 18f;
        float left = x;
        for (String answer : answers) {
            RadioButton button = new RadioButton(textFont, answer);
            float right = button.setLocation(left, top).drawOn(null)[0];
            if (right > x + width && left > x) {
                left = x;
                top += LINE_HEIGHT;
                button.setLocation(left, top);
            }
            left = button.drawOn(page)[0] + 12f;
        }
        return top + LINE_HEIGHT + 10f;
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
        new Example_63();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_63 => %4d ms%n", time1 - time0);
    }
}   // End of Example_63.java
