using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

using PDFjet.NET;

/**
 * Example_47.cs
 */
public class Example_47 {
    public Example_47() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_47.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(14f);

        // Use Regex.Split to match Java's split("\\n\\n") regex behavior
        List<string> paragraphs = new List<string>(
                Regex.Split(Content.OfTextFile("data/dostoevsky.txt"), "\\n\\n"));

        float x = 50f;
        float y = 50f;
        float w = 230f;
        float h = 500f;
        float gap = 20f;

        Page page = null;
        TextFrame textFrame = new TextFrame(f1, paragraphs);
        while (textFrame.HasMoreText()) {
            page = new Page(pdf, Letter.LANDSCAPE);

            textFrame.SetLocation(x, y);
            textFrame.SetWidth(w);
            textFrame.SetHeight(h);
            textFrame.DrawOn(page);

            if (textFrame.HasMoreText()) {
                x += w + gap;
                textFrame.SetLocation(x, y);
                textFrame.SetWidth(w);
                textFrame.SetHeight(h);
                textFrame.DrawOn(page);
            }

            if (textFrame.HasMoreText()) {
                x += w + gap;
                textFrame.SetLocation(x, y);
                textFrame.SetWidth(w);
                textFrame.SetHeight(h);
                textFrame.DrawOn(page);
            }

            x = 50f;
            y = 50f;
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_47();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_47", time0, time1);
    }
}   // End of Example_47.cs
