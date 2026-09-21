/*
 * Example_17.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_17.java
 *
 * Draws a PNG image of each kind PDFjet reads and says what each one is: the
 * color types and the bit depths, the two ways a PNG carries transparency,
 * and a PNG that asks to be drawn at 300 dots per inch. The images of the
 * grid are from PngSuite, the test images of the PNG format, drawn at four
 * times their 32 by 32 pixels so their samples can be seen; the samples
 * themselves are checked in PNGImageTest, against the whole of PngSuite.
 */
public class Example_17 {
    // The images of the grid: the file, what it is, and whether it carries
    // transparency, which is drawn over a color so it can be seen.
    private static final String[][] IMAGES = {
        {"BASN3P08", "Palette, 8 bits", "Vertical bands of red, orange, yellow, green, cyan, blue and magenta, each shading from black at the top to white at the bottom, from a palette of 256 colors.", "no"},
        {"BASN0G08", "Grayscale, 8 bits", "Horizontal bands of gray, each shading from black at the top to white at the bottom, 8 bits a pixel.", "no"},
        {"BASN2C08", "Truecolor, 8 bits", "Horizontal bands of yellow, magenta, cyan and gray, each shading from pale at the top of the band to full color at the bottom of it, in 8 bit color.", "no"},
        {"BASN0G16", "Grayscale, 16 bits", "A ramp of 16 bit gray samples that brightens from black at the left to white near the right edge and falls away again.", "no"},
        {"BASN2C16", "Truecolor, 16 bits", "Red, green and blue of 16 bit samples mixed across the square: yellow at the top left, green at the top right, red at the bottom left and blue at the bottom right.", "no"},
        {"BASN6A08", "Truecolor with alpha", "A rainbow that shades from red at the top to blue at the bottom, which its alpha channel fades from transparent at the left to opaque at the right, over a yellow square.", "yes"},
        {"BASN4A08", "Grayscale with alpha", "A gray ramp from white at the top to black at the bottom, which its alpha channel fades from transparent at the left to opaque at the right, over a yellow square.", "yes"},
        {"TP1N3P08", "Palette with transparency", "A black cube with the word NeXT on it in colored letters, and transparent pixels around it from the tRNS chunk of its palette, over a yellow square.", "yes"},
    };

    public Example_17() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_17.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("PNG Images");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(f1, "PNG Images");
        title.setStructureType(StructElem.H1);
        title.setFontSize(18f);
        title.setLocation(50f, 50f);
        title.drawOn(page);

        TextBlock textBlock = new TextBlock(f2,
                "PDFjet reads a PNG of any color type and bit depth the format has: a "
                + "palette, grayscale or truecolor image of 1, 2, 4, 8 or 16 bits a "
                + "sample. The samples go into the PDF as they are, so an image is "
                + "embedded once and drawn at any size without being resampled. The two "
                + "ways a PNG carries transparency -- the alpha channel of a truecolor "
                + "or grayscale image, and the tRNS chunk of a palette -- both become "
                + "the soft mask of the image, which is why the three below let the "
                + "yellow square behind them through. An interlaced PNG is refused with "
                + "a message that says how to convert it.");
        textBlock.setFontSize(11f);
        textBlock.setLineSpacing(1.4f);
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        // The grid: four across, each image over the name of what it is.
        float size = 128f;          // 32 pixels drawn at four times their size
        float columnWidth = 128f;
        float rowHeight = 168f;
        float top = xy[1] + 24f;
        for (int i = 0; i < IMAGES.length; i++) {
            float x = 50f + (i%4)*columnWidth;
            float y = top + (i/4)*rowHeight;

            // A transparent image is drawn over a color, which its soft mask
            // lets through where the image is not opaque.
            if (IMAGES[i][3].equals("yes")) {
                page.addArtifactBMC();
                page.setBrushColor(0xFFE9A0);       // A pale yellow
                page.fillRect(x, y, size, size);
                page.addEMC();
            }

            Image image = new Image(pdf, "PngSuite/" + IMAGES[i][0] + ".PNG");
            image.setAltDescription(IMAGES[i][2]);
            image.scaleBy(4f);
            image.setLocation(x, y);
            image.drawOn(page);

            TextLine caption = new TextLine(f2, IMAGES[i][1]);
            caption.setFontSize(9f);
            caption.setLocation(x, y + size + 14f);
            caption.drawOn(page);
        }

        // A PNG from outside PngSuite, which carries the chunks a file written
        // by a drawing program has and asks to be drawn at 300 dots per inch.
        float y = top + 2*rowHeight + 10f;
        TextLine heading = new TextLine(f1, "A PNG that asks for its own size");
        heading.setStructureType(StructElem.H2);
        heading.setFontSize(13f);
        heading.setLocation(50f, y);
        heading.drawOn(page);

        TextBlock note = new TextBlock(f2,
                "The pHYs chunk of a PNG says how large the image is meant to be. This "
                + "one is 380 by 100 pixels at 300 dots per inch, so it is drawn 91.2 by "
                + "24 points: a quarter of the size it would be at one point for each "
                + "pixel, and sharp for it. It carries an iCCP color profile, a bKGD "
                + "background, a tIME timestamp and two IDAT chunks.");
        note.setFontSize(11f);
        note.setLineSpacing(1.4f);
        note.setLocation(50f, y + 16f);
        note.setWidth(512f);
        float[] xy2 = note.drawOn(page);

        Image chunks = new Image(pdf, "images/rgba-8bit-chunks.png");
        chunks.setAltDescription(
                "Three half transparent circles in red, green and blue that overlap, "
                + "beside the heading 8-bit RGBA PNG, 380 by 100, and the note that the "
                + "image has anti-aliased text and half transparent circles on a "
                + "transparent background, not interlaced, with iCCP, bKGD, pHYs, tIME "
                + "and two IDAT chunks.");
        chunks.setLocation(50f, xy2[1] + 14f);
        chunks.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_17();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_17 => %4d ms%n", time1 - time0);
    }
}   // End of Example_17.java
