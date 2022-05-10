package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;

/**
 * Example_47.java
 */
public class Example_47 {

    public Example_47() throws Exception {

        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("Example_47.pdf")));

        FileInputStream stream = new FileInputStream("fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f1 = new Font(pdf, stream, Font.STREAM);
        stream.close();
        f1.setSize(12f);

        stream = new FileInputStream("fonts/OpenSans/OpenSans-Italic.ttf.stream");
        Font f2 = new Font(pdf, stream, Font.STREAM);
        stream.close();
        f2.setSize(12f);

        stream = new FileInputStream("images/AU-map.png");
        Image image1 = new Image(pdf, stream, ImageType.PNG);
        stream.close();
        image1.scaleBy(0.50f);

        stream = new FileInputStream("images/HU-map.png");
        Image image2 = new Image(pdf, stream, ImageType.PNG);
        stream.close();
        image2.scaleBy(0.50f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        image1.setLocation(20f, 20f);
        image1.drawOn(page);

        image2.setLocation(
                page.getWidth() - (image2.getWidth() + 20f),
                page.getHeight() - (image2.getHeight() + 20f));
        image2.drawOn(page);

        BufferedReader reader =
                new BufferedReader(new FileReader("data/austria_hungary.txt"));
        StringBuffer buffer = new StringBuffer();
        String text = null;
        while ((text = reader.readLine()) != null) {
            buffer.append(text);
            buffer.append("\n");
        }
        reader.close();

        List<TextLine> paragraphs = new ArrayList<TextLine>();
        String[] textLines = buffer.toString().split("\\n\\n");
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
        frame.setDrawBorder(true);
        frame.drawOn(page);

        xPos += 200f;
        if (frame.isNotEmpty()) {
            frame.setLocation(xPos, yPos);
            frame.setWidth(width);
            frame.setHeight(height);
            frame.setDrawBorder(false);
            frame.drawOn(page);
        }

        xPos += 200f;
        if (frame.isNotEmpty()) {
            frame.setLocation(xPos, yPos);
            frame.setWidth(width);
            frame.setHeight(height);
            frame.setDrawBorder(true);
            frame.drawOn(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_47();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_47 => " + (t1 - t0));
    }

}   // End of Example_47.java
