/*
 * BigTableWrapTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Iterator;
import java.util.List;
import java.util.Random;
import java.util.function.Consumer;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

// The tests of setWrapText, as the Go port's bigtable_wrap_test.go has them.
class BigTableWrapTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static final String[] HEADER = {"Name", "City", "Total"};

    // A hundred rows, with a delimiter inside a field, and a short row near
    // the end, which is skipped.
    private static List<String[]> rows() {
        List<String[]> rows = new ArrayList<String[]>();
        for (int i = 0; i < 100; i++) {
            if (i == 90) {
                rows.add(new String[] {"short"});
            }
            rows.add(new String[] {"n" + i, "City, " + i, i + ".5"});
        }
        return rows;
    }

    // Draws the rows in a table of the header's columns, with wrapping on or
    // off, and returns its pages.
    private static List<Page> wrapTable(PDF pdf, Font font, boolean wrap, String[] header, List<String[]> rows,
            Consumer<BigTable> set) throws Exception {
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(header.length);
        table.setTableData(header, rows);
        table.setWrapText(wrap);
        if (set != null) {
            set.accept(table);
        }
        table.setLocation(20f, 20f);
        table.complete();
        return table.getPages();
    }

    // A text of n words of the random lengths.
    private static String words(Random r, int n) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < n; i++) {
            if (i > 0) {
                sb.append(' ');
            }
            char c = (char) ('a' + r.nextInt(26));
            for (int k = 1 + r.nextInt(9); k > 0; k--) {
                sb.append(c);
            }
        }
        return sb.toString();
    }

    private static String repeat(String text, int n) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < n; i++) {
            sb.append(text);
        }
        return sb.toString();
    }

    // The strings the page draws, in the order they are drawn.
    private static List<String> drawnText(Page page) {
        List<String> list = new ArrayList<String>();
        java.util.regex.Matcher m = java.util.regex.Pattern.compile("\\[<([0-9A-Fa-f]*)>\\] TJ")
                .matcher(TestSupport.content(page));
        while (m.find()) {
            // A core font draws a character as the one byte of its code.
            StringBuilder text = new StringBuilder();
            for (int i = 0; i + 1 < m.group(1).length(); i += 2) {
                text.append((char) Integer.parseInt(m.group(1).substring(i, i + 2), 16));
            }
            list.add(text.toString());
        }
        return list;
    }

    private static Object field(BigTable table, String name) throws Exception {
        java.lang.reflect.Field field = BigTable.class.getDeclaredField(name);
        field.setAccessible(true);
        return field.get(table);
    }

    // A table whose columns fit its page draws the same with wrapping on as
    // off, byte for byte: nothing wraps, and the rows and pages are where they
    // were. A page releases its content when it is written, so the last page
    // is compared, with the count of the pages.
    @Test
    void wrapOnATableThatFitsDrawsTheSameAsOff() throws Exception {
        PDF pdf1 = TestSupport.newPDF();
        List<Page> off = wrapTable(pdf1, TestSupport.helvetica(pdf1), false, HEADER, rows(), null);
        PDF pdf2 = TestSupport.newPDF();
        List<Page> on = wrapTable(pdf2, TestSupport.helvetica(pdf2), true, HEADER, rows(), null);
        assertEquals(off.size(), on.size());
        assertEquals(TestSupport.content(off.get(off.size() - 1)), TestSupport.content(on.get(on.size() - 1)));
    }

    // A field too long for its column wraps onto more lines, its words whole
    // and in their order, no line wider than the column, and nothing cut.
    @Test
    void aLongFieldWrapsWholeWithinItsColumn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String text = "The quick brown fox jumps over the lazy dog, again and again, until the page "
                + "is far too narrow for the whole of the sentence on one line of the table.";
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"1", text, "2.5"});
        List<Page> pages = wrapTable(pdf, font, true, new String[] {"No", "Description", "Total"}, rows, null);
        List<String> drawn = drawnText(pages.get(0));
        // The header, then the row's three fields, the description on lines
        assertTrue(drawn.size() >= 6, drawn.toString());
        List<String> lines = drawn.subList(4, drawn.size() - 2);    // Before the total and the footer
        assertTrue(lines.size() > 1, lines.toString());
        assertEquals(text, String.join(" ", lines));
        float room = Letter.PORTRAIT.getWidth() - 40f;
        for (String line : lines) {
            assertFalse(line.endsWith(" ..."), "a line was cut: " + line);
            assertTrue(font.stringWidth(line) <= room, "a line is wider than the table: " + line);
        }
    }

    // A word longer than its column is broken between its characters, the
    // pieces in their order.
    @Test
    void aWordLongerThanItsColumnIsBroken() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String word = repeat("abcdefghij", 12);
        String[] lines = BigTable.wrap(word, font, 60f);
        assertTrue(lines.length >= 2 && String.join("", lines).equals(word), Arrays.toString(lines));
        for (String line : lines) {
            assertTrue(font.stringWidth(line) <= 60f, "a piece is wider than the column: " + line);
        }
        // A column too narrow for one character still draws one on each line
        assertArrayEquals(new String[] {"a", "b", "c"}, BigTable.wrap("abc", font, 0.5f));
        // Spaces are where lines break, not text: an empty field is one line
        assertArrayEquals(new String[] {""}, BigTable.wrap("  ", font, 60f));
        // A character is a code point: one outside the BMP is never split
        String astral = repeat("😀", 3);
        String[] pieces = BigTable.wrap(astral, font, 0.5f);
        assertEquals(3, pieces.length, Arrays.toString(pieces));
        assertEquals(astral, String.join("", pieces));
    }

    // The narrow columns keep their widths, and the wide ones share the rest
    // of the page equally.
    @Test
    void theNarrowColumnsKeepTheirWidths() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String text = repeat("word ", 60);
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"1", text, text, "12.50"});
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(4);
        table.setTableData(new String[] {"No", "A", "B", "Total"}, rows);
        table.setWrapText(true);
        table.setLocation(20f, 20f);
        float[] natural = ((float[]) field(table, "widths")).clone();
        float[] textWidths = (float[]) field(table, "textWidths");
        assertEquals(natural[0], textWidths[0], 0f, "a narrow column lost its width");
        assertEquals(natural[3], textWidths[3], 0f, "a narrow column lost its width");
        assertEquals(textWidths[1], textWidths[2], 0f, "the wide columns do not share the rest equally");
        assertTrue(textWidths[1] < natural[1], Arrays.toString(textWidths));
        float right = ((float[]) field(table, "vertLines"))[4];
        assertTrue(right <= Letter.PORTRAIT.getWidth() - 20f + 0.01f, "the table is wider than its page: " + right);
    }

    // The footer of every page says how many pages there are, however the
    // rows wrap: the pages are counted as they are drawn.
    @Test
    void thePagesAreCountedAsTheyAreDrawn() throws Exception {
        for (long seed = 1; seed <= 20; seed++) {
            final Random r = new Random(seed);
            List<String[]> rows = new ArrayList<String[]>();
            int n = 40 + r.nextInt(120);
            for (int i = 0; i < n; i++) {
                rows.add(new String[] {String.valueOf(i), words(r, r.nextInt(60)), words(r, r.nextInt(12))});
            }
            PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            final float padding = r.nextInt(4);
            List<Page> pages = wrapTable(pdf, font, true, new String[] {"No", "Notes", "Name"}, rows,
                    table -> table.setVerticalPadding(padding));
            String last = "Page " + pages.size() + " of " + pages.size();
            assertTrue(TestSupport.content(pages.get(pages.size() - 1)).contains(TestSupport.hex(last)),
                    "seed " + seed + ": " + pages.size() + " pages, and the last does not say " + last);
        }
    }

    // A row that does not fit what is left of a page starts the next page
    // whole: it is the first row of the last page.
    @Test
    void aRowThatDoesNotFitStartsTheNextPage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<String[]> rows = new ArrayList<String[]>();
        for (int i = 0; i < 50; i++) {
            rows.add(new String[] {String.valueOf(i), "short"});
        }
        String tall = repeat("several words of a long note ", 40);
        rows.add(new String[] {"tall", tall});
        List<Page> pages = wrapTable(pdf, font, true, new String[] {"No", "Notes"}, rows, null);
        assertTrue(pages.size() >= 2, pages.size() + " pages");
        List<String> drawn = drawnText(pages.get(pages.size() - 1));
        assertEquals("tall", drawn.get(2), "the tall row is not the first row of the last page: " + drawn);
        assertTrue(drawn.get(3).startsWith("several words"), drawn.toString());
    }

    // A row taller than a whole page is cut at the lines that fit it, its last
    // line ending in the mark, and the cell of a PDF/UA document keeps the
    // whole of its text.
    @Test
    void aRowTallerThanAPageIsCutAndItsCellKeepsTheText() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        Font font = new Font(pdf, CoreFont.HELVETICA).setSize(12f);
        String text = repeat("a note that runs on and on ", 400).trim();
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"1", text});
        List<Page> pages = wrapTable(pdf, font, true, new String[] {"No", "Notes"}, rows, null);
        assertEquals(1, pages.size());
        List<String> drawn = drawnText(pages.get(0));
        assertTrue(drawn.get(drawn.size() - 2).endsWith(" ..."),
                "the last line drawn does not end in the mark: " + drawn.get(drawn.size() - 2));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        StringBuilder alt = new StringBuilder();
        for (byte b : ("﻿" + text).getBytes(StandardCharsets.UTF_16BE)) {
            alt.append(String.format("%02x", b & 0xFF));
        }
        assertTrue(raw.contains("/Alt <" + alt + ">"), "the cell does not keep the whole of the text");
    }

    // A header too long for its column wraps too, and the rows start under it.
    @Test
    void theHeaderWrapsToo() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String[] header = {"No", "A heading of many words for a narrow column of short values"};
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"1", "x"});
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2);
        table.setTableData(header, rows).setWrapText(true).setColumnWidthsInPercent(80f, 20f);
        table.setLocation(20f, 20f);
        assertTrue((Integer) field(table, "headerCount") >= 2, "the header did not wrap");
        table.complete();
        List<String> drawn = drawnText(table.getPages().get(0));
        assertEquals("x", drawn.get(drawn.size() - 2), drawn.toString());
    }

    // Wrapping reads the rows once more, to count the pages: three times in
    // all, and none of them kept.
    @Test
    void readsTheRowsThreeTimes() throws Exception {
        final List<String[]> rows = rows();
        final int[] opened = {0};
        Iterable<String[]> source = () -> {
            opened[0]++;
            return new Iterator<String[]>() {
                private int index;

                @Override
                public boolean hasNext() {
                    return index < rows.size();
                }

                @Override
                public String[] next() {
                    return rows.get(index++);
                }
            };
        };
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3)
                .setTableData(HEADER, source).setWrapText(true);
        table.setLocation(10f, 10f);
        table.complete();
        assertEquals(3, opened[0]);
    }

    // Wrapping turned off again draws the table as if it was never on.
    @Test
    void wrapTurnedOffAgainIsAsNeverOn() throws Exception {
        String text = repeat("word ", 80);
        List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"1", text});
        rows.add(new String[] {"2", "short"});
        PDF pdf1 = TestSupport.newPDF();
        String never = TestSupport.content(
                wrapTable(pdf1, TestSupport.helvetica(pdf1), false, new String[] {"No", "Notes"}, rows, null).get(0));
        PDF pdf2 = TestSupport.newPDF();
        String again = TestSupport.content(wrapTable(pdf2, TestSupport.helvetica(pdf2), false,
                new String[] {"No", "Notes"}, rows, table -> table.setWrapText(true).setWrapText(false)).get(0));
        assertEquals(never, again);
    }
}
