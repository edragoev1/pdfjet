package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_39.java
 *
 * Draws a bar chart with horizontal bars: one series, the value written at
 * the end of each bar.
 */
final public class Example_39 {
    public Example_39() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_39.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.setSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.setSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        BarChart chart = new BarChart(f1, f2);
        chart.setLocation(70f, 50f);
        chart.setSize(500f, 300f);
        chart.setTitle("Longest rivers");
        chart.setXAxisTitle("Length in km");
        chart.setCategories("Nile", "Amazon", "Yangtze", "Mississippi", "Yenisei", "Yellow River");
        chart.addSeries("", new float[] {6650f, 6400f, 6300f, 6275f, 5539f, 5464f}, Color.steelblue);
        chart.setHorizontal(true);
        chart.setDrawValueLabels(true);
        chart.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_39();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_39 => " + (time1 - time0) + " ms");
    }
}   // End of Example_39.java
