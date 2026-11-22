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
