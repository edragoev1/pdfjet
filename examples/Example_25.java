package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_25.java
 */
public class Example_25 {
    public Example_25() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_25.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(12.0f);
        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.setSize(10.0f);

        DonutChart chart = new DonutChart(f1, f2, true);       // true = full donut (with hole)
        chart.setLocation(300.0f, 400.0f);
        chart.setR1AndR2(200.0f, 120.0f);

        chart.addSlice(new Slice(90.0f,  0xC1121F, "Apples",   ""));   // deep red
        chart.addSlice(new Slice(72.0f,  0x1D3557, "Oranges",  ""));   // navy blue
        chart.addSlice(new Slice(108.0f, 0x1A7468, "Bananas",  ""));   // dark teal
        chart.addSlice(new Slice(54.0f,  0xD97706, "Grapes",   ""));   // burnt orange
        chart.addSlice(new Slice(36.0f,  0xCAAA2F, "Lemons",   ""));   // dark gold
        chart.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_25();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_25", time0, time1);
    }
}   // End of Example_25.java
