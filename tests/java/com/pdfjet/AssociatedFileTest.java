/*
 * AssociatedFileTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;

/** The files a document carries with it, which PDF/A-3 calls associated files. */
class AssociatedFileTest {
    private static final String XML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<invoice/>\n";

    private static ByteArrayInputStream bytes(String text) {
        return new ByteArrayInputStream(text.getBytes(StandardCharsets.UTF_8));
    }

    private static EmbeddedFile attach(PDF pdf, String fileName, String text) throws Exception {
        return new EmbeddedFile(pdf, fileName, bytes(text), false,
                "text/xml", Relationship.ALTERNATIVE, "The invoice, as data.");
    }

    // A document of one page that carries the files.
    private static String document(PDF pdf, ByteArrayOutputStream bos, String... fileNames)
            throws Exception {
        for (String fileName : fileNames) {
            pdf.addAssociatedFile(attach(pdf, fileName, XML));
        }
        new TextLine(TestSupport.helvetica(pdf), "Invoice")
                .setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        return TestSupport.latin1(bos.toByteArray());
    }

    private static String document(String... fileNames) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        return document(new PDF(bos, Compliance.PDF_A_3B), bos, fileNames);
    }

    @Test
    void theCatalogSaysWhichFilesTheDocumentCarries() throws Exception {
        String raw = document("factur-x.xml");
        Matcher files = Pattern.compile("/AF \\[(\\d+) 0 R\\]").matcher(raw);
        assertTrue(files.find(), raw.substring(raw.lastIndexOf("/Type /Catalog")));
        // The number is the file specification, not the stream of the bytes.
        assertTrue(raw.contains(files.group(1) + " 0 obj\n<<\n/Type /Filespec"), files.group(1));
    }

    @Test
    void aReaderFindsTheFileByItsName() throws Exception {
        String raw = document("factur-x.xml");
        Matcher names = Pattern.compile(
                "/Names <</EmbeddedFiles <</Names \\[<([0-9a-fA-F]+)> (\\d+) 0 R\\]>>>>").matcher(raw);
        assertTrue(names.find(), raw.substring(raw.lastIndexOf("/Type /Catalog")));
        assertEquals("factur-x.xml", TestSupport.utf16Hex(names.group(1)));
        assertTrue(raw.contains("/AF [" + names.group(2) + " 0 R]"));
    }

    @Test
    void theNamesOfTheFilesAreInOrder() throws Exception {
        String raw = document("invoice.xml", "data.xml", "Notes.txt");
        Matcher names = Pattern.compile("/Names \\[(.+?)\\]>>>>").matcher(raw);
        assertTrue(names.find(), raw.substring(raw.lastIndexOf("/Type /Catalog")));
        StringBuilder order = new StringBuilder();
        Matcher name = Pattern.compile("<([0-9a-fA-F]+)>").matcher(names.group(1));
        while (name.find()) {
            order.append(TestSupport.utf16Hex(name.group(1))).append(' ');
        }
        assertEquals("Notes.txt data.xml invoice.xml ", order.toString());
        // The /AF array is the order the files were added in, which the
        // specification leaves to the writer of the document.
        assertEquals(3, raw.split("/Type /Filespec", -1).length - 1);
    }

    @Test
    void theFileSaysWhatItHoldsAndHowItRelatesToTheDocument() throws Exception {
        String raw = document("factur-x.xml");
        int filespec = raw.indexOf("/Type /Filespec");
        String dictionary = raw.substring(filespec, raw.indexOf("endobj", filespec));
        assertTrue(dictionary.contains("/AFRelationship /Alternative\n"), dictionary);
        assertTrue(dictionary.contains("/Desc <"), dictionary);
        assertEquals("The invoice, as data.", TestSupport.utf16Hex(
                dictionary.substring(dictionary.indexOf("/Desc <") + 6,
                        dictionary.indexOf('>', dictionary.indexOf("/Desc <")) + 1)));
        // Readers of PDF 1.7 look at /UF first and older ones at /F, and
        // PDF/A-3 asks for both, in the file specification and in /EF.
        assertTrue(dictionary.contains("/F <"), dictionary);
        assertTrue(dictionary.contains("/UF <"), dictionary);
        Matcher stream = Pattern.compile("/EF <</F (\\d+) 0 R /UF (\\d+) 0 R>>").matcher(dictionary);
        assertTrue(stream.find(), dictionary);
        assertEquals(stream.group(1), stream.group(2));

        int file = raw.indexOf(stream.group(1) + " 0 obj\n<<\n/Type /EmbeddedFile");
        String embedded = raw.substring(file, raw.indexOf("stream\n", file));
        assertTrue(embedded.contains("/Subtype /text#2Fxml\n"), embedded);
        assertTrue(embedded.contains("/Params <</Size " + XML.length() + " /ModDate (D:"), embedded);
        assertTrue(embedded.contains("/Length " + XML.length() + "\n"), embedded);
    }

    @Test
    void theSizeOfACompressedFileIsTheSizeItHadBeforeItWasCompressed() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_A_3B);
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 1000; i++) {
            text.append("<line>The same line, over and over.</line>\n");
        }
        pdf.addAssociatedFile(new EmbeddedFile(pdf, "long.xml", bytes(text.toString()), true,
                "text/xml", Relationship.DATA, "A long file."));
        new TextLine(TestSupport.helvetica(pdf), "x")
                .setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains("/Params <</Size " + text.length() + " /ModDate (D:"), "the size");
        assertTrue(raw.contains("/Filter /FlateDecode\n"), "the filter");
        Matcher length = Pattern.compile("/Length (\\d+)\n").matcher(
                raw.substring(raw.indexOf("/Type /EmbeddedFile")));
        assertTrue(length.find());
        assertTrue(Integer.parseInt(length.group(1)) < text.length() / 10, length.group(1));
    }

    @Test
    void theMediaTypeIsWrittenAsANameWhateverItHolds() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_A_3B);
        pdf.addAssociatedFile(new EmbeddedFile(pdf, "sheet.ods", bytes("x"), false,
                "application/vnd.oasis.opendocument.spreadsheet", Relationship.SOURCE, "A sheet."));
        pdf.addAssociatedFile(new EmbeddedFile(pdf, "odd.bin", bytes("x"), false,
                "application/x-(odd) #1", Relationship.SUPPLEMENT, "Something else."));
        new TextLine(TestSupport.helvetica(pdf), "x")
                .setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains(
                "/Subtype /application#2Fvnd.oasis.opendocument.spreadsheet\n"), "the sheet");
        assertTrue(raw.contains("/Subtype /application#2Fx-#28odd#29#20#231\n"), "the odd one");
        assertTrue(raw.contains("/AFRelationship /Source\n"), "the source");
        assertTrue(raw.contains("/AFRelationship /Supplement\n"), "the supplement");
    }

    @Test
    void aDocumentThatCarriesNoFilesSaysNothingAboutThem() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_A_3B);
        new TextLine(TestSupport.helvetica(pdf), "x")
                .setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(!raw.contains("/AF ["), "the array");
        assertTrue(!raw.contains("/EmbeddedFiles"), "the names");
    }

    @Test
    void aFileThatSaysNothingAboutItselfIsNotOneTheDocumentCanCarry() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_A_3B);
        EmbeddedFile file = new EmbeddedFile(pdf, "factur-x.xml", bytes(XML), false);
        IllegalArgumentException problem = assertThrows(IllegalArgumentException.class,
                () -> pdf.addAssociatedFile(file));
        assertTrue(problem.getMessage().contains("factur-x.xml"), problem.getMessage());
        // The file it does not carry is still embedded, for an annotation of a
        // page to point at, and is written without the entries of PDF/A-3.
        assertTrue(!problem.getMessage().contains("/AFRelationship"), problem.getMessage());
    }

    @Test
    void theLevelOfPdfA3aAndPdfUA1IsBoth() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_A_3A_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf), "Invoice").setLocation(50f, 50f).drawOn(page);
        // Tagged, as PDF/UA asks, where PDF/A-3a alone is not
        assertTrue(TestSupport.content(page).contains("BDC"), "the text is not tagged");
        String raw = document(pdf, bos, "factur-x.xml");
        for (String want : new String[] {"<pdfuaid:part>1</pdfuaid:part>", "<pdfaid:part>3</pdfaid:part>", "<pdfaid:conformance>A</pdfaid:conformance>", "<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>", "/StructTreeRoot", "/OutputIntents", "/AF ["}) {
            assertTrue(raw.contains(want), want);
        }
    }

    @Test
    void theMetadataHasOneListOfExtensionSchemas() throws Exception {
        // A document of PDF/A-3a and PDF/UA-1 that adds a list of its own, as
        // Factur-X does, has the PDF/UA identification schema in that list.
        for (boolean own : new boolean[] {false, true}) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            PDF pdf = new PDF(bos, Compliance.PDF_A_3A_UA_1);
            if (own) {
                pdf.addMetadata("<rdf:Description rdf:about=\"\" xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\">\n" +
                        "  <pdfaExtension:schemas>\n" +
                        "    <rdf:Bag>\n" +
                        "    </rdf:Bag>\n" +
                        "  </pdfaExtension:schemas>\n" +
                        "</rdf:Description>\n");
            }
            String raw = document(pdf, bos, "factur-x.xml");
            assertEquals(1, raw.split("<pdfaExtension:schemas>", -1).length - 1, "own list " + own);
            assertTrue(raw.contains("<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>"), "own list " + own);
        }
    }

    @Test
    void theDocumentsThatCannotCarryAFileSayTheyCannot() throws Exception {
        for (Compliance compliance : new Compliance[] {
                Compliance.PDF_A_1A, Compliance.PDF_A_1B,
                Compliance.PDF_A_2A, Compliance.PDF_A_2B}) {
            PDF pdf = new PDF(new ByteArrayOutputStream(), compliance);
            EmbeddedFile file = attach(pdf, "factur-x.xml", XML);
            IllegalStateException problem = assertThrows(IllegalStateException.class,
                    () -> pdf.addAssociatedFile(file));
            assertTrue(problem.getMessage().contains(compliance.toString()), problem.getMessage());
        }
        // The documents that can: PDF/A-3, and the ones of no profile at all.
        for (Compliance compliance : new Compliance[] {
                Compliance.PDF_A_3A, Compliance.PDF_A_3B,
                Compliance.PDF_1_7, Compliance.PDF_UA_1}) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            PDF pdf = new PDF(bos, compliance);
            document(pdf, bos, "factur-x.xml");
            assertTrue(TestSupport.latin1(bos.toByteArray()).contains("/AF ["), compliance.toString());
        }
    }

    @Test
    void theMetadataCarriesTheDescriptionsOfTheStandardsOfTheDocument() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_A_3B);
        pdf.addMetadata("<rdf:Description rdf:about=\"\" xmlns:fx=\"urn:test:1p0#\">\n"
                + "  <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>\n"
                + "</rdf:Description>");
        document(pdf, bos, "factur-x.xml");
        String raw = TestSupport.latin1(bos.toByteArray());
        int description = raw.indexOf("<fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>");
        assertTrue(description != -1, "the property is written");
        // Inside the metadata: after the description of the document and
        // before the end of the RDF.
        assertTrue(raw.lastIndexOf("</pdfaid:conformance>", description) != -1, "after the document");
        assertTrue(raw.indexOf("</rdf:RDF>", description) != -1, "before the end");
        assertTrue(raw.indexOf("</x:xmpmeta>", description) != -1, "inside the metadata");
    }
}
