package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_74.java
 *
 */
public class Example_74 {

    public Example_74() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_74.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Font f1 = new Font(pdf, CoreFont.HELVETICA);

        Image image1 = new Image(
                pdf,
                getClass().getResourceAsStream("../images/ee-map.png"),
                ImageType.PNG);
/*
        Image image1 = new Image(
                pdf,
                getClass().getResourceAsStream("../images/ee-map.png.stream"),
                ImageType.PNG_STREAM);
*/

        // image1.scaleBy(0.75);
        image1.resizeWidth(306f);
        float[] xy = image1.drawOn(page);

        image1.setLocation(306f, 0f);
        image1.drawOn(page);

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_74();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_74 => " + (t1 - t0));
    }

}   // End of Example_74.java
