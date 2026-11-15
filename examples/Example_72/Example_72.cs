/*
 * Example_72.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PDFjet.NET;

/**
 * Example_72.cs
 * A job application form to print and fill in by hand: a box under each
 * label, larger boxes for the address and the experience, radio buttons for
 * the hours, check boxes for the consent, and a box to sign in.
 *
 * The form is drawn, not interactive: PDFjet draws text, lines and boxes, and
 * makes no fields that a viewer types into. That is the form most people
 * meet, on a clipboard at the front desk; it can also be filled in on screen
 * with the tools of a PDF viewer that type or sign on a page. So the boxes are
 * sized for handwriting, 24 points, about a third of an inch, tall, and each
 * label is above its box, where it stays readable once the box is filled. A
 * required field says so in its label, with an asterisk, as the note at the
 * top explains.
 *
 * The employer prints the form for one opening, so the position is filled in
 * already: the field helper draws a value in its box when it is given one.
 * The fields are placed on a grid of two columns with a gap between them, so
 * that the boxes of the rows line up.
 *
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels and the options are text, and the boxes are artifacts. The
 * layout is that of the job application template of PDFjet Forms. The
 * company is made up.
 */
public class Example_72 {
    private const String COMPANY = "Northwind Studio";
    private const String RETURN_TO = "Please return the form to Northwind Studio, "
            + "12 Harbour Street, Port Elgin, ON N0H 2C0, or scan it to jobs@example.com.";
    private const String POSITION = "Junior graphic designer (opening 2026-17)";

    private const int BLUE = 0x1f58b0;
    private const int RED = 0xb00020;
    private const int RULE = 0xdde1e8;
    private const int BOX = 0x4d4d4d;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    private const float LEFT = 48f;
    private const float WIDTH = 516f;
    private const float GAP = 12f;
    private const float HALF = (WIDTH - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    private readonly Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_72() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_72.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Job application");
        pdf.SetAuthor(COMPANY);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what happens to the form, and a rule
        Heading("Job application", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, COMPANY + " thanks you for your interest in working "
                + "with us. We read every application and answer within two weeks.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // Your contact details
        y = Heading("Your contact details", StructElem.H2, 14f, y + 46f);
        Field("First name *", LEFT, y, HALF, 1, null);
        y = Field("Last name *", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("Email *", LEFT, y, HALF, 1, null);
        y = Field("Phone *", LEFT + HALF + GAP, y, HALF, 1, null);
        y = Field("Street address", LEFT, y, WIDTH, 2, null);
        float quarter = (HALF - GAP) / 2f;
        Field("City", LEFT, y, HALF, 1, null);
        Field("State or province", LEFT + HALF + GAP, y, quarter, 1, null);
        y = Field("Postal code", LEFT + HALF + GAP + quarter + GAP, y, quarter, 1, null);

        // The position
        y = Heading("The position", StructElem.H2, 14f, y + 18f);
        Field("Position you apply for *", LEFT, y, HALF, 1, POSITION);
        y = Field("Available from", LEFT + HALF + GAP, y, HALF, 1, null);

        // The hours, one of three, beside their label: each button starts
        // where the one before it ended
        Label("Hours", LEFT, y);
        float x = LEFT + 70f;
        foreach (String option in new String[] {"Full time", "Part time", "Either"}) {
            x = new RadioButton(textFont, option).SetLocation(x, y - 1f).DrawOn(page)[0] + 24f;
        }
        y += 30f;

        y = Field("Your experience for this position *", LEFT, y, WIDTH, 4, null);

        // The consent, and the signature under it
        new CheckBox(textFont, "You may keep my application for other positions for six months")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        new CheckBox(textFont, "I confirm the above is correct and sign below. *")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);
        y += 50f;
        Field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        Field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        // Where the form goes, at the foot of the page
        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.SetFontSize(9f);
        returnTo.SetTextColor(Color.dimgray);
        returnTo.SetLocation(LEFT, page.GetHeight() - 36f);
        returnTo.DrawOn(page);

        pdf.Complete();
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    private float Heading(String text, StructElem level, float size, float y) {
        TextLine heading = new TextLine(labelFont, text);
        heading.SetStructureType(level);
        heading.SetFontSize(size);
        heading.SetTextColor(BLUE);
        heading.SetLocation(LEFT, y);
        heading.DrawOn(page);
        return y + 16f;
    }

    // Draws the label of a field, its top at y.
    private void Label(String text, float x, float y) {
        TextLine label = new TextLine(labelFont, text);
        label.SetLocation(x, y + labelFont.GetAscent());
        label.DrawOn(page);
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    private float Field(String text, float x, float y, float width, int lines, String value) {
        Label(text, x, y);
        float top = y + 16f;
        float height = BOX_HEIGHT + (lines - 1) * LINE_HEIGHT;
        Rect box = new Rect(x, top, width, height);
        box.SetBorderColor(BOX);
        box.SetBorderWidth(0.5f);
        box.DrawOn(page);
        if (value != null) {
            TextLine line = new TextLine(textFont, value);
            line.SetTextColor(BLUE);
            line.SetLocation(x + 6f, top + 16f);
            line.DrawOn(page);
        }
        return top + height + 12f;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private void Rule(float y) {
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_72();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_72 => {time1 - time0,4} ms");
    }
}   // End of Example_72.cs
