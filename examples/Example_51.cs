using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;

/**
 *  Example_51.cs
 *
 */
public class Example_51 {
    public Example_51(String fileNumber) {
        MemoryStream buf1 = new MemoryStream();
        PDF pdf = new PDF(buf1);
        Page page = new Page(pdf, Letter.PORTRAIT);

        Box box = new Box();
        box.SetLocation(50f, 50f);
        box.SetSize(100.0f, 100.0f);
        box.SetColor(Color.red);
        box.SetFillShape(true);
        box.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        box = new Box();
        box.SetLocation(50f, 50f);
        box.SetSize(100.0f, 100.0f);
        box.SetColor(Color.green);
        box.SetFillShape(true);
        box.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        box = new Box();
        box.SetLocation(50f, 50f);
        box.SetSize(100.0f, 100.0f);
        box.SetColor(Color.blue);
        box.SetFillShape(true);
        box.DrawOn(page);

        pdf.Complete();

        BufferedStream buf2 = new BufferedStream(new FileStream(
                "Example_" + fileNumber + ".pdf", FileMode.Open, FileAccess.Write));
        AddFooterToPDF(buf1, buf2);
    }

    public void AddFooterToPDF(MemoryStream buf, Stream outputStream) {
        PDF pdf = new PDF(outputStream);
        List<PDFobj> objects = pdf.Read(new MemoryStream(buf.ToArray()));

        Font font = new Font(objects,
                new FileStream("fonts/Droid/DroidSans.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read), Font.STREAM);
        font.SetSize(12f);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        for (int i = 0; i < pages.Count; i++) {
            String footer = "Page " + (i + 1) + " of " + pages.Count;
            Page page = new Page(pdf, pages[i]);
            page.AddResource(font, objects);
            page.SetBrushColor(Color.transparent);  // Required!
            page.SetBrushColor(Color.black);
            page.DrawString(
                    font,
                    footer,
                    (page.GetWidth() - font.StringWidth(footer))/2f,
                    (page.GetHeight() - 5f));
            page.Complete(objects);
        }
        pdf.AddObjects(objects);
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_51("51");
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine("Example_51 => " + (time1 - time0));
    }
}   // End of Example_51.cs
