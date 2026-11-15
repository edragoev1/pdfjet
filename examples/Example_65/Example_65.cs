/*
 * Example_65.cs
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
 * Example_65.cs
 * A photo and video consent form to print and sign, handed out at the door
 * of an event: how the organizer may use the photos a guest appears in,
 * whether the guest may be named, a line for a parent who signs for a child,
 * and the name, the date and the signature.
 *
 * This is a form, not a finished document: the guest fills it in. It is
 * drawn as the job application of Example_72 is, a label above each box. The
 * organizer prints it for one event, so the event is filled in already: the
 * field helper draws a value in its box when it is given one. The answers to
 * "How may we use them?" are long, so they are radio buttons one under the
 * other, and what changing one's mind means is a paragraph, a TextBlock that
 * wraps to the width and returns where it ended. The name and the date share
 * a row two thirds to one third, and the child's name is as wide as the
 * name, so the boxes line up.
 *
 * The document is PDF/UA-1: the title and the two sections are headings,
 * the labels, the answers and the paragraph are text, and the boxes are
 * artifacts. The layout is that of the consent form template of PDFjet
 * Forms. The organization and the event are made up.
 */
public class Example_65 {
    private const String ORGANIZER = "Lakeside Arts Collective";
    private const String ADDRESS = "Lakeside Arts Collective, 18 Shore Road, Collingwood, "
            + "ON L9Y 1A1. Please hand the form in at the door.";
    private const String EVENT = "Winter concert, Saturday, December 12, 2026";
    private const String CHANGE_OF_MIND = "You can change your mind at any time by writing to "
            + "photos@example.com. We then stop using the photos and videos you appear in, and "
            + "take them off our website and social media within two weeks; copies already "
            + "printed stay as they are.";

    private const int BLUE = 0x1f58b0;
    private const int RED = 0xb00020;
    private const int RULE = 0xdde1e8;
    private const int BOX = 0x4d4d4d;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row. This form's rows are split two
    // thirds to one third.
    private const float LEFT = 48f;
    private const float WIDTH = 516f;
    private const float GAP = 12f;
    private const float TWO_THIRDS = (WIDTH - GAP) * 2f / 3f;
    private const float ONE_THIRD = (WIDTH - GAP) / 3f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    private readonly Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_65() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_65.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Photo and video consent");
        pdf.SetAuthor(ORGANIZER);
        pdf.SetLanguage("en-CA");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        page = new Page(pdf, Letter.PORTRAIT);

        // The title, what the form is about, and a rule
        Heading("Photo and video consent", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "During the event we take photos and short "
                + "videos. Tell us how we may use those you appear in.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // The consent: the event, printed in, and the use of the photos
        y = Heading("Your consent", StructElem.H2, 14f, y + 46f);
        y = Field("Event", LEFT, y, TWO_THIRDS, 1, EVENT);
        y = Choice("How may we use them? *", LEFT, y,
                "Anywhere: our website, social media and print",
                "Only inside our organization",
                "Not at all");
        y = CheckBox("You may name me beside a photo of me", y);
        y = CheckBox("I sign for a child in my care", y);
        y = Field("Child's name, if you sign for one", LEFT, y + 10f, TWO_THIRDS, 1, null);

        TextBlock change = new TextBlock(textFont, CHANGE_OF_MIND);
        change.SetLineSpacing(1.15f);
        change.SetLocation(LEFT, y);
        change.SetWidth(WIDTH);
        y = change.DrawOn(page)[1] + 12f;

        // The signature, under the confirmation it signs
        y = Heading("Your signature", StructElem.H2, 14f, y + 18f);
        Field("Full name *", LEFT, y, TWO_THIRDS, 1, null);
        y = Field("Date *", LEFT + TWO_THIRDS + GAP, y, ONE_THIRD, 1, null);
        y = CheckBox("I agree to the above and sign below. *", y);
        Field("Signature *", LEFT, y + 10f, TWO_THIRDS, 1, null);

        // Who the organizer is, at the foot of the page
        TextLine organizer = new TextLine(textFont, ADDRESS);
        organizer.SetFontSize(9f);
        organizer.SetTextColor(Color.dimgray);
        organizer.SetLocation(LEFT, page.GetHeight() - 36f);
        organizer.DrawOn(page);

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
    // for each answer under it, one a line. Returns where the next row starts.
    private float Choice(String text, float x, float y, params String[] answers) {
        Label(text, x, y);
        float top = y + 18f;
        foreach (String answer in answers) {
            new RadioButton(textFont, answer).SetLocation(x, top).DrawOn(page);
            top += LINE_HEIGHT;
        }
        return top + 10f;
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
        new Example_65();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_65 => {time1 - time0,4} ms");
    }
}   // End of Example_65.cs
