package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_07.java
 *
 */
public class Example_07 {

    public Example_07(String fontType) throws Exception {

        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_07.pdf")));
        // TODO: Move this to better suited example.
        // pdf.setPageLayout(PageLayout.SINGLE_PAGE);
        // pdf.setPageMode(PageMode.FULL_SCREEN);

        Font f1 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Regular.ttf.stream"),
                Font.STREAM);

        Font f2 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Italic.ttf.stream"),
                Font.STREAM);

        Page page = new Page(pdf, A4.LANDSCAPE);

        f1.setSize(72f);
        page.addWatermark(f1, "This is a Draft");
        f1.setSize(18f);
        f2.setSize(18f);

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

        yPos = 130f;
        buf.setLength(0);
        j = 0;
        for (int i = 0x390; i < 0x3EF; i++) {
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

        xPos = 20f; 
        yPos = 190f;
        buf.setLength(0);
        textLine.setFont(f2);
        j = 0;
        for (int i = 0x390; i < 0x3EF; i++) {
            if (j % 64 == 0) {
                textLine.setText(buf.toString());
                textLine.setLocation(800f - f2.stringWidth(buf.toString()), yPos);
                textLine.drawOn(page);
                buf.setLength(0);
                yPos += 24f;
            }
            buf.appendCodePoint(i);
            j++;
        }
        textLine.setText(buf.toString());
        textLine.setLocation(800f - f2.stringWidth(buf.toString()), yPos);
        textLine.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        if (args.length > 0 && args[0].equals("stream")) {
            new Example_07("stream");
        }
        else {
            new Example_07("OpenType");
        }
        long time1 = System.currentTimeMillis();
        System.out.println("Example_07 => " + (time1 - time0));
    }

}   // End of Example_07.java
