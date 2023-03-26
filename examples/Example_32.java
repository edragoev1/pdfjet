package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_32.java
 *
 */
public class Example_32 {

    private Font f1;
    private float x = 50f;
    private float y = 50f;
    private float leading = 10f;

    public Example_32() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_32.pdf")));

        // f1 = new Font(pdf, CoreFont.COURIER);
        f1 = new Font(pdf, new FileInputStream(
            "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream"), Font.STREAM);
        f1.setSize(8f);

        BufferedReader reader = new BufferedReader(
                new InputStreamReader(
                        new FileInputStream("examples/Example_02.java"), "UTF-8"));

        String line = reader.readLine();
        Page page = null;
        while (line != null) {
            if (page == null) {
                y = 50f;
                page = newPage(pdf);
            }
            page.println(line);
            y += leading;
            if (y > (Letter.PORTRAIT[1] - 20f)) {
                page.setTextEnd();
                page = null;
            }
            line = reader.readLine();
        }
        if (page != null) {
            page.setTextEnd();
        }
        reader.close();

        pdf.complete();
    }

    private Page newPage(PDF pdf) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setTextStart();
        page.setTextFont(f1);
        page.setTextLocation(x, y);
        page.setTextLeading(leading);
        return page;
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_32();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_32 => " + (t1 - t0));
    }

}   // End of Example_32.java
