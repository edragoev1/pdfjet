package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_05.java
 *
 * Draws text at every angle around a point, and the words "WAVE AWAY" with and
 * without kerning, in the core font Helvetica-Bold, which is not embedded.
 *
 * A core font is one of the fourteen fonts every PDF viewer has, so the
 * document carries no font program: it is small, it is written fast, and the
 * kerning pairs and the widths of the font are built into the library, which
 * is what setKernPairs shows. The disadvantages: the viewer draws the text with
 * its own version of the font, so the look differs a little between viewers;
 * only the WinAnsi characters can be drawn, so no Cyrillic, Greek or CJK text;
 * and a document with a font that is not embedded cannot claim PDF/A or PDF/UA
 * compliance. For those, use an embedded font like IBM Plex Sans, as the
 * other examples do.
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
            text.setTextRotation(-i);
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

        Point point = new Point(300f, 300f);
        point.setShape(Shape.CIRCLE);
        point.setFillColor(Color.blue);
        point.setRadius(37f);
        point.drawOn(page);
        point.setRadius(25f);
        point.setFillColor(Color.white);
        point.drawOn(page);

        float[] arcPoints = (new Arc())
            .setLocation(300f, 600f)
            .setRadiusX(75f)
            .setRadiusY(75f)
            .setStartAngle(0f)
            .setSweep(270f)
            // .setSweep(-270f)
            // .scaleBy(2f)
            // .setRotation(-90f)
            .setStrokeWidth(5f)
            .setStrokeColor(Color.blue)
            .drawOn(page);

        (new Ellipse())
            .setLocation(300f, 720f)
            .setRadiusX(100f)
            .setRadiusY(50f)
            .setFillColor(Color.azure)
            .setStrokeWidth(1.5f)
            .setStrokeColor(Color.blue)
            .scaleBy(0.5f)
            .setRotation(45f)
            .drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_05();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_05 => " + (time1 - time0) + " ms");
    }
}   // End of Example_05.java
