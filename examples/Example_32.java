package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_32.java
 */
public class Example_32 {
    public Example_32() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_32.pdf")));

        Font font = new Font(pdf, JetBrainsMono.Regular);
        font.setSize(10f);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("new", Color.red);
        colors.put("class", Color.blue);
        colors.put("void", Color.green);
        float[] grayColor = new float[] {0.2f, 0.2f, 0.2f};

        Page page = new Page(pdf, Letter.PORTRAIT);
        float x = 50f;
        float y = 50f;
        float leading = font.getBodyHeight();
        List<String> lines = Text.readLines("examples/Example_02.java");
        for (String line : lines) {
            page.drawString(font, font.getSize(), line, x, y, grayColor, colors);
            y += leading;
            if (y > (page.getHeight() - 20f)) {
                page = new Page(pdf, Letter.PORTRAIT);
                y = 50f;
            }
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_32();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_32", time0, time1);
    }
}   // End of Example_32.java
