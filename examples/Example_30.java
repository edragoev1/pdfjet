package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_30.java
 */
public class Example_30 {
    public Example_30() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_30.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);

        Image image1 = new Image(pdf, "images/map407.png");
        image1.setLocation(10f, 100f);

        Image image2 = new Image(pdf, "images/qrcode.png");
        image2.setLocation(10f, 100f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f2, "© OpenStreetMap contributors");
        textLine.setLocation(10f, 655f);
        float[] xy = textLine.drawOn(page);

        textLine = new TextLine(f2, "http://www.openstreetmap.org/copyright");
        textLine.setURIAction("http://www.openstreetmap.org/copyright");
        textLine.setLocation(10f, xy[1] + f2.getHeight());
        textLine.drawOn(page);

        OptionalContentGroup group = new OptionalContentGroup(pdf, "Map");
        group.add(image1);
        group.setVisible(true);
        group.setPrintable(true);
        group.drawOn(page);

        TextBox textBox = new TextBox(f1);
        // textBox.setFontSize(16f);
        textBox.setText("Blue Layer Text");
        textBox.setLocation(10f, 130f);

        Line line = new Line();
        line.setPointA(300f, 150f);
        line.setPointB(500f, 150f);
        line.setWidth(2f);
        line.setColor(Color.blue);

        group = new OptionalContentGroup(pdf, "Blue Line");
        group.add(textBox);
        group.add(line);
        group.setVisible(true);
        group.drawOn(page);

        line = new Line();
        line.setPointA(300f, 160f);
        line.setPointB(500f, 160f);
        line.setWidth(2f);
        line.setColor(Color.red);

        group = new OptionalContentGroup(pdf, "Barcode");
        group.add(image2);
        group.add(line);
        group.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_30();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_30", time0, time1);
    }
}   // End of Example_30.java
