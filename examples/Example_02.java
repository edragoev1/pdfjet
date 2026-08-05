package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_02.java
 */
public class Example_02 {
    public Example_02() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_02.pdf")));

        Font f1 = new Font(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream");
        f1.setSize(12f);

        Font f2 = new Font(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream");
        f2.setSize(12f);

        Font f3 = new Font(pdf, "fonts/NotoSansSC/NotoSansSC-Regular-SC3500.ttf.stream");
        // Font f3 = new Font(pdf, "fonts/NotoSansSC/NotoSansSC-Regular.ttf.stream");
        f3.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBlock textBlock = new TextBlock(
                f1, Content.ofTextFile("data/languages/japanese.txt"));
        textBlock.setLocation(50f, 50f);
        textBlock.setWidth(415f);
        textBlock.drawOn(page);

        textBlock = new TextBlock(
                f2, Content.ofTextFile("data/languages/korean.txt"));
        textBlock.setLocation(50f, 450f);
        textBlock.setWidth(415f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        textBlock = new TextBlock(
                f3, Content.ofTextFile("data/languages/simplified-chinese.txt"));
        textBlock.setLocation(50f, 50f);
        textBlock.setWidth(415f);
        textBlock.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_02();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_02", time0, time1);
    }
}   // End of Example_02.java
