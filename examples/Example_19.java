package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_19.java
 */
public class Example_19 {
    public Example_19() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_19.pdf")));
        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");
        f1.setSize(10f);
        f2.setSize(10f);
        String contents = Contents.ofTextFile("data/calculus-short.txt");

        Page page = new Page(pdf, Letter.PORTRAIT);
        // Columns x coordinates
        float x1 = 50f;
        float y1 = 50f;
        float x2 = 300f;
        float w2 = 300f;    // Width of the second column

        Image image1 = new Image(pdf, "images/fruit.jpg");
        Image image2 = new Image(pdf, "images/ee-map.png");

        // Draw the first image
        image1.setLocation(x1, y1);
        image1.scaleBy(0.75f);
        image1.drawOn(page);

        TextBox textBox = new TextBox(f1);
        textBox.setText(contents);
        textBox.setLocation(x2, y1);
        textBox.setWidth(w2);
        textBox.setBorders(true);
        // textBlock.setTextAlignment(Align.RIGHT);
        // textBlock.setTextAlignment(Align.CENTER);
        float[] xy = textBox.drawOn(page);

        // Draw the second image
        image2.setLocation(x1, xy[1] + 10f);
        image2.scaleBy(1f/3f);
        image2.drawOn(page);

        textBox = new TextBox(f1);
        textBox.setText(Contents.ofTextFile("data/latin.txt"));
        textBox.setLocation(x2, xy[1] + 10f);
        textBox.setWidth(w2);
        textBox.setBorders(true);
        textBox.drawOn(page);

        textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText(Contents.ofTextFile("data/chinese.txt"));
        textBox.setLocation(x1, 530f);
        textBox.setWidth(350f);
        textBox.setBorders(true);
        xy = textBox.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_19();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_19", time0, time1);
    }
}   // End of Example_19.java
