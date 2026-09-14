using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_40.cs
 *
 * Draws a bar chart with vertical bars: two series grouped by month, with a
 * legend under the title.
 */
public class Example_40 {
    public Example_40() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_40.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.SetSize(8f);

        BarChart chart = new BarChart(f1, f2);
        chart.SetLocation(70f, 50f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("Units sold by month");
        chart.SetXAxisTitle("Month");
        chart.SetYAxisTitle("Units");
        chart.SetCategories(
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec");
        chart.AddSeries("2025",
                new float[] {45f, 65f, 31f, 45f, 65f, 31f, 38f, 52f, 47f, 59f, 66f, 72f},
                Color.seagreen);
        chart.AddSeries("2026",
                new float[] {75f, 20f, 73f, 75f, 20f, 73f, 61f, 58f, 69f, 64f, 77f, 80f},
                Color.indianred);
        chart.SetGroupGap(0.4f);
        chart.SetBarGap(0.1f);
        chart.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_40();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_40", time0, time1);
    }
}   // End of Example_40.cs
