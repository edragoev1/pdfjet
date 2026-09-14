/*
 * PDFTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;

/** Writing a document and reading it back. */
class PDFTest {
    private static byte[] document(PageSize... sizes) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        for (PageSize size : sizes) {
            Page page = new Page(pdf, size);
            new TextLine(font, "Page").setLocation(50f, 50f).drawOn(page);
        }
        pdf.complete();
        return bos.toByteArray();
    }

    @Test
    void startsWithTheHeaderAndEndsWithEof() throws Exception {
        String raw = TestSupport.latin1(document(Letter.PORTRAIT));
        assertTrue(raw.startsWith("%PDF-1.7\n%"));
        assertTrue(raw.endsWith("%%EOF\n"));
    }

    @Test
    void theCrossReferenceTablePointsAtEveryObject() throws Exception {
        String raw = TestSupport.latin1(document(Letter.PORTRAIT, A4.PORTRAIT));
        Matcher header = Pattern.compile("xref\n0 (\\d+)\n").matcher(raw);
        assertTrue(header.find());
        int count = Integer.parseInt(header.group(1));
        int entries = header.end();
        for (int number = 1; number < count; number++) {
            String entry = raw.substring(entries + 20 * number, entries + 20 * number + 20);
            if (entry.charAt(17) == 'n') {
                int offset = Integer.parseInt(entry.substring(0, 10));
                assertTrue(raw.startsWith(number + " 0 obj", offset), "object " + number + " at " + offset);
            }
        }
        Matcher startxref = Pattern.compile("startxref\n(\\d+)\n%%EOF\n$").matcher(raw);
        assertTrue(startxref.find());
        assertTrue(raw.startsWith("xref\n", Integer.parseInt(startxref.group(1))));
    }

    @Test
    void documentIdsAreRandomAndDifferent() throws Exception {
        Set<String> ids = new HashSet<String>();
        for (int i = 0; i < 100; i++) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            PDF pdf = new PDF(bos);
            new Page(pdf, A4.PORTRAIT);
            pdf.complete();
            String id = TestSupport.trailerID(bos.toByteArray());
            assertTrue(id.matches("[0-9a-f]{32}"), id);
            ids.add(id);
        }
        assertEquals(100, ids.size());
    }

    @Test
    void theXmpDocumentIdIsTheTrailerId() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        String id = TestSupport.trailerID(bos.toByteArray());
        assertTrue(TestSupport.latin1(bos.toByteArray()).contains("<xapMM:DocumentID>uuid:" + id + "</xapMM:DocumentID>"));
    }

    @Test
    void theInfoDictionaryHasTheTitleInUtf16() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.setTitle("Grüße (x)").setAuthor("Author");
        new TextLine(TestSupport.helvetica(pdf), "x").setLocation(10f, 10f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        PDFobj info = TestSupport.findObject(TestSupport.read(bos.toByteArray()), "/Producer");
        assertNotNull(info);
        assertEquals("Grüße (x)", TestSupport.utf16Hex(info.getValue("/Title")));
        assertEquals("Author", TestSupport.utf16Hex(info.getValue("/Author")));
    }

    @Test
    void pageSizesSurviveReadingBack() throws Exception {
        List<PDFobj> objects = TestSupport.read(document(Letter.PORTRAIT, A4.LANDSCAPE, Letter.PORTRAIT));
        List<PDFobj> pages = new PDF().getPageObjects(objects);
        assertEquals(3, pages.size());
        assertEquals(612f, pages.get(0).getPageSize().getWidth(), 0f);
        assertEquals(842f, pages.get(1).getPageSize().getWidth(), 0f);
        assertEquals(595f, pages.get(1).getPageSize().getHeight(), 0f);
        assertEquals(792f, pages.get(2).getPageSize().getHeight(), 0f);
    }

    @Test
    void readsAPdfWhoseCrossReferenceOffsetIsWrong() throws Exception {
        String raw = TestSupport.latin1(document(Letter.PORTRAIT, Letter.PORTRAIT));
        String damaged = raw.replaceFirst("startxref\n\\d+\n", "startxref\n12\n");
        List<PDFobj> objects = TestSupport.read(damaged.getBytes("ISO-8859-1"));
        assertEquals(2, new PDF().getPageObjects(objects).size());
    }

    @Test
    void crossReferenceOffsetsAreTenDigits() throws Exception {
        assertEquals("0000000017", PDF.xrefOffset(17));
        assertEquals("9999999999", PDF.xrefOffset(9999999999L));
        IOException e = assertThrows(IOException.class, () -> PDF.xrefOffset(10000000000L));
        assertEquals("The PDF is too large for a cross-reference table: an object starts at byte 10000000000.",
                e.getMessage());
    }

    @Test
    void completeClosesTheStream() throws Exception {
        final boolean[] closed = {false};
        ByteArrayOutputStream bos = new ByteArrayOutputStream() {
            @Override
            public void close() throws IOException {
                closed[0] = true;
                super.close();
            }
        };
        PDF pdf = new PDF(bos);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        assertTrue(closed[0]);
    }

    @Test
    void readsAPdfWithABlankPage() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        assertEquals(1, new PDF().getPageObjects(TestSupport.read(bos.toByteArray())).size());
    }

    // A PDF with one stream whose data starts with a line feed, the byte that
    // ends the stream keyword. Every stream of an encrypted PDF starts with a
    // random IV, so one in 256 of them starts that way.
    private static byte[] pdfWithStream(String data) {
        String o1 = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
        String o2 = "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n";
        String o3 = "3 0 obj\n<< /Length " + data.length() + " >>\nstream\n" + data + "\nendstream\nendobj\n";
        String header = "%PDF-1.4\n";
        int off1 = header.length();
        int off2 = off1 + o1.length();
        int off3 = off2 + o2.length();
        int xref = off3 + o3.length();
        String body = header + o1 + o2 + o3
                + "xref\n0 4\n0000000000 65535 f \n"
                + String.format("%010d 00000 n \n%010d 00000 n \n%010d 00000 n \n", off1, off2, off3)
                + "trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n";
        return body.getBytes(StandardCharsets.ISO_8859_1);
    }

    @Test
    void aStreamThatStartsWithALineFeedKeepsIt() throws Exception {
        List<PDFobj> objects = TestSupport.read(pdfWithStream("\nHELLO"));
        for (PDFobj obj : objects) {
            if (obj.getNumber() == 3) {
                assertEquals("\nHELLO", new String(obj.getData(), StandardCharsets.ISO_8859_1));
                return;
            }
        }
        throw new AssertionError("object 3 not read");
    }
}
