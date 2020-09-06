package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_18.java
 *
 */
class Example_18 {

    public Example_18() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_18.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);

        page.setPenWidth(5.0f);
        page.setBrushColor(0x353638);

        float x1 = 300f;
        float y1 = 300f;
        float r1 = 50f;
        float r2 = 50f;

        List<Point> path = new ArrayList<Point>();

        List<Point> segment1 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_00_03);
        List<Point> segment2 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_03_06);
        List<Point> segment3 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_06_09);

        path.addAll(segment1);

        segment2.remove(0);
        path.addAll(segment2);

        segment3.remove(0);
        path.addAll(segment3);

        // page.drawPath(path, Operation.FILL);
        page.drawPath(path, Operation.STROKE);

        List<Point> segment4 = Path.getCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_09_12);
        page.setPenWidth(15f);
        page.setPenColor(Color.red);
        page.drawPath(segment4, Operation.STROKE);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_18();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_18 => " + (t1 - t0));
    }

}   // End of Example_18.java
