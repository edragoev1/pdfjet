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
 * own text engine shapes them, with the same code. The Universal Declaration
 * of Human Rights in Hindi, then in Khmer: the paragraphs flow from page to
 * page, a paragraph cut where a page ends.
 */
public class Example_52 {
    private static final String DEVANAGARI = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf";
    private static final String KHMER = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf";
    private static final String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf";

    private static final float SIZE = 10.5f;
    private static final float LEFT = 50f;
    private static final float WIDTH = 512f;
    private static final float TOP = 50f;       // The top margin
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

        String hindi = draw(pdf, heading, body, "Hindi", "hi", DEVANAGARI,
                "resources/data/udhr/udhr_hin.txt");
        String khmer = draw(pdf, heading, body, "Khmer", "km", KHMER,
                "resources/data/udhr/udhr_khm.txt");

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        pdf.attachText("udhr-hindi.txt", hindi);
        pdf.attachText("udhr-khmer.txt", khmer);

        pdf.complete();
    }

    // Draws the Declaration in the language, from a new page, on as many
    // pages as it takes, and returns its text.
    private static String draw(PDF pdf, Font heading, Font body, String name, String language,
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

        List<String> paragraphs = paragraphs(textFile);
        ComplexText text = new ComplexText(script, paragraphs)
                .setFallbackFont(latin)
                .setLanguage(language)
                .setWidth(WIDTH)
                .setParagraphGap(0.5f * SIZE);
        float top = 96f;    // Below the heading, on the first page
        while (true) {
            text.setHeight(BOTTOM - top).setLocation(LEFT, top).drawOn(page);
            if (!text.hasMoreText()) {
                break;
            }
            page = new Page(pdf, Letter.PORTRAIT);
            top = TOP;
        }

        // A blank line between paragraphs, as plain text has them
        StringBuilder all = new StringBuilder();
        for (String paragraph : paragraphs) {
            if (all.length() > 0) {
                all.append('\n');
            }
            all.append(paragraph).append('\n');
        }
        return all.toString();
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
