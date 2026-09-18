/*
 * Example_02.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_02.java
 */
public class Example_02 {
    public Example_02() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_02.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("The Universal Declaration of Human Rights in Four Languages");

        Font f1 = new Font(pdf, IBMPlexSansJP.Regular);
        f1.setSize(14f);

        Font f2 = new Font(pdf, IBMPlexSansKR.Regular);
        f2.setSize(14f);

        Font f3 = new Font(pdf, IBMPlexSansSC.Regular);
        f3.setSize(14f);

        Font f4 = new Font(pdf, IBMPlexSansTC.Regular);
        f4.setSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f1, "This block is Japanese.").setLocation(50f, 50f).drawOn(page);

        TextBlock textBlock = new TextBlock(
                f1, Content.ofTextFile("data/languages/japanese.txt"));
        textBlock.setLanguage("ja");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(415f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f2, "This block is Korean.").setLocation(50f, 50f).drawOn(page);

        textBlock = new TextBlock(
                f2, Content.ofTextFile("data/languages/korean.txt"));
        textBlock.setLanguage("ko");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(415f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f3, "This block is Simplified Chinese.").setLocation(50f, 50f).drawOn(page);

        textBlock = new TextBlock(
                f3, Content.ofTextFile("data/languages/simplified-chinese.txt"));
        textBlock.setLanguage("zh-Hans");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(415f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f4, "This block is Traditional Chinese.").setLocation(50f, 50f).drawOn(page);

        textBlock = new TextBlock(
                f4, Content.ofTextFile("data/languages/traditional-chinese.txt"));
        textBlock.setLanguage("zh-Hant");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(415f);
        textBlock.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_02();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_02 => %4d ms%n", time1 - time0);
    }
}   // End of Example_02.java
