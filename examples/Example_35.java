package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_35.java
 *
 */
public class Example_35 {

    public Example_35() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_35.pdf")));

        // Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f1 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Bold.ttf.stream"),
                Font.STREAM);
        f1.setSize(8f);

        // Font f2 = new Font(pdf, CoreFont.HELVETICA);
        Font f2 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Regular.ttf.stream"),
                Font.STREAM);
        f2.setSize(8f);

        Page page = new Page(pdf, A4.PORTRAIT);

        List<List<Point>> chartData = new ArrayList<List<Point>>();

        List<Point> path1 = new ArrayList<Point>();
        path1.add(new Point(50f, 50f).setDrawPath().setColor(Color.blue));
        path1.add(new Point(55f, 55f));
        path1.add(new Point(60f, 60f));
        path1.add(new Point(65f, 58f));
        path1.add(new Point(70f, 59f));
        path1.add(new Point(75f, 63f));
        path1.add(new Point(80f, 65f));
        chartData.add(path1);

        List<Point> path2 = new ArrayList<Point>();
        path2.add(new Point(50f, 30f).setDrawPath().setColor(Color.red));
        path2.add(new Point(55f, 35f));
        path2.add(new Point(60f, 40f));
        path2.add(new Point(65f, 48f));
        path2.add(new Point(70f, 49f));
        path2.add(new Point(75f, 53f));
        path2.add(new Point(80f, 55f));
        chartData.add(path2);

        List<Point> path3 = new ArrayList<Point>();
        path3.add(new Point(50f, 80f).setDrawPath().setColor(Color.green));
        path3.add(new Point(55f, 70f));
        path3.add(new Point(60f, 60f));
        path3.add(new Point(65f, 55f));
        path3.add(new Point(70f, 59f));
        path3.add(new Point(75f, 63f));
        path3.add(new Point(80f, 61f));
        chartData.add(path3);

        Chart chart = new Chart(f1, f2);
        chart.setData(chartData);
        chart.setLocation(70f, 50f);
        chart.setSize(500f, 300f);
        chart.setTitle("Chart Title");
        chart.setXAxisTitle("X Axis Title");
        chart.setYAxisTitle("Y Axis Title");

        // You can adjust the X and Y min and max manually:
        // chart.setXAxisMinMax(45f, 80f, 7);
        // chart.setYAxisMinMax(20f, 80f, 6);
        chart.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_35();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_35 => " + (t1 - t0));
    }

}   // End of Example_35.java
