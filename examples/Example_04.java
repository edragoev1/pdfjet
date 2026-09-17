/*
 * Example_04.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Draws Chinese, Japanese and Korean text with the CJK fonts, and Latin text
 * with Helvetica. None of these fonts is embedded: the PDF names them and the
 * viewer supplies them.
 * <p>
 * The advantage is size and speed. A CJK font holds tens of thousands of
 * glyphs, and this document carries none of them, so it is a few kilobytes
 * and is written in a moment. The disadvantages: the viewer must have the
 * Adobe Asian font packs, or a substitute, and the text takes the shapes and
 * widths of whatever font it finds, so the document does not look the same
 * everywhere; the core font Helvetica is limited to the WinAnsi characters; and
 * a document with a font that is not embedded cannot claim PDF/A or PDF/UA
 * compliance. To ship the glyphs with the document, use an embedded font like
 * IBM Plex Sans JP, KR, SC or TC, as Example_02 and 19 do.
 * </p>
 *
 * @see Font
 * @see CJKFont
 */
public class Example_04 {
    public Example_04() throws Exception {
        // Create a new PDF document
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_04.pdf")));

        // Core fonts for the Latin text
        Font f0 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f5 = new Font(pdf, CoreFont.HELVETICA);

        // Create font for Traditional Chinese text
        // Uses Adobe's Ming Standard Light font (明體)
        Font f1 = new Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT);

        // Create font for Simplified Chinese text
        // Uses Adobe's Heiti SC Light font (黑体-简)
        Font f2 = new Font(pdf, CJKFont.ST_HEITI_SC_LIGHT);

        // Create font for Japanese text
        // Uses Kozuka Mincho Pro VI Regular font (小塚明朝)
        Font f3 = new Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR);

        // Create font for Korean text
        // Uses Adobe's Myungjo Standard Medium font (명조체)
        Font f4 = new Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f0, "Happy New Year!");
        text.setFontSize(26f);
        text.setLocation(70f, 90f);
        text.drawOn(page);

        text = new TextLine(f5, "In four languages, with CJK fonts that are not embedded in this PDF.");
        text.setFontSize(11f);
        text.setTextColor(Color.gray);
        text.setLocation(70f, 112f);
        text.drawOn(page);

        String[] languages = {
            "Chinese (Traditional)",
            "Chinese (Simplified)",
            "Japanese",
            "Korean",
        };
        String[] fontNames = {
            "Adobe Ming Std Light",
            "STHeiti SC Light",
            "Kozuka Mincho Pro VI Regular",
            "Adobe Myungjo Std Medium",
        };
        String[] greetings = {
            "新年快樂!",
            "新年快乐!",
            "明けましておめでとう!",
            "새해 복 많이 받으세요!",
        };
        Font[] fonts = {f1, f2, f3, f4};

        float y = 170f;
        for (int i = 0; i < languages.length; i++) {
            text = new TextLine(f0, languages[i]);
            text.setFontSize(12f);
            text.setLocation(70f, y);
            text.drawOn(page);

            text = new TextLine(f5, fontNames[i]);
            text.setFontSize(10f);
            text.setTextColor(Color.gray);
            text.setLocation(70f, y + 15f);
            text.drawOn(page);

            text = new TextLine(fonts[i], greetings[i]);
            text.setFontSize(32f);
            text.setLocation(70f, y + 60f);
            text.drawOn(page);

            Line line = new Line(70f, y + 80f, 540f, y + 80f);
            line.setStrokeColor(Color.lightgray);
            line.drawOn(page);

            y += 115f;
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_04();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_04 => %4d ms%n", time1 - time0);
    }
}   // End of Example_04.java
