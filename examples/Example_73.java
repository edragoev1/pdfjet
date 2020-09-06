package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_73.java
 *
 */
public class Example_73 {

    public Example_73() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_73.pdf")));

        Font font = new Font(pdf, CoreFont.HELVETICA);

        Image image_00 = new Image(
                pdf,
                new BufferedInputStream(new FileInputStream("images/ee-map.png")),
                ImageType.PNG);
/*
        Image image_01 = new Image(
                pdf,
                new BufferedInputStream(new FileInputStream("images/linux-logo.jet")),
                ImageType.PNG_STREAM);
*/
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font,
                "This is PNG image.")
                .setTextDirection(0)
                .setLocation(50f, 50f)
                .drawOn(page);
        image_00.setLocation(50f, 60f).scaleBy(0.5).drawOn(page);

        float[] xy = page.addHeader(new TextLine(font, "This is a header!"));
        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(30f, 30f);
        box.drawOn(page);

        page.addFooter(new TextLine(font, "And this is a footer."));
/*
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font,
                "This is JET image.")
                .setTextDirection(0)
                .setLocation(50f, 50f)
                .drawOn(page);
        image_01.setLocation(50f, 60f).drawOn(page);
*/
        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_73();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_73 => " + (t1 - t0));
    }

}   // End of Example_73.java
