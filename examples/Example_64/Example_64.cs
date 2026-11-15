/*
 * Example_64.cs
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
 * Example_64.cs
 * A new client form to print and fill in by hand, that a design studio hands
 * to a client before the first meeting: who the client is, what the project
 * needs, the budget, the dates, and what the project should achieve.
 *
 * This is a form, not a finished document: the client fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * two questions of several answers, the kind of work and the budget, are
 * lists of radio buttons, one answer a line, side by side; the list of five
 * answers is longer than the list of four, so the row under them starts
 * where the longer one ended. The last answer, "Something else", has a line
 * after it to write what, drawn from where the button ended to the edge of
 * its column.
 *
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels and the answers are text, and the boxes and the write-in line
 * are artifacts. The layout is that of the client intake template of PDFjet
 * Forms. The studio is made up.
 */
public class Example_64 {
    private const String STUDIO = "Tidewater Studio";
    private const String RETURN_TO = "Please bring the form to our first meeting at 240 Front "
            + "Street, Kingston, or scan it to hello@example.com.";

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

    public Example_64() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_64.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("New client form");
        pdf.SetAuthor(STUDIO);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, why the studio asks, and a rule
        Heading("New client form", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Tell us about you and your project, so that "
                + "we can prepare for our first meeting. It takes about five minutes.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // About you
        y = Heading("About you", StructElem.H2, 14f, y + 46f);
        Field("Full name *", LEFT, y, HALF, 1, null);
        y = Field("Company", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("Email *", LEFT, y, HALF, 1, null);
        y = Field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // Your project. The two lists are side by side, and the row under
        // them starts below the longer one.
        y = Heading("Your project", StructElem.H2, 14f, y + 18f);
        float end = Choice("What do you need? *", LEFT, y, HALF,
                "Brand identity", "Website", "Print design", "Photography", "Something else");
        y = Math.Max(end, Choice("Budget", LEFT + HALF + GAP, y, HALF,
                "Under $1,000", "$1,000 to $5,000", "$5,000 to $15,000", "Over $15,000"));
        Field("Ideal start date", LEFT, y, HALF, 1, null);
        y = Field("Deadline", LEFT + HALF + GAP, y, HALF, 1, null);
        y = Field("What should the project achieve? *", LEFT, y, WIDTH, 4, null);
        y = Field("How did you hear about us?", LEFT, y, WIDTH, 1, null);

        new CheckBox(textFont, "Send me occasional news about our work")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);

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

    // Draws a question of several answers: its label at y and a radio button
    // for each answer under it, one a line. An answer that starts with
    // "Something else" gets a line after it, to write what. Returns where the
    // next row starts.
    private float Choice(String text, float x, float y, float width, params String[] answers) {
        Label(text, x, y);
        float top = y + 18f;
        foreach (String answer in answers) {
            float[] end = new RadioButton(textFont, answer).SetLocation(x, top).DrawOn(page);
            if (answer.StartsWith("Something else")) {
                Line line = new Line(end[0], end[1], x + width, end[1]);
                line.SetStrokeColor(BOX);
                line.SetStrokeWidth(0.5f);
                page.AddArtifactBMC();
                line.DrawOn(page);
                page.AddEMC();
            }
            top += LINE_HEIGHT;
        }
        return top + 10f;
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
        new Example_64();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_64 => {time1 - time0,4} ms");
    }
}   // End of Example_64.cs
