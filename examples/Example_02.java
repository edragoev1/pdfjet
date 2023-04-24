package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_02.java
 *
 *  Draw the Canadian Maple Leaf using a Path object that contains both lines
 *  and curve segments. Every curve segment must have exactly 2 control points.
 */
public class Example_02 {
    public Example_02() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_02.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);
        Path path = new Path();
        path.setLocation(100f, 50f);

        path.add(new Point(13.0f,  0.0f));
        path.add(new Point(15.5f,  4.5f));

        path.add(new Point(18.0f,  3.5f));
        path.add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point(20.5f,  7.5f));

        path.add(new Point(21.0f,  9.5f));
        path.add(new Point(25.0f,  9.0f));
        path.add(new Point(24.0f, 13.0f));
        path.add(new Point(25.5f, 14.0f));
        path.add(new Point(19.0f, 19.0f));
        path.add(new Point(20.0f, 21.5f));
        path.add(new Point(13.5f, 20.5f));
        path.add(new Point(13.5f, 27.0f));
        path.add(new Point(12.5f, 27.0f));
        path.add(new Point(12.5f, 20.5f));
        path.add(new Point( 6.0f, 21.5f));
        path.add(new Point( 7.0f, 19.0f));
        path.add(new Point( 0.5f, 14.0f));
        path.add(new Point( 2.0f, 13.0f));
        path.add(new Point( 1.0f,  9.0f));
        path.add(new Point( 5.0f,  9.5f));

        path.add(new Point( 5.5f,  7.5f));
        path.add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point( 8.0f,  3.5f));

        path.add(new Point(10.5f,  4.5f));
        path.setClosePath(true);
        path.setColor(Color.red);
        path.setFillShape(true);
        path.scaleBy(4f);
        path.drawOn(page);

        path.scaleBy(4f);
        path.setFillShape(false);
        float[] xy = path.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        page.setPenColorCMYK(1.0f, 0.0f, 0.0f, 0.0f);
        page.setPenWidth(5.0f);
        page.drawLine(50f, 500f, 300f, 500f);

        page.setPenColorCMYK(0.0f, 1.0f, 0.0f, 0.0f);
        page.setPenWidth(5.0f);
        page.drawLine(50f, 550f, 300f, 550f);

        page.setPenColorCMYK(0.0f, 0.0f, 1.0f, 0.0f);
        page.setPenWidth(5.0f);
        page.drawLine(50f, 600f, 300f, 600f);

        page.setPenColorCMYK(0.0f, 0.0f, 0.0f, 1.0f);
        page.setPenWidth(5.0f);
        page.drawLine(50f, 650f, 300f, 650f);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_02();
        long t1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_02", t0, t1);
    }

}   // End of Example_02.java
