/*
 * Example_07.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_07.java
 * This example draws in fonts read from their files. PDFjet works with any
 * TrueType (.ttf) or OpenType (.otf) font file: pass the path of the file to
 * the Font constructor. The fonts PDFjet carries are .otf files; the third
 * font here is a .ttf on purpose, IBM's TrueType edition of IBM Plex Sans in
 * examples/fonts, to show a .ttf at work. A font is embedded as a subset of
 * the glyphs the document draws, unless it is set to stay whole.
 */
public class Example_07 {
    public Example_07() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_07.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Fonts from .otf and .ttf Files");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Fonts from .otf and .ttf Files");
        text.setStructureType(StructElem.H1);
        text.setFontSize(22f);
        text.setLocation(50f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "PDFjet works with any TrueType (.ttf) or OpenType (.otf) font file: pass "
                + "the path of the file to the Font constructor. The fonts that come with "
                + "PDFjet, the font constants such as IBMPlexSans and IBMPlexSansJP, are "
                + ".otf files. The paragraph below is drawn four times; the third is from a "
                + ".ttf file on purpose: IBM's TrueType edition of IBM Plex Sans, which the "
                + "examples have in examples/fonts, the same design as the second. A font is "
                + "embedded with only the glyphs the document draws, which keeps the file small.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        String[] files = {
            IBMPlexSansJP.Regular,
            IBMPlexSans.Regular,
            "examples/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf",
            IBMPlexSerif.Regular,
        };
        String[] kinds = {
            "OpenType with CFF outlines, keyed by CIDs, from the .otf file, embedded as a subset",
            "OpenType with CFF outlines, from the .otf file, embedded as a subset",
            "TrueType, IBM Plex Sans again, from IBM's .ttf file, embedded as a subset",
            "A font kept whole: subsetting turned off",
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
            text.setTextColor(Color.dimgray);
            text.setLocation(50f, y + 15f);
            text.drawOn(page);

            Font font = new Font(pdf, files[i]);
            font.setSubset(i != 3);
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
        new Example_07();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_07 => %4d ms%n", time1 - time0);
    }
}   // End of Example_07.java
