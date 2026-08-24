using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_25.cs
 */
public class Example_25 {
    public Example_25() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_25.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(12.0f);
        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.SetSize(10.0f);

        DonutChart chart = new DonutChart(f1, f2, true);    // true = full donut (with hole)
        chart.SetLocation(300.0f, 400.0f);
        chart.SetR1AndR2(200.0f, 120.0f);

        chart.AddSlice(new Slice(90.0f,  0xC1121F, "Apples",   ""));    // deep red
        chart.AddSlice(new Slice(72.0f,  0x1D3557, "Oranges",  ""));    // navy blue
        chart.AddSlice(new Slice(108.0f, 0x1A7468, "Bananas",  ""));    // dark teal
        chart.AddSlice(new Slice(54.0f,  0xD97706, "Grapes",   ""));    // burnt orange
        chart.AddSlice(new Slice(36.0f,  0xCAAA2F, "Lemons",   ""));    // dark gold
        chart.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_25();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_25", time0, time1);
    }
}   // End of Example_25.cs
