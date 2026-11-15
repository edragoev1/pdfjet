/*
 * Example_65.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A photo and video consent form to print and sign, handed out at the door
 * of an event: how the organizer may use the photos a guest appears in,
 * whether the guest may be named, a line for a parent who signs for a child,
 * and the name, the date and the signature.
 * <p>
 * This is a form, not a finished document: the guest fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * organizer prints it for one event, so the event is filled in already: the
 * field helper draws a value in its box when it is given one. The answers to
 * "How may we use them?" are long, so they are radio buttons one under the
 * other, and what changing one's mind means is a paragraph, a TextBlock that
 * wraps to the width and returns where it ended. The name and the date share
 * a row two thirds to one third, and the child's name is as wide as the
 * name, so the boxes line up.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels, the answers and the paragraph are text, and the boxes are
 * artifacts. The layout is that of the consent form template of PDFjet
 * Forms. The organization and the event are made up.
 * </p>
 *
 * @see RadioButton
 * @see CheckBox
 * @see TextBlock
 */
public class Example_65 {
    static final String ORGANIZER = "Lakeside Arts Collective";
    static final String ADDRESS = "Lakeside Arts Collective, 18 Shore Road, Collingwood, "
            + "ON L9Y 1A1. Please hand the form in at the door.";
    static final String EVENT = "Winter concert, Saturday, December 12, 2026";
    static final String CHANGE_OF_MIND = "You can change your mind at any time by writing to "
            + "photos@example.com. We then stop using the photos and videos you appear in, and "
            + "take them off our website and social media within two weeks; copies already "
            + "printed stay as they are.";

    static final int BLUE = 0x1f58b0;
    static final int RED = 0xb00020;
    static final int RULE = 0xdde1e8;
    static final int BOX = 0x4d4d4d;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row. This form's rows are split two
    // thirds to one third.
    static final float LEFT = 48f;
    static final float WIDTH = 516f;
    static final float GAP = 12f;
    static final float TWO_THIRDS = (WIDTH - GAP) * 2f / 3f;
    static final float ONE_THIRD = (WIDTH - GAP) / 3f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    private final Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_65() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_65.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Photo and video consent");
        pdf.setAuthor(ORGANIZER);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what the form is about, and a rule
        heading("Photo and video consent", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "During the event we take photos and short "
                + "videos. Tell us how we may use those you appear in.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        // The consent: the event, printed in, and the use of the photos
        y = heading("Your consent", StructElem.H2, 14f, y + 46f);
        y = field("Event", LEFT, y, TWO_THIRDS, 1, EVENT);
        y = choice("How may we use them? *", LEFT, y,
                "Anywhere: our website, social media and print",
                "Only inside our organization",
                "Not at all");
        y = checkBox("You may name me beside a photo of me", y);
        y = checkBox("I sign for a child in my care", y);
        y = field("Child's name, if you sign for one", LEFT, y + 10f, TWO_THIRDS, 1, null);

        TextBlock change = new TextBlock(textFont, CHANGE_OF_MIND);
        change.setLineSpacing(1.15f);
        change.setLocation(LEFT, y);
        change.setWidth(WIDTH);
        y = change.drawOn(page)[1] + 12f;

        // The signature, under the confirmation it signs
        y = heading("Your signature", StructElem.H2, 14f, y + 18f);
        field("Full name *", LEFT, y, TWO_THIRDS, 1, null);
        y = field("Date *", LEFT + TWO_THIRDS + GAP, y, ONE_THIRD, 1, null);
        y = checkBox("I agree to the above and sign below. *", y);
        field("Signature *", LEFT, y + 10f, TWO_THIRDS, 1, null);

        // Who the organizer is, at the foot of the page
        TextLine organizer = new TextLine(textFont, ADDRESS);
        organizer.setFontSize(9f);
        organizer.setTextColor(Color.dimgray);
        organizer.setLocation(LEFT, page.getHeight() - 36f);
        organizer.drawOn(page);

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
    // for each answer under it, one a line. Returns where the next row starts.
    private float choice(String text, float x, float y, String... answers) throws Exception {
        label(text, x, y);
        float top = y + 18f;
        for (String answer : answers) {
            new RadioButton(textFont, answer).setLocation(x, top).drawOn(page);
            top += LINE_HEIGHT;
        }
        return top + 10f;
    }

    // Draws a check box and its text, its top at y, and returns where the
    // next one starts.
    private float checkBox(String text, float y) throws Exception {
        new CheckBox(textFont, text)
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        return y + LINE_HEIGHT;
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
        new Example_65();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_65 => %4d ms%n", time1 - time0);
    }
}   // End of Example_65.java
