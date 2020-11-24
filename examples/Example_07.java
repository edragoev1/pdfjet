package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_07.java
 *
 */
public class Example_07 {

    public Example_07() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_07.pdf")),
                Compliance.PDF_UA);
        pdf.setTitle("This is a PDF/UA compliant PDF.");

        Font f1 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Regular.ttf.stream"),
                Font.STREAM);
        f1.setSize(18f);

        Font f2 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Italic.ttf.stream"),
                Font.STREAM);
        f2.setSize(18f);

        Page page = new Page(pdf, A4.LANDSCAPE);

        f1.setSize(72f);
        page.addWatermark(f1, "This is a Draft");
        f1.setSize(18f);

        float xPos = 20f;
        float yPos = 10f;
        StringBuilder buf = new StringBuilder();
        TextLine textLine = new TextLine(f1);
        int j = 0;
        for (int i = 0x410; i < 0x46F; i++) {
            if (j % 64 == 0) {
                textLine.setText(buf.toString());
                textLine.setLocation(xPos, yPos);
                textLine.drawOn(page);
                buf.setLength(0);
                yPos += 24f;
            }
            buf.appendCodePoint(i);
            j++;
        }
        textLine.setText(buf.toString());
        textLine.setLocation(xPos, yPos);
        textLine.drawOn(page);

        yPos = 70f;
        buf.setLength(0);
        j = 0;
        for (int i = 0x20; i < 0x7F; i++) {
            if (j % 64 == 0) {
                textLine.setText(buf.toString());
                textLine.setLocation(xPos, yPos);
                textLine.drawOn(page);
                buf.setLength(0);
                yPos += 24f;
            }
            buf.appendCodePoint(i);
            j++;
        }
        textLine.setText(buf.toString());
        textLine.setLocation(xPos, yPos);
        textLine.drawOn(page);

        page = new Page(pdf, A4.LANDSCAPE);
        textLine.setText("Hello, World!");
        textLine.setLocation(xPos, 34f);
        textLine.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_07();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_07 => " + (time1 - time0));
    }

}   // End of Example_07.java
