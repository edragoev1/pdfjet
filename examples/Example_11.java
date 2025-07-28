package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_11.java
 */
public class Example_11 {
    public Example_11() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_11.pdf")), Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/NotoSans/NotoSans-Regular.ttf.stream");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Barcode code = new Barcode(Barcode.CODE_128, "Hellö, World!");
        code.setLocation(170f, 70f);
        code.setModuleLength(0.75f);
        code.setFont(f1);
        code.drawOn(page);

        code = new Barcode(Barcode.CODE_128, "G86513JVW0C");
        code.setLocation(170f, 170f);
        code.setModuleLength(0.75f);
        code.setDirection(Barcode.TOP_TO_BOTTOM);
        code.setFont(f1);
        code.drawOn(page);

        code = new Barcode(Barcode.CODE_39, "WIKIPEDIA");
        code.setLocation(270f, 370f);
        code.setModuleLength(0.75f);
        code.setFont(f1);
        code.drawOn(page);

        code = new Barcode(Barcode.CODE_39, "CODE39");
        code.setLocation(400f, 70f);
        code.setModuleLength(0.75f);
        code.setDirection(Barcode.TOP_TO_BOTTOM);
        code.setFont(f1);
        code.drawOn(page);

        code = new Barcode(Barcode.CODE_39, "CODE39");
        code.setLocation(450f, 70f);
        code.setModuleLength(0.75f);
        code.setDirection(Barcode.BOTTOM_TO_TOP);
        code.setFont(f1);
        code.drawOn(page);

        code = new Barcode(Barcode.UPC_A, "51234567890"); // TODO: Do not allow more than 11 digits!!!
        code.setLocation(450f, 250f);
        code.setModuleLength(1.0);
        code.setDirection(Barcode.BOTTOM_TO_TOP);
        code.setFont(f1);
        code.drawOn(page);

        code = new Barcode(Barcode.EAN_13, "051234567890");   // EAN-13 without the check digit which we calculate!!
        code.setLocation(450f, 450f);
        code.setModuleLength(1.0);
        code.setDirection(Barcode.BOTTOM_TO_TOP);
        code.setFont(f1);
        code.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_11();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_11", time0, time1);
    }
}   // End of Example_11.java
