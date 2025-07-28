package examples;

import com.pdfjet.PDF;
import com.pdfjet.PDFobj;
import com.pdfjet.Page;
import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.util.List;

public class Example_53 {
    public Example_53(String fileName) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_53.pdf")));
        List<PDFobj> objects;
        try (BufferedInputStream bis = new BufferedInputStream(new FileInputStream(fileName))) {
            objects = pdf.read(bis);
        }

        List<PDFobj> pages = pdf.getPageObjects(objects);
        for (int i = 0; i < pages.size(); i++) {
            Page page = new Page(pdf, pages.get(i));
            page.drawLine(0f, 0f, 200f, 200f);
            page.complete(objects);
        }
        pdf.addObjects(objects);
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        new Example_53("../testPDFs/cairo-graphics-1.pdf");
        // new Example_53("../testPDFs/cairo-graphics-2.pdf");
    }
}
