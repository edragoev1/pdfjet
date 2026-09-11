/*
 * PDF.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import com.pdfjet.barcodes.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.text.*;
import java.util.*;
import java.util.logging.Logger;
import java.util.zip.*;

/**
 * Used to create PDF objects that represent PDF documents.
 */
final public class PDF {
    Compliance compliance;
    Bookmark toc = null;
    List<Font> fonts = new ArrayList<Font>();
    List<Image> images = new ArrayList<Image>();
    List<OptionalContentGroup> groups = new ArrayList<OptionalContentGroup>();
    Map<String, Integer> states = new LinkedHashMap<String, Integer>();
    List<Stamp> stamps = new ArrayList<Stamp>();
    List<StructElem> structElements = new ArrayList<StructElem>();
    Encryption encryption = null;

    private int metadataObjNumber = 0;
    private int outputIntentObjNumber = 0;
    private final List<Page> pages = new ArrayList<Page>();
    private final Map<String, Destination> destinations = new HashMap<String, Destination>();
    private OutputStream os = null;
    private final List<Integer> objOffset = new ArrayList<Integer>();
    private final String producer = "PDFjet v8.7.0";
    private String title;
    private String author;
    private String subject;
    private String keywords;
    private String creator;
    private String createDate;      // XMP metadata
    private int byteCount = 0;
    private int pagesObjNumber = 0;
    private String pageLayout = null;
    private String pageMode = null;
    private String language = "en-US";
    private String uuid = (new Salsa20()).getID();
    private final List<String> importedFonts = new ArrayList<String>();
    private final List<String> importedXObjects = new ArrayList<String>();
    private String extGState = "";
    private Page prevPage = null;
    private boolean contentStreamsCompression = true;

    static final Logger LOG = Logger.getLogger(PDF.class.getName());

    /**
     * The default constructor - use when reading PDF files.
     */
    public PDF() {
    }

    /**
     *  Creates a PDF object that represents a PDF document.
     *
     *  @param os the associated output stream.
     *  @throws Exception if an input or output exception occurred
     */
    public PDF(OutputStream os) throws Exception { this(os, Compliance.PDF_17); }

    // Here is the layout of the PDF document:
    //
    // Metadata Object
    // Output Intent Object
    // Fonts
    // Images
    // Resources Object
    // Content1
    // Content2
    // ...
    // ContentN
    // Annot1
    // Annot2
    // ...
    // AnnotN
    // Page1
    // Page2
    // ...
    // PageN
    // Pages
    // StructElem1
    // StructElem2
    // ...
    // StructElemN
    // StructTreeRoot
    // Root
    // xref table
    // Trailer
    /**
     *  Creates a PDF object that represents a PDF document.
     *  Use this constructor to create PDF/A compliant PDF documents.
     *  Please note: PDF/A compliance requires all fonts to be embedded in the PDF.
     *
     *  @param os the associated output stream.
     *  @param compliance must be: Compliance.PDF_UA_1 or Compliance.PDF_A_1A to Compliance.PDF_A_3B
     *  @throws Exception  If an input or output exception occurred
     */
    public PDF(OutputStream os, Compliance compliance) throws Exception {
        this.os = os;
        this.compliance = compliance;

        Date date = new Date();
        SimpleDateFormat sdf1 = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
        createDate = sdf1.format(date);     // XMP metadata

        append("%PDF-1.7\n");
        append('%');
        append((byte) 0xF2);
        append((byte) 0xF3);
        append((byte) 0xF4);
        append((byte) 0xF5);
        append((byte) 0xF6);
        append(Token.NEWLINE);
    }

    /**
     * Sets the PDF document compliance.
     *
     * @param compliance the compliance level.
     * @return this PDF object.
     */
    public PDF setCompliance(Compliance compliance) {
        this.compliance = compliance;
        return this;
    }

    /**
     * Sets the encryption applied to this document.
     *
     * @param encryption the encryption.
     * @return this PDF object.
     */
    public PDF setEncryption(Encryption encryption) {
        this.encryption = encryption;
        return this;
    }

    /**
     * Starts a new object in the document output and records its offset.
     *
     * @throws IOException if writing to the output fails.
     */
    public void newobj() throws IOException {
        objOffset.add(byteCount);
        append(objOffset.size());
        append(Token.NEW_OBJ);
    }

    /**
     * Ends the current object in the document output.
     *
     * @throws IOException if writing to the output fails.
     */
    public void endobj() throws IOException {
        append(Token.END_OBJ);
    }

    /**
     * Returns the number of the most recently started object.
     *
     * @return the object number.
     */
    public int getObjNumber() {
        return objOffset.size();
    }

    /**
     * Records the offset of an object that carries its own number, growing the
     * table with placeholders for any number that has no object yet.
     */
    private void setObjOffset(int number, int offset) {
        if (number <= 0) {          // No number of its own - just append.
            objOffset.add(offset);
            return;
        }
        while (objOffset.size() < number) {
            objOffset.add(0);
        }
        objOffset.set(number - 1, offset);
    }

