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
            new BufferedOutputStream(new FileOutputStream("Example_47.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(12f);

        List<TextLine> paragraphs = new ArrayList<TextLine>();
        String contents = Content.ofTextFile("data/dostoevsky.txt");
        String[] textLines = contents.split("\\n\\n");
        for (String textLine : textLines) {
            paragraphs.add(new TextLine(f1, textLine));
        }

        float x = 50f;
        float y = 50f;
        float w = 230f;
        float h = 500f;

        Page page = null;
        TextFrame frame = new TextFrame(paragraphs);
        while (frame.isNotEmpty()) {
            page = new Page(pdf, Letter.LANDSCAPE);

            frame.setLocation(x, y);
            frame.setWidth(w);
            frame.setHeight(h);
            frame.setBorder(true);
            frame.drawOn(page);

            if (frame.isNotEmpty()) {
                x += 250f;
                frame.setLocation(x, y);
                frame.setWidth(w);
                frame.setHeight(h);
                // frame.setBorder(false);
                frame.drawOn(page);
            }

            if (frame.isNotEmpty()) {
                x += 250f;
                frame.setLocation(x, y);
                frame.setWidth(w);
                frame.setHeight(h);
                // frame.setBorder(true);
                frame.drawOn(page);
            }

            x = 50f;
            y = 50f;
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
