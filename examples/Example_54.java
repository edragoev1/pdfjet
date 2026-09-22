/*
 * Example_54.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_54.java
 * A Markdown text, data/markdown/pdfjet.md, drawn down the pages by Markdown
 * as a PDF/UA document: its headings, paragraphs, lists, quote, code, table
 * and image, each tagged for screen readers, with a number at the foot of
 * every page. The image is read from data/markdown, the directory that
 * setImageDirectory names.
 */
public class Example_54 {
    public Example_54() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_54.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("PDFjet");

        Font regular = new Font(pdf, IBMPlexSans.Regular).setSize(11f);
        Font bold = new Font(pdf, IBMPlexSans.Bold).setSize(11f);
        Font italic = new Font(pdf, IBMPlexSans.Italic).setSize(11f);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic).setSize(11f);
        Font code = new Font(pdf, IBMPlexMono.Regular).setSize(9.5f);

        Markdown markdown = new Markdown(regular, bold, italic, boldItalic, code);
        markdown.setImageDirectory("data/markdown");
        List<Page> pages = new ArrayList<Page>();
        markdown.drawOn(pdf, Content.ofTextFile("data/markdown/pdfjet.md"), pages, Letter.PORTRAIT);

        for (int i = 0; i < pages.size(); i++) {
            TextLine number = new TextLine(regular, String.valueOf(i + 1));
            number.setFontSize(9f);
            pages.get(i).addFooter(number, 36f);
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_54();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_54 => %4d ms%n", time1 - time0);
    }
}   // End of Example_54.java
