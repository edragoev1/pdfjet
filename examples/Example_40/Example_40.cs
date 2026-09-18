/*
 * Example_40.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_40.cs
 *
 * Draws two bar charts with vertical bars from the same data: the two series
 * grouped by month, and the same series stacked, with a legend under each title.
 * The second page is the calendar of 2026, a CalendarMonth for each month, with
 * the weeks starting on Monday.
 */
public class Example_40 {
    public Example_40() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_40.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Units sold by month");

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
        chart.SetAltDescription("Units sold by month in 2025 and 2026, side by side: from 31 to 72 a month in 2025 and from 20 to 80 in 2026, the most in December in both years.");
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
        stacked.SetAltDescription("Units sold by month in 2025 and 2026, stacked: from 85 a month, in February and May, to 152 in December.");
        stacked.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(f1, "Calendar 2026").SetFontSize(14f).SetLocation(50f, 45f).DrawOn(page);
        String[] monthNames = {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"};
        for (int i = 0; i < 12; i++) {
            float x = 50f + (i % 3)*182f;
            float y = 65f + (i / 3)*177f;
            new TextLine(f1, monthNames[i]).SetLocation(x, y + 12f).DrawOn(page);
            CalendarMonth calendar = new CalendarMonth(f1, f2, 2026, i + 1);
            calendar.SetFirstDayOfWeek(DayOfWeek.Monday);
            calendar.SetCellWidth(21f);
            calendar.SetCellHeight(21f);
            calendar.SetLocation(x, y + 18f);
            calendar.DrawOn(page);
        }

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
