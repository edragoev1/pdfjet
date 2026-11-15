/*
 * Example_70.cs
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
 * Example_70.cs
 * An incident report form to print and fill in by hand, kept in the first
 * aid room of a warehouse: on the first page, who reports, when and where it
 * happened, what happened, who was hurt, the witnesses and what was done
 * right away, and the signature; on the second, the follow-up that the
 * supervisor fills in.
 *
 * This is a form, not a finished document: the employee and then the
 * supervisor fill it in. It is drawn as the job application of Example_72
 * is, a label above each box, with larger boxes where the answer takes a
 * few lines. The form is two pages, each with its form number and "Page 1
 * of 2" at the foot. The pages are made detached and kept in a list, then
 * added to the document at the end, once their number is known; a third
 * page added to the form needs no change to the footers.
 *
 * The document is PDF/UA-1: the title and the supervisor's section are
 * headings, the labels and the answers are text, and the boxes and the
 * footers are artifacts. The layout is that of the incident report template of PDFjet
 * Forms. The company is made up.
 */
public class Example_70 {
    private const String COMPANY = "Granite Bay Logistics";
    private const String FORM_NUMBER = "Form HS-04, revised September 2026";

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
    private const float THIRD = (WIDTH - 2f * GAP) / 3f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    private Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_70() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_70.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Incident report");
        pdf.SetAuthor(COMPANY);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        List<Page> pages = new List<Page>();

        // The first page, for the person who reports
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        Heading("Incident report", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Report any accident, injury, damage or near "
                + "miss as soon as you can, while the details are fresh.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        y += 40f;
        Field("Your name *", LEFT, y, HALF, 1, null);
        y = Field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);
        Field("Date of the incident *", LEFT, y, THIRD, 1, null);
        Field("Time", LEFT + THIRD + GAP, y, THIRD, 1, null);
        y = Field("Where it happened *", LEFT + 2f * (THIRD + GAP), y, THIRD, 1, null);

        // The kind of incident, one of four, in a row under its label
        Label("What happened? *", LEFT, y);
        float x = LEFT;
        foreach (String kind in new String[] {"Injury", "Damage to property", "Near miss", "Other"}) {
            x = new RadioButton(textFont, kind).SetLocation(x, y + 18f).DrawOn(page)[0] + 18f;
        }
        y += 48f;

        y = Field("Describe what happened, step by step *", LEFT, y, WIDTH, 4, null);
        y = Field("Was anyone hurt? Who, and how?", LEFT, y, WIDTH, 2, null);
        y = CheckBox("First aid or medical care was given", y);
        y = Field("Witnesses: names and phone numbers", LEFT, y + 10f, WIDTH, 2, null);
        y = Field("What was done right away?", LEFT, y, WIDTH, 2, null);
        y = CheckBox("I confirm this report is true to the best of my knowledge "
                + "and sign below. *", y);
        Signature("Signature *", "Date *", y + 10f);

        // The second page, for the supervisor
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        y = Heading("For the supervisor", StructElem.H2, 14f, 62f);
        Field("Received by", LEFT, y, HALF, 1, null);
        y = Field("Date received", LEFT + HALF + GAP, y, HALF, 1, null);
        y = Field("What caused it?", LEFT, y, WIDTH, 3, null);
        y = Field("What will be done so that it does not happen again, by whom and by when?",
                LEFT, y, WIDTH, 4, null);
        Label("Reported to", LEFT, y);
        y += 18f;
        foreach (String to in new String[] {"The health and safety committee", "Our insurer",
                "The owner of the building"}) {
            y = CheckBox(to, y);
        }
        y = Field("Closed on", LEFT, y + 10f, THIRD, 1, null);
        Signature("Supervisor's signature", "Date", y);

        // The footer of each page, now that their number is known
        for (int i = 0; i < pages.Count; i++) {
            Page p = pages[i];
            TextLine form = new TextLine(textFont, COMPANY + ", " + FORM_NUMBER);
            form.SetFontSize(9f);
            form.SetTextColor(Color.dimgray);
            form.SetLocation(LEFT, p.GetHeight() - 36f);
            TextLine number = new TextLine(textFont, "Page " + (i + 1) + " of " + pages.Count);
            number.SetFontSize(9f);
            number.SetTextColor(Color.dimgray);
            number.SetLocation(LEFT + WIDTH - number.GetWidth(), p.GetHeight() - 36f);
            p.AddArtifactBMC();
            form.DrawOn(p);
            number.DrawOn(p);
            p.AddEMC();
        }
        pdf.AddPages(pages);
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

    // Draws a check box and its text, its top at y, and returns where the
    // next one starts.
    private float CheckBox(String text, float y) {
        new CheckBox(textFont, text)
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        return y + LINE_HEIGHT;
    }

    // Draws the signature and the date beside it, 60 and 40 percent of the row.
    private void Signature(String text, String date, float y) {
        Field(text, LEFT, y, 0.6f * WIDTH - GAP / 2f, 1, null);
        Field(date, LEFT + 0.6f * WIDTH + GAP / 2f, y, 0.4f * WIDTH - GAP / 2f, 1, null);
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
        new Example_70();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_70 => {time1 - time0,4} ms");
    }
}   // End of Example_70.cs
