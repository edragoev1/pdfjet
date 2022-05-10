package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_30.java
 */
public class Example_30 {

    public Example_30() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_30.pdf")));

        Font font = new Font(pdf, CoreFont.HELVETICA);

        FileInputStream stream = new FileInputStream("images/map407.png");
        Image image1 = new Image(pdf, stream, ImageType.PNG);
        stream.close();
        image1.setLocation(10f, 100f);

        stream = new FileInputStream("images/qrcode.png");
        Image image2 = new Image(pdf, stream, ImageType.PNG);
        stream.close();
        image2.setLocation(10f, 100f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(font);
        textLine.setText("© OpenStreetMap contributors");
        textLine.setLocation(430f, 655f);
        float[] xy = textLine.drawOn(page);

        textLine = new TextLine(font);
        textLine.setText("http://www.openstreetmap.org/copyright");
        textLine.setURIAction("http://www.openstreetmap.org/copyright");
        textLine.setLocation(380f, xy[1] + font.getHeight());
        textLine.drawOn(page);

        OptionalContentGroup group = new OptionalContentGroup("Map");
        group.add(image1);
        group.setVisible(true);
        // group.setPrintable(true);
        group.drawOn(page);

        TextBox textBox = new TextBox(font);
        textBox.setText("Hello Blue Layer Text");
        textBox.setLocation(300f, 200f);

        Line line = new Line();
        line.setPointA(300f, 250f);
        line.setPointB(500f, 250f);
        line.setWidth(2f);
        line.setColor(Color.blue);

        group = new OptionalContentGroup("Blue");
        group.add(textBox);
        group.add(line);
        // group.setVisible(true);
        group.drawOn(page);

        line = new Line();
        line.setPointA(300f, 260f);
        line.setPointB(500f, 260f);
        line.setWidth(2f);
        line.setColor(Color.red);

        group = new OptionalContentGroup("Barcode");
        group.add(image2);
        group.add(line);
        group.setVisible(true);
        group.setPrintable(true);
        group.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_30();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_30 => " + (t1 - t0));
    }

}   // End of Example_30.java
