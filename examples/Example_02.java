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

        Font f0 = new Font(pdf, IBMPlexSans.Regular);
        f0.setSize(12f);

        Font f1 = new Font(pdf, IBMPlexSansJP.Regular);
        f1.setSize(12f);

        Font f2 = new Font(pdf, IBMPlexSansKR.Regular);
        f2.setSize(12f);

        Font f3 = new Font(pdf, IBMPlexSansSC.Regular);
        f3.setSize(12f);

        Font f4 = new Font(pdf, IBMPlexSansTC.Regular);
        f4.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The heading is in IBM Plex Sans, and the characters it has no glyph for,
        // the name of the language, are in the fallback font. The names are
        // escapes, so that the source Example_32 prints stays ASCII.
        new TextLine(f0, "This block is Japanese: \u65e5\u672c\u8a9e").setFallbackFont(f1).setLocation(50f, 50f).drawOn(page);

        TextBlock textBlock = new TextBlock(
                f1, Content.ofTextFile("data/languages/japanese.txt"));
        textBlock.setLanguage("ja");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(512f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Korean: \ud55c\uad6d\uc5b4").setFallbackFont(f2).setLocation(50f, 50f).drawOn(page);

        textBlock = new TextBlock(
                f2, Content.ofTextFile("data/languages/korean.txt"));
        textBlock.setLanguage("ko");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(512f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Simplified Chinese: \u7b80\u4f53\u4e2d\u6587").setFallbackFont(f3).setLocation(50f, 50f).drawOn(page);

        textBlock = new TextBlock(
                f3, Content.ofTextFile("data/languages/simplified-chinese.txt"));
        textBlock.setLanguage("zh-Hans");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(512f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Traditional Chinese: \u7e41\u9ad4\u4e2d\u6587").setFallbackFont(f4).setLocation(50f, 50f).drawOn(page);

        textBlock = new TextBlock(
                f4, Content.ofTextFile("data/languages/traditional-chinese.txt"));
        textBlock.setLanguage("zh-Hant");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(512f);
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
