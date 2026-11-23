/*
 * StampPathTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import org.junit.jupiter.api.Test;

// A stamp's path is written as a page's is: a curve's operator, c, v or y,
// and a newline after it and after the path's operator. Stamp had no
// append(char), so Java widened the char to a float and wrote 99 for c and
// 10 for the newline: every drawPath of a stamp ended "S10", and its curves
// were not curves (the cleanup scan of 23 November 2026).
public class StampPathTest {
    @Test
    void aStampsPathHasItsOperatorsAndNewlines() throws Exception {
        ByteArrayOutputStream out = new ByteArrayOutputStream();
        PDF pdf = new PDF(out);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = new Stamp(pdf).setSize(200f, 200f);
        stamp.drawPath(Arrays.asList(new Point(15f, 15f),
                new Point(25f, 25f, Point.CONTROL_POINT_C),
                new Point(35f, 35f, Point.CONTROL_POINT_C),
                new Point(45f, 45f)), PathOperator.STROKE);
        stamp.drawPath(Arrays.asList(new Point(15f, 15f), new Point(45f, 15f)), PathOperator.FILL);
        stamp.complete();
        stamp.setLocation(50f, 50f);
        stamp.drawOn(page);
        pdf.complete();
        // Without the document's ID, in the trailer and the metadata: 32
        // random hex digits, which can hold "f10"
        String text = new String(out.toByteArray(), StandardCharsets.ISO_8859_1)
                .replaceAll("[0-9a-f]{32}", "");
        assertTrue(text.contains(" c\nS\n"), "the curve's c and the stroke's S, each on its line");
        assertTrue(text.contains(" l\nf\n"), "the fill's f on its line");
        assertFalse(text.contains("S10") || text.contains("f10"), "no 10 for a newline");
    }
}
