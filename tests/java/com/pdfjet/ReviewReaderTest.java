/*
 * ReviewReaderTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.function.Executable;

/**
 * Reading PDFs that are made to be slow to read, or to break what is made of
 * them. The time limits are generous: each of these took from five seconds to
 * more than a minute, or ran out of stack, and takes a time in proportion to
 * its size now.
 */
class ReviewReaderTest {
    private static final String EARLIER = "The PDF was not completed because of an earlier error: ";

    // Reads the PDF, which must take less than five seconds, and returns its
    // objects, or null when it cannot be read.
    private static List<PDFobj> readQuickly(byte[] pdf) {
        long start = System.nanoTime();
        List<PDFobj> objects;
        try {
            objects = TestSupport.read(pdf);
        } catch (Exception e) {
            objects = null;
        }
        long elapsed = (System.nanoTime() - start) / 1000000L;
        assertTrue(elapsed < 5000L, "reading " + pdf.length + " bytes took " + elapsed + " ms");
        return objects;
    }

    private static String repeat(String text, int count) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++) {
            sb.append(text);
        }
        return sb.toString();
    }

    private static byte[] latin1(String text) {
        return text.getBytes(StandardCharsets.ISO_8859_1);
    }

    private static byte[] pdfWithObjects(List<String> objects) {
        StringBuilder sb = new StringBuilder("%PDF-1.4\n");
        int[] offsets = new int[objects.size()];
        for (int i = 0; i < objects.size(); i++) {
            offsets[i] = sb.length();
            sb.append(i + 1).append(" 0 obj\n").append(objects.get(i)).append("\nendobj\n");
        }
        int xref = sb.length();
        sb.append("xref\n0 ").append(objects.size() + 1).append("\n0000000000 65535 f \n");
        for (int offset : offsets) {
            sb.append(String.format("%010d 00000 n \n", offset));
        }
        sb.append("trailer\n<< /Size ").append(objects.size() + 1)
                .append(" /Root 1 0 R >>\nstartxref\n").append(xref).append("\n%%EOF\n");
        return latin1(sb.toString());
    }

    private static byte[] pdfWithObjects(String... objects) {
        return pdfWithObjects(Arrays.asList(objects));
    }

    private static String readError(String raw) {
        try {
            TestSupport.read(latin1(raw));
            return "(no error)";
        } catch (Exception e) {
            return e.getMessage();
        }
    }

    private static void assertRefused(final PDF pdf, String message) {
        IllegalStateException e = assertThrows(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.complete(); }
        });
        assertEquals(EARLIER + message, e.getMessage());
    }

    // Returns the objects of a small PDF of one page, as read() returns them.
    private static List<PDFobj> existingObjects() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        new TextLine(font, "Existing").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        return TestSupport.read(bos.toByteArray());
    }

    // Returns a PDF with no cross-reference table, of a catalog, a page tree
    // of one page and then count objects "<< /A i >>", with or without their
    // endobj.
    private static byte[] numberedObjects(int count, boolean endobj) {
        List<String> objects = new ArrayList<String>(Arrays.asList(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"));
        for (int i = 0; i < count; i++) {
            objects.add("<< /A " + i + " >>");
        }
        StringBuilder sb = new StringBuilder("%PDF-1.7\n");
        for (int i = 0; i < objects.size(); i++) {
            sb.append(i + 1).append(" 0 obj\n").append(objects.get(i)).append("\n");
            if (endobj) {
                sb.append("endobj\n");
            }
        }
        sb.append("trailer\n<< /Size ").append(objects.size() + 1).append(" /Root 1 0 R >>\n");
        return latin1(sb.toString());
    }

    @Test
    void aRunOfWhiteSpaceIsScannedOnce() {
        // Every space looked at all the spaces after it for a number.
        readQuickly(latin1("%PDF-1.7\n" + repeat(" ", 400000)));
        readQuickly(latin1("%PDF-1.7\n1" + repeat("\n", 400000)));
    }

    @Test
    void objectsWithNoEndobjAreReadOnce() {
        // Every object was read to the end of the PDF.
        readQuickly(latin1(repeat("1 0 obj\n", 31000)));

        // Each object ends where the next one starts.
        List<PDFobj> objects = readQuickly(numberedObjects(20000, false));
        assertNotNull(objects);
        assertEquals(20003, objects.size());
        assertEquals("7", objects.get(10).getValue("/A"));
        assertEquals("[ 3 0 R ]", objects.get(1).getValue("/Kids"));
        assertEquals(1, new PDF().getPageObjects(objects).size());
    }

    @Test
    void objectsWithNoEndobjThatTheTableListsAreReadOnce() {
        // Every object that the cross-reference table lists was read to the
        // end of the PDF.
        List<String> objects = new ArrayList<String>(Arrays.asList(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"));
        for (int i = 0; i < 20000; i++) {
            objects.add("<< /A " + i + " >>");
        }
        StringBuilder sb = new StringBuilder("%PDF-1.7\n");
        int[] offsets = new int[objects.size()];
        for (int i = 0; i < objects.size(); i++) {
            offsets[i] = sb.length();
            sb.append(i + 1).append(" 0 obj\n").append(objects.get(i)).append("\n");
        }
        int xref = sb.length();
        sb.append("xref\n0 ").append(objects.size() + 1).append("\n0000000000 65535 f \n");
        for (int offset : offsets) {
            sb.append(String.format("%010d 00000 n \n", offset));
        }
        sb.append("trailer\n<< /Size ").append(objects.size() + 1)
                .append(" /Root 1 0 R >>\nstartxref\n").append(xref).append("\n%%EOF\n");
        List<PDFobj> read = readQuickly(latin1(sb.toString()));
        assertNotNull(read);
        assertEquals(20003, read.size());
        assertEquals("7", read.get(10).getValue("/A"));
        // The object is "11 0 obj << /A 7 >>", without the objects after it.
        assertEquals("11 0 obj << /A 7 >>", String.join(" ", read.get(10).dict));
    }

    @Test
    void aCrossReferenceSectionThatIsItsOwnPrevIsReadOnce() {
        // The section of a big table was read a thousand times: its /Prev is
        // itself. The PDF is then read by looking for its objects.
        String pdf = new String(pdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"), StandardCharsets.ISO_8859_1);
        int xref = pdf.indexOf("xref");
        StringBuilder sb = new StringBuilder(pdf.substring(0, xref));
        sb.append("xref\n0 4\n0000000000 65535 f \n");
        for (int i = 1; i <= 3; i++) {
            sb.append(String.format("%010d 00000 n \n", pdf.indexOf(i + " 0 obj")));
        }
        // Free entries, which make the table big.
        sb.append(repeat("0000000000 65535 f \n", 100000));
        sb.append("trailer\n<< /Size 4 /Root 1 0 R /Prev ").append(xref)
                .append(" >>\nstartxref\n").append(xref).append("\n%%EOF\n");
        List<PDFobj> objects = readQuickly(latin1(sb.toString()));
        assertNotNull(objects);
        assertEquals(1, new PDF().getPageObjects(objects).size());
    }

    @Test
    void theLengthOfAStreamIsFoundByItsNumber() throws Exception {
        // Every stream looked for its /Length among all the objects.
        List<String> objects = new ArrayList<String>(Arrays.asList(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"));
        for (int i = 0; i < 60000; i++) {
            objects.add("<< /Length " + (objects.size() + 2) + " 0 R >>\nstream\nq Q\nendstream");
            objects.add("3");
        }
        List<PDFobj> read = readQuickly(pdfWithObjects(objects));
        assertNotNull(read);
        assertEquals(120003, read.size());
        assertEquals("q Q", new String(read.get(119999).getData(), StandardCharsets.ISO_8859_1));
    }

    @Test
    void theLengthOfAStreamIsItsNewestVersion() throws Exception {
        // The /Length of the stream is updated from 2 to 15 at the end of the
        // PDF, and the stream holds the keyword that the first length ends at.
        String pdf = new String(pdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>",
                "<< /Length 5 0 R >>\nstream\nAB endstream CD\nendstream",
                "2"), StandardCharsets.ISO_8859_1);
        int prev = pdf.indexOf("xref");
        StringBuilder sb = new StringBuilder(pdf);
        int offset = sb.length();
        sb.append("5 0 obj\n15\nendobj\n");
        int xref = sb.length();
        sb.append(String.format("xref\n0 1\n0000000000 65535 f \n5 1\n%010d 00000 n \n", offset));
        sb.append("trailer\n<< /Size 6 /Root 1 0 R /Prev ").append(prev)
                .append(" >>\nstartxref\n").append(xref).append("\n%%EOF\n");
        List<PDFobj> objects = TestSupport.read(latin1(sb.toString()));
        assertEquals("AB endstream CD", new String(objects.get(3).getData(), StandardCharsets.ISO_8859_1));
    }

    // Returns a PDF whose page uses a form XObject that refers to the next
    // object, which refers to the next, count times, and whose page tree has
    // count nodes, one under the other.
    private static byte[] chainOfObjects(int count) {
        List<String> objects = new ArrayList<String>();
        objects.add("");    // The catalog, below
        objects.add("<< /Type /Page /MediaBox [0 0 612 792] /Resources << /XObject << /X0 3 0 R >> >> >>");
        for (int i = 0; i < count; i++) {   // Objects 3 and on
            objects.add("<< /Next " + (objects.size() + 2) + " 0 R >>");
        }
        objects.add("<< >>");
        int first = objects.size() + 1;
        for (int i = 0; i < count; i++) {
            int kid = (i == count - 1) ? 2 : objects.size() + 2;   // The last is the page.
            objects.add("<< /Type /Pages /Kids [" + kid + " 0 R] /Count 1 >>");
        }
        objects.set(0, "<< /Type /Catalog /Pages " + first + " 0 R >>");
        return pdfWithObjects(objects);
    }

    @Test
    void longChainsOfObjectsAreFollowedWithoutRecursion() throws Exception {
        // They ran out of stack.
        int count = 100000;
        List<PDFobj> objects = TestSupport.read(chainOfObjects(count));
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        assertEquals(1, pdf.getPageObjects(objects).size());
        pdf.addResourceObjects(objects);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        List<PDFobj> written = TestSupport.read(bos.toByteArray());
        assertEquals(Arrays.asList("<<", ">>"), PDF.valueOf(written.get(count + 2)));
    }

    @Test
    void addObjectsIsRefusedWhereItWouldLosePages() throws Exception {
        final List<PDFobj> objects = existingObjects();
        final PDF pdf = TestSupport.newPDF();
        new Page(pdf, Letter.PORTRAIT);
        assertEquals("The objects of an existing PDF cannot be added to a PDF that has pages of its own.",
                assertThrows(IllegalStateException.class, new Executable() {
                    public void execute() throws Throwable { pdf.addObjects(objects); }
                }).getMessage());

        // A page after the objects is not in their page tree.
        final PDF pdf2 = TestSupport.newPDF();
        pdf2.addObjects(existingObjects());
        String message = "A page cannot be added to a PDF that addObjects added the objects of an existing PDF to.";
        assertEquals(message, assertThrows(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { new Page(pdf2, Letter.PORTRAIT); }
        }).getMessage());
        assertRefused(pdf2, message);

        // The pages were not made for the compliance of the document.
        final PDF pdf3 = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        assertEquals("The objects of an existing PDF cannot be added to a PDF/UA or PDF/A document.",
                assertThrows(IllegalStateException.class, new Executable() {
                    public void execute() throws Throwable { pdf3.addObjects(objects); }
                }).getMessage());
    }

    @Test
    void aNumberWithNoObjectIsAFreeEntry() throws Exception {
        // Object 4 is not in the PDF, and was written as "4 0 obj endobj".
        List<PDFobj> objects = TestSupport.read(pdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
                "<< /Removed true >>",
                "<< /Kept true >>"));
        PDFobj empty = new PDFobj();    // As read() gives a number with no object
        empty.setNumber(4);
        objects.set(3, empty);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.addObjects(objects);
        pdf.complete();
        String raw = new String(bos.toByteArray(), StandardCharsets.ISO_8859_1);
        assertFalse(raw.contains("\n4 0 obj"), "an empty object is written");
        String[] entries = raw.substring(raw.lastIndexOf("\nxref\n")).split("\n");
        assertEquals("0000000000 65535 f ", entries[7]);    // Object 4
        assertEquals("true", TestSupport.read(bos.toByteArray()).get(4).getValue("/Kept"));
    }

    @Test
    void twoPagesThatNameDifferentResourcesAlikeAreRefused() throws Exception {
        final List<PDFobj> objects = TestSupport.read(pdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 6 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream",
                "<< /Subtype /Form /BBox [0 0 20 20] /Length 0 >>\nstream\n\nendstream"));
        final PDF pdf = TestSupport.newPDF();
        String message = "The pages of the PDF use the name /X0 for different resources, "
                + "and the pages of this document share one resources dictionary.";
        assertEquals(message, assertThrows(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { pdf.addResourceObjects(objects); }
        }).getMessage());
        assertRefused(pdf, message);

        // Pages that use the same resource under the same name share it.
        List<PDFobj> shared = TestSupport.read(pdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream"));
        PDF pdf2 = TestSupport.newPDF();
        pdf2.addResourceObjects(shared);
        new Page(pdf2, Letter.PORTRAIT);
        pdf2.complete();
    }

    @Test
    void theTypeOfAnObjectIsAnEntryOfItsOwn() throws Exception {
        // The /Type of the /Group was taken for the type of the page, which a
        // form refers to, and the page was copied with the form.
        List<PDFobj> objects = TestSupport.read(pdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Group << /Type /Group /S /Transparency >> /Type /Page /Parent 2 0 R"
                        + " /Resources << /XObject << /X0 4 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Page 3 0 R /Length 0 >>\nstream\n\nendstream"));
        assertEquals("/Page", objects.get(2).getValue("/Type"));
        assertEquals("", objects.get(2).getValue("/S"));
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.addResourceObjects(objects);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        assertFalse(new String(bos.toByteArray(), StandardCharsets.ISO_8859_1).contains("/Transparency"),
                "the page is copied with the form");
    }

    @Test
    void aNumberThatIsNotAnObjectNumberIsSkipped() throws Exception {
        List<PDFobj> objects = TestSupport.read(pdfWithObjects(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /XObject <<"
                        + " /Im1 abc 0 R /Im2 99999999999 0 R /Im3 4 0 R >> >> >>",
                "<< /Subtype /Form /BBox [0 0 10 10] /Ref 2147483648 0 R /Length 0 >>\nstream\n\nendstream"));
        PDF pdf = TestSupport.newPDF();
        pdf.addResourceObjects(objects);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
    }

    @Test
    void theNumbersOfAnObjectStreamAreDigits() {
        assertEquals("The object stream of the PDF is malformed: \"+5\" is not a number.",
                readError("1 0 obj<</Type/ObjStm/N 1/First 5/Length 9>>stream\n+5 0 <<>>\nendstream endobj"));
        assertEquals("The object stream of the PDF is malformed: \"2147483648\" is not a number.",
                readError("1 0 obj<</Type/ObjStm/N 1/First 2147483648/Length 9>>stream\n5 0 <<>>\nendstream endobj"));
        // An offset past the end of the stream, which overflowed an int.
        assertEquals("(no error)",
                readError("1 0 obj<</Type/ObjStm/N 2/First 2147483000/Length 13>>stream\n5 1000 6 1001\nendstream endobj"));
    }
}
