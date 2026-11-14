/*
 * Example_52.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.awt.font.FontRenderContext;
import java.awt.font.GlyphVector;
import java.awt.font.LineBreakMeasurer;
import java.awt.font.TextAttribute;
import java.awt.font.TextLayout;
import java.awt.geom.AffineTransform;
import java.awt.geom.Point2D;
import java.io.*;
import java.text.AttributedCharacterIterator;
import java.text.AttributedString;
import java.util.HashMap;
import java.util.Map;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_52.java
 *
 * Draws Hindi and Khmer as real text: Java's own text engine, java.awt.font,
 * shapes them -- conjuncts, vowel signs, stacked consonants, English words
 * inside -- and Page.drawGlyphs draws the glyphs it found, at their places,
 * with the text they stand for, so that copying, searching and screen readers
 * get it. PDFjet does not shape these scripts itself; any engine that finds
 * the glyphs can: java.awt.font here, Android's TextShaper, Apple's Core Text
 * or HarfBuzz elsewhere. The text is Article 1 of the Universal Declaration
 * of Human Rights.
 */
public class Example_52 {
    // Fractional metrics, as a PDF has them: no rounding to whole pixels
    private static final FontRenderContext CONTEXT =
            new FontRenderContext(new AffineTransform(), true, true);

    private static final String DEVANAGARI = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf";
    private static final String KHMER = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.ttf";
    private static final String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";

    public Example_52() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_52.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Hindi and Khmer as real text");  // Required for PDF/UA !
        pdf.setLanguage("en");

        Font heading = new Font(pdf, IBMPlexSans.SemiBold);
        heading.setSize(16f);
        Font body = new Font(pdf, IBMPlexSans.Regular);
        body.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(heading, "Article 1 of the Universal Declaration of Human Rights")
                .setStructureType(StructElem.H1)
                .setLocation(50f, 60f)
                .drawOn(page);
        new TextLine(body, "Shaped by java.awt.font, drawn by PDFjet with drawGlyphs: real text, in Hindi and in Khmer.")
                .setLocation(50f, 82f)
                .drawOn(page);

        float y = 100f;
        y = drawShaped(pdf, page,
                "सभी मनुष्यों को गौरव और अधिकारों के मामले में जन्मजात स्वतन्त्रता और समानता प्राप्त है । "
                + "उन्हें बुद्धि और अन्तरात्मा की देन प्राप्त है और परस्पर उन्हें भाईचारे के भाव से बर्ताव करना चाहिए ।",
                "hi", DEVANAGARI, 12f, 50f, y, 512f);
        y = drawShaped(pdf, page,
                "មនុស្សទាំងអស់ កើតមកមានសេរីភាព និងសមភាព ក្នុងផ្នែកសេចក្ដីថ្លៃថ្នូរនិងសិទ្ធិ។ "
                + "មនុស្ស មានវិចារណញ្ញាណនិងសតិសម្បជញ្ញៈជាប់ពីកំណើត "
                + "ហើយគប្បីប្រព្រឹត្ដចំពោះគ្នាទៅវិញទៅមកក្នុងស្មារតីភាតរភាពជាបងប្អូន។",
                "km", KHMER, 12f, 50f, y + 14f, 512f);

        pdf.complete();
    }

    // Draws a paragraph in a script PDFjet does not shape, wrapped to the
    // width, as real text, and returns the y below its last line. The script's
    // font is used where it has the characters, IBM Plex Sans elsewhere; the
    // same font files are loaded by java.awt.font, which shapes, and by PDFjet,
    // which embeds them, so that the glyph numbers match.
    private static float drawShaped(PDF pdf, Page page, String text, String language,
            String scriptFile, float size, float x, float y, float width) throws Exception {
        java.awt.Font script = java.awt.Font.createFont(java.awt.Font.TRUETYPE_FONT, new File(scriptFile)).deriveFont(size);
        java.awt.Font latin = java.awt.Font.createFont(java.awt.Font.TRUETYPE_FONT, new File(LATIN)).deriveFont(size);
        Map<java.awt.Font, Font> embedded = new HashMap<java.awt.Font, Font>();
        embedded.put(script, new Font(pdf, new FileInputStream(scriptFile)).setSize(size));
        embedded.put(latin, new Font(pdf, new FileInputStream(LATIN)).setSize(size));

        // Each character in the script's font where it has it, else in the Latin one
        AttributedString styled = new AttributedString(text);
        for (int i = 0; i < text.length(); ) {
            int c = text.codePointAt(i);
            int n = Character.charCount(c);
            styled.addAttribute(TextAttribute.FONT, script.canDisplay(c) ? script : latin, i, i + n);
            i += n;
        }

        page.addBDC(StructElem.P, language, null, null);
        char[] chars = text.toCharArray();
        LineBreakMeasurer lines = new LineBreakMeasurer(styled.getIterator(), CONTEXT);
        while (lines.getPosition() < text.length()) {
            int start = lines.getPosition();
            TextLayout line = lines.nextLayout(width);
            int end = lines.getPosition();
            float baseline = y + line.getAscent();
            // The line, a run of one font at a time: its glyphs, their places
            // and the characters each one comes from (its cluster)
            AttributedCharacterIterator runs = styled.getIterator(null, start, end);
            float offset = 0f;
            int runStart = start;
            while (runStart < end) {
                runs.setIndex(runStart);
                int runEnd = Math.min(runs.getRunLimit(TextAttribute.FONT), end);
                java.awt.Font font = (java.awt.Font) runs.getAttribute(TextAttribute.FONT);
                GlyphVector shaped = font.layoutGlyphVector(
                        CONTEXT, chars, runStart, runEnd, java.awt.Font.LAYOUT_LEFT_TO_RIGHT);
                int count = shaped.getNumGlyphs();
                int[] glyphs = new int[count];
                float[] positions = new float[2 * count];
                int[] clusters = new int[count];
                for (int i = 0; i < count; i++) {
                    Point2D p = shaped.getGlyphPosition(i);
                    glyphs[i] = shaped.getGlyphCode(i);
                    positions[2 * i] = x + offset + (float) p.getX();
                    positions[2 * i + 1] = baseline + (float) p.getY();
                    clusters[i] = shaped.getGlyphCharIndex(i);
                }
                page.drawGlyphs(embedded.get(font), glyphs, positions, clusters,
                        text.substring(runStart, runEnd), language);
                offset += (float) shaped.getGlyphPosition(count).getX();
                runStart = runEnd;
            }
            y = baseline + line.getDescent() + line.getLeading();
        }
        page.addEMC();
        return y;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_52();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_52 => %4d ms%n", time1 - time0);
    }
}   // End of Example_52.java
