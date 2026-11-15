/*
 * Example_81.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using PDFjet.NET;

/**
 * Example_81.cs
 * A rental application, two pages to print and fill in by hand: the
 * property, the applicant and where they live now, who will live in the
 * property, their work and income, two references, and the agreement to
 * sign, with "Page 1 of 2" at the bottom of each page.
 *
 * This is a form people fill in, not a document with data. It is drawn as
 * Example_72 draws the job application, with the same field helper: the
 * label above its box, the boxes on a grid of two or three columns, and an
 * asterisk on the label of a field that is required. The choices, as the
 * length of the lease or the pets, are radio buttons under their label.
 *
 * The landlord prints the form for one property, so its address and its
 * rent are filled in already, in the dark red of the answers of the
 * template, apart from the black of the form; the rent is a decimal
 * written with CultureInfo.InvariantCulture. The second page starts with a
 * section: a form is broken between its sections, never inside one.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * labels and the options are text, and the boxes and the page numbers are
 * artifacts. The layout is that of the rental application template of
 * PDFjet Forms. The landlord and the address are made up.
 */
public class Example_81 {
    private const String LANDLORD = "Cedar Row Rentals";
    private const String RETURN_TO = "Please return both pages to Cedar Row Rentals, "
            + "40 Woolwich Street, Guelph, ON N1H 3V1, or scan them to rent@example.com.";

    // The property the form is printed for
    private const String PROPERTY = "Unit 4, 118 Elm Street, Guelph, ON N1H 3T2";
    private const decimal RENT = 1850.00m;

    private const int INK = 0x333333;           // the headings
    private const int MAROON = 0x800000;         // the answers printed on the form
    private const int RED = 0xb00020;
    private const int RULE = 0xdde1e8;
    private const int BOX = 0x4d4d4d;

    // The grid: the margin, the width of the page between the margins, the
    // gap between two fields of a row, and the widths of two and three fields
    private const float LEFT = 48f;
    private const float WIDTH = 516f;
    private const float GAP = 12f;
    private const float HALF = (WIDTH - GAP) / 2f;
    private const float THIRD = (WIDTH - 2 * GAP) / 3f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    private const int PAGES = 2;

    private Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_81() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_81.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Rental application");
        pdf.SetAuthor(LANDLORD);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        // Page 1: the title, the property and the applicant
        page = new Page(pdf, Letter.PORTRAIT);
        Heading("Rental application", StructElem.H1, 20f, 62f);
        float y = Paragraph("Thank you for your interest in the property. Fill in both pages, "
                + "sign at the end and send the form back to us; we answer within three "
                + "working days.", 74f) + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        y = Heading("The property", StructElem.H2, 14f, y + 46f);
        y = Field("Address of the property *", LEFT, y, WIDTH, 1, PROPERTY);
        Field("Move-in date *", LEFT, y, HALF, 1, null);
        y = Field("Monthly rent", LEFT + HALF + GAP, y, HALF, 1,
                "$" + RENT.ToString("N2", CultureInfo.InvariantCulture));
        y = Choice("Length of the lease", LEFT, y, "12 months", "6 months", "Month to month");

