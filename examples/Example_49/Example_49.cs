/*
 * Example_49.cs
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
 * Example_49.cs
 * This example draws a menu with paragraphs that mix fonts, sizes and colors,
 * currency signs raised with a vertical offset, and a rotated, underlined label.
 */
public class Example_49 {
    public Example_49() {
        PDF pdf = new PDF(new BufferedStream(
            new FileStream("Example_49.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Paragraphs with mixed text styles");

        Font f1 = new Font(pdf, SourceSerif4.Regular);
        f1.SetSize(14f);

        Font f2 = new Font(pdf, SourceSerif4.Italic);
        f2.SetSize(14f);

        Font f3 = new Font(pdf, SourceSerif4.SemiBold);
        f3.SetSize(14f);

        Font f4 = new Font(pdf, SourceSerif4.SemiBold);
        f4.SetSize(9f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(f3, "Café Menu");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(28f);
        title.SetLocation(70f, 100f);
        title.DrawOn(page);

        // Each paragraph mixes a name, an italic description and a price
        // with a small dollar sign raised by a vertical offset.
        String[] names = {"Espresso", "Cappuccino", "Hot chocolate"};
        String[] notes = {"rich and intense", "with steamed milk foam", "made with dark cocoa"};
        String[] prices = {"3.25", "4.50", "3.95"};

        TextColumn column = new TextColumn();
        for (int i = 0; i < names.Length; i++) {
            Paragraph paragraph = new Paragraph()
                    .Add(new TextLine(f3, names[i]))
                    .Add(new TextLine(f2, notes[i]).SetTextColor(Color.gray))
                    .Add(new TextLine(f4, "$").SetVerticalOffset(-4f))
                    .Add(new TextLine(f1, prices[i]).SetTextColor(Color.darkred));
            column.AddParagraph(paragraph);
        }

        // A paragraph that colors some of its words, aligned to the right.
        column.AddParagraph(new Paragraph()
                .Add(new TextLine(f2, "Freshly"))
                .Add(new TextLine(f3, "roasted").SetTextColor(Color.saddlebrown))
                .Add(new TextLine(f2, "every"))
                .Add(new TextLine(f3, "morning").SetTextColor(Color.darkorange))
                .SetTextAlignment(Alignment.RIGHT));

        column.SetLocation(70f, 140f);
        column.SetWidth(470f);
        column.SetParagraphSpacing(1.8f);
        float[] xy = column.DrawOn(page);

        // A TextFrame wraps the words of its paragraphs to its width.
        List<Paragraph> paragraphs = new List<Paragraph>();
        paragraphs.Add(new Paragraph()
                .Add(new TextLine(f1, "Our beans come from small farms in"))
                .Add(new TextLine(f3, "Colombia,"))
                .Add(new TextLine(f3, "Ethiopia"))
                .Add(new TextLine(f1, "and"))
                .Add(new TextLine(f3, "Guatemala,"))
                .Add(new TextLine(f1, "and we roast them in small batches."))
                .Add(new TextLine(f2, "Ask us about the beans of the week.").SetTextColor(Color.darkred)));
        paragraphs.Add(new Paragraph()
                .Add(new TextLine(f2, "Prices include tax.").SetTextColor(Color.gray)));

        TextFrame frame = new TextFrame(paragraphs);
        frame.SetLocation(70f, xy[1] + 30f);
        frame.SetWidth(470f);
        frame.DrawOn(page);

        TextLine label = new TextLine(f3, "Today's special!");
        label.SetFontSize(18f);
        label.SetTextColor(Color.red);
        label.SetLocation(400f, 90f);
        label.SetTextRotation(15);
        label.SetUnderline(true);
        label.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_49();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_49 => {time1 - time0,4} ms");
    }
}   // End of Example_49.cs