    int addMetadataObject(String notice, boolean fontMetadataObject) throws Exception {
        StringBuilder sb = new StringBuilder();
        sb.append("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"\n");
        sb.append("    x:xmptk=\"Adobe XMP Core 5.4-c005 78.147326, 2012/08/23-13:03:03\">\n");
        sb.append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");

        if (fontMetadataObject) {
            sb.append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n");
            sb.append("<xmpRights:UsageTerms>\n");
            sb.append("<rdf:Alt>\n");
            sb.append("<rdf:li xml:lang=\"x-default\">\n");
            sb.append(notice);
            sb.append("</rdf:li>\n");
            sb.append("</rdf:Alt>\n");
            sb.append("</xmpRights:UsageTerms>\n");
            sb.append("</rdf:Description>\n");
        } else {
            sb.append("<rdf:Description rdf:about=\"\"\n");
            sb.append("    xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n");
            sb.append("    xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
            sb.append("    xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
            sb.append("    xmlns:xapMM=\"http://ns.adobe.com/xap/1.0/mm/\"\n");
            sb.append("    xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n");
            sb.append("    xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\">\n");

            sb.append("    <dc:format>application/pdf</dc:format>\n");
            if (compliance == Compliance.PDF_UA_1) {
                sb.append("  <pdfuaid:part>1</pdfuaid:part>\n");
            } else  if (compliance == Compliance.PDF_A_1A) {
                sb.append("  <pdfaid:part>1</pdfaid:part>\n");
                sb.append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_1B) {
                sb.append("  <pdfaid:part>1</pdfaid:part>\n");
                sb.append("  <pdfaid:conformance>B</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_2A) {
                sb.append("  <pdfaid:part>2</pdfaid:part>\n");
                sb.append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_2B) {
                sb.append("  <pdfaid:part>2</pdfaid:part>\n");
                sb.append("  <pdfaid:conformance>B</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_3A) {
                sb.append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_3B) {
                sb.append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.append("  <pdfaid:conformance>B</pdfaid:conformance>\n");
            }

            sb.append("  <pdf:Producer>");
            sb.append(producer);
            sb.append("</pdf:Producer>\n");

            if (title != null) {
                sb.append("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.append(title);
                sb.append("</rdf:li></rdf:Alt></dc:title>\n");
            }

            if (author != null) {
                sb.append("  <dc:creator><rdf:Seq><rdf:li>");
                sb.append(author);
                sb.append("</rdf:li></rdf:Seq></dc:creator>\n");
            }

            if (subject != null) {
                sb.append("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.append(subject);
                sb.append("</rdf:li></rdf:Alt></dc:description>\n");
            }

            if (keywords != null) {
                sb.append("  <pdf:Keywords>");
                sb.append(keywords);
                sb.append("</pdf:Keywords>\n");
            }

            if (creator != null) {
                sb.append("  <xmp:CreatorTool>");
                sb.append(creator);
                sb.append("</xmp:CreatorTool>\n");
            }

            sb.append("  <xmp:CreateDate>");
            sb.append(createDate + "-05:00");       // Append the time zone.
            sb.append("</xmp:CreateDate>\n");

            sb.append("  <xapMM:DocumentID>uuid:");
            sb.append(uuid);
            sb.append("</xapMM:DocumentID>\n");

            sb.append("  <xapMM:InstanceID>uuid:");
            sb.append(uuid);
            sb.append("</xapMM:InstanceID>\n");

            sb.append("</rdf:Description>\n");
        }

        if (!fontMetadataObject) {
            // Add the recommended 2000 bytes padding
            for (int i = 0; i < 20; i++) {
                for (int j = 0; j < 10; j++) {
                    sb.append("          ");
                }
                sb.append("\n");
            }
        }

        sb.append("</rdf:RDF>\n");
        sb.append("</x:xmpmeta>\n");
        sb.append("<?xpacket end=\"w\"?>");

        byte[] xml = sb.toString().getBytes(StandardCharsets.UTF_8);
        if (encryption != null) {
            xml = AES256.encrypt(xml, encryption.getKey());
        }

        // This is the metadata object
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Metadata\n");
        append("/Subtype /XML\n");
        append("/Length ");
        append(xml.length);
        append(Token.NEWLINE);
        append(Token.END_DICTIONARY);
        append(Token.STREAM);
        append(xml, 0, xml.length);
        append(Token.END_STREAM);
        endobj();

        return getObjNumber();
    }

    private int addOutputIntentObject() throws Exception {
        byte[] profile = ICCBlackScaled.profile;
        if (encryption != null) {
            profile = AES256.encrypt(profile, encryption.getKey());
        }

        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/N 3\n");

        append("/Length ");
        append(profile.length);
        append(Token.NEWLINE);

        append("/Filter /FlateDecode\n");
        append(Token.END_DICTIONARY);
        append(Token.STREAM);
        append(profile, 0, profile.length);
        append(Token.END_STREAM);
        endobj();

        byte[] identifierBytes = "sRGB IEC61966-2.1".getBytes(StandardCharsets.UTF_8);
        if (encryption != null) {
            identifierBytes = AES256.encrypt(identifierBytes, encryption.getKey());
        }
        // OutputIntent object
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /OutputIntent\n");
        append("/S /GTS_PDFA1\n");

        append("/OutputCondition <");
        append(Util.toHexString(identifierBytes));
        append(">\n");

        append("/OutputConditionIdentifier <");
        append(Util.toHexString(identifierBytes));
        append(">\n");

        append("/Info <");
        append(Util.toHexString(identifierBytes));
        append(">\n");

        append("/DestOutputProfile ");
        append(getObjNumber() - 1);
        append(Token.OBJ_REF);
        append(Token.END_DICTIONARY);
        endobj();

        return getObjNumber();
    }

    // Appends a token of a PDF that was read. Each of its characters is a byte
    // of the PDF, so a string or name with bytes of 0x80 or more is copied
    // unchanged, where UTF-8 would write two bytes for each of them.
    private void appendToken(String token) throws IOException {
        append(token.getBytes(StandardCharsets.ISO_8859_1));
    }

    /**
     * Writes the "/Name number 0 R" entries collected from a PDF that was read.
     */
    private void appendImportedEntries(List<String> tokens) throws IOException {
        for (String token : tokens) {
            appendToken(token);
            if (token.equals("R")) {
                append(Token.NEWLINE);
            } else {
                append(Token.SPACE);
            }
        }
    }

    private int addResourcesObject() throws Exception {
        newobj();
        append(Token.BEGIN_DICTIONARY);
        if (!extGState.equals("")) {
            appendToken(extGState);
        }
        if (fonts.size() > 0 || importedFonts.size() > 0) {
            append("/Font\n");
            append(Token.BEGIN_DICTIONARY);
            appendImportedEntries(importedFonts);
            for (Font font : fonts) {
                append("/F");
                append(font.objNumber);
                append(Token.SPACE);
                append(font.objNumber);
                append(Token.OBJ_REF);
            }
            append(Token.END_DICTIONARY);
        }
        if (images.size() > 0 || stamps.size() > 0 || importedXObjects.size() > 0) {
            append("/XObject\n");
            append(Token.BEGIN_DICTIONARY);
            appendImportedEntries(importedXObjects);
            for (Image image : images) {
                append("/Im");
                append(image.objNumber);
                append(Token.SPACE);
                append(image.objNumber);
                append(Token.OBJ_REF);
            }
            for (Stamp stamp : stamps) {
                append("/Fm");
                append(stamp.objNumber);
                append(' ');
                append(stamp.objNumber);
                append(" 0 R\n");
            }
            append(Token.END_DICTIONARY);
        }
        if (groups.size() > 0) {
            append("/Properties\n");
            append(Token.BEGIN_DICTIONARY);
            for (int i = 0; i < groups.size(); i++) {
                OptionalContentGroup ocg = groups.get(i);
                append("/OC");
                append(i + 1);
                append(Token.SPACE);
                append(ocg.objNumber);
                append(Token.OBJ_REF);
            }
            append(Token.END_DICTIONARY);
        }
        // String state = "/CA 0.5 /ca 0.5";
        if (states.size() > 0) {
            append("/ExtGState <<\n");
            for (Map.Entry<String, Integer> entry : states.entrySet()) {
                append("/GS");
                append(entry.getValue());
                append(" <<");
                append(entry.getKey());
                append(Token.END_DICTIONARY);
            }
            append(Token.END_DICTIONARY);
        }
        append(Token.END_DICTIONARY);
        endobj();
        return getObjNumber();
    }

    private void addPagesObject() throws Exception {
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Pages\n");
        append("/Kids [\n");
        for (Page page : pages) {
            if (compliance != Compliance.PDF_17) {
                page.setStructElementsPageObjNumber(page.objNumber);
            }
            append(page.objNumber);
            append(Token.OBJ_REF);
        }
        append("]\n");
        append("/Count ");
        append(pages.size());
        append(Token.NEWLINE);
        append(Token.END_DICTIONARY);
        endobj();
    }

    private int addStructTreeRootObject() throws Exception {
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /StructTreeRoot\n");
        append("/ParentTree ");
        append(getObjNumber() + 1);
        append(" 0 R\n");
        append("/K [\n");
        append(getObjNumber() + 2);
        append(Token.OBJ_REF);
        append("]\n");
        append(Token.END_DICTIONARY);
        endobj();
        return getObjNumber();
    }

    private int addStructDocumentObject(int parent) throws Exception {
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /StructElem\n");
        append("/S /Document\n");
        append("/P ");
        append(parent);
        append(" 0 R\n");
        append("/K [\n");
        for (StructElem structElement : this.structElements) {
            append(structElement.objNumber);
            append(Token.OBJ_REF);
        }
        append("]\n");
        append(Token.END_DICTIONARY);
        endobj();
        return getObjNumber();
    }

    private void addStructElementObjects() throws Exception {
        int structTreeRootObjNumber = getObjNumber() + 1;
        structTreeRootObjNumber += this.structElements.size();

        for (StructElem element : this.structElements) {
            newobj();
            element.objNumber = getObjNumber();
            append("<<\n/Type /StructElem /S /");
            append(element.structure);
            append("\n/P ");
            append(structTreeRootObjNumber + 2);    // Use the document struct as parent!
            append(" 0 R /Pg ");
            append(element.pageObjNumber);
            append(Token.OBJ_REF);

            if (element.annotation != null) {
                append("/K <</Type /OBJR /Obj ");
                append(element.annotation.objNumber);
                append(" 0 R>>\n");
            } else {
                append("/K ");
                append(element.mcid);
                append("\n");
            }

            // The actual text is written only with an alternate description,
            // since a text block and a text box pass the text they draw as the
            // actual text without one.
            boolean hasAltDescription =
                    element.altDescription != null && !element.altDescription.isEmpty();
            boolean hasActualText = hasAltDescription &&
                    element.actualText != null && !element.actualText.isEmpty();
            String language = element.language;
            if ((language == null || language.isEmpty()) && hasAltDescription) {
                language = this.language;
            }

            if (language != null && !language.isEmpty()) {
                byte[] languageBytes = language.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    languageBytes = AES256.encrypt(languageBytes, encryption.getKey());
                }
                append("/Lang <");
                append(Util.toHexString(languageBytes));
                append(">\n");
            }

            if (hasActualText) {
                byte[] actualTextBytes = element.actualText.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    actualTextBytes = AES256.encrypt(actualTextBytes, encryption.getKey());
                }
                append("/ActualText <");
                append(Util.toHexString(actualTextBytes));
                append(">\n");
            }

            if (hasAltDescription) {
                byte[] altDescriptionBytes = element.altDescription.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    altDescriptionBytes = AES256.encrypt(altDescriptionBytes, encryption.getKey());
                }
                append("/Alt <");
                append(Util.toHexString(altDescriptionBytes));
                append(">\n");
            }

            append(">>\n");
            endobj();
        }
    }

    private void addNumsParentTree() throws Exception {
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Nums [\n");
        // The keys must be listed in increasing order, so the page entries -
        // whose keys are the /StructParents values 0 .. pages.size()-1 - come
        // first. Each value is the array of struct elements of that page,
        // indexed by the MCID they were marked with.
        for (int i = 0; i < pages.size(); i++) {
            append(i);
            append(" [");
            for (StructElem element : pages.get(i).structures) {
                if (element.annotation == null) {
                    append(Token.SPACE);
                    append(element.objNumber);
                    append(" 0 R");
                }
            }
            append("]\n");
        }
        // The annotations follow, keyed by the /StructParent values handed out
        // by addAnnotDictionaries(), which continue where the pages left off.
        int structParent = pages.size();
        for (StructElem element : this.structElements) {
            if (element.annotation != null) {
                append(structParent++);
                append(Token.SPACE);
                append(element.objNumber);
                append(Token.OBJ_REF);
            }
        }
        append("]\n");
        append(Token.END_DICTIONARY);
        endobj();
    }

    private int addRootObject(
            int structTreeRootObjNumber, int outlineDictNumber) throws Exception {
        // Add the root object
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Catalog\n");

        if (compliance != Compliance.PDF_17) {
            byte[] languageBytes = this.language.getBytes(java.nio.charset.StandardCharsets.UTF_8);
            if (encryption != null) {
                languageBytes = AES256.encrypt(languageBytes, encryption.getKey());
            }
            append("/Lang <");
            append(Util.toHexString(languageBytes));
            append(">\n");

            append("/StructTreeRoot ");
            append(structTreeRootObjNumber);
            append(Token.OBJ_REF);

            append("/MarkInfo <</Marked true>>\n");
            append("/ViewerPreferences <</DisplayDocTitle true>>\n");
        }

        if (pageLayout != null) {
            append("/PageLayout /");
            append(pageLayout);
            append(Token.NEWLINE);
        }

        if (pageMode != null) {
            append("/PageMode /");
            append(pageMode);
            append(Token.NEWLINE);
        }

        addOCProperties();

        append("/Pages ");
        append(pagesObjNumber);
        append(Token.OBJ_REF);

        if (compliance != Compliance.PDF_17) {
            append("/Metadata ");
            append(metadataObjNumber);
            append(Token.OBJ_REF);

            append("/OutputIntents [");
            append(outputIntentObjNumber);
            append(" 0 R]\n");
        }

        if (outlineDictNumber > 0) {
            append("/Outlines ");
            append(outlineDictNumber);
            append(Token.OBJ_REF);
        }

        append(Token.END_DICTIONARY);
        endobj();
        return getObjNumber();
    }

    private void addPageBox(String boxName, Page page, float[] rect) throws Exception {
        append("/");
        append(boxName);
        append(" [");
        append(rect[0]);
        append(Token.SPACE);
        append(page.height - rect[3]);
        append(Token.SPACE);
        append(rect[2]);
        append(Token.SPACE);
        append(page.height - rect[1]);
        append("]\n");
    }

    private void setDestinationObjNumbers() {
        int numberOfAnnotations = 0;
        for (Page page : pages) {
            numberOfAnnotations += page.annots.size();
        }
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            for (Destination destination : page.destinations) {
                destination.pageObjNumber =
                        getObjNumber() + numberOfAnnotations + i + 1;
                destinations.put(destination.name, destination);
            }
        }
    }

    private void addAllPages(int resObjNumber) throws Exception {
        setDestinationObjNumbers();
        addAnnotDictionaries();

        // Calculate the object number of the Pages object
        pagesObjNumber = getObjNumber() + pages.size() + 1;

        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            // Page object
            newobj();
            page.objNumber = getObjNumber();
            append(Token.BEGIN_DICTIONARY);
            append("/Type /Page\n");
            append("/Parent ");
            append(pagesObjNumber);
            append(Token.OBJ_REF);
            append("/MediaBox [0 0 ");
            append(page.width);
            append(Token.SPACE);
            append(page.height);
            append("]\n");

            if (page.rotateDegrees != 0f) {
                append("/Rotate ");
                append(page.rotateDegrees);
                append("\n");
            }

            if (page.cropBox != null) {
                addPageBox("CropBox", page, page.cropBox);
            }
            if (page.bleedBox != null) {
                addPageBox("BleedBox", page, page.bleedBox);
            }
            if (page.trimBox != null) {
                addPageBox("TrimBox", page, page.trimBox);
            }
            if (page.artBox != null) {
                addPageBox("ArtBox", page, page.artBox);
            }

            append("/Resources ");
            append(resObjNumber);
            append(Token.OBJ_REF);

            append("/Contents [ ");
            for (Integer n : page.contents) {
                append(n);
                append(" 0 R ");
            }
            append("]\n");
            if (page.annots.size() > 0) {
                append("/Annots [ ");
                for (Annotation annot : page.annots) {
                    append(annot.objNumber);
                    append(" 0 R ");
                }
                append("]\n");
            }

            if (compliance != Compliance.PDF_17) {
                append("/Tabs /S\n");
                append("/StructParents ");
                append(i);
                append(Token.NEWLINE);
            }

            append(Token.END_DICTIONARY);
            endobj();
        }
    }

    private void addPageContent(Page page) throws Exception {
        if (contentStreamsCompression) {
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            Deflater deflater = new Deflater();
            DeflaterOutputStream dos = new DeflaterOutputStream(baos, deflater);
            byte[] buf = page.buf.toByteArray();
            dos.write(buf, 0, buf.length);
            dos.finish();
            deflater.end();
            page.buf = null;    // Release the page content memory!

            buf = baos.toByteArray();
            if (encryption != null) {
                buf = AES256.encrypt(buf, encryption.getKey());
            }

            newobj();
            append(Token.BEGIN_DICTIONARY);
            append("/Filter /FlateDecode\n");
            append(Token.LENGTH);
            append(buf.length);
            append(Token.NEWLINE);
            append(Token.END_DICTIONARY);
            append(Token.STREAM);
            append(buf);
            append(Token.END_STREAM);
            endobj();
            page.contents.add(getObjNumber());
        } else {    // No compression. Used for diagnostics
            byte[] buf = page.buf.toByteArray();
            if (encryption != null) {
                buf = AES256.encrypt(buf, encryption.getKey());
            }
            page.buf = null;    // Release the page content memory!

            newobj();
            append(Token.BEGIN_DICTIONARY);
            append(Token.LENGTH);
            append(buf.length);
            append(Token.NEWLINE);
            append(Token.END_DICTIONARY);
            append(Token.STREAM);
            append(buf);
            append(Token.END_STREAM);
            endobj();
            page.contents.add(getObjNumber());
        }
    }

    private int addAnnotationObject(Annotation annot, int index) throws Exception {
        newobj();
        annot.objNumber = getObjNumber();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Annot\n");
        append("/Subtype /");
        append(annot.annotationType);
        append("\n");

        append("/Rect [");
        append(annot.x1);
        append(' ');
        append(annot.y1);
        append(' ');
        append(annot.x2);
        append(' ');
        append(annot.y2);
        append("]\n");
        append("/Border [0 0 0]\n");

        if (annot.annotationType.equals(Annotation.FileAttachment)) {
            append("/FS ");
            append(annot.fileAttachment.embeddedFile.objNumber);
            append(" 0 R\n");
            append("/Name /");
            append(annot.fileAttachment.icon);
            append("\n");

            if (annot.fileAttachment.title != null) {
                byte[] title = annot.fileAttachment.title.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    title = AES256.encrypt(title, encryption.getKey());
                }
                append("/T <");
                append(Util.toHexString(title));
                append(">\n");
            }

            if (annot.fileAttachment.contents != null) {
                byte[] contents = annot.fileAttachment.contents.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    contents = AES256.encrypt(contents, encryption.getKey());
                }
                append("/Contents <");
                append(Util.toHexString(contents));
                append(">\n");
            }
        } else if (annot.annotationType.equals(Annotation.Link)) {
            // PDF/UA requires a link to carry an alternate description in its
            // Contents key.
            String description = annot.contents;
            if (description == null || description.isEmpty()) {
                description = annot.altDescription;
            }
            if (description == null || description.isEmpty()) {
                description = annot.uri;
            }
            if (description == null || description.isEmpty()) {
                description = annot.key;
            }
            if (description != null && !description.isEmpty()) {
                byte[] bytes = description.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    bytes = AES256.encrypt(bytes, encryption.getKey());
                }
                append("/Contents <");
                append(Util.toHexString(bytes));
                append(">\n");
            }
            if (annot.uri != null) {
                append("/F 4\n");
                append("/A <<\n");
                append("/S /URI\n");
                byte[] uri = annot.uri.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    uri = AES256.encrypt(uri, encryption.getKey());
                }
                append("/URI <");
                append(Util.toHexString(uri));
                append(">\n");
                append(">>\n");
            } else if (annot.key != null) {
                Destination destination = destinations.get(annot.key);
                if (destination != null) {
                    append("/F 4\n");
                    append("/Dest [");
                    append(destination.pageObjNumber);
                    append(" 0 R /XYZ ");
                    append(destination.xPosition);
                    append(" ");
                    append(destination.yPosition);
                    append(" 0]\n");
                }
            }
        } else if (annot.annotationType.equals(Annotation.Polygon)) {
            append("/Vertices [ ");
            for (int i = 0; i < annot.vertices.length; i += 2) {
                append(annot.x1 + annot.vertices[i]);
                append(' ');
                append(annot.y1 - annot.vertices[i + 1]);
                append(' ');
            }
            append("]\n");

            append("/IC [");
            append(annot.fillColor[0]);
            append(' ');
            append(annot.fillColor[1]);
            append(' ');
            append(annot.fillColor[2]);
            append("]\n");

            append("/CA ");
            append(annot.transparency);
            append("\n");

            if (annot.title != null) {
                byte[] title = annot.title.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    title = AES256.encrypt(title, encryption.getKey());
                }
                append("/T <");
                append(Util.toHexString(title));
                append(">\n");
            }

