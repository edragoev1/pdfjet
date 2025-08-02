package examples;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Paths;
import com.pdfjet.*;
import com.pdfjet.font.*;

/**
 *  Example_01.java
 */
public class Example_01 {
    public Example_01() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_01.pdf")));

        // Font font = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");
        Font font = new Font(pdf, IBMPlexSans.Regular);

        Page page = new Page(pdf, Letter.PORTRAIT);

        com.pdfjet.TextBlock textBlock = new TextBlock(font, new String(
                Files.readAllBytes(Paths.get("data/languages/english.txt"))));
        textBlock.setLocation(50f, 50f);
        textBlock.setWidth(430f);
        textBlock.setTextPadding(10f);
        textBlock.drawOn(page);

        textBlock = new TextBlock(font, new String(
                Files.readAllBytes(Paths.get("data/languages/greek.txt"))));
        textBlock.setLocation(50f, 280f);
        textBlock.setWidth(430f);
        textBlock.setBorderColor(Color.none);
        textBlock.drawOn(page);

        textBlock = new TextBlock(font, new String(
                Files.readAllBytes(Paths.get("data/languages/bulgarian.txt"))));
        textBlock.setLocation(50f, 480f);
        textBlock.setWidth(430f);
        textBlock.setTextPadding(10f);
        textBlock.setBorderColor(Color.blue);
        textBlock.setBorderCornerRadius(10f);
        textBlock.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_01();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_01", time0, time1);
    }
}   // End of Example_01.java
