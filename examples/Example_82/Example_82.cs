/*
 * Example_82.cs
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
 * Example_82.cs
 * A time off request, to print and fill in by hand: the employee writes the
 * kind of leave, the first and the last day, and signs; the manager writes
 * the decision in a part of the form of its own, and signs too.
 *
 * This is a form people fill in, not a document with data. It is drawn as
 * Example_72 draws the job application, with the same field helper: the
 * label above its box, the boxes on a grid, and an asterisk on the label of
 * a field that is required. The kind of leave and the decision are radio
 * buttons, one of the options, under their label.
 *
 * Two people fill in the form, one after the other, so the part of the
 * manager is set apart: a rule across the page and a heading of its own.
 * Its fields have no asterisk, as the employee leaves them empty.
 *
 * The document is PDF/UA-1: the title and the two parts are headings,
 * the labels and the options are text, and the boxes are artifacts. The
 * layout is that of the time off request template of PDFjet Forms. The
 * company is made up.
 */
public class Example_82 {
    private const String COMPANY = "Northwind Studio";
    private static readonly String[] KINDS_OF_LEAVE = {
        "Vacation", "Sick leave", "Personal day", "Parental leave", "Other",
    };

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
    private const float QUARTER = (HALF - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    // The signature box and the date box beside it, 60 and 40 percent
    private const float SIGNATURE = 0.6f * WIDTH - GAP / 2f;
    private const float DATE = 0.4f * WIDTH - GAP / 2f;

    private readonly Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_82() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_82.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Time off request");
        pdf.SetAuthor(COMPANY);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, when to send the form, and a rule
        Heading("Time off request", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Fill in this form at least two weeks before "
                + "your time off, sign it and give it to your manager.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // The employee's part
        y = Heading("Your request", StructElem.H2, 14f, y + 46f);
        Field("Your name *", LEFT, y, HALF, 1, null);
        y = Field("Department", LEFT + HALF + GAP, y, HALF, 1, null);
        y = Choice("Type of leave *", LEFT, y, KINDS_OF_LEAVE);
        Field("First day *", LEFT, y, QUARTER, 1, null);
        Field("Last day *", LEFT + QUARTER + GAP, y, QUARTER, 1, null);
        Field("Working days", LEFT + HALF + GAP, y, QUARTER, 1, null);
        y = Field("Phone while away", LEFT + HALF + GAP + QUARTER + GAP, y, QUARTER, 1, null);
        y = Field("Notes for your manager", LEFT, y, WIDTH, 2, null);

        new CheckBox(textFont, "I confirm this request and sign below. *")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        y += 30f;
        Field("Your signature *", LEFT, y, SIGNATURE, 1, null);
        y = Field("Date *", LEFT + SIGNATURE + GAP, y, DATE, 1, null);

        // The manager's part, below a rule, filled in after the employee's
        Rule(y + 12f);
        y = Heading("For your manager", StructElem.H2, 14f, y + 42f);
        y = Choice("Decision", LEFT, y, "Approved", "Not approved");
        y = Field("Manager's name", LEFT, y, HALF, 1, null);
        new CheckBox(textFont, "I confirm this decision and sign below.")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        y += 30f;
        Field("Manager's signature", LEFT, y, SIGNATURE, 1, null);
        Field("Date of decision", LEFT + SIGNATURE + GAP, y, DATE, 1, null);

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

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_82();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_82 => {time1 - time0,4} ms");
    }
}   // End of Example_82.cs
