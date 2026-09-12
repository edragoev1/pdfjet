using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_18.cs
 * This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public Example_18() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_18.pdf", FileMode.Create)));

        Font font = new Font(pdf, IBMPlexSans.Regular);
        float fontSize = 14f;

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        Rect rect = new Rect(50f, 50f, 100f, 100f);
        rect.SetFillColor(Color.red);
        rect.DrawOn(page);
        pages.Add(page);

        page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);
        rect = new Rect(50f, 50f, 100f, 100f);
        rect.SetFillColor(Color.green);
        rect.DrawOn(page);
        pages.Add(page);

        page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);
        rect = new Rect(50f, 50f, 100f, 100f);
        rect.SetFillColor(Color.blue);
        rect.DrawOn(page);
        pages.Add(page);

        for (int i = 0; i < pages.Count; i++) {
            page = pages[i];
            String footer = "Page " + (i + 1) + " of " + pages.Count;
            page.SetBrushColor(Color.black);
            page.DrawString(
                    font,
                    fontSize,
                    footer,
                    (page.GetWidth() - font.StringWidth(fontSize, footer))/2f,
                    (page.GetHeight() - 3f*fontSize/2f));
        }
        pdf.AddPages(pages);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_18();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_18", time0, time1);
    }

}   // End of Example_18.cs
