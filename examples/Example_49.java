/*
 * Example_49.java
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
 * Example_49.java
 *
 * Merges and splits existing PDF documents. merge draws a cover page and
 * merges three documents after it, into Example_49.pdf: the pages of each
 * follow in their order and keep their content, resources, annotations and
 * links, and what belongs to a whole document, such as its bookmarks, form
 * fields and tagging, is left out. split writes one PDF for each page of a
 * document, Example_49_1.pdf to Example_49_5.pdf, and all of its pages in
 * reverse order to Example_49_reversed.pdf; a link to a page that is not in
 * the same PDF leads nowhere.
 */
final class Example_49 {
    public Example_49() throws Exception {
        merge();
        split();
    }

    private void merge() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_49.pdf")));

        String[] fileNames = {
            "resources/data/testPDFs/scanned-linearized.pdf",
            "resources/data/testPDFs/sample-document.pdf",
            "resources/data/testPDFs/PDFjetLogo.pdf"
        };
        List<List<PDFobj>> documents = new ArrayList<List<PDFobj>>();
        for (String fileName : fileNames) {
            BufferedInputStream bis = new BufferedInputStream(new FileInputStream(fileName));
            documents.add(pdf.read(bis));
            bis.close();
        }

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(24f);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(f1, "Merged documents").setLocation(50f, 80f).drawOn(page);
        float y = 130f;
        for (int i = 0; i < fileNames.length; i++) {
            int pages = pdf.getPageObjects(documents.get(i)).size();
            String text = fileNames[i] + ", " + pages + (pages == 1 ? " page" : " pages");
            new TextLine(f2, text).setLocation(50f, y).drawOn(page);
            y += 20f;
        }

        for (List<PDFobj> objects : documents) {
            pdf.merge(objects);
        }

        pdf.complete();
    }

    private void split() throws Exception {
        BufferedInputStream bis = new BufferedInputStream(new FileInputStream("resources/data/testPDFs/scanned-linearized.pdf"));
        List<PDFobj> objects = new PDF().read(bis);
        bis.close();
        int count = new PDF().getPageObjects(objects).size();

        for (int i = 1; i <= count; i++) {
            PDF part = new PDF(new BufferedOutputStream(new FileOutputStream("Example_49_" + i + ".pdf")));
            part.merge(objects, i);
            part.complete();
        }

        int[] reversed = new int[count];
        for (int i = 0; i < count; i++) {
            reversed[i] = count - i;
        }
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_49_reversed.pdf")));
        pdf.merge(objects, reversed);
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_49();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_49 => %4d ms%n", time1 - time0);
    }
}   // End of Example_49.java
