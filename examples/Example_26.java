package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_26.java
 */
public class Example_26 {
    public Example_26() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_26.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x = 50f;
        float y = 50f;

        new CheckBox(f1, "Hello")
                .setLocation(x, y)
                .setCheckmark(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page);

        y += 30f;
        new CheckBox(f1, "World!")
                .setLocation(x, y)
                .setCheckmark(Color.blue)
                .setURIAction("http://pdfjet.com")
                .check(Mark.CHECK)
                .drawOn(page);

        y += 30f;
        new CheckBox(f1, "This is a test.")
                .setLocation(x, y)
                .setURIAction("http://pdfjet.com")
                .drawOn(page);

        y += 30f;
        new RadioButton(f1, "Hello, World!")
                .setLocation(x, y)
                .select(true)
                .drawOn(page);

        float[] xy = (new RadioButton(f1, "Yes"))
                .setLocation(x + 100f, 50f)
                .setURIAction("http://pdfjet.com")
                .select(true)
                .drawOn(page);

        xy = (new RadioButton(f1, "No"))
                .setLocation(xy[0], 50f)
                .drawOn(page);

        xy = (new CheckBox(f1, "Hello"))
                .setLocation(xy[0], 50f)
                .setCheckmark(Color.blue)
                .check(Mark.X)
                .drawOn(page);

        xy = (new CheckBox(f1, "Yahoo")
                .setLocation(xy[0], 50f)
                .setCheckmark(Color.blue)
                .check(Mark.CHECK)
                .drawOn(page));

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_26();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_26 => " + (t1 - t0));
    }
}   // End of Example_26.java
