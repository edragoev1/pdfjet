/*
 * Example_40.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.time.DayOfWeek;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_40.java
 *
 * Draws two bar charts with vertical bars from the same data: the two series
 * grouped by month, and the same series stacked, with a legend under each title.
 * The second page is the calendar of 2026, a CalendarMonth for each month, with
 * the weeks starting on Monday.
 */
final public class Example_40 {
    public Example_40() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_40.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Units sold by month");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(8f);

        String[] months = {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        float[] units2025 = {45f, 65f, 31f, 45f, 65f, 31f, 38f, 52f, 47f, 59f, 66f, 72f};
        float[] units2026 = {75f, 20f, 73f, 75f, 20f, 73f, 61f, 58f, 69f, 64f, 77f, 80f};

        BarChart chart = new BarChart(f1, f2);
        chart.setLocation(70f, 50f);
        chart.setSize(500f, 300f);
        chart.setTitle("Units sold by month");
        chart.setXAxisTitle("Month");
        chart.setYAxisTitle("Units");
        chart.setCategories(months);
        chart.addSeries("2025", units2025, Color.seagreen);
        chart.addSeries("2026", units2026, Color.indianred);
        chart.setGroupGap(0.4f);
        chart.setBarGap(0.1f);
        chart.setAltDescription("Units sold by month in 2025 and 2026, side by side: from 31 to 72 a month in 2025 and from 20 to 80 in 2026, the most in December in both years.");
        chart.drawOn(page);

        BarChart stacked = new BarChart(f1, f2);
        stacked.setLocation(70f, 400f);
        stacked.setSize(500f, 300f);
        stacked.setTitle("Units sold by month, stacked");
        stacked.setXAxisTitle("Month");
        stacked.setYAxisTitle("Units");
        stacked.setCategories(months);
        stacked.addSeries("2025", units2025, Color.seagreen);
        stacked.addSeries("2026", units2026, Color.indianred);
        stacked.setStacked(true);
        stacked.setDrawValueLabels(true);
        stacked.setAltDescription("Units sold by month in 2025 and 2026, stacked: from 85 a month, in February and May, to 152 in December.");
        stacked.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(f1, "Calendar 2026").setFontSize(14f).setLocation(50f, 45f).drawOn(page);
        String[] monthNames = {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"};
        for (int i = 0; i < 12; i++) {
            float x = 50f + (i % 3)*182f;
            float y = 65f + (i / 3)*177f;
            new TextLine(f1, monthNames[i]).setLocation(x, y + 12f).drawOn(page);
            CalendarMonth calendar = new CalendarMonth(f1, f2, 2026, i + 1);
            calendar.setFirstDayOfWeek(DayOfWeek.MONDAY);
            calendar.setCellWidth(21f);
            calendar.setCellHeight(21f);
            calendar.setLocation(x, y + 18f);
            calendar.drawOn(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_40();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_40 => %4d ms%n", time1 - time0);
    }
}   // End of Example_40.java
