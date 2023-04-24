package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_51.java
 * 
 *  This example shows how to add "Page X of N" footer to every page of
 *  the PDF file. In this case we create new PDF and store it in a buffer.
 */
public class Example_51 {
    public Example_51(String fileNumber) throws Exception {
        ByteArrayOutputStream buf1 = new ByteArrayOutputStream();
        PDF pdf = new PDF(buf1);
        Page page = new Page(pdf, Letter.PORTRAIT);

        Box box = new Box();
        box.setLocation(50f, 50f);
        box.setSize(100.0f, 100.0f);
        box.setColor(Color.red);
        box.setFillShape(true);
        box.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        box = new Box();
        box.setLocation(50f, 50f);
        box.setSize(100.0f, 100.0f);
        box.setColor(Color.green);
        box.setFillShape(true);
        box.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        box = new Box();
        box.setLocation(50f, 50f);
        box.setSize(100.0f, 100.0f);
        box.setColor(Color.blue);
        box.setFillShape(true);
        box.drawOn(page);

        pdf.complete();

        BufferedOutputStream buf2 = new BufferedOutputStream(
                new FileOutputStream("Example_" + fileNumber + ".pdf"));
        addFooterToPDF(buf1, buf2);
    }

    public void addFooterToPDF(
            ByteArrayOutputStream buf, OutputStream outputStream) throws Exception {
        PDF pdf = new PDF(outputStream);
        List<PDFobj> objects = pdf.read(
                new ByteArrayInputStream(buf.toByteArray()));

        Font font = new Font(
                objects,
                new FileInputStream("fonts/RedHatText/RedHatText-Regular.ttf.stream"),
                Font.STREAM);
        font.setSize(12f);

        List<PDFobj> pages = pdf.getPageObjects(objects);
        for (int i = 0; i < pages.size(); i++) {
            String footer = "Page " + (i + 1) + " of " + pages.size();
            Page page = new Page(pdf, pages.get(i));
            page.addResource(font, objects);
            page.setBrushColor(Color.transparent);  // Required!
            page.setBrushColor(Color.black);
            page.drawString(
                    font,
                    footer,
                    (page.getWidth() - font.stringWidth(footer))/2f,
                    (page.getHeight() - 5f));
            page.complete(objects);
        }
        pdf.addObjects(objects);
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_51("51");
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_51", time0, time1);
    }
}   // End of Example_51.java
