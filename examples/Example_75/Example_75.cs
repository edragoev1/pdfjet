/*
 * Example_75.cs
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
 * Example_75.cs
 * A membership application of a community garden, to print and fill in by
 * hand: the name, the address and how to reach the member, the kind of
 * membership with its fee, what they would like to take part in, the
 * newsletter and the rules to agree to, and a box to sign in.
 *
 * This is a form people fill in, not a document with data: the boxes are
 * empty. It is drawn as Example_72 draws the job application, with the same
 * field helper: the label above its box, the boxes on a grid of columns so
 * that the rows line up, 24 points tall for a line of handwriting, and an
 * asterisk on the label of a field that is required.
 *
 * The kinds of membership are radio buttons, one of three, and their fees
 * come from the data of the club, so that a change of fee is made in one
 * place and the form printed again. The options of a
 * choice are placed one after the other, each where the one before it ended,
 * so a longer option moves the next one along.
 *
 * The document is PDF/UA-1: the title and the sections are headings, the
 * labels and the options are text, and the boxes are artifacts. The layout
 * is that of the membership application template of PDFjet Forms. The garden
 * is made up.
 */
public class Example_75 {
    private const String CLUB = "Riverbend Community Garden";
    private const String RETURN_TO = "Please bring the form to the garden shed on a Saturday "
            + "morning, or mail it to 85 Mill Road, Elora, ON N0B 1S0.";

    // The kinds of membership and their fees for a year, in dollars
    private static readonly String[] KINDS = {"Individual", "Family", "Student"};
    private static readonly decimal[] FEES = {
        40m, 70m, 20m,
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

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    private readonly Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_75() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_75.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Membership application");
        pdf.SetAuthor(CLUB);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what to do with the form, and a rule
        Heading("Membership application", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Join " + CLUB + " for a year. Fill in this "
                + "form and send it back with your first payment.");
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
        Field("First name *", LEFT, y, HALF, 1, null);
        y = Field("Last name *", LEFT + HALF + GAP, y, HALF, 1, null);
        y = Field("Street address", LEFT, y, WIDTH, 1, null);
        // City, province and postal code: 40, 30 and 30 percent of the row
        float city = 0.4f * (WIDTH - 2 * GAP);
        float rest = 0.3f * (WIDTH - 2 * GAP);
        Field("City", LEFT, y, city, 1, null);
        Field("Province or state", LEFT + city + GAP, y, rest, 1, null);
        y = Field("Postal code", LEFT + city + GAP + rest + GAP, y, rest, 1, null);
        Field("Email *", LEFT, y, HALF, 1, null);
        y = Field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The membership: its kind, one of three, with the fee of each
        y = Heading("Your membership", StructElem.H2, 14f, y + 18f);
        Label("Membership *", LEFT, y);
        float x = LEFT + 96f;
        for (int i = 0; i < KINDS.Length; i++) {
            String option = KINDS[i] + ", $" + FEES[i].ToString(CultureInfo.InvariantCulture);
            x = new RadioButton(textFont, option).SetLocation(x, y - 1f).DrawOn(page)[0] + 24f;
        }
        y += 30f;
        y = Field("Start date *", LEFT, y, HALF, 1, null);
        y = Field("What would you like to take part in?", LEFT, y, WIDTH, 2, null);

        // The newsletter, the rules, and the signature under them
        new CheckBox(textFont, "Send me the monthly newsletter by email")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        new CheckBox(textFont, "I agree to the members' rules and sign below. *")
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
        new Example_75();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_75 => {time1 - time0,4} ms");
    }
}   // End of Example_75.cs
