/**
 * Example_52.java — demonstrates line charts with a trend line
 * using the Chart class. Output: line_chart.pdf
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

public class Example_52 {
    public Example_52() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_52.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(8f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Chart chart = new Chart(f1, f2);
        chart.setLocation(100f, 70f);
        chart.setSize(350f, 250f);

        // --- Titles and appearance ---
        chart.setTitle("Monthly Revenue (thousands)");
        chart.setXAxisTitle("Month");
        chart.setYAxisTitle("Revenue");
        chart.setFontSize(8f);
        chart.setChartBorderWidth(1.2f);
        chart.setInnerBorderWidth(0.8f);
        chart.setHGridLineWidth(0.4f);
        chart.setVGridLineWidth(0.4f);
        chart.setHGridLinePattern("[1 1] 0");      // dotted grid
        chart.setVGridLinePattern("[2 2] 0");
        chart.setMinimumFractionDigits(0);
        chart.setMaximumFractionDigits(1);

        // --- Series 1: actual revenue (drawPath = connecting line) ---
        List<Point> revenue = new ArrayList<Point>();
        float[][] monthly = {
            {1, 42}, {2, 48}, {3, 45}, {4, 61},
            {5, 55}, {6, 72}, {7, 68}, {8, 81}
        };
        for (float[] m : monthly) {
            Point p = new Point(m[0], m[1]);
            p.setDrawPath();
            p.setShape(Point.CIRCLE);
            p.setStrokeColor(0x6633cc);
            p.setFillColor(0x6633cc);
            p.setRadius(2f);
            p.setStrokeWidth(0.8f);
            p.setURIAction("https://example.com/report?month=" + (int) m[0]);
            revenue.add(p);
        }

        // --- Series 2: OLS trend line (uses chart.slope()/intercept()) ---
        float slope = chart.slope(revenue);
        float intercept = chart.intercept(revenue, slope);
        float firstX = monthly[0][0];
        float lastX  = monthly[monthly.length - 1][0];
        List<Point> trend = new ArrayList<Point>();
        trend.add(makeTrendPoint(firstX, slope * firstX + intercept));
        trend.add(makeTrendPoint(lastX,  slope * lastX  + intercept));

        List<List<Point>> data = new ArrayList<List<Point>>();
        data.add(trend);     // draw trend line beneath the data
        data.add(revenue);
        chart.setData(data);

        // Leave axis auto-ranging on (grid lines == 0 triggers it)
        chart.drawOn(page);

        pdf.complete();
    }

    private Point makeTrendPoint(float x, float y) {
        Point p = new Point(x, y);
        p.setDrawPath();
        p.setShape(Point.INVISIBLE);              // line only, no markers
        p.setStrokeColor(0xd94f00);
        p.setStrokeWidth(0.6f);
        p.setStrokeDashPattern("[4 2] 0");        // dashed trend line
        return p;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_52();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_52", time0, time1);
    }
}
