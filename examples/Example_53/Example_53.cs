/*
 * Example_53.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using PDFjet.NET;

/**
 * Example_53.cs
 * Paragraphs written with the inline markup of Markdown: **bold**, *italic*,
 * `code` and [links](url), each drawn in its own font by Markup, with the
 * punctuation after a word in another style next to it. The paragraphs flow
 * in a text frame, and the list is a list of paragraphs with labels.
 */
public class Example_53 {
    public Example_53() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_53.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Inline markup");

        Font regular = new Font(pdf, IBMPlexSans.Regular).SetSize(11f);
        Font bold = new Font(pdf, IBMPlexSans.Bold).SetSize(11f);
        Font italic = new Font(pdf, IBMPlexSans.Italic).SetSize(11f);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic).SetSize(11f);
        Font code = new Font(pdf, IBMPlexMono.Regular).SetSize(10f);
        Font heading = new Font(pdf, IBMPlexSans.SemiBold);
        Markup markup = new Markup(regular, bold, italic, boldItalic, code);

        List<Paragraph> paragraphs = new List<Paragraph>();
        paragraphs.Add(new Paragraph(new TextLine(heading, "Inline markup").SetFontSize(22f))
                .SetStructureType(StructElem.H1));
        paragraphs.AddRange(markup.Paragraphs(
                "**Markup** reads the inline markup of Markdown and draws each part in its own "
                + "font: **bold**, *italic*, ***bold italic***, `code` and "
                + "[links](https://pdfjet.com). A word keeps the punctuation after it, as in "
                + "*this*, and a mark with no match, such as the one in 2 * 3, is text.\n"
                + "\n"
                + "A backslash makes a mark text too: \\*not italic\\*. Code keeps its marks "
                + "as they are, as in `a*b*c`, and a link can have emphasis in it: "
                + "[the **PDFjet** repository](https://github.com/edragoev1/pdfjet)."));

        String[] items = {
            "`Markup.paragraph` makes one paragraph of a text.",
            "`Markup.paragraphs` makes one of each part between the empty lines.",
            "The paragraphs go in a **TextFrame** or a **TextColumn**, as any others do.",
        };
        for (int i = 0; i < items.Length; i++) {
            paragraphs.Add(markup.Paragraph(items[i])
                    .SetListLabel(new TextLine(regular, (i + 1) + "."), 16f));
        }

        TextFrame frame = new TextFrame(paragraphs);
        frame.SetLocation(70f, 70f);
        frame.SetWidth(470f);
        frame.SetParagraphGap(8f);
        List<Page> pages = new List<Page>();
        frame.DrawOn(pdf, pages, Letter.PORTRAIT);
        pdf.AddPages(pages);
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_53();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_53 => {time1 - time0,4} ms");
    }
}   // End of Example_53.cs
