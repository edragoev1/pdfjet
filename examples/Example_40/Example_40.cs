using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_40.cs
 *
 * Draws two bar charts with vertical bars from the same data: the two series
 * grouped by month, and the same series stacked, with a legend under each title.
 */
public class Example_40 {
    public Example_40() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_40.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(8f);

        String[] months = {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        float[] units2025 = {45f, 65f, 31f, 45f, 65f, 31f, 38f, 52f, 47f, 59f, 66f, 72f};
        float[] units2026 = {75f, 20f, 73f, 75f, 20f, 73f, 61f, 58f, 69f, 64f, 77f, 80f};

        BarChart chart = new BarChart(f1, f2);
        chart.SetLocation(70f, 50f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("Units sold by month");
        chart.SetXAxisTitle("Month");
        chart.SetYAxisTitle("Units");
        chart.SetCategories(months);
        chart.AddSeries("2025", units2025, Color.seagreen);
        chart.AddSeries("2026", units2026, Color.indianred);
        chart.SetGroupGap(0.4f);
        chart.SetBarGap(0.1f);
        chart.DrawOn(page);

        BarChart stacked = new BarChart(f1, f2);
        stacked.SetLocation(70f, 400f);
        stacked.SetSize(500f, 300f);
        stacked.SetTitle("Units sold by month, stacked");
        stacked.SetXAxisTitle("Month");
        stacked.SetYAxisTitle("Units");
        stacked.SetCategories(months);
        stacked.AddSeries("2025", units2025, Color.seagreen);
        stacked.AddSeries("2026", units2026, Color.indianred);
        stacked.SetStacked(true);
        stacked.SetDrawValueLabels(true);
        stacked.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_40();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_40 => {time1 - time0,4} ms");
    }
}   // End of Example_40.cs
