/*
 * Example_52.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_52.java
 *
 * Draws Hindi and Khmer, scripts PDFjet does not shape itself, with
 * ComplexText: Java's own text engine, java.awt.font, shapes them --
 * conjuncts, vowel signs, stacked consonants -- and PDFjet draws the glyphs it
 * found, at their places, as outlines read from the font file: sharp on
 * screen and in print, and read by screen readers, as each paragraph is
 * tagged with its text and its language. On Android 12 and later, Android's
 * own text engine shapes them, with the same code. A page of each, filled
 * with the Universal Declaration of Human Rights from its beginning: Hindi on
 * the first page, Khmer on the second.
 */
public class Example_52 {
    private static final String DEVANAGARI = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf";
    private static final String KHMER = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.otf";
    private static final String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf";

    private static final float SIZE = 10.5f;
    private static final float LEFT = 50f;
    private static final float WIDTH = 512f;
    private static final float BOTTOM = 742f;   // The page's 792 points less the margin

    public Example_52() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_52.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("The Universal Declaration of Human Rights in Hindi and Khmer");  // Required for PDF/UA !
        pdf.setLanguage("en");

        Font heading = new Font(pdf, IBMPlexSans.SemiBold);
        heading.setSize(16f);
        Font body = new Font(pdf, IBMPlexSans.Regular);
        body.setSize(9f);

        String hindi = drawPage(pdf, heading, body, "Hindi", "hi", DEVANAGARI,
                "resources/data/udhr/udhr_hin.txt");
        String khmer = drawPage(pdf, heading, body, "Khmer", "km", KHMER,
                "resources/data/udhr/udhr_khm.txt");

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        pdf.attachText("udhr-hindi.txt", hindi);
        pdf.attachText("udhr-khmer.txt", khmer);

        pdf.complete();
    }

    // Draws a page of the Declaration in the language, its paragraphs from
    // the beginning until the page is full, and returns the text drawn.
    private static String drawPage(PDF pdf, Font heading, Font body, String name, String language,
            String scriptFile, String textFile) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(heading, "Universal Declaration of Human Rights, in " + name)
                .setStructureType(StructElem.H1)
                .setLocation(LEFT, 60f)
                .drawOn(page);
        new TextLine(body, "Shaped by java.awt.font, drawn by PDFjet's ComplexText as outlines;"
                + " the text is attached to the document.")
                .setLocation(LEFT, 80f)
                .drawOn(page);

        // The script's font, and IBM Plex Sans for the characters it does
        // not have, such as digits and punctuation of the Latin script
        Font script = new Font(pdf, scriptFile).setSize(SIZE);
        Font latin = new Font(pdf, LATIN);

        StringBuilder drawn = new StringBuilder();
        float y = 96f;  // The top of the next paragraph
        for (String paragraph : paragraphs(textFile)) {
            ComplexText text = new ComplexText(script, paragraph)
                    .setFallbackFont(latin)
                    .setLanguage(language)
                    .setLocation(LEFT, y)
                    .setWidth(WIDTH);
            if (y + text.getHeight() > BOTTOM) {
                break;      // The page is full
            }
            y = text.drawOn(page)[1] + 0.5f * SIZE;
            // A blank line between paragraphs, as plain text has them
            if (drawn.length() > 0) {
                drawn.append('\n');
            }
            drawn.append(paragraph).append('\n');
        }
        return drawn.toString();
    }

    // The paragraphs of the text file, one to a line.
    private static List<String> paragraphs(String path) throws IOException {
        List<String> paragraphs = new ArrayList<String>();
        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream(path), StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                if (!line.trim().isEmpty()) {
                    paragraphs.add(line.trim());
                }
            }
        }
        return paragraphs;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_52();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_52 => %4d ms%n", time1 - time0);
    }
}   // End of Example_52.java
