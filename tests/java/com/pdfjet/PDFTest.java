/*
 * PDFTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertFalse;
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

    @Test
    void anEmptyDocumentPropertyIsNotWritten() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.setTitle("").setAuthor("").setSubject("").setKeywords("").setCreator("");
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        for (String key : new String[] {"/Title", "/Author", "/Subject", "/Keywords", "/Creator"}) {
            assertFalse(raw.contains(key), key);
        }
    }

    @Test
    void aShapeWithoutADescriptionWritesNoAltText() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(10f, 10f, 100f, 10f).drawOn(page);
        new Line(10f, 20f, 100f, 20f).setAltDescription("A rule").drawOn(page);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, raw.split("/Alt <").length - 1);
        assertFalse(raw.contains("/ActualText"));
    }

    @Test
    void pointsAndTwoDimensionalBarcodesAreArtifacts() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Drawable[] drawables = {
                new Point(50f, 50f),
                new com.pdfjet.qrcode.QRCode("https://pdfjet.com", com.pdfjet.qrcode.ErrorCorrectionLevel.M),
                new com.pdfjet.datamatrix.DataMatrix("PDFjet")};
        for (Drawable drawable : drawables) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            drawable.drawOn(page);
            String content = TestSupport.content(page);
            assertTrue(content.startsWith("/Artifact BMC\n"), content);
            assertTrue(content.endsWith("EMC\n"), content);
        }
    }

    @Test
    void aLinkIsInALinkElementAndAnyOtherAnnotationInAnAnnotElement() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "PDFjet").setURIAction("https://pdfjet.com").setLocation(70f, 80f).drawOn(page);
        TextAnnotation note = new TextAnnotation();
        note.setLocation(70f, 100f);
        note.setContents("A note");
        note.drawOn(page);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, raw.split("/S /Link\n").length - 1, raw);
        assertEquals(1, raw.split("/S /Annot\n").length - 1, raw);
    }

    // The numbers of the page objects, in page order.
    private static String[] pageNumbers(byte[] pdf) throws Exception {
        List<PDFobj> pages = new PDF().getPageObjects(TestSupport.read(pdf));
        String[] numbers = new String[pages.size()];
        for (int i = 0; i < numbers.length; i++) {
            numbers[i] = String.valueOf(pages.get(i).getNumber());
        }
        return numbers;
    }

    @Test
    void aDetachedPageThatIsNeverAddedLeavesNoTrace() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        // A dry run, like one that measures the text, on a page that is never added.
        Page dry = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new TextLine(font, "PDFjet").setURIAction("https://pdfjet.com").setLocation(70f, 80f).drawOn(dry);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go to page 2").setGoToAction("dest2").setLocation(70f, 80f).drawOn(page1);
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        page2.addDestination("dest2", 100f);
        pdf.complete();

        String raw = TestSupport.latin1(bos.toByteArray());
        assertFalse(raw.contains("/Pg 0 0 R"));
        assertEquals(1, raw.split("/Type /Annot\n").length - 1);
        // The link leads to the second page, not to the object before it.
        Matcher dest = Pattern.compile("/Dest \\[(\\d+) 0 R").matcher(raw);
        assertTrue(dest.find());
        assertEquals(pageNumbers(bos.toByteArray())[1], dest.group(1));
    }

    @Test
    void theStructureTreeFollowsThePagesNotTheOrderTheyWereDrawnIn() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page second = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new TextLine(font, "Second").setLocation(70f, 80f).drawOn(second);
        Page first = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new TextLine(font, "First").setLocation(70f, 80f).drawOn(first);
        pdf.addPage(first);
        pdf.addPage(second);
        pdf.complete();

        // The structure elements are written, and listed by the document
        // element, in the order of their pages.
        String[] pages = pageNumbers(bos.toByteArray());
        Matcher pg = Pattern.compile("/Pg (\\d+) 0 R").matcher(TestSupport.latin1(bos.toByteArray()));
        assertTrue(pg.find());
        assertEquals(pages[0], pg.group(1));
        assertTrue(pg.find());
        assertEquals(pages[1], pg.group(1));
    }

    @Test
    void textStringsAreUtf16SoThatEveryReaderDecodesThem() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(10f, 20f, 100f, 20f).setAltDescription("Gr\u00fc\u00dfe \u2013 \u7dda").drawOn(page);
        pdf.complete();
        PDFobj element = TestSupport.findObject(TestSupport.read(bos.toByteArray()), "/Alt");
        assertNotNull(element);
        assertTrue(element.getValue("/Alt").toLowerCase().startsWith("<feff"), element.getValue("/Alt"));
        assertEquals("Gr\u00fc\u00dfe \u2013 \u7dda", TestSupport.utf16Hex(element.getValue("/Alt")));
    }

    // A PDF whose font has its widths and its encoding in objects of their own,
    // as the PDFs that Word makes do.
    private static byte[] pdfWithIndirectWidths() {
        String[] objects = {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792]"
                    + " /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            "<< /Length 32 >>\nstream\nBT /F1 24 Tf 72 700 Td (H) Tj ET\nendstream",
            "<< /Type /Font /Subtype /TrueType /BaseFont /Helvetica /FirstChar 72 /LastChar 72"
                    + " /Widths 6 0 R /Encoding 7 0 R >>",
            "[ 722 ]",
            "<< /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [ 72 /H ] >>",
        };
        return pdfWithObjects(objects);
    }

    private static byte[] pdfWithObjects(String[] objects) {
        StringBuilder sb = new StringBuilder("%PDF-1.4\n");
        int[] offsets = new int[objects.length];
        for (int i = 0; i < objects.length; i++) {
            offsets[i] = sb.length();
            sb.append(i + 1).append(" 0 obj\n").append(objects[i]).append("\nendobj\n");
        }
        int xref = sb.length();
        sb.append("xref\n0 ").append(objects.length + 1).append("\n0000000000 65535 f \n");
        for (int offset : offsets) {
            sb.append(String.format("%010d 00000 n \n", offset));
        }
        sb.append("trailer\n<< /Size ").append(objects.length + 1)
                .append(" /Root 1 0 R >>\nstartxref\n").append(xref).append("\n%%EOF\n");
        return sb.toString().getBytes(StandardCharsets.ISO_8859_1);
    }

    @Test
    void aFontIsImportedWithTheObjectsItRefersTo() throws Exception {
        List<PDFobj> source = TestSupport.read(pdfWithIndirectWidths());
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.addResourceObjects(source);
        Page page = new Page(pdf, Letter.PORTRAIT);
        PDFobj content = pdf.getPageObjects(source).get(0).getContentObject(source);
        page.drawContents(content.getData(), 792f, 0f, 0f, 1f, 1f);
        pdf.complete();

        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        assertEquals("/Font", objects.get(4).getValue("/Type"));
        assertTrue(objects.get(5).getDict().contains("722"), objects.get(5).getDict().toString());
        assertEquals("/Encoding", objects.get(6).getValue("/Type"));
    }

    @Test
    void aPageTreeThatLoopsIsReadOnce() throws Exception {
        List<PDFobj> objects = TestSupport.read(pdfWithObjects(new String[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R 99 0 R] /Count 1 >>",
            "<< /Type /Pages /Parent 2 0 R /Kids [3 0 R 2 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        }));
        assertEquals(1, new PDF().getPageObjects(objects).size());
        PDF pdf = new PDF(new ByteArrayOutputStream());
        pdf.merge(objects);
        pdf.complete();
    }

    @Test
    void objectsWithoutAPageTreeHaveNoPages() throws Exception {
        List<PDFobj> objects = TestSupport.read(pdfWithObjects(new String[] {"<< /Type /Catalog >>"}));
        assertEquals(0, new PDF().getPageObjects(objects).size());
    }

    @Test
    void theNameOfAnEmbeddedFileIsATextStringInFAndUF() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        EmbeddedFile file = new EmbeddedFile(pdf, "\u00dcbersicht \u2013 r\u00e9sum\u00e9.txt",
                new java.io.ByteArrayInputStream("Hello".getBytes(StandardCharsets.US_ASCII)), false);
        new FileAttachment(file).setLocation(100f, 100f).drawOn(page);
        pdf.complete();
        PDFobj spec = TestSupport.findObject(TestSupport.read(bos.toByteArray()), "/UF");
        assertNotNull(spec);
        assertEquals("/Filespec", spec.getValue("/Type"));
        assertEquals("\u00dcbersicht \u2013 r\u00e9sum\u00e9.txt", TestSupport.utf16Hex(spec.getValue("/UF")));
        assertEquals(spec.getValue("/UF"), spec.getValue("/F"));
    }
    // The PDFs that are not valid, as the Go fuzz target of the reader found
    // them: each fails with a message or reads what it can, and none reads
    // past the file, allocates what the file does not have or traps in Swift.

    // The message that reading the PDF fails with, or "(no error)" when it
    // is read.
    private static String readError(String raw) throws Exception {
        try {
            TestSupport.read(raw.getBytes("ISO-8859-1"));
            return "(no error)";
        } catch (Exception e) {
            return e.getMessage();
        }
    }

    @Test
    void anObjectNumberedHigherThanTheFileHasBytesIsRefused() throws Exception {
        // One object of every number up to the one it says would take
        // gigabytes of memory for a file of 31 bytes.
        assertEquals("The PDF of 31 bytes cannot hold an object numbered 44444441.",
                readError("44444441 0 obj/Filter/Fl streil"));
    }

    @Test
    void aStreamLongerThanTheFileIsRefused() throws Exception {
        // The bytes of the stream are counted before it is made, so that a
        // file of a few bytes that says its stream is a gigabyte takes no
        // memory.
        assertEquals("The stream of an object is not in the PDF.",
                readError("1 0 obj<</Length 1000000000>>stream\nx\nendstream endobj"));
    }

    @Test
    void anObjectStreamThatIsNotANumberIsRefused() throws Exception {
        assertEquals("The object stream of the PDF is malformed: \"x\" is not a number.",
                readError("1 0 obj<</Type/ObjStm/First x>>stream\n\nendstream endobj"));
        // An object stream with no stream of its own has no objects.
        assertEquals("(no error)", readError("1 0 obj 1 0 obj/Type/ObjStm/First 0"));
    }

    @Test
    void aReferenceToAnObjectThatIsNotThereHasNoContents() throws Exception {
        // A page whose /Contents names an object the PDF does not have, and
        // one whose dictionary ends where a value belongs.
        List<PDFobj> objects = TestSupport.read((
                "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n"
                + "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n"
                + "3 0 obj<</Type/Page/Parent 2 0 R/Contents 99 0 R>>endobj\n"
                ).getBytes("ISO-8859-1"));
        List<PDFobj> pages = new PDF().getPageObjects(objects);
        assertEquals(1, pages.size());
        assertNull(pages.get(0).getContentObject(objects));
        assertNull(pages.get(0).getResourcesObject(objects));
        assertEquals("", pages.get(0).getValue("/Contents2"));
    }

    @Test
    void aDictionaryThatEndsInTheMiddleOfAValueIsClosedThere() throws Exception {
        List<PDFobj> objects = TestSupport.read(
                "1 0 obj<</Type/Catalog/Kids[3 0 R\n".getBytes("ISO-8859-1"));
        assertEquals(1, objects.size());
        assertEquals("[ 3 0 R ]", objects.get(0).getValue("/Kids"));
        assertEquals("", objects.get(0).getValue("/Nothing"));
    }

}
