package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_51.java
 *
 * Splits an existing PDF document into one PDF for each of its pages,
 * Example_51_1.pdf to Example_51_5.pdf, and writes all of its pages in reverse
 * order to Example_51.pdf. The objects that read returns are merged into every
 * PDF. The pages keep their content, resources, annotations and links; a link
 * to a page that is not in the same PDF leads nowhere.
 */
class Example_51 {
    public Example_51() throws Exception {
        BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/wirth.pdf"));
        List<PDFobj> objects = new PDF().read(bis);
        bis.close();
        int count = new PDF().getPageObjects(objects).size();

        for (int i = 1; i <= count; i++) {
            PDF part = new PDF(new BufferedOutputStream(new FileOutputStream("Example_51_" + i + ".pdf")));
            part.merge(objects, i);
            part.complete();
        }

        int[] reversed = new int[count];
        for (int i = 0; i < count; i++) {
            reversed[i] = count - i;
        }
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_51.pdf")));
        pdf.merge(objects, reversed);
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_51();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_51 => " + (time1 - time0) + " ms");
    }
}   // End of Example_51.java
