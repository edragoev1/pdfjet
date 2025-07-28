package examples;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Paths;
import com.pdfjet.*;

/**
 *  Example_01.java
 */
public class Example_01 {
    public Example_01() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_01.pdf")));

        Font font1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBox textBox = new TextBox(font1, new String(
                Files.readAllBytes(Paths.get("data/languages/english.txt"))));
        textBox.setLocation(50f, 50f);
        textBox.setWidth(430f);
        textBox.drawOn(page);

        textBox = new TextBox(font1, new String(
                Files.readAllBytes(Paths.get("data/languages/greek.txt"))));
        textBox.setLocation(50f, 250f);
        textBox.setWidth(430f);
        textBox.drawOn(page);

        textBox = new TextBox(font1, new String(
                Files.readAllBytes(Paths.get("data/languages/bulgarian.txt"))));
        textBox.setLocation(50f, 450f);
        textBox.setWidth(430f);
        textBox.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_01();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_01", time0, time1);
    }
}   // End of Example_01.java
