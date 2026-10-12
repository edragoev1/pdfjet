/*
 * ReviewCodesTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.barcodes.Barcode;
import com.pdfjet.encryption.Passwords;
import com.pdfjet.encryption.Permissions;
import com.pdfjet.encryption.UserAccess;
import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.EnumSet;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

class ReviewCodesTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    // Returns the bounding box of the structure element, in the coordinates of the PDF.
    private static float[] bbox(StructElement element) {
        Matcher matcher = Pattern.compile("/BBox \\[(\\S+) (\\S+) (\\S+) (\\S+)\\]").matcher(element.attributes);
        assertTrue(matcher.find(), element.attributes);
        float[] box = new float[4];
        for (int i = 0; i < 4; i++) {
            box[i] = Float.parseFloat(matcher.group(i + 1));
        }
        return box;
    }

    // Returns the object with the number and the tokens of the raw text, which
    // are separated by single spaces.
    private static PDFobj object(int number, String raw) {
        PDFobj obj = new PDFobj();
        obj.number = number;
        obj.dict.addAll(Arrays.asList(raw.split(" ")));
        return obj;
    }

    @Test
    void chartFlatDataOfLargeValuesHasARange() throws Exception {
        for (float value : new float[] {2e7f, -3e7f, 1e30f}) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.helvetica(pdf);
            Chart chart = new Chart(font, font).setLocation(50f, 50f).setSize(300f, 200f);
            chart.addSeries("").addPoint(value, value).addPoint(value, value);
            chart.drawOn(page);
            assertFalse(TestSupport.content(page).contains("NaN"), value + ": NaN in the content");
            pdf.complete();
        }
    }

    @Test
    void chartTheRoundedRangeOfFlatDataHasAGridLine() {
        for (float value : new float[] {0f, 5f, 2e7f, -3e7f, 1e30f}) {
            Round round = Chart.roundMaxAndMinValues(value, value);
            assertTrue(round.maxValue > round.minValue && round.numOfGridLines >= 1,
                    value + ": " + round.minValue + " to " + round.maxValue + " with " + round.numOfGridLines);
        }
    }

    // Data with no number, or whose range does not fit a float, has the range
    // from 0 to 1, where Swift stopped the program (review of 12 October 2026,
    // R3).
    @Test
    void chartTheRangeOfDataPastTheLargestFloatIsFromZeroToOne() {
        float inf = Float.POSITIVE_INFINITY;
        float[][] ranges = {{inf, 0f}, {0f, -inf}, {Float.NaN, Float.NaN}, {3.4e38f, -3.4e38f},
                {-Float.MAX_VALUE, Float.MAX_VALUE}, {Float.MAX_VALUE, 0f}};
        for (float[] range : ranges) {
            Round round = Chart.roundMaxAndMinValues(range[0], range[1]);
            assertTrue(round.minValue == 0f && round.maxValue == 1f && round.numOfGridLines == 1,
                    range[0] + ", " + range[1] + ": " + round.minValue + " to " + round.maxValue);
        }
    }

    // Stacked bars whose sums overflow a float are drawn on the range from 0
    // to 1.
    @Test
    void barChartStackedBarsPastTheLargestFloatAreDrawn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        BarChart chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                .setCategories("a").setStacked(true);
        chart.addSeries("", new float[] {3e38f});
        chart.addSeries("", new float[] {3e38f});
        chart.drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("1")), "the axis is not from 0 to 1");
        pdf.complete();
    }

    @Test
    void barChartAValueThatIsNotANumberHasNoBar() throws Exception {
        for (boolean stacked : new boolean[] {false, true}) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.helvetica(pdf);
            BarChart chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                    .setCategories("a", "b", "c").setStacked(stacked).setDrawValueLabels(true);
            chart.addSeries("", new float[] {Float.NaN, 10f, Float.POSITIVE_INFINITY});
            TestSupport.assertXY(350f, 250f, chart.drawOn(page));
            String content = TestSupport.content(page);
            assertFalse(content.contains("NaN") || content.contains(TestSupport.hex("NaN")), "stacked " + stacked);
            assertTrue(content.contains(TestSupport.hex("10")), "stacked " + stacked + ": the axis does not reach 10");
            pdf.complete();
        }
    }

    @Test
    void barcodeTheFigureHasTheBoxOfTheBarsAndTheDigits() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        float height = page.height;
        Object[][] cases = {
            {Barcode.EAN_13, "400638133393", Direction.LEFT_TO_RIGHT},
            {Barcode.UPC_A, "03600029145", Direction.LEFT_TO_RIGHT},
            {Barcode.UPC_A, "03600029145", Direction.TOP_TO_BOTTOM},
            {Barcode.CODE_128, "1111111111111111", Direction.LEFT_TO_RIGHT},
        };
        for (int i = 0; i < cases.length; i++) {
            Barcode barcode = new Barcode((Integer) cases[i][0], (String) cases[i][1]);
            barcode.setDirection((Direction) cases[i][2]);
            barcode.setFont(font);
            barcode.setAltDescription((String) cases[i][1]);
            barcode.setLocation(100f, 100f);
            float[] xy = barcode.drawOn(page);
            float[] box = bbox(page.structures.get(i));
            // The first digit of EAN-13 and UPC-A is left of the bars, and the
            // digits of a barcode drawn top to bottom left of them, the first
            // above them; the digits of Code 128 are wider than its bars.
            assertTrue(box[0] < 100f, "case " + i + ": the box starts at x " + box[0]);
            float top = height - box[3];
            if (cases[i][2] == Direction.TOP_TO_BOTTOM) {
                assertTrue(top < 100f, "case " + i + ": the box has its top at " + top);
            } else {
                assertEquals(100f, top, 0.01f, "case " + i);
            }
            assertEquals(xy[0], box[2], 0.01f, "case " + i);
            assertEquals(height - xy[1], box[1], 0.01f, "case " + i);
        }
    }

    @Test
    void donutChartTheFigureHasTheBoxOfTheLabels() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        new DonutChart(font, font).setLocation(100f, 100f).setRadii(100f, 50f)
                .addSlice(new Slice(25f, Color.red, "Apples and pears"))
                .addSlice(new Slice(75f, Color.blue, "Oranges")).drawOn(page);
        float[] box = bbox(page.structures.get(0));
        // The circle is from 100 to 300; the labels are right and left of it
        assertTrue(box[0] < 100f && box[2] > 300f, Arrays.toString(box));
    }

    @Test
    void decryptorAnAESKeyThatIsTooShortIsRefused() {
        PDFobj encrypt = object(6, "6 0 obj << /Filter /Standard /V 5 /R 4 /Length 40 "
                + "/CF << /StdCF << /CFM /AESV2 >> >> /StmF /StdCF /StrF /StdCF /O <00> /U <00> /P -4 >> endobj");
        PDFobj trailer = object(0, "trailer << /Encrypt 6 0 R >>");
        List<PDFobj> objects = new ArrayList<PDFobj>(Collections.singletonList(encrypt));
        Exception e = assertThrows(Exception.class, () -> Decryptor.getDecryptor(trailer, objects, ""));
        assertEquals("The encryption of the PDF is not valid: /R 4 with a key of 40 bits for AES", e.getMessage());
    }

    @Test
    void decryptorTheContentsOfASignatureAreNotDecrypted() throws Exception {
        String signature = "<0123456789ABCDEF>";
        String[] raws = {
            "7 0 obj << /Type /Sig /Filter /Adobe.PPKLite /Contents " + signature + " >> endobj",
            "7 0 obj << /ByteRange [ 0 10 20 30 ] /Contents " + signature + " >> endobj",
            "7 0 obj << /FT /Sig /V << /Type /Sig /Contents " + signature + " >> /T (Signature1) >> endobj",
        };
        for (String raw : raws) {
            PDFobj obj = object(7, raw);
            int i = obj.dict.indexOf(signature);
            assertTrue(Decryptor.isSignatureDict(obj.dict, i), raw);
        }
        // The /Contents of another dictionary is decrypted
        PDFobj obj = object(7, "7 0 obj << /Type /Annot /Contents (Note) >> endobj");
        assertFalse(Decryptor.isSignatureDict(obj.dict, obj.dict.indexOf("(Note)")));
    }

    @Test
    void encryptionThePermissionsOfTheCallerAreNotChanged() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        Permissions permissions = new Permissions().setAccess(EnumSet.of(UserAccess.PRINT));
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Encrypted title");
        pdf.setEncryption(new Encryption(pdf,
                new Passwords().setUserPassword("").setOwnerPassword("owner"), permissions));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf), "Secret text").setLocation(50f, 50f).drawOn(page);
        pdf.complete();
        assertEquals(EnumSet.of(UserAccess.PRINT), permissions.getAccess());
        // The PDF grants the extraction for accessibility all the same
        Matcher matcher = Pattern.compile("/P (-?\\d+)").matcher(TestSupport.latin1(bos.toByteArray()));
        assertTrue(matcher.find());
        int value = Integer.parseInt(matcher.group(1));
        assertTrue((value & UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.getValue()) != 0);
    }

    // An extension lookup's offset of 32 bits is read unsigned: one of
    // 0x80000000 or more wrapped round to a subtable earlier in the table, which
    // was read; past the table there is no subtable, as in Go (review of
    // 12 October 2026, R27). The table: a mark-to-base subtable at 10, and an
    // extension at 68 whose offset, -58 as 32 bits, wrapped round to it.
    @Test
    void gposAnExtensionOffsetPastTheTableIsNoSubtable() {
        byte[] table = {
            0, 1, 0, 0, 0, 0, 0, 0, 0, 56,  // The header: version 1.0, lookup list at 56
            0, 1, 0, 12, 0, 18, 0, 1, 0, 24, 0, 36,  // Mark to base at 10
            0, 1, 0, 1, 0, 5,  // Mark coverage: glyph 5
            0, 1, 0, 1, 0, 7,  // Base coverage: glyph 7
            0, 1, 0, 0, 0, 6, 0, 1, 0, 10, 0, 20,  // Mark array: class 0, anchor 10, 20
            0, 1, 0, 4, 0, 1, 0, 30, 0, 40,  // Base array: anchor 30, 40
            0, 1, 0, 4,  // Lookup list at 56: one lookup, at 60
            0, 9, 0, 0, 0, 1, 0, 8,  // Extension lookup: one subtable, at 68
            0, 1, 0, 4, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xC6,  // Extension: mark to base, at -58
        };
        GPOS gpos = GPOS.of(table, 0, table.length, 10, false).read();
        assertEquals(0, gpos.markAnchors.size(), "the extension wrapped round to the subtable before it");
    }
}
