/*
 * Example_37.java
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
 * Example_37.java
 */
class Example_37 {
    public Example_37(String fileName) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_37.pdf")));
        List<PDFobj> objects = pdf.read(new FileInputStream(fileName));

        Font f1 = new Font(
                objects,
                new FileInputStream(IBMPlexSans.Regular));
        f1.setSize(72f);

        TextLine text = new TextLine(f1, "This is a test!");
        text.setLocation(150f, 350f);
        text.setTextColor(Color.peru);

        List<PDFobj> pages = pdf.getPageObjects(objects);
        for (PDFobj pageObj : pages) {
            GraphicsState gs = new GraphicsState();
            gs.setAlphaStroking(0.75f);         // Stroking alpha
            gs.setAlphaNonStroking(0.75f);      // Non-stroking alpha
            pageObj.setGraphicsState(gs, objects);

            Page page = new Page(pdf, pageObj);
            page.addResource(f1, objects);
            page.setBrushColor(Color.blue);
            // page.drawString(f1, "Hello, World!", 50f, 200f);
            text.drawOn(page);

            page.complete(objects); // The graphics stack is unwinded automatically
        }
        pdf.addObjects(objects);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_37("data/testPDFs/wirth.pdf");
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_37 => %4d ms%n", time1 - time0);
    }
}   // End of Example_37.java
