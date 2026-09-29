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
import java.security.SecureRandom;
import java.text.*;
import java.util.*;
import java.util.logging.Logger;
import java.util.zip.*;

/**
 * Used to create PDF objects that represent PDF documents.
 */
final public class PDF {
    Compliance compliance = Compliance.PDF_1_7;

    // The description of the PDF/UA identification schema, which PDF/A accepts
    // only when the metadata describes it, as ISO 19005-3 6.6.2.3 asks: the
    // metadata of PDF_A_3A_UA_1 names the part of PDF/UA as well as of PDF/A. It
    // goes into the list of extension schemas of a description the document
    // adds, such as that of Factur-X, as the metadata has one list, or into a
    // list of its own when the document adds none.
    private static final String PDF_UA_SCHEMA =
            "      <rdf:li rdf:parseType=\"Resource\">\n" +
            "        <pdfaSchema:namespaceURI>http://www.aiim.org/pdfua/ns/id/</pdfaSchema:namespaceURI>\n" +
            "        <pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>\n" +
            "        <pdfaSchema:schema>PDF/UA identification schema</pdfaSchema:schema>\n" +
            "        <pdfaSchema:property>\n" +
            "          <rdf:Seq>\n" +
            "            <rdf:li rdf:parseType=\"Resource\">\n" +
            "              <pdfaProperty:category>internal</pdfaProperty:category>\n" +
            "              <pdfaProperty:description>PDF/UA version identifier</pdfaProperty:description>\n" +
            "              <pdfaProperty:name>part</pdfaProperty:name>\n" +
            "              <pdfaProperty:valueType>Integer</pdfaProperty:valueType>\n" +
            "            </rdf:li>\n" +
            "          </rdf:Seq>\n" +
            "        </pdfaSchema:property>\n" +
            "      </rdf:li>\n";
    private static final String PDF_UA_EXTENSION_SCHEMAS =
            "<rdf:Description rdf:about=\"\"\n" +
            "    xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\"\n" +
            "    xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\"\n" +
            "    xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">\n" +
            "  <pdfaExtension:schemas>\n" +
            "    <rdf:Bag>\n" +
            PDF_UA_SCHEMA +
            "    </rdf:Bag>\n" +
            "  </pdfaExtension:schemas>\n" +
            "</rdf:Description>\n";

    // The descriptions the document adds, the first list of extension schemas
    // among them with the PDF/UA identification schema, or null when none has
    // such a list.
    private static List<String> withPDFUASchema(List<String> descriptions) {
        List<String> result = new ArrayList<String>(descriptions);
        for (int i = 0; i < result.size(); i++) {
            String description = result.get(i);
            int schemas = description.indexOf("<pdfaExtension:schemas>");
            int bag = schemas == -1 ? -1 : description.indexOf("<rdf:Bag>", schemas);
            if (bag == -1) {
                continue;
            }
            int at = bag + "<rdf:Bag>".length();
            result.set(i, description.substring(0, at) + "\n"
                    + PDF_UA_SCHEMA.substring(0, PDF_UA_SCHEMA.length() - 1) + description.substring(at));
            return result;
        }
        return null;
    }

    // Whether the content of the document is tagged, and follows the rules of
    // PDF/UA: that of PDF_UA_1, of the A levels of PDF/A, which ask for tagged
    // content, and of PDF_A_3A_UA_1, which is both.
    @SuppressWarnings("deprecation")
    boolean isTagged() {
        return compliance == Compliance.PDF_UA_1 || compliance == Compliance.PDF_A_1A
                || compliance == Compliance.PDF_A_2A || compliance == Compliance.PDF_A_3A
                || compliance == Compliance.PDF_A_3A_UA_1;
    }

    // Whether the document is of PDF/A, of any part and level.
    boolean isPDFA() {
        return compliance != Compliance.PDF_1_7 && compliance != Compliance.PDF_UA_1;
    }
    Bookmark toc = null;
    // The headings of a tagged document, in the order they are drawn, which
    // its bookmarks are made of when it has none of its own
    List<Heading> headings = new ArrayList<Heading>();
    List<Font> fonts = new ArrayList<Font>();
    List<Image> images = new ArrayList<Image>();
    List<OptionalContentGroup> groups = new ArrayList<OptionalContentGroup>();
    Map<String, Integer> states = new LinkedHashMap<String, Integer>();
    List<Stamp> stamps = new ArrayList<Stamp>();
    Encryption encryption = null;

    private int metadataObjNumber = 0;
    private int outputIntentObjNumber = 0;
    private final List<Page> pages = new ArrayList<Page>();
    private final Map<String, Destination> destinations = new HashMap<String, Destination>();
    private OutputStream os = null;
    private final List<Long> objOffset = new ArrayList<Long>();
    private final String producer = "PDFjet v9.0.2";
    private String title;
    private String author;
    private String subject;
    private String keywords;
    private String creator;
    private String createDate;      // XMP metadata
    private long byteCount = 0;
    private int pagesObjNumber = 0;
    private PageLayout pageLayout = null;
    private PageMode pageMode = null;
    private String language = "en-US";
    private String uuid = newDocumentID();
    // The files the document carries, in the order they were added.
    private final List<EmbeddedFile> associatedFiles = new ArrayList<EmbeddedFile>();
    // The descriptions that a standard of its own asks the metadata to carry.
    private final List<String> metadata = new ArrayList<String>();
    private final List<String> importedFonts = new ArrayList<String>();
    private final List<String> importedXObjects = new ArrayList<String>();
    private final List<String> importedExtGStates = new ArrayList<String>();
    private Page prevPage = null;
    private boolean contentStreamsCompression = true;
    // The structure elements of the pages of the document, in page order.
    // complete() collects them, so a detached page that was never added has
    // no part in the structure tree.
    private final List<StructElement> annotElements = new ArrayList<StructElement>();
    // The elements of the Document element, by number.
    private final List<Integer> documentKids = new ArrayList<Integer>();
    // The structure tree root, the parent tree and the Document element take
    // three numbers reserved before the first element is written.
    private int structTreeRootNumber = 0;
    private int parentTreeNumber = 0;
    private int documentElementNumber = 0;

    // The first misuse of the API. The call that finds it throws, and complete()
    // then refuses to finish the document, as the file would be broken even if
    // the program caught the exception and carried on.
    private String error = null;
    // True after complete(): the document is written and closed.
    boolean completed = false;
    // The pages made for this document, added or detached.
    int pagesCreated = 0;

    static final Logger LOG = Logger.getLogger(PDF.class.getName());

    // SecureRandom is safe to share between threads.
    private static final SecureRandom RANDOM = new SecureRandom();

