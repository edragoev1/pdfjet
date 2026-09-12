package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_14.java
 * Drawing Data Matrix barcodes.
 */
public class Example_14 {
    public Example_14() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_14.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        DataMatrix barcode = new DataMatrix("https://github.com/edragoev1/pdfjet");
        barcode.setLocation(50f, 50f);
        barcode.setModuleLength(3f);
        float[] xy = barcode.drawOn(page);
        TextLine caption = new TextLine(f1, "A web address");
        caption.setLocation(50f, xy[1] + 20f);
        caption.drawOn(page);

        barcode = new DataMatrix("Grüße aus München! こんにちは 😀");
        barcode.setLocation(300f, 50f);
        barcode.setModuleLength(3f);
        xy = barcode.drawOn(page);
        caption = new TextLine(f1, "Text in UTF-8");
        caption.setLocation(300f, xy[1] + 20f);
        caption.drawOn(page);

        barcode = new DataMatrix("PDFjet 9.0.0", DataMatrix.RECTANGLE);
        barcode.setLocation(50f, 250f);
        barcode.setModuleLength(4f);
        barcode.setColor(Color.blue);
        xy = barcode.drawOn(page);
        caption = new TextLine(f1, "A rectangular symbol");
        caption.setLocation(50f, xy[1] + 20f);
        caption.drawOn(page);

        StringBuilder sb = new StringBuilder();
        for (int i = 1; i <= 20; i++) {
            sb.append("Line ").append(i).append(" of a longer text in a larger symbol.\n");
        }
        barcode = new DataMatrix(sb.toString());
        barcode.setLocation(300f, 250f);
        barcode.setModuleLength(2f);
        xy = barcode.drawOn(page);
        caption = new TextLine(f1, "A larger symbol");
        caption.setLocation(300f, xy[1] + 20f);
        caption.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_14();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_14", time0, time1);
    }
}   // End of Example_14.java
