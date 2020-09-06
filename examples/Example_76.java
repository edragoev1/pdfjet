package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_76.java
 *
 *  This example program will print out all the fonts that are not embedded in the PDF file we read.
 */
class Example_76 {

    public Example_76() throws Exception {
        try {
            PDF pdf = new PDF();

            BufferedInputStream stream =
                    new BufferedInputStream(new FileInputStream("data/testPDFs/Bluebook.pdf"));
            List<PDFobj> objects = pdf.read(stream);
            stream.close();
/*
            for (PDFobj obj : objects) {
                String type = obj.getValue("/Type");
                if (type.equals("/Font")
                        && obj.getValue("/Subtype").equals("/Type0") == false
                        && obj.getValue("/FontDescriptor").equals("")) {

                    System.out.println("Non-EmbeddedFont -> "
                            + obj.getValue("/BaseFont").substring(1));
                }
                else if (type.equals("/FontDescriptor")) {
                    String fontFile = obj.getValue("/FontFile");
                    if (fontFile.equals("")) {
                        fontFile = obj.getValue("/FontFile2");
                    }
                    if (fontFile.equals("")) {
                        fontFile = obj.getValue("/FontFile3");
                    }

                    if (fontFile.equals("")) {
                        System.out.println("Non-Embedded Font -> "
                                + obj.getValue("/FontName").substring(1));
                    }
                }
            }
*/
        }
        catch (Exception e) {
            e.printStackTrace();
        }

    }


    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_76();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_76 => " + (time1 - time0));
    }

}   // End of Example_76.java
