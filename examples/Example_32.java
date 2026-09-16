/*
 * Example_32.java
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
 * Example_32.java
 */
public class Example_32 {
    public Example_32() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_32.pdf")));

        Font font = new Font(pdf, JetBrainsMono.Regular);
        font.setSize(10f);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("new", Color.red);
        colors.put("class", Color.blue);
        colors.put("void", Color.green);
        float[] grayColor = new float[] {0.2f, 0.2f, 0.2f};

        Page page = new Page(pdf, Letter.PORTRAIT);
        float x = 50f;
        float y = 50f;
        float leading = font.getBodyHeight();
        List<String> lines = Content.linesOfTextFile("examples/Example_02.java");
        for (String line : lines) {
            new TextLine(font, line).setTextColor(grayColor).setHighlightColors(colors).setLocation(x, y).drawOn(page);
            y += leading;
            if (y > (page.getHeight() - 20f)) {
                page = new Page(pdf, Letter.PORTRAIT);
                y = 50f;
            }
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_32();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_32 => %4d ms%n", time1 - time0);
    }
}   // End of Example_32.java
