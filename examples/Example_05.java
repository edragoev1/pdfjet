/*
 * Example_05.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Embedded fonts: the same words in five weights of IBM Plex Sans, in a PDF/UA
 * document.
 * <p>
 * An embedded font travels with the document, so the text looks the same in
 * every viewer, every character of the font can be drawn, and the document can
 * be PDF/UA and PDF/A. The fourteen core fonts, Helvetica, Times and Courier,
 * are in every viewer and make the smallest documents, but they draw only the
 * WinAnsi characters, and PDF/UA and PDF/A do not allow them.
 * </p>
 *
 * @see Font
 * @see TextBlock
 */
public class Example_05 {
    private static final String SAMPLE = "Embedded fonts look the same in every viewer.";

    private static final int BACKGROUND = 0xf1f4f8;

    public Example_05() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_05.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Embedded Fonts");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        bold.setSize(24f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(bold, "Embedded Fonts");
        title.setStructureType(StructElem.H1);
        title.setLocation(50f, 70f);
        title.drawOn(page);

        TextBlock about = new TextBlock(regular,
                "An embedded font travels with the document: the PDF carries the font program, "
                + "so the text looks the same in every viewer, every character of the font can be "
                + "drawn, and the document can be PDF/UA and PDF/A. PDFjet comes with the IBM Plex "
                + "fonts, Sans, Serif and Mono, with Arabic, Hebrew, Thai, Japanese, Korean and "
                + "Chinese, in their weights.\n\n"
                + "The fourteen core fonts, Helvetica, Times and Courier with their bold and italic, "
                + "Symbol and ZapfDingbats, are in every viewer, so the document carries no font "
                + "program and is the smallest it can be. But they draw only the characters of "
                + "Windows Latin 1, the viewer draws them with its own version of the font, and "
                + "PDF/UA and PDF/A do not allow them. The boxes below are the same words in five "
                + "weights of IBM Plex Sans.");
        about.setLineSpacing(1.4f);
        about.setLocation(50f, 90f);
        about.setWidth(512f);
        float y = about.drawOn(page)[1] + 30f;

        String[] names = {"Light", "Regular", "Medium", "SemiBold", "Bold"};
        String[] weights = {
            IBMPlexSans.Light,
            IBMPlexSans.Regular,
            IBMPlexSans.Medium,
            IBMPlexSans.SemiBold,
            IBMPlexSans.Bold,
        };
        for (int i = 0; i < weights.length; i++) {
            Font font = new Font(pdf, weights[i]);
            font.setSize(20f);
            y = drawSample(page, regular, font, "IBM Plex Sans " + names[i], y) + 22f;
        }

        pdf.complete();
    }

    private static float drawSample(Page page, Font labelFont, Font font, String label, float y)
            throws Exception {
        TextLine caption = new TextLine(labelFont, label);
        caption.setTextColor(Color.dimgray);
        caption.setLocation(50f, y);
        caption.drawOn(page);

        TextBlock block = new TextBlock(font, SAMPLE);
        block.setBackgroundColor(BACKGROUND);
        block.setPadding(10f);
        block.setLineSpacing(1.2f);
        block.setLocation(50f, y + 8f);
        block.setWidth(512f);
        return block.drawOn(page)[1];
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_05();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_05 => %4d ms%n", time1 - time0);
    }
}   // End of Example_05.java
