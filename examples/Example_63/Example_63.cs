/*
 * Example_63.cs
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
 * Example_63.cs
 * An appointment request form to print and fill in by hand, for the front
 * desk of a small accounting office: the name and how to reach the client,
 * what the appointment is for, two dates that suit, how to meet, and room
 * for a note; at the foot, on a shade, the part the office fills in when it
 * books the time.
 *
 * This is a form, not a finished document: the client fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box, and
 * the questions with a few answers are radio buttons. Those answers are laid
 * out by a helper that measures each button first, with DrawOn(null), and
 * starts a new line when the next one would pass the width of its field. So
 * the four answers of "What is it for?" wrap to two lines in half the page,
 * and the row under it starts below the taller of the two fields beside each
 * other.
 *
 * The document is PDF/UA-1: the title and the three sections are headings,
 * the labels and the answers are text, and the boxes are artifacts. The
 * layout is that of the appointment request template of PDFjet Forms. The
 * office is made up.
 */
public class Example_63 {
    private const String OFFICE = "Maple Lane Accounting";
    private const String CONTACT = "Maple Lane Accounting, 85 Mill Street, Elora, ON N0B 1S0. "
            + "Phone 519-555-0142, or email appointments@example.com.";

    private const int BLUE = 0x1f58b0;
    private const int RED = 0xb00020;
    private const int RULE = 0xdde1e8;
    private const int BOX = 0x4d4d4d;
    private const int SHADE = 0xf1f3f6;

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

    public Example_63() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_63.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Appointment request");
        pdf.SetAuthor(OFFICE);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what happens to the form, and a rule
        Heading("Book an appointment", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Tell us when suits you, and we will confirm "
                + "by email or phone within one working day.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // Who you are and how to reach you
        y = Heading("Your details", StructElem.H2, 14f, y + 46f);
        y = Field("Full name *", LEFT, y, WIDTH, 1, null);
        Field("Email *", LEFT, y, HALF, 1, null);
        y = Field("Phone", LEFT + HALF + GAP, y, HALF, 1, null);

        // The appointment. The two choices share a row, and the next row
        // starts under the one that took more lines.
        y = Heading("The appointment", StructElem.H2, 14f, y + 18f);
        float end = Choice("What is it for? *", LEFT, y, HALF,
                "First consultation", "Follow-up", "Review of a document", "Other");
        y = Math.Max(end, Choice("Preferred time", LEFT + HALF + GAP, y, HALF,
                "Morning", "Afternoon", "Evening"));
        Field("Preferred date *", LEFT, y, HALF, 1, null);
        y = Field("Another date that works", LEFT + HALF + GAP, y, HALF, 1, null);
        y = Choice("How would you like to meet?", LEFT, y, WIDTH,
                "In person", "Video call", "Phone call");
        y = Field("Anything we should know before we meet", LEFT, y, WIDTH, 3, null);

        new CheckBox(textFont, "Send me a reminder the day before")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);

        // The office fills in the rest when it books the time, so its part is
        // at the foot of the page, on a shade that sets it apart. The shade
        // is a Rect, and a Rect is drawn as an artifact.
        float office = page.GetHeight() - 132f;
        new Rect(LEFT - 8f, office - 8f, WIDTH + 16f, 84f).SetFillColor(SHADE).DrawOn(page);
        Heading("For office use", StructElem.H2, 12f, office + 8f);
        float third = (WIDTH - 2f * GAP) / 3f;
        Field("Booked for", LEFT, office + 18f, third, 1, null);
        Field("With", LEFT + third + GAP, office + 18f, third, 1, null);
        Field("Confirmed by, and how", LEFT + 2f * (third + GAP), office + 18f, third, 1, null);

        // How to reach the office, at the foot of the page
        TextLine contact = new TextLine(textFont, CONTACT);
        contact.SetFontSize(9f);
        contact.SetTextColor(Color.dimgray);
        contact.SetLocation(LEFT, page.GetHeight() - 36f);
        contact.DrawOn(page);

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

    // Draws a question of a few answers: its label at y and a radio button for
    // each answer under it, left to right. A button that would pass the width
    // goes to the start of the next line; DrawOn(null) measures it without
    // drawing it. Returns where the next row starts.
    private float Choice(String text, float x, float y, float width, params String[] answers) {
        Label(text, x, y);
        float top = y + 18f;
        float left = x;
        foreach (String answer in answers) {
            RadioButton button = new RadioButton(textFont, answer);
            float right = button.SetLocation(left, top).DrawOn(null)[0];
            if (right > x + width && left > x) {
                left = x;
                top += LINE_HEIGHT;
                button.SetLocation(left, top);
            }
            left = button.DrawOn(page)[0] + 12f;
        }
        return top + LINE_HEIGHT + 10f;
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
        new Example_63();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_63 => {time1 - time0,4} ms");
    }
}   // End of Example_63.cs
