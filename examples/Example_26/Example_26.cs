/*
 * Example_26.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_26.cs
 * This example draws a survey with check boxes and radio buttons.
 */
public class Example_26 {
    public Example_26() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_26.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x = 70f;
        float y = 90f;

        TextLine text = new TextLine(f2, "Customer Survey");
        text.SetFontSize(22f);
        text.SetLocation(x, y);
        text.DrawOn(page);

        // Check boxes, one below the other.
        y += 50f;
        new TextLine(f2, "Which PDFjet ports do you use?").SetLocation(x, y).DrawOn(page);

        y += 15f;
        new CheckBox(f1, "Java")
                .SetLocation(x, y)
                .SetCheckmarkColor(Color.blue)
                .Check(Mark.CHECK)
                .DrawOn(page);

        y += 25f;
        new CheckBox(f1, "C#")
                .SetLocation(x, y)
                .SetCheckmarkColor(Color.blue)
                .Check(Mark.CHECK)
                .DrawOn(page);

        y += 25f;
        new CheckBox(f1, "Swift")
                .SetLocation(x, y)
                .DrawOn(page);

        y += 25f;
        new CheckBox(f1, "Go")
                .SetLocation(x, y)
                .DrawOn(page);

        // Radio buttons in a row. Each one starts where the one before it ends.
        y += 50f;
        new TextLine(f2, "How did you hear about PDFjet?").SetLocation(x, y).DrawOn(page);

        y += 15f;
        float[] xy = new RadioButton(f1, "Web search")
                .SetLocation(x, y)
                .Select(true)
                .DrawOn(page);

        xy = new RadioButton(f1, "A colleague")
                .SetLocation(xy[0] + 20f, y)
                .DrawOn(page);

        new RadioButton(f1, "Other")
                .SetLocation(xy[0] + 20f, y)
                .DrawOn(page);

        y += 50f;
        new TextLine(f2, "Would you recommend PDFjet?").SetLocation(x, y).DrawOn(page);

        y += 15f;
        xy = new RadioButton(f1, "Yes")
                .SetLocation(x, y)
                .Select(true)
                .DrawOn(page);

        new RadioButton(f1, "No")
                .SetLocation(xy[0] + 20f, y)
                .DrawOn(page);

        // A check box marked with an X, and one with a link.
        y += 50f;
        new TextLine(f2, "Stay in touch").SetLocation(x, y).DrawOn(page);

        y += 15f;
        new CheckBox(f1, "Send me news about new releases")
                .SetLocation(x, y)
                .SetCheckmarkColor(Color.red)
                .Check(Mark.X)
                .DrawOn(page);

        y += 25f;
        xy = new CheckBox(f1, "Visit https://pdfjet.com")
                .SetLocation(x, y)
                .SetURIAction("https://pdfjet.com")
                .DrawOn(page);

        // A border around the survey.
        Rect rect = new Rect(50f, 50f, 512f, xy[1] + 25f - 50f);
        rect.SetBorderColor(Color.lightgray);
        rect.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_26();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_26 => {time1 - time0,4} ms");
    }
}   // End of Example_26.cs
