using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_39.cs
 *
 * Draws a bar chart with horizontal bars: one series, the value written at
 * the end of each bar.
 */
public class Example_39 {
    public Example_39() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_39.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.SetSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        BarChart chart = new BarChart(f1, f2);
        chart.SetLocation(70f, 50f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("Longest rivers");
        chart.SetXAxisTitle("Length in km");
        chart.SetCategories("Nile", "Amazon", "Yangtze", "Mississippi", "Yenisei", "Yellow River");
        chart.AddSeries("", new float[] {6650f, 6400f, 6300f, 6275f, 5539f, 5464f}, Color.steelblue);
        chart.SetHorizontal(true);
        chart.SetDrawValueLabels(true);
        chart.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_39();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_39", time0, time1);
    }
}   // End of Example_39.cs
