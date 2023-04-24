package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_47.java
 */
public class Example_47 {
    public Example_47() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_47.pdf")));

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Italic.ttf.stream");
                
        f1.setSize(12f);
        f2.setSize(12f);

        Image image1 = new Image(pdf, "images/AU-map.png");
        image1.scaleBy(0.50f);

        Image image2 = new Image(pdf, "images/HU-map.png");
        image2.scaleBy(0.50f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        image1.setLocation(20f, 20f);
        image1.drawOn(page);

        image2.setLocation(
                page.getWidth() - (image2.getWidth() + 20f),
                page.getHeight() - (image2.getHeight() + 20f));
        image2.drawOn(page);

        List<TextLine> paragraphs = new ArrayList<TextLine>();
        String contents = Contents.ofTextFile("data/austria_hungary.txt");
        String[] textLines = contents.split("\\n\\n");
        for (String textLine : textLines) {
            paragraphs.add(new TextLine(f1, textLine));
        }

        float xPos = 20f;
        float yPos = 250f;

        float width = 180f;
        float height = 315f;

        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(xPos, yPos);
        frame.setWidth(width);
        frame.setHeight(height);
        frame.setBorder(true);
        frame.drawOn(page);

        xPos += 200f;
        if (frame.isNotEmpty()) {
            frame.setLocation(xPos, yPos);
            frame.setWidth(width);
            frame.setHeight(height);
            frame.setBorder(false);
            frame.drawOn(page);
        }

        xPos += 200f;
        if (frame.isNotEmpty()) {
            frame.setLocation(xPos, yPos);
            frame.setWidth(width);
            frame.setHeight(height);
            frame.setBorder(true);
            frame.drawOn(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_47();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_47", time0, time1);
    }
}   // End of Example_47.java
