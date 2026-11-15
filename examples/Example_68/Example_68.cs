/*
 * Example_68.cs
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
 * Example_68.cs
 * A customer feedback card to print and fill in by hand, left on the counter
 * of a bicycle shop: the date of the visit and what it was for, a rating of
 * 1 to 5 for the quality, the service, the value and the visit overall,
 * whether the customer would recommend the shop, and two boxes for comments.
 *
 * This is a form, not a finished document: the customer fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * ratings are a grid: a row for each question, with five radio buttons
 * labeled 1 to 5 in columns of the same width, under a line that says what
 * 1 and 5 mean. The columns are placed by a constant, not by the width of
 * the labels, so the buttons of all the rows line up, and a thin line
 * between the rows, an artifact, keeps the eye on the row.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * questions and the answers are text, and the boxes and the lines are
 * artifacts. The layout is that of the customer feedback template of PDFjet
 * Forms. The shop is made up.
 */
public class Example_68 {
    private const String SHOP = "Harbor Lane Bicycles";
    private const String RETURN_TO = "Leave the card at the counter, or mail it to Harbor Lane "
            + "Bicycles, 72 Harbor Lane, Mystic, CT 06355. Thank you.";

    // The questions of the rating grid, each rated from 1 to 5
    private static readonly String[] RATINGS = {
        "Quality of the work or the product",
        "Service: friendly, clear, on time",
        "Value for money",
        "Your visit overall *",
    };

    private const int BLUE = 0x1f58b0;
    private const int RED = 0xb00020;
    private const int RULE = 0xdde1e8;
    private const int BOX = 0x4d4d4d;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    private const float LEFT = 44f;
    private const float WIDTH = 524f;
    private const float GAP = 12f;
    private const float HALF = (WIDTH - GAP) / 2f;

    // The rating grid: where its first column of buttons starts, and how
    // far apart the columns are
    private const float SCALE_LEFT = LEFT + 250f;
    private const float SCALE_STEP = 54f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    private readonly Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_68() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_68.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Customer feedback");
        pdf.SetAuthor(SHOP);
        pdf.SetLanguage("en-US");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, a line on what it is for, and a rule
        Heading("How did we do?", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Two minutes of your time help us do better. "
                + "Every card is read by the owner.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(LEFT, y, WIDTH);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // Your visit: the date, and what it was for, any of three
        y = Heading("Your visit", StructElem.H2, 14f, y + 46f);
        float third = (WIDTH - 2f * GAP) / 3f;
        Field("Date of your visit", LEFT, y, third, 1, null);
        float x = LEFT + third + GAP;
        Label("What did you come for?", x, y);
        foreach (String reason in new String[] {"A purchase", "A repair or a service", "Advice"}) {
            x = new CheckBox(textFont, reason)
                    .SetBorderColor(BOX)
                    .SetLocation(x, y + 21f)
                    .DrawOn(page)[0] + 12f;
        }
        y += 64f;

        // Your ratings: what 1 and 5 mean, then a row for each question
        y = Heading("Your ratings", StructElem.H2, 14f, y + 18f);
        new TextLine(italic, "1 is poor, 5 is excellent.")
                .SetLocation(LEFT, y + italic.GetAscent())
                .DrawOn(page);
        y += 22f;
        foreach (String question in RATINGS) {
            new TextLine(textFont, question)
                    .SetLocation(LEFT, y + textFont.GetAscent())
                    .DrawOn(page);
            for (int i = 1; i <= 5; i++) {
                new RadioButton(textFont, i.ToString())
                        .SetLocation(SCALE_LEFT + (i - 1) * SCALE_STEP, y)
                        .DrawOn(page);
            }
            Rule(LEFT, y + 21f, WIDTH);
            y += 28f;
        }

        // Would you recommend us, in a row
        Label("Would you recommend us to a friend?", LEFT, y + 6f);
        x = LEFT;
        foreach (String answer in new String[] {"Yes, gladly", "Probably", "Not sure", "No"}) {
            x = new RadioButton(textFont, answer).SetLocation(x, y + 24f).DrawOn(page)[0] + 18f;
        }
        y += 56f;

        // In the customer's words
        y = Field("What did we do well?", LEFT, y, WIDTH, 2, null);
        y = Field("What could we do better?", LEFT, y, WIDTH, 2, null);
        new CheckBox(textFont, "You may quote me, without my name")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        new CheckBox(textFont, "Please contact me about my answers")
                .SetBorderColor(BOX)
                .SetLocation(LEFT + HALF + GAP, y)
                .DrawOn(page);
        y += 30f;
        Field("Name", LEFT, y, HALF, 1, null);
        Field("Email or phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // Where the card goes, at the foot of the page
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

    // A thin line, decoration only, so an artifact that a screen reader skips.
    private void Rule(float x, float y, float width) {
        Line line = new Line(x, y, x + width, y);
        line.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_68();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_68 => {time1 - time0,4} ms");
    }
}   // End of Example_68.cs
