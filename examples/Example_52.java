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
import java.nio.charset.StandardCharsets;
import java.text.AttributedCharacterIterator;
import java.text.AttributedString;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_52.java
 *
 * Draws Hindi and Khmer, scripts PDFjet does not shape itself: Java's own
 * text engine, java.awt.font, shapes them -- conjuncts, vowel signs, stacked
 * consonants -- and Page.drawGlyphs draws the glyphs it found, at their
 * places, as outlines read from the font file: sharp on screen and in print,
 * and read by screen readers, as each paragraph is tagged with its text and
 * its language. Any engine that finds the glyphs can do the shaping:
 * java.awt.font here, Android's TextShaper, Apple's Core Text or HarfBuzz
 * elsewhere. A page of each, filled with the Universal Declaration of Human
 * Rights from its beginning: Hindi on the first page, Khmer on the second.
 */
public class Example_52 {
    // Fractional metrics, as a PDF has them: no rounding to whole pixels
    private static final FontRenderContext CONTEXT =
            new FontRenderContext(new AffineTransform(), true, true);

    private static final String DEVANAGARI = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf";
    private static final String KHMER = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.ttf";
    private static final String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf";

    private static final float SIZE = 10.5f;
    private static final float LEFT = 50f;
    private static final float WIDTH = 512f;
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

        String hindi = drawPage(pdf, heading, body, "Hindi", "hi", DEVANAGARI,
                "resources/data/udhr/udhr_hin.txt");
        String khmer = drawPage(pdf, heading, body, "Khmer", "km", KHMER,
                "resources/data/udhr/udhr_khm.txt");

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        pdf.attachText("udhr-hindi.txt", hindi);
        pdf.attachText("udhr-khmer.txt", khmer);

        pdf.complete();
    }

    // Draws a page of the Declaration in the language, its paragraphs from
    // the beginning until the page is full, and returns the text drawn.
    private static String drawPage(PDF pdf, Font heading, Font body, String name, String language,
            String scriptFile, String textFile) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(heading, "Universal Declaration of Human Rights, in " + name)
                .setStructureType(StructElem.H1)
                .setLocation(LEFT, 60f)
                .drawOn(page);
        new TextLine(body, "Shaped by java.awt.font, drawn by PDFjet with drawGlyphs as outlines;"
                + " the text is attached to the document.")
                .setLocation(LEFT, 80f)
                .drawOn(page);

        // The same font files are loaded by java.awt.font, which shapes, and
        // by PDFjet, which reads the outlines, so that the glyph numbers match.
        java.awt.Font script = java.awt.Font.createFont(java.awt.Font.TRUETYPE_FONT, new File(scriptFile)).deriveFont(SIZE);
        java.awt.Font latin = java.awt.Font.createFont(java.awt.Font.TRUETYPE_FONT, new File(LATIN)).deriveFont(SIZE);
        Map<java.awt.Font, Font> fonts = new HashMap<java.awt.Font, Font>();
        Font scriptFont = new Font(pdf, new FileInputStream(scriptFile)).setSize(SIZE);
        fonts.put(script, scriptFont);
        fonts.put(latin, new Font(pdf, new FileInputStream(LATIN)).setSize(SIZE));
        float ascent = scriptFont.getAscent();
        float descent = scriptFont.getDescent();

        StringBuilder drawn = new StringBuilder();
        float[] y = {96f};  // The top of the next line
        for (String paragraph : paragraphs(textFile)) {
            String text = drawParagraph(page, paragraph, language, script, latin, fonts, y, ascent, descent);
            if (text.isEmpty()) {
                break;      // The page is full
            }
            // A blank line between paragraphs, as plain text has them
            if (drawn.length() > 0) {
                drawn.append('\n');
            }
            drawn.append(text).append('\n');
            if (text.length() < paragraph.length()) {
                break;      // The page is full
            }
            y[0] += 0.5f * SIZE;
        }
        return drawn.toString();
    }

    // Draws the paragraph, wrapped to the width, as many of its lines as fit
    // above the bottom of the page, from the top y[0], which it moves below
    // them, and returns the text of the lines drawn.
    // The script's font is used where it has the characters, IBM Plex Sans
    // elsewhere.
    private static String drawParagraph(Page page, String text, String language,
            java.awt.Font script, java.awt.Font latin, Map<java.awt.Font, Font> fonts,
            float[] y, float ascent, float descent) throws Exception {
        // Each character in the script's font where it has it, else in the Latin one
        AttributedString styled = new AttributedString(text);
        for (int i = 0; i < text.length(); ) {
            int c = text.codePointAt(i);
            int n = Character.charCount(c);
            styled.addAttribute(TextAttribute.FONT, script.canDisplay(c) ? script : latin, i, i + n);
            i += n;
        }

        char[] chars = text.toCharArray();
        LineBreakMeasurer lines = new LineBreakMeasurer(styled.getIterator(), CONTEXT);
        int drawnEnd = 0;
        while (lines.getPosition() < text.length()) {
            if (y[0] + ascent + descent > BOTTOM) {
                break;
            }
            if (drawnEnd == 0) {
                page.addBDC(StructElem.P, language, null, null);
            }
            int start = lines.getPosition();
            lines.nextLayout(WIDTH);
            int end = lines.getPosition();
            float baseline = y[0] + ascent;
            // The line, a run of one font at a time: its glyphs and their places
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
                for (int i = 0; i < count; i++) {
                    Point2D p = shaped.getGlyphPosition(i);
                    glyphs[i] = shaped.getGlyphCode(i);
                    positions[2 * i] = LEFT + offset + (float) p.getX();
                    positions[2 * i + 1] = baseline + (float) p.getY();
                }
                page.drawGlyphs(fonts.get(font), glyphs, positions,
                        text.substring(runStart, runEnd), language);
                offset += (float) shaped.getGlyphPosition(count).getX();
                runStart = runEnd;
            }
            y[0] = baseline + descent + 0.25f * SIZE;
            drawnEnd = end;
        }
        if (drawnEnd > 0) {
            page.addEMC();
        }
        return text.substring(0, drawnEnd);
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