        y = Heading("About you", StructElem.H2, 14f, y + 18f);
        Field("First name *", LEFT, y, HALF, 1, null);
        y = Field("Last name *", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("Email *", LEFT, y, HALF, 1, null);
        y = Field("Phone *", LEFT + HALF + GAP, y, HALF, 1, null);
        y = Field("Current address *", LEFT, y, WIDTH, 2, null);
        y = Choice("How long have you lived there?", LEFT, y,
                "Less than a year", "1 to 3 years", "More than 3 years");
        Field("Current landlord's name", LEFT, y, HALF, 1, null);
        Field("Current landlord's phone", LEFT + HALF + GAP, y, HALF, 1, null);
        Footer(1);

        // Page 2: the household, the income, the references and the agreement
        page = new Page(pdf, Letter.PORTRAIT);
        y = Heading("Who will live there", StructElem.H2, 14f, 62f);
        Field("Adults *", LEFT, y, THIRD, 1, null);
        y = Field("Children", LEFT + THIRD + GAP, y, THIRD, 1, null);
        y = Choice("Pets", LEFT, y, "No pets", "Cat", "Dog", "Other");
        y = Field("About your pets, if any", LEFT, y, WIDTH, 1, null);

        y = Heading("Work and income", StructElem.H2, 14f, y + 18f);
        Field("Employer", LEFT, y, HALF, 1, null);
        y = Field("Position", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("Years there", LEFT, y, THIRD, 1, null);
        Field("Monthly income", LEFT + THIRD + GAP, y, THIRD, 1, null);
        y = Field("Employer's phone", LEFT + 2 * (THIRD + GAP), y, THIRD, 1, null);

        y = Heading("References", StructElem.H2, 14f, y + 18f);
        y = Paragraph("Two people who know you, not related to you: a former landlord, "
                + "an employer or a colleague.", y) + 10f;
        foreach (String which in new String[] {"First", "Second"}) {
            Field(which + " reference", LEFT, y, THIRD, 1, null);
            Field("Phone", LEFT + THIRD + GAP, y, THIRD, 1, null);
            y = Field("How they know you", LEFT + 2 * (THIRD + GAP), y, THIRD, 1, null);
        }

        y = Heading("Agreement", StructElem.H2, 14f, y + 18f);
        y = Paragraph("I confirm that what I have written in this application is true, and "
                + "I allow you to contact my landlord, my employer and my references to "
                + "check it. Sending this application does not commit either of us to a "
                + "lease.", y) + 12f;
        new CheckBox(textFont, "I have read and agree to the above and sign below. *")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        y += 30f;
        Field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        Field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.SetFontSize(9f);
        returnTo.SetTextColor(Color.dimgray);
        returnTo.SetLocation(LEFT, page.GetHeight() - 52f);
        returnTo.DrawOn(page);
        Footer(2);

        pdf.Complete();
    }

    // Draws a heading in gray, its baseline at y, and returns where the field
    // under it starts.
    private float Heading(String text, StructElem level, float size, float y) {
        TextLine heading = new TextLine(labelFont, text);
        heading.SetStructureType(level);
        heading.SetFontSize(size);
        heading.SetTextColor(INK);
        heading.SetLocation(LEFT, y);
        heading.DrawOn(page);
        return y + 16f;
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns where it ended.
    private float Paragraph(String text, float y) {
        TextBlock block = new TextBlock(textFont, text);
        block.SetLineSpacing(1.15f);
        block.SetLocation(LEFT, y);
        block.SetWidth(WIDTH);
        return block.DrawOn(page)[1];
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
            line.SetTextColor(MAROON);
            line.SetLocation(x + 6f, top + 16f);
            line.DrawOn(page);
        }
        return top + height + 12f;
    }

    // Draws a question of a few answers: its label at y and a radio button
    // for each answer under it, where a box would be, each starting where the
    // one before it ended. DrawOn returns the end of a button with some room
    // after its text already. Returns where the next row starts.
    private float Choice(String text, float x, float y, params String[] answers) {
        Label(text, x, y);
        foreach (String answer in answers) {
            x = new RadioButton(textFont, answer).SetLocation(x, y + 20f).DrawOn(page)[0] + 12f;
        }
        return y + 50f;
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

    // The page number at the foot of the page, as an artifact: it is not
    // part of the form, and a screen reader skips it.
    private void Footer(int number) {
        TextLine footer = new TextLine(textFont, "Page " + number + " of " + PAGES);
        footer.SetFontSize(10f);
        footer.SetLocation(LEFT + WIDTH - footer.GetWidth(), page.GetHeight() - 36f);
        page.AddArtifactBMC();
        footer.DrawOn(page);
        page.AddEMC();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_81();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_81 => {time1 - time0,4} ms");
    }
}   // End of Example_81.cs