    // Returns a new document ID for the trailer and the XMP metadata: 16 random
    // bytes as 32 hexadecimal digits, so documents made at the same time, even
    // in the same millisecond, get different IDs.
    private static String newDocumentID() {
        byte[] bytes = new byte[16];
        RANDOM.nextBytes(bytes);
        return Util.toHexString(bytes);
    }

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
    public PDF(OutputStream os) throws Exception { this(os, Compliance.PDF_1_7); }

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
    // Info
    // Root
    // xref table
    // Trailer
    /**
     *  Creates a PDF object that represents a PDF document.
     *  Use this constructor to create PDF/A compliant PDF documents.
     *  Please note: PDF/A compliance requires all fonts to be embedded in the PDF.
     *
     *  @param os the associated output stream.
     *  @param compliance must be: Compliance.PDF_UA_1, Compliance.PDF_A_1A to Compliance.PDF_A_3B, or Compliance.PDF_A_3A_UA_1, which is both PDF/A-3a and PDF/UA-1
     *  @throws Exception  If an input or output exception occurred
     */
    public PDF(OutputStream os, Compliance compliance) throws Exception {
        this.os = os;
        this.compliance = compliance;

        // The creation date is in UTC, so the XMP metadata says so with a Z.
        SimpleDateFormat sdf1 = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.US);
        sdf1.setTimeZone(TimeZone.getTimeZone("UTC"));
        createDate = sdf1.format(new Date()) + "Z";     // XMP metadata

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
        // ISO 19005 does not allow a PDF/A document to be encrypted, and the
        // encryption is written for the compliance: it grants a PDF/UA
        // document the permission to extract its content for accessibility.
        if (encryption != null && compliance != Compliance.PDF_1_7 && compliance != Compliance.PDF_UA_1) {
            fail(new IllegalStateException("A PDF/A document cannot be encrypted."));
        }
        if (encryption != null && compliance != this.compliance) {
            fail(new IllegalStateException("Set the compliance before the encryption, which is written for it."));
        }
        // The fonts and the page content are written for the compliance.
        if (compliance != this.compliance && (getObjNumber() > 0 || pagesCreated > 0)) {
            fail(new IllegalStateException("Set the compliance before adding fonts, images or pages to the PDF."));
        }
        this.compliance = compliance;
        return this;
    }

    /**
     * Returns the PDF document compliance.
     *
     * @return the compliance level.
     */
    public Compliance getCompliance() {
        return compliance;
    }

    /**
     * Sets the encryption applied to this document.
     *
     * @param encryption the encryption.
     * @return this PDF object.
     */
    public PDF setEncryption(Encryption encryption) {
        // Every object after the encryption dictionary is encrypted.
        if (encryption != null && encryption.getObjNumber() != getObjNumber()) {
            fail(new IllegalStateException("Set the encryption before adding fonts, images or pages to the PDF."));
        }
        // ISO 19005 does not allow a PDF/A document to be encrypted.
        if (encryption != null && compliance != Compliance.PDF_1_7 && compliance != Compliance.PDF_UA_1) {
            fail(new IllegalStateException("A PDF/A document cannot be encrypted."));
        }
        this.encryption = encryption;
        return this;
    }

    /**
     * Records the first misuse of the API and throws the exception.
     *
     * @param e the exception that says what was wrong.
     */
    void fail(RuntimeException e) {
        if (error == null) {
            error = e.getMessage();
        }
        throw e;
    }

    /**
     * Starts a new object in the document output and records its offset.
     *
     * @throws IOException if writing to the output fails.
     */
    void newObj() throws IOException {
        objOffset.add(byteCount);
        append(objOffset.size());
        append(Token.NEW_OBJ);
    }

    /**
     * Ends the current object in the document output.
     *
     * @throws IOException if writing to the output fails.
     */
    void endObj() throws IOException {
        append(Token.END_OBJ);
    }

    /**
     * Returns the number of the most recently started object.
     *
     * @return the object number.
     */
    int getObjNumber() {
        return objOffset.size();
    }

    /**
     * Records the offset of an object that carries its own number, growing the
     * table with placeholders for any number that has no object yet.
     */
    private void setObjOffset(int number, long offset) {
        if (number <= 0) {          // No number of its own - just append.
            objOffset.add(offset);
            return;
        }
        while (objOffset.size() < number) {
            objOffset.add(0L);
        }
        objOffset.set(number - 1, offset);
    }

    // Returns the offset as the 10 digits of an entry of the cross-reference
    // table, which cannot hold an offset of more than 10 digits.
    static String xrefOffset(long offset) throws IOException {
        String digits = Long.toString(offset);
        if (digits.length() > 10) {
            throw new IOException("The PDF is too large for a cross-reference table: "
                    + "an object starts at byte " + offset + ".");
        }
        return "0000000000".substring(digits.length()) + digits;
    }

    @SuppressWarnings("deprecation") // PDF_A_3A, which is still written
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
            // The notice of a font is text, which may hold an ampersand, like
            // that of the Noto fonts of Japanese, Korean and Chinese.
            sb.append(escapeXML(notice));
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
            } else if (compliance == Compliance.PDF_A_3A_UA_1) {
                sb.append("  <pdfuaid:part>1</pdfuaid:part>\n");
                sb.append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            }

            sb.append("  <pdf:Producer>");
            sb.append(producer);
            sb.append("</pdf:Producer>\n");

            if (title != null) {
                sb.append("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.append(escapeXML(title));
                sb.append("</rdf:li></rdf:Alt></dc:title>\n");
            }

            if (author != null) {
                sb.append("  <dc:creator><rdf:Seq><rdf:li>");
                sb.append(escapeXML(author));
                sb.append("</rdf:li></rdf:Seq></dc:creator>\n");
            }

            if (subject != null) {
                sb.append("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.append(escapeXML(subject));
                sb.append("</rdf:li></rdf:Alt></dc:description>\n");
            }

            if (keywords != null) {
                sb.append("  <pdf:Keywords>");
                sb.append(escapeXML(keywords));
                sb.append("</pdf:Keywords>\n");
            }

            if (creator != null) {
                sb.append("  <xmp:CreatorTool>");
                sb.append(escapeXML(creator));
                sb.append("</xmp:CreatorTool>\n");
            }

            sb.append("  <xmp:CreateDate>");
            sb.append(createDate);
            sb.append("</xmp:CreateDate>\n");

            sb.append("  <xapMM:DocumentID>uuid:");
            sb.append(uuid);
            sb.append("</xapMM:DocumentID>\n");

            sb.append("  <xapMM:InstanceID>uuid:");
            sb.append(uuid);
            sb.append("</xapMM:InstanceID>\n");

            sb.append("</rdf:Description>\n");

            List<String> descriptions = metadata;
            if (compliance == Compliance.PDF_A_3A_UA_1) {
                descriptions = withPDFUASchema(metadata);
                if (descriptions == null) {
                    descriptions = metadata;
                    sb.append(PDF_UA_EXTENSION_SCHEMAS);
                }
            }

            for (String description : descriptions) {
                sb.append(description);
                sb.append("\n");
            }
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

        // The metadata is encrypted like every other stream, and the
        // encryption dictionary says so with /EncryptMetadata true. Readers do
        // not agree on which metadata streams to leave alone when it is false.
        byte[] xml = sb.toString().getBytes(StandardCharsets.UTF_8);
        if (encryption != null) {
            xml = AES256.encrypt(xml, encryption.getKey());
        }

        // This is the metadata object
        newObj();
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
        endObj();

        return getObjNumber();
    }

    // Returns the text with the characters that have a meaning in XML escaped,
    // and without what XML does not allow, which cleanText leaves out.
    static String escapeXML(String text) {
        String clean = cleanText(text);
        StringBuilder sb = new StringBuilder(clean.length());
        for (int i = 0; i < clean.length(); i++) {
            char ch = clean.charAt(i);
            if (ch == '&') {
                sb.append("&amp;");
            } else if (ch == '<') {
                sb.append("&lt;");
            } else if (ch == '>') {
                sb.append("&gt;");
            } else {
                sb.append(ch);
            }
        }
        return sb.toString();
    }

    // Returns the text without what XML does not allow: the control
    // characters other than tab, line feed and carriage return, U+FFFE,
    // U+FFFF and unpaired surrogates, which would make the metadata
    // unreadable. The title and the other properties of the document are
    // cleaned when they are set, so that the information dictionary says what
    // the metadata says, as PDF/A asks.
    static String cleanText(String text) {
        if (text == null) {
            return null;
        }
        StringBuilder sb = new StringBuilder(text.length());
        for (int i = 0; i < text.length(); i++) {
            char ch = text.charAt(i);
            if (Character.isHighSurrogate(ch)
                    && i + 1 < text.length() && Character.isLowSurrogate(text.charAt(i + 1))) {
                sb.append(ch);
                sb.append(text.charAt(++i));
            } else if (ch == '\t' || ch == '\n' || ch == '\r'
                    || (ch >= 0x20 && ch <= 0xFFFD && !Character.isSurrogate(ch))) {
                sb.append(ch);
            }
        }
        return sb.toString();
    }

    private int addOutputIntentObject() throws Exception {
        byte[] profile = ICCBlackScaled.profile;
        if (encryption != null) {
            profile = AES256.encrypt(profile, encryption.getKey());
        }

        newObj();
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
        endObj();

        byte[] identifierBytes = "sRGB IEC61966-2.1".getBytes(StandardCharsets.UTF_8);
        if (encryption != null) {
            identifierBytes = AES256.encrypt(identifierBytes, encryption.getKey());
        }
        // OutputIntent object
        newObj();
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
        endObj();

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
        newObj();
        append(Token.BEGIN_DICTIONARY);
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
        // The graphics states of a PDF that was read and those of the pages
        // go in the same dictionary.
        if (states.size() > 0 || importedExtGStates.size() > 0) {
            append("/ExtGState <<\n");
            appendImportedEntries(importedExtGStates);
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
        endObj();
        return getObjNumber();
    }

    private void addPagesObject() throws Exception {
        setObjOffset(pagesObjNumber, byteCount);
        append(pagesObjNumber);
        append(Token.NEW_OBJ);
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Pages\n");
        append("/Kids [\n");
        for (Page page : pages) {
            append(page.objNumber);
            append(Token.OBJ_REF);
        }
        append("]\n");
        append("/Count ");
        append(pages.size());
        append(Token.NEWLINE);
        append(Token.END_DICTIONARY);
        endObj();
    }

    // Reserves the numbers of the structure tree root, the parent tree and
    // the Document element, which the elements written with their pages refer
    // to before the three are written.
    void reserveStructTreeNumbers() {
        if (structTreeRootNumber == 0) {
            structTreeRootNumber = reserveObjNumber();
            parentTreeNumber = reserveObjNumber();
            documentElementNumber = reserveObjNumber();
        }
    }

    private int addStructTreeRootObject() throws Exception {
        setObjOffset(structTreeRootNumber, byteCount);
        append(structTreeRootNumber);
        append(" 0 obj\n");
        append(Token.BEGIN_DICTIONARY);
        append("/Type /StructTreeRoot\n");
        append("/ParentTree ");
        append(parentTreeNumber);
        append(" 0 R\n");
        append("/K [\n");
        append(documentElementNumber);
        append(Token.OBJ_REF);
        append("]\n");
        // The types of PDF 2.0 that the document uses, which PDF 1.7 does not
        // have, are mapped to the standard types that PDF/UA-1 knows.
        if (!roles.isEmpty()) {
            append("/RoleMap <<");
            for (String[] role : ROLE_MAP) {
                if (roles.contains(role[0])) {
                    append(" /");
                    append(role[0]);
                    append(" /");
                    append(role[1]);
                }
            }
            append(" >>\n");
        }
        append(Token.END_DICTIONARY);
        endObj();
        return structTreeRootNumber;
    }

    // The structure types of PDF 2.0 that PDFjet writes, mapped to the
    // standard types of PDF 1.7, in the order the role map lists them.
    private static final String[][] ROLE_MAP = {
        {StructElem.TITLE.type, StructElem.P.type},
        {StructElem.EM.type, StructElem.SPAN.type},
        {StructElem.STRONG.type, StructElem.SPAN.type},
    };
    // The types of PDF 2.0 among the elements, which the role map maps.
    private final Set<String> roles = new HashSet<String>();

    private int addStructDocumentObject(int parent) throws Exception {
        setObjOffset(documentElementNumber, byteCount);
        append(documentElementNumber);
        append(" 0 obj\n");
        append(Token.BEGIN_DICTIONARY);
        append("/Type /StructElem\n");
        append("/S /Document\n");
        append("/P ");
        append(parent);
        append(" 0 R\n");
        append("/K [\n");
        for (Integer number : this.documentKids) {
            append(number.intValue());
            append(Token.OBJ_REF);
        }
        append("]\n");
        append(Token.END_DICTIONARY);
        endObj();
        return documentElementNumber;
    }

    // Writes the structure elements of a page that is written, so that a
    // document of many pages holds no more of them than the page it is
    // drawing. What it keeps is the elements that are still open and the ones
    // of an annotation, whose object is written when the document is completed.
    private void addPageStructElements(Page page) throws Exception {
        if (!isTagged() || page.structures.isEmpty()) {
            return;
        }
        List<StructElement> kept = new ArrayList<StructElement>();
        for (StructElement element : page.structures) {
            // The element of an annotation alone has no marked content, its
            // mcid -1; the Link of a text has the marked content of the text
            // too. The negative entries of mcids are the Link elements of the
            // words of a paragraph, not marked content.
            List<Integer> mcids = new ArrayList<Integer>();
            for (Integer mcid : element.mcids) {
                if (mcid.intValue() >= 0) {
                    mcids.add(mcid);
                }
            }
            if (element.mcid >= 0) {
                mcids.add(element.mcid);
            }
            for (Integer mcid : mcids) {
                while (page.mcidNumbers.size() <= mcid.intValue()) {
                    page.mcidNumbers.add(0);
                }
                page.mcidNumbers.set(mcid.intValue(), element.objNumber);
            }
            if (element.parent == null) {
                documentKids.add(element.objNumber);
            }
            if (element.annotation != null) {
                annotElements.add(element);
            }
            if (element.open || element.annotation != null) {
                kept.add(element);
                continue;
            }
            addStructElementObject(element);
        }
        page.structures = kept;
    }

    // Writes one structure element, under the number it was given when it was
    // made.
    private void addStructElementObject(StructElement element) throws Exception {
        {
            setObjOffset(element.objNumber, byteCount);
            append(element.objNumber);
            append(" 0 obj\n");
            for (String[] role : ROLE_MAP) {
                if (role[0].equals(element.structure)) {
                    roles.add(role[0]);
                }
            }
            append("<<\n/Type /StructElem /S /");
            append(element.structure);
            append("\n/P ");
            if (element.parent != null) {
                append(element.parent.objNumber);
            } else {
                append(documentElementNumber);
            }
            append(" 0 R /Pg ");
            append(element.pageObjNumber);
            append(Token.OBJ_REF);

            if (element.annotation != null) {
                // A link: the marked content of its text, or the elements it
                // holds, like the Figure of an image, then its annotation
                append("/K [");
                if (element.mcid >= 0) {
                    append(element.mcid);
                    append(Token.SPACE);
                }
                for (Integer kid : element.kids) {
                    append(kid.intValue());
                    append(" 0 R ");
                }
                append("<</Type /OBJR /Obj ");
                append(element.annotation.objNumber);
                append(" 0 R>>]\n");
            } else if (element.mcid >= 0) {
                append("/K ");
                append(element.mcid);
                append("\n");
            } else if (!element.mcids.isEmpty()) {
                // The marked contents of a paragraph drawn word by word, and
                // the Link elements of its words that are links
                append("/K [");
                for (int i = 0; i < element.mcids.size(); i++) {
                    if (i > 0) {
                        append(Token.SPACE);
                    }
                    int mcid = element.mcids.get(i).intValue();
                    if (mcid < 0) {
                        append(-mcid);
                        append(" 0 R");
                    } else {
                        append(mcid);
                    }
                }
                append("]\n");
            } else if (!element.kids.isEmpty()) {
                append("/K [");
                for (Integer kid : element.kids) {
                    append(kid.intValue());
                    append(" 0 R ");
                }
                append("]\n");
            }

            String attributes = element.attributes;
            if (element.placedAsBlock()) {
                attributes = StructElement.withPlacementBlock(attributes);
            }
            if (attributes != null && !attributes.isEmpty()) {
                append("/A ");
                append(attributes);
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
                append("/ActualText ");
                appendTextString(element.actualText);
                append("\n");
            }

            if (hasAltDescription) {
                append("/Alt ");
                appendTextString(element.altDescription);
                append("\n");
            }

            append(">>\n");
            endObj();
        }
    }

    private void addNumsParentTree() throws Exception {
        setObjOffset(parentTreeNumber, byteCount);
        append(parentTreeNumber);
        append(Token.NEW_OBJ);
        append(Token.BEGIN_DICTIONARY);
        append("/Nums [\n");
        // The keys must be listed in increasing order, so the page entries -
        // whose keys are the /StructParents values 0 .. pages.size()-1 - come
        // first. Each value is the array of struct elements of that page,
        // indexed by the MCID they were marked with.
        for (int i = 0; i < pages.size(); i++) {
            append(i);
            append(" [");
            for (Integer number : pages.get(i).mcidNumbers) {
                append(Token.SPACE);
                append(number.intValue());
                append(" 0 R");
            }
            append("]\n");
            pages.get(i).mcidNumbers = new ArrayList<Integer>();
        }
        // The annotations follow, keyed by the /StructParent values handed out
        // by addAnnotDictionaries(), which continue where the pages left off.
        int structParent = pages.size();
        for (StructElement element : this.annotElements) {
            if (element.annotation != null) {
                append(structParent++);
                append(Token.SPACE);
                append(element.objNumber);
                append(Token.OBJ_REF);
            }
        }
        append("]\n");
        append(Token.END_DICTIONARY);
        endObj();
    }

    // Adds the document information dictionary, which readers like pdfinfo
    // show. It says what the XMP metadata of PDF/A and PDF/UA documents says.
    private int addInfoObject() throws Exception {
        newObj();
        append(Token.BEGIN_DICTIONARY);
        appendInfoText("/Title", title);
        appendInfoText("/Author", author);
        appendInfoText("/Subject", subject);
        appendInfoText("/Keywords", keywords);
        appendInfoText("/Creator", creator);
        appendInfoText("/Producer", producer);
        appendInfoString("/CreationDate", getDate().getBytes(StandardCharsets.US_ASCII));
        append(Token.END_DICTIONARY);
        endObj();
        return getObjNumber();
    }

    // The moment the document was made, as a date string of PDF: the XMP
    // creation date 2026-01-31T12:00:00Z is D:20260131120000Z.
    String getDate() {
        return "D:" + createDate.replace("-", "").replace("T", "").replace(":", "");
    }

    // Appends an entry of the information dictionary with the text, unless
    // the text is null.
    private void appendInfoText(String key, String text) throws Exception {
        if (text != null) {
            append(key);
            append(Token.SPACE);
            appendTextString(text);
            append(Token.NEWLINE);
        }
    }

    /**
     * Appends a text string, like a bookmark title or an alternate description:
     * UTF-16BE with a byte order mark in hexadecimal, encrypted if the document
     * is encrypted. A text string without the mark is in PDFDocEncoding, so
     * UTF-8 bytes would show as two or three wrong characters each.
     *
     * @param text the text.
     * @throws Exception if writing to the output fails.
     */
    void appendTextString(String text) throws Exception {
        byte[] bytes = ("\uFEFF" + text).getBytes(StandardCharsets.UTF_16BE);
        if (encryption != null) {
            bytes = AES256.encrypt(bytes, encryption.getKey());
        }
        append('<');
        append(Util.toHexString(bytes));
        append('>');
    }

    // Appends an entry of the information dictionary with the bytes of a
    // string, encrypted if the document is encrypted.
    private void appendInfoString(String key, byte[] bytes) throws Exception {
        append(key);
        append(Token.SPACE);
        appendByteString(bytes);
        append(Token.NEWLINE);
    }

    // Appends a string of bytes, like a date, in hexadecimal, encrypted if the
    // document is encrypted.
    void appendByteString(byte[] bytes) throws Exception {
        if (encryption != null) {
            bytes = AES256.encrypt(bytes, encryption.getKey());
        }
        append('<');
        append(Util.toHexString(bytes));
        append('>');
    }

    private int addRootObject(
            int structTreeRootObjNumber, int outlineDictNumber) throws Exception {
        // Add the root object
        newObj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Catalog\n");

        if (compliance != Compliance.PDF_1_7) {
            byte[] languageBytes = this.language.getBytes(java.nio.charset.StandardCharsets.UTF_8);
            if (encryption != null) {
                languageBytes = AES256.encrypt(languageBytes, encryption.getKey());
            }
            append("/Lang <");
            append(Util.toHexString(languageBytes));
            append(">\n");

            // Only a tagged document has a structure tree: a PDF/A of level B
            // that said it was marked would say its content is tagged, which
            // it is not.
            if (isTagged()) {
                append("/StructTreeRoot ");
                append(structTreeRootObjNumber);
                append(Token.OBJ_REF);

                append("/MarkInfo <</Marked true>>\n");
            }
            append("/ViewerPreferences <</DisplayDocTitle true>>\n");
        }

        if (pageLayout != null) {
            append("/PageLayout /");
            append(pageLayout.value);
            append(Token.NEWLINE);
        }

        if (pageMode != null) {
            append("/PageMode /");
            append(pageMode.value);
            append(Token.NEWLINE);
        }

        addOCProperties();

        append("/Pages ");
        append(pagesObjNumber);
        append(Token.OBJ_REF);

        addAssociatedFiles();

        if (compliance != Compliance.PDF_1_7) {
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
        endObj();
        return getObjNumber();
    }

    // The files the document carries: /AF says which they are and how each of
    // them relates to the document, and the name tree of /EmbeddedFiles is
    // where a reader of the document looks for a file by its name.
    private void addAssociatedFiles() throws Exception {
        if (associatedFiles.isEmpty()) {
            return;
        }
        append("/AF [");
        for (int i = 0; i < associatedFiles.size(); i++) {
            if (i > 0) {
                append(Token.SPACE);
            }
            append(associatedFiles.get(i).objNumber);
            append(" 0 R");
        }
        append("]\n");

        // The names of a name tree are in order, which here means the order of
        // the characters of the names. The tree is one node, as a document
        // carries few files.
        List<EmbeddedFile> sorted = new ArrayList<EmbeddedFile>(associatedFiles);
        Collections.sort(sorted, new Comparator<EmbeddedFile>() {
            @Override
            public int compare(EmbeddedFile file1, EmbeddedFile file2) {
                return file1.fileName.compareTo(file2.fileName);
            }
        });
        append("/Names <</EmbeddedFiles <</Names [");
        for (EmbeddedFile file : sorted) {
            appendTextString(file.fileName);
            append(Token.SPACE);
            append(file.objNumber);
            append(" 0 R");
        }
        append("]>>>>\n");
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

    // Gives every destination the object number of the page it is on, which
    // the page was given when it was added.
    private void setDestinationObjNumbers() {
        for (Page page : pages) {
            for (Destination destination : page.destinations) {
                destination.pageObjNumber = page.objNumber;
                destinations.put(destination.name, destination);
            }
        }
    }

    private void addAllPages(int resObjNumber) throws Exception {
        setDestinationObjNumbers();
        addAnnotDictionaries();

        pagesObjNumber = reserveObjNumber();

        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            if (page.mergedDict != null) {
                List<String> dict = new ArrayList<String>(page.mergedDict);
                setEntry(dict, "/Parent", Arrays.asList(String.valueOf(pagesObjNumber), "0", "R"));
                setObjOffset(page.objNumber, byteCount);
                append(page.objNumber);
                append(Token.NEW_OBJ);
                appendTokens(dict);
                append(Token.NEWLINE);
                append(Token.END_OBJ);
                continue;
            }
            // Page object, under the number it was given when it was added.
            setObjOffset(page.objNumber, byteCount);
            append(page.objNumber);
            append(Token.NEW_OBJ);
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

            if (isTagged()) {
                append("/Tabs /S\n");
                append("/StructParents ");
                append(i);
                append(Token.NEWLINE);
            }

            append(Token.END_DICTIONARY);
            endObj();
        }
    }

    private void addPageContent(Page page) throws Exception {
        page.checkBalanced();
        if (contentStreamsCompression) {
            // Page content usually compresses to less than an eighth of its size.
            ByteArrayOutputStream baos = new ByteArrayOutputStream(page.buf.size() / 8 + 64);
            Deflater deflater = new Deflater();
            DeflaterOutputStream dos = new DeflaterOutputStream(baos, deflater, 8192);
            page.buf.writeTo(dos);      // The content, without a copy of it
            dos.finish();
            deflater.end();
            page.buf = new Page.WrittenContent(this);  // Release the page content memory!

            byte[] encrypted = null;
            if (encryption != null) {
                encrypted = AES256.encrypt(baos.toByteArray(), encryption.getKey());
            }

            newObj();
            append(Token.BEGIN_DICTIONARY);
            append("/Filter /FlateDecode\n");
            append(Token.LENGTH);
            append(encrypted != null ? encrypted.length : baos.size());
            append(Token.NEWLINE);
            append(Token.END_DICTIONARY);
            append(Token.STREAM);
            if (encrypted != null) {
                append(encrypted);
            } else {
                append(baos);       // The compressed content, without a copy of it
            }
            append(Token.END_STREAM);
            endObj();
            page.contents.add(getObjNumber());
        } else {    // No compression. Used for diagnostics
            byte[] buf = page.buf.toByteArray();
            if (encryption != null) {
                buf = AES256.encrypt(buf, encryption.getKey());
            }
            page.buf = new Page.WrittenContent(this);  // Release the page content memory!

            newObj();
            append(Token.BEGIN_DICTIONARY);
            append(Token.LENGTH);
            append(buf.length);
            append(Token.NEWLINE);
            append(Token.END_DICTIONARY);
            append(Token.STREAM);
            append(buf);
            append(Token.END_STREAM);
            endObj();
            page.contents.add(getObjNumber());
        }
        addPageStructElements(page);
    }

    // Writes the appearance of an annotation that is not a link, which PDF/A
    // asks for, and returns its object number. It draws what a viewer draws
    // for the annotation: the square, the circle or the polygon in its fill
    // color, and a note or a file as a white box with a black frame. Its box
    // is the rectangle of the annotation, in the coordinates of the page.
    private int addAppearanceObject(
            Annotation annot, float x1, float y1, float x2, float y2) throws Exception {
        float minX = Math.min(x1, x2);
        float maxX = Math.max(x1, x2);
        float minY = Math.min(y1, y2);
        float maxY = Math.max(y1, y2);
        float w = maxX - minX;
        float h = maxY - minY;
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        String type = annot.annotationType;
        if (type.equals(Annotation.Square)) {
            appendFill(buf, annot);
            appendNumbers(buf, minX, minY, w, h);
            appendAscii(buf, "re f\n");
        } else if (type.equals(Annotation.Circle)) {
            // Four Bezier curves, one for each quarter of the ellipse.
            final float kappa = 0.55228475f;
            float rx = w / 2;
            float ry = h / 2;
            float cx = minX + rx;
            float cy = minY + ry;
            float ox = rx * kappa;
            float oy = ry * kappa;
            appendFill(buf, annot);
            appendNumbers(buf, cx + rx, cy);
            appendAscii(buf, "m\n");
            appendNumbers(buf, cx + rx, cy + oy, cx + ox, cy + ry, cx, cy + ry);
            appendAscii(buf, "c\n");
            appendNumbers(buf, cx - ox, cy + ry, cx - rx, cy + oy, cx - rx, cy);
            appendAscii(buf, "c\n");
            appendNumbers(buf, cx - rx, cy - oy, cx - ox, cy - ry, cx, cy - ry);
            appendAscii(buf, "c\n");
            appendNumbers(buf, cx + ox, cy - ry, cx + rx, cy - oy, cx + rx, cy);
            appendAscii(buf, "c\n");
            appendAscii(buf, "f\n");
        } else if (type.equals(Annotation.Polygon)) {
            appendFill(buf, annot);
            for (int i = 0; i + 1 < annot.vertices.length; i += 2) {
                appendNumbers(buf, annot.x1 + annot.vertices[i], annot.y1 - annot.vertices[i + 1]);
                appendAscii(buf, (i == 0) ? "m\n" : "l\n");
            }
            appendAscii(buf, "h f\n");
        } else {
            appendAscii(buf, "1 g 0 G 0.5 w\n");
            appendNumbers(buf, minX + 0.25f, minY + 0.25f, w - 0.5f, h - 0.5f);
            appendAscii(buf, "re B\n");
        }
        byte[] content = buf.toByteArray();
        if (encryption != null) {
            content = AES256.encrypt(content, encryption.getKey());
        }

        newObj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /XObject\n");
        append("/Subtype /Form\n");
        append("/BBox [");
        append(minX);
        append(' ');
        append(minY);
        append(' ');
        append(maxX);
        append(' ');
        append(maxY);
        append("]\n");
        append("/Length ");
        append(content.length);
        append(Token.NEWLINE);
        append(Token.END_DICTIONARY);
        append("stream\n");
        append(content);
        append("\nendstream\n");
        endObj();
        return getObjNumber();
    }

    // Appends the fill color of the annotation to the content of its appearance.
    private static void appendFill(ByteArrayOutputStream buf, Annotation annot) {
        appendNumbers(buf, annot.fillColor[0], annot.fillColor[1], annot.fillColor[2]);
        appendAscii(buf, "rg\n");
    }

    // Appends the numbers to the content of an appearance, each followed by a space.
    private static void appendNumbers(ByteArrayOutputStream buf, float... values) {
        for (float value : values) {
            byte[] number = FastFloat.toByteArray(value);
            buf.write(number, 0, number.length);
            buf.write(' ');
        }
    }

    private static void appendAscii(ByteArrayOutputStream buf, String text) {
        byte[] bytes = text.getBytes(StandardCharsets.US_ASCII);
        buf.write(bytes, 0, bytes.length);
    }

    private int addAnnotationObject(Annotation annot, int index) throws Exception {
        // The rectangle of an annotation of vertices, a polygon, is the box of its
        // vertices, which are relative to its location; its second corner is not
        // set
        float x1 = annot.x1;
        float y1 = annot.y1;
        float x2 = annot.x2;
        float y2 = annot.y2;
        if (annot.vertices != null && annot.vertices.length >= 2) {
            float minX = annot.vertices[0];
            float maxX = annot.vertices[0];
            float minY = annot.vertices[1];
            float maxY = annot.vertices[1];
            for (int i = 2; i + 1 < annot.vertices.length; i += 2) {
                minX = Math.min(minX, annot.vertices[i]);
                maxX = Math.max(maxX, annot.vertices[i]);
                minY = Math.min(minY, annot.vertices[i + 1]);
                maxY = Math.max(maxY, annot.vertices[i + 1]);
            }
            x1 = annot.x1 + minX;
            y1 = annot.y1 - maxY;
            x2 = annot.x1 + maxX;
            y2 = annot.y1 - minY;
        }

        // PDF/A asks every annotation but a link for an appearance of its own,
        // which is written before it.
        int appearance = 0;
        if (isPDFA() && !annot.annotationType.equals(Annotation.Link)) {
            appearance = addAppearanceObject(annot, x1, y1, x2, y2);
        }

        newObj();
        annot.objNumber = getObjNumber();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Annot\n");
        append("/Subtype /");
        append(annot.annotationType);
        append("\n");
        append("/Rect [");
        append(x1);
        append(' ');
        append(y1);
        append(' ');
        append(x2);
        append(' ');
        append(y2);
        append("]\n");
        append("/Border [0 0 0]\n");
        // Every annotation is printed, as PDF/A asks.
        append("/F 4\n");
        if (appearance > 0) {
            append("/AP <</N ");
            append(appearance);
            append(" 0 R>>\n");
        }

        if (annot.annotationType.equals(Annotation.FileAttachment)) {
            append("/FS ");
            append(annot.fileAttachment.embeddedFile.objNumber);
            append(" 0 R\n");
            append("/Name /");
            append(annot.fileAttachment.icon);
            append("\n");

            if (annot.fileAttachment.title != null && !annot.fileAttachment.title.isEmpty()) {
                append("/T ");
                appendTextString(annot.fileAttachment.title);
                append("\n");
            }

            if (annot.fileAttachment.contents != null && !annot.fileAttachment.contents.isEmpty()) {
                append("/Contents ");
                appendTextString(annot.fileAttachment.contents);
                append("\n");
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
                append("/Contents ");
                appendTextString(description);
                append("\n");
            }
            if (!Util.isEmpty(annot.uri)) {
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
            } else if (!Util.isEmpty(annot.key)) {
                Destination destination = destinations.get(annot.key);
                if (destination == null) {
                    // A link to nowhere would do nothing when it is clicked.
                    fail(new IllegalStateException("The link goes to the destination " + annot.key
                            + ", which the document does not have."));
                } else {
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
            append(annot.opacity);
            append("\n");

            if (annot.title != null && !annot.title.isEmpty()) {
                append("/T ");
                appendTextString(annot.title);
                append("\n");
            }

            if (annot.contents != null && !annot.contents.isEmpty()) {
                append("/Contents ");
                appendTextString(annot.contents);
                append("\n");
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
            append(annot.opacity);
            append("\n");

            if (annot.title != null && !annot.title.isEmpty()) {
                append("/T ");
                appendTextString(annot.title);
                append("\n");
            }

            if (annot.contents != null && !annot.contents.isEmpty()) {
                append("/Contents ");
                appendTextString(annot.contents);
                append("\n");
            }
        } else if (annot.annotationType.equals(Annotation.Text)) {
            append("/Name /Comment\n");

            if (annot.title != null && !annot.title.isEmpty()) {
                append("/T ");
                appendTextString(annot.title);
                append("\n");
            }

            if (annot.contents != null && !annot.contents.isEmpty()) {
                append("/Contents ");
                appendTextString(annot.contents);
                append("\n");
            }
        }

        if (index != -1) {
            append("/StructParent ");
            append(index++);
            append("\n");
        }
        append(Token.END_DICTIONARY);
        endObj();

        return index;
    }

    private void addAnnotDictionaries() throws Exception {
        int index = pages.size();
        for (StructElement element : this.annotElements) {
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

            // The groups hidden by default, for the viewers that read the
            // configuration and not the usage of each group
            StringBuilder off = new StringBuilder();
            for (OptionalContentGroup ocg : this.groups) {
                if (!ocg.visible) {
                    off.append(' ');
                    off.append(ocg.objNumber);
                    off.append(" 0 R");
                }
            }
            if (off.length() > 0) {
                append("/OFF [");
                append(off.toString());
                append(" ]\n");
            }

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
        if (completed) {
            fail(new IllegalStateException("The PDF was already completed."));
        }
        if (page.pdf != this) {
            fail(new IllegalArgumentException("The page belongs to another PDF."));
        }
        if (page.added) {
            fail(new IllegalStateException("The page was already added to the PDF."));
        }
        if (pagesObjNumber != 0) {
            // The page tree of the objects that addObjects wrote does not list it.
            fail(new IllegalStateException(
                    "A page cannot be added to a PDF that addObjects added the objects of an existing PDF to."));
        }
        page.added = true;
        if (page.objNumber == 0) {
            page.objNumber = reserveObjNumber();
        }
        // A page that was drawn before it was added has elements of its own.
        if (isTagged()) {
            page.setStructElementsPageObjNumber(page.objNumber);
        }
        pages.add(page);
        if (prevPage != null) {
            addPageContent(prevPage);
        }
        prevPage = page;
    }

    /**
     * Adds all the pages of a document that was read with read() after the
     * pages of this document, in their order. A PDF can merge several
     * documents and draw pages of its own before, between and after them.
     * <p>
     * The merged pages keep their content, resources, annotations and links.
     * The parts of the read document that belong to the whole document are
     * left out: its bookmarks, form fields, tagging, named destinations and
     * optional content settings. The objects that the pages use are written
     * at once, so the list of objects is not needed after the call.
     * <p>
     * A PDF/UA or PDF/A document cannot merge pages, which were not made for
     * its compliance, and merge cannot be used with addObjects.
     *
     * @param objects the objects of the document, as read() returns them.
     * @throws Exception if an input or output exception occurred.
     */
    public void merge(List<PDFobj> objects) throws Exception {
        checkMerge(objects);
        mergePages(objects, getPageObjects(objects));
    }

    /**
     * Adds the listed pages of a document that was read with read() after the
     * pages of this document, in the order they are listed. A document is
     * split by merging each part of it into a PDF of its own: the objects that
     * read() returned can be merged into any number of PDFs.
     * <p>
     * The pages are merged as merge(objects) merges all of them, and a link to
     * a page that is not merged leads nowhere. A page number that the document
     * does not have, or one that is listed twice, is refused.
     *
     * @param objects the objects of the document, as read() returns them.
     * @param pageNumbers the numbers of the pages, counted from 1.
     * @throws Exception if an input or output exception occurred.
     */
    public void merge(List<PDFobj> objects, int... pageNumbers) throws Exception {
        checkMerge(objects);
        List<PDFobj> pageObjects = getPageObjects(objects);
        List<PDFobj> listed = new ArrayList<PDFobj>();
        Set<Integer> seen = new HashSet<Integer>();
        for (int number : pageNumbers) {
            if (number < 1 || number > pageObjects.size()) {
                fail(new IllegalArgumentException("The document has no page " + number + "."));
            }
            if (!seen.add(number)) {
                fail(new IllegalArgumentException("Page " + number + " is listed twice."));
            }
            listed.add(pageObjects.get(number - 1));
        }
        mergePages(objects, listed);
    }

    // Refuses a merge that would break this document.
    private void checkMerge(List<PDFobj> objects) {
        if (completed) {
            fail(new IllegalStateException("The PDF was already completed."));
        }
        if (compliance != Compliance.PDF_1_7) {
            fail(new IllegalStateException(
                    "Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document."));
        }
        if (pagesObjNumber != 0) {
            fail(new IllegalStateException("merge and addObjects cannot be used on the same PDF."));
        }
        if (getPagesObject(objects) == null) {
            fail(new IllegalArgumentException("The objects have no root /Pages object."));
        }
    }

    // Adds the pages, in their order, and every object that they use.
    private void mergePages(List<PDFobj> objects, List<PDFobj> pageObjects) throws Exception {
        Set<Integer> mergedPages = new HashSet<Integer>();
        for (PDFobj page : pageObjects) {
            mergedPages.add(page.number);
        }

        // Each page and every object that it uses, found through the
        // references, gets a number of this document before anything is
        // written, as the objects refer to each other: a page to its
        // annotations, and a link annotation to the page it points at.
        Map<Integer, Integer> numbers = new HashMap<Integer, Integer>();
        Map<Integer, List<String>> values = new HashMap<Integer, List<String>>();
        List<PDFobj> queue = new ArrayList<PDFobj>();
        Map<PDFobj, Map<String, List<String>>> nodes = new IdentityHashMap<PDFobj, Map<String, List<String>>>();
        for (PDFobj page : pageObjects) {
            if (!numbers.containsKey(page.number)) {
                numbers.put(page.number, reserveObjNumber());
                queue.add(page);
            }
        }
        for (int i = 0; i < queue.size(); i++) {
            PDFobj obj = queue.get(i);
            List<String> value = mergedValue(obj, mergedPages.contains(obj.number), objects, nodes);
            values.put(obj.number, value);
            for (int j = 0; j < value.size(); j++) {
                if (isReference(value, j)) {
                    int number = Integer.parseInt(value.get(j));
                    if (!numbers.containsKey(number) && isMergedObject(number, objects, mergedPages)) {
                        numbers.put(number, reserveObjNumber());
                        queue.add(objects.get(number - 1));
                    }
                    j += 2;
                }
            }
        }

        for (PDFobj obj : queue) {
            if (!mergedPages.contains(obj.number)) {
                addMergedObject(obj, numbers.get(obj.number), renumbered(values.get(obj.number), numbers));
            }
        }
        for (PDFobj obj : queue) {
            if (mergedPages.contains(obj.number)) {
                pages.add(new Page(this, numbers.get(obj.number), renumbered(values.get(obj.number), numbers)));
            }
        }
    }

    // The entries of a page that it can inherit from the page tree.
    private static final String[] INHERITED = {"/Resources", "/MediaBox", "/CropBox", "/Rotate"};

    // Reserves the next object number for an object written later.
    int reserveObjNumber() {
        objOffset.add(0L);
        return objOffset.size();
    }

    // Returns true when the tokens at index i are a reference: "n g R".
    static boolean isReference(List<String> tokens, int i) {
        return i + 2 < tokens.size()
                && tokens.get(i + 2).equals("R")
                && isObjectNumber(tokens.get(i))
                && isObjectNumber(tokens.get(i + 1));
    }

    // Returns true for a number that can be an object number or a generation
    // number: digits, which can have leading zeros, as pdf.js tests it in
    // issue10491, "0000000003 0 R", and no more than nine others.
    private static boolean isObjectNumber(String token) {
        int zeros = 0;
        while (zeros < token.length() && token.charAt(zeros) == '0') {
            zeros++;
        }
        if (token.isEmpty() || token.length() - zeros > 9) {
            return false;
        }
        for (int i = 0; i < token.length(); i++) {
            if (token.charAt(i) < '0' || token.charAt(i) > '9') {
                return false;
            }
        }
        return true;
    }

    // Returns true for an object that the merged pages can use: not the page
    // tree, the catalog, a page that is not merged or an object that is missing.
    private boolean isMergedObject(int number, List<PDFobj> objects, Set<Integer> mergedPages) {
        if (number < 1 || number > objects.size()) {
            return false;
        }
        PDFobj obj = objects.get(number - 1);
        if (obj.dict == null || obj.dict.isEmpty()) {
            return false;
        }
        String type = obj.getValue("/Type");
        if (type.equals("/Pages") || type.equals("/Catalog")) {
            return false;
        }
        return !isPageObject(obj) || mergedPages.contains(number);
    }

    // Returns the value of an object that was read, without its "n g obj" and
    // its "stream" and "endobj" keywords, with a direct /Length for a stream,
    // and for a page with the entries it inherits and without its /Parent.
    private static List<String> mergedValue(
            PDFobj obj, boolean isPage, List<PDFobj> objects, Map<PDFobj, Map<String, List<String>>> nodes) {
        List<String> value = valueOf(obj);
        if (obj.stream != null) {
            setEntry(value, "/Length", Collections.singletonList(String.valueOf(obj.stream.length)));
        }
        if (isPage) {
            for (String key : INHERITED) {
                if (entryIndex(value, key) == -1) {
                    List<String> inherited = inheritedValue(obj, key, objects, nodes);
                    if (inherited == null && key.equals("/MediaBox")) {
                        inherited = Arrays.asList("[", "0", "0", "612", "792", "]");    // Letter
                    }
                    if (inherited != null) {
                        setEntry(value, key, inherited);
                    }
                }
            }
            removeEntry(value, "/Parent");
        }
        return value;
    }

    static List<String> valueOf(PDFobj obj) {
        List<String> dict = obj.dict;
        int start = (dict.size() >= 3 && dict.get(2).equals("obj")) ? 3 : 0;
        int end = dict.size();
        if (end > start && dict.get(end - 1).equals("endobj")) {
            end--;
        }
        if (end > start && dict.get(end - 1).equals("stream")) {
            end--;
        }
        return new ArrayList<String>(dict.subList(start, end));
    }

    // Returns the value of the entry from the nearest node of the page tree
    // above the page that has it, or null. The entries of each node are found
    // once and kept in nodes, as the node of a flat tree lists thousands of
    // pages, and each of them looks up each entry it does not have.
    private static List<String> inheritedValue(
            PDFobj page, String key, List<PDFobj> objects, Map<PDFobj, Map<String, List<String>>> nodes) {
        List<String> tokens = valueOf(page);
        int i = entryIndex(tokens, "/Parent");
        List<String> parent = (i == -1) ? null : tokens.subList(i + 1, valueEnd(tokens, i + 1));
        for (int depth = 0; depth < 64; depth++) {  // A loop in a broken tree ends here.
            if (parent == null || !isReference(parent, 0)) {
                return null;
            }
            int number = Integer.parseInt(parent.get(0));
            if (number < 1 || number > objects.size() || objects.get(number - 1).dict.isEmpty()) {
                return null;
            }
            PDFobj node = objects.get(number - 1);
            Map<String, List<String>> entries = nodes.get(node);
            if (entries == null) {
                entries = entriesOf(valueOf(node));
                nodes.put(node, entries);
            }
            List<String> value = entries.get(key);
            if (value != null) {
                return new ArrayList<String>(value);
            }
            parent = entries.get("/Parent");
        }
        return null;
    }

    // Returns the entries of the dictionary, the first of each key, by key.
    private static Map<String, List<String>> entriesOf(List<String> tokens) {
        Map<String, List<String>> entries = new HashMap<String, List<String>>();
        if (tokens.isEmpty() || !tokens.get(0).equals("<<")) {
            return entries;
        }
        int i = 1;
        while (i < tokens.size() && !tokens.get(i).equals(">>")) {
            int end = valueEnd(tokens, i + 1);
            if (!entries.containsKey(tokens.get(i))) {
                entries.put(tokens.get(i), tokens.subList(i + 1, end));
            }
            i = end;
        }
        return entries;
    }

    // Returns the index after the value that starts at index i.
    static int valueEnd(List<String> tokens, int i) {
        if (i >= tokens.size()) {
            return tokens.size();
        }
        String token = tokens.get(i);
        if (token.equals("<<") || token.equals("[")) {
            int depth = 0;
            for (int j = i; j < tokens.size(); j++) {
                String t = tokens.get(j);
                if (t.equals("<<") || t.equals("[")) {
                    depth++;
                } else if (t.equals(">>") || t.equals("]")) {
                    depth--;
                    if (depth == 0) {
                        return j + 1;
                    }
                }
            }
            return tokens.size();
        }
        return isReference(tokens, i) ? i + 3 : i + 1;
    }

    // Returns the index of the key of an entry of the dictionary, not of a
    // dictionary inside it, or -1.
    static int entryIndex(List<String> tokens, String key) {
        if (tokens.isEmpty() || !tokens.get(0).equals("<<")) {
            return -1;
        }
        int i = 1;
        while (i < tokens.size() && !tokens.get(i).equals(">>")) {
            if (tokens.get(i).equals(key)) {
                return i;
            }
            i = valueEnd(tokens, i + 1);
        }
        return -1;
    }

    // Sets the value of an entry of the dictionary, adding the entry at its end.
    private static void setEntry(List<String> tokens, String key, List<String> value) {
        if (tokens.isEmpty() || !tokens.get(0).equals("<<")) {
            return;
        }
        int i = entryIndex(tokens, key);
        if (i != -1) {
            tokens.subList(i + 1, valueEnd(tokens, i + 1)).clear();
            tokens.addAll(i + 1, value);
        } else {
            int end = valueEnd(tokens, 0) - 1;     // The index of the closing >>
            tokens.addAll(end, value);
            tokens.add(end, key);
        }
    }

    private static void removeEntry(List<String> tokens, String key) {
        int i = entryIndex(tokens, key);
        if (i != -1) {
            tokens.subList(i, valueEnd(tokens, i + 1)).clear();
        }
    }

    // Returns the tokens with the references renumbered for this document, a
    // reference to an object that is not merged replaced with null, and the
    // strings encrypted when this document is encrypted.
    private List<String> renumbered(List<String> tokens, Map<Integer, Integer> numbers) throws Exception {
        List<String> result = new ArrayList<String>(tokens.size());
        for (int i = 0; i < tokens.size(); i++) {
            String token = tokens.get(i);
            if (isReference(tokens, i)) {
                Integer number = numbers.get(Integer.parseInt(token));
                if (number == null) {
                    result.add("null");
                } else {
                    result.add(String.valueOf(number));
                    result.add("0");
                    result.add("R");
                }
                i += 2;
            } else if (encryption != null
                    && (token.startsWith("(") || (token.startsWith("<") && !token.equals("<<")))) {
                result.add("<" + Util.toHexString(AES256.encrypt(Decryptor.toBytes(token), encryption.getKey())) + ">");
            } else {
                result.add(token);
            }
        }
        return result;
    }

    private void addMergedObject(PDFobj obj, int number, List<String> value) throws Exception {
        byte[] stream = obj.stream;
        if (stream != null && encryption != null) {
            stream = AES256.encrypt(stream, encryption.getKey());
            setEntry(value, "/Length", Collections.singletonList(String.valueOf(stream.length)));
        }
        setObjOffset(number, byteCount);
        append(number);
        append(Token.NEW_OBJ);
        appendTokens(value);
        append(Token.NEWLINE);
        if (stream != null) {
            append(Token.STREAM);
            append(stream);
            append(Token.END_STREAM);
        }
        append(Token.END_OBJ);
    }

    private void appendTokens(List<String> tokens) throws IOException {
        for (int i = 0; i < tokens.size(); i++) {
            if (i > 0) {
                append(Token.SPACE);
            }
            appendToken(tokens.get(i));
        }
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
        // The output stream is closed also when the document could not be
        // completed.
        try {
            writeRest();
        } catch (Exception e) {
            try {
                os.close();
            } catch (IOException closeError) {
                e.addSuppressed(closeError);
            }
            throw e;
        }
        os.close();
    }

    // Writes the rest of the PDF.
    private void writeRest() throws Exception {
        if (completed) {
            fail(new IllegalStateException("complete() was already called."));
        }
        if (error != null) {
            throw new IllegalStateException("The PDF was not completed because of an earlier error: " + error);
        }
        if (pages.isEmpty() && pagesObjNumber == 0) {
            fail(new IllegalStateException("A PDF needs at least one page."));
        }
        // PDF/UA asks for the title of the document, which a reader shows in
        // place of the name of the file.
        if ((compliance == Compliance.PDF_UA_1 || compliance == Compliance.PDF_A_3A_UA_1)
                && (title == null || title.trim().isEmpty())) {
            fail(new IllegalStateException("A PDF/UA document needs a title: use setTitle."));
        }
        if (prevPage != null) {
            addPageContent(prevPage);
        }
        completed = true;
        if (compliance != Compliance.PDF_1_7) {
            metadataObjNumber = addMetadataObject("", false);
            outputIntentObjNumber = addOutputIntentObject();
        }

        if (pagesObjNumber == 0) {
            addAllPages(addResourcesObject());
            addPagesObject();
        }

        int structTreeRootObjNumber = 0;
        if (isTagged()) {
            // The elements of every page are written with it; the ones still
            // open and the ones of the annotations are what is left.
            for (Page page : pages) {
                for (StructElement element : page.structures) {
                    addStructElementObject(element);
                }
                page.structures = new ArrayList<StructElement>();
            }
            reserveStructTreeNumbers();
            structTreeRootObjNumber = addStructTreeRootObject();
            addNumsParentTree();
            addStructDocumentObject(structTreeRootObjNumber);
        }

        // A tagged document with headings and no bookmarks of its own has the
        // bookmarks of its headings, which a reader shows as its outline, as
        // PAC asks of a document with headings
        if (toc == null && !headings.isEmpty()) {
            toc = Bookmark.ofHeadings(this, headings);
        }
        int outlineDictNum = 0;
        if (toc != null && toc.getChildren() != null) {
            List<Bookmark> list = toc.toArrayList();
            outlineDictNum = addOutlineDict(toc);
            for (int i = 1; i < list.size(); i++) {
                addOutlineItem(outlineDictNum, list.get(i));
            }
        }

        int infoObjNumber = addInfoObject();
        int rootObjNumber = addRootObject(structTreeRootObjNumber, outlineDictNum);
        long startxref = byteCount;

        // Create the xref table
        append("xref\n");
        append("0 ");
        append(rootObjNumber + 1);
        append('\n');
        append("0000000000 65535 f \n");
        for (long offset : objOffset) {
            if (offset == 0) {      // A number that no object was written for.
                append("0000000000 65535 f \n");
                continue;
            }
            append(xrefOffset(offset));
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

        append("/Info ");
        append(infoObjNumber);
        append(" 0 R\n");

        append("/Root ");
        append(rootObjNumber);
        append(" 0 R\n");

        append(Token.END_DICTIONARY);
        append("startxref\n");
        append(Long.toString(startxref));
        append('\n');
        append("%%EOF\n");
    }

    /**
     * Adds a file that the document carries with it: a reader shows it beside
     * the document, and a program that reads the document finds it by its
     * name. This is how a document of PDF/A-3 carries the data behind what it
     * shows, such as the XML of an invoice.
     *
     * <p>The file names what it holds, how it relates to the document and what
     * it is, so it has to be made with the constructor of EmbeddedFile that
     * takes them.
     *
     * @param file the embedded file.
     * @return this PDF object.
     */
    @SuppressWarnings("deprecation") // PDF_A_3A, which carries files too
    public PDF addAssociatedFile(EmbeddedFile file) {
        // PDF/A-1 carries no files at all, and PDF/A-2 only other documents of
        // PDF/A, so a document of either that carries a file is not the
        // document it says it is.
        if (compliance == Compliance.PDF_A_1A || compliance == Compliance.PDF_A_1B
                || compliance == Compliance.PDF_A_2A || compliance == Compliance.PDF_A_2B) {
            fail(new IllegalStateException("A document of " + compliance
                    + " cannot carry the file " + file.getFileName()
                    + ": PDF/A-3 is the one that carries files."));
            return this;
        }
        if (file.relationship == null) {
            fail(new IllegalArgumentException("The file " + file.getFileName()
                    + " was embedded without a media type, a relationship and a description, "
                    + "which a file the document carries needs: use the constructor of "
                    + "EmbeddedFile that takes them."));
            return this;
        }
        // PDF/A-3 asks a file it carries for what it holds, its /Subtype, and
        // for its size and date, which are written with the media type.
        if ((file.mediaType == null || file.mediaType.isEmpty())
                && (compliance == Compliance.PDF_A_3A || compliance == Compliance.PDF_A_3B
                || compliance == Compliance.PDF_A_3A_UA_1)) {
            fail(new IllegalArgumentException("The file " + file.getFileName()
                    + " was embedded without a media type, which a file of a document of PDF/A-3 needs."));
            return this;
        }
        associatedFiles.add(file);
        return this;
    }

    /**
     * Adds a description to the metadata of the document: the rdf:Description
     * element of a standard that asks for properties of its own, such as the
     * invoice standards that say which of the files the document carries is
     * the invoice. The text is written into the metadata as it is given, so it
     * has to be XML, and the document has to be of PDF/A or PDF/UA, which are
     * the documents that carry metadata.
     *
     * @param rdfDescription the rdf:Description element.
     * @return this PDF object.
     */
    public PDF addMetadata(String rdfDescription) {
        metadata.add(rdfDescription);
        return this;
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

    // An empty document property is the same as one that was never set: it is
    // not written, as in the Go port, which cannot tell the two apart.
    private static String nullIfEmpty(String text) {
        return (text != null && text.isEmpty()) ? null : text;
    }

    /**
     *  Set the "Title" document property of the PDF file. An empty title is not written.
     *  @param title The title of this document.
     *  @return this PDF object.
     */
    public PDF setTitle(String title) {
        this.title = nullIfEmpty(cleanText(title));
        return this;
    }

    /**
     *  Set the "Author" document property of the PDF file.
     *  @param author The author of this document.
     *  @return this PDF object.
     */
    public PDF setAuthor(String author) {
        this.author = nullIfEmpty(cleanText(author));
        return this;
    }

    /**
     *  Set the "Subject" document property of the PDF file.
     *  @param subject The subject of this document.
     *  @return this PDF object.
     */
    public PDF setSubject(String subject) {
        this.subject = nullIfEmpty(cleanText(subject));
        return this;
    }

    /**
     * Sets the PDF keywords.
     *
     * @param keywords the keywords.
     * @return this PDF object.
     */
    public PDF setKeywords(String keywords) {
        this.keywords = nullIfEmpty(cleanText(keywords));
        return this;
    }

    /**
     * Sets the PDF creator.
     *
     * @param creator the creator.
     * @return this PDF object.
     */
    public PDF setCreator(String creator) {
        this.creator = nullIfEmpty(cleanText(creator));
        return this;
    }

    /**
     * Sets the PDF page layout.
     *
     * @param pageLayout the page layout.
     * @return this PDF object.
     */
    public PDF setPageLayout(PageLayout pageLayout) {
        this.pageLayout = pageLayout;
        return this;
    }

    /**
     * Set the PDF page mode.
     *
     * @param pageMode the page mode.
     * @return this PDF object.
     */
    public PDF setPageMode(PageMode pageMode) {
        this.pageMode = pageMode;
        return this;
    }

    /**
     * Writes the number to the document output.
     *
     * @param num the number.
     * @throws IOException if writing to the output fails.
     */
    void append(int num) throws IOException {
        append(Integer.toString(num));
    }

    /**
     * Writes the number to the document output.
     *
     * @param f the number.
     * @throws IOException if writing to the output fails.
     */
    void append(float f) throws IOException {
        if (!FastFloat.isWritable(f)) {
            fail(new IllegalArgumentException(FastFloat.NOT_WRITABLE));
        }
        append(FastFloat.toByteArray(f));
    }

    /**
     * Writes the string to the document output as UTF-8.
     *
     * @param str the string.
     * @throws IOException if writing to the output fails.
     */
    void append(String str) throws IOException {
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
    void append(char ch) throws IOException {
        os.write((byte) ch);
        byteCount += 1;
    }

    /**
     * Writes the byte to the document output.
     *
     * @param b the byte.
     * @throws IOException if writing to the output fails.
     */
    void append(byte b) throws IOException {
        os.write(b);
        byteCount += 1;
    }

    /**
     * Writes the bytes to the document output.
     *
     * @param buf the bytes.
     * @throws IOException if writing to the output fails.
     */
    void append(byte[] buf) throws IOException {
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
    void append(byte[] buf, int off, int len) throws IOException {
        os.write(buf, off, len);
        byteCount += len;
    }

    /**
     * Writes the contents of the stream to the document output.
     *
     * @param baos the stream.
     * @throws IOException if writing to the output fails.
     */
    void append(ByteArrayOutputStream baos) throws IOException {
        baos.writeTo(os);
        byteCount += baos.size();
    }

    // The highest object number read in a file of any size: its empty objects
    // take about 30 MB.
    private static final int MIN_OBJ_NUMBER_LIMIT = 262144;

    // Returns the objects by their number, with an empty object at the number
    // of every one the PDF does not have, so that the object a reference names
    // is the one at its number. A PDF may number its objects as it likes, and
    // one that kept the numbers of the document it was cut from has few
    // objects and high numbers, so a number up to MIN_OBJ_NUMBER_LIMIT is read
    // in any file, and a larger one in a file that has as many bytes. The empty
    // objects up to a larger number would take the memory a file of a few
    // bytes never names.
    private List<PDFobj> getSortedObjects(List<PDFobj> objects, int size) throws Exception {
        List<PDFobj> sorted = new ArrayList<PDFobj>();

        int maxObjNumber = 0;
        for (PDFobj obj : objects) {
            if (obj.number > maxObjNumber) {
                maxObjNumber = obj.number;
            }
        }
        if (maxObjNumber > Math.max(size, MIN_OBJ_NUMBER_LIMIT)) {
            throw new Exception("The PDF of " + size
                    + " bytes cannot hold an object numbered " + maxObjNumber + ".");
        }

        for (int number = 1; number <= maxObjNumber; number++) {
            PDFobj obj = new PDFobj();
            obj.setNumber(number);
            sorted.add(obj);
        }

        for (PDFobj obj : objects) {
            if (obj.number > 0) {
                sorted.set(obj.number - 1, obj);
            }
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
        return read(inputStream, "");
    }

    /**
     *  Returns a list of objects of type PDFobj read from input stream, which
     *  holds a PDF that is encrypted with the standard security handler.
     *  The PDF is decrypted with the password, which is its user or its owner
     *  password.
     *
     *  @param inputStream the PDF input stream.
     *  @param password the user or owner password of the PDF.
     *
     *  @return the list of PDF objects.
     *  @throws Exception  If the password is not correct, or an input or output exception occurred
     */
    public List<PDFobj> read(InputStream inputStream, String password) throws Exception {
        byte[] buf = Content.getFromStream(inputStream);

        List<PDFobj> objects1 = new ArrayList<PDFobj>();
        PDFobj.DecodeBudget budget = new PDFobj.DecodeBudget(buf.length); // For all the streams of this PDF together
        PDFobj trailer = null;
        try {
            trailer = getObjects(buf, getStartXRef(buf), objects1, 0, new HashSet<Integer>(), budget);
        } catch (PDFobj.DecodedTotalException e) {
            throw e;            // Not a reason to scan the PDF for its objects
        } catch (Exception e) {
            trailer = null;     // A cross-reference stream that cannot be decoded.
        }
        if (trailer == null || objects1.isEmpty()) {
            // The cross-reference table is missing or wrong, like in a PDF
            // that was changed without updating it.
            objects1.clear();
            trailer = getObjectsByScanning(buf, objects1, budget);
        }
        Decryptor decryptor = Decryptor.getDecryptor(trailer, objects1, password);

        // The object of each number, for a /Length that is an object of its
        // own. The newest version of an object that was updated comes last
        // and wins.
        Map<Integer, PDFobj> numbered = new HashMap<Integer, PDFobj>();
        for (PDFobj obj : objects1) {
            numbered.put(obj.number, obj);
        }

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
                obj.setStreamAndData(buf, obj.getLength(numbered), decryptor, budget);
            }

            if (type.equals("/ObjStm")) {
                // A malformed object stream is an error, as in the other
                // ports, and not a reason to stop the program.
                int first = objectStreamNumber(obj.getValue("/First"));
                // An object stream with no stream of its own has no objects,
                // as it has in the Go and Swift ports.
                byte[] data = (obj.data == null) ? new byte[0] : obj.data;
                PDFobj o2 = getObject(data, 0, Math.min(first, data.length));
                // Its objects are read in no more than its length in all, as
                // one whose offset is listed again was read again: a stream
                // of a few kilobytes that decodes to a megabyte, with an
                // object listed a thousand times, took seconds.
                PDFobj.DecodeBudget streamBudget = new PDFobj.DecodeBudget(0);
                streamBudget.readLeft = data.length;
                for (int i = 0; i + 1 < o2.dict.size(); i += 2) {
                    String num = o2.dict.get(i);
                    int number = objectStreamNumber(num);
                    int off = objectStreamNumber(o2.dict.get(i + 1));
                    // The offsets are added as longs, as their sum can be
                    // more than an int holds; one past the end of the data
                    // is the end.
                    int end = data.length;
                    if (i <= o2.dict.size() - 4) {
                        end = (int) Math.min(
                                (long) first + objectStreamNumber(o2.dict.get(i + 3)), data.length);
                    }
                    int start = (int) Math.min((long) first + off, data.length);
                    PDFobj o3 = readObject(data, start, end, streamBudget);
                    o3.setNumber(number);
                    o3.dict.add(0, "obj");
                    o3.dict.add(0, "0");
                    o3.dict.add(0, num);
                    objects2.add(o3);
                }
            } else {
                objects2.add(obj);
            }
        }

        List<PDFobj> sorted = getSortedObjects(objects2, buf.length);
        PDFobj root = PDFobj.objectNumbered(sorted, trailerRoot(trailer));
        if (root != null) {
            root.root = true;
        }
        return sorted;
    }

    // Returns the number of the catalog that the /Root of the trailer names,
    // or 0. The trailer is the dictionary after the cross-reference table, or
    // that of the cross-reference stream.
    private static int trailerRoot(PDFobj trailer) {
        if (trailer == null) {
            return 0;
        }
        int open = trailer.dict.indexOf("<<");
        if (open == -1) {
            return 0;
        }
        List<String> tokens = trailer.dict.subList(open, trailer.dict.size());
        int i = entryIndex(tokens, "/Root");
        if (i == -1 || !isReference(tokens, i + 1)) {
            return 0;
        }
        return toInteger(tokens.get(i + 1));
    }

    // Returns the number in the header of an object stream, which is digits
    // and no more than the largest int, as in the other ports.
    private int objectStreamNumber(String token) throws Exception {
        int number = toInteger(token);
        if (number >= 0) {
            return number;
        }
        throw new Exception("The object stream of the PDF is malformed: \""
                + token + "\" is not a number.");
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
            // The keyword ends with CRLF or LF, and the tokenizer consumed the
            // first of those bytes, so only the LF of a CRLF is left to skip.
            // A data byte that is a line feed, like the first byte of the IV
            // of an encrypted stream, stays.
            obj.streamOffset = off;
            if (off > 0 && buf[off - 1] == '\r' && off < buf.length && buf[off] == '\n') {
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

    // Returns the object at the offset, which ends at len at the latest.
    private PDFobj getObject(byte[] buf, int off, int len) {
        PDFobj obj = new PDFobj();
        obj.offset = off;
        obj.end = len;
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
        obj.read = off;

        return obj;
    }

    // Returns the object at the offset that a cross-reference section or an
    // object stream names, or the section itself, which ends at end at the
    // latest, and takes the bytes it reads from the budget. An object that
    // what is left of the budget does not reach the end of has no tokens, as
    // one that is outside of the PDF, and the PDF is then read by looking for
    // its objects: a thousand sections chained by /Prev with no startxref
    // after them, or a table that lists one object thousands of times, was
    // each read to the end of the PDF or of the object.
    private PDFobj readObject(byte[] buf, int off, int end, PDFobj.DecodeBudget budget) {
        if (off < 0 || off >= buf.length) {
            return new PDFobj();
        }
        end = Math.min(end, buf.length);
        int limit = (int) Math.min(end, (long) off + Math.max(budget.readLeft, 0));
        PDFobj obj = getObject(buf, off, limit);
        budget.readLeft -= obj.read - off;
        if (limit < end && obj.read >= limit) {
            return new PDFobj();
        }
        return obj;
    }

    static boolean isWhiteSpace(int c) {
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
     * A section that a /Prev leads back to, which visited holds the offset
     * of, is a broken PDF, and is not read again: a /Prev that points at its
     * own section read the section a thousand times.
     *
     * @return the trailer of the section, which is the cross-reference
     *     stream object when there is no table, or null when an offset in
     *     the section is not that of its object.
     */
    private PDFobj getObjects(
            byte[] buf, int offset, List<PDFobj> objects, int depth,
            Set<Integer> visited, PDFobj.DecodeBudget budget) throws Exception {
        if (!visited.add(offset)) {
            return null;
        }
        PDFobj xref = readObject(buf, offset, buf.length, budget);
        boolean table = !xref.dict.isEmpty() && xref.dict.get(0).equals("xref");
        if (depth > 1000 || (!table && !isObject(xref, -1))) {
            return null;
        }
        String prev = xref.getValue("/Prev");
        if (!prev.isEmpty() && getObjects(buf, toInteger(prev), objects, depth + 1, visited, budget) == null) {
            return null;
        }
        if (table) {
            // The objects in the table replace those in the /XRefStm stream.
            String xrefStm = xref.getValue("/XRefStm");
            if (!xrefStm.isEmpty() &&
                    !getStreamObjects(buf, readObject(buf, toInteger(xrefStm), buf.length, budget), objects, budget)) {
                return null;
            }
            if (!getTableObjects(buf, xref, objects, budget)) {
                return null;
            }
        } else if (!getStreamObjects(buf, xref, objects, budget)) {
            return null;
        }
        return xref;
    }

    // Adds the objects of the entries, each a number and an offset, and
    // returns false when an offset is not that of its object. An object ends
    // where the next one of the section starts at the latest, as one with no
    // endobj was read to the end of the PDF: a section of objects with no
    // endobj took seconds for every hundred kilobytes.
    private boolean getEntryObjects(
            byte[] buf, List<int[]> entries, List<PDFobj> objects, PDFobj.DecodeBudget budget) {
        int[] offsets = new int[entries.size()];
        for (int i = 0; i < offsets.length; i++) {
            offsets[i] = entries.get(i)[1];
        }
        Arrays.sort(offsets);
        for (int[] entry : entries) {
            int end = buf.length;
            int j = firstAfter(offsets, entry[1]);
            if (j < offsets.length) {
                end = Math.min(offsets[j], buf.length);
            }
            PDFobj obj = readObject(buf, entry[1], end, budget);
            if (!isObject(obj, entry[0])) {
                return false;
            }
            obj.number = entry[0];
            objects.add(obj);
        }
        return true;
    }

    // Returns the index of the first of the sorted values that is greater
    // than the value, or the number of values when there is none.
    private static int firstAfter(int[] sorted, int value) {
        int low = 0;
        int high = sorted.length;
        while (low < high) {
            int mid = (low + high) >>> 1;
            if (sorted[mid] <= value) {
                low = mid + 1;
            } else {
                high = mid;
            }
        }
        return low;
    }

    // Adds the objects in use of a cross-reference table, and returns false
    // when an offset is not that of its object.
    private boolean getTableObjects(
            byte[] buf, PDFobj xref, List<PDFobj> objects, PDFobj.DecodeBudget budget) {
        List<int[]> entries = new ArrayList<int[]>();
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
                // The entry is the offset, the generation number and n for an
                // object in use. Object 0 heads the list of free objects, and
                // one that is marked in use, as pdf.js tests it in issue10004,
                // is skipped, as MuPDF skips it.
                if (dict.get(i + 2).equals("n") && number != 0) {
                    entries.add(new int[] {number, toInteger(dict.get(i))});
                }
            }
        }
        return i < dict.size() && dict.get(i).equals("trailer") && getEntryObjects(buf, entries, objects, budget);
    }

    // Adds the objects of a cross-reference stream that are not in object
    // streams, and returns false when an offset is not that of its object.
    private boolean getStreamObjects(
            byte[] buf, PDFobj xref, List<PDFobj> objects,
            PDFobj.DecodeBudget budget) throws Exception {
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
        xref.setStreamAndData(buf, length, budget);
        int n = n1 + n2 + n3;   // Number of bytes per entry
        List<int[]> entries = new ArrayList<int[]>();
        int offset = 0;
        for (int s = 0; s + 1 < index.size(); s += 2) {
            int number = index.get(s);
            for (int j = 0; j < index.get(s + 1) && offset + n <= xref.data.length; j++) {
                // Process the entries in a cross-reference stream.
                // Page 51 in PDF32000_2008.pdf
                int type = (n1 == 0) ? 1 : toInt(xref.data, offset, n1);
                if (type == 1) {
                    entries.add(new int[] {number, toInt(xref.data, offset + n1, n2)});
                }
                number++;
                offset += n;
            }
        }
        return getEntryObjects(buf, entries, objects, budget);
    }

    /**
     * Adds the objects of the PDF to the list by looking for "number
     * generation obj" in it, for when the cross-reference table is missing
     * or wrong. The objects of incremental updates are later in the PDF, so
     * the newest version of an object comes last.
     *
     * An object ends where the next one starts at the latest, as one with no
     * endobj was read to the end of the PDF, and the last trailer is read
     * once when the scan is done: a PDF of objects with no endobj took
     * seconds for every hundred kilobytes.
     *
     * @return the last trailer, or the last cross-reference stream object
     *     when there is no trailer, or null when there is neither.
     */
    private PDFobj getObjectsByScanning(byte[] buf, List<PDFobj> objects, PDFobj.DecodeBudget budget) {
        PDFobj xrefStream = null;
        int trailerOffset = -1;
        int next = 0;   // The start of the object after the one at i
        int i = 0;
        while (i < buf.length) {
            if (isObjectStart(buf, i)) {
                if (next <= i) {
                    next = nextObjectStart(buf, i + 1);
                }
                PDFobj obj = getObject(buf, i, next);
                if (isObject(obj, -1)) {
                    obj.number = toInteger(obj.dict.get(0));
                    objects.add(obj);
                    if (obj.getValue("/Type").equals("/XRef")) {
                        xrefStream = obj;
                    }
                    if (obj.dict.contains("stream")) {
                        // Skip the stream, as its bytes can look like an
                        // object: to the endstream after its /Length, or else
                        // to the first one before the next object, or to the
                        // next object. The endstream was looked for to the end
                        // of the PDF, and the objects after a stream with none
                        // were not read.
                        int length = toInteger(obj.getValue("/Length"));
                        int end = PDFobj.endstreamAfter(buf, obj.streamOffset, length, budget);
                        if (end == -1) {
                            end = indexOf(buf, "endstream", obj.streamOffset, next);
                        }
                        i = (end == -1) ? next : end;
                        continue;
                    }
                }
            } else if (startsWith(buf, i, "trailer")) {
                trailerOffset = i;
            }
            i++;
        }
        return (trailerOffset != -1) ? getObject(buf, trailerOffset) : xrefStream;
    }

    // Returns the offset of the first "number generation obj" from the
    // offset on, or the length of the PDF when there is none.
    private static int nextObjectStart(byte[] buf, int off) {
        for (int i = off; i < buf.length; i++) {
            if (isObjectStart(buf, i)) {
                return i;
            }
        }
        return buf.length;
    }

    // Returns true when "number generation obj" starts at the offset, after
    // white space or at the start of the PDF. An offset that is not at a
    // number returns at once, as the white space after it was scanned at
    // every offset of a run of white space.
    private static boolean isObjectStart(byte[] buf, int off) {
        if (off > 0 && !isWhiteSpace(buf[off - 1])) {
            return false;
        }
        int i = off;
        while (i < buf.length && buf[i] >= '0' && buf[i] <= '9') {
            i++;
        }
        if (i == off) {
            return false;
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
        return j > i && k > j && m > k && startsWith(buf, m, "obj");
    }

    static boolean startsWith(byte[] buf, int off, String str) {
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

    // Returns the offset of the first str from the offset from on that ends
    // by the offset to, or -1.
    static int indexOf(byte[] buf, String str, int from, int to) {
        for (int i = Math.max(from, 0); i + str.length() <= Math.min(to, buf.length); i++) {
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
     * Adds the outline dictionary to the PDF. The bookmarks were numbered by
     * toArrayList(), level by level, and the object of a bookmark is that many
     * objects after the outline dictionary.
     *
     * @param toc the root of the bookmarks.
     * @return the object number of the outline dictionary.
     * @throws Exception if there is an issue.
     */
    int addOutlineDict(Bookmark toc) throws Exception {
        newObj();
        append(Token.BEGIN_DICTIONARY);
        append("/Type /Outlines\n");
        append("/First ");
        append(getObjNumber() + toc.getFirstChild().objNumber);
        append(" 0 R\n");
        append("/Last ");
        append(getObjNumber() + toc.getLastChild().objNumber);
        append(" 0 R\n");
        // The items that are visible: those of the first level, as the items
        // with children are closed.
        append("/Count ");
        append(toc.getChildren().size());
        append("\n");
        append(Token.END_DICTIONARY);
        endObj();
        return getObjNumber();
    }

    /**
     * Adds an outline item for the bookmark.
     *
     * @param outlines the object number of the outline dictionary.
     * @param bm1 the bookmark.
     * @throws Exception if there is an issue.
     */
    void addOutlineItem(int outlines, Bookmark bm1) throws Exception {
        int prev = (bm1.getPrevBookmark() == null) ? 0 : outlines + bm1.getPrevBookmark().objNumber;
        int next = (bm1.getNextBookmark() == null) ? 0 : outlines + bm1.getNextBookmark().objNumber;

        int first = 0;
        int last  = 0;
        int count = 0;
        if (bm1.getChildren() != null && bm1.getChildren().size() > 0) {
            first = outlines + bm1.getFirstChild().objNumber;
            last  = outlines + bm1.getLastChild().objNumber;
            // A closed item: the items that opening it would show.
            count = (-1) * bm1.getChildren().size();
        }

        newObj();
        append(Token.BEGIN_DICTIONARY);
        append("/Title ");
        appendTextString(bm1.getTitle());
        append("\n");
        // The root of the bookmarks is the outline dictionary itself.
        append("/Parent ");
        append(outlines + bm1.getParent().objNumber);
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
        append("/Dest [");
        append(bm1.getDestination().pageObjNumber);
        append(" 0 R /XYZ ");
        append(bm1.getDestination().xPosition);
        append(" ");
        append(bm1.getDestination().yPosition);
        append(" 0]\n");
        append(Token.END_DICTIONARY);
        endObj();
    }

    /**
     * Adds the specified objects to the PDF. The objects keep their numbers and
     * are written as they are, so they are added before any font, image or
     * page of this document, and an encrypted PDF cannot take them. Their page
     * tree is the page tree of this document, so it can have no pages of its
     * own, before or after them, and it cannot be a PDF/UA or PDF/A document,
     * as the pages were not made for its compliance.
     *
     * @param objects the objects.
     * @throws Exception if there is an issue.
     */
    public void addObjects(List<PDFobj> objects) throws Exception {
        for (Page page : pages) {
            if (page.mergedDict != null) {
                fail(new IllegalStateException("merge and addObjects cannot be used on the same PDF."));
            }
        }
        if (compliance != Compliance.PDF_1_7) {
            fail(new IllegalStateException(
                    "The objects of an existing PDF cannot be added to a PDF/UA or PDF/A document."));
        }
        if (!pages.isEmpty()) {
            fail(new IllegalStateException(
                    "The objects of an existing PDF cannot be added to a PDF that has pages of its own."));
        }
        PDFobj pagesObject = getPagesObject(objects);
        if (pagesObject == null) {
            fail(new IllegalArgumentException("The objects have no root /Pages object."));
        }
        checkObjects(objects);
        this.pagesObjNumber = Integer.parseInt(pagesObject.dict.get(0));
        addObjectsToPDF(objects);
    }

    // Refuses objects of a PDF that was read when writing them would break this
    // document. They keep their numbers and are written as they are, so they
    // have to come before the objects that this document numbers itself, and
    // an encrypted document cannot take them.
    private void checkObjects(List<PDFobj> objects) {
        if (completed) {
            fail(new IllegalStateException("The PDF was already completed."));
        }
        if (encryption != null) {
            fail(new IllegalStateException(
                    "The objects of an existing PDF cannot be added to an encrypted PDF."));
        }
        for (PDFobj obj : objects) {
            if (obj.number > 0 && obj.number <= objOffset.size() && objOffset.get(obj.number - 1) != 0L) {
                fail(new IllegalStateException("Add the objects of an existing PDF before "
                        + "fonts, images or pages are added to the PDF: object "
                        + obj.number + " is already written."));
            }
        }
    }

    /**
     * Returns the root of the page tree: the node that the /Pages of the
     * catalog names, the catalog that the trailer's /Root names, as MuPDF and
     * pdf.js find it. A PDF can have another tree with no /Parent, before that
     * one: one left from an earlier version of the document, as pdf.js tests
     * it in issue19281, or the pages of an XFA form behind a tree of one page,
     * in xfa_issue13556. Objects with no such catalog, like those of a PDF
     * whose trailer is lost, have the first node with no /Parent as the root.
     *
     * @param objects the page objects.
     * @return the pages object.
     */
    PDFobj getPagesObject(List<PDFobj> objects) {
        for (PDFobj obj : objects) {
            if (obj.root) {
                List<String> tokens = valueOf(obj);
                int i = entryIndex(tokens, "/Pages");
                if (i != -1 && isReference(tokens, i + 1)) {
                    PDFobj pages = PDFobj.objectAt(objects, tokens.get(i + 1));
                    if (pages != null && entryIndex(valueOf(pages), "/Kids") != -1) {
                        return pages;
                    }
                }
                break;
            }
        }
        for (PDFobj obj : objects) {
            if (obj.getValue("/Type").equals("/Pages") && obj.getValue("/Parent").equals("")) {
                return obj;
            }
        }
        return null;
    }

    /**
     * Returns the page objects, each holding the entries it inherits from the
     * page tree: /Resources, /MediaBox, /CropBox and /Rotate, which a PDF can
     * write once on a node above the pages rather than on every page. A page
     * that has an entry of its own keeps it, and the entries are added to the
     * page the first time it is returned, so that getPageSize and
     * getResourcesObject read what the page is.
     *
     * @param objects the objects.
     * @return the page objects.
     */
    public List<PDFobj> getPageObjects(List<PDFobj> objects) {
        List<PDFobj> pages = new ArrayList<PDFobj>();
        PDFobj pagesObject = getPagesObject(objects);
        if (pagesObject != null) {
            getPageObjects(pagesObject, objects, pages, new HashSet<Integer>(),
                    new IdentityHashMap<PDFobj, Map<String, List<String>>>());
        }
        return pages;
    }

    // The nodes of the page tree that were visited are skipped, as a node of a
    // broken tree can list itself or a node above it as a kid. The tree is
    // walked with a list of the kids still to visit, the last of them first,
    // and not by recursion, as a tree of a hundred thousand nodes, one under
    // the other, overflowed the stack.
    private void getPageObjects(
            PDFobj root,
            List<PDFobj> objects,
            List<PDFobj> pages,
            Set<Integer> visited,
            Map<PDFobj, Map<String, List<String>>> nodes) {
        List<PDFobj> stack = new ArrayList<PDFobj>();
        stack.add(root);
        for (boolean first = true; !stack.isEmpty(); first = false) {
            PDFobj obj = stack.remove(stack.size() - 1);
            if (!first && isPageObject(obj)) {  // The root is a node, whatever it is.
                addInheritedEntries(obj, objects, nodes);
                resolveMediaBox(obj, objects);
                nodes.remove(obj);  // A broken tree can have a page as a node.
                pages.add(obj);
                continue;
            }
            if (!visited.add(obj.number)) {
                continue;
            }
            List<Integer> kids = obj.getObjectNumbers("/Kids");
            for (int i = kids.size() - 1; i >= 0; i--) {
                int number = kids.get(i);
                if (number >= 1 && number <= objects.size()) {  // A kid that the document has.
                    stack.add(objects.get(number - 1));
                }
            }
        }
    }

    // Adds to the page the entries it inherits from the page tree and does
    // not have itself. A page of another program's PDF often carries no
    // /MediaBox or /Resources of its own, and read() gives the objects as the
    // file has them, so the page holds what it is only after this.
    private static void addInheritedEntries(
            PDFobj page, List<PDFobj> objects, Map<PDFobj, Map<String, List<String>>> nodes) {
        int open = page.dict.indexOf("<<");
        if (open == -1) {
            return;
        }
        for (String key : INHERITED) {
            if (entryIndex(valueOf(page), key) != -1) {
                continue;       // An entry of its own.
            }
            List<String> value = inheritedValue(page, key, objects, nodes);
            if (value != null) {
                page.dict.addAll(open + 1, value);
                page.dict.add(open + 1, key);
            }
        }
    }

    // Writes the /MediaBox of the page as the array of numbers it is, when it
    // is an object of its own, "/MediaBox 4 0 R", or holds references to its
    // numbers, "[4 0 R 5 0 R 6 0 R 7 0 R]", so that getPageSize reads it. A box
    // that is not four numbers then is left as it is: a reference to anything
    // else, like the page itself, would grow the page each time it is read.
    private static void resolveMediaBox(PDFobj page, List<PDFobj> objects) {
        int open = page.dict.indexOf("<<");
        if (open == -1) {
            return;
        }
        List<String> tokens = page.dict.subList(open, page.dict.size());
        int i = entryIndex(tokens, "/MediaBox");
        if (i == -1) {
            return;
        }
        int start = open + i + 1;
        int end = open + valueEnd(tokens, i + 1);
        List<String> value = page.dict.subList(start, end);
        if (isReference(value, 0)) {
            PDFobj obj = PDFobj.objectNumbered(objects, Integer.parseInt(value.get(0)));
            if (obj == null) {
                return;
            }
            value = valueOf(obj);
        }
        if (value.isEmpty() || !value.get(0).equals("[")) {
            return;
        }
        List<String> box = new ArrayList<String>();
        for (int j = 0; j < value.size(); j++) {
            if (isReference(value, j)) {
                PDFobj obj = PDFobj.objectNumbered(objects, Integer.parseInt(value.get(j)));
                if (obj == null) {
                    return;
                }
                box.addAll(valueOf(obj));
                j += 2;
            } else {
                box.add(value.get(j));
            }
            if (box.size() > 6) {
                return;
            }
        }
        if (box.size() != 6 || !box.get(0).equals("[") || !box.get(5).equals("]")) {
            return;
        }
        for (String number : box.subList(1, 5)) {
            try {
                Double.parseDouble(number);
            } catch (NumberFormatException e) {
                return;
            }
        }
        page.dict.subList(start, end).clear();
        page.dict.addAll(start, box);
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

    // Adds the entries of the /ExtGState dictionary of the resources, which can
    // be an object of its own, with the names that an earlier page did not add.
    private void addExtGStates(PDFobj resources, List<PDFobj> objects) {
        List<String> entries = getResourceEntries(resources, "/ExtGState", objects);
        int i = 0;
        while (i < entries.size()) {
            // The value after the name is a dictionary, a reference or one token.
            int end = i + 1;
            if (end < entries.size() && entries.get(end).equals("<<")) {
                int level = 0;
                do {
                    String token = entries.get(end++);
                    if (token.equals("<<")) {
                        level++;
                    } else if (token.equals(">>")) {
                        level--;
                    }
                } while (level > 0 && end < entries.size());
            } else if (end + 2 < entries.size() && entries.get(end + 2).equals("R")) {
                end += 3;
            } else {
                end = Math.min(end + 1, entries.size());
            }
            if (!importedExtGStates.contains(entries.get(i))) {
                importedExtGStates.addAll(entries.subList(i, end));
            } else {
                checkImportedName(importedExtGStates, entries.subList(i, end));
            }
            i = end;
        }
    }

    private List<PDFobj> getFontObjects(PDFobj resources, List<PDFobj> objects) {
        List<PDFobj> fonts = new ArrayList<PDFobj>();
        // The /Font dictionary holds one "/Name number 0 R" entry per font, and
        // can be an object of its own. Every entry is written to the resources
        // object, so every font it names is collected here.
        List<String> entries = getResourceEntries(resources, "/Font", objects);
        int i = 0;
        while (i < entries.size()) {
            String token = entries.get(i);
            if (token.startsWith("/") && (i + 3) < entries.size() && entries.get(i + 3).equals("R")) {
                // Pages can carry separate resource dictionaries that name the
                // same fonts. They are merged into one /Font dictionary here,
                // so a name that is already present must not be added twice.
                if (!importedFonts.contains(token)) {
                    importedFonts.addAll(entries.subList(i, i + 4));
                    int number = toInteger(entries.get(i + 1));
                    if (number > 0 && number <= objects.size()) {
                        fonts.add(objects.get(number - 1));
                    }
                } else {
                    checkImportedName(importedFonts, entries.subList(i, i + 4));
                }
                i += 4;
                continue;
            }
            importedFonts.add(token);
            i += 1;
        }
        return fonts;
    }

    // Records the mistake when a name that an earlier page added to the
    // resources has another value on this page, the entry: the pages of this
    // document share one resources dictionary, where the name has the value
    // of the first page, and the content of this page, drawn with
    // drawContents, would draw the resource of the first page.
    private void checkImportedName(List<String> imported, List<String> entry) {
        int i = imported.indexOf(entry.get(0));
        if (i == -1 || imported.subList(i + 1, Math.min(i + entry.size(), imported.size()))
                .equals(entry.subList(1, entry.size()))) {
            return;
        }
        fail(new IllegalArgumentException("The pages of the PDF use the name " + entry.get(0)
                + " for different resources, and the pages of this document share one resources dictionary."));
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
            int number = toInteger(dict.get(i));
            if (number < 1 || number > objects.size()) {
                return entries;     // An object that the PDF does not have.
            }
            dict = objects.get(number - 1).getDict();
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
            if (token.charAt(i) < '0' || token.charAt(i) > '9') {
                return false;
            }
        }
        return true;
    }

    /**
     * Returns the numbers of the objects that "number 0 R" references in the
     * tokens refer to. A number that is too large for an int is left out, as
     * in the other ports.
     */
    private List<Integer> getReferences(List<String> tokens) {
        List<Integer> numbers = new ArrayList<Integer>();
        for (int i = 0; i + 2 < tokens.size(); i++) {
            if (tokens.get(i + 2).equals("R")
                    && isInteger(tokens.get(i)) && isInteger(tokens.get(i + 1))) {
                int number = toInteger(tokens.get(i));
                if (number >= 0) {
                    numbers.add(number);
                }
                i += 2;
            }
        }
        return numbers;
    }

    /**
     * Collects the object with the given number and every object it refers to,
     * directly or through other objects, like the color space of an image or
     * the resources of a form XObject. The page tree is not followed. The
     * objects are found with a list of the numbers still to visit, and not by
     * recursion, as a chain of a hundred thousand objects, each referring to
     * the next, overflowed the stack.
     */
    private void addObjectTree(
            int objNumber, List<PDFobj> objects, Set<Integer> numbers, List<PDFobj> resources) {
        List<Integer> stack = new ArrayList<Integer>();
        stack.add(objNumber);
        while (!stack.isEmpty()) {
            int number = stack.remove(stack.size() - 1);
            if (number <= 0 || number > objects.size() || !numbers.add(number)) {
                continue;
            }
            PDFobj obj = objects.get(number - 1);
            String type = obj.getValue("/Type");
            if (obj.dict.isEmpty()
                    || type.equals("/Page") || type.equals("/Pages") || type.equals("/Catalog")) {
                continue;
            }
            resources.add(obj);
            List<Integer> references = getReferences(obj.dict);
            for (int i = references.size() - 1; i >= 0; i--) {
                stack.add(references.get(i));
            }
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
                    addObjectTree(toInteger(entries.get(i + 1)), objects, numbers, resources);
                } else {
                    checkImportedName(importedXObjects, entries.subList(i, i + 4));
                }
                i += 4;
            } else {
                i += 1;
            }
        }
    }

    /**
     * Adds the fonts, images and graphics states used by the pages of a PDF
     * that was read to this document. The objects keep their numbers and are
     * written as they are, so they are added before any font, image or page of
     * this document, and an encrypted PDF cannot take them.
     *
     * @param objects the objects of the PDF that was read.
     * @throws Exception if there is an issue.
     */
    public void addResourceObjects(List<PDFobj> objects) throws Exception {
        List<PDFobj> resources = new ArrayList<PDFobj>();
        Set<Integer> numbers = new HashSet<Integer>();

        checkObjects(objects);
        List<PDFobj> pages = getPageObjects(objects);
        for (PDFobj page : pages) {
            PDFobj resObj = page.getResourcesObject(objects);
            if (resObj == null) {
                continue;       // A page without resources of its own.
            }
            // A font is copied with every object that it refers to: its
            // descriptor and font file, and also the widths, the encoding and
            // the other entries that can be objects of their own.
            for (PDFobj font : getFontObjects(resObj, objects)) {
                addObjectTree(font.number, objects, numbers, resources);
            }
            addXObjects(resObj, objects, numbers, resources);
            addExtGStates(resObj, objects);
            // The /ExtGState entries are copied as they are, so the objects
            // that they refer to have to be copied too.
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
    private void addObjectsToPDF(List<PDFobj> objects) throws Exception {
        for (PDFobj obj : objects) {
            if (obj.dict.isEmpty() && obj.stream == null) {
                // A number that the PDF that was read has no object for stays
                // a free entry of the cross-reference table.
                continue;
            }
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
