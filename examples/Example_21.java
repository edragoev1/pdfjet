package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_21.java
 *
 */
public class Example_21 {

    public Example_21() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_21.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f1,
                "QR codes encoded with Low, Medium, High and Very High error correction level - Java");
        text.setLocation(100.0f, 30.0f);
        text.drawOn(page);

        // Please note:
        // The higher the error correction level - the shorter the string that you can encode.
        QRCode qr = new QRCode(
                "https://kazuhikoarase.github.io/qrcode-generator/js/demo",
                ErrorCorrectLevel.L);   // Low
        qr.setModuleLength(3f);
        qr.setLocation(100f, 100f);
        // qr.setColor(Color.blue);
        qr.drawOn(page);

        qr = new QRCode(
                "https://github.com/kazuhikoarase/qrcode-generator",
                ErrorCorrectLevel.M);   // Medium
        qr.setLocation(400f, 100f);
        qr.setModuleLength(3f);
        qr.drawOn(page);

        qr = new QRCode(
                "https://github.com/kazuhikoarase/jaconv",
                ErrorCorrectLevel.Q);   // High
        qr.setLocation(100f, 400f);
        qr.setModuleLength(3f);
        qr.drawOn(page);

        qr = new QRCode(
                "https://github.com/kazuhikoarase",
                ErrorCorrectLevel.H);   // Very High
        qr.setLocation(400f, 400f);
        qr.setModuleLength(3f);
        float[] xy = qr.drawOn(page);
/*
        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);
*/
        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_21();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_21 => " + (t1 - t0));
    }

}   // End of Example_21.java
