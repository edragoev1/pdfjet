/*
 * BarcodeTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.barcodes;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.Color;
import com.pdfjet.Direction;
import com.pdfjet.Font;
import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;

class BarcodeTest {
    // type, text, direction, with font, corner x, corner y
    private static final Object[][] CORNERS = {
        {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, false, 171.25f, 141.25f},
        {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, true, 171.25f, 151.31f},
        {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, false, 141.25f, 171.25f},
        {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, false, 141.25f, 171.25f},
        {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, true, 141.25f, 171.25f},
        {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, false, 171.25f, 141.25f},
        {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, true, 179.59f, 151.31f},
        {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, false, 141.25f, 171.25f},
        {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, false, 141.25f, 171.25f},
        {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, true, 141.25f, 179.59f},
        {Barcode.CODE_128, "Hello", Direction.LEFT_TO_RIGHT, false, 167.5f, 137.5f},
        {Barcode.CODE_128, "Hello", Direction.LEFT_TO_RIGHT, true, 167.5f, 154.072f},
        {Barcode.CODE_128, "Hello", Direction.BOTTOM_TO_TOP, false, 137.5f, 167.5f},
        {Barcode.CODE_128, "Hello", Direction.BOTTOM_TO_TOP, true, 154.072f, 167.5f},
        {Barcode.CODE_128, "Hello", Direction.TOP_TO_BOTTOM, false, 137.5f, 167.5f},
        {Barcode.CODE_128, "Hello", Direction.TOP_TO_BOTTOM, true, 137.5f, 167.5f},
        {Barcode.CODE_39, "HELLO-39", Direction.LEFT_TO_RIGHT, false, 219.25f, 137.5f},
        {Barcode.CODE_39, "HELLO-39", Direction.LEFT_TO_RIGHT, true, 219.25f, 154.072f},
        {Barcode.CODE_39, "HELLO-39", Direction.BOTTOM_TO_TOP, false, 137.5f, 219.25f},
        {Barcode.CODE_39, "HELLO-39", Direction.BOTTOM_TO_TOP, true, 154.072f, 219.25f},
        {Barcode.CODE_39, "HELLO-39", Direction.TOP_TO_BOTTOM, false, 137.5f, 219.25f},
        {Barcode.CODE_39, "HELLO-39", Direction.TOP_TO_BOTTOM, true, 137.5f, 219.25f},
        // The bearer bars of ITF-14 are around the bars and their quiet zones
        {Barcode.ITF_14, "1540014128876", Direction.LEFT_TO_RIGHT, false, 211.375f, 143.5f},
        {Barcode.ITF_14, "1540014128876", Direction.LEFT_TO_RIGHT, true, 211.375f, 160.072f},
        {Barcode.ITF_14, "1540014128876", Direction.BOTTOM_TO_TOP, false, 143.5f, 211.375f},
        {Barcode.ITF_14, "1540014128876", Direction.BOTTOM_TO_TOP, true, 160.072f, 211.375f},
        {Barcode.ITF_14, "1540014128876", Direction.TOP_TO_BOTTOM, false, 143.5f, 211.375f},
        {Barcode.ITF_14, "1540014128876", Direction.TOP_TO_BOTTOM, true, 143.5f, 211.375f},
    };

    @Test
    void drawOnReturnsTheCornerOfTheBarsAndTheTextInEveryDirection() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        for (Object[] row : CORNERS) {
            Barcode barcode = new Barcode((Integer) row[0], (String) row[1])
                    .setLocation(100f, 100f).setDirection((Direction) row[2]);
            if ((Boolean) row[3]) {
                barcode.setFont(font);
            }
            String name = row[0] + " " + row[2] + " font " + row[3];
            float[] first = barcode.drawOn(page);
            assertEquals((Float) row[4], first[0], TestSupport.DELTA, name);
            assertEquals((Float) row[5], first[1], TestSupport.DELTA, name);
            float[] second = barcode.drawOn(page);
            assertEquals(first[0], second[0], 0f, name + " drawn again");
            assertEquals(first[1], second[1], 0f, name + " drawn again");
            assertEquals((Float) row[5] - 100f, barcode.getHeight(), TestSupport.DELTA, name + " height");
        }
    }

    @Test
    void code39RejectsCharactersItCannotEncode() throws Exception {
        final Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        Exception e = assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_39, "hello").drawOn(page));
        assertEquals("The input string '*hello*' contains characters that are invalid in a Code39 barcode.", e.getMessage());
    }

    @Test
    void upcAndEanNeedTheirNumberOfDigits() {
        Exception upc = assertThrows(Exception.class, () -> new Barcode(Barcode.UPC_A, "123"));
        assertEquals("UPC-A barcodes must have exactly 11 digits!", upc.getMessage());
        Exception ean = assertThrows(Exception.class, () -> new Barcode(Barcode.EAN_13, "0123456789012"));
        assertEquals("EAN-13 barcodes must have exactly 12 digits!", ean.getMessage());
    }

    @Test
    void code128RefusesATextItCannotHold() throws Exception {
        String tooLong = "Code 128 barcodes hold at most 48 codewords, and a character below 32 or from 128 to 255 takes two!";
        assertEquals(tooLong, assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_128, repeat("A", 49))).getMessage());
        assertEquals(tooLong, assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_128, repeat("\u00e9", 25))).getMessage());
        assertEquals(tooLong, assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_128, repeat("7", 98))).getMessage());
        assertEquals("Code 128 barcodes can only hold characters up to U+00FF!",
                assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_128, "A\u20ac")).getMessage());
        // The most a barcode holds is drawn whole: 48 codewords, and the start,
        // the check digit and the stop, of 11 modules each but the stop of 13
        for (String text : new String[] {repeat("A", 48), repeat("\u00e9", 24), repeat("7", 96)}) {
            assertEquals((11 * 50 + 13) * 0.75f, new Barcode(Barcode.CODE_128, text).drawOn(null)[0], 0f, text);
        }
    }

    // ZXing reads the barcodes of these texts back as they are.
    @Test
    void code128TakesCodeSetCForRunsOfDigits() throws Exception {
        String[] texts = {"0123456789", "42", "123", "12345", "A1234B", "A12345", "A12B", "12\t34\u00fc5678"};
        int[] codewords = {5, 1, 3, 4, 6, 5, 4, 11};     // Of the data, from the Go port's test
        for (int i = 0; i < texts.length; i++) {
            float width = new Barcode(Barcode.CODE_128, texts[i]).drawOn(null)[0];
            assertEquals((11 * (codewords[i] + 2) + 13) * 0.75f, width, 0f, texts[i]);
        }
    }

    // Where the bars the content draws end, in points from the top of a letter
    // page, each once, in the order of their first bar: a bar is a line moved
    // to and drawn from the top of the bars.
    private static List<Float> barEnds(String content) {
        List<Float> ends = new ArrayList<Float>();
        String[] lines = content.split("\n");
        for (int i = 0; i + 1 < lines.length; i++) {
            String[] move = lines[i].trim().split(" ");
            String[] line = lines[i + 1].trim().split(" ");
            if (move.length == 3 && move[2].equals("m") && line.length == 3 && line[2].equals("l")
                    && move[0].equals(line[0])) {
                float end = Letter.PORTRAIT.getHeight() - Float.parseFloat(line[1]);
                if (!ends.contains(end)) {
                    ends.add(end);
                }
            }
        }
        return ends;
    }

    // How many of the bars the content draws end where the given one does.
    private static int barsEndingAt(String content, float end) {
        int count = 0;
        String[] lines = content.split("\n");
        for (int i = 0; i + 1 < lines.length; i++) {
            String[] move = lines[i].trim().split(" ");
            String[] line = lines[i + 1].trim().split(" ");
            if (move.length == 3 && move[2].equals("m") && line.length == 3 && line[2].equals("l")
                    && move[0].equals(line[0]) && Letter.PORTRAIT.getHeight() - Float.parseFloat(line[1]) == end) {
                count++;
            }
        }
        return count;
    }

    @Test
    void theGuardBarsReachFiveModulesBelowTheOthers() throws Exception {
        // At a module of 2 the bars are 100 long, so the guard bars are 110.
        for (int type : new int[] {Barcode.EAN_13, Barcode.UPC_A}) {
            Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
            Barcode barcode = new Barcode(type, type == Barcode.UPC_A ? "01234567890" : "012345678901");
            barcode.setModuleLength(2f);
            barcode.setLocation(0f, 0f);
            barcode.drawOn(page);
            assertEquals(Arrays.asList(110f, 100f), barEnds(TestSupport.content(page)), "type " + type);
        }
    }

    @Test
    void upcATheBarsOfTheFirstAndTheLastDigitAreAsLongAsTheGuardBars() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        Barcode barcode = new Barcode(Barcode.UPC_A, "01234567890");
        barcode.setModuleLength(2f);
        barcode.setLocation(0f, 0f);
        barcode.drawOn(page);
        // The guard bars and the two bars of each of the digits outside them are
        // long: 3 guards of 2 bars and 2 digits of 2 bars.
        assertEquals(10, barsEndingAt(TestSupport.content(page), 110f));
    }

    @Test
    void isBlackWhateverPenColorThePageHas() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setPenColor(Color.blue);
        new Barcode(Barcode.CODE_128, "AB").drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("q\n0 0 0 RG\n") && content.endsWith("Q\n"), content);
    }

    // The GS1-128 barcodes below were read back with ZXing, which gave the
    // symbology identifier ]C1 of GS1-128.
    @Test
    void gs1128TakesCodeSetCForRunsOfDigits() throws Exception {
        // Start C, FNC1, 10 codewords for the 20 digits, the check digit and the stop
        assertEquals((11 * 13 + 13) * 0.75f,
                new Barcode(Barcode.GS1_128, "(00)106141412345678908").drawOn(null)[0], 0f);
        // Start B, FNC1, 1 0 A 1 in code set B, Code C, 23 45, Code B, B,
        // FNC1 after the batch, 2 1 7: 14 codewords, with the start and the check digit 16
        assertEquals((11 * 16 + 13) * 0.75f,
                new Barcode(Barcode.GS1_128, "(10)A12345B(21)7").drawOn(null)[0], 0f);
    }

    @Test
    void gs1128RefusesDataThatIsNotGS1OrTooLong() throws Exception {
        new Barcode(Barcode.GS1_128, "(91)" + repeat("X", 46));    // 48 characters
        String[][] cases = {
            {"(91)" + repeat("X", 47), "GS1-128 barcodes hold at most 48 characters, not counting the separators!"},
            {"(01)09506000134353", "The check digit of (01) is wrong!"},
            {"01095060001343", "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!"},
        };
        for (final String[] c : cases) {
            assertEquals(c[1], assertThrows(Exception.class, () -> new Barcode(Barcode.GS1_128, c[0])).getMessage(), c[0]);
        }
    }

    @Test
    void itf14NeedsThirteenDigits() throws Exception {
        for (final String text : new String[] {"154001412887", "15400141288763", "154001412887A"}) {
            assertEquals("ITF-14 barcodes must have exactly 13 digits!",
                    assertThrows(Exception.class, () -> new Barcode(Barcode.ITF_14, text)).getMessage(), text);
        }
    }

    // ZXing reads the ITF-14 barcodes of these GTINs back, their check digits
    // added, in each direction.
    @Test
    void itf14DrawsTheBarsAndTheBearerBars() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        Barcode barcode = new Barcode(Barcode.ITF_14, "1540014128876");
        barcode.setLocation(100f, 100f);
        barcode.drawOn(page);
        String content = TestSupport.content(page);
        // The 39 bars: 2 of the start, 5 of each of the 7 pairs of digits and 2
        // of the stop, then the 4 bearer bars, 3 thick
        assertEquals(39 + 4, content.split(" l\nS\n", -1).length - 1, content);
        assertTrue(content.contains("3 w\n"), content);
    }

    private static String repeat(String s, int n) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < n; i++) {
            sb.append(s);
        }
        return sb.toString();
    }
}
