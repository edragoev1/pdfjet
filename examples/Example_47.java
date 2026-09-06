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
        pdf.setTitle("Text flowing through columns");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(14f);

        List<String> paragraphs = new ArrayList<>(
                Arrays.asList(Content.ofTextFile("data/dostoevsky.txt").split("\\n\\n")));

        float x = 50f;
        float y = 50f;
        float w = 230f;
        float h = 500f;
        float gap = 20f;

        Page page = null;
        TextFrame textFrame = new TextFrame(f1, paragraphs);
        while (textFrame.hasMoreText()) {
            page = new Page(pdf, Letter.LANDSCAPE);

            textFrame.setLocation(x, y);
            textFrame.setWidth(w);
            textFrame.setHeight(h);
            textFrame.drawOn(page);

            if (textFrame.hasMoreText()) {
                x += w + gap;
                textFrame.setLocation(x, y);
                textFrame.setWidth(w);
                textFrame.setHeight(h);
                textFrame.drawOn(page);
            }

            if (textFrame.hasMoreText()) {
                x += w + gap;
                textFrame.setLocation(x, y);
                textFrame.setWidth(w);
                textFrame.setHeight(h);
                textFrame.drawOn(page);
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
