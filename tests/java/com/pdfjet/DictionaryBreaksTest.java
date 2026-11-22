/*
 * DictionaryBreaksTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import org.junit.jupiter.api.Test;

class DictionaryBreaksTest {
    // Every run of Thai, Lao, Khmer and Burmese in the UDHR is broken into the
    // words ICU breaks it into (tests/data/dictionarybreaks/expected.tsv): the
    // same dictionaries, ICU's engines ported.
    @Test
    void wordEndsAreICUs() throws Exception {
        List<String> rows = Files.readAllLines(
                TestSupport.file("tests/data/dictionarybreaks/expected.tsv").toPath(), StandardCharsets.UTF_8);
        Map<String, Integer> runs = new HashMap<>();
        Map<String, Integer> differ = new HashMap<>();
        List<String> examples = new ArrayList<>();
        for (String row : rows) {
            if (row.startsWith("#") || row.isEmpty()) {
                continue;
            }
            String[] fields = row.split("\t", -1);
            List<Integer> want = new ArrayList<>();
            for (String end : fields[2].split(",")) {
                if (!end.isEmpty()) {
                    want.add(Integer.parseInt(end));
                }
            }
            List<Integer> got = DictionaryBreaks.wordEnds(fields[1].codePoints().toArray());
            runs.merge(fields[0], 1, Integer::sum);
            if (!got.equals(want)) {
                differ.merge(fields[0], 1, Integer::sum);
                if (examples.size() < 3) {
                    examples.add(fields[0] + " " + fields[1] + ": " + got + ", ICU " + want);
                }
            }
        }
        for (String code : new String[] {"tha", "lao", "khm", "mya"}) {
            assertTrue(runs.getOrDefault(code, 0) >= 300, code);
            assertEquals(0, (int) differ.getOrDefault(code, 0), code + " " + examples);
        }
    }

    // java.awt.font, which breaks Khmer nowhere but at its spaces, breaks it
    // between its words, where ICU's dictionary puts their ends
    @Test
    void awtShaperBreaksKhmerBetweenWords() throws Exception {
        byte[] font = Files.readAllBytes(
                TestSupport.file("resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf").toPath());
        AwtShaper shaper = new AwtShaper();
        int breaks = 0;
        int inside = 0;
        for (String paragraph : Files.readAllLines(
                TestSupport.file("resources/data/udhr/udhr_khm.txt").toPath(), StandardCharsets.UTF_8)) {
            if (paragraph.isEmpty()) {
                continue;
            }
            List<ShapedLine> lines = shaper.shape(new ShapingRequest(paragraph, "km", new byte[][] {font}, 12f, 300f));
            int[] cps = paragraph.codePoints().toArray();
            java.util.Set<Integer> ends = new java.util.HashSet<>(DictionaryBreaks.wordEnds(cps));
            for (int i = 0; i < lines.size() - 1; i++) {
                int at = paragraph.codePointCount(0, lines.get(i).getEnd());
                breaks++;
                if (cps[at - 1] != ' ' && cps[at] != ' ' && !ends.contains(at)) {
                    inside++;
                }
            }
        }
        assertTrue(breaks >= 50, breaks + " breaks");
        assertEquals(0, inside, inside + " of " + breaks + " breaks inside a word");
    }

    // Bengali with a zero width non-joiner, shaped by java.awt.font: its
    // invisible glyph, 0xFFFF, is not a glyph of the font, and failed the PDF
    // ("The font has no glyph number 65535"); it is left out, as it draws nothing
    @Test
    void awtShaperLeavesItsInvisibleGlyphsOut() throws Exception {
        String text = "\u0986\u09AE\u09BF \u0995\u09BE\u09B0\u200C\u09CD\u09AF \u0985\u09A7\u09BF\u0995\u09BE\u09B0";
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, TestSupport.file("resources/fonts/world/NotoSansBengali/NotoSansBengali-Regular.otf").getPath());
        font.setSize(12f);
        List<ShapedLine> lines = new AwtShaper().shape(new ShapingRequest(text, "bn",
                new byte[][] {Files.readAllBytes(TestSupport.file(
                        "resources/fonts/world/NotoSansBengali/NotoSansBengali-Regular.otf").toPath())}, 12f, 300f));
        for (ShapedLine line : lines) {
            for (int glyph : line.getGlyphs()) {
                assertTrue(glyph < 0xFFFE, "glyph " + glyph);
            }
        }
        new ComplexText(font, text).setLanguage("bn").setWidth(300f).setLocation(50f, 50f)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
    }

    // Thai text without zero width spaces breaks between its words: the Thai
    // UDHR, each paragraph 300 points wide in IBM Plex Sans Thai 12, in the
    // lines Chrome breaks it into (tests/data/dictionarybreaks/thai-textblock-300.txt).
    @Test
    void thaiTextBlockBreaksBetweenWordsAsChrome() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, TestSupport.open("resources/fonts/world/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"));
        font.setSize(12f);
        List<String> paragraphs = Files.readAllLines(
                TestSupport.file("resources/data/udhr/udhr_tha.txt").toPath(), StandardCharsets.UTF_8);
        StringBuilder got = new StringBuilder();
        for (String paragraph : paragraphs) {
            if (paragraph.isEmpty()) {
                continue;
            }
            TextBlock block = new TextBlock(font, paragraph);
            block.setWidth(300f);
            for (TextLine line : block.getTextLines()) {
                got.append(line.getText().replaceAll(" +$", "")).append('\n');
            }
            got.append('\n');
        }
        String want = new String(Files.readAllBytes(
                TestSupport.file("tests/data/dictionarybreaks/thai-textblock-300.txt").toPath()), StandardCharsets.UTF_8);
        String[] g = got.toString().trim().split("\n", -1);
        String[] w = want.trim().split("\n", -1);
        for (int i = 0; i < Math.min(g.length, w.length); i++) {
            assertEquals(w[i], g[i], "line " + (i + 1));
        }
        assertEquals(w.length, g.length, "lines");
    }
}
