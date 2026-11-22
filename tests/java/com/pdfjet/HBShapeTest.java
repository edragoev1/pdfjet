/*
 * HBShapeTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assumptions.assumeTrue;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

import org.junit.jupiter.api.Test;

// The tests of HBShape need hb-shape, HarfBuzz's program: skipped without it.
class HBShapeTest {
    private static final String DEVANAGARI = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf";
    private static final String KHMER = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf";

    private static HBShape hbShape() {
        try {
            String version = new HBShape().getVersion();
            assumeTrue(version.contains("HarfBuzz"), "hb-shape is not installed");
        } catch (Exception e) {
            assumeTrue(false, "hb-shape is not installed");
        }
        return new HBShape();
    }

    private static List<String> udhr(String code) throws Exception {
        return Files.readAllLines(TestSupport.file("resources/data/udhr/udhr_" + code + ".txt").toPath(),
                StandardCharsets.UTF_8);
    }

    private static byte[] bytes(String path) throws Exception {
        return Files.readAllBytes(TestSupport.file(path).toPath());
    }

    // A paragraph of one font is the glyphs hb-shape gives for it, at their places
    @Test
    void givesHBShapesGlyphs() throws Exception {
        HBShape hb = hbShape();
        String paragraph = udhr("hin").get(2);
        List<ShapedLine> lines = hb.shape(new ShapingRequest(paragraph, "hi",
                new byte[][] {bytes(DEVANAGARI)}, 10f, 1e6f));
        assertEquals(1, lines.size());
        // The text in a UTF-8 file, as HBShape gives it, not as an argument:
        // Java writes arguments in the encoding of the system's locale, and
        // one it has not, as CI's Turkish run's, made the Hindi question marks
        File out = File.createTempFile("pdfjet-test-", ".json");
        File in = File.createTempFile("pdfjet-test-", ".txt");
        Files.write(in.toPath(), paragraph.getBytes(StandardCharsets.UTF_8));
        try {
            Process p = new ProcessBuilder("hb-shape", "--no-glyph-names", "--output-format=json", "--language=hi",
                    "--text-file=" + in.getPath(), TestSupport.file(DEVANAGARI).getPath()).redirectOutput(out).start();
            assertEquals(0, p.waitFor());
            String json = new String(Files.readAllBytes(out.toPath()), StandardCharsets.UTF_8).trim();
            ShapedLine line = lines.get(0);
            String[] glyphs = json.substring(2, json.length() - 2).split("\\},\\{");
            assertEquals(glyphs.length, line.getGlyphs().length);
            float pen = 0f;
            float scale = 10f / 1000;   // Noto Sans Devanagari has 1,000 units an em
            for (int i = 0; i < glyphs.length; i++) {
                int g = 0, ax = 0, dx = 0, dy = 0;
                for (String pair : glyphs[i].split(",")) {
                    String[] kv = pair.replace("\"", "").split(":");
                    int v = Integer.parseInt(kv[1]);
                    if (kv[0].equals("g")) g = v;
                    if (kv[0].equals("ax")) ax = v;
                    if (kv[0].equals("dx")) dx = v;
                    if (kv[0].equals("dy")) dy = v;
                }
                assertEquals(g, line.getGlyphs()[i], "glyph " + i);
                assertEquals(pen + dx * scale, line.getPositions()[2 * i], 1e-4f, "x " + i);
                assertEquals(-dy * scale, line.getPositions()[2 * i + 1], 1e-4f, "y " + i);
                pen += ax * scale;
            }
            assertEquals(0, line.getStart());
            assertEquals(paragraph.length(), line.getEnd());
        } finally {
            out.delete();
            in.delete();
        }
    }

    // Khmer breaks at the ends of words, as ICU's dictionary has them; Hindi at its spaces
    @Test
    void breaksBetweenWords() throws Exception {
        HBShape hb = hbShape();
        String[][] cases = {{"khm", "km", KHMER}, {"hin", "hi", DEVANAGARI}};
        for (String[] c : cases) {
            byte[] font = bytes(c[2]);
            int breaks = 0;
            for (String paragraph : udhr(c[0])) {
                if (paragraph.isEmpty()) {
                    continue;
                }
                List<ShapedLine> lines = hb.shape(new ShapingRequest(paragraph, c[1], new byte[][] {font}, 12f, 300f));
                int[] cps = paragraph.codePoints().toArray();
                Set<Integer> ends = new HashSet<>(DictionaryBreaks.wordEnds(cps));
                for (int i = 0; i < lines.size(); i++) {
                    ShapedLine line = lines.get(i);
                    if (i > 0) {
                        assertEquals(lines.get(i - 1).getEnd(), line.getStart());
                    }
                    if (i == lines.size() - 1) {
                        assertEquals(paragraph.length(), line.getEnd());
                        continue;
                    }
                    breaks++;
                    int at = paragraph.codePointCount(0, line.getEnd());
                    assertTrue(cps[at - 1] == ' ' || ends.contains(at), c[0] + ": a break inside a word at " + at);
                }
            }
            assertTrue(breaks >= 50, c[0] + ": " + breaks + " breaks");
        }
    }

    // ComplexText draws Hindi with an English word, in two fonts, and Khmer
    @Test
    void drawsComplexText() throws Exception {
        HBShape hb = hbShape();
        ByteArrayOutputStream out = new ByteArrayOutputStream();
        PDF pdf = new PDF(out);
        Font hindi = new Font(pdf, TestSupport.file(DEVANAGARI).getPath());
        Font khmer = new Font(pdf, TestSupport.file(KHMER).getPath());
        Font latin = new Font(pdf, TestSupport.file("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf").getPath());
        Object[][] cases = {{hindi, udhr("hin").get(2) + " PDFjet", "hi"}, {khmer, udhr("khm").get(2), "km"}};
        for (Object[] c : cases) {
            Font font = (Font) c[0];
            font.setSize(12f);
            Page page = new Page(pdf, Letter.PORTRAIT);
            float[] xy = new ComplexText(font, (String) c[1]).setFallbackFont(latin).setLanguage((String) c[2])
                    .setShaper(hb).setWidth(300f).setLocation(50f, 50f).drawOn(page);
            assertTrue(xy[1] > 50 + 3 * 12, c[2] + ": drawn down to " + xy[1]);
        }
        pdf.complete();
        assertTrue(out.size() > 1000);
    }
}
