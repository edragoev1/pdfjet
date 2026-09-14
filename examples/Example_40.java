package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_40.java
 *
 * Draws a bar chart with vertical bars: two series grouped by month, with a
 * legend under the title.
 */
final public class Example_40 {
    public Example_40() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_40.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.setSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.setSize(8f);

        BarChart chart = new BarChart(f1, f2);
        chart.setLocation(70f, 50f);
        chart.setSize(500f, 300f);
        chart.setTitle("Units sold by month");
        chart.setXAxisTitle("Month");
        chart.setYAxisTitle("Units");
        chart.setCategories(
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec");
        chart.addSeries("2025",
                new float[] {45f, 65f, 31f, 45f, 65f, 31f, 38f, 52f, 47f, 59f, 66f, 72f},
                Color.seagreen);
        chart.addSeries("2026",
                new float[] {75f, 20f, 73f, 75f, 20f, 73f, 61f, 58f, 69f, 64f, 77f, 80f},
                Color.indianred);
        chart.setGroupGap(0.4f);
        chart.setBarGap(0.1f);
        chart.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_40();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_40", time0, time1);
    }
}   // End of Example_40.java
