/*
 * Example_04.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_04.java
 */
public class Example_04 {
    public Example_04() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_04.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("The Universal Declaration of Human Rights in Japanese and Korean");

        Font f0 = new Font(pdf, IBMPlexSans.Regular);
        f0.setSize(12f);

        Font f1 = new Font(pdf, IBMPlexSansJP.Regular);
        f1.setSize(12f);

        Font f2 = new Font(pdf, IBMPlexSansKR.Regular);
        f2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The heading is in IBM Plex Sans, and the characters it has no glyph for,
        // the name of the language, are in the fallback font.
        // The line above each block is its heading
        new TextLine(f0, "This block is Japanese: \u65e5\u672c\u8a9e").setFallbackFont(f1)
                .setStructureType(StructElem.H1).setLocation(50f, 50f).drawOn(page);

        TextBlock textBlock = new TextBlock(
                f1, Content.ofTextFile("data/languages/japanese.txt"));
        textBlock.setLanguage("ja");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(512f);
        textBlock.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Korean: \ud55c\uad6d\uc5b4").setFallbackFont(f2)
                .setStructureType(StructElem.H1).setLocation(50f, 50f).drawOn(page);

        textBlock = new TextBlock(
                f2, Content.ofTextFile("data/languages/korean.txt"));
        textBlock.setLanguage("ko");
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(512f);
        textBlock.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_04();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_04 => %4d ms%n", time1 - time0);
    }
}   // End of Example_04.java
