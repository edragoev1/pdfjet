package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_54.java
 *
 *  Draw the Canadian Maple Leaf using a Path object that contains both lines
 *  and curve segments. Every curve segment must have exactly 2 control points.
 */
public class Example_54 {
    public Example_54() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_54.pdf")));

        Page page = new Page(pdf, Letter.LANDSCAPE);
/*
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
*/
        page.setPenColorCMYK(0.0f, 0.0f, 0.0f, 1.0f);
        float y = 7f;
        for (int i = 0; i < 10; i++) {
            drawBlockOfLines_4_3_4(page, 20f, y, 365f, y);
            drawBlockOfLines_4_3_4(page, 420f, y, 365f, y);
            y += 60f;
        }

        pdf.complete();
    }

    private void drawArrow(Page page, float x1, float y1) {
        float x = x1;
        float y = y1;
        page.moveTo(x - 2f, y);
        page.lineTo(x - 7f, y - 5f);
        page.lineTo(x - 7f, y - 2f);
        page.lineTo(x - 14f, y - 2f);
        page.lineTo(x - 14f, y + 2f);
        page.lineTo(x - 7f, y + 2f);
        page.lineTo(x - 7f, y + 5f);
        page.closePath();
    }

    private void drawBlockOfLines_4_3_4(Page page, float x0, float y0, float w, float h) {
        float x = x0;
        float y = y0;

        page.setPenWidth(0.5f);
        page.setDefaultLinePattern();
        page.drawLine(x, y, x + w, y);
        y += 15f;

        page.setPenWidth(0.3f);
        page.setLinePattern("[1 3] 0");
        page.drawLine(x, y, x + w, y);
        y += 5f;
        page.drawLine(x, y, x + w, y);
        y += 15f;

        page.setDefaultLinePattern();
        page.drawLine(x, y, x + w, y);
        drawArrow(page, x, y);
        y += 20f;

        page.setPenWidth(0.5f);
        page.drawLine(x, y, x + w, y);
    }
    
    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_54();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_54", time0, time1);
    }
}   // End of Example_54.java
