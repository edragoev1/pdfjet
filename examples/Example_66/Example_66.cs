/*
 * Example_66.cs
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
 * Example_66.cs
 * A contact details update form, mailed by a property manager to each of its
 * tenants: one page a tenant, with the name and the tenant number printed in,
 * the details on file shown above the boxes, and room for the new address,
 * email and phone, the date they apply from, and a signature.
 *
 * This is a form, not a finished document: the tenant fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * forms come from a list of tenants, the way a mail merge does: the same
 * code draws a page for each one, and the field helper draws the name and
 * the number in their boxes. What is on file is printed in gray, so the
 * tenant fills in only what has changed. The address row is split 40, 30 and
 * 30 percent, the signature row 60 and 40, with a gap between the fields.
 *
 * The document is PDF/UA-1: the title and the section of each page are
 * headings, the labels and the details are text, and the boxes are
 * artifacts. The layout is that of the contact details update template of
 * PDFjet Forms. The company and the tenants are made up.
 */
public class Example_66 {
    private const String COMPANY = "Oakridge Property Management";
    private const String RETURN_TO = "Please return the form in the envelope enclosed, or to the "
            + "office at 310 Queen Street, Fredericton, NB E3B 1B2.";

    // The tenants: the name, the tenant number, and the details on file. In
    // an application they come from the database.
    private static readonly String[][] TENANTS = {
        new String[] {"Maria Alvarez", "T-1042", "Unit 4, 55 Charlotte Street, Fredericton, NB E3B 1L4\n"
                + "maria.alvarez@example.com, 506-555-0117"},
        new String[] {"Samuel Okafor", "T-1057", "Unit 12, 55 Charlotte Street, Fredericton, NB E3B 1L4\n"
                + "s.okafor@example.com, 506-555-0183"},
        new String[] {"Hannah Lindqvist", "T-1103", "88 Waterloo Row, Fredericton, NB E3B 1Y9\n"
                + "hannah.l@example.com, 506-555-0129"},
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

    private Page page;
    private readonly Font labelFont;
    private readonly Font textFont;
    private readonly Font italic;

    public Example_66() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_66.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Contact details update");
        pdf.SetAuthor(COMPANY);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        foreach (String[] tenant in TENANTS) {
            page = new Page(pdf, Letter.PORTRAIT);
            DrawForm(tenant[0], tenant[1], tenant[2]);
        }
        pdf.Complete();
    }

    // Draws the form of one tenant on the page.
    private void DrawForm(String name, String number, String onFile) {
        // The title, what to fill in, and a rule
        Heading("Update your contact details", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Tell us what has changed. "
                + "Fill in only the details that are new.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // Who the form is for, printed in, and what is on file now
        y += 40f;
        Field("Full name *", LEFT, y, HALF, 1, name);
        y = Field("Tenant number", LEFT + HALF + GAP, y, HALF, 1, number);
        Label("On file now", LEFT, y);
        TextBlock current = new TextBlock(textFont, onFile);
        current.SetLineSpacing(1.15f);
        current.SetTextColor(Color.dimgray);
        current.SetLocation(LEFT, y + 16f);
        current.SetWidth(WIDTH);
        y = current.DrawOn(page)[1] + 12f;

        // The new details. The address row is split 40, 30 and 30 percent.
        y = Heading("Your new details", StructElem.H2, 14f, y + 18f);
        y = Field("Street address", LEFT, y, WIDTH, 1, null);
        float rest = WIDTH - 2f * GAP;
        Field("City", LEFT, y, 0.4f * rest, 1, null);
        Field("Province or state", LEFT + 0.4f * rest + GAP, y, 0.3f * rest, 1, null);
        y = Field("Postal code", LEFT + 0.7f * rest + 2f * GAP, y, 0.3f * rest, 1, null);
        Field("Email", LEFT, y, HALF, 1, null);
        y = Field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("These details apply from *", LEFT, y, HALF, 1, null);

        // The best way to reach the tenant, three answers in a row beside the date
        Label("Best way to reach you", LEFT + HALF + GAP, y);
        float x = LEFT + HALF + GAP;
        foreach (String answer in new String[] {"Email", "Phone", "Mail"}) {
            x = new RadioButton(textFont, answer).SetLocation(x, y + 21f).DrawOn(page)[0] + 18f;
        }
        y += 64f;

        // The confirmation, and the signature under it
        new CheckBox(textFont, "I confirm these details and sign below. *")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        y += 30f;
        Field("Signature *", LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        Field("Date *", LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);

        // Where the form goes, at the foot of the page
        TextLine returnTo = new TextLine(textFont, RETURN_TO);
        returnTo.SetFontSize(9f);
        returnTo.SetTextColor(Color.dimgray);
        returnTo.SetLocation(LEFT, page.GetHeight() - 36f);
        returnTo.DrawOn(page);
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
        new Example_66();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_66 => {time1 - time0,4} ms");
    }
}   // End of Example_66.cs