            if (annot.contents != null) {
                byte[] contents = annot.contents.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    contents = AES256.encrypt(contents, encryption.getKey());
                }
                append("/Contents <");
                append(Util.toHexString(contents));
                append(">\n");
            }
        } else if (annot.annotationType.equals(Annotation.Square) ||
                annot.annotationType.equals(Annotation.Circle)) {
            append("/IC [");
            append(annot.fillColor[0]);
            append(' ');
            append(annot.fillColor[1]);
            append(' ');
            append(annot.fillColor[2]);
            append("]\n");

            append("/CA ");
            append(annot.transparency);
            append("\n");

            if (annot.title != null) {
                byte[] title = annot.title.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    title = AES256.encrypt(title, encryption.getKey());
                }
                append("/T <");
                append(Util.toHexString(title));
                append(">\n");
            }

            if (annot.contents != null) {
                byte[] contents = annot.contents.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    contents = AES256.encrypt(contents, encryption.getKey());
                }
                append("/Contents <");
                append(Util.toHexString(contents));
                append(">\n");
            }
        } else if (annot.annotationType.equals(Annotation.Text)) {
            append("/Name /Comment\n");

            if (annot.title != null) {
                byte[] title = annot.title.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    title = AES256.encrypt(title, encryption.getKey());
                }
                append("/T <");
                append(Util.toHexString(title));
                append(">\n");
            }

            if (annot.contents != null) {
                byte[] contents = annot.contents.getBytes(StandardCharsets.UTF_8);
                if (encryption != null) {
                    contents = AES256.encrypt(contents, encryption.getKey());
                }
                append("/Contents <");
                append(Util.toHexString(contents));
                append(">\n");
            }
        }

        if (index != -1) {
            append("/StructParent ");
            append(index++);
            append("\n");
        }
        append(Token.END_DICTIONARY);
        endobj();

        return index;
    }

    private void addAnnotDictionaries() throws Exception {
        int index = pages.size();
        for (StructElem element : this.structElements) {
            if (element.annotation != null) {
                index = addAnnotationObject(element.annotation, index);
                element.annotation.structParentWritten = true;
            }
        }

        for (Page page : pages) {
            for (Annotation annotation : page.annots) {
                // Skip the annotations that were already written above -
                // writing them twice would leave the page referencing a copy
                // that has no /StructParent key.
                if (!annotation.structParentWritten) {
                    addAnnotationObject(annotation, -1);
                }
            }
        }
    }

    private void addOCProperties() throws Exception {
        if (!groups.isEmpty()) {
            List<OCG> list = new ArrayList<OCG>();
            StringBuilder buf = new StringBuilder();
            for (OptionalContentGroup ocg : this.groups) {
                buf.append(' ');
                buf.append(ocg.objNumber);
                buf.append(" 0 R");
                list.add(new OCG(ocg.objNumber, ocg.name));
            }
            list.sort((x, y) -> x.name.compareTo(y.name));

            append("/OCProperties\n");
            append(Token.BEGIN_DICTIONARY);
            append("/OCGs [");
            append(buf.toString());
            append(" ]\n");
            append("/D <<\n");

            append("/AS [\n");
            append("<< /Event /View /Category [/View] /OCGs [");
            append(buf.toString());
            append(" ] >>\n");
            append("<< /Event /Print /Category [/Print] /OCGs [");
            append(buf.toString());
            append(" ] >>\n");
            append("<< /Event /Export /Category [/Export] /OCGs [");
            append(buf.toString());
            append(" ] >>\n");
            append("]\n");

            append("/Order [");
            for (OCG ocg : list) {
                append(' ');
                append(ocg.objNumber);
                append(" 0 R ");
            }
            append("]\n");

            append(Token.END_DICTIONARY);
            append(Token.END_DICTIONARY);
        }
    }

    /**
     * Adds the specified page to the PDF.
     *
     * @param page the page.
     * @throws Exception if there is an issue.
     */
    public void addPage(Page page) throws Exception {
        if (page == null) {
            return;
        }
        pages.add(page);
        if (prevPage != null) {
            addPageContent(prevPage);
        }
        prevPage = page;
    }

    /**
     * Adds the pages to this document.
     *
     * @param pages the pages.
     * @throws Exception if an input or output exception occurred.
     */
    public void addPages(List<Page> pages) throws Exception {
        for (Page page : pages) {
            addPage(page);
        }
    }

    /**
     * Completes the construction of the PDF and writes it to the output stream.
     * The output stream is then automatically closed.
     *
     * @throws Exception  If an input or output exception occurred
     */
    public void complete() throws Exception {
        if (prevPage != null) {
            addPageContent(prevPage);
        }
        if (compliance != Compliance.PDF_17) {
            metadataObjNumber = addMetadataObject("", false);
            outputIntentObjNumber = addOutputIntentObject();
        }

        if (pagesObjNumber == 0) {
            addAllPages(addResourcesObject());
            addPagesObject();
        }

        int structTreeRootObjNumber = 0;
        if (compliance != Compliance.PDF_17) {
            addStructElementObjects();
            structTreeRootObjNumber = addStructTreeRootObject();
            addNumsParentTree();
            addStructDocumentObject(structTreeRootObjNumber);
        }

        int outlineDictNum = 0;
        if (toc != null && toc.getChildren() != null) {
            List<Bookmark> list = toc.toArrayList();
            outlineDictNum = addOutlineDict(toc);
            for (int i = 1; i < list.size(); i++) {
                Bookmark bookmark = list.get(i);
                addOutlineItem(outlineDictNum, i, bookmark);
            }
        }

        int rootObjNumber = addRootObject(structTreeRootObjNumber, outlineDictNum);
        int startxref = byteCount;

        // Create the xref table
        append("xref\n");
        append("0 ");
        append(rootObjNumber + 1);
        append('\n');
        append("0000000000 65535 f \n");
        for (int offset : objOffset) {
            if (offset == 0) {      // A number that no object was written for.
                append("0000000000 65535 f \n");
                continue;
            }
            String str = Integer.toString(offset);
            for (int i = 0; i < 10 - str.length(); i++) {
                append('0');
            }
            append(str);
            append(" 00000 n \n");
        }
        append("trailer\n");
        append(Token.BEGIN_DICTIONARY);
        append("/Size ");
        append(rootObjNumber + 1);
        append('\n');

        append("/ID[<");
        append(uuid);
        append("><");
        append(uuid);
        append(">]\n");

        if (encryption != null) {
            append("/Encrypt ");
            append(encryption.getObjNumber());
            append(" 0 R\n");
        }

        append("/Root ");
        append(rootObjNumber);
        append(" 0 R\n");

        append(Token.END_DICTIONARY);
        append("startxref\n");
        append(startxref);
        append('\n');
        append("%%EOF\n");

        os.close();
    }

    /**
     *  Set the "Language" document property of the PDF file.
     *  @param language The language of this document.
     *  @return this PDF object.
     */
    public PDF setLanguage(String language) {
        this.language = language;
        return this;
    }

    /**
     *  Set the "Title" document property of the PDF file.
     *  @param title The title of this document.
     *  @return this PDF object.
     */
    public PDF setTitle(String title) {
        this.title = title;
        return this;
    }

    /**
     *  Set the "Author" document property of the PDF file.
     *  @param author The author of this document.
     *  @return this PDF object.
     */
    public PDF setAuthor(String author) {
        this.author = author;
        return this;
    }

    /**
     *  Set the "Subject" document property of the PDF file.
     *  @param subject The subject of this document.
     *  @return this PDF object.
     */
    public PDF setSubject(String subject) {
        this.subject = subject;
        return this;
    }

    /**
     * Sets the PDF keywords.
     *
     * @param keywords the keywords.
     * @return this PDF object.
     */
    public PDF setKeywords(String keywords) {
        this.keywords = keywords;
        return this;
    }

    /**
     * Sets the PDF creator.
     *
     * @param creator the creator.
     * @return this PDF object.
     */
    public PDF setCreator(String creator) {
        this.creator = creator;
        return this;
    }

    /**
     * Sets the PDF page layout.
     *
     * @param pageLayout the page layout.
     * @return this PDF object.
     */
    public PDF setPageLayout(String pageLayout) {
        this.pageLayout = pageLayout;
        return this;
    }

    /**
     * Set the PDF page mode.
     *
     * @param pageMode the page mode.
     * @return this PDF object.
     */
    public PDF setPageMode(String pageMode) {
        this.pageMode = pageMode;
        return this;
    }

    /**
     * Writes the number to the document output.
     *
     * @param num the number.
     * @throws IOException if writing to the output fails.
     */
    public void append(int num) throws IOException {
        append(Integer.toString(num));
    }

    /**
     * Writes the number to the document output.
     *
     * @param f the number.
     * @throws IOException if writing to the output fails.
     */
    public void append(float f) throws IOException {
        append(FastFloat.toByteArray(f));
    }

    /**
     * Writes the string to the document output as UTF-8.
     *
     * @param str the string.
     * @throws IOException if writing to the output fails.
     */
    public void append(String str) throws IOException {
        byte[] buf = str.getBytes(StandardCharsets.UTF_8);
        os.write(buf);
        byteCount += buf.length;
    }

    /**
     * Writes the character to the document output as a single byte.
     *
     * @param ch the character.
     * @throws IOException if writing to the output fails.
     */
    public void append(char ch) throws IOException {
        os.write((byte) ch);
        byteCount += 1;
    }

    /**
     * Writes the byte to the document output.
     *
     * @param b the byte.
     * @throws IOException if writing to the output fails.
     */
    public void append(byte b) throws IOException {
        os.write(b);
        byteCount += 1;
    }

    /**
     * Writes the bytes to the document output.
     *
     * @param buf the bytes.
     * @throws IOException if writing to the output fails.
     */
    public void append(byte[] buf) throws IOException {
        os.write(buf, 0, buf.length);
        byteCount += buf.length;
    }

    /**
     * Writes part of the byte array to the document output.
     *
     * @param buf the bytes.
     * @param off the offset of the first byte to write.
     * @param len the number of bytes to write.
     * @throws IOException if writing to the output fails.
     */
    public void append(byte[] buf, int off, int len) throws IOException {
        os.write(buf, off, len);
        byteCount += len;
    }

    /**
     * Writes the contents of the stream to the document output.
     *
     * @param baos the stream.
     * @throws IOException if writing to the output fails.
     */
    public void append(ByteArrayOutputStream baos) throws IOException {
        baos.writeTo(os);
        byteCount += baos.size();
    }

    private List<PDFobj> getSortedObjects(List<PDFobj> objects) {
        List<PDFobj> sorted = new ArrayList<PDFobj>();

        int maxObjNumber = 0;
        for (PDFobj obj : objects) {
            if (obj.number > maxObjNumber) {
                maxObjNumber = obj.number;
            }
        }

        for (int number = 1; number <= maxObjNumber; number++) {
            PDFobj obj = new PDFobj();
            obj.setNumber(number);
            sorted.add(obj);
        }

        for (PDFobj obj : objects) {
            sorted.set(obj.number - 1, obj);
        }

        return sorted;
    }

    /**
     *  Returns a list of objects of type PDFobj read from input stream.
     *  An encrypted PDF is decrypted when it opens without a password.
     *
     *  @param inputStream the PDF input stream.
     *
     *  @return the list of PDF objects.
     *  @throws Exception  If an input or output exception occurred
     */
    public List<PDFobj> read(InputStream inputStream) throws Exception {
        byte[] buf = Content.getFromStream(inputStream);

        List<PDFobj> objects1 = new ArrayList<PDFobj>();
        PDFobj trailer = null;
        try {
            trailer = getObjects(buf, getStartXRef(buf), objects1, 0);
        } catch (Exception e) {
            trailer = null;     // A cross-reference stream that cannot be decoded.
        }
        if (trailer == null || objects1.isEmpty()) {
            // The cross-reference table is missing or wrong, like in a PDF
            // that was changed without updating it.
            objects1.clear();
            trailer = getObjectsByScanning(buf, objects1);
        }
        Decryptor decryptor = Decryptor.getDecryptor(trailer, objects1);

        List<PDFobj> objects2 = new ArrayList<PDFobj>();
        for (PDFobj obj : objects1) {
            String type = obj.getValue("/Type");
            if (type.equals("/XRef")) {
                continue;       // Skip the cross-reference streams.
            }
            if (decryptor != null) {
                if (obj.number == decryptor.objNumber) {
                    continue;   // Skip the encryption dictionary.
                }
                decryptor.decryptStrings(obj);
            }
            if (obj.dict.contains("stream")) {
                obj.setStreamAndData(buf, obj.getLength(objects1), decryptor);
            }

            if (type.equals("/ObjStm")) {
                int first = Integer.parseInt(obj.getValue("/First"));
                PDFobj o2 = getObject(obj.data, 0, first);
                int count = o2.dict.size();
                for (int i = 0; i < count; i += 2) {
                    String num = o2.dict.get(i);
                    int off = Integer.parseInt(o2.dict.get(i + 1));
                    int end = obj.data.length;
                    if (i <= count - 4) {
                        end = first + Integer.parseInt(o2.dict.get(i + 3));
                    }
                    PDFobj o3 = getObject(obj.data, first + off, end);
                    o3.setNumber(Integer.parseInt(num));
                    o3.dict.add(0, "obj");
                    o3.dict.add(0, "0");
                    o3.dict.add(0, num);
                    objects2.add(o3);
                }
            } else {
                objects2.add(obj);
            }
        }

        return getSortedObjects(objects2);
    }

    private boolean process(
            PDFobj obj, StringBuilder sb1, byte[] buf, int off) {
        String str = sb1.toString().trim();
        if (!str.isEmpty()) {
            obj.dict.add(str);
        }
        sb1.setLength(0);
        if (str.equals("endobj")) {
            return true;
        } else if (str.equals("stream")) {
            obj.streamOffset = off;
            if (off < buf.length && buf[off] == '\n') {
                obj.streamOffset += 1;
            }
            return true;
        } else if (str.equals("startxref")) {
            return true;
        }
        return false;
    }

    private PDFobj getObject(byte[] buf, int off) {
        if (off < 0 || off >= buf.length) {
            return new PDFobj();    // An offset outside of the PDF has no tokens.
        }
        return getObject(buf, off, buf.length);
    }

    private PDFobj getObject(byte[] buf, int off, int len) {
        PDFobj obj = new PDFobj();
        obj.offset = off;
        StringBuilder token = new StringBuilder();

        int p = 0;          // The nesting level of the parentheses in a literal string
        boolean done = false;
        while (!done && off < len) {
            char c2 = (char) (buf[off++] & 0xff);
            if (p > 0) {
                // A literal string is one token, with its white space and
                // delimiters. A backslash escapes the character after it.
                token.append(c2);
                if (c2 == '\\') {
                    if (off < len) {
                        token.append((char) (buf[off++] & 0xff));
                    }
                } else if (c2 == '(') {
                    ++p;
                } else if (c2 == ')') {
                    --p;
                    if (p == 0) {
                        done = process(obj, token, buf, off);
                    }
                }
            } else if (c2 == '(') {
                done = process(obj, token, buf, off);
                if (!done) {
                    token.append(c2);
                    p = 1;
                }
            } else if (isWhiteSpace(c2)) {
                done = process(obj, token, buf, off);
            } else if (c2 == '/') {
                done = process(obj, token, buf, off);
                if (!done) {
                    token.append(c2);
                }
            } else if (c2 == '<' || c2 == '>') {
                done = process(obj, token, buf, off);
                if (!done) {
                    if (off < len && buf[off] == c2) {
                        obj.dict.add(c2 == '<' ? "<<" : ">>");
                        off++;
                    } else if (c2 == '<') {
                        // A hexadecimal string is one token, without its white space.
                        token.append(c2);
                        while (off < len && buf[off] != '>') {
                            char c = (char) (buf[off++] & 0xff);
                            if (!isWhiteSpace(c)) {
                                token.append(c);
                            }
                        }
                        token.append('>');
                        off++;
                        done = process(obj, token, buf, off);
                    } else {
                        obj.dict.add(">");
                    }
                }
            } else if (c2 == '%') {
                // A comment ends at the end of the line.
                done = process(obj, token, buf, off);
                while (!done && off < len && buf[off] != '\n' && buf[off] != '\r') {
                    off++;
                }
            } else if (c2 == '[' || c2 == ']' || c2 == '{' || c2 == '}') {
                done = process(obj, token, buf, off);
                if (!done) {
                    obj.dict.add(String.valueOf(c2));
                }
            } else {
                token.append(c2);
            }
        }
        if (!done) {
            process(obj, token, buf, off);  // The last token, at the end of the data.
        }

        return obj;
    }

    private static boolean isWhiteSpace(int c) {
        return c == 0x00        // Null
            || c == 0x09        // Horizontal Tab
            || c == 0x0A        // Line Feed (LF)
            || c == 0x0C        // Form Feed
            || c == 0x0D        // Carriage Return (CR)
            || c == 0x20;       // Space
    }

    /**
     * Converts an array of bytes to an integer.
     * @param buf byte[]
     * @return int
     */
    private int toInt(byte[] buf, int off, int len) {
        int i = 0;
        for (int j = 0; j < len; j++) {
            i |= buf[off + j] & 0xFF;
            if (j < len - 1) {
                i <<= 8;
            }
        }
        return i;
    }

    // Returns the value of the token, or -1 when it is not an integer.
    private static int toInteger(String token) {
        try {
            return isInteger(token) ? Integer.parseInt(token) : -1;
        } catch (NumberFormatException e) {
            return -1;
        }
    }

    // Returns true when the tokens of the object start with "number
    // generation obj", where the number is above 0, and is the number
    // that is given unless that is -1.
    private static boolean isObject(PDFobj obj, int number) {
        if (obj.dict.size() < 3 || !obj.dict.get(2).equals("obj") || !isInteger(obj.dict.get(1))) {
            return false;
        }
        int n = toInteger(obj.dict.get(0));
        return n > 0 && (number == -1 || n == number);
    }

    /**
     * Adds the objects of the cross-reference section at the offset to the
     * list, after the objects of the sections before it, so that the newest
     * version of an object that was updated comes last. A section is a
     * cross-reference table, which can have an /XRefStm stream for the
     * objects in object streams, or a cross-reference stream.
     *
     * @return the trailer of the section, which is the cross-reference
     *     stream object when there is no table, or null when an offset in
     *     the section is not that of its object.
     */
    private PDFobj getObjects(
            byte[] buf, int offset, List<PDFobj> objects, int depth) throws Exception {
        PDFobj xref = getObject(buf, offset);
        boolean table = !xref.dict.isEmpty() && xref.dict.get(0).equals("xref");
        if (depth > 1000 || (!table && !isObject(xref, -1))) {
            return null;
        }
        String prev = xref.getValue("/Prev");
        if (!prev.isEmpty() && getObjects(buf, toInteger(prev), objects, depth + 1) == null) {
            return null;
        }
        if (table) {
            // The objects in the table replace those in the /XRefStm stream.
            String xrefStm = xref.getValue("/XRefStm");
            if (!xrefStm.isEmpty() &&
                    !getStreamObjects(buf, getObject(buf, toInteger(xrefStm)), objects)) {
                return null;
            }
            if (!getTableObjects(buf, xref, objects)) {
                return null;
            }
        } else if (!getStreamObjects(buf, xref, objects)) {
            return null;
        }
        return xref;
    }

    // Adds the objects in use of a cross-reference table, and returns false
    // when an offset is not that of its object.
    private boolean getTableObjects(byte[] buf, PDFobj xref, List<PDFobj> objects) {
        List<String> dict = xref.dict;
        int i = 1;
        // Each subsection starts with its first object number and the number of entries.
        while (i + 1 < dict.size() && isInteger(dict.get(i))) {
            int number = toInteger(dict.get(i));
            int count = toInteger(dict.get(i + 1));
            i += 2;
            for (int j = 0; j < count; j++, number++, i += 3) {
                if (i + 2 >= dict.size()) {
                    return false;
                }
                // The entry is the offset, the generation number and n for an object in use.
                if (dict.get(i + 2).equals("n")) {
                    PDFobj obj = getObject(buf, toInteger(dict.get(i)));
                    if (!isObject(obj, number)) {
                        return false;
                    }
                    obj.number = number;
                    objects.add(obj);
                }
            }
        }
        return i < dict.size() && dict.get(i).equals("trailer");
    }

    // Adds the objects of a cross-reference stream that are not in object
    // streams, and returns false when an offset is not that of its object.
    private boolean getStreamObjects(
            byte[] buf, PDFobj xref, List<PDFobj> objects) throws Exception {
        if (!isObject(xref, -1) || !xref.getValue("/Type").equals("/XRef") ||
                !xref.dict.contains("stream")) {
            return false;
        }
        // See page 50 in PDF32000_2008.pdf
        List<String> dict = xref.dict;
        int w = dict.indexOf("/W");
        if (w == -1 || w + 4 >= dict.size()) {
            return false;
        }
        int n1 = toInteger(dict.get(w + 2));    // Field 1 number of bytes
        int n2 = toInteger(dict.get(w + 3));    // Field 2 number of bytes
        int n3 = toInteger(dict.get(w + 4));    // Field 3 number of bytes
        int length = toInteger(xref.getValue("/Length"));
        if (n1 < 0 || n2 < 0 || n3 < 0 || n1 + n2 + n3 == 0 ||
                length < 0 || xref.streamOffset + length > buf.length) {
            return false;
        }
        // The /Index array has the first object number and the number of
        // entries of each subsection, and is [0 /Size] when it is missing.
        List<Integer> index = new ArrayList<Integer>();
        int k = dict.indexOf("/Index");
        if (k != -1 && k + 1 < dict.size() && dict.get(k + 1).equals("[")) {
            for (k += 2; k + 1 < dict.size() && isInteger(dict.get(k)); k += 2) {
                index.add(toInteger(dict.get(k)));
                index.add(toInteger(dict.get(k + 1)));
            }
        } else {
            index.add(0);
            index.add(toInteger(xref.getValue("/Size")));
        }

        // setStreamAndData undoes the predictor, so each entry is a row of the data.
        xref.setStreamAndData(buf, length);
        int n = n1 + n2 + n3;   // Number of bytes per entry
        int offset = 0;
        for (int s = 0; s + 1 < index.size(); s += 2) {
            int number = index.get(s);
            for (int j = 0; j < index.get(s + 1) && offset + n <= xref.data.length; j++) {
                // Process the entries in a cross-reference stream.
                // Page 51 in PDF32000_2008.pdf
                int type = (n1 == 0) ? 1 : toInt(xref.data, offset, n1);
                if (type == 1) {
                    PDFobj obj = getObject(buf, toInt(xref.data, offset + n1, n2));
                    if (!isObject(obj, number)) {
                        return false;
                    }
                    obj.number = number;
                    objects.add(obj);
                }
                number++;
                offset += n;
            }
        }
        return true;
    }

    /**
     * Adds the objects of the PDF to the list by looking for "number
     * generation obj" in it, for when the cross-reference table is missing
     * or wrong. The objects of incremental updates are later in the PDF, so
     * the newest version of an object comes last.
     *
     * @return the last trailer, or the last cross-reference stream object
     *     when there is no trailer, or null when there is neither.
     */
    private PDFobj getObjectsByScanning(byte[] buf, List<PDFobj> objects) {
        PDFobj trailer = null;
        PDFobj xrefStream = null;
        int i = 0;
        while (i < buf.length) {
            if (isObjectStart(buf, i)) {
                PDFobj obj = getObject(buf, i);
                if (isObject(obj, -1)) {
                    obj.number = toInteger(obj.dict.get(0));
                    objects.add(obj);
                    if (obj.getValue("/Type").equals("/XRef")) {
                        xrefStream = obj;
                    }
                    if (obj.dict.contains("stream")) {
                        // Skip the stream, as its bytes can look like an object.
                        int end = indexOf(buf, "endstream", obj.streamOffset);
                        i = (end == -1) ? buf.length : end;
                        continue;
                    }
                }
            } else if (startsWith(buf, i, "trailer")) {
                trailer = getObject(buf, i);
            }
            i++;
        }
        return (trailer != null) ? trailer : xrefStream;
    }

    // Returns true when "number generation obj" starts at the offset, after
    // white space or at the start of the PDF.
    private static boolean isObjectStart(byte[] buf, int off) {
        if (off > 0 && !isWhiteSpace(buf[off - 1])) {
            return false;
        }
        int i = off;
        while (i < buf.length && buf[i] >= '0' && buf[i] <= '9') {
            i++;
        }
        int j = i;
        while (j < buf.length && isWhiteSpace(buf[j])) {
            j++;
        }
        int k = j;
        while (k < buf.length && buf[k] >= '0' && buf[k] <= '9') {
            k++;
        }
        int m = k;
        while (m < buf.length && isWhiteSpace(buf[m])) {
            m++;
        }
        return i > off && j > i && k > j && m > k && startsWith(buf, m, "obj");
    }

    private static boolean startsWith(byte[] buf, int off, String str) {
        if (off + str.length() > buf.length) {
            return false;
        }
        for (int i = 0; i < str.length(); i++) {
            if (buf[off + i] != str.charAt(i)) {
                return false;
            }
        }
        return true;
    }

    private static int indexOf(byte[] buf, String str, int from) {
        for (int i = Math.max(from, 0); i + str.length() <= buf.length; i++) {
            if (startsWith(buf, i, str)) {
                return i;
            }
        }
        return -1;
    }

    // Returns the offset after the last startxref, or -1 when there is none.
    private int getStartXRef(byte[] buf) {
        for (int i = buf.length - 9; i >= 0; i--) {
            if (startsWith(buf, i, "startxref")) {
                int j = i + 9;
                while (j < buf.length && isWhiteSpace(buf[j])) {
                    j++;
                }
                long offset = 0;
                int k = j;
                while (k < buf.length && buf[k] >= '0' && buf[k] <= '9' && offset <= Integer.MAX_VALUE) {
                    offset = offset * 10 + (buf[k] - '0');
                    k++;
                }
                return (k > j && offset <= Integer.MAX_VALUE) ? (int) offset : -1;
            }
        }
        return -1;
    }

    /**
     * Adds outline dictionary to the PDF.
     *
     * @param toc the bookmark table of contents.
     * @return the number of children.
     * @throws Exception if there is an issue.
     */
    public int addOutlineDict(Bookmark toc) throws Exception {
        int numOfChildren = getNumOfChildren(0, toc);
        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Outlines\n");
        append("/First ");
        append(getObjNumber() + 1);
        append(" 0 R\n");
        append("/Last ");
        append(getObjNumber() + numOfChildren);
        append(" 0 R\n");
        append("/Count ");
        append(numOfChildren);
        append("\n");
        append(Token.END_DICTIONARY);
        endobj();
        return getObjNumber();
    }

    /**
     * Adds an outline item to the bookmark.
     *
     * @param parent the parent number.
     * @param i the item number.
     * @param bm1 the bookmark.
     * @throws Exception if there is an issue.
     */
    public void addOutlineItem(int parent, int i, Bookmark bm1) throws Exception {
        int prev = (bm1.getPrevBookmark() == null) ? 0 : parent + (i - 1);
        int next = (bm1.getNextBookmark() == null) ? 0 : parent + (i + 1);

        int first = 0;
        int last  = 0;
        int count = 0;
        if (bm1.getChildren() != null && bm1.getChildren().size() > 0) {
            first = parent + bm1.getFirstChild().objNumber;
            last  = parent + bm1.getLastChild().objNumber;
            count = (-1) * getNumOfChildren(0, bm1);
        }

        byte[] title = bm1.getTitle().getBytes(java.nio.charset.StandardCharsets.UTF_8);
        if (encryption != null) {
            title = AES256.encrypt(title, encryption.getKey());
        }

        newobj();
        append(Token.BEGIN_DICTIONARY);
        append("/Title <");
        append(Util.toHexString(title));
        append(">\n");
        append("/Parent ");
        append(parent);
        append(" 0 R\n");
        if (prev > 0) {
            append("/Prev ");
            append(prev);
            append(" 0 R\n");
        }
        if (next > 0) {
            append("/Next ");
            append(next);
            append(" 0 R\n");
        }
        if (first > 0) {
            append("/First ");
            append(first);
            append(" 0 R\n");
        }
        if (last > 0) {
            append("/Last ");
            append(last);
            append(" 0 R\n");
        }
        if (count != 0) {
            append("/Count ");
            append(count);
            append("\n");
        }
        append("/F 4\n");       // No Zoom
        append("/Dest [");
        append(bm1.getDestination().pageObjNumber);
        append(" 0 R /XYZ ");
        append(bm1.getDestination().xPosition);
        append(" ");
        append(bm1.getDestination().yPosition);
        append(" 0]\n");
        append(Token.END_DICTIONARY);
        endobj();
    }

    private int getNumOfChildren(int numOfChildren, Bookmark bm1) {
        List<Bookmark> children = bm1.getChildren();
        if (children != null) {
            for (Bookmark bm2 : children) {
                numOfChildren = getNumOfChildren(++numOfChildren, bm2);
            }
        }
        return numOfChildren;
    }

    /**
     * Adds the specified objects to the PDF.
     *
     * @param objects the objects.
     * @throws Exception if there is an issue.
     */
    public void addObjects(List<PDFobj> objects) throws Exception {
        this.pagesObjNumber = Integer.parseInt(getPagesObject(objects).dict.get(0));
        addObjectsToPDF(objects);
    }

    /**
     * Returns the pages object.
     *
     * @param objects the page objects.
     * @return the pages object.
     */
    public PDFobj getPagesObject(List<PDFobj> objects) {
        for (PDFobj obj : objects) {
            if (obj.getValue("/Type").equals("/Pages") && obj.getValue("/Parent").equals("")) {
                return obj;
            }
        }
        return null;
    }

    /**
     * Returns the page objects.
     *
     * @param objects the objects.
     * @return the page objects.
     */
    public List<PDFobj> getPageObjects(List<PDFobj> objects) {
        List<PDFobj> pages = new ArrayList<PDFobj>();
        getPageObjects(getPagesObject(objects), objects, pages);
        return pages;
    }

    private void getPageObjects(
            PDFobj pdfObj,
            List<PDFobj> objects,
            List<PDFobj> pages) {
        List<Integer> kids = pdfObj.getObjectNumbers("/Kids");
        for (Integer number : kids) {
            PDFobj obj =  objects.get(number - 1);
            if (isPageObject(obj)) {
                pages.add(obj);
            } else {
                getPageObjects(obj, objects, pages);
            }
        }
    }

    private boolean isPageObject(PDFobj obj) {
        for (int i = 0; i < obj.dict.size() - 1; i++) {
            if (obj.dict.get(i).equals("/Type") &&
                    obj.dict.get(i + 1).equals("/Page")) {
                return true;
            }
        }
        return false;
    }

    private String getExtGState(PDFobj resources) {
        StringBuilder buf = new StringBuilder();
        List<String> dict = resources.getDict();
        int level = 0;
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/ExtGState")) {
                buf.append("/ExtGState << ");
                ++i;
                ++level;
                while (level > 0) {
                    String token = dict.get(++i);
                    if (token.equals("<<")) {
                        ++level;
                    } else if (token.equals(">>")) {
                        --level;
                    }
                    buf.append(token);
                    if (level > 0) {
                        // Token.SPACE and Token.NEWLINE are bytes, and appending
                        // a byte to a StringBuilder writes its number, not the
                        // character. The other ports already use literals here.
                        buf.append(' ');
                    } else {
                        buf.append('\n');
                    }
                }
                break;
            }
        }
        return buf.toString();
    }

    private List<PDFobj> getFontObjects(PDFobj resources, List<PDFobj> objects) {
        List<PDFobj> fonts = new ArrayList<PDFobj>();

        List<String> dict = resources.getDict();
        int i = 0;
        while (i < dict.size() && !dict.get(i).equals("/Font")) {
            i += 1;
        }
        i += 2;     // Skip over "/Font" and the "<<" that follows it.

        // The sub-dictionary holds one "/Name <number> 0 R" entry per font.
        // Every one of them is re-emitted in the resources object, so every
        // one of them has to be collected here - taking only the first left
        // the rest of the references dangling.
        while (i < dict.size() && !dict.get(i).equals(">>")) {
            String token = dict.get(i);
            if (token.startsWith("/") && (i + 3) < dict.size()
                    && dict.get(i + 3).equals("R")) {
                // Pages can carry separate resource dictionaries that name the
                // same fonts. They are merged into one /Font dictionary here,
                // so a name that is already present must not be added twice.
                if (importedFonts.contains(token)) {
                    i += 4;
                    continue;
                }
                importedFonts.add(token);
                importedFonts.add(dict.get(i + 1));
                importedFonts.add(dict.get(i + 2));
                importedFonts.add(dict.get(i + 3));
                int number = Integer.parseInt(dict.get(i + 1));
                if (number > 0 && number <= objects.size()) {
                    fonts.add(objects.get(number - 1));
                }
                i += 4;
                continue;
            }
            importedFonts.add(token);
            i += 1;
        }

        if (fonts.isEmpty()) {
            return null;
        }
        return fonts;
    }

    private List<PDFobj> getDescendantFonts(PDFobj font, List<PDFobj> objects) {
        List<PDFobj> descendantFonts = new ArrayList<PDFobj>();
        List<String> dict = font.getDict();
        for (int i = 0; i < dict.size() - 2; i++) {
            if (dict.get(i).equals("/DescendantFonts")) {
                String token = dict.get(i + 2);
                if (!token.equals("]")) {
                    descendantFonts.add(objects.get(Integer.parseInt(token) - 1));
                }
            }
        }
        return descendantFonts;
    }

    private PDFobj getObject(String name, PDFobj obj, List<PDFobj> objects) {
        List<String> dict = obj.getDict();
        for (int i = 0; i < dict.size() - 1; i++) {
            if (dict.get(i).equals(name)) {
                String token = dict.get(i + 1);
                return objects.get(Integer.parseInt(token) - 1);
            }
        }
        return null;
    }

    /**
     * Collects the font descriptor of the given font, together with whichever
     * embedded font program it carries.
     */
    private void addFontDescriptor(
            PDFobj font, List<PDFobj> objects, List<PDFobj> resources) {
        PDFobj descriptor = getObject("/FontDescriptor", font, objects);
        if (descriptor == null) {
            return;
        }
        resources.add(descriptor);
        for (String key : new String[] {"/FontFile", "/FontFile2", "/FontFile3"}) {
            PDFobj fontFile = getObject(key, descriptor, objects);
            if (fontFile != null) {
                resources.add(fontFile);
            }
        }
    }

    /**
     * Returns the entries of a sub-dictionary of the resources, like /XObject,
     * without the brackets around them. The sub-dictionary can also be an
     * object of its own.
     */
    private List<String> getResourceEntries(
            PDFobj resources, String name, List<PDFobj> objects) {
        List<String> entries = new ArrayList<String>();
        List<String> dict = resources.getDict();
        int i = dict.indexOf(name) + 1;
        if (i == 0 || i >= dict.size()) {
            return entries;
        }
        if (isInteger(dict.get(i))) {   // "/XObject 12 0 R"
            dict = objects.get(Integer.parseInt(dict.get(i)) - 1).getDict();
            i = dict.indexOf("<<");
            if (i == -1) {
                return entries;
            }
        }
        if (!dict.get(i).equals("<<")) {
            return entries;
        }
        int level = 1;
        while (++i < dict.size()) {
            String token = dict.get(i);
            if (token.equals("<<")) {
                ++level;
            } else if (token.equals(">>") && --level == 0) {
                break;
            }
            entries.add(token);
        }
        return entries;
    }

    private static boolean isInteger(String token) {
        if (token.isEmpty()) {
            return false;
        }
        for (int i = 0; i < token.length(); i++) {
            if (!Character.isDigit(token.charAt(i))) {
                return false;
            }
        }
        return true;
    }

    /**
     * Returns the numbers of the objects that "number 0 R" references in the
     * tokens refer to.
     */
    private List<Integer> getReferences(List<String> tokens) {
        List<Integer> numbers = new ArrayList<Integer>();
        for (int i = 0; i + 2 < tokens.size(); i++) {
            if (tokens.get(i + 2).equals("R")
                    && isInteger(tokens.get(i)) && isInteger(tokens.get(i + 1))) {
                numbers.add(Integer.valueOf(tokens.get(i)));
                i += 2;
            }
        }
        return numbers;
    }

    /**
     * Collects the object with the given number and every object it refers to,
     * directly or through other objects, like the color space of an image or
     * the resources of a form XObject. The page tree is not followed.
     */
    private void addObjectTree(
            int number, List<PDFobj> objects, Set<Integer> numbers, List<PDFobj> resources) {
        if (number <= 0 || number > objects.size() || !numbers.add(number)) {
            return;
        }
        PDFobj obj = objects.get(number - 1);
        String type = obj.getValue("/Type");
        if (obj.dict.isEmpty()
                || type.equals("/Page") || type.equals("/Pages") || type.equals("/Catalog")) {
            return;
        }
        resources.add(obj);
        for (int reference : getReferences(obj.dict)) {
            addObjectTree(reference, objects, numbers, resources);
        }
    }

    /**
     * Collects the images and forms in the /XObject resources, with the
     * objects they use, and adds their names to the resources object.
     */
    private void addXObjects(
            PDFobj resObj, List<PDFobj> objects, Set<Integer> numbers, List<PDFobj> resources) {
        List<String> entries = getResourceEntries(resObj, "/XObject", objects);
        int i = 0;
        while (i < entries.size()) {
            String token = entries.get(i);
            if (token.startsWith("/") && (i + 3) < entries.size()
                    && entries.get(i + 3).equals("R")) {
                // Like the fonts, a name that an earlier page added is kept.
                if (!importedXObjects.contains(token)) {
                    importedXObjects.addAll(entries.subList(i, i + 4));
                    addObjectTree(Integer.parseInt(entries.get(i + 1)), objects, numbers, resources);
                }
                i += 4;
            } else {
                i += 1;
            }
        }
    }

    /**
     * Adds the specified objects to the PDF.
     *
     * @param objects the objects.
     * @throws Exception if there is an issue.
     */
    public void addResourceObjects(List<PDFobj> objects) throws Exception {
        List<PDFobj> resources = new ArrayList<PDFobj>();
        Set<Integer> numbers = new HashSet<Integer>();

        List<PDFobj> pages = getPageObjects(objects);
        for (PDFobj page : pages) {
            PDFobj resObj = page.getResourcesObject(objects);
            List<PDFobj> fonts = getFontObjects(resObj, objects);
            if (fonts != null) {
                for (PDFobj font : fonts) {
                    resources.add(font);
                    PDFobj obj = getObject("/ToUnicode", font, objects);
                    if (obj != null) {
                        resources.add(obj);
                    }
                    // A simple font carries its descriptor directly; only a
                    // composite one puts it on the descendant.
                    addFontDescriptor(font, objects, resources);
                    List<PDFobj> descendantFonts = getDescendantFonts(font, objects);
                    for (PDFobj descendantFont : descendantFonts) {
                        resources.add(descendantFont);
                        addFontDescriptor(descendantFont, objects, resources);
                    }
                }
            }
            addXObjects(resObj, objects, numbers, resources);
            extGState = getExtGState(resObj);
            // The /ExtGState dictionary is copied as it is, so the objects
            // that its entries refer to have to be copied too.
            for (int number : getReferences(getResourceEntries(resObj, "/ExtGState", objects))) {
                addObjectTree(number, objects, numbers, resources);
            }
        }
        resources.sort((o1, o2) -> Integer.compare(o1.number, o2.number));
        // An object can be collected twice, like a font that a form XObject
        // uses too, and must be written once.
        List<PDFobj> unique = new ArrayList<PDFobj>();
        for (PDFobj obj : resources) {
            if (unique.isEmpty() || unique.get(unique.size() - 1).number != obj.number) {
                unique.add(obj);
            }
        }
        addObjectsToPDF(unique);
    }

    /**
     * Adds the specified objects to the PDF.
     *
     * @param objects the objects.
     * @throws Exception if there is an issue.
     */
    public void addObjectsToPDF(List<PDFobj> objects) throws Exception {
        for (PDFobj obj : objects) {
            if (obj.offset == 0) {
                // Create new object.
                setObjOffset(obj.number, byteCount);
                append(obj.number);
                append(Token.NEW_OBJ);
                if (obj.dict != null) {
                    for (String token : obj.dict) {
                        appendToken(token);
                        append(Token.SPACE);
                    }
                }
                if (obj.stream != null) {
                    if (obj.dict.isEmpty()) {
                        append("<< /Length ");
                        append(obj.stream.length);
                        append(" >>");
                    }
                    append(Token.NEWLINE);
                    append(Token.STREAM);
                    append(obj.stream, 0, obj.stream.length);
                    append(Token.END_STREAM);
                }
                append("endobj\n");
            } else {
                setObjOffset(obj.number, byteCount);
                // Uncomment to see the format of the objects.
                // System.out.println(obj.dict);
                int n = obj.dict.size();
                String token = null;
                for (int i = 0; i < n; i++) {
                    token = obj.dict.get(i);
                    appendToken(token);
                    if (i < (n - 1)) {
                        append(Token.SPACE);
                    } else {
                        append(Token.NEWLINE);
                    }
                }
                if (obj.stream != null) {
                    append(obj.stream, 0, obj.stream.length);
                    append(Token.END_STREAM);
                }
                if (token == null || !token.equals("endobj")) {
                    append(Token.END_OBJ);
                }
            }
        }
    }
}   // End of PDF.java
