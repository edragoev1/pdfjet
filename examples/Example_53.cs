using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_53.cs
 */
public class Example_53 {
    public Example_53(String fileName) {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_53.pdf", FileMode.Create)));

        FileStream fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        List<PDFobj> objects = pdf.Read(fis);
        List<PDFobj> pages = pdf.GetPageObjects(objects);
        for (int i = 0; i < pages.Count; i++) {
            Page page = new Page(pdf, pages[i]);
            page.DrawLine(0f, 0f, 200f, 200f);
            page.Complete(objects);
        }
        pdf.AddObjects(objects);
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_53("../testPDFs/cairo-graphics-1.pdf");
        // new Example_53("../testPDFs/cairo-graphics-2.pdf");
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_53", time0, time1);
    }
}
