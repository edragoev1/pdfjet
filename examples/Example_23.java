package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_23.java
 */
public class Example_23 {
    public Example_23() throws Exception {
        PDF pdf = new PDF(new FileOutputStream("Example_23.pdf"));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(72f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.setSize(24f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x1 = 90f;
        float y1 = 50f;

        TextLine textLine = new TextLine(f2, "(x1, y1)");
        textLine.setLocation(x1, y1 - 15f);
        textLine.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
            "Hello, World! This example shows the functionality of the TextBlock.");
        textBlock.setLocation(x1, y1);
        textBlock.setWidth(500f);
        textBlock.setBorderColor(Color.lightgreen);
        textBlock.setFillColor(Color.lightgreen);
        textBlock.setTextColor(Color.black);
        float[] xy = textBlock.drawOn(page);

        // Text on the left
        TextLine ascentText = new TextLine(f2, "Ascent");
        ascentText.setFontSize(18f);
        ascentText.setLocation(x1 - 85f, y1 + 40f);
        ascentText.drawOn(page);

        TextLine descentText = new TextLine(f2, "Descent");
        descentText.setFontSize(18f);
        descentText.setLocation(x1 - 85f, y1 + f1.getAscent() + 15f);
        descentText.drawOn(page);

        // Line beside the text ascent
        Line blueLine = new Line(
            x1 - 10f,
            y1,
            x1 - 10f,
            y1 + f1.getAscent());
        blueLine.setColor(Color.blue);
        blueLine.setWidth(3f);
        blueLine.drawOn(page);

        // Line beside the text descent
        Line redLine = new Line(
            x1 - 10f,
            y1 + f1.getAscent(),
            x1 - 10f,
            y1 + f1.getAscent() + f1.getDescent());
        redLine.setColor(Color.red);
        redLine.setWidth(3f);
        redLine.drawOn(page);

        Line baseLine = new Line(
                x1,
                y1 + f1.getAscent(),
                xy[0],
                y1 + f1.getAscent());
        baseLine.drawOn(page);

        Line descentLine = new Line(
                x1,
                y1 + (f1.getAscent() + f1.getDescent()),
                xy[0],
                y1 + (f1.getAscent() + f1.getDescent()));
        descentLine.drawOn(page);

        Line ascentLine = new Line(
                x1,
                y1 + f1.getBodyHeight() + f1.getAscent(),
                xy[0],
                y1 + f1.getBodyHeight() + f1.getAscent());
        ascentLine.drawOn(page);

        Point p1 = new Point(x1, y1);
        p1.setRadius(5f);
        p1.drawOn(page);

        Point p2 = new Point(xy[0], xy[1]);
        p2.setRadius(5f);
        p2.drawOn(page);

        f2.setSize(24f);
        TextLine textLine3 = new TextLine(f2, "(x2, y2)");
        textLine3.setLocation(xy[0] - 80f, xy[1] + 30f);
        textLine3.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_23();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_73", time0, time1);
    }
}   // End of Example_23.java
