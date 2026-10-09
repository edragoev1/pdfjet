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
 * This example reads fonts from OpenType and TrueType files. Any .otf or
 * .ttf file on the computer is a font for PDFjet. A font is
 * embedded as a subset of the glyphs the document draws, unless it is set
 * to stay whole.
 */
public class Example_28 {
    public Example_28() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_28.pdf")));
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
                "PDFjet reads OpenType and TrueType fonts as they are: pass the path "
                + "of any .otf or .ttf file on the computer to the Font constructor. "
                + "The IBMPlexSans and NotoSans constants are the paths of the fonts "
                + "that come with PDFjet. A font is embedded with only the "
                + "glyphs the document draws, which keeps the file small, whatever its "
                + "outlines. The paragraph below is drawn four times.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        String[] files = {
            "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
            IBMPlexSans.Regular,
            SourceSerif4.Regular,
            NotoSans.Regular,
        };
        String[] kinds = {
            "OpenType with CFF outlines, from the .otf file, embedded as a subset",
            "TrueType, from the .ttf file, embedded as a subset",
            "Another TrueType font, embedded as a subset",
            "A TrueType font kept whole: subsetting turned off",
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
        new Example_28();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_28 => %4d ms%n", time1 - time0);
    }
}   // End of Example_28.java
