/*
 * MergeTest.java
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

import com.pdfjet.encryption.Passwords;
import com.pdfjet.encryption.Permissions;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;

/** Merging the pages of documents that were read. */
class MergeTest {
    // Returns a document with one page for each text, drawn with Helvetica.
    private static byte[] document(String... texts) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        for (String text : texts) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            new TextLine(font, text).setLocation(50f, 50f).drawOn(page);
        }
        pdf.complete();
        return bos.toByteArray();
    }

    // Returns the decoded content of each page, in the order of the pages.
    private static List<String> pageContents(List<PDFobj> objects) {
        List<String> contents = new ArrayList<String>();
        for (PDFobj page : new PDF().getPageObjects(objects)) {
            contents.add(TestSupport.latin1(page.getContentObject(objects).getData()));
        }
        return contents;
    }

    // Checks that every reference of the document is to an object it has.
    private static void assertReferencesResolve(List<PDFobj> objects) {
        for (PDFobj obj : objects) {
            List<String> dict = obj.dict;
            for (int i = 0; i + 2 < dict.size(); i++) {
                if (dict.get(i + 2).equals("R") && dict.get(i).matches("\\d+") && dict.get(i + 1).matches("\\d+")) {
                    int number = Integer.parseInt(dict.get(i));
                    assertTrue(number >= 1 && number <= objects.size() && !objects.get(number - 1).dict.isEmpty(),
                            "object " + obj.number + " refers to the missing object " + number);
                }
            }
        }
    }

    @Test
    void mergesDocumentsInTheirOrder() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.merge(TestSupport.read(document("A1", "A2")));
        pdf.merge(TestSupport.read(document("B1")));
        pdf.complete();
        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        List<String> contents = pageContents(objects);
        assertEquals(3, contents.size());
        assertTrue(contents.get(0).contains(TestSupport.hex("A1")), contents.get(0));
        assertTrue(contents.get(1).contains(TestSupport.hex("A2")), contents.get(1));
        assertTrue(contents.get(2).contains(TestSupport.hex("B1")), contents.get(2));
        assertReferencesResolve(objects);
    }

    @Test
    void drawnPagesKeepTheirPlace() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        new TextLine(font, "G1").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.merge(TestSupport.read(document("B1")));
        new TextLine(font, "G2").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        List<String> contents = pageContents(objects);
        assertEquals(3, contents.size());
        assertTrue(contents.get(0).contains(TestSupport.hex("G1")), contents.get(0));
        assertTrue(contents.get(1).contains(TestSupport.hex("B1")), contents.get(1));
        assertTrue(contents.get(2).contains(TestSupport.hex("G2")), contents.get(2));
        assertReferencesResolve(objects);
    }

    @Test
    void aMergedPageInheritsFromThePageTree() throws Exception {
        String content = "BT /F1 24 Tf 20 300 Td (Inherited) Tj ET";
        String source = "%PDF-1.4\n"
                + "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n"
                + "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 300 400] /Rotate 90"
                + " /Resources << /Font << /F1 4 0 R >> >> >> endobj\n"
                + "3 0 obj << /Type /Page /Parent 2 0 R /Contents 5 0 R >> endobj\n"
                + "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
                + "5 0 obj << /Length " + content.length() + " >>\nstream\n" + content + "\nendstream\nendobj\n"
                + "trailer << /Root 1 0 R >>\n%%EOF\n";
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.merge(TestSupport.read(source.getBytes(StandardCharsets.ISO_8859_1)));
        pdf.complete();

        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        PDFobj page = new PDF().getPageObjects(objects).get(0);
        assertEquals(300f, page.getPageSize().getWidth(), 0f);
        assertEquals(400f, page.getPageSize().getHeight(), 0f);
        assertEquals("90", page.getValue("/Rotate"));
        assertTrue(page.dict.contains("/F1"), page.dict.toString());
        int parent = page.getObjectNumbers("/Parent").get(0);
        assertEquals("/Pages", objects.get(parent - 1).getValue("/Type"));
        assertTrue(pageContents(objects).get(0).contains("(Inherited) Tj"));
        assertReferencesResolve(objects);
    }

    @Test
    void linksPointAtTheMergedPages() throws Exception {
        ByteArrayOutputStream source = new ByteArrayOutputStream();
        PDF pdf1 = new PDF(source);
        Font font1 = TestSupport.helvetica(pdf1);
        Page page1 = new Page(pdf1, Letter.PORTRAIT);
        new TextLine(font1, "Go").setGoToAction("there").setLocation(50f, 50f).drawOn(page1);
        Page page2 = new Page(pdf1, Letter.PORTRAIT);
        page2.addDestination("there", 30f, 100f);
        pdf1.complete();

        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        new TextLine(TestSupport.helvetica(pdf), "Cover").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.merge(TestSupport.read(source.toByteArray()));
        pdf.complete();

        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        List<PDFobj> pages = new PDF().getPageObjects(objects);
        assertEquals(3, pages.size());
        PDFobj link = null;
        for (PDFobj obj : objects) {
            if (obj.getValue("/Subtype").equals("/Link")) {
                link = obj;
            }
        }
        assertNotNull(link);
        // The link on the second page points at the third page and is listed by the second.
        int dest = link.dict.indexOf("/Dest");
        assertEquals("[", link.dict.get(dest + 1));
        assertEquals(String.valueOf(pages.get(2).number), link.dict.get(dest + 2));
        assertEquals("R", link.dict.get(dest + 4));
        assertTrue(pages.get(1).getObjectNumbers("/Annots").contains(link.number));
        assertReferencesResolve(objects);
    }

    @Test
    void mergesTheListedPagesInTheirOrder() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.merge(TestSupport.read(document("A1", "A2", "A3")), 3, 1);
        pdf.complete();
        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        List<String> contents = pageContents(objects);
        assertEquals(2, contents.size());
        assertTrue(contents.get(0).contains(TestSupport.hex("A3")), contents.get(0));
        assertTrue(contents.get(1).contains(TestSupport.hex("A1")), contents.get(1));
        assertReferencesResolve(objects);
    }

    @Test
    void splitsADocumentIntoOnePDFPerPage() throws Exception {
        List<PDFobj> source = TestSupport.read(document("A1", "A2", "A3"));
        for (int i = 1; i <= 3; i++) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            PDF part = new PDF(bos);
            part.merge(source, i);
            part.complete();
            List<PDFobj> objects = TestSupport.read(bos.toByteArray());
            List<String> contents = pageContents(objects);
            assertEquals(1, contents.size());
            assertTrue(contents.get(0).contains(TestSupport.hex("A" + i)), contents.get(0));
            assertReferencesResolve(objects);
        }
    }

    @Test
    void aLinkToAPageThatIsNotMergedLeadsNowhere() throws Exception {
        ByteArrayOutputStream source = new ByteArrayOutputStream();
        PDF pdf1 = new PDF(source);
        Page page1 = new Page(pdf1, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf1), "Go").setGoToAction("there").setLocation(50f, 50f).drawOn(page1);
        Page page2 = new Page(pdf1, Letter.PORTRAIT);
        page2.addDestination("there", 30f, 100f);
        pdf1.complete();

        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.merge(TestSupport.read(source.toByteArray()), 1);
        pdf.complete();

        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        assertEquals(1, new PDF().getPageObjects(objects).size());
        PDFobj link = null;
        for (PDFobj obj : objects) {
            if (obj.getValue("/Subtype").equals("/Link")) {
                link = obj;
            }
        }
        assertNotNull(link);
        int dest = link.dict.indexOf("/Dest");
        assertEquals("[", link.dict.get(dest + 1));
        assertEquals("null", link.dict.get(dest + 2));
        assertReferencesResolve(objects);
    }

    @Test
    void anEncryptedDocumentMergesThePagesEncrypted() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.setEncryption(new Encryption(pdf, new Passwords(), new Permissions()));
        pdf.merge(TestSupport.read(document("Secret A", "Secret B")));
        pdf.complete();
        byte[] bytes = bos.toByteArray();
        assertTrue(TestSupport.latin1(bytes).contains("/Encrypt "));
        List<PDFobj> objects = TestSupport.read(bytes, "");
        List<String> contents = pageContents(objects);
        assertEquals(2, contents.size());
        assertTrue(contents.get(0).contains(TestSupport.hex("Secret A")), contents.get(0));
        assertTrue(contents.get(1).contains(TestSupport.hex("Secret B")), contents.get(1));
        assertReferencesResolve(objects);
    }

    @Test
    void anEncryptedDocumentIsMergedDecrypted() throws Exception {
        ByteArrayOutputStream source = new ByteArrayOutputStream();
        PDF pdf1 = new PDF(source);
        pdf1.setEncryption(new Encryption(pdf1, new Passwords(), new Permissions()));
        new TextLine(TestSupport.helvetica(pdf1), "Plain").setLocation(50f, 50f).drawOn(new Page(pdf1, Letter.PORTRAIT));
        pdf1.complete();

        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.merge(TestSupport.read(source.toByteArray(), ""));
        pdf.complete();
        byte[] bytes = bos.toByteArray();
        assertFalse(TestSupport.latin1(bytes).contains("/Encrypt "));
        List<PDFobj> objects = TestSupport.read(bytes);
        assertTrue(pageContents(objects).get(0).contains(TestSupport.hex("Plain")));
        assertReferencesResolve(objects);
    }

    @Test
    void mergesTheTestDocuments() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        for (String name : new String[] {"wirth.pdf", "rc65-16e.pdf", "PDFjetLogo.pdf"}) {
            pdf.merge(new PDF().read(TestSupport.open("data/testPDFs/" + name)));
        }
        pdf.complete();
        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        List<PDFobj> pages = new PDF().getPageObjects(objects);
        assertEquals(8, pages.size());
        for (PDFobj page : pages) {
            assertNotNull(page.getContentObject(objects).getData());
        }
        assertReferencesResolve(objects);
    }

    @Test
    void mergeIsRefusedWhereItWouldBreakTheDocument() throws Exception {
        final List<PDFobj> objects = TestSupport.read(document("A"));

        final PDF ua = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        assertEquals("Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document.",
                assertThrows(IllegalStateException.class, () -> ua.merge(objects)).getMessage());

        final PDF completed = new PDF(new ByteArrayOutputStream());
        new Page(completed, Letter.PORTRAIT);
        completed.complete();
        assertEquals("The PDF was already completed.",
                assertThrows(IllegalStateException.class, () -> completed.merge(objects)).getMessage());

        final PDF rewritten = new PDF(new ByteArrayOutputStream());
        rewritten.addObjects(TestSupport.read(document("B")));
        assertEquals("merge and addObjects cannot be used on the same PDF.",
                assertThrows(IllegalStateException.class, () -> rewritten.merge(objects)).getMessage());

        final PDF merged = new PDF(new ByteArrayOutputStream());
        merged.merge(objects);
        assertEquals("merge and addObjects cannot be used on the same PDF.",
                assertThrows(IllegalStateException.class,
                        () -> merged.addObjects(TestSupport.read(document("C")))).getMessage());

        final PDF empty = new PDF(new ByteArrayOutputStream());
        assertEquals("The objects have no root /Pages object.",
                assertThrows(IllegalArgumentException.class,
                        () -> empty.merge(new ArrayList<PDFobj>())).getMessage());

        final PDF split = new PDF(new ByteArrayOutputStream());
        assertEquals("The document has no page 0.",
                assertThrows(IllegalArgumentException.class, () -> split.merge(objects, 0)).getMessage());
        assertEquals("The document has no page 2.",
                assertThrows(IllegalArgumentException.class, () -> split.merge(objects, 2)).getMessage());
        assertEquals("Page 1 is listed twice.",
                assertThrows(IllegalArgumentException.class, () -> split.merge(objects, 1, 1)).getMessage());
    }
}
