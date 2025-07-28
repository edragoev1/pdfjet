package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_32.java
 */
public class Example_32 {
    public Example_32() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_32.pdf")));

        Font font = new Font(pdf, "fonts/JetBrainsMono/JetBrainsMono-Regular.ttf.stream");
        font.setSize(8f);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("new", Color.red);
        colors.put("ArrayList", Color.blue);
        colors.put("List", Color.blue);
        colors.put("String", Color.blue);
        colors.put("Field", Color.blue);
        colors.put("Form", Color.blue);
        colors.put("Smart", Color.green);
        colors.put("Widget", Color.green);
        colors.put("Designs", Color.green);

        Page page = new Page(pdf, Letter.PORTRAIT);
        float x = 50f;
        float y = 50f;            
        float leading = font.getBodyHeight();
        List<String> lines = Text.readLines("examples/Example_02.java");
        for (String line : lines) {
            page.drawString(font, null, line, x, y, Color.gray, colors);
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
