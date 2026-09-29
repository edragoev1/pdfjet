/*
 * ReviewWriterTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.encryption.Passwords;
import com.pdfjet.encryption.Permissions;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.function.Executable;

/**
 * The tests of the writer: the catalog, the structure tree, the annotations,
 * the metadata and the files a document carries.
 */
class ReviewWriterTest {
    private interface Drawing {
        void draw(PDF pdf, Page page) throws Exception;
    }

    // Writes a document of the compliance with a heading on its one page,
    // drawn with drawing, and returns it.
    private static String document(Compliance compliance, Drawing drawing) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, compliance).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf), "Heading")
                .setStructureType(StructElem.H1).setLocation(50f, 50f).drawOn(page);
        if (drawing != null) {
            drawing.draw(pdf, page);
        }
        pdf.complete();
        return TestSupport.latin1(bos.toByteArray());
    }

    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i != -1; i = text.indexOf(part, i + part.length())) {
            n++;
        }
        return n;
    }

    private static String fails(Class<? extends Exception> type, Executable executable) {
        return assertThrows(type, executable).getMessage();
    }

    @Test
    @SuppressWarnings("deprecation")
    void aPDFAOfLevelBIsNotTagged() throws Exception {
        for (Compliance compliance : new Compliance[] {
                Compliance.PDF_A_1B, Compliance.PDF_A_2B, Compliance.PDF_A_3B}) {
            String raw = document(compliance, null);
            for (String entry : new String[] {"/StructTreeRoot", "/MarkInfo", "/Tabs /S", "/StructParents", "/StructElem"}) {
                assertFalse(raw.contains(entry), compliance + " has " + entry);
            }
            for (String entry : new String[] {"/Lang <", "/DisplayDocTitle true"}) {
                assertTrue(raw.contains(entry), compliance + " has no " + entry);
            }
        }
        for (Compliance compliance : new Compliance[] {
                Compliance.PDF_A_1A, Compliance.PDF_A_2A, Compliance.PDF_A_3A,
                Compliance.PDF_UA_1, Compliance.PDF_A_3A_UA_1}) {
            String raw = document(compliance, null);
            for (String entry : new String[] {"/StructTreeRoot", "/MarkInfo <</Marked true>>", "/Tabs /S", "/StructParents 0"}) {
                assertTrue(raw.contains(entry), compliance + " has no " + entry);
            }
        }
    }

    @Test
    void theNoticeOfAFontIsEscapedInItsMetadata() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.addMetadataObject("Copyright A & B <c>", true);
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains("Copyright A &amp; B &lt;c&gt;"), raw);

        List<PDFobj> objects = new ArrayList<PDFobj>();
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        font.info = "Copyright A & B";
        int number = FontStream2.addMetadataObject(objects, font);
        String xml = new String(objects.get(number - 1).stream, StandardCharsets.UTF_8);
        assertTrue(xml.contains("Copyright A &amp; B"), xml);
    }

    // Draws a square, a circle, a polygon, a note and a file.
    private static void annotations(PDF pdf, Page page) throws Exception {
        SquareAnnotation square = new SquareAnnotation();
        square.setLocation(100f, 100f);
        square.setSize(50f, 50f);
        square.setContents("A square");
        square.drawOn(page);
        CircleAnnotation circle = new CircleAnnotation();
        circle.setLocation(200f, 100f);
        circle.setSize(80f, 40f);
        circle.setContents("A circle");
        circle.drawOn(page);
        PolygonAnnotation polygon = new PolygonAnnotation().setVertices(new float[] {0f, 0f, 50f, 0f, 25f, 40f});
        polygon.setLocation(300f, 300f);
        polygon.setContents("A polygon");
        polygon.drawOn(page);
        TextAnnotation note = new TextAnnotation();
        note.setLocation(100f, 400f);
        note.setSize(20f, 20f);
        note.setContents("A note");
        note.drawOn(page);
        EmbeddedFile file = new EmbeddedFile(pdf, "a.txt",
                new ByteArrayInputStream("A file".getBytes(StandardCharsets.UTF_8)), false);
        FileAttachment attachment = new FileAttachment(file);
        attachment.setLocation(200f, 400f);
        attachment.setContents("A file");
        attachment.drawOn(page);
    }

    @Test
    @SuppressWarnings("deprecation")
    void everyAnnotationIsPrintedAndHasAnAppearance() throws Exception {
        for (Compliance compliance : new Compliance[] {Compliance.PDF_1_7, Compliance.PDF_A_2B, Compliance.PDF_A_3A}) {
            String raw = document(compliance, ReviewWriterTest::annotations);
            assertEquals(5, count(raw, "/Type /Annot\n"), compliance.toString());
            assertEquals(5, count(raw, "/F 4\n"), compliance.toString());
            assertEquals(5, count(raw, "/AP <</N "), compliance.toString());
            assertEquals(5, count(raw, "/Subtype /Form\n"), compliance.toString());
        }
        // The square is drawn in its fill color in its box, which is its rectangle.
        String raw = document(Compliance.PDF_1_7, ReviewWriterTest::annotations);
        assertTrue(raw.contains("/BBox [100 642 150 692]\n/Length 34\n>>\nstream\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n"), raw);
        // The note and the file are drawn as their icons, scaled to their boxes.
        assertTrue(raw.contains("q\n20 0 0 20 100 372 cm\n" + Annotation.NOTE_ICON + "Q\n"), raw);
        assertTrue(raw.contains("q\n24 0 0 24 200 368 cm\n" + Annotation.PUSH_PIN_ICON + "Q\n"), raw);
    }

    @Test
    void theRectangleOfAnAnnotationIsFromItsLowerLeftCorner() throws Exception {
        String raw = document(Compliance.PDF_1_7, new Drawing() {
            public void draw(PDF pdf, Page page) throws Exception {
                annotations(pdf, page);
                new TextLine(TestSupport.helvetica(pdf), "Link").setURIAction("https://pdfjet.com")
                        .setLocation(50f, 100f).drawOn(page);
            }
        });
        for (String rect : new String[] {
                "/Rect [100 642 150 692]", "/Rect [200 652 280 692]", "/Rect [300 452 350 492]",
                "/Rect [100 372 120 392]", "/Rect [200 368 224 392]"}) {
            assertTrue(raw.contains(rect), rect);
        }
        Matcher m = Pattern.compile("/Rect \\[(\\S+) (\\S+) (\\S+) (\\S+)\\]").matcher(raw);
        int rects = 0;
        while (m.find()) {
            assertTrue(Float.parseFloat(m.group(1)) <= Float.parseFloat(m.group(3)), m.group());
            assertTrue(Float.parseFloat(m.group(2)) <= Float.parseFloat(m.group(4)), m.group());
            rects++;
        }
        assertEquals(6, rects);
    }

    @Test
    void aShapeThatIsNotOpaqueIsDrawnWithItsOpacity() throws Exception {
        String raw = document(Compliance.PDF_1_7, new Drawing() {
            public void draw(PDF pdf, Page page) throws Exception {
                SquareAnnotation square = new SquareAnnotation();
                square.setLocation(100f, 100f);
                square.setSize(50f, 50f);
                square.setOpacity(0.5f);
                square.setContents("A square");
                square.drawOn(page);
            }
        });
        assertTrue(raw.contains("/BBox [100 642 150 692]\n/Resources <</ExtGState <</GS0 <</CA 0.5 /ca 0.5>>>>>>\n" +
                "/Length 42\n>>\nstream\n/GS0 gs\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n"), raw);
    }

    @Test
    void aLinkToADestinationTheDocumentDoesNotHaveIsRefused() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf), "Nowhere").setGoToAction("missing").setLocation(50f, 100f).drawOn(page);
        assertEquals("The link goes to the destination missing, which the document does not have.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.complete(); }
        }));
    }

    @Test
    void theTypesOfPDF20AreMappedInTheRoleMap() throws Exception {
        assertFalse(document(Compliance.PDF_UA_1, null).contains("/RoleMap"));
        String raw = document(Compliance.PDF_UA_1, new Drawing() {
            public void draw(PDF pdf, Page page) throws Exception {
                Font font = TestSupport.helvetica(pdf);
                new TextLine(font, "Strong").setStructureType(StructElem.STRONG).setLocation(50f, 100f).drawOn(page);
                new TextLine(font, "Title").setStructureType(StructElem.TITLE).setLocation(50f, 120f).drawOn(page);
            }
        });
        assertTrue(raw.contains("/RoleMap << /Title /P /Strong /Span >>\n"), raw);
    }

    @Test
    void aTextLineOfTheTypeArtifactIsAnArtifact() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf), "Header")
                .setStructureType(StructElem.ARTIFACT).setLocation(50f, 50f).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("/Artifact BMC\n"), content);
        assertFalse(content.contains("/Artifact <<"), content);
        assertEquals(0, page.structures.size());

        PDF pdf2 = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        final Page page2 = new Page(pdf2, Letter.PORTRAIT);
        assertEquals("An artifact is not a structure element: draw it between addArtifactBMC and addEMC.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { page2.beginStructElement(StructElem.ARTIFACT); }
        }));
    }

    @Test
    void anAnnotationOnAWrittenPageOfATaggedDocumentIsRefused() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        final Page page1 = new Page(pdf, Letter.PORTRAIT);
        new Page(pdf, Letter.PORTRAIT);
        final SquareAnnotation square = new SquareAnnotation();
        square.setContents("Late");
        assertEquals("The page was already written to the PDF: "
                + "draw on a page before creating the next page or completing the PDF.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { square.drawOn(page1); }
        }));
        assertEquals(0, page1.annots.size());
    }

    @Test
    @SuppressWarnings("deprecation")
    void aPDFUADocumentNeedsATitle() throws Exception {
        for (Compliance compliance : new Compliance[] {Compliance.PDF_UA_1, Compliance.PDF_A_3A_UA_1}) {
            final PDF pdf = new PDF(new ByteArrayOutputStream(), compliance).setTitle(" ");
            new Page(pdf, Letter.PORTRAIT);
            assertEquals("A PDF/UA document needs a title: use setTitle.",
                    fails(IllegalStateException.class, new Executable() {
                public void execute() throws Throwable { pdf.complete(); }
            }));
        }
    }

    @Test
    void anAnnotationOfATaggedDocumentNeedsADescription() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        final Page page = new Page(pdf, Letter.PORTRAIT);
        assertEquals("An annotation of a tagged document, PDF/UA or PDF/A of level A, "
                + "needs contents, a title or an alternative description.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { new SquareAnnotation().drawOn(page); }
        }));
        // One that is not tagged needs none.
        PDF pdf2 = TestSupport.newPDF();
        new SquareAnnotation().drawOn(new Page(pdf2, Letter.PORTRAIT));
        pdf2.complete();
    }

    // Returns the text as the writer writes a text string that is not encrypted.
    private static String textString(String text) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        int start = bos.size();
        pdf.appendTextString(text);
        return TestSupport.latin1(bos.toByteArray()).substring(start);
    }

    @Test
    void theInformationSaysWhatTheMetadataSays() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_A_2B);
        pdf.setTitle("A\u0001B\uD800C￾D\tE").setAuthor("F\u0002G");
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        String raw = new String(bos.toByteArray(), StandardCharsets.UTF_8);
        assertTrue(raw.contains("<rdf:li xml:lang=\"x-default\">ABCD\tE</rdf:li>"), raw);
        assertTrue(raw.contains("/Title " + textString("ABCD\tE") + "\n"), raw);
        assertTrue(raw.contains("<rdf:li>FG</rdf:li>"), raw);
        assertTrue(raw.contains("/Author " + textString("FG") + "\n"), raw);
    }

    @Test
    void theDateOfAnEmbeddedFileIsEncrypted() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        pdf.setEncryption(new Encryption(pdf, new Passwords(), new Permissions()));
        String date = pdf.getDate();
        new EmbeddedFile(pdf, "a.xml", new ByteArrayInputStream("<a/>".getBytes(StandardCharsets.UTF_8)),
                false, "text/xml", Relationship.DATA, null);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertFalse(raw.contains(date), "the date is not encrypted");
        assertFalse(raw.contains(Util.toHexString(date.getBytes(StandardCharsets.US_ASCII))), "the date is not encrypted");
        assertTrue(raw.contains("/ModDate <"), raw);
    }

    @Test
    void completeClosesTheStreamWhenItFails() throws Exception {
        final boolean[] closed = {false};
        final PDF pdf = new PDF(new ByteArrayOutputStream() {
            @Override
            public void close() {
                closed[0] = true;
            }
        });
        fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.complete(); }
        });
        assertTrue(closed[0], "the stream is open");
    }

    @Test
    void theLayersAreOrderedByTheirUTF16Names() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        List<OptionalContentGroup> groups = new ArrayList<OptionalContentGroup>();
        for (String name : new String[] {"", "b", "a", "😀", "a"}) {
            OptionalContentGroup group = new OptionalContentGroup(pdf, name);
            group.add(new Rect(10f, 10f, 20f, 20f));
            group.drawOn(page);
            groups.add(group);
        }
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        StringBuilder order = new StringBuilder("/Order [");
        for (int i : new int[] {2, 4, 1, 3, 0}) {
            order.append(" ").append(groups.get(i).objNumber).append(" 0 R ");
        }
        assertTrue(raw.contains(order + "]\n"), raw);
    }

    @Test
    @SuppressWarnings("deprecation")
    void aPDFA3FileNeedsAMediaType() throws Exception {
        for (Compliance compliance : new Compliance[] {Compliance.PDF_A_3A, Compliance.PDF_A_3B, Compliance.PDF_A_3A_UA_1}) {
            final PDF pdf = new PDF(new ByteArrayOutputStream(), compliance);
            final EmbeddedFile file = new EmbeddedFile(pdf, "a.xml",
                    new ByteArrayInputStream("<a/>".getBytes(StandardCharsets.UTF_8)),
                    false, null, Relationship.DATA, "Data.");
            assertEquals("The file a.xml was embedded without a media type, "
                    + "which a file of a document of PDF/A-3 needs.",
                    fails(IllegalArgumentException.class, new Executable() {
                public void execute() throws Throwable { pdf.addAssociatedFile(file); }
            }));
        }
        PDF pdf = TestSupport.newPDF();
        pdf.addAssociatedFile(new EmbeddedFile(pdf, "a.xml",
                new ByteArrayInputStream("<a/>".getBytes(StandardCharsets.UTF_8)),
                false, null, Relationship.DATA, "Data."));
    }

    @Test
    void theProducerIsTheVersionOfTheLibrary() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        assertTrue(TestSupport.latin1(bos.toByteArray()).contains("/Producer " + textString("PDFjet v9.0.2")));
    }

    @Test
    void theHexadecimalOfBytesIsLowerCase() {
        assertEquals("00ff7f10", Util.toHexString(new byte[] {0, (byte) 0xFF, 0x7F, 0x10}));
    }

    @Test
    void theFontFileRefersToItsMetadataWhoseNoticeIsEscaped() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_A_2B);
        Font font = new Font(pdf, TestSupport.file("fonts/NotoSansJP/NotoSansJP-Regular.ttf").getPath());
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Hello").setLocation(50f, 50f).drawOn(page);
        pdf.complete();
        String raw = new String(bos.toByteArray(), StandardCharsets.UTF_8);
        assertTrue(java.util.regex.Pattern.compile(
                "\\d+ 0 obj\n<<\n/Filter /FlateDecode\n/Length1 \\d+\n/Metadata \\d+ 0 R\n/Length \\d+\n>>\nstream\n")
                .matcher(raw).find(), "the font file does not refer to its metadata");
        assertTrue(raw.contains("<xmpRights:UsageTerms>") && raw.contains(" &amp; "), "the notice is not escaped");
    }
}
