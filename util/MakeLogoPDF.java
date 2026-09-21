/*
 * MakeLogoPDF.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import java.io.*;
import com.pdfjet.*;

/**
 * Writes data/testPDFs/PDFjetLogo.pdf, the logo of pdfjet.com as the vector
 * graphics of one Letter page, which Example_20 reads and draws on its
 * letterhead and Example_41 merges. The logo is drawn from
 * images/readme/pdfjet-logo.svg at its own size, 224 by 72 points, in the
 * bottom left corner of the page, where the content of the page draws it from
 * the origin.
 */
public class MakeLogoPDF {
    private MakeLogoPDF() {
    }

    /**
     * Writes the file.
     *
     * @param args not used.
     * @throws Exception if the SVG file cannot be read or the PDF written.
     */
    public static void main(String[] args) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("data/testPDFs/PDFjetLogo.pdf")));
        Page page = new Page(pdf, Letter.PORTRAIT);
        SVGImage logo = new SVGImage("images/readme/pdfjet-logo.svg");
        logo.setLocation(0f, page.getHeight() - logo.getHeight());
        logo.drawOn(page);
        pdf.complete();
        System.out.printf("data/testPDFs/PDFjetLogo.pdf => %.0f by %.0f points%n",
                logo.getWidth(), logo.getHeight());
    }
}   // End of MakeLogoPDF.java
