package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_73.java
 */
public class Example_73 {
    public Example_73() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                    new FileOutputStream("Example_73.pdf")), Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");
        f1.setSize(12f);

        Font f2 = new Font(pdf, "fonts/NotoSansTC/NotoSansTC-Regular.ttf.stream");
        f2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine line1 = new TextLine(f1, "Hello, Beautiful World");
        TextLine line2 = new TextLine(f1, "Hello,BeautifulWorld");

        TextBox textBox = new TextBox(f1, line1.getText());
        textBox.setLocation(50f, 50f);
        textBox.setWidth(line1.getWidth() + 2*textBox.getMargin());
        textBox.setBgColor(Color.lightgreen);
        textBox.setBorders(true);
        // The drawOn method returns the x and y of the bottom right corner of the TextBox
        float[] xy = textBox.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line1.getText() + "!");
        textBox.setWidth(line1.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 100f);
        textBox.setBorders(true);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);
        
        textBox = new TextBox(f1, line2.getText());
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 200f);
        textBox.setBorders(true);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "!");
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 300f);
        textBox.setBorders(true);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "! Left Align");
        textBox.setMargin(30f);
        textBox.setVerticalAlignment(Align.TOP);
        textBox.setBgColor(Color.lightgreen);
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 400f);
        textBox.setBorders(true);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "! Right Align");
        textBox.setMargin(10f);
        textBox.setTextAlignment(Align.RIGHT);
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 500f);
        textBox.setBorders(true);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        textBox = new TextBox(f1, line2.getText() + "! Center");
        textBox.setMargin(10f);
        textBox.setTextAlignment(Align.CENTER);
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin());
        textBox.setLocation(50f, 600f);
        textBox.setBorders(true);
        xy = textBox.drawOn(page);

        box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        String text = Contents.ofTextFile("data/chinese.txt");

        textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText(text);
        // textBox.setMargin(10f);
        textBox.setBgColor(Color.lightblue);
        textBox.setVerticalAlignment(Align.TOP);
        // textBox.setHeight(210f);
        // textBox.setHeight(151f);
        textBox.setHeight(14f);
        textBox.setWidth(300f);
        textBox.setLocation(250f, 50f);
        textBox.setBorders(true);
        textBox.drawOn(page);

        textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText(text);
        // textBox.setMargin(10f);
        textBox.setBgColor(Color.lightblue);
        textBox.setVerticalAlignment(Align.CENTER);
        // textBox.setHeight(210f);
        textBox.setHeight(151f);
        textBox.setWidth(300f);
        textBox.setLocation(250f, 300f);
        textBox.setBorders(true);
        textBox.drawOn(page);

        textBox = new TextBox(f1);
        textBox.setFallbackFont(f2);
        textBox.setText(text);
        // textBox.setMargin(10f);
        textBox.setBgColor(Color.lightblue);
        textBox.setVerticalAlignment(Align.BOTTOM);
        // textBox.setHeight(210f);
        textBox.setHeight(151f);
        textBox.setWidth(300f);
        textBox.setLocation(250f, 550f);
        textBox.setBorders(true);
        textBox.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_73();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_73", time0, time1);
    }
}
