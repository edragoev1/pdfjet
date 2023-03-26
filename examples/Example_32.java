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

        // Font f1 = new Font(pdf, CoreFont.COURIER);
        Font f1 = new Font(pdf, new FileInputStream(
                "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream"), Font.STREAM);
        f1.setSize(8f);

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

        float x = 50f;
        float y = 50f;            
        List<String> lines = Text.readLines("examples/Example_02.java");
        Page page = new Page(pdf, Letter.PORTRAIT);
        for (String line : lines) {
            page.drawString(f1, line, x, y, colors);
            y += 10f;
            if (y > (page.getHeight() - 20f)) {
                page = new Page(pdf, Letter.PORTRAIT);
                y = 50f;
            }
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_32();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_32 => " + (t1 - t0));
    }

}   // End of Example_32.java
