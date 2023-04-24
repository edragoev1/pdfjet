package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_36.java
 */
public class Example_36 {
    public Example_36() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_36.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Image image1 = new Image(pdf, "images/ee-map.png.stream");
        Image image2 = new Image(pdf, "images/fruit.jpg");
        Image image3 = new Image(pdf, "images/palette.bmp");

        Page page1 = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        TextLine text = new TextLine(f1,
                "The map below is an embedded PNG image");
        text.setLocation(90f, 30f);
        float[] xy1 = text.drawOn(page1);

        image1.setLocation(90f, xy1[1] + 10f);
        image1.scaleBy(2f/3f);
        float[] xy2 = image1.drawOn(page1);

        text.setText("JPG image file embedded once and drawn 3 times");
        text.setLocation(90f, xy2[1] + 10f);
        float[] xy3 = text.drawOn(page1);

        image2.setLocation(90f, xy3[1] + 10f);
        image2.scaleBy(0.5f);
        float[] xy4 = image2.drawOn(page1);

        image2.setLocation(xy4[0] + 10f, xy3[1] + 10f);
        image2.scaleBy(0.5f);
        image2.setRotateCW90(true);
        float[] xy5 = image2.drawOn(page1);

        image2.setLocation(xy5[0] + 10f, xy3[1] + 10f);
        image2.setRotateCW90(false);
        image2.scaleBy(0.5f);
        float[] xy6 = image2.drawOn(page1);

        image3.setLocation(xy6[0] + 10f, xy6[1] + 10f);
        image3.scaleBy(0.5f);
        image3.drawOn(page1);

        Page page2 = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        text.setText("This page was created after the second one but it was drawn first!");
        text.setLocation(90f, 30f);
        float[] xy7 = text.drawOn(page2);

        image1.setLocation(90f, xy7[1] + 10f);
        image1.drawOn(page2);

        pdf.addPage(page2);
        pdf.addPage(page1);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_36();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_36", time0, time1);
    }
}   // End of Example_36.java
