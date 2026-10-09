/*
 * RepairScanTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

import java.nio.charset.StandardCharsets;
import java.util.List;
import org.junit.jupiter.api.Test;

// The repair scan of a PDF whose cross-reference table is broken, which reads
// the PDF by looking for its objects.
class RepairScanTest {
    private static final String INNER = "BT /F1 12 Tf 72 720 Td (x) Tj ET\n"
            + "5 0 obj\n<< /Length 3 >>\nstream\nabc\nendstream\nendobj\n"
            + "q Q";

    // A PDF whose page's content is an unfiltered stream with a wrong /Length
    // that holds an object of an embedded PDF, with a stream of its own.
    private static byte[] pdfWithAnObjectInAStream() {
        StringBuilder sb = new StringBuilder();
        sb.append("%PDF-1.7\n");
        sb.append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        sb.append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        sb.append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        sb.append("4 0 obj\n<< /Length 10 >>\nstream\n" + INNER + "\nendstream\nendobj\n");
        sb.append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n999999\n%%EOF\n");
        return sb.toString().getBytes(StandardCharsets.ISO_8859_1);
    }

    @Test
    void aStreamThatHoldsAnObjectIsReadWhole() throws Exception {
        // An object ended at the next "number generation obj": one in the
        // bytes of a stream, as an embedded PDF has, unfiltered, cut the stream
        // short and was read as an object of the PDF, and the first endstream
        // after the stream, that of the embedded object, was taken for the
        // stream's own.
        List<PDFobj> objects = TestSupport.read(pdfWithAnObjectInAStream());
        PDFobj content = null;
        for (PDFobj obj : objects) {
            if (obj == null) {
                continue;
            }
            assertNotEquals(5, obj.number, "the object in the stream is read as an object of the PDF");
            if (obj.number == 4) {
                content = obj;
            }
        }
        assertNotNull(content);
        assertEquals(INNER, new String(content.getData(), StandardCharsets.ISO_8859_1));
    }
}
