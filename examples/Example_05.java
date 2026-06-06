package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_05.java
 */
public class Example_05 {
    public Example_05() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_05.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.setItalic(true);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f1);
        text.setLocation(300f, 300f);
        for (int i = 0; i < 360; i += 15) {
            text.setTextDirection(i);
            text.setUnderline(true);
            // text.setStrikeLine(true);
            text.setText("             Hello, World -- " + i + " degrees.");
            text.drawOn(page);
        }

        text = new TextLine(f1, "WAVE AWAY");
        text.setLocation(70f, 50f);
        text.drawOn(page);

        f1.setKernPairs(true);
        text = new TextLine(f1, "WAVE AWAY");
        text.setLocation(70f, 70f);
        text.drawOn(page);

        f1.setKernPairs(false);
        text = new TextLine(f1, "WAVE AWAY");
        text.setLocation(70f, 90f);
        text.drawOn(page);

        f1.setSize(8f);
        text = new TextLine(f1, "-- font.setKernPairs(false);");
        text.setLocation(150f, 50f);
        text.drawOn(page);
        text.setLocation(150f, 90f);
        text.drawOn(page);
        text = new TextLine(f1, "-- font.setKernPairs(true);");
        text.setLocation(150f, 70f);
        text.drawOn(page);

        float[] arcPoints = (new Arc())
            .setCenterXY(300f, 600f)
            .setRadiusX(75f)
            .setRadiusY(75f)
            .setStartAngle(0f)
            .setSweepDegreesCW(270f)
            // .setSweepDegreesCCW(270f)
            // .setScaleFactor(2f)
            // .setRotateDegreesCW(90f)
            // .setRotateDegreesCCW(90f)
            .setStrokeWidth(5f)
            .setStrokeColor(Color.blue)
            .drawOn(page);
/*
        (new Ellipse())
            .setCenterXY(300f, 720f)
            .setRadiusX(100f)
            .setRadiusY(50f)
            .setFillColor(Color.azure)
            .setStrokeWidth(1.5f)
            .setStrokeColor(Color.blue)
            .setScaleFactor(0.5f)
            .setRotateDegreesCW(45f)
            // .setRotateDegreesCCW(45f)
            .drawOn(page);
*/
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_05();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_05", time0, time1);
    }
}   // End of Example_05.java
