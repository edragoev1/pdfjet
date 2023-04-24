package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_40.java
 *
 */
public class Example_40 {
    public Example_40() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_40.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.setItalic(true);
        f1.setSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.setItalic(true);
        f2.setSize(8f);

        Chart chart = new Chart(f1, f2);
        chart.setData(getData());
        chart.setLocation(70f, 50f);
        chart.setSize(500f, 300f);
        chart.setTitle("Vertical Bar Chart Example");
        chart.setXAxisTitle("Bar Chart");
        chart.setYAxisTitle("Vertical");
        chart.setDrawYAxisLines(false);
        chart.setDrawXAxisLabels(false);
        chart.setXYChart(false);
        chart.drawOn(page);

        pdf.complete();
    }

    public List<List<Point>> getData() throws Exception {
        List<List<Point>> chartData = new ArrayList<List<Point>>();
/*
        addVerticalBar(chartData, 15f, 24f, 45f, Color.blue, " Vertical", Color.white);
        addVerticalBar(chartData, 17f, 24f, 75f, Color.yellow, " Vertical", Color.black);
        addVerticalBar(chartData, 19f, 24f, 65f, Color.peachpuff, " Vertical", Color.black);
        addVerticalBar(chartData, 25f, 24f, 20f, Color.green, " Bar", Color.white);
        addVerticalBar(chartData, 35f, 24f, 31f, Color.red, " Chart", Color.white);
        addVerticalBar(chartData, 45f, 24f, 73f, Color.gold, " Example", Color.black);
*/
        float w = 14f;
        float x = 10f;
        float dx1 = 16f;
        float dx2 = 26f;
        addVerticalBar(chartData, x, w, 45f, Color.green, " January", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 75f, Color.red, " January", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 65f, Color.green, " February", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 20f, Color.red, " February", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " March", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " March", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 45f, Color.green, " April", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 75f, Color.red, " April", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 65f, Color.green, " May", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 20f, Color.red, " May", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " June", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " June", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " July", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " July", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " August", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " August", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " Septemeber", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " Septemeber", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " October", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " October", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " November", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " November", Color.white);
        x += dx2;
        addVerticalBar(chartData, x, w, 31f, Color.green, " December", Color.white);
        x += dx1;
        addVerticalBar(chartData, x, w, 73f, Color.red, " December", Color.white);

        return chartData;
    }

    private void addVerticalBar(
            List<List<Point>> chartData,
            Float x,
            Float w,
            Float h,
            int color,
            String text,
            int textColor) {
        List<Point> path1 = new ArrayList<Point>();

        Point point = new Point();
        point.setDrawPath();
        point.setX(x);
        point.setY(0f);
        point.setShape(Point.INVISIBLE);
        point.setColor(color);
        point.setLineWidth(w);
        point.setText(text);
        point.setTextColor(textColor);
        point.setTextDirection(90);
        path1.add(point);

        point = new Point();
        point.setX(x);
        point.setY(h);
        point.setShape(Point.INVISIBLE);
        path1.add(point);

        chartData.add(path1);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_40();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_40", time0, time1);
    }
}   // End of Example_40.java
