/*
 * Example_28.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_28.java
 * This example reads fonts from OpenType and TrueType files and from the
 * .stream files of the same fonts. Any .otf or .ttf file on the computer is a
 * font for PDFjet; a .stream file is the same font, compressed once, so that
 * it loads and embeds faster.
 */
public class Example_28 {
    public Example_28() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_28.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Fonts from .otf, .ttf and .stream Files");
        text.setFontSize(22f);
        text.setLocation(50f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "PDFjet reads OpenType and TrueType fonts as they are: pass the path "
                + "of any .otf or .ttf file on the computer to the Font constructor. "
                + "The .stream files that come with PDFjet hold the same fonts, "
                + "compressed once, so that a font loads and embeds faster; the "
                + "IBMPlexSans and NotoSans constants are their paths. "
                + "The paragraph below is drawn four times, from the two kinds of file.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        String[] files = {
            "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
            "fonts/NotoSans/NotoSans-Regular.ttf",
            IBMPlexSans.Regular,
            NotoSans.Regular,
        };
        String[] kinds = {
            "OpenType, with CFF outlines, read from the .otf file",
            "TrueType, read from the .ttf file",
            "The same OpenType font from its .stream file",
            "The same TrueType font from its .stream file",
        };
        String sample = "The quick brown fox jumps over the lazy dog. "
                + "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία. "
                + "Съешь же ещё этих мягких французских булок, да выпей чаю.";

        float y = xy[1] + 30f;
        for (int i = 0; i < files.length; i++) {
            text = new TextLine(f2, files[i]);
            text.setFontSize(11f);
            text.setLocation(50f, y);
            text.drawOn(page);

            text = new TextLine(f1, kinds[i]);
            text.setFontSize(10f);
            text.setTextColor(Color.gray);
            text.setLocation(50f, y + 15f);
            text.drawOn(page);

            Font font = new Font(pdf, files[i]);
            textBlock = new TextBlock(font, sample);
            textBlock.setFontSize(13f);
            textBlock.setLineSpacing(1.4f);
            textBlock.setLocation(50f, y + 28f);
            textBlock.setWidth(512f);
            xy = textBlock.drawOn(page);
            y = xy[1] + 30f;
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_28();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_28 => %4d ms%n", time1 - time0);
    }
}   // End of Example_28.java
