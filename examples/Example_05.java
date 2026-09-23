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
 * Kerning with a core font: what it is, and the same words in two text blocks,
 * one above the other, drawn in Helvetica-Bold without kerning and with it.
 * <p>
 * The fonts are core fonts, of the fourteen fonts every PDF viewer has, so the
 * document carries no font program. It is small, it is written fast, and the
 * widths and the kerning pairs of the fonts are built into PDFjet, which is
 * what setKernPairs applies. The disadvantages: the viewer draws the text with
 * its own version of the font, so the look differs a little between viewers;
 * only the WinAnsi characters can be drawn, so no Cyrillic, Greek or CJK text;
 * and a document with a font that is not embedded cannot claim PDF/A or PDF/UA
 * compliance. For those, use an embedded font like IBM Plex Sans, as the other
 * examples do.
 * </p>
 *
 * @see Font#setKernPairs(boolean)
 * @see TextBlock
 */
public class Example_05 {
    // Words with the pairs of letters kerning closes up: WA, AV, AW, AY, Yo,
    // To, Vo, VA and the like.
    private static final String SAMPLE = "WAVE AWAY: Your Tokyo voyage, VAT paid.";

    private static final int BACKGROUND = 0xf1f4f8;

    public Example_05() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_05.pdf")));
        pdf.setTitle("Kerning");

        Font regular = new Font(pdf, CoreFont.HELVETICA);
        regular.setSize(11f);
        Font bold = new Font(pdf, CoreFont.HELVETICA_BOLD);
        bold.setSize(24f);

        // The same font twice: the one without kerning, which is the default,
        // and the one with it.
        Font plain = new Font(pdf, CoreFont.HELVETICA_BOLD);
        plain.setSize(30f);
        Font kerned = new Font(pdf, CoreFont.HELVETICA_BOLD);
        kerned.setSize(30f);
        kerned.setKernPairs(true);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(bold, "Kerning");
        title.setLocation(50f, 70f);
        title.drawOn(page);

        TextBlock about = new TextBlock(regular,
                "Kerning moves particular pairs of letters closer together, so that the space "
                + "between the letters of a word looks even. Every letter of a font has a width, "
                + "the box it is drawn in, and some pairs of letters leave a gap between their "
                + "boxes that the eye reads as a space: a capital A beside a V or a W, a capital T, "
                + "V or Y over a small o, an L before a T. A font lists these pairs and how far to "
                + "move each of them.\n\n"
                + "The fourteen core fonts every PDF viewer has come with their lists, which are "
                + "built into PDFjet. font.setKernPairs(true) turns kerning on for a font: PDFjet "
                + "moves the letters of each pair as it draws them, with the TJ operator, and "
                + "measures the text the same way, so that a TextBlock breaks its lines where the "
                + "kerned words end. The two blocks below are the same words in the same font, "
                + "without kerning and with it.");
        about.setLineSpacing(1.4f);
        about.setLocation(50f, 90f);
        about.setWidth(512f);
        float[] xy = about.drawOn(page);

        float y = xy[1] + 30f;
        y = drawSample(page, regular, plain, "Without kerning: font.setKernPairs(false), the default", y);
        y = drawSample(page, regular, kerned, "With kerning: font.setKernPairs(true)", y + 25f);

        // How much kerning takes off the width of the words, as PDFjet
        // measures them, to the nearest point.
        int narrower = Math.round(plain.stringWidth(SAMPLE) - kerned.stringWidth(SAMPLE));
        TextLine note = new TextLine(regular,
                "Kerning makes these words " + narrower + " points narrower at 30 points.");
        note.setTextColor(Color.gray);
        note.setLocation(50f, y + 30f);
        note.drawOn(page);

        pdf.complete();
    }

    // Draws the label, and under it the sample words in the font, in a text
    // block with a light background, and returns the bottom of the block.
    private static float drawSample(Page page, Font labelFont, Font font, String label, float y)
            throws Exception {
        TextLine caption = new TextLine(labelFont, label);
        caption.setTextColor(Color.gray);
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
