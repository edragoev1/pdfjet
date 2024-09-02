package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_29.java
 */
public class Example_29 {
    public Example_29() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_29.pdf")));

        Font font = new Font(pdf, CoreFont.HELVETICA);
        font.setSize(16f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Paragraph paragraph = new Paragraph();
        paragraph.add(new TextLine(font,
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis."));

        TextColumn column = new TextColumn();
        column.setLocation(50f, 50f);
        column.setSize(540f, 0f);
        // column.SetLineBetweenParagraphs(true);
        column.setLineBetweenParagraphs(false);
        column.addParagraph(paragraph);

        Dimension dim0 = column.getSize();
        float[] point1 = column.drawOn(page);
        float[] point2 = column.drawOn(null);
        Dimension dim1 = column.getSize();
        Dimension dim2 = column.getSize();
        Dimension dim3 = column.getSize();
/*
        System.out.println("height0: " + dim0.getHeight());
        System.out.println("point1.x: " + point1[0] + "    point1,y " + point1[1]);
        System.out.println("point2.x: " + point2[0] + "    point2.y " + point2[1]);
        System.out.println("height1: " + dim1.getHeight());
        System.out.println("height2: " + dim2.getHeight());
        System.out.println("height3: " + dim3.getHeight());
        System.out.println();
*/
        column.removeLastParagraph();
        column.setLocation(50f, point2[1]);
        paragraph = new Paragraph();
        paragraph.add(new TextLine(font, "Peter Blood, bachelor of medicine and several other things besides, smoked a pipe and tended the geraniums boxed on the sill of his window above Water Lane in the town of Bridgewater."));
        column.addParagraph(paragraph);

        Dimension dim4 = column.getSize();
        float[] point = column.drawOn(page);  // Draw the updated text column

        Box box = new Box();
        box.setLocation(point[0], point[1]);
        box.setSize(540f, 25f);
        box.setLineWidth(2f);
        box.setColor(Color.darkblue);
        box.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_29();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_29", time0, time1);
    }
}   // End of Example_29.java
