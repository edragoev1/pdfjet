/*
 * Example_59.cs
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
 * Example_59.cs
 * A business letter on Letter paper: the letterhead of the sender, the date,
 * the address of the recipient, the subject, the paragraphs of the letter,
 * the closing and the signature block, and a line for the enclosure.
 *
 * Each part is placed under the one before it: DrawOn returns where a
 * TextBlock ended, so a longer address or one more paragraph moves
 * everything below it down, and nothing is placed by hand twice. The
 * paragraphs are TextBlocks, which wrap the text to the width of the page,
 * with a blank line between them and no indent.
 *
 * The document is PDF/UA-1: the subject is the heading of the letter, the
 * rule under the letterhead is an artifact, and the language is set, so that
 * a screen reader reads the letter as it is written. The layout is that of
 * the business letter template of PDFjet Forms. The people and the companies
 * are made up.
 */
public class Example_59 {
    private const String SENDER = "Northwind Studio";
    private const String SENDER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";

    private static readonly DateTime DATE = new DateTime(2026, 9, 25);
    private const String RECIPIENT = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4";
    private const String SUBJECT = "Your annual report, accessible edition";
    private const String SALUTATION = "Dear Ms. Raman,";

    private static readonly String[] PARAGRAPHS = {
        "Thank you for choosing us for this year's annual report. The printed copies are on "
        + "their way, and the accessible PDF edition is on the USB drive enclosed.",

        "The PDF meets the PDF/UA standard, so screen readers read it in the right order, "
        + "with every picture described. If any of your readers has trouble with it, tell us "
        + "and we will fix it at no charge.",

        "Our invoice for the work is enclosed. We look forward to working with you again.",
    };

    private const String CLOSING = "Sincerely,";
    private const String SIGNER = "Dana Whitfield\nDesign Lead, Northwind Studio";
    private const String ENCLOSURE = "Enclosures: Invoice 2026-042, USB drive";

    private const int RULE = 0xdde1e8;

    // The space between the parts of the letter, and between its paragraphs
    private const float GAP = 14f;

    public Example_59() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_59.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle(SUBJECT);
        pdf.SetAuthor(SENDER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        float left = 48f;
        float width = page.GetWidth() - 2 * left;

        // The letterhead
        new TextLine(semiBold, SENDER).SetFontSize(16f).SetLocation(left, 64f).DrawOn(page);
        float y = Paragraph(page, regular, SENDER_ADDRESS, left, 80f, width);

        // A thin rule under the letterhead, decoration only
        y += GAP;
        Line rule = new Line(left, y, left + width, y);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The date, written out in words, so that 10/12 is not read as the
        // 10th of December
        String date = DATE.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        y = Paragraph(page, regular, date, left, y + 2 * GAP, width);

        // The recipient, as on the envelope
        y = Paragraph(page, regular, RECIPIENT, left, y + GAP, width);

        // The subject, the heading of the letter for a screen reader
        TextBlock subject = new TextBlock(semiBold, SUBJECT);
        subject.SetStructureType(StructElem.H1);
        subject.SetLocation(left, y + GAP);
        subject.SetWidth(width);
        y = subject.DrawOn(page)[1];

        // The body: the salutation and the paragraphs, a blank line between them
        y = Paragraph(page, regular, SALUTATION, left, y + GAP, width);
        foreach (String text in PARAGRAPHS) {
            y = Paragraph(page, regular, text, left, y + GAP, width);
        }

        // The closing, room to sign by hand, and the name under it
        y = Paragraph(page, regular, CLOSING, left, y + GAP, width);
        y = Paragraph(page, regular, SIGNER, left, y + 3 * GAP, width);

        Paragraph(page, regular, ENCLOSURE, left, y + 2 * GAP, width);

        pdf.Complete();
    }

    // Draws the text wrapped to the width, its top at y, and returns the y
    // where it ended, for the next part of the letter.
    private static float Paragraph(Page page, Font font, String text, float x, float y, float width) {
        TextBlock block = new TextBlock(font, text);
        block.SetLineSpacing(1.25f);
        block.SetLocation(x, y);
        block.SetWidth(width);
        return block.DrawOn(page)[1];
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_59();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_59 => {time1 - time0,4} ms");
    }
}   // End of Example_59.cs
