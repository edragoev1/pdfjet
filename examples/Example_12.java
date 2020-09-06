package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_12.java
 *
 */
public class Example_12 {

    public Example_12() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_12.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);

        Page page = new Page(pdf, Letter.PORTRAIT);

        StringBuilder buf = new StringBuilder();
        BufferedReader reader =
                new BufferedReader(new FileReader("examples/Example_12.java"));
        String line = null;
        while ((line = reader.readLine()) != null) {
            buf.append(line);
            // Both CR and LF are required by the scanner!
            buf.append("\r\n");
        }
        reader.close();

        BarCode2D code2D = new BarCode2D(buf.toString());
        code2D.setModuleWidth(0.5f);
        code2D.setLocation(100f, 60f);
        float[] xy = code2D.drawOn(page);
/*
        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);
*/
        TextLine text = new TextLine(f1,
                "PDF417 barcode containing the program that created it.");
        text.setLocation(100f, 40f);
        text.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_12();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_12 => " + (t1 - t0));
    }

}   // End of Example_12.java
