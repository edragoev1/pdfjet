package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_41.java
 *
 * Merges existing PDF documents into one, after a cover page drawn with PDFjet.
 * The pages of each document follow in their order and keep their content,
 * resources, annotations and links. The parts of a document that belong to the
 * whole document, such as its bookmarks, form fields and tagging, are left out.
 */
class Example_41 {
    public Example_41() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_41.pdf")));

        String[] fileNames = {
            "data/testPDFs/wirth.pdf",
            "data/testPDFs/rc65-16e.pdf",
            "data/testPDFs/PDFjetLogo.pdf"
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

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_41();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_41 => %4d ms%n", time1 - time0);
    }
}   // End of Example_41.java
