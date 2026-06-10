package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_12.java
 */
public class Example_12 {
    public Example_12() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_12.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);

        // Font font = new Font(pdf, CoreFont.HELVETICA);
        Font font = new Font(pdf, IBMPlexSans.Regular);
        Page page = new Page(pdf, Letter.PORTRAIT);

        List<String> lines = Text.readLines("examples/Example_12.java");
        StringBuilder buf = new StringBuilder();
        for (String line : lines) {
            buf.append(line);
            // Both CR and LF are required!
            buf.append("\r\n");
        }

        PDF417 barcode = new PDF417(buf.toString());
        barcode.setModuleWidth(0.5f);
        barcode.setLocation(100f, 60f);
        barcode.drawOn(page);

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
