package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_11.java
 *
 */
public class Example_11 {

    public Example_11() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_11.pdf")));

        FileInputStream stream = new FileInputStream("fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f1 = new Font(pdf, stream, Font.STREAM);
        stream.close();

        Page page = new Page(pdf, Letter.PORTRAIT);

        BarCode code = new BarCode(BarCode.CODE128, "Hellö, World!");
        code.setLocation(170f, 70f);
        code.setModuleLength(0.75f);
        code.setFont(f1);
        code.drawOn(page);

        code = new BarCode(BarCode.CODE128, "G86513JVW0C");
        code.setLocation(170f, 170f);
        code.setModuleLength(0.75f);
        code.setDirection(BarCode.TOP_TO_BOTTOM);
        code.setFont(f1);
        code.drawOn(page);

        code = new BarCode(BarCode.CODE39, "WIKIPEDIA");
        code.setLocation(270f, 370f);
        code.setModuleLength(0.75f);
        code.setFont(f1);
        code.drawOn(page);

        code = new BarCode(BarCode.CODE39, "CODE39");
        code.setLocation(400f, 70f);
        code.setModuleLength(0.75f);
        code.setDirection(BarCode.TOP_TO_BOTTOM);
        code.setFont(f1);
        code.drawOn(page);

        code = new BarCode(BarCode.CODE39, "CODE39");
        code.setLocation(450f, 70f);
        code.setModuleLength(0.75f);
        code.setDirection(BarCode.BOTTOM_TO_TOP);
        code.setFont(f1);
        code.drawOn(page);

        code = new BarCode(BarCode.UPC, "712345678904");
        code.setLocation(450f, 270f);
        code.setModuleLength(0.75f);
        code.setDirection(BarCode.BOTTOM_TO_TOP);
        code.setFont(f1);
        code.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_11();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_11 => " + (t1 - t0));
    }

}   // End of Example_11.java
