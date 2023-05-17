package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_12.java
 */
public class Example_12 {
    public Example_12() throws Exception {
        PDF pdf = new PDF(new FileOutputStream("Example_12.pdf"), Compliance.PDF_UA);
        // Font font = new Font(pdf, CoreFont.HELVETICA);
        Font font = new Font(pdf, "fonts/Andika/Andika-Regular.ttf");
        Page page = new Page(pdf, Letter.PORTRAIT);
 
        List<String> lines = Text.readLines("examples/Example_12.java");
        StringBuilder buf = new StringBuilder();
        for (String line : lines) {
            buf.append(line);
            // Both CR and LF are required!
            buf.append("\r\n");
        }

        Barcode2D code2D = new Barcode2D(buf.toString());
        code2D.setModuleWidth(0.5f);
        code2D.setLocation(100f, 60f);
        code2D.drawOn(page);

        TextLine text = new TextLine(font,
                "PDF417 barcode containing the program that created it.");
        text.setLocation(100f, 40f);
        text.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_12();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_12", time0, time1);
    }
}
