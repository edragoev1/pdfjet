package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_40.java
 *
 * Draws two bar charts with vertical bars from the same data: the two series
 * grouped by month, and the same series stacked, with a legend under each title.
 */
final public class Example_40 {
    public Example_40() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_40.pdf")));

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
        stacked.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_40();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_40 => %4d ms%n", time1 - time0);
    }
}   // End of Example_40.java
