package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;

/**
 *  Example_18.java
 *  This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public Example_18() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_18.pdf")));

        Font font = new Font(
                pdf,
                new FileInputStream("fonts/RedHatText/RedHatText-Regular.ttf.stream"),
                Font.STREAM);
        font.setSize(12f);

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, A4.PORTRAIT, false);

        Box box = new Box();
        box.setLocation(50f, 50f);
        box.setSize(100.0f, 100.0f);
        box.setColor(Color.red);
        box.setFillShape(true);
        box.drawOn(page);
        pages.add(page);

        page = new Page(pdf, Letter.PORTRAIT, false);
        box = new Box();
        box.setLocation(50f, 50f);
        box.setSize(100.0f, 100.0f);
        box.setColor(Color.green);
        box.setFillShape(true);
        box.drawOn(page);
        pages.add(page);

        page = new Page(pdf, Letter.PORTRAIT, false);
        box = new Box();
        box.setLocation(50f, 50f);
        box.setSize(100.0f, 100.0f);
        box.setColor(Color.blue);
        box.setFillShape(true);
        box.drawOn(page);
        pages.add(page);

        int numOfPages = pages.size();
        for (int i = 0; i < numOfPages; i++) {
            page = pages.get(i);
            String footer = "Page " + (i + 1) + " of " + numOfPages;
            page.setBrushColor(Color.black);
            page.drawString(
                    font,
                    footer,
                    (page.getWidth() - font.stringWidth(footer))/2f,
                    (page.getHeight() - 5f));
        }

        for (int i = 0; i < numOfPages; i++) {
            pdf.addPage(pages.get(i));
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_18();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_18 => " + (t1 - t0));
    }
}   // End of Example_18.java
