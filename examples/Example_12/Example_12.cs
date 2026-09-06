using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using PDFjet.NET;

// Example_12.cs
public class Example_12 {
    public Example_12() {
        PDF pdf = new PDF(new FileStream("Example_12.pdf", FileMode.Create));
        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Page page = new Page(pdf, A4.PORTRAIT);

        List<String> lines = Text.ReadLines("data/Example_12.java");
        StringBuilder buf = new StringBuilder();
        foreach (String line in lines) {
            buf.Append(line);
            buf.Append("\r\n"); // CR and LF both required!
        }

        PDF417 barcode = new PDF417(buf.ToString());
        barcode.SetModuleWidth(0.5f);
        barcode.SetLocation(100f, 60f);
        barcode.DrawOn(page);

        TextLine text = new TextLine(f1, "PDF417 barcode containing the contents of data/Example_12.java");
        text.SetLocation(100f, 40f);
        text.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_12();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_12", time0, time1);
    }
}
