/*
 * Example_60.cs
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
 * Example_60.cs
 * A certificate of completion of a workshop, on Letter paper turned on its
 * side: a band of color at the top and the bottom, the studio that gives it,
 * the title, the name of the participant on a line, what was completed, and
 * the lines of the instructor's signature and of the date.
 *
 * It is a document people receive, finished: everything on it is centered,
 * so each line is measured and placed half its width left of the middle of
 * the page, and the paragraph of what was completed is a TextBlock centered
 * in a column narrower than the page. The serif faces of IBM Plex give it
 * the look of a printed certificate; the name is the largest text after the
 * title, as it is what the reader looks for.
 *
 * The certificate is drawn by a method that takes the name, so that a
 * certificate for each person of a class is a loop over the list of names, a
 * page each in one document: the fonts are embedded once, however many
 * pages there are, and the file is printed in one go.
 *
 * The document is PDF/UA-1: the title is a heading, the bands and the lines
 * are artifacts, and the language is set. The layout is that of the
 * certificate template of PDFjet Forms. The people and the studio are made up.
 */
public class Example_60 {
    private const String ISSUER = "Northwind Studio";
    private const String TITLE = "Certificate of completion";
    private const String NAME = "Robin Brook";
    private const String WORKSHOP = "Design with PDF";
    private static readonly DateTime DATE = new DateTime(2026, 11, 14);
    private const String INSTRUCTOR = "Alex Morgan, instructor";

    private const int NAVY = 0x1d3557;
    private const int INK = 0x333333;
    private const int GRAY = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float BAND = 16f;              // The height of the bands

    private Font sans;
    private Font serif;
    private Font serifBold;
    private Font serifItalic;

    public Example_60() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_60.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle(TITLE);
        pdf.SetAuthor(ISSUER);
        pdf.SetLanguage("en-CA");

        sans = new Font(pdf, IBMPlexSans.SemiBold);
        serif = new Font(pdf, IBMPlexSerif.Regular);
        serifBold = new Font(pdf, IBMPlexSerif.SemiBold);
        serifItalic = new Font(pdf, IBMPlexSerif.Italic);

        // One certificate. For a class, a page each from the list of names:
        // foreach (String name in names) {
        //     DrawCertificate(new Page(pdf, Letter.LANDSCAPE), name);
        // }
        DrawCertificate(new Page(pdf, Letter.LANDSCAPE), NAME);

        pdf.Complete();
    }

    // Draws the certificate of the person on the page.
    private void DrawCertificate(Page page, String name) {
        float width = page.GetWidth();
        float inner = width - 2 * MARGIN;       // The width between the margins
        float middle = width / 2f;

        // The bands at the top and the bottom. A Rect is decoration, an
        // artifact, on its own.
        new Rect(MARGIN, MARGIN, inner, BAND).SetFillColor(NAVY).DrawOn(page);
        new Rect(MARGIN, page.GetHeight() - MARGIN - BAND, inner, BAND)
                .SetFillColor(NAVY).DrawOn(page);

        // Who gives it, and the title, the heading of the page
        Center(page, sans, 13f, ISSUER, GRAY, 112f).DrawOn(page);
        TextLine title = Center(page, serifBold, 38f, TITLE, NAVY, 170f);
        title.SetStructureType(StructElem.H1);
        title.DrawOn(page);

        // The name, on a line as wide as three fifths of the page
        Center(page, serifItalic, 15f, "This certifies that", INK, 214f).DrawOn(page);
        Center(page, serifBold, 30f, name, INK, 264f).DrawOn(page);
        float lineWidth = 0.6f * inner;
        Rule(page, middle - lineWidth / 2f, middle + lineWidth / 2f, 286f);

        // What was completed, in a column of seven tenths of the page, each
        // line centered in it
        String date = DATE.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        float columnWidth = 0.7f * inner;
        TextBlock what = new TextBlock(serif, "has completed " + WORKSHOP + ", a hands-on workshop "
                + "of one day, on " + date + ", in Port Elgin, Ontario: layouts that print the "
                + "same everywhere, fillable forms, and accessible PDF.");
        what.SetFontSize(14f);
        what.SetTextColor(INK);
        what.SetLineSpacing(1.15f);
        what.SetTextAlignment(Alignment.CENTER);
        what.SetLocation(middle - columnWidth / 2f, 306f);
        what.SetWidth(columnWidth);
        float y = what.DrawOn(page)[1] + 64f;

        // The signature and the date, each on its line, a little over a third
        // of the page wide, in the left and the right half of the page
        float signWidth = 0.35f * inner;
        float[] starts = {MARGIN + 0.1f * inner, MARGIN + 0.55f * inner};
        String[] captions = {INSTRUCTOR, date};
        for (int i = 0; i < 2; i++) {
            float x = starts[i];
            Rule(page, x, x + signWidth, y);
            TextLine caption = new TextLine(serif, captions[i]);
            caption.SetFontSize(11f);
            caption.SetTextColor(GRAY);
            caption.SetLocation(x + (signWidth - caption.GetWidth()) / 2f, y + 22f);
            caption.DrawOn(page);
        }
    }

    // Returns a line of text centered on the page, its baseline at y, for the
    // caller to draw, after it has made it a heading where it is one.
    private static TextLine Center(Page page, Font font, float size, String text, int color, float y) {
        TextLine line = new TextLine(font, text);
        line.SetFontSize(size);
        line.SetTextColor(color);
        line.SetLocation((page.GetWidth() - line.GetWidth()) / 2f, y);
        return line;
    }

    // A line to write or sign on, decoration only, so an artifact that a
    // screen reader skips.
    private static void Rule(Page page, float x1, float x2, float y) {
        Line line = new Line(x1, y, x2, y);
        line.SetStrokeWidth(0.75f);
        line.SetStrokeColor(INK);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_60();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_60 => {time1 - time0,4} ms");
    }
}   // End of Example_60.cs
