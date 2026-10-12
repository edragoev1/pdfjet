/*
 * Example_05.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Fonts that are not embedded: the twelve text faces of the core fonts, and
 * Chinese, Japanese and Korean in the CJK fonts.
 * <p>
 * The PDF names each font and the viewer supplies it, so the document carries
 * no glyphs: it is a few kilobytes, where an embedded CJK font adds hundreds,
 * and it is written in a moment. The cost: the text takes the shapes of the
 * font the viewer has, so it does not look the same everywhere; the core fonts
 * draw only the characters of Western European languages; the CJK fonts need
 * the Adobe Asian font pack, and draw Latin letters and spaces full width; and
 * a document with a font that is not embedded cannot be PDF/A or PDF/UA. To
 * carry the glyphs with the document, use an embedded font, as Example_02 and
 * Example_03 do.
 * </p>
 *
 * @see CoreFont
 * @see CJKFont
 */
public class Example_05 {
    public Example_05() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_05.pdf")));

        Font title = new Font(pdf, CoreFont.HELVETICA_BOLD);
        title.setSize(22f);
        Font heading = new Font(pdf, CoreFont.HELVETICA_BOLD);
        heading.setSize(13f);
        Font body = new Font(pdf, CoreFont.HELVETICA);
        body.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(title, "Fonts that are not embedded");
        text.setLocation(50f, 60f);
        text.drawOn(page);

        TextBlock block = new TextBlock(body,
                "None of the fonts of this document is embedded: the PDF names each font, "
                + "and the viewer supplies it. That keeps the file very small, a few kilobytes, "
                + "where an embedded Chinese, Japanese or Korean font adds hundreds, and it is "
                + "written in a moment. What it costs is at the bottom of the page.");
        block.setLocation(50f, 75f);
        block.setWidth(512f);
        float[] end = block.drawOn(page);

        // The twelve text faces of the core fonts, each drawn in itself
        text = new TextLine(heading, "The core fonts");
        text.setLocation(50f, end[1] + 30f);
        text.drawOn(page);
        int[] coreFonts = {
            CoreFont.HELVETICA, CoreFont.HELVETICA_BOLD,
            CoreFont.HELVETICA_OBLIQUE, CoreFont.HELVETICA_BOLD_OBLIQUE,
            CoreFont.TIMES_ROMAN, CoreFont.TIMES_BOLD,
            CoreFont.TIMES_ITALIC, CoreFont.TIMES_BOLD_ITALIC,
            CoreFont.COURIER, CoreFont.COURIER_BOLD,
            CoreFont.COURIER_OBLIQUE, CoreFont.COURIER_BOLD_OBLIQUE,
        };
        String[] coreNames = {
            "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
            "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
            "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
        };
        float y = end[1] + 52f;
        for (int i = 0; i < coreFonts.length; i++) {
            Font font = new Font(pdf, coreFonts[i]);
            font.setSize(11f);
            text = new TextLine(font, coreNames[i]);
            text.setLocation(50f + (i % 2) * 256f, y);
            text.drawOn(page);
            if (i % 2 == 1) {
                y += 17f;
            }
        }

        // A greeting in each of the four CJK fonts
        text = new TextLine(heading, "The CJK fonts");
        text.setLocation(50f, y + 20f);
        text.drawOn(page);
        CJKFont[] cjkFonts = {
            CJKFont.ADOBE_MING_STD_LIGHT,
            CJKFont.ST_HEITI_SC_LIGHT,
            CJKFont.KOZ_MIN_PRO_VI_REGULAR,
            CJKFont.ADOBE_MYUNGJO_STD_MEDIUM,
        };
        String[] languages = {
            "Chinese (Traditional), Adobe Ming Std Light",
            "Chinese (Simplified), STHeiti SC Light",
            "Japanese, Kozuka Mincho Pro VI Regular",
            "Korean, Adobe Myungjo Std Medium",
        };
        String[] greetings = {
            "新年快樂!",
            "新年快乐!",
            "明けましておめでとう!",
            "새해 복 많이 받으세요!",
        };
        y += 42f;
        for (int i = 0; i < cjkFonts.length; i++) {
            text = new TextLine(body, languages[i]);
            text.setTextColor(Color.dimgray);
            text.setLocation(50f, y);
            text.drawOn(page);
            Font font = new Font(pdf, cjkFonts[i]);
            font.setSize(20f);
            text = new TextLine(font, greetings[i]);
            text.setLocation(50f, y + 26f);
            text.drawOn(page);
            y += 48f;
        }

        // What it gains, and what it costs
        text = new TextLine(heading, "Small, at a cost");
        text.setLocation(50f, y + 8f);
        text.drawOn(page);
        Font mark = new Font(pdf, CoreFont.HELVETICA_BOLD);
        mark.setSize(10f);
        String[] marks = {"+", "\u2013", "\u2013", "\u2013", "\u2013"};
        String[] items = {
            "Very small and fast: the document carries no glyphs, where one CJK font holds "
                    + "tens of thousands.",
            "The text takes the shapes and widths of the font the viewer has, so the document "
                    + "does not look the same everywhere.",
            "The core fonts draw only the characters of Western European languages.",
            "The CJK fonts need the Adobe Asian font pack, which Acrobat and Reader offer to "
                    + "download, and draw Latin letters and spaces full width.",
            "A document with a font that is not embedded cannot be PDF/A or PDF/UA, so it is "
                    + "neither archival nor accessible.",
        };
        y += 26f;
        for (int i = 0; i < items.length; i++) {
            text = new TextLine(mark, marks[i]);
            text.setLocation(52f, y + 8f);
            text.drawOn(page);
            block = new TextBlock(body, items[i]);
            block.setLocation(66f, y);
            block.setWidth(496f);
            end = block.drawOn(page);
            y = end[1] + 6f;
        }
        block = new TextBlock(body, "To carry its glyphs with the document, use an embedded "
                + "font, as Example_02 and Example_03 do.");
        block.setLocation(50f, y + 6f);
        block.setWidth(512f);
        block.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_05();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_05 => %4d ms%n", time1 - time0);
    }
}   // End of Example_05.java
