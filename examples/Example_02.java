package examples;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Paths;
import com.pdfjet.*;

/**
 *  Example_02.java
 */
public class Example_02 {
    public Example_02() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_02.pdf")));

        Font font1 = new Font(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream");
        font1.setSize(12f);

        Font font2 = new Font(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream");
        font2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBox textBox = new TextBox(font1, new String(
                Files.readAllBytes(Paths.get("data/languages/japanese.txt"))));
        textBox.setLocation(50f, 50f);
        textBox.setWidth(415f);
        textBox.drawOn(page);

        textBox = new TextBox(font2, new String(
                Files.readAllBytes(Paths.get("data/languages/korean.txt"))));
        textBox.setLocation(50f, 450f);
        textBox.setWidth(415f);
        textBox.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_02();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_02", time0, time1);
    }
}   // End of Example_02.java
