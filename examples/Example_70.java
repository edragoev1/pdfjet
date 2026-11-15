/*
 * Example_70.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.ArrayList;
import java.util.List;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * An incident report form to print and fill in by hand, kept in the first
 * aid room of a warehouse: on the first page, who reports, when and where it
 * happened, what happened, who was hurt, the witnesses and what was done
 * right away, and the signature; on the second, the follow-up that the
 * supervisor fills in.
 * <p>
 * This is a form, not a finished document: the employee and then the
 * supervisor fill it in. It is drawn as the job application of Example_72
 * is, a label above each box, with larger boxes where the answer takes a
 * few lines. The form is two pages, each with its form number and "Page 1
 * of 2" at the foot. The pages are made detached and kept in a list, then
 * added to the document at the end, once their number is known; a third
 * page added to the form needs no change to the footers.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title and the supervisor's section are
 * headings, the labels and the answers are text, and the boxes and the
 * footers are artifacts. The layout is that of the incident report template of PDFjet
 * Forms. The company is made up.
 * </p>
 *
 * @see Page#DETACHED
 * @see RadioButton
 * @see CheckBox
 */
public class Example_70 {
    static final String COMPANY = "Granite Bay Logistics";
    static final String FORM_NUMBER = "Form HS-04, revised September 2026";

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
    static final float THIRD = (WIDTH - 2f * GAP) / 3f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static final float BOX_HEIGHT = 24f;
    static final float LINE_HEIGHT = 20f;

    private Page page;
    private final Font labelFont;
    private final Font textFont;

    public Example_70() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_70.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Incident report");
        pdf.setAuthor(COMPANY);
        pdf.setLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.setSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.setSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.setSize(10f);

        List<Page> pages = new ArrayList<Page>();

        // The first page, for the person who reports
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        heading("Incident report", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Report any accident, injury, damage or near "
                + "miss as soon as you can, while the details are fresh.");
        intro.setLineSpacing(1.15f);
        intro.setLocation(LEFT, 74f);
        intro.setWidth(WIDTH);
        float y = intro.drawOn(page)[1] + 12f;
        rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .setTextColor(RED)
                .setLocation(LEFT, y + 20f)
                .drawOn(page);

        y += 40f;
        field("Your name *", LEFT, y, HALF, 1, null);
        y = field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);
        field("Date of the incident *", LEFT, y, THIRD, 1, null);
        field("Time", LEFT + THIRD + GAP, y, THIRD, 1, null);
        y = field("Where it happened *", LEFT + 2f * (THIRD + GAP), y, THIRD, 1, null);

        // The kind of incident, one of four, in a row under its label
        label("What happened? *", LEFT, y);
        float x = LEFT;
        for (String kind : new String[] {"Injury", "Damage to property", "Near miss", "Other"}) {
            x = new RadioButton(textFont, kind).setLocation(x, y + 18f).drawOn(page)[0] + 18f;
        }
        y += 48f;

        y = field("Describe what happened, step by step *", LEFT, y, WIDTH, 4, null);
        y = field("Was anyone hurt? Who, and how?", LEFT, y, WIDTH, 2, null);
        y = checkBox("First aid or medical care was given", y);
        y = field("Witnesses: names and phone numbers", LEFT, y + 10f, WIDTH, 2, null);
        y = field("What was done right away?", LEFT, y, WIDTH, 2, null);
        y = checkBox("I confirm this report is true to the best of my knowledge "
                + "and sign below. *", y);
        signature("Signature *", "Date *", y + 10f);

        // The second page, for the supervisor
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        y = heading("For the supervisor", StructElem.H2, 14f, 62f);
        field("Received by", LEFT, y, HALF, 1, null);
        y = field("Date received", LEFT + HALF + GAP, y, HALF, 1, null);
        y = field("What caused it?", LEFT, y, WIDTH, 3, null);
        y = field("What will be done so that it does not happen again, by whom and by when?",
                LEFT, y, WIDTH, 4, null);
        label("Reported to", LEFT, y);
        y += 18f;
        for (String to : new String[] {"The health and safety committee", "Our insurer",
                "The owner of the building"}) {
            y = checkBox(to, y);
        }
        y = field("Closed on", LEFT, y + 10f, THIRD, 1, null);
        signature("Supervisor's signature", "Date", y);

        // The footer of each page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            Page p = pages.get(i);
            TextLine form = new TextLine(textFont, COMPANY + ", " + FORM_NUMBER);
            form.setFontSize(9f);
            form.setTextColor(Color.dimgray);
            form.setLocation(LEFT, p.getHeight() - 36f);
            TextLine number = new TextLine(textFont, "Page " + (i + 1) + " of " + pages.size());
            number.setFontSize(9f);
            number.setTextColor(Color.dimgray);
            number.setLocation(LEFT + WIDTH - number.getWidth(), p.getHeight() - 36f);
            p.addArtifactBMC();
            form.drawOn(p);
            number.drawOn(p);
            p.addEMC();
        }
        pdf.addPages(pages);
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

    // Draws a check box and its text, its top at y, and returns where the
    // next one starts.
    private float checkBox(String text, float y) throws Exception {
        new CheckBox(textFont, text)
                .setBorderColor(BOX)
                .setLocation(LEFT, y)
                .drawOn(page);
        return y + LINE_HEIGHT;
    }

    // Draws the signature and the date beside it, 60 and 40 percent of the row.
    private void signature(String text, String date, float y) throws Exception {
        field(text, LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        field(date, LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);
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
        new Example_70();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_70 => %4d ms%n", time1 - time0);
    }
}   // End of Example_70.java
