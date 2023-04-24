using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_18.cs
 *  This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public Example_18() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_18.pdf", FileMode.Create)));

        Font font = new Font(pdf, "fonts/RedHatText/RedHatText-Regular.ttf.stream");
        font.SetSize(12f);

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        Box box = new Box();
        box.SetLocation(50f, 50f);
        box.SetSize(100.0f, 100.0f);
        box.SetColor(Color.red);
        box.SetFillShape(true);
        box.DrawOn(page);
        pages.Add(page);

        page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);
        box = new Box();
        box.SetLocation(50f, 50f);
        box.SetSize(100.0f, 100.0f);
        box.SetColor(Color.green);
        box.SetFillShape(true);
        box.DrawOn(page);
        pages.Add(page);

        page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);
        box = new Box();
        box.SetLocation(50f, 50f);
        box.SetSize(100.0f, 100.0f);
        box.SetColor(Color.blue);
        box.SetFillShape(true);
        box.DrawOn(page);
        pages.Add(page);

        int numOfPages = pages.Count;
        for (int i = 0; i < numOfPages; i++) {
            page = pages[i];
            String footer = "Page " + (i + 1) + " of " + numOfPages;
            page.SetBrushColor(Color.black);
            page.DrawString(
                    font,
                    footer,
                    (page.GetWidth() - font.StringWidth(footer))/2f,
                    (page.GetHeight() - 5f));
        }

        for (int i = 0; i < numOfPages; i++) {
            pdf.AddPage(pages[i]);
        }

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
