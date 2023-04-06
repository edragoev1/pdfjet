package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_06.java
 *  We will draw the American flag using Box, Line and Point objects.
 */
public class Example_06 {
    public Example_06() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_06.pdf")));
        pdf.setTitle("Hello");
        pdf.setAuthor("Eugene");
        pdf.setSubject("Example");
        pdf.setKeywords("Hello World This is a test");
        pdf.setCreator("Application Name");

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", false);     // Don't compress the image.
        EmbeddedFile file2 = new EmbeddedFile(pdf, "examples/Example_02.java", true);   // Compress text files.

        Page page = new Page(pdf, Letter.PORTRAIT);

        Box flag = new Box();
        flag.setLocation(100f, 100f);
        flag.setSize(190f, 100f);
        flag.setColor(Color.white);
        flag.drawOn(page);

        float sw = 7.69f;       // stripe width
        Line stripe = new Line(0.0f, sw/2, 190.0f, sw/2);
        stripe.setWidth(sw);
        stripe.setColor(Color.oldgloryred);
        for (int row = 0; row < 7; row++) {
            stripe.placeIn(flag, 0.0f, row * 2 * sw);
            stripe.drawOn(page);
        }

        Box union = new Box();
        union.setSize(76.0f, 53.85f);
        union.setColor(Color.oldgloryblue);
        union.setFillShape(true);
        union.placeIn(flag, 0f, 0f);
        union.drawOn(page);

        float h_si = 12.6f;     // horizontal star interval
        float v_si = 10.8f;     // vertical star interval
        Point star = new Point(h_si/2, v_si/2);
        star.setShape(Point.STAR);
        star.setRadius(3.0f);
        star.setColor(Color.white);
        star.setFillShape(true);

        for (int row = 0; row < 6; row++) {
            for (int col = 0; col < 5; col++) {
                star.placeIn(union, row * h_si, col * v_si);
                star.drawOn(page);
            }
        }

        star.setLocation(h_si, v_si);
        for (int row = 0; row < 5; row++) {
            for (int col = 0; col < 4; col++) {
                star.placeIn(union, row * h_si, col * v_si);
                star.drawOn(page);
            }
        }

        FileAttachment attachment = new FileAttachment(pdf, file1);
        attachment.setLocation(100f, 300f);
        attachment.setIconPushPin();
        attachment.setIconSize(24f);
        attachment.setTitle("Attached File: " + file1.getFileName());
        attachment.setDescription(
                "Right mouse click or double click on the icon to save the attached file.");
        attachment.drawOn(page);

        attachment = new FileAttachment(pdf, file2);
        attachment.setLocation(200f, 300f);
        attachment.setIconPaperclip();
        attachment.setIconSize(24f);
        attachment.setTitle("Attached File: " + file2.getFileName());
        attachment.setDescription(
                "Right mouse click or double click on the icon to save the attached file.");
        attachment.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_06();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_06 => " + (t1 - t0));
    }
}   // End of Example_06.java
