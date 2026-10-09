/*
 * PDF.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to create PDF objects that represent PDF documents.
/// </summary>
public sealed class PDF {
    internal List<Font> fonts = new List<Font>();
    internal List<Image> images = new List<Image>();
    internal List<OptionalContentGroup> groups = new List<OptionalContentGroup>();
    internal Dictionary<String, Int32> states = new Dictionary<String, Int32>();
    internal List<Stamp> stamps = new List<Stamp>();
    internal Compliance compliance = Compliance.PDF_1_7;

    // The description of the PDF/UA identification schema, which PDF/A accepts
    // only when the metadata describes it, as ISO 19005-3 6.6.2.3 asks: the
    // metadata of PDF_A_3A_UA_1 names the part of PDF/UA as well as of PDF/A. It
    // goes into the list of extension schemas of a description the document
    // adds, such as that of Factur-X, as the metadata has one list, or into a
    // list of its own when the document adds none.
    private const String PDF_UA_SCHEMA =
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
    private const String PDF_UA_EXTENSION_SCHEMAS =
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
    private static List<String> WithPDFUASchema(List<String> descriptions) {
        List<String> result = new List<String>(descriptions);
        for (int i = 0; i < result.Count; i++) {
            String description = result[i];
            int schemas = description.IndexOf("<pdfaExtension:schemas>", StringComparison.Ordinal);
            int bag = schemas == -1 ? -1 : description.IndexOf("<rdf:Bag>", schemas, StringComparison.Ordinal);
            if (bag == -1) {
                continue;
            }
            int at = bag + "<rdf:Bag>".Length;
            result[i] = description.Substring(0, at) + "\n"
                    + PDF_UA_SCHEMA.Substring(0, PDF_UA_SCHEMA.Length - 1) + description.Substring(at);
            return result;
        }
        return null;
    }

    // Whether the content of the document is tagged, and follows the rules of
    // PDF/UA: that of PDF_UA_1, of the A levels of PDF/A, which ask for tagged
    // content, and of PDF_A_3A_UA_1, which is both.
    internal bool IsTagged() {
#pragma warning disable CS0618 // PDF_A_3A is deprecated, and still supported
        return compliance == Compliance.PDF_UA_1 || compliance == Compliance.PDF_A_1A
                || compliance == Compliance.PDF_A_2A || compliance == Compliance.PDF_A_3A
                || compliance == Compliance.PDF_A_3A_UA_1;
#pragma warning restore CS0618
    }

    // Whether the document is of PDF/A, of any part and level.
    internal bool IsPDFA() {
        return compliance != Compliance.PDF_1_7 && compliance != Compliance.PDF_UA_1;
    }

    // Whether the document is of PDF/A-1, which has no transparency.
    internal bool IsPDFA1() {
        return compliance == Compliance.PDF_A_1A || compliance == Compliance.PDF_A_1B;
    }
    internal Bookmark toc = null;
    // The headings of a tagged document, in the order they are drawn, which
    // its bookmarks are made of when it has none of its own
    internal List<Heading> headings = new List<Heading>();
    internal Encryption encryption = null;

    private int metadataObjNumber = 0;
    private int outputIntentObjNumber = 0;
    private List<Page> pages = new List<Page>();
    private Dictionary<String, Destination> destinations = new Dictionary<String, Destination>();
    // The document ID for the trailer and the XMP metadata: 16 random bytes as
    // 32 hexadecimal digits, so documents made at the same time get different IDs.
    private String uuid = Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    private Stream os = null;
    private readonly List<long> objOffset = new List<long>(); // Required by the xref section
    private String producer = "PDFjet v9.0.3";
    private String title;
    private String author;
    private String subject;
    private String keywords;
    private String creator;
    private String createDate;      // XMP metadata
    private long byteCount = 0;
    private int pagesObjNumber = 0;
    private PageLayout? pageLayout = null;
    private PageMode? pageMode = null;
    private String language = "en-US";
    // The files the document carries, in the order they were added.
    private readonly List<EmbeddedFile> associatedFiles = new List<EmbeddedFile>();
    // The descriptions that a standard of its own asks the metadata to carry.
    private readonly List<String> metadata = new List<String>();
    private List<String> importedFonts = new List<String>();
    private List<String> importedXObjects = new List<String>();
    private List<String> importedExtGStates = new List<String>();
    private Page prevPage = null;
    private bool contentStreamsCompression = true;
    // The structure elements of the pages of the document, in page order.
    // Complete() collects them, so a detached page that was never added has
    // no part in the structure tree.
    private readonly List<StructElement> annotElements = new List<StructElement>();
    // The elements of the Document element, by number.
    private readonly List<int> documentKids = new List<int>();
    // The structure tree root, the parent tree and the Document element take
    // three numbers reserved before the first element is written.
    private int structTreeRootNumber = 0;
    private int parentTreeNumber = 0;
    private int documentElementNumber = 0;

    // The first misuse of the API. The call that finds it throws, and Complete()
    // then refuses to finish the document, as the file would be broken even if
    // the program caught the exception and carried on.
    private String error = null;
    // True after Complete(): the document is written and closed.
    internal bool completed = false;
    // The pages made for this document, added or detached.
    internal int pagesCreated = 0;

    /// <summary>
    /// The default constructor - use when reading PDF files.
    /// </summary>
    public PDF() {
    }

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
    /// <summary>Creates a PDF document that is written to the stream.</summary>
    public PDF(Stream os) : this(os, Compliance.PDF_1_7) {
    }

    /// <summary>
    /// Creates a PDF document with the specified compliance level.
    /// </summary>
    /// <param name="os">the associated output stream.</param>
    /// <param name="compliance">must be: Compliance.PDF_UA_1, Compliance.PDF_A_1A to Compliance.PDF_A_3B, or Compliance.PDF_A_3A_UA_1, which is both PDF/A-3a and PDF/UA-1</param>
    public PDF(Stream os, Compliance compliance) {
        this.os = os;
        this.compliance = compliance;

        // The creation date is in UTC, so the XMP metadata says so with a Z.
        createDate = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + "Z";

        Append("%PDF-1.7\n");
        Append('%');
        Append((byte) 0xF2);
        Append((byte) 0xF3);
        Append((byte) 0xF4);
        Append((byte) 0xF5);
        Append((byte) 0xF6);
        Append(Token.Newline);
    }

    /// <summary>
    /// Sets the PDF/UA or PDF/A compliance of this document, before any font,
    /// image or page is added.
    /// </summary>
    public PDF SetCompliance(Compliance compliance) {
        // ISO 19005 does not allow a PDF/A document to be encrypted, and the
        // encryption is written for the compliance: it grants a PDF/UA
        // document the permission to extract its content for accessibility.
        if (encryption != null && compliance != Compliance.PDF_1_7 && compliance != Compliance.PDF_UA_1) {
            Fail(new InvalidOperationException("A PDF/A document cannot be encrypted."));
        }
        if (encryption != null && compliance != this.compliance) {
            Fail(new InvalidOperationException("Set the compliance before the encryption, which is written for it."));
        }
        // The fonts and the page content are written for the compliance.
        if (compliance != this.compliance && (GetObjNumber() > 0 || pagesCreated > 0)) {
            Fail(new InvalidOperationException("Set the compliance before adding fonts, images or pages to the PDF."));
        }
        this.compliance = compliance;
        return this;
    }

    /// <summary>Returns the PDF document compliance.</summary>
    public Compliance GetCompliance() {
        return compliance;
    }

    /// <summary>
    /// Sets the encryption applied to this document, before any font, image or
    /// page is added.
    /// </summary>
    public PDF SetEncryption(Encryption encryption) {
        // Every object after the encryption dictionary is encrypted.
        if (encryption != null && encryption.GetObjNumber() != GetObjNumber()) {
            Fail(new InvalidOperationException("Set the encryption before adding fonts, images or pages to the PDF."));
        }
        // ISO 19005 does not allow a PDF/A document to be encrypted.
        if (encryption != null && compliance != Compliance.PDF_1_7 && compliance != Compliance.PDF_UA_1) {
            Fail(new InvalidOperationException("A PDF/A document cannot be encrypted."));
        }
        this.encryption = encryption;
        return this;
    }

    // Records the first misuse of the API and throws the exception.
    internal void Fail(Exception e) {
        if (error == null) {
            error = e.Message;
        }
        throw e;
    }

    internal void NewObj() {
        objOffset.Add(byteCount);
        Append(objOffset.Count);
        Append(Token.NewObj);
    }

    internal void EndObj() {
        Append(Token.EndObj);
    }

    internal int GetObjNumber() {
        return objOffset.Count;
    }

    /// <summary>
    /// Records the offset of an object that carries its own number, growing the
    /// table with placeholders for any number that has no object yet.
    /// </summary>
    private void SetObjOffset(int number, long offset) {
        if (number <= 0) {          // No number of its own - just append.
            objOffset.Add(offset);
            return;
        }
        while (objOffset.Count < number) {
            objOffset.Add(0);
        }
        objOffset[number - 1] = offset;
    }

    /// <summary>
    /// Returns the offset as the 10 digits of an entry of the cross-reference
    /// table, which cannot hold an offset of more than 10 digits.
    /// </summary>
    internal static String XrefOffset(long offset) {
        String digits = offset.ToString(CultureInfo.InvariantCulture);
        if (digits.Length > 10) {
            throw new IOException("The PDF is too large for a cross-reference table: "
                    + "an object starts at byte " + digits + ".");
        }
        return new String('0', 10 - digits.Length) + digits;
    }

    internal int AddMetadataObject(String notice, bool fontMetadataObject) {
        StringBuilder sb = new StringBuilder();
        sb.Append("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"\n");
        sb.Append("    x:xmptk=\"Adobe XMP Core 5.4-c005 78.147326, 2012/08/23-13:03:03\">\n");
        sb.Append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");

        if (fontMetadataObject) {
            sb.Append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n");
            sb.Append("<xmpRights:UsageTerms>\n");
            sb.Append("<rdf:Alt>\n");
            sb.Append("<rdf:li xml:lang=\"x-default\">\n");
            // The notice of a font is text, which may hold an ampersand, like
            // that of the Noto fonts of Japanese, Korean and Chinese.
            sb.Append(EscapeXML(notice));
            sb.Append("</rdf:li>\n");
            sb.Append("</rdf:Alt>\n");
            sb.Append("</xmpRights:UsageTerms>\n");
            sb.Append("</rdf:Description>\n");
        } else {
            sb.Append("<rdf:Description rdf:about=\"\"\n");
            sb.Append("    xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n");
            sb.Append("    xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
            sb.Append("    xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
            sb.Append("    xmlns:xapMM=\"http://ns.adobe.com/xap/1.0/mm/\"\n");
            sb.Append("    xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n");
            sb.Append("    xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\">\n");

            sb.Append("    <dc:format>application/pdf</dc:format>\n");
            if (compliance == Compliance.PDF_UA_1) {
                sb.Append("  <pdfuaid:part>1</pdfuaid:part>\n");
            } else if (compliance == Compliance.PDF_A_1A) {
                sb.Append("  <pdfaid:part>1</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_1B) {
                sb.Append("  <pdfaid:part>1</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>B</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_2A) {
                sb.Append("  <pdfaid:part>2</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_2B) {
                sb.Append("  <pdfaid:part>2</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>B</pdfaid:conformance>\n");
#pragma warning disable CS0618 // PDF_A_3A is deprecated, and still written
            } else if (compliance == Compliance.PDF_A_3A) {
#pragma warning restore CS0618
                sb.Append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_3B) {
                sb.Append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>B</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_3A_UA_1) {
                sb.Append("  <pdfuaid:part>1</pdfuaid:part>\n");
                sb.Append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            }

            sb.Append("  <pdf:Producer>");
            sb.Append(producer);
            sb.Append("</pdf:Producer>\n");

            if (title != null) {
                sb.Append("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.Append(EscapeXML(title));
                sb.Append("</rdf:li></rdf:Alt></dc:title>\n");
            }

            if (author != null) {
                sb.Append("  <dc:creator><rdf:Seq><rdf:li>");
                sb.Append(EscapeXML(author));
                sb.Append("</rdf:li></rdf:Seq></dc:creator>\n");
            }

            if (subject != null) {
                sb.Append("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.Append(EscapeXML(subject));
                sb.Append("</rdf:li></rdf:Alt></dc:description>\n");
            }

            if (keywords != null) {
                sb.Append("  <pdf:Keywords>");
                sb.Append(EscapeXML(keywords));
                sb.Append("</pdf:Keywords>\n");
            }

            if (creator != null) {
                sb.Append("  <xmp:CreatorTool>");
                sb.Append(EscapeXML(creator));
                sb.Append("</xmp:CreatorTool>\n");
            }

            sb.Append("  <xmp:CreateDate>");
            sb.Append(createDate);
            sb.Append("</xmp:CreateDate>\n");

            sb.Append("  <xapMM:DocumentID>uuid:");
            sb.Append(uuid);
            sb.Append("</xapMM:DocumentID>\n");

            sb.Append("  <xapMM:InstanceID>uuid:");
            sb.Append(uuid);
            sb.Append("</xapMM:InstanceID>\n");

            sb.Append("</rdf:Description>\n");

            List<String> descriptions = metadata;
            if (compliance == Compliance.PDF_A_3A_UA_1) {
                descriptions = WithPDFUASchema(metadata);
                if (descriptions == null) {
                    descriptions = metadata;
                    sb.Append(PDF_UA_EXTENSION_SCHEMAS);
                }
            }

            foreach (String description in descriptions) {
                sb.Append(description);
                sb.Append("\n");
            }
        }

        if (!fontMetadataObject) {
            // Add the recommended 2000 bytes padding
            for (int i = 0; i < 20; i++) {
                for (int j = 0; j < 10; j++) {
                    sb.Append("          ");
                }
                sb.Append("\n");
            }
        }

        sb.Append("</rdf:RDF>\n");
        sb.Append("</x:xmpmeta>\n");
        sb.Append("<?xpacket end=\"w\"?>");

        // The metadata is encrypted like every other stream, and the
        // encryption dictionary says so with /EncryptMetadata true. Readers do
        // not agree on which metadata streams to leave alone when it is false.
        byte[] xml = (new System.Text.UTF8Encoding()).GetBytes(sb.ToString());
        if (encryption != null) {
            xml = AES256.Encrypt(xml, encryption.GetKey());
        }

        // This is the metadata object
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /Metadata\n");
        Append("/Subtype /XML\n");
        Append("/Length ");
        Append(xml.Length);
        Append(Token.Newline);
        Append(Token.EndDictionary);
        Append(Token.Stream);
        Append(xml, 0, xml.Length);
        Append(Token.EndStream);
        EndObj();

        return GetObjNumber();
    }

    // Returns the text with the characters that have a meaning in XML escaped,
    // and without what XML does not allow, which CleanText leaves out.
    internal static String EscapeXML(String text) {
        String clean = CleanText(text);
        StringBuilder sb = new StringBuilder(clean.Length);
        foreach (char ch in clean) {
            if (ch == '&') {
                sb.Append("&amp;");
            } else if (ch == '<') {
                sb.Append("&lt;");
            } else if (ch == '>') {
                sb.Append("&gt;");
            } else {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    // Returns the text without what XML does not allow: the control
    // characters other than tab, line feed and carriage return, U+FFFE,
    // U+FFFF and unpaired surrogates, which would make the metadata
    // unreadable. The title and the other properties of the document are
    // cleaned when they are set, so that the information dictionary says what
    // the metadata says, as PDF/A asks.
    internal static String CleanText(String text) {
        if (text == null) {
            return null;
        }
        StringBuilder sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++) {
            char ch = text[i];
            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) {
                sb.Append(ch);
                sb.Append(text[++i]);
            } else if (ch == '\t' || ch == '\n' || ch == '\r'
                    || (ch >= 0x20 && ch <= 0xFFFD && !char.IsSurrogate(ch))) {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    private int AddOutputIntentObject() {
        byte[] profile = ICCBlackScaled.profile;
        if (encryption != null) {
            profile = AES256.Encrypt(profile, encryption.GetKey());
        }

        NewObj();
        Append(Token.BeginDictionary);
        Append("/N 3\n");

        Append("/Length ");
        Append(profile.Length);
        Append("\n");

        Append("/Filter /FlateDecode\n");
        Append(Token.EndDictionary);
        Append(Token.Stream);
        Append(profile, 0, profile.Length);
        Append(Token.EndStream);
        EndObj();

        byte[] identifierBytes = Encoding.UTF8.GetBytes("sRGB IEC61966-2.1");
        if (encryption != null) {
            identifierBytes = AES256.Encrypt(identifierBytes, encryption.GetKey());
        }
        // OutputIntent object
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /OutputIntent\n");
        Append("/S /GTS_PDFA1\n");

        Append("/OutputCondition <");
        Append(Util.ToHexString(identifierBytes));
        Append(">\n");

        Append("/OutputConditionIdentifier <");
        Append(Util.ToHexString(identifierBytes));
        Append(">\n");

        Append("/Info <");
        Append(Util.ToHexString(identifierBytes));
        Append(">\n");

        Append("/DestOutputProfile ");
        Append(GetObjNumber() - 1);
        Append(Token.ObjRef);
        Append(Token.EndDictionary);
        EndObj();

        return GetObjNumber();
    }

    // Appends a token of a PDF that was read. Each of its characters is a byte
    // of the PDF, so a string or name with bytes of 0x80 or more is copied
    // unchanged, where UTF-8 would write two bytes for each of them.
    private void AppendToken(String token) {
        Append(Encoding.Latin1.GetBytes(token));
    }

    /// <summary>
    /// Writes the "/Name number 0 R" entries collected from a PDF that was read.
    /// </summary>
    private void AppendImportedEntries(List<String> tokens) {
        foreach (String token in tokens) {
            AppendToken(token);
            if (token.Equals("R")) {
                Append(Token.Newline);
            } else {
                Append(Token.Space);
            }
        }
    }

    private int AddResourcesObject() {
        NewObj();
        Append(Token.BeginDictionary);
        if (fonts.Count > 0 || importedFonts.Count > 0) {
            Append("/Font\n");
            Append(Token.BeginDictionary);
            AppendImportedEntries(importedFonts);
            foreach (Font font in fonts) {
                Append("/F");
                Append(font.objNumber);
                Append(Token.Space);
                Append(font.objNumber);
                Append(Token.ObjRef);
            }
            Append(Token.EndDictionary);
        }

        if (images.Count > 0 || stamps.Count > 0 || importedXObjects.Count > 0) {
            Append("/XObject\n");
            Append(Token.BeginDictionary);
            AppendImportedEntries(importedXObjects);
            foreach (Image image in images) {
                Append("/Im");
                Append(image.objNumber);
                Append(' ');
                Append(image.objNumber);
                Append(" 0 R\n");
            }
            foreach (Stamp stamp in stamps) {
                Append("/Fm");
                Append(stamp.objNumber);
                Append(' ');
                Append(stamp.objNumber);
                Append(" 0 R\n");
            }
            Append(Token.EndDictionary);
        }

        if (groups.Count > 0) {
            Append("/Properties\n");
            Append(Token.BeginDictionary);
            for (int i = 0; i < groups.Count; i++) {
                OptionalContentGroup ocg = groups[i];
                Append("/OC");
                Append(i + 1);
                Append(Token.Space);
                Append(ocg.objNumber);
                Append(Token.ObjRef);
            }
            Append(Token.EndDictionary);
        }
        // The graphics states of a PDF that was read and those of the pages
        // go in the same dictionary.
        if (states.Count > 0 || importedExtGStates.Count > 0) {
            Append("/ExtGState <<\n");
            AppendImportedEntries(importedExtGStates);
            List<KeyValuePair<String, Int32>> entries =
                    new List<KeyValuePair<String, Int32>>(states);
            entries.Sort(delegate(KeyValuePair<String, Int32> e1,
                    KeyValuePair<String, Int32> e2) {
                return e1.Value.CompareTo(e2.Value);
            });
            foreach (KeyValuePair<String, Int32> entry in entries) {
                Append("/GS");
                Append(entry.Value);
                Append(" <<");
                Append(entry.Key);
                Append(Token.EndDictionary);
            }
            Append(Token.EndDictionary);
        }
        Append(Token.EndDictionary);
        EndObj();
        return GetObjNumber();
    }

    private void AddPagesObject() {
        SetObjOffset(pagesObjNumber, byteCount);
        Append(pagesObjNumber);
        Append(Token.NewObj);
        Append(Token.BeginDictionary);
        Append("/Type /Pages\n");
        Append("/Kids [\n");
        foreach (Page page in pages) {
            Append(page.objNumber);
            Append(" 0 R\n");
        }
        Append("]\n");
        Append("/Count ");
        Append(pages.Count);
        Append(Token.Newline);
        Append(Token.EndDictionary);
        EndObj();
    }

    // Reserves the numbers of the structure tree root, the parent tree and
    // the Document element, which the elements written with their pages refer
    // to before the three are written.
    internal void ReserveStructTreeNumbers() {
        if (structTreeRootNumber == 0) {
            structTreeRootNumber = ReserveObjNumber();
            parentTreeNumber = ReserveObjNumber();
            documentElementNumber = ReserveObjNumber();
        }
    }

    private int AddStructTreeRootObject() {
        SetObjOffset(structTreeRootNumber, byteCount);
        Append(structTreeRootNumber);
        Append(Token.NewObj);
        Append(Token.BeginDictionary);
        Append("/Type /StructTreeRoot\n");
        Append("/ParentTree ");
        Append(parentTreeNumber);
        Append(Token.ObjRef);
        Append("/K [\n");
        Append(documentElementNumber);
        Append(Token.ObjRef);
        Append("]\n");
        // The types of PDF 2.0 that the document uses, which PDF 1.7 does not
        // have, are mapped to the standard types that PDF/UA-1 knows.
        if (roles.Count > 0) {
            Append("/RoleMap <<");
            foreach (String[] role in RoleMapOfCompliance()) {
                if (roles.Contains(role[0])) {
                    Append(" /");
                    Append(role[0]);
                    Append(" /");
                    Append(role[1]);
                }
            }
            Append(" >>\n");
        }
        Append(Token.EndDictionary);
        EndObj();
        return structTreeRootNumber;
    }

    // The structure types of PDF 2.0 that PDFjet writes, mapped to the
    // standard types of PDF 1.7, in the order the role map lists them.
    private static readonly String[][] ROLE_MAP = {
        new String[] {StructElem.TITLE.Type(), StructElem.P.Type()},
        new String[] {StructElem.EM.Type(), StructElem.SPAN.Type()},
        new String[] {StructElem.STRONG.Type(), StructElem.SPAN.Type()},
    };
    // The types that PDF 1.5 brought, which PDF/A-1, of PDF 1.4, does not
    // know: an annotation's element is mapped to a Span, which its reference
    // to the annotation stays in (veraPDF, PDF/A-1 6.8.3.4).
    private static readonly String[][] ROLE_MAP_OF_PDF_A_1 = {
        new String[] {StructElem.TITLE.Type(), StructElem.P.Type()},
        new String[] {StructElem.EM.Type(), StructElem.SPAN.Type()},
        new String[] {StructElem.STRONG.Type(), StructElem.SPAN.Type()},
        new String[] {StructElem.ANNOT.Type(), StructElem.SPAN.Type()},
    };

    // Returns the role map of the document's compliance.
    private String[][] RoleMapOfCompliance() {
        return (compliance == Compliance.PDF_A_1A || compliance == Compliance.PDF_A_1B)
                ? ROLE_MAP_OF_PDF_A_1 : ROLE_MAP;
    }
    // The types of PDF 2.0 among the elements, which the role map maps.
    private readonly HashSet<String> roles = new HashSet<String>();

    private int AddStructDocumentObject(int parent) {
        SetObjOffset(documentElementNumber, byteCount);
        Append(documentElementNumber);
        Append(Token.NewObj);
        Append(Token.BeginDictionary);
        Append("/Type /StructElem\n");
        Append("/S /Document\n");
        Append("/P ");
        Append(parent);
        Append(Token.ObjRef);
        Append("/K [\n");
        foreach (int number in this.documentKids) {
            Append(number);
            Append(" 0 R\n");
        }
        Append("]\n");
        Append(Token.EndDictionary);
        EndObj();
        return documentElementNumber;
    }

    // Writes the structure elements of a page that is written, so that a
    // document of many pages holds no more of them than the page it is
    // drawing. What it keeps is the elements that are still open and the ones
    // of an annotation, whose object is written when the document is completed.
    private void AddPageStructElements(Page page) {
        if (!IsTagged() || page.structures.Count == 0) {
            return;
        }
        List<StructElement> kept = new List<StructElement>();
        foreach (StructElement element in page.structures) {
            // The element of an annotation alone has no marked content, its
            // mcid -1; the Link of a text has the marked content of the text
            // too. The Link elements among the words of a paragraph are the
            // negative entries of its mcids, which are not marked content.
            List<int> mcids = new List<int>();
            foreach (int mcid in element.mcids) {
                if (mcid >= 0) {
                    mcids.Add(mcid);
                }
            }
            if (element.mcid >= 0) {
                mcids.Add(element.mcid);
            }
            foreach (int mcid in mcids) {
                while (page.mcidNumbers.Count <= mcid) {
                    page.mcidNumbers.Add(0);
                }
                page.mcidNumbers[mcid] = element.objNumber;
            }
            if (element.parent == null) {
                documentKids.Add(element.objNumber);
            }
            if (element.annotation != null) {
                annotElements.Add(element);
            }
            if (element.open || element.annotation != null) {
                kept.Add(element);
                continue;
            }
            AddStructElementObject(element);
        }
        page.structures = kept;
    }

    // Writes one structure element, under the number it was given when it was
    // made.
    private void AddStructElementObject(StructElement element) {
        {
            SetObjOffset(element.objNumber, byteCount);
            Append(element.objNumber);
            Append(Token.NewObj);
            foreach (String[] role in RoleMapOfCompliance()) {
                if (role[0].Equals(element.structure)) {
                    roles.Add(role[0]);
                }
            }
            Append("<<\n/Type /StructElem /S /");
            Append(element.structure);
            Append("\n/P ");
            if (element.parent != null) {
                Append(element.parent.objNumber);
            } else {
                Append(documentElementNumber);
            }
            Append(" 0 R /Pg ");
            Append(element.pageObjNumber);
            Append(Token.ObjRef);

            if (element.annotation != null) {
                // A link: the marked content of its text, or the elements it
                // holds, like the Figure of an image, then its annotation
                Append("/K [");
                if (element.mcid >= 0) {
                    Append(element.mcid);
                    Append(" ");
                }
                foreach (int kid in element.kids) {
                    Append(kid);
                    Append(" 0 R ");
                }
                Append("<</Type /OBJR /Obj ");
                Append(element.annotation.objNumber);
                Append(" 0 R>>]\n");
            } else if (element.mcid >= 0) {
                Append("/K ");
                Append(element.mcid);
                Append("\n");
            } else if (element.mcids.Count > 0) {
                // The marked contents of a paragraph drawn word by word, and
                // the Link elements of its words that are links
                Append("/K [");
                for (int i = 0; i < element.mcids.Count; i++) {
                    if (i > 0) {
                        Append(Token.Space);
                    }
                    int mcid = element.mcids[i];
                    if (mcid < 0) {
                        Append(-mcid);
                        Append(" 0 R");
                    } else {
                        Append(mcid);
                    }
                }
                Append("]\n");
            } else if (element.kids.Count > 0) {
                Append("/K [");
                foreach (int kid in element.kids) {
                    Append(kid);
                    Append(" 0 R ");
                }
                Append("]\n");
            }

            String attributes = element.attributes;
            if (StructElement.PlacedAsBlock(element)) {
                attributes = StructElement.WithPlacementBlock(attributes);
            }
            if (attributes != null) {
                Append("/A ");
                Append(attributes);
                Append("\n");
            }

            // The actual text is written only with an alternate description,
            // since a text block and a text box pass the text they draw as the
            // actual text without one.
            bool hasAltDescription = !String.IsNullOrEmpty(element.altDescription);
            bool hasActualText = hasAltDescription && !String.IsNullOrEmpty(element.actualText);
            String language = element.language;
            if (String.IsNullOrEmpty(language) && hasAltDescription) {
                language = this.language;
            }

            if (!String.IsNullOrEmpty(language)) {
                byte[] languageBytes = Encoding.UTF8.GetBytes(language);
                if (encryption != null) {
                    languageBytes = AES256.Encrypt(languageBytes, encryption.GetKey());
                }
                Append("/Lang <");
                Append(Util.ToHexString(languageBytes));
                Append(">\n");
            }

            if (hasActualText) {
                Append("/ActualText ");
                AppendTextString(element.actualText);
                Append("\n");
            }

            if (hasAltDescription) {
                Append("/Alt ");
                AppendTextString(element.altDescription);
                Append("\n");
            }

            Append(">>\n");
            EndObj();
        }
    }

    private void AddNumsParentTree() {
        SetObjOffset(parentTreeNumber, byteCount);
        Append(parentTreeNumber);
        Append(Token.NewObj);
        Append(Token.BeginDictionary);
        Append("/Nums [\n");
        // The keys must be listed in increasing order, so the page entries -
        // whose keys are the /StructParents values 0 .. pages.Count-1 - come
        // first. Each value is the array of struct elements of that page,
        // indexed by the MCID they were marked with.
        for (int i = 0; i < pages.Count; i++) {
            Append(i);
            Append(" [");
            foreach (int number in pages[i].mcidNumbers) {
                Append(Token.Space);
                Append(number);
                Append(" 0 R");
            }
            Append("]\n");
            pages[i].mcidNumbers = new List<int>();
        }
        // The annotations follow, keyed by the /StructParent values handed out
        // by AddAnnotDictionaries(), which continue where the pages left off.
        int structParent = pages.Count;
        foreach (StructElement element in this.annotElements) {
            if (element.annotation != null) {
                Append(structParent++);
                Append(Token.Space);
                Append(element.objNumber);
                Append(Token.ObjRef);
            }
        }
        Append("]\n");
        Append(Token.EndDictionary);
        EndObj();
    }

    // Adds the document information dictionary, which readers like pdfinfo
    // show. It says what the XMP metadata of PDF/A and PDF/UA documents says.
    private int AddInfoObject() {
        NewObj();
        Append(Token.BeginDictionary);
        AppendInfoText("/Title", title);
        AppendInfoText("/Author", author);
        AppendInfoText("/Subject", subject);
        AppendInfoText("/Keywords", keywords);
        AppendInfoText("/Creator", creator);
        AppendInfoText("/Producer", producer);
        AppendInfoString("/CreationDate", Encoding.ASCII.GetBytes(GetDate()));
        Append(Token.EndDictionary);
        EndObj();
        return GetObjNumber();
    }

    // The moment the document was made, as a date string of PDF: the XMP
    // creation date 2026-01-31T12:00:00Z is D:20260131120000Z.
    internal String GetDate() {
        return "D:" + createDate.Replace("-", "").Replace("T", "").Replace(":", "");
    }

    // Appends an entry of the information dictionary with the text, unless
    // the text is null.
    private void AppendInfoText(String key, String text) {
        if (text != null) {
            Append(key);
            Append(Token.Space);
            AppendTextString(text);
            Append(Token.Newline);
        }
    }

    /// <summary>
    /// Appends a text string, like a bookmark title or an alternate description:
    /// UTF-16BE with a byte order mark in hexadecimal, encrypted if the document
    /// is encrypted. A text string without the mark is in PDFDocEncoding, so
    /// UTF-8 bytes would show as two or three wrong characters each.
    /// </summary>
    internal void AppendTextString(String text) {
        byte[] bytes = Encoding.BigEndianUnicode.GetBytes("\uFEFF" + text);
        if (encryption != null) {
            bytes = AES256.Encrypt(bytes, encryption.GetKey());
        }
        Append('<');
        Append(Util.ToHexString(bytes));
        Append('>');
    }

    // Appends an entry of the information dictionary with the bytes of a
    // string, encrypted if the document is encrypted.
    private void AppendInfoString(String key, byte[] bytes) {
        Append(key);
        Append(Token.Space);
        AppendByteString(bytes);
        Append(Token.Newline);
    }

    // Appends a string of bytes, like a date, in hexadecimal, encrypted if the
    // document is encrypted.
    internal void AppendByteString(byte[] bytes) {
        if (encryption != null) {
            bytes = AES256.Encrypt(bytes, encryption.GetKey());
        }
        Append('<');
        Append(Util.ToHexString(bytes));
        Append('>');
    }

    private int AddRootObject(int structTreeRootObjNumber, int outlineDictNum) {
        // Add the root object
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /Catalog\n");

        if (compliance != Compliance.PDF_1_7) {
            byte[] languageBytes = Encoding.UTF8.GetBytes(this.language);
            if (encryption != null) {
                languageBytes = AES256.Encrypt(languageBytes, encryption.GetKey());
            }
            Append("/Lang <");
            Append(Util.ToHexString(languageBytes));
            Append(">\n");

            // Only a tagged document has a structure tree: a PDF/A of level B
            // that said it was marked would say its content is tagged, which
            // it is not.
            if (IsTagged()) {
                Append("/StructTreeRoot ");
                Append(structTreeRootObjNumber);
                Append(" 0 R\n");

                Append("/MarkInfo <</Marked true>>\n");
            }
            Append("/ViewerPreferences <</DisplayDocTitle true>>\n");
        }

        if (pageLayout != null) {
            Append("/PageLayout /");
            Append(pageLayout.Value.ToPDFName());
            Append(Token.Newline);
        }

        if (pageMode != null) {
            Append("/PageMode /");
            Append(pageMode.Value.ToPDFName());
            Append(Token.Newline);
        }

        AddOCProperties();

        Append("/Pages ");
        Append(pagesObjNumber);
        Append(" 0 R\n");

        AddAssociatedFiles();

        if (compliance != Compliance.PDF_1_7) {
            Append("/Metadata ");
            Append(metadataObjNumber);
            Append(" 0 R\n");

            Append("/OutputIntents [");
            Append(outputIntentObjNumber);
            Append(" 0 R]\n");
        }

        if (outlineDictNum > 0) {
            Append("/Outlines ");
            Append(outlineDictNum);
            Append(" 0 R\n");
        }

        Append(Token.EndDictionary);
        EndObj();
        return GetObjNumber();
    }

    // The files the document carries: /AF says which they are and how each of
    // them relates to the document, and the name tree of /EmbeddedFiles is
    // where a reader of the document looks for a file by its name.
    private void AddAssociatedFiles() {
        if (associatedFiles.Count == 0) {
            return;
        }
        Append("/AF [");
        for (int i = 0; i < associatedFiles.Count; i++) {
            if (i > 0) {
                Append(Token.Space);
            }
            Append(associatedFiles[i].objNumber);
            Append(" 0 R");
        }
        Append("]\n");

        // The names of a name tree are in order, which here means the order of
        // the characters of the names. The tree is one node, as a document
        // carries few files.
        // Two files of the same name keep the order they were added in, as
        // the sort of Java keeps it and the sort of C# does not.
        List<EmbeddedFile> sorted = new List<EmbeddedFile>(associatedFiles);
        sorted.Sort((file1, file2) => {
            int order = String.CompareOrdinal(file1.fileName, file2.fileName);
            return (order != 0) ? order
                    : associatedFiles.IndexOf(file1) - associatedFiles.IndexOf(file2);
        });
        Append("/Names <</EmbeddedFiles <</Names [");
        foreach (EmbeddedFile file in sorted) {
            AppendTextString(file.fileName);
            Append(Token.Space);
            Append(file.objNumber);
            Append(" 0 R");
        }
        Append("]>>>>\n");
    }

    private void AddPageBox(String boxName, Page page, float[] rect) {
        Append("/");
        Append(boxName);
        Append(" [");
        Append(rect[0]);
        Append(Token.Space);
        Append(page.height - rect[3]);
        Append(Token.Space);
        Append(rect[2]);
        Append(Token.Space);
        Append(page.height - rect[1]);
        Append("]\n");
    }

    // Gives every destination the object number of the page it is on, which
    // the page was given when it was added.
    private void SetDestinationObjNumbers() {
        foreach (Page page in pages) {
            foreach (Destination destination in page.destinations) {
                destination.pageObjNumber = page.objNumber;
                destinations[destination.name] = destination;
            }
        }
    }

    private void AddAllPages(int resObjNumber) {
        SetDestinationObjNumbers();
        AddAnnotDictionaries();
        pagesObjNumber = ReserveObjNumber();

        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            if (page.mergedDict != null) {
                List<String> dict = new List<String>(page.mergedDict);
                SetEntry(dict, "/Parent", new List<String> { pagesObjNumber.ToString(CultureInfo.InvariantCulture), "0", "R" });
                SetObjOffset(page.objNumber, byteCount);
                Append(page.objNumber);
                Append(Token.NewObj);
                AppendTokens(dict);
                Append(Token.Newline);
                Append(Token.EndObj);
                continue;
            }

            // Page object, under the number it was given when it was added.
            SetObjOffset(page.objNumber, byteCount);
            Append(page.objNumber);
            Append(Token.NewObj);
            Append(Token.BeginDictionary);
            Append("/Type /Page\n");
            Append("/Parent ");
            Append(pagesObjNumber);
            Append(" 0 R\n");
            Append("/MediaBox [0 0 ");
            Append(page.width);
            Append(' ');
            Append(page.height);
            Append("]\n");

            if (page.rotateDegrees != 0f) {
                Append("/Rotate ");
                Append(page.rotateDegrees);
                Append("\n");
            }

            if (page.cropBox != null) {
                AddPageBox("CropBox", page, page.cropBox);
            }
            if (page.bleedBox != null) {
                AddPageBox("BleedBox", page, page.bleedBox);
            }
            if (page.trimBox != null) {
                AddPageBox("TrimBox", page, page.trimBox);
            }
            if (page.artBox != null) {
                AddPageBox("ArtBox", page, page.artBox);
            }

            Append("/Resources ");
            Append(resObjNumber);
            Append(" 0 R\n");
            Append("/Contents [ ");
            foreach (Int32 n in page.contents) {
                Append(n);
                Append(" 0 R ");
            }
            Append("]\n");
            if (page.annots.Count > 0) {
                Append("/Annots [ ");
                foreach (Annotation annot in page.annots) {
                    Append(annot.objNumber);
                    Append(" 0 R ");
                }
                Append("]\n");
            }

            if (IsTagged()) {
                Append("/Tabs /S\n");
                Append("/StructParents ");
                Append(i);
                Append(Token.Newline);
            }

            Append(Token.EndDictionary);
            EndObj();
        }
    }

    private void AddPageContent(Page page) {
        page.CheckBalanced();
        if (contentStreamsCompression) {
            // The content, without a copy of it
            byte[] buf = Compressor.Deflate(page.buf.GetBuffer(), 0, (int) page.buf.Length);
            if (encryption != null) {
                buf = AES256.Encrypt(buf, encryption.GetKey());
            }
            page.buf = new Page.WrittenContent(this);  // Release the page content memory!

            NewObj();
            Append(Token.BeginDictionary);
            Append("/Filter /FlateDecode\n");
            Append("/Length ");
            Append(buf.Length);
            Append(Token.Newline);
            Append(Token.EndDictionary);
            Append(Token.Stream);
            Append(buf);
            Append(Token.EndStream);
            EndObj();
            page.contents.Add(GetObjNumber());
        } else {    // No compression. Used for diagnostics
            byte[] buf = page.buf.ToArray();
            if (encryption != null) {
                buf = AES256.Encrypt(buf, encryption.GetKey());
            }
            page.buf = new Page.WrittenContent(this);  // Release the page content memory!

            NewObj();
            Append(Token.BeginDictionary);
            Append("/Length ");
            Append(buf.Length);
            Append(Token.Newline);
            Append(Token.EndDictionary);
            Append(Token.Stream);
            Append(buf);
            Append(Token.EndStream);
            EndObj();
            page.contents.Add(GetObjNumber());
        }
        AddPageStructElements(page);
    }

    // Writes the appearance of an annotation that is not a link and returns
    // its object number. It draws what a viewer draws for the annotation: the
    // square, the circle or the polygon in its fill color, and a note or a file
    // as its icon in a white box with a black frame. Its box is the rectangle
    // of the annotation, from its lower left corner to its upper right one, in
    // the coordinates of the page.
    private int AddAppearanceObject(Annotation annot, float minX, float minY, float maxX, float maxY) {
        float w = maxX - minX;
        float h = maxY - minY;
        MemoryStream buf = new MemoryStream();
        String type = annot.annotationType;
        // A shape that is not opaque is drawn with its opacity, which a viewer
        // does not apply to an appearance of its own. PDF/A-1 has no
        // transparency, so there the shape is drawn opaque.
        float opacity = IsPDFA1() ? 1f : annot.opacity;
        bool transparent = (type.Equals(Annotation.Square) ||
                type.Equals(Annotation.Circle) || type.Equals(Annotation.Polygon)) &&
                opacity < 1f;
        if (type.Equals(Annotation.Square)) {
            AppendFill(buf, annot, transparent);
            AppendNumbers(buf, minX, minY, w, h);
            AppendAscii(buf, "re f\n");
        } else if (type.Equals(Annotation.Circle)) {
            // Four Bezier curves, one for each quarter of the ellipse.
            const float kappa = 0.55228475f;
            float rx = w / 2;
            float ry = h / 2;
            float cx = minX + rx;
            float cy = minY + ry;
            float ox = rx * kappa;
            float oy = ry * kappa;
            AppendFill(buf, annot, transparent);
            AppendNumbers(buf, cx + rx, cy);
            AppendAscii(buf, "m\n");
            AppendNumbers(buf, cx + rx, cy + oy, cx + ox, cy + ry, cx, cy + ry);
            AppendAscii(buf, "c\n");
            AppendNumbers(buf, cx - ox, cy + ry, cx - rx, cy + oy, cx - rx, cy);
            AppendAscii(buf, "c\n");
            AppendNumbers(buf, cx - rx, cy - oy, cx - ox, cy - ry, cx, cy - ry);
            AppendAscii(buf, "c\n");
            AppendNumbers(buf, cx + ox, cy - ry, cx + rx, cy - oy, cx + rx, cy);
            AppendAscii(buf, "c\n");
            AppendAscii(buf, "f\n");
        } else if (type.Equals(Annotation.Polygon)) {
            AppendFill(buf, annot, transparent);
            for (int i = 0; i + 1 < annot.vertices.Length; i += 2) {
                AppendNumbers(buf, annot.x1 + annot.vertices[i], annot.y1 - annot.vertices[i + 1]);
                AppendAscii(buf, (i == 0) ? "m\n" : "l\n");
            }
            AppendAscii(buf, "h f\n");
        } else {
            AppendAscii(buf, "1 g 0 G 0.5 w\n");
            AppendNumbers(buf, minX + 0.25f, minY + 0.25f, w - 0.5f, h - 0.5f);
            AppendAscii(buf, "re B\n");
            // The icon, in a box of 1 by 1 scaled to the rectangle
            String icon = Annotation.NoteIcon;
            if (annot.fileAttachment != null) {
                icon = annot.fileAttachment.icon.Equals("Paperclip") ?
                        Annotation.PaperclipIcon : Annotation.PushPinIcon;
            }
            AppendAscii(buf, "q\n");
            AppendNumbers(buf, w);
            AppendAscii(buf, "0 0 ");
            AppendNumbers(buf, h, minX, minY);
            AppendAscii(buf, "cm\n");
            AppendAscii(buf, icon);
            AppendAscii(buf, "Q\n");
        }
        byte[] content = buf.ToArray();
        if (encryption != null) {
            content = AES256.Encrypt(content, encryption.GetKey());
        }

        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /XObject\n");
        Append("/Subtype /Form\n");
        Append("/BBox [");
        Append(minX);
        Append(' ');
        Append(minY);
        Append(' ');
        Append(maxX);
        Append(' ');
        Append(maxY);
        Append("]\n");
        if (transparent) {
            Append("/Resources <</ExtGState <</GS0 <</CA ");
            Append(opacity);
            Append(" /ca ");
            Append(opacity);
            Append(">>>>>>\n");
        }
        Append("/Length ");
        Append(content.Length);
        Append(Token.Newline);
        Append(Token.EndDictionary);
        Append("stream\n");
        Append(content);
        Append("\nendstream\n");
        EndObj();
        return GetObjNumber();
    }

    // Appends the fill color of the annotation to the content of its appearance.
    private static void AppendFill(MemoryStream buf, Annotation annot, bool transparent) {
        if (transparent) {
            AppendAscii(buf, "/GS0 gs\n");
        }
        AppendNumbers(buf, annot.fillColor[0], annot.fillColor[1], annot.fillColor[2]);
        AppendAscii(buf, "rg\n");
    }

    // Appends the numbers to the content of an appearance, each followed by a space.
    private static void AppendNumbers(MemoryStream buf, params float[] values) {
        foreach (float value in values) {
            byte[] number = FastFloat.ToByteArray(value);
            buf.Write(number, 0, number.Length);
            buf.WriteByte((byte) ' ');
        }
    }

    private static void AppendAscii(MemoryStream buf, String text) {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        buf.Write(bytes, 0, bytes.Length);
    }

    private int AddAnnotationObject(Annotation annot, int index) {
        // The rectangle of an annotation of vertices, a polygon, is the box of its
        // vertices, which are relative to its location; its second corner is not
        // set
        float x1 = annot.x1, y1 = annot.y1, x2 = annot.x2, y2 = annot.y2;
        if (annot.vertices != null && annot.vertices.Length >= 2) {
            float minX = annot.vertices[0], maxX = annot.vertices[0];
            float minY = annot.vertices[1], maxY = annot.vertices[1];
            for (int i = 2; i + 1 < annot.vertices.Length; i += 2) {
                minX = Math.Min(minX, annot.vertices[i]);
                maxX = Math.Max(maxX, annot.vertices[i]);
                minY = Math.Min(minY, annot.vertices[i + 1]);
                maxY = Math.Max(maxY, annot.vertices[i + 1]);
            }
            x1 = annot.x1 + minX;
            y1 = annot.y1 - maxY;
            x2 = annot.x1 + maxX;
            y2 = annot.y1 - minY;
        }
        // The rectangle is written from its lower left corner to its upper
        // right one, as viewers expect: PDFium draws the icon of a note at its
        // first y.
        float left = Math.Min(x1, x2), bottom = Math.Min(y1, y2);
        x2 = Math.Max(x1, x2);
        y2 = Math.Max(y1, y2);
        x1 = left;
        y1 = bottom;

        // Every annotation but a link has an appearance of its own, which PDF/A
        // asks for, and without which each viewer draws it its own way, or not
        // at all: PDFium draws no polygon and no file attachment. It is written
        // before the annotation.
        int appearance = 0;
        if (!annot.annotationType.Equals(Annotation.Link)) {
            appearance = AddAppearanceObject(annot, x1, y1, x2, y2);
        }

        NewObj();
        annot.objNumber = GetObjNumber();
        Append(Token.BeginDictionary);
        Append("/Type /Annot\n");
        Append("/Subtype /");
        Append(annot.annotationType);
        Append("\n");
        Append("/Rect [");
        Append(x1);
        Append(' ');
        Append(y1);
        Append(' ');
        Append(x2);
        Append(' ');
        Append(y2);
        Append("]\n");
        Append("/Border [0 0 0]\n");
        // Every annotation is printed, as PDF/A asks.
        Append("/F 4\n");
        if (appearance > 0) {
            Append("/AP <</N ");
            Append(appearance);
            Append(" 0 R>>\n");
        }

        if (annot.annotationType.Equals(Annotation.FileAttachment)) {
            Append("/FS ");
            Append(annot.fileAttachment.embeddedFile.objNumber);
            Append(" 0 R\n");
            Append("/Name /");
            Append(annot.fileAttachment.icon);
            Append("\n");

            if (!String.IsNullOrEmpty(annot.fileAttachment.title)) {
                Append("/T ");
                AppendTextString(annot.fileAttachment.title);
                Append("\n");
            }

            if (!String.IsNullOrEmpty(annot.fileAttachment.contents)) {
                Append("/Contents ");
                AppendTextString(annot.fileAttachment.contents);
                Append("\n");
            }
        } else if (annot.annotationType.Equals(Annotation.Link)) {
            // PDF/UA requires a link to carry an alternate description in its
            // Contents key.
            String description = annot.contents;
            if (String.IsNullOrEmpty(description)) {
                description = annot.altDescription;
            }
            if (String.IsNullOrEmpty(description)) {
                description = annot.uri;
            }
            if (String.IsNullOrEmpty(description)) {
                description = annot.key;
            }
            if (!String.IsNullOrEmpty(description)) {
                Append("/Contents ");
                AppendTextString(description);
                Append("\n");
            }
            if (!String.IsNullOrEmpty(annot.uri)) {
                Append("/A <<\n");
                Append("/S /URI\n");
                byte[] uri = Encoding.UTF8.GetBytes(annot.uri);
                if (encryption != null) {
                    uri = AES256.Encrypt(uri, encryption.GetKey());
                }
                Append("/URI <");
                Append(Util.ToHexString(uri));
                Append(">\n");
                Append(">>\n");
            } else if (!String.IsNullOrEmpty(annot.key)) {
                Destination destination;
                if (!destinations.TryGetValue(annot.key, out destination)) {
                    // A link to nowhere would do nothing when it is clicked.
                    Fail(new InvalidOperationException("The link goes to the destination " + annot.key
                            + ", which the document does not have."));
                } else {
                    Append("/Dest [");
                    Append(destination.pageObjNumber);
                    Append(" 0 R /XYZ ");
                    Append(destination.xPosition);
                    Append(" ");
                    Append(destination.yPosition);
                    Append(" 0]\n");
                }
            }
        } else if (annot.annotationType.Equals(Annotation.Polygon)) {
            Append("/Vertices [ ");
            for (int i = 0; i < annot.vertices.Length; i += 2) {
                Append(annot.x1 + annot.vertices[i]);
                Append(' ');
                Append(annot.y1 - annot.vertices[i + 1]);
                Append(' ');
            }
            Append("]\n");

            Append("/IC [");
            Append(annot.fillColor[0]);
            Append(' ');
            Append(annot.fillColor[1]);
            Append(' ');
            Append(annot.fillColor[2]);
            Append("]\n");

            if (!IsPDFA1()) {
                Append("/CA ");
                Append(annot.opacity);
                Append("\n");
            }

            if (!String.IsNullOrEmpty(annot.title)) {
                Append("/T ");
                AppendTextString(annot.title);
                Append("\n");
            }

            if (!String.IsNullOrEmpty(annot.contents)) {
                Append("/Contents ");
                AppendTextString(annot.contents);
                Append("\n");
            }
        } else if (annot.annotationType.Equals(Annotation.Square) ||
                annot.annotationType.Equals(Annotation.Circle)) {
            Append("/IC [");
            Append(annot.fillColor[0]);
            Append(' ');
            Append(annot.fillColor[1]);
            Append(' ');
            Append(annot.fillColor[2]);
            Append("]\n");

            if (!IsPDFA1()) {
                Append("/CA ");
                Append(annot.opacity);
                Append("\n");
            }

            if (!String.IsNullOrEmpty(annot.title)) {
                Append("/T ");
                AppendTextString(annot.title);
                Append("\n");
            }

            if (!String.IsNullOrEmpty(annot.contents)) {
                Append("/Contents ");
                AppendTextString(annot.contents);
                Append("\n");
            }
        } else if (annot.annotationType.Equals(Annotation.Text)) {
            Append("/Name /Comment\n");

            if (!String.IsNullOrEmpty(annot.title)) {
                Append("/T ");
                AppendTextString(annot.title);
                Append("\n");
            }

            if (!String.IsNullOrEmpty(annot.contents)) {
                Append("/Contents ");
                AppendTextString(annot.contents);
                Append("\n");
            }
        }

        if (index != -1) {
            Append("/StructParent ");
            Append(index++);
            Append("\n");
        }
        Append(Token.EndDictionary);
        EndObj();

        return index;
    }

    private void AddAnnotDictionaries() {
        int index = pages.Count;
        foreach (StructElement element in this.annotElements) {
            if (element.annotation != null) {
                index = AddAnnotationObject(element.annotation, index);
                element.annotation.structParentWritten = true;
            }
        }

        foreach (Page page in pages) {
            foreach (Annotation annotation in page.annots) {
                // Skip the annotations that were already written above -
                // writing them twice would leave the page referencing a copy
                // that has no /StructParent key.
                if (!annotation.structParentWritten) {
                    AddAnnotationObject(annotation, -1);
                }
            }
        }
    }

    private void AddOCProperties() {
        if (groups.Count > 0) {
            List<OCG> list = new List<OCG>();
            StringBuilder buf = new StringBuilder();
            foreach (OptionalContentGroup ocg in this.groups) {
                buf.Append(' ');
                buf.Append(ocg.objNumber);
                buf.Append(" 0 R");
                list.Add(new OCG(ocg.objNumber, ocg.name));
            }
            // The groups are in the order of their names, by the UTF-16 code
            // units as in every port, and those of the same name in the order
            // they were added, as the sort of Java keeps it and the sort of
            // C# does not.
            List<OCG> added = new List<OCG>(list);
            list.Sort((x, y) => {
                int order = String.CompareOrdinal(x.name, y.name);
                return (order != 0) ? order : added.IndexOf(x) - added.IndexOf(y);
            });

            Append("/OCProperties\n");
            Append("<<\n");
            Append("/OCGs [");
            Append(buf.ToString());
            Append(" ]\n");
            Append("/D <<\n");

            // PDF/UA and PDF/A ask for the configuration to have a name and no
            // usage states, /AS, which say when a group is printed: a group set
            // not to print prints in such a document.
            if (compliance == Compliance.PDF_1_7) {
                Append("/AS [\n");
                Append("<< /Event /View /Category [/View] /OCGs [");
                Append(buf.ToString());
                Append(" ] >>\n");
                Append("<< /Event /Print /Category [/Print] /OCGs [");
                Append(buf.ToString());
                Append(" ] >>\n");
                Append("<< /Event /Export /Category [/Export] /OCGs [");
                Append(buf.ToString());
                Append(" ] >>\n");
                Append("]\n");
            } else {
                Append("/Name (Default)\n");
            }

            // The groups hidden by default, for the viewers that read the
            // configuration and not the usage of each group
            StringBuilder off = new StringBuilder();
            foreach (OptionalContentGroup ocg in this.groups) {
                if (!ocg.visible) {
                    off.Append(' ');
                    off.Append(ocg.objNumber);
                    off.Append(" 0 R");
                }
            }
            if (off.Length > 0) {
                Append("/OFF [");
                Append(off.ToString());
                Append(" ]\n");
            }

            Append("/Order [");
            foreach (OCG ocg in list) {
                Append(' ');
                Append(ocg.objNumber);
                Append(" 0 R ");
            }
            Append("]\n");

            Append(">>\n");
            Append(">>\n");
        }
    }

    /// <summary>Adds the page to this document.</summary>
    public void AddPage(Page page) {
        if (page == null) {
            return;
        }
        if (completed) {
            Fail(new InvalidOperationException("The PDF was already completed."));
        }
        if (page.pdf != this) {
            Fail(new ArgumentException("The page belongs to another PDF."));
        }
        if (page.added) {
            Fail(new InvalidOperationException("The page was already added to the PDF."));
        }
        if (pagesObjNumber != 0) {
            // The page tree of the objects that AddObjects wrote does not list it.
            Fail(new InvalidOperationException(
                    "A page cannot be added to a PDF that AddObjects added the objects of an existing PDF to."));
        }
        page.added = true;
        if (page.objNumber == 0) {
            page.objNumber = ReserveObjNumber();
        }
        // A page that was drawn before it was added has elements of its own.
        if (IsTagged()) {
            page.SetStructElementsPageObjNumber(page.objNumber);
        }
        pages.Add(page);
        if (prevPage != null) {
            AddPageContent(prevPage);
        }
        prevPage = page;
    }

    /// <summary>
    /// Adds all the pages of a document that was read with Read after the
    /// pages of this document, in their order. A PDF can merge several
    /// documents and draw pages of its own before, between and after them.
    /// </summary>
    /// <remarks>
    /// The merged pages keep their content, resources, annotations and links.
    /// The parts of the read document that belong to the whole document are
    /// left out: its bookmarks, form fields, tagging, named destinations and
    /// optional content settings. The objects that the pages use are written
    /// at once, so the list of objects is not needed after the call.
    /// A PDF/UA or PDF/A document cannot merge pages, which were not made for
    /// its compliance, and Merge cannot be used with AddObjects.
    /// </remarks>
    /// <param name="objects">the objects of the document, as Read returns them.</param>
    public void Merge(List<PDFobj> objects) {
        CheckMerge(objects);
        MergePages(objects, GetPageObjects(objects));
    }

    /// <summary>
    /// Adds the listed pages of a document that was read with Read after the
    /// pages of this document, in the order they are listed. A document is
    /// split by merging each part of it into a PDF of its own: the objects that
    /// Read returned can be merged into any number of PDFs.
    /// </summary>
    /// <remarks>
    /// The pages are merged as Merge(objects) merges all of them, and a link to
    /// a page that is not merged leads nowhere. A page number that the document
    /// does not have, or one that is listed twice, is refused.
    /// </remarks>
    /// <param name="objects">the objects of the document, as Read returns them.</param>
    /// <param name="pageNumbers">the numbers of the pages, counted from 1.</param>
    public void Merge(List<PDFobj> objects, params int[] pageNumbers) {
        CheckMerge(objects);
        List<PDFobj> pageObjects = GetPageObjects(objects);
        List<PDFobj> listed = new List<PDFobj>();
        HashSet<int> seen = new HashSet<int>();
        foreach (int number in pageNumbers) {
            if (number < 1 || number > pageObjects.Count) {
                Fail(new ArgumentException("The document has no page " + number.ToString(CultureInfo.InvariantCulture) + "."));
            }
            if (!seen.Add(number)) {
                Fail(new ArgumentException("Page " + number.ToString(CultureInfo.InvariantCulture) + " is listed twice."));
            }
            listed.Add(pageObjects[number - 1]);
        }
        MergePages(objects, listed);
    }

    // Refuses a merge that would break this document.
    private void CheckMerge(List<PDFobj> objects) {
        if (completed) {
            Fail(new InvalidOperationException("The PDF was already completed."));
        }
        if (compliance != Compliance.PDF_1_7) {
            Fail(new InvalidOperationException(
                    "Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document."));
        }
        if (pagesObjNumber != 0) {
            Fail(new InvalidOperationException("Merge and AddObjects cannot be used on the same PDF."));
        }
        if (GetPagesObject(objects) == null) {
            Fail(new ArgumentException("The objects have no root /Pages object."));
        }
    }

    // Adds the pages, in their order, and every object that they use.
    private void MergePages(List<PDFobj> objects, List<PDFobj> pageObjects) {
        HashSet<int> mergedPages = new HashSet<int>();
        foreach (PDFobj page in pageObjects) {
            mergedPages.Add(page.number);
        }

        // Each page and every object that it uses, found through the
        // references, gets a number of this document before anything is
        // written, as the objects refer to each other: a page to its
        // annotations, and a link annotation to the page it points at.
        Dictionary<int, int> numbers = new Dictionary<int, int>();
        Dictionary<int, List<String>> values = new Dictionary<int, List<String>>();
        List<PDFobj> queue = new List<PDFobj>();
        PageTreeNodes nodes = new PageTreeNodes();
        foreach (PDFobj page in pageObjects) {
            if (!numbers.ContainsKey(page.number)) {
                numbers[page.number] = ReserveObjNumber();
                queue.Add(page);
            }
        }
        for (int i = 0; i < queue.Count; i++) {
            PDFobj obj = queue[i];
            List<String> value = MergedValue(obj, mergedPages.Contains(obj.number), objects, nodes);
            values[obj.number] = value;
            for (int j = 0; j < value.Count; j++) {
                if (IsReference(value, j)) {
                    int number = Int32.Parse(value[j], CultureInfo.InvariantCulture);
                    if (!numbers.ContainsKey(number) && IsMergedObject(number, objects, mergedPages)) {
                        numbers[number] = ReserveObjNumber();
                        queue.Add(objects[number - 1]);
                    }
                    j += 2;
                }
            }
        }

        foreach (PDFobj obj in queue) {
            if (!mergedPages.Contains(obj.number)) {
                AddMergedObject(obj, numbers[obj.number], Renumbered(values[obj.number], numbers));
            }
        }
        foreach (PDFobj obj in queue) {
            if (mergedPages.Contains(obj.number)) {
                pages.Add(new Page(this, numbers[obj.number], Renumbered(values[obj.number], numbers)));
            }
        }
    }

    // The entries of a page that it can inherit from the page tree.
    private static readonly String[] INHERITED = {"/Resources", "/MediaBox", "/CropBox", "/Rotate"};

    // Reserves the next object number for an object written later.
    internal int ReserveObjNumber() {
        objOffset.Add(0L);
        return objOffset.Count;
    }

    // Returns true when the tokens at index i are a reference: "n g R".
    internal static bool IsReference(List<String> tokens, int i) {
        return i + 2 < tokens.Count
                && tokens[i + 2].Equals("R")
                && IsObjectNumber(tokens[i])
                && IsObjectNumber(tokens[i + 1]);
    }

    // Returns true for a number that can be an object number or a generation
    // number: digits, which can have leading zeros, as pdf.js tests it in
    // issue10491, "0000000003 0 R", and no more than nine others.
    private static bool IsObjectNumber(String token) {
        if (token.Length == 0 || token.TrimStart('0').Length > 9) {
            return false;
        }
        foreach (char ch in token) {
            if (ch < '0' || ch > '9') {
                return false;
            }
        }
        return true;
    }

    // Returns true for an object that the merged pages can use: not the page
    // tree, the catalog, a page that is not merged or an object that is missing.
    private bool IsMergedObject(int number, List<PDFobj> objects, HashSet<int> mergedPages) {
        if (number < 1 || number > objects.Count) {
            return false;
        }
        PDFobj obj = objects[number - 1];
        if (obj.dict == null || obj.dict.Count == 0) {
            return false;
        }
        String type = obj.GetValue("/Type");
        if (type.Equals("/Pages") || type.Equals("/Catalog")) {
            return false;
        }
        return !IsPageObject(obj) || mergedPages.Contains(number);
    }

    // Returns the value of an object that was read, without its "n g obj" and
    // its "stream" and "endobj" keywords, with a direct /Length for a stream,
    // and for a page with the entries it inherits and without its /Parent.
    private static List<String> MergedValue(
            PDFobj obj, bool isPage, List<PDFobj> objects, PageTreeNodes nodes) {
        List<String> value = ValueOf(obj);
        if (obj.stream != null) {
            SetEntry(value, "/Length", new List<String> { obj.stream.Length.ToString(CultureInfo.InvariantCulture) });
        }
        if (isPage) {
            foreach (String key in INHERITED) {
                if (EntryIndex(value, key) == -1) {
                    List<String> inherited = InheritedValue(obj, key, objects, nodes);
                    if (inherited == null && key.Equals("/MediaBox")) {
                        inherited = new List<String> { "[", "0", "0", "612", "792", "]" };    // Letter
                    }
                    if (inherited != null) {
                        SetEntry(value, key, inherited);
                    }
                }
            }
            RemoveEntry(value, "/Parent");
        }
        return value;
    }

    internal static List<String> ValueOf(PDFobj obj) {
        List<String> dict = obj.dict;
        int start = (dict.Count >= 3 && dict[2].Equals("obj")) ? 3 : 0;
        int end = dict.Count;
        if (end > start && dict[end - 1].Equals("endobj")) {
            end--;
        }
        if (end > start && dict[end - 1].Equals("stream")) {
            end--;
        }
        return dict.GetRange(start, end - start);
    }

    // The entries of the nodes of a page tree, each read once. A node can have
    // thousands of kids, and reading it again for each entry of each page that
    // inherits one took time that grew as the square of the number of pages.
    private sealed class PageTreeNodes {
        private readonly Dictionary<int, Dictionary<String, List<String>>> entries =
                new Dictionary<int, Dictionary<String, List<String>>>();

        internal Dictionary<String, List<String>> EntriesOf(PDFobj node) {
            Dictionary<String, List<String>> nodeEntries;
            if (!entries.TryGetValue(node.number, out nodeEntries)) {
                nodeEntries = Entries(ValueOf(node));
                entries[node.number] = nodeEntries;
            }
            return nodeEntries;
        }

        // Forgets the entries of an object that was changed.
        internal void Forget(PDFobj obj) {
            entries.Remove(obj.number);
        }
    }

    // Returns the entries of the dictionary by their keys, the first entry of
    // a key that it has twice, as EntryIndex finds it.
    private static Dictionary<String, List<String>> Entries(List<String> tokens) {
        Dictionary<String, List<String>> entries = new Dictionary<String, List<String>>();
        if (tokens.Count == 0 || !tokens[0].Equals("<<")) {
            return entries;
        }
        int i = 1;
        while (i < tokens.Count && !tokens[i].Equals(">>")) {
            int end = ValueEnd(tokens, i + 1);
            if (!entries.ContainsKey(tokens[i])) {
                entries[tokens[i]] = tokens.GetRange(i + 1, end - (i + 1));
            }
            i = end;
        }
        return entries;
    }

    // Returns the value of the entry from the nearest node of the page tree
    // above the page that has it, or null.
    private static List<String> InheritedValue(
            PDFobj page, String key, List<PDFobj> objects, PageTreeNodes nodes) {
        Dictionary<String, List<String>> entries = Entries(ValueOf(page));
        for (int depth = 0; depth < 64; depth++) {  // A loop in a broken tree ends here.
            List<String> parent;
            if (!entries.TryGetValue("/Parent", out parent) || !IsReference(parent, 0)) {
                return null;
            }
            int number = Int32.Parse(parent[0], CultureInfo.InvariantCulture);
            if (number < 1 || number > objects.Count || objects[number - 1].dict.Count == 0) {
                return null;
            }
            entries = nodes.EntriesOf(objects[number - 1]);
            List<String> value;
            if (entries.TryGetValue(key, out value)) {
                return new List<String>(value);
            }
        }
        return null;
    }

    // Returns the index after the value that starts at index i.
    internal static int ValueEnd(List<String> tokens, int i) {
        if (i >= tokens.Count) {
            return tokens.Count;
        }
        String token = tokens[i];
        if (token.Equals("<<") || token.Equals("[")) {
            int depth = 0;
            for (int j = i; j < tokens.Count; j++) {
                String t = tokens[j];
                if (t.Equals("<<") || t.Equals("[")) {
                    depth++;
                } else if (t.Equals(">>") || t.Equals("]")) {
                    depth--;
                    if (depth == 0) {
                        return j + 1;
                    }
                }
            }
            return tokens.Count;
        }
        return IsReference(tokens, i) ? i + 3 : i + 1;
    }

    // Returns the index of the key of an entry of the dictionary, not of a
    // dictionary inside it, or -1.
    internal static int EntryIndex(List<String> tokens, String key) {
        if (tokens.Count == 0 || !tokens[0].Equals("<<")) {
            return -1;
        }
        int i = 1;
        while (i < tokens.Count && !tokens[i].Equals(">>")) {
            if (tokens[i].Equals(key)) {
                return i;
            }
            i = ValueEnd(tokens, i + 1);
        }
        return -1;
    }

    // Sets the value of an entry of the dictionary, adding the entry at its end.
    private static void SetEntry(List<String> tokens, String key, List<String> value) {
        if (tokens.Count == 0 || !tokens[0].Equals("<<")) {
            return;
        }
        int i = EntryIndex(tokens, key);
        if (i != -1) {
            tokens.RemoveRange(i + 1, ValueEnd(tokens, i + 1) - (i + 1));
            tokens.InsertRange(i + 1, value);
        } else {
            int end = ValueEnd(tokens, 0) - 1;     // The index of the closing >>
            tokens.InsertRange(end, value);
            tokens.Insert(end, key);
        }
    }

    private static void RemoveEntry(List<String> tokens, String key) {
        int i = EntryIndex(tokens, key);
        if (i != -1) {
            tokens.RemoveRange(i, ValueEnd(tokens, i + 1) - i);
        }
    }

    // Returns the tokens with the references renumbered for this document, a
    // reference to an object that is not merged replaced with null, and the
    // strings encrypted when this document is encrypted.
    private List<String> Renumbered(List<String> tokens, Dictionary<int, int> numbers) {
        List<String> result = new List<String>(tokens.Count);
        for (int i = 0; i < tokens.Count; i++) {
            String token = tokens[i];
            if (IsReference(tokens, i)) {
                int number;
                if (numbers.TryGetValue(Int32.Parse(token, CultureInfo.InvariantCulture), out number)) {
                    result.Add(number.ToString(CultureInfo.InvariantCulture));
                    result.Add("0");
                    result.Add("R");
                } else {
                    result.Add("null");
                }
                i += 2;
            } else if (encryption != null
                    && (token.StartsWith("(", StringComparison.Ordinal) || (token.StartsWith("<", StringComparison.Ordinal) && !token.Equals("<<")))) {
                result.Add("<" + Util.ToHexString(AES256.Encrypt(Decryptor.ToBytes(token), encryption.GetKey())) + ">");
            } else {
                result.Add(token);
            }
        }
        return result;
    }

    private void AddMergedObject(PDFobj obj, int number, List<String> value) {
        byte[] stream = obj.stream;
        if (stream != null && encryption != null) {
            stream = AES256.Encrypt(stream, encryption.GetKey());
            SetEntry(value, "/Length", new List<String> { stream.Length.ToString(CultureInfo.InvariantCulture) });
        }
        SetObjOffset(number, byteCount);
        Append(number);
        Append(Token.NewObj);
        AppendTokens(value);
        Append(Token.Newline);
        if (stream != null) {
            Append(Token.Stream);
            Append(stream);
            Append(Token.EndStream);
        }
        Append(Token.EndObj);
    }

    private void AppendTokens(List<String> tokens) {
        for (int i = 0; i < tokens.Count; i++) {
            if (i > 0) {
                Append(Token.Space);
            }
            AppendToken(tokens[i]);
        }
    }

    /// <summary>Adds the pages to this document.</summary>
    public void AddPages(List<Page> pages) {
        foreach (Page page in pages) {
            AddPage(page);
        }
    }

    /// <summary>
    /// Completes the construction of the PDF and writes it to the output stream.
    /// The output stream is then automatically closed.
    /// </summary>
    public void Complete() {
        // The output stream is closed also when the document could not be
        // completed.
        try {
            WriteRest();
        } catch (Exception) {
            os.Close();
            throw;
        }
        os.Close();
    }

    // Writes the rest of the PDF.
    private void WriteRest() {
        if (completed) {
            Fail(new InvalidOperationException("Complete() was already called."));
        }
        if (error != null) {
            throw new InvalidOperationException("The PDF was not completed because of an earlier error: " + error);
        }
        if (pages.Count == 0 && pagesObjNumber == 0) {
            Fail(new InvalidOperationException("A PDF needs at least one page."));
        }
        // PDF/UA asks for the title of the document, which a reader shows in
        // place of the name of the file.
        if ((compliance == Compliance.PDF_UA_1 || compliance == Compliance.PDF_A_3A_UA_1)
                && String.IsNullOrWhiteSpace(title)) {
            Fail(new InvalidOperationException("A PDF/UA document needs a title: use SetTitle."));
        }
        if (prevPage != null) {
            AddPageContent(prevPage);
        }
        completed = true;
        if (compliance != Compliance.PDF_1_7) {
            metadataObjNumber = AddMetadataObject("", false);
            outputIntentObjNumber = AddOutputIntentObject();
        }

        if (pagesObjNumber == 0) {
            AddAllPages(AddResourcesObject());
            AddPagesObject();
        }

        int structTreeRootObjNumber = 0;
        if (IsTagged()) {
            // The elements of every page are written with it; the ones still
            // open and the ones of the annotations are what is left.
            foreach (Page page in pages) {
                foreach (StructElement element in page.structures) {
                    AddStructElementObject(element);
                }
                page.structures = new List<StructElement>();
            }
            ReserveStructTreeNumbers();
            structTreeRootObjNumber = AddStructTreeRootObject();
            AddNumsParentTree();
            AddStructDocumentObject(structTreeRootObjNumber);
        }

        // A tagged document with headings and no bookmarks of its own has the
        // bookmarks of its headings, which a reader shows as its outline, as
        // PAC asks of a document with headings
        if (toc == null && headings.Count > 0) {
            toc = Bookmark.OfHeadings(this);
        }
        int outlineDictNum = 0;
        if (toc != null && toc.GetChildren() != null) {
            List<Bookmark> list = toc.ToArrayList();
            outlineDictNum = AddOutlineDict(toc);
            for (int i = 1; i < list.Count; i++) {
                AddOutlineItem(outlineDictNum, list[i]);
            }
        }

        int infoObjNumber = AddInfoObject();
        int rootObjNumber = AddRootObject(structTreeRootObjNumber, outlineDictNum);
        long startxref = byteCount;

        // Create the xref table
        Append("xref\n");
        Append("0 ");
        Append(rootObjNumber + 1);
        Append('\n');

        Append("0000000000 65535 f \n");
        foreach (long offset in objOffset) {
            if (offset == 0) {      // A number that no object was written for.
                Append("0000000000 65535 f \n");
                continue;
            }
            Append(XrefOffset(offset));
            Append(" 00000 n \n");
        }
        Append("trailer\n");
        Append("<<\n");
        Append("/Size ");
        Append(rootObjNumber + 1);
        Append('\n');

        Append("/ID[<");    // Do not need to be encrypted!
        Append(uuid);
        Append("><");
        Append(uuid);
        Append(">]\n");

        if (encryption != null) {
            Append("/Encrypt ");
            Append(encryption.GetObjNumber());
            Append(" 0 R\n");
        }

        Append("/Info ");
        Append(infoObjNumber);
        Append(" 0 R\n");

        Append("/Root ");
        Append(rootObjNumber);
        Append(" 0 R\n");

        Append(">>\n");
        Append("startxref\n");
        Append(startxref.ToString(CultureInfo.InvariantCulture));
        Append('\n');
        Append("%%EOF\n");
    }

    /// <summary>
    /// Adds a file that the document carries with it: a reader shows it beside
    /// the document, and a program that reads the document finds it by its
    /// name. This is how a document of PDF/A-3 carries the data behind what it
    /// shows, such as the XML of an invoice.
    /// <para>The file names what it holds, how it relates to the document and
    /// what it is, so it has to be made with the constructor of EmbeddedFile
    /// that takes them.</para>
    /// </summary>
    /// <param name="file">The embedded file.</param>
    /// <returns>this PDF object.</returns>
    public PDF AddAssociatedFile(EmbeddedFile file) {
        // PDF/A-1 carries no files at all, and PDF/A-2 only other documents of
        // PDF/A, so a document of either that carries a file is not the
        // document it says it is.
        if (compliance == Compliance.PDF_A_1A || compliance == Compliance.PDF_A_1B
                || compliance == Compliance.PDF_A_2A || compliance == Compliance.PDF_A_2B) {
            Fail(new InvalidOperationException("A document of " + compliance
                    + " cannot carry the file " + file.GetFileName()
                    + ": PDF/A-3 is the one that carries files."));
            return this;
        }
        if (file.relationship == null) {
            Fail(new ArgumentException("The file " + file.GetFileName()
                    + " was embedded without a media type, a relationship and a description, "
                    + "which a file the document carries needs: use the constructor of "
                    + "EmbeddedFile that takes them."));
            return this;
        }
        // PDF/A-3 asks a file it carries for what it holds, its /Subtype, and
        // for its size and date, which are written with the media type.
#pragma warning disable CS0618 // PDF_A_3A is deprecated, and still supported
        if (String.IsNullOrEmpty(file.mediaType)
                && (compliance == Compliance.PDF_A_3A || compliance == Compliance.PDF_A_3B
                || compliance == Compliance.PDF_A_3A_UA_1)) {
#pragma warning restore CS0618
            Fail(new ArgumentException("The file " + file.GetFileName()
                    + " was embedded without a media type, which a file of a document of PDF/A-3 needs."));
            return this;
        }
        associatedFiles.Add(file);
        return this;
    }

    /// <summary>
    /// Adds a description to the metadata of the document: the rdf:Description
    /// element of a standard that asks for properties of its own, such as the
    /// invoice standards that say which of the files the document carries is
    /// the invoice. The text is written into the metadata as it is given, so it
    /// has to be XML, and the document has to be of PDF/A or PDF/UA, which are
    /// the documents that carry metadata.
    /// </summary>
    /// <param name="rdfDescription">The rdf:Description element.</param>
    /// <returns>this PDF object.</returns>
    public PDF AddMetadata(String rdfDescription) {
        metadata.Add(rdfDescription);
        return this;
    }

    // An empty document property is the same as one that was never set: it is
    // not written, as in the Go port, which cannot tell the two apart.
    private static String NullIfEmpty(String text) {
        return (text != null && text.Length == 0) ? null : text;
    }

    /// <summary>
    /// Set the "Title" document property of the PDF file. An empty title is not written.
    /// </summary>
    /// <param name="title">The title of this document.</param>
    /// <returns>this PDF object.</returns>
    public PDF SetTitle(String title) {
        this.title = NullIfEmpty(CleanText(title));
        return this;
    }

    /// <summary>
    /// Set the "Language" document property of the PDF file.
    /// </summary>
    /// <param name="language">The language of this document.</param>
    /// <returns>this PDF object.</returns>
    public PDF SetLanguage(String language) {
        this.language = language;
        return this;
    }

    /// <summary>
    /// Set the "Author" document property of the PDF file.
    /// </summary>
    /// <param name="author">The author of this document.</param>
    /// <returns>this PDF object.</returns>
    public PDF SetAuthor(String author) {
        this.author = NullIfEmpty(CleanText(author));
        return this;
    }

    /// <summary>
    /// Set the "Subject" document property of the PDF file.
    /// </summary>
    /// <param name="subject">The subject of this document.</param>
    /// <returns>this PDF object.</returns>
    public PDF SetSubject(String subject) {
        this.subject = NullIfEmpty(CleanText(subject));
        return this;
    }

    /// <summary>Sets the keywords in the document metadata.</summary>
    public PDF SetKeywords(String keywords) {
        this.keywords = NullIfEmpty(CleanText(keywords));
        return this;
    }

    /// <summary>Sets the creator in the document metadata.</summary>
    public PDF SetCreator(String creator) {
        this.creator = NullIfEmpty(CleanText(creator));
        return this;
    }

    /// <summary>Sets the page layout used when the document is opened. See PageLayout.</summary>
    public PDF SetPageLayout(PageLayout pageLayout) {
        this.pageLayout = pageLayout;
        return this;
    }

    /// <summary>Sets the page mode used when the document is opened. See PageMode.</summary>
    public PDF SetPageMode(PageMode pageMode) {
        this.pageMode = pageMode;
        return this;
    }

    internal void Append(int num) {
        // The digits and the minus sign of every culture are the ASCII ones
        // of the invariant culture, which PDF asks for.
        Append(num.ToString(CultureInfo.InvariantCulture));
    }

    internal void Append(float f) {
        if (!FastFloat.IsWritable(f)) {
            Fail(new ArgumentException(FastFloat.NOT_WRITABLE));
        }
        Append(FastFloat.ToByteArray(f));
    }

    internal void Append(String str) {
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        os.Write(bytes, 0, bytes.Length);
        byteCount += bytes.Length;
    }

    internal void Append(char ch) {
        Append((byte) ch);
    }

    internal void Append(byte b) {
        os.WriteByte(b);
        byteCount += 1;
    }

    internal void Append(byte[] buf) {
        os.Write(buf, 0, buf.Length);
        byteCount += buf.Length;
    }

    internal void Append(byte[] buf, int off, int len) {
        os.Write(buf, off, len);
        byteCount += len;
    }

    internal void Append(MemoryStream baos) {
        baos.WriteTo(os);
        byteCount += baos.Length;
    }

    // The highest object number read in a file of any size: its empty objects
    // take about 30 MB.
    private const int MIN_OBJ_NUMBER_LIMIT = 262144;

    // Returns the objects by their number, with an empty object at the number
    // of every one the PDF does not have, so that the object a reference names
    // is the one at its number. A PDF may number its objects as it likes, and
    // one that kept the numbers of the document it was cut from has few objects
    // and high numbers, so a number up to MIN_OBJ_NUMBER_LIMIT is read in any
    // file, and a larger one in a file that has as many bytes. The empty
    // objects up to a larger number would take the memory a file of a few
    // bytes never names.
    internal List<PDFobj> GetSortedObjects(List<PDFobj> objects, int size) {
        List<PDFobj> sorted = new List<PDFobj>();

        int maxObjNumber = 0;
        foreach (PDFobj obj in objects) {
            if (obj.number > maxObjNumber) {
                maxObjNumber = obj.number;
            }
        }
        if (maxObjNumber > Math.Max(size, MIN_OBJ_NUMBER_LIMIT)) {
            throw new Exception("The PDF of " + size
                    + " bytes cannot hold an object numbered " + maxObjNumber + ".");
        }

        for (int number = 1; number <= maxObjNumber; number++) {
            PDFobj obj = new PDFobj();
            obj.SetNumber(number);
            sorted.Add(obj);
        }

        foreach (PDFobj obj in objects) {
            if (obj.number > 0) {
                sorted[obj.number - 1] = obj;
            }
        }

        return sorted;
    }

    /// <summary>
    /// Reads the objects of an existing PDF from the stream. An encrypted PDF
    /// is decrypted when it opens without a password.
    /// </summary>
    public List<PDFobj> Read(Stream inputStream) {
        return Read(inputStream, "");
    }

    /// <summary>
    /// Reads the objects of an existing PDF from the stream, which holds a PDF
    /// that is encrypted with the standard security handler. The PDF is
    /// decrypted with the password, which is its user or its owner password.
    /// </summary>
    /// <param name="inputStream">The PDF input stream.</param>
    /// <param name="password">The user or owner password of the PDF.</param>
    /// <exception cref="Exception">If the password is not correct.</exception>
    public List<PDFobj> Read(Stream inputStream, String password) {
        byte[] buf = Content.GetFromStream(inputStream);

        List<PDFobj> objects1 = new List<PDFobj>();
        PDFobj.DecodeBudget budget = new PDFobj.DecodeBudget(buf.Length); // For all the streams of this PDF together
        PDFobj trailer;
        try {
            trailer = GetObjects(buf, GetStartXRef(buf), objects1, 0, new HashSet<int>(), budget);
        } catch (PDFobj.DecodedTotalException) {
            throw;              // Not a reason to scan the PDF for its objects
        } catch (Exception) {
            trailer = null;     // A cross-reference stream that cannot be decoded.
        }
        if (trailer == null || objects1.Count == 0) {
            // The cross-reference table is missing or wrong, like in a PDF
            // that was changed without updating it.
            objects1.Clear();
            trailer = GetObjectsByScanning(buf, objects1, budget);
        }
        Decryptor decryptor = Decryptor.GetDecryptor(trailer, objects1, password);

        // The object of each number, for a /Length that is an object of its
        // own. The newest version of an object that was updated comes last
        // and wins.
        Dictionary<int, PDFobj> numbered = new Dictionary<int, PDFobj>();
        foreach (PDFobj obj in objects1) {
            numbered[obj.number] = obj;
        }

        List<PDFobj> objects2 = new List<PDFobj>();
        foreach (PDFobj obj in objects1) {
            String type = obj.GetValue("/Type");
            if (type.Equals("/XRef")) {
                continue;       // Skip the cross-reference streams.
            }
            if (decryptor != null) {
                if (obj.number == decryptor.objNumber) {
                    continue;   // Skip the encryption dictionary.
                }
                decryptor.DecryptStrings(obj);
            }
            if (obj.dict.Contains("stream")) {
                obj.SetStreamAndData(buf, obj.GetLength(numbered), decryptor, budget);
            }

            if (type.Equals("/ObjStm")) {
                // A malformed object stream is an error, as in the other
                // ports, and not a reason to stop the program.
                int first = ObjectStreamNumber(obj.GetValue("/First"));
                // An object stream with no stream of its own has no objects,
                // as it has in the Go and Swift ports.
                byte[] data = obj.data ?? new byte[0];
                PDFobj o2 = GetObject(data, 0, Math.Min(first, data.Length));
                // Its objects are read in no more than its length in all, as
                // one whose offset is listed again was read again: a stream
                // of a few kilobytes that decodes to a megabyte, with an
                // object listed a thousand times, took seconds.
                PDFobj.DecodeBudget streamBudget = new PDFobj.DecodeBudget(0);
                streamBudget.readLeft = data.Length;
                for (int i = 0; i + 1 < o2.dict.Count; i += 2) {
                    String num = o2.dict[i];
                    int number = ObjectStreamNumber(num);
                    int off = ObjectStreamNumber(o2.dict[i + 1]);
                    // The offsets are added as longs, as their sum can be
                    // more than an int holds; one past the end of the data
                    // is the end.
                    int end = data.Length;
                    if (i <= o2.dict.Count - 4) {
                        end = (int) Math.Min(
                                (long) first + ObjectStreamNumber(o2.dict[i + 3]), data.Length);
                    }
                    int start = (int) Math.Min((long) first + off, data.Length);
                    PDFobj o3 = ReadObject(data, start, end, streamBudget);
                    o3.SetNumber(number);
                    o3.dict.Insert(0, "obj");
                    o3.dict.Insert(0, "0");
                    o3.dict.Insert(0, num);
                    objects2.Add(o3);
                }
            } else {
                objects2.Add(obj);
            }
        }

        List<PDFobj> sorted = GetSortedObjects(objects2, buf.Length);
        PDFobj root = PDFobj.ObjectNumbered(sorted, TrailerRoot(trailer));
        if (root != null) {
            root.root = true;
        }
        return sorted;
    }

    // Returns the number of the catalog that the /Root of the trailer names,
    // or 0. The trailer is the dictionary after the cross-reference table, or
    // that of the cross-reference stream.
    private static int TrailerRoot(PDFobj trailer) {
        if (trailer == null) {
            return 0;
        }
        int open = trailer.dict.IndexOf("<<");
        if (open == -1) {
            return 0;
        }
        List<String> tokens = trailer.dict.GetRange(open, trailer.dict.Count - open);
        int i = EntryIndex(tokens, "/Root");
        if (i == -1 || !IsReference(tokens, i + 1)) {
            return 0;
        }
        return ToInteger(tokens[i + 1]);
    }

    // Returns the number in the header of an object stream, which is digits
    // and no more than the largest int, as in the other ports.
    private int ObjectStreamNumber(String token) {
        int number = ToInteger(token);
        if (number >= 0) {
            return number;
        }
        throw new Exception("The object stream of the PDF is malformed: \""
                + token + "\" is not a number.");
    }

    private bool Process(
            PDFobj obj, StringBuilder sb1, byte[] buf, int off) {
        String str = TrimToken(sb1.ToString());
        if (!str.Equals("")) {
            obj.dict.Add(str);
        }
        sb1.Length = 0;
        if (str.Equals("endobj")) {
            return true;
        } else if (str.Equals("stream")) {
            // The keyword ends with CRLF or LF, and the tokenizer consumed the
            // first of those bytes, so only the LF of a CRLF is left to skip.
            // A data byte that is a line feed, like the first byte of the IV
            // of an encrypted stream, stays.
            obj.streamOffset = off;
            if (off > 0 && buf[off - 1] == '\r' && off < buf.Length && buf[off] == '\n') {
                obj.streamOffset += 1;
            }
            return true;
        } else if (str.Equals("startxref")) {
            return true;
        }
        return false;
    }

    // Removes the characters up to the space at both ends, like trim() in Java.
    // Trim() also removes Unicode spaces like the no-break space, which is the
    // byte 0xA0, so it could remove a byte at the end of a name.
    private static String TrimToken(String str) {
        int start = 0;
        int end = str.Length;
        while (start < end && str[start] <= ' ') {
            start++;
        }
        while (end > start && str[end - 1] <= ' ') {
            end--;
        }
        return str.Substring(start, end - start);
    }

    internal PDFobj GetObject(byte[] buf, int off) {
        if (off < 0 || off >= buf.Length) {
            return new PDFobj();    // An offset outside of the PDF has no tokens.
        }
        return GetObject(buf, off, buf.Length);
    }

    // Returns the object at the offset, which ends at len at the latest.
    private PDFobj GetObject(byte[] buf, int off, int len) {
        PDFobj obj = new PDFobj();
        obj.offset = off;
        obj.end = len;
        StringBuilder token = new StringBuilder();

        int p = 0;          // The nesting level of the parentheses in a literal string
        bool done = false;
        while (!done && off < len) {
            char c2 = (char) buf[off++];
            if (p > 0) {
                // A literal string is one token, with its white space and
                // delimiters. A backslash escapes the character after it.
                token.Append(c2);
                if (c2 == '\\') {
                    if (off < len) {
                        token.Append((char) buf[off++]);
                    }
                } else if (c2 == '(') {
                    ++p;
                } else if (c2 == ')') {
                    --p;
                    if (p == 0) {
                        done = Process(obj, token, buf, off);
                    }
                }
            } else if (c2 == '(') {
                done = Process(obj, token, buf, off);
                if (!done) {
                    token.Append(c2);
                    p = 1;
                }
            } else if (IsWhiteSpace(c2)) {
                done = Process(obj, token, buf, off);
            } else if (c2 == '/') {
                done = Process(obj, token, buf, off);
                if (!done) {
                    token.Append(c2);
                }
            } else if (c2 == '<' || c2 == '>') {
                done = Process(obj, token, buf, off);
                if (!done) {
                    if (off < len && buf[off] == c2) {
                        obj.dict.Add(c2 == '<' ? "<<" : ">>");
                        off++;
                    } else if (c2 == '<') {
                        // A hexadecimal string is one token, without its white space.
                        token.Append(c2);
                        while (off < len && buf[off] != '>') {
                            char c = (char) buf[off++];
                            if (!IsWhiteSpace(c)) {
                                token.Append(c);
                            }
                        }
                        token.Append('>');
                        off++;
                        done = Process(obj, token, buf, off);
                    } else {
                        obj.dict.Add(">");
                    }
                }
            } else if (c2 == '%') {
                // A comment ends at the end of the line.
                done = Process(obj, token, buf, off);
                while (!done && off < len && buf[off] != '\n' && buf[off] != '\r') {
                    off++;
                }
            } else if (c2 == '[' || c2 == ']' || c2 == '{' || c2 == '}') {
                done = Process(obj, token, buf, off);
                if (!done) {
                    obj.dict.Add(c2.ToString());
                }
            } else {
                token.Append(c2);
            }
        }
        if (!done) {
            Process(obj, token, buf, off);  // The last token, at the end of the data.
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
    private PDFobj ReadObject(byte[] buf, int off, int end, PDFobj.DecodeBudget budget) {
        if (off < 0 || off >= buf.Length) {
            return new PDFobj();
        }
        end = Math.Min(end, buf.Length);
        int limit = (int) Math.Min(end, (long) off + Math.Max(budget.readLeft, 0));
        PDFobj obj = GetObject(buf, off, limit);
        budget.readLeft -= obj.read - off;
        if (limit < end && obj.read >= limit) {
            return new PDFobj();
        }
        return obj;
    }

    internal static bool IsWhiteSpace(int c) {
        return c == 0x00        // Null
            || c == 0x09        // Horizontal Tab
            || c == 0x0A        // Line Feed (LF)
            || c == 0x0C        // Form Feed
            || c == 0x0D        // Carriage Return (CR)
            || c == 0x20;       // Space
    }

    /// <summary>
    /// Converts an array of bytes to an integer.
    /// </summary>
    /// <param name="buf">byte[]</param>
    /// <param name="off">the index of the first byte.</param>
    /// <param name="len">the number of bytes.</param>
    /// <returns>int</returns>
    private int ToInt(byte[] buf, int off, int len) {
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
    private static int ToInteger(String token) {
        return (IsInteger(token) && Int32.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) ? value : -1;
    }

    // Returns true when the tokens of the object start with "number
    // generation obj", where the number is above 0, and is the number
    // that is given unless that is -1.
    private static bool IsObject(PDFobj obj, int number) {
        if (obj.dict.Count < 3 || !obj.dict[2].Equals("obj") || !IsInteger(obj.dict[1])) {
            return false;
        }
        int n = ToInteger(obj.dict[0]);
        return n > 0 && (number == -1 || n == number);
    }

    // Adds the objects of the cross-reference section at the offset to the
    // list, after the objects of the sections before it, so that the newest
    // version of an object that was updated comes last. A section is a
    // cross-reference table, which can have an /XRefStm stream for the
    // objects in object streams, or a cross-reference stream. Returns the
    // trailer of the section, which is the cross-reference stream object when
    // there is no table, or null when an offset in the section is not that
    // of its object. A section that a /Prev leads back to, which visited
    // holds the offset of, is a broken PDF, and is not read again: a /Prev
    // that points at its own section read the section a thousand times.
    private PDFobj GetObjects(
            byte[] buf, int offset, List<PDFobj> objects, int depth,
            HashSet<int> visited, PDFobj.DecodeBudget budget) {
        if (!visited.Add(offset)) {
            return null;
        }
        PDFobj xref = ReadObject(buf, offset, buf.Length, budget);
        bool table = xref.dict.Count > 0 && xref.dict[0].Equals("xref");
        if (depth > 1000 || (!table && !IsObject(xref, -1))) {
            return null;
        }
        String prev = xref.GetValue("/Prev");
        if (!prev.Equals("") && GetObjects(buf, ToInteger(prev), objects, depth + 1, visited, budget) == null) {
            return null;
        }
        if (table) {
            // The objects in the table replace those in the /XRefStm stream.
            String xrefStm = xref.GetValue("/XRefStm");
            if (!xrefStm.Equals("") &&
                    !GetStreamObjects(buf, ReadObject(buf, ToInteger(xrefStm), buf.Length, budget), objects, budget)) {
                return null;
            }
            if (!GetTableObjects(buf, xref, objects, budget)) {
                return null;
            }
        } else if (!GetStreamObjects(buf, xref, objects, budget)) {
            return null;
        }
        return xref;
    }

    // Returns the index of the first of the sorted values that is greater
    // than the value, or the number of values when there is none.
    private static int FirstAfter(int[] sorted, int value) {
        int low = 0;
        int high = sorted.Length;
        while (low < high) {
            int mid = low + (high - low) / 2;
            if (sorted[mid] <= value) {
                low = mid + 1;
            } else {
                high = mid;
            }
        }
        return low;
    }

    // Adds the objects of the entries, each a number and an offset, and
    // returns false when an offset is not that of its object. An object ends
    // where the next one of the section starts at the latest, as one with no
    // endobj was read to the end of the PDF: a section of objects with no
    // endobj took seconds for every hundred kilobytes.
    private bool GetEntryObjects(
            byte[] buf, List<int[]> entries, List<PDFobj> objects, PDFobj.DecodeBudget budget) {
        int[] offsets = new int[entries.Count];
        for (int i = 0; i < offsets.Length; i++) {
            offsets[i] = entries[i][1];
        }
        Array.Sort(offsets);
        foreach (int[] entry in entries) {
            int end = buf.Length;
            int j = FirstAfter(offsets, entry[1]);
            if (j < offsets.Length) {
                end = Math.Min(offsets[j], buf.Length);
            }
            PDFobj obj = ReadObject(buf, entry[1], end, budget);
            if (!IsObject(obj, entry[0])) {
                return false;
            }
            obj.number = entry[0];
            objects.Add(obj);
        }
        return true;
    }

    // Adds the objects in use of a cross-reference table, and returns false
    // when an offset is not that of its object.
    private bool GetTableObjects(byte[] buf, PDFobj xref, List<PDFobj> objects, PDFobj.DecodeBudget budget) {
        List<int[]> entries = new List<int[]>();
        List<String> dict = xref.dict;
        int i = 1;
        // Each subsection starts with its first object number and the number of entries.
        while (i + 1 < dict.Count && IsInteger(dict[i])) {
            int number = ToInteger(dict[i]);
            int count = ToInteger(dict[i + 1]);
            i += 2;
            for (int j = 0; j < count; j++, number++, i += 3) {
                if (i + 2 >= dict.Count) {
                    return false;
                }
                // The entry is the offset, the generation number and n for an
                // object in use. Object 0 heads the list of free objects, and
                // one that is marked in use, as pdf.js tests it in issue10004,
                // is skipped, as MuPDF skips it.
                if (dict[i + 2].Equals("n") && number != 0) {
                    entries.Add(new int[] {number, ToInteger(dict[i])});
                }
            }
        }
        return i < dict.Count && dict[i].Equals("trailer") && GetEntryObjects(buf, entries, objects, budget);
    }

    // Adds the objects of a cross-reference stream that are not in object
    // streams, and returns false when an offset is not that of its object.
    private bool GetStreamObjects(byte[] buf, PDFobj xref, List<PDFobj> objects, PDFobj.DecodeBudget budget) {
        if (!IsObject(xref, -1) || !xref.GetValue("/Type").Equals("/XRef") ||
                !xref.dict.Contains("stream")) {
            return false;
        }
        // See page 50 in PDF32000_2008.pdf
        List<String> dict = xref.dict;
        int w = dict.IndexOf("/W");
        if (w == -1 || w + 4 >= dict.Count) {
            return false;
        }
        int n1 = ToInteger(dict[w + 2]);    // Field 1 number of bytes
        int n2 = ToInteger(dict[w + 3]);    // Field 2 number of bytes
        int n3 = ToInteger(dict[w + 4]);    // Field 3 number of bytes
        int length = ToInteger(xref.GetValue("/Length"));
        if (n1 < 0 || n2 < 0 || n3 < 0 || n1 + n2 + n3 == 0 ||
                length < 0 || xref.streamOffset + length > buf.Length) {
            return false;
        }
        // The /Index array has the first object number and the number of
        // entries of each subsection, and is [0 /Size] when it is missing.
        List<int> index = new List<int>();
        int k = dict.IndexOf("/Index");
        if (k != -1 && k + 1 < dict.Count && dict[k + 1].Equals("[")) {
            for (k += 2; k + 1 < dict.Count && IsInteger(dict[k]); k += 2) {
                index.Add(ToInteger(dict[k]));
                index.Add(ToInteger(dict[k + 1]));
            }
        } else {
            index.Add(0);
            index.Add(ToInteger(xref.GetValue("/Size")));
        }

        // SetStreamAndData undoes the predictor, so each entry is a row of the data.
        xref.SetStreamAndData(buf, length, budget);
        int n = n1 + n2 + n3;   // Number of bytes per entry
        List<int[]> entries = new List<int[]>();
        int offset = 0;
        for (int s = 0; s + 1 < index.Count; s += 2) {
            int number = index[s];
            for (int j = 0; j < index[s + 1] && offset + n <= xref.data.Length; j++) {
                // Process the entries in a cross-reference stream.
                // Page 51 in PDF32000_2008.pdf
                int type = (n1 == 0) ? 1 : ToInt(xref.data, offset, n1);
                if (type == 1) {
                    entries.Add(new int[] {number, ToInt(xref.data, offset + n1, n2)});
                }
                number++;
                offset += n;
            }
        }
        return GetEntryObjects(buf, entries, objects, budget);
    }

    // Adds the objects of the PDF to the list by looking for "number
    // generation obj" in it, for when the cross-reference table is missing
    // or wrong. The objects of incremental updates are later in the PDF, so
    // the newest version of an object comes last. Returns the last trailer,
    // or the last cross-reference stream object when there is no trailer, or
    // null when there is neither.
    //
    // An object ends where the next one starts at the latest, as one with no
    // endobj was read to the end of the PDF, and the last trailer is read
    // once when the scan is done: a PDF of objects with no endobj took
    // seconds for every hundred kilobytes.
    private PDFobj GetObjectsByScanning(byte[] buf, List<PDFobj> objects, PDFobj.DecodeBudget budget) {
        PDFobj xrefStream = null;
        int trailerOffset = -1;
        int next = 0;   // The start of the object after the one at i
        int i = 0;
        while (i < buf.Length) {
            if (IsObjectStart(buf, i)) {
                if (next <= i) {
                    next = NextObjectStart(buf, i + 1);
                }
                PDFobj obj = GetObject(buf, i, next);
                if (IsObject(obj, -1)) {
                    obj.number = ToInteger(obj.dict[0]);
                    objects.Add(obj);
                    if (obj.GetValue("/Type").Equals("/XRef")) {
                        xrefStream = obj;
                    }
                    if (obj.dict.Contains("stream")) {
                        // Skip the stream, as its bytes can look like an
                        // object: to the endstream after its /Length, or else
                        // to the first one before the next object, or to the
                        // next object. The endstream was looked for to the end
                        // of the PDF, and the objects after a stream with none
                        // were not read.
                        int length = ToInteger(obj.GetValue("/Length"));
                        int end = PDFobj.EndstreamAfter(buf, obj.streamOffset, length, budget);
                        if (end == -1) {
                            end = IndexOf(buf, "endstream", obj.streamOffset, next);
                        }
                        if (end == -1) {
                            // The next "number generation obj" is in the
                            // stream's own bytes, as in an embedded PDF: the
                            // stream goes on to the endstream and endobj that
                            // end it.
                            int[] past = StreamPastNext(buf, obj.streamOffset, budget);
                            if (past[0] != -1) {
                                obj.end = past[1];
                                end = past[0];
                            }
                        }
                        i = (end == -1) ? next : end;
                        continue;
                    }
                }
            } else if (StartsWith(buf, i, "trailer")) {
                trailerOffset = i;
            }
            i++;
        }
        return (trailerOffset != -1) ? GetObject(buf, trailerOffset) : xrefStream;
    }

    // Returns the offset of the endstream that ends the stream at the offset,
    // past the object start the scan found next, and the offset after the
    // endobj that must follow it; or -1 and -1. It reads within what is left of
    // the budget, so that a PDF of many streams with no endstream is not read
    // to its end for each of them.
    private static int[] StreamPastNext(byte[] buf, int offset, PDFobj.DecodeBudget budget) {
        int end = (int) Math.Min(buf.Length, (long) offset + Math.Max(budget.readLeft, 0));
        int found = PDFobj.EndstreamOf(buf, offset, end);
        if (found == -1) {
            budget.readLeft -= end - offset;
            return new int[] {-1, -1};
        }
        budget.readLeft -= found - offset;
        int after = PDFobj.EndobjAfter(buf, found);
        return (after == -1) ? new int[] {-1, -1} : new int[] {found, after};
    }

    // Returns the offset of the first "number generation obj" from the
    // offset on, or the length of the PDF when there is none.
    private static int NextObjectStart(byte[] buf, int off) {
        for (int i = off; i < buf.Length; i++) {
            if (IsObjectStart(buf, i)) {
                return i;
            }
        }
        return buf.Length;
    }

    // Returns true when "number generation obj" starts at the offset, after
    // white space or at the start of the PDF. An offset that is not at a
    // number returns at once, as the white space after it was scanned at
    // every offset of a run of white space.
    internal static bool IsObjectStart(byte[] buf, int off) {
        if (off > 0 && !IsWhiteSpace(buf[off - 1])) {
            return false;
        }
        int i = off;
        while (i < buf.Length && buf[i] >= '0' && buf[i] <= '9') {
            i++;
        }
        if (i == off) {
            return false;
        }
        int j = i;
        while (j < buf.Length && IsWhiteSpace(buf[j])) {
            j++;
        }
        int k = j;
        while (k < buf.Length && buf[k] >= '0' && buf[k] <= '9') {
            k++;
        }
        int m = k;
        while (m < buf.Length && IsWhiteSpace(buf[m])) {
            m++;
        }
        return j > i && k > j && m > k && StartsWith(buf, m, "obj");
    }

    internal static bool StartsWith(byte[] buf, int off, String str) {
        if (off + str.Length > buf.Length) {
            return false;
        }
        for (int i = 0; i < str.Length; i++) {
            if (buf[off + i] != str[i]) {
                return false;
            }
        }
        return true;
    }

    // Returns the offset of the first str from the offset from on that ends
    // by the offset to, or -1.
    internal static int IndexOf(byte[] buf, String str, int from, int to) {
        for (int i = Math.Max(from, 0); i + str.Length <= Math.Min(to, buf.Length); i++) {
            if (StartsWith(buf, i, str)) {
                return i;
            }
        }
        return -1;
    }

    // Returns the offset after the last startxref, or -1 when there is none.
    private int GetStartXRef(byte[] buf) {
        for (int i = buf.Length - 9; i >= 0; i--) {
            if (StartsWith(buf, i, "startxref")) {
                int j = i + 9;
                while (j < buf.Length && IsWhiteSpace(buf[j])) {
                    j++;
                }
                long offset = 0;
                int k = j;
                while (k < buf.Length && buf[k] >= '0' && buf[k] <= '9' && offset <= Int32.MaxValue) {
                    offset = offset * 10 + (buf[k] - '0');
                    k++;
                }
                return (k > j && offset <= Int32.MaxValue) ? (int) offset : -1;
            }
        }
        return -1;
    }

    /// <summary>
    /// Adds the outline dictionary for the bookmarks and returns its object
    /// number. The bookmarks were numbered by ToArrayList(), level by level,
    /// and the object of a bookmark is that many objects after the outline
    /// dictionary.
    /// </summary>
    internal int AddOutlineDict(Bookmark toc) {
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /Outlines\n");
        Append("/First ");
        Append(GetObjNumber() + toc.GetFirstChild().objNumber);
        Append(" 0 R\n");
        Append("/Last ");
        Append(GetObjNumber() + toc.GetLastChild().objNumber);
        Append(" 0 R\n");
        // The items that are visible: those of the first level, as the items
        // with children are closed.
        Append("/Count ");
        Append(toc.GetChildren().Count);
        Append(Token.Newline);
        Append(Token.EndDictionary);
        EndObj();
        return GetObjNumber();
    }

    /// <summary>Adds an outline item for the bookmark, under the outline dictionary with the given object number.</summary>
    internal void AddOutlineItem(int outlines, Bookmark bm1) {
        int prev = (bm1.GetPrevBookmark() == null) ? 0 : outlines + bm1.GetPrevBookmark().objNumber;
        int next = (bm1.GetNextBookmark() == null) ? 0 : outlines + bm1.GetNextBookmark().objNumber;

        int first = 0;
        int last  = 0;
        int count = 0;
        if (bm1.GetChildren() != null && bm1.GetChildren().Count > 0) {
            first = outlines + bm1.GetFirstChild().objNumber;
            last  = outlines + bm1.GetLastChild().objNumber;
            // A closed item: the items that opening it would show.
            count = (-1) * bm1.GetChildren().Count;
        }

        NewObj();
        Append(Token.BeginDictionary);
        Append("/Title ");
        AppendTextString(bm1.GetTitle());
        Append("\n");
        // The root of the bookmarks is the outline dictionary itself.
        Append("/Parent ");
        Append(outlines + bm1.GetParent().objNumber);
        Append(" 0 R\n");
        if (prev > 0) {
            Append("/Prev ");
            Append(prev);
            Append(" 0 R\n");
        }
        if (next > 0) {
            Append("/Next ");
            Append(next);
            Append(" 0 R\n");
        }
        if (first > 0) {
            Append("/First ");
            Append(first);
            Append(" 0 R\n");
        }
        if (last > 0) {
            Append("/Last ");
            Append(last);
            Append(" 0 R\n");
        }
        if (count != 0) {
            Append("/Count ");
            Append(count);
            Append("\n");
        }
        Append("/Dest [");
        Append(bm1.GetDestination().pageObjNumber);
        Append(" 0 R /XYZ ");
        Append(bm1.GetDestination().xPosition);
        Append(" ");
        Append(bm1.GetDestination().yPosition);
        Append(" 0]\n");
        Append(Token.EndDictionary);
        EndObj();
    }

    /// <summary>
    /// Adds objects read from an existing PDF to this document. The objects
    /// keep their numbers and are written as they are, so they are added
    /// before any font, image or page of this document, and an encrypted PDF
    /// cannot take them. Their page tree is the page tree of this document,
    /// so it can have no pages of its own, before or after them, and it
    /// cannot be a PDF/UA or PDF/A document, as the pages were not made for
    /// its compliance.
    /// </summary>
    public void AddObjects(List<PDFobj> objects) {
        foreach (Page page in pages) {
            if (page.mergedDict != null) {
                Fail(new InvalidOperationException("Merge and AddObjects cannot be used on the same PDF."));
            }
        }
        if (compliance != Compliance.PDF_1_7) {
            Fail(new InvalidOperationException(
                    "The objects of an existing PDF cannot be added to a PDF/UA or PDF/A document."));
        }
        if (pages.Count > 0) {
            Fail(new InvalidOperationException(
                    "The objects of an existing PDF cannot be added to a PDF that has pages of its own."));
        }
        PDFobj pagesObject = GetPagesObject(objects);
        if (pagesObject == null) {
            Fail(new ArgumentException("The objects have no root /Pages object."));
        }
        CheckObjects(objects);
        this.pagesObjNumber = Int32.Parse(pagesObject.dict[0], CultureInfo.InvariantCulture);
        AddObjectsToPDF(objects);
    }

    // Refuses objects of a PDF that was read when writing them would break this
    // document. They keep their numbers and are written as they are, so they
    // have to come before the objects that this document numbers itself, and
    // an encrypted document cannot take them.
    private void CheckObjects(List<PDFobj> objects) {
        if (completed) {
            Fail(new InvalidOperationException("The PDF was already completed."));
        }
        if (encryption != null) {
            Fail(new InvalidOperationException(
                    "The objects of an existing PDF cannot be added to an encrypted PDF."));
        }
        foreach (PDFobj obj in objects) {
            if (obj.number > 0 && obj.number <= objOffset.Count && objOffset[obj.number - 1] != 0) {
                Fail(new InvalidOperationException("Add the objects of an existing PDF before "
                        + "fonts, images or pages are added to the PDF: object "
                        + obj.number + " is already written."));
            }
        }
    }

    /// <summary>
    /// Returns the root of the page tree: the node that the /Pages of the
    /// catalog names, the catalog that the trailer's /Root names, as MuPDF
    /// and pdf.js find it. A PDF can have another tree with no /Parent,
    /// before that one: one left from an earlier version of the document, as
    /// pdf.js tests it in issue19281, or the pages of an XFA form behind a
    /// tree of one page, in xfa_issue13556. Objects with no such catalog,
    /// like those of a PDF whose trailer is lost, have the first node with no
    /// /Parent as the root.
    /// </summary>
    internal PDFobj GetPagesObject(List<PDFobj> objects) {
        foreach (PDFobj obj in objects) {
            if (obj.root) {
                List<String> tokens = ValueOf(obj);
                int i = EntryIndex(tokens, "/Pages");
                if (i != -1 && IsReference(tokens, i + 1)) {
                    PDFobj pages = PDFobj.ObjectAt(objects, tokens[i + 1]);
                    if (pages != null && EntryIndex(ValueOf(pages), "/Kids") != -1) {
                        return pages;
                    }
                }
                break;
            }
        }
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Type").Equals("/Pages") &&
                    obj.GetValue("/Parent").Equals("")) {
                return obj;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the page objects, each holding the entries it inherits from
    /// the page tree: /Resources, /MediaBox, /CropBox and /Rotate, which a
    /// PDF can write once on a node above the pages rather than on every
    /// page. A page that has an entry of its own keeps it, and the entries
    /// are added to the page the first time it is returned, so that
    /// GetPageSize and GetResourcesObject read what the page is.
    /// </summary>
    public List<PDFobj> GetPageObjects(List<PDFobj> objects) {
        List<PDFobj> pages = new List<PDFobj>();
        PDFobj pagesObject = GetPagesObject(objects);
        if (pagesObject != null) {
            GetPageObjects(pagesObject, objects, pages, new HashSet<int>(), new PageTreeNodes());
        }
        return pages;
    }

    // The nodes of the page tree that were visited are skipped, as a node of a
    // broken tree can list itself or a node above it as a kid. The tree is
    // walked with a list of the kids still to visit, the last of them first,
    // and not by recursion, as a tree of a hundred thousand nodes, one under
    // the other, overflowed the stack and ended the process.
    private void GetPageObjects(
            PDFobj root,
            List<PDFobj> objects,
            List<PDFobj> pages,
            HashSet<int> visited,
            PageTreeNodes nodes) {
        List<PDFobj> stack = new List<PDFobj>();
        stack.Add(root);
        for (bool first = true; stack.Count > 0; first = false) {
            PDFobj obj = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (!first && IsPageObject(obj)) {  // The root is a node, whatever it is.
                AddInheritedEntries(obj, objects, nodes);
                ResolveMediaBox(obj, objects);
                nodes.Forget(obj);  // A page of a broken tree can be a node.
                pages.Add(obj);
                continue;
            }
            if (!visited.Add(obj.number)) {
                continue;
            }
            List<Int32> kids = obj.GetObjectNumbers("/Kids");
            for (int i = kids.Count - 1; i >= 0; i--) {
                int number = kids[i];
                if (number >= 1 && number <= objects.Count) {   // A kid that the document has.
                    stack.Add(objects[number - 1]);
                }
            }
        }
    }

    // Adds to the page the entries it inherits from the page tree and does
    // not have itself. A page of another program's PDF often carries no
    // /MediaBox or /Resources of its own, and Read gives the objects as the
    // file has them, so the page holds what it is only after this.
    private static void AddInheritedEntries(PDFobj page, List<PDFobj> objects, PageTreeNodes nodes) {
        int open = page.dict.IndexOf("<<");
        if (open == -1) {
            return;
        }
        foreach (String key in INHERITED) {
            if (EntryIndex(ValueOf(page), key) != -1) {
                continue;       // An entry of its own.
            }
            List<String> value = InheritedValue(page, key, objects, nodes);
            if (value != null) {
                page.dict.InsertRange(open + 1, value);
                page.dict.Insert(open + 1, key);
            }
        }
    }

    // Writes the /MediaBox of the page as the array of numbers it is, when it
    // is an object of its own, "/MediaBox 4 0 R", or holds references to its
    // numbers, "[4 0 R 5 0 R 6 0 R 7 0 R]", so that GetPageSize reads it. A
    // box that is not four numbers then is left as it is: a reference to
    // anything else, like the page itself, would grow the page each time it is
    // read.
    private static void ResolveMediaBox(PDFobj page, List<PDFobj> objects) {
        int open = page.dict.IndexOf("<<");
        if (open == -1) {
            return;
        }
        List<String> tokens = page.dict.GetRange(open, page.dict.Count - open);
        int i = EntryIndex(tokens, "/MediaBox");
        if (i == -1) {
            return;
        }
        int start = open + i + 1;
        int end = open + ValueEnd(tokens, i + 1);
        List<String> value = page.dict.GetRange(start, end - start);
        if (IsReference(value, 0)) {
            PDFobj obj = PDFobj.ObjectNumbered(objects, Int32.Parse(value[0], CultureInfo.InvariantCulture));
            if (obj == null) {
                return;
            }
            value = ValueOf(obj);
        }
        if (value.Count == 0 || !value[0].Equals("[")) {
            return;
        }
        List<String> box = new List<String>();
        for (int j = 0; j < value.Count; j++) {
            if (IsReference(value, j)) {
                PDFobj obj = PDFobj.ObjectNumbered(objects, Int32.Parse(value[j], CultureInfo.InvariantCulture));
                if (obj == null) {
                    return;
                }
                box.AddRange(ValueOf(obj));
                j += 2;
            } else {
                box.Add(value[j]);
            }
            if (box.Count > 6) {
                return;
            }
        }
        if (box.Count != 6 || !box[0].Equals("[") || !box[5].Equals("]")) {
            return;
        }
        for (int j = 1; j < 5; j++) {
            if (!Double.TryParse(box[j], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
                return;
            }
        }
        page.dict.RemoveRange(start, end - start);
        page.dict.InsertRange(start, box);
    }

    private bool IsPageObject(PDFobj obj) {
        bool isPage = false;
        for (int i = 0; i < obj.dict.Count - 1; i++) {
            if (obj.dict[i].Equals("/Type") &&
                    obj.dict[i + 1].Equals("/Page")) {
                isPage = true;
            }
        }
        return isPage;
    }

    // Adds the entries of the /ExtGState dictionary of the resources, which can
    // be an object of its own, with the names that an earlier page did not add.
    private void AddExtGStates(PDFobj resources, List<PDFobj> objects) {
        List<String> entries = GetResourceEntries(resources, "/ExtGState", objects);
        int i = 0;
        while (i < entries.Count) {
            // The value after the name is a dictionary, a reference or one token.
            int end = i + 1;
            if (end < entries.Count && entries[end].Equals("<<")) {
                int level = 0;
                do {
                    String token = entries[end++];
                    if (token.Equals("<<")) {
                        level++;
                    } else if (token.Equals(">>")) {
                        level--;
                    }
                } while (level > 0 && end < entries.Count);
            } else if (end + 2 < entries.Count && entries[end + 2].Equals("R")) {
                end += 3;
            } else {
                end = Math.Min(end + 1, entries.Count);
            }
            if (!importedExtGStates.Contains(entries[i])) {
                importedExtGStates.AddRange(entries.GetRange(i, end - i));
            } else {
                CheckImportedName(importedExtGStates, entries.GetRange(i, end - i));
            }
            i = end;
        }
    }

    private List<PDFobj> GetFontObjects(PDFobj resources, List<PDFobj> objects) {
        List<PDFobj> fonts = new List<PDFobj>();
        // The /Font dictionary holds one "/Name number 0 R" entry per font, and
        // can be an object of its own. Every entry is written to the resources
        // object, so every font it names is collected here.
        List<String> entries = GetResourceEntries(resources, "/Font", objects);
        int i = 0;
        while (i < entries.Count) {
            String token = entries[i];
            if (token.StartsWith("/", StringComparison.Ordinal) && (i + 3) < entries.Count && entries[i + 3].Equals("R")) {
                // Pages can carry separate resource dictionaries that name the
                // same fonts. They are merged into one /Font dictionary here,
                // so a name that is already present must not be added twice.
                if (!importedFonts.Contains(token)) {
                    importedFonts.AddRange(entries.GetRange(i, 4));
                    int number = ToInteger(entries[i + 1]);
                    if (number > 0 && number <= objects.Count) {
                        fonts.Add(objects[number - 1]);
                    }
                } else {
                    CheckImportedName(importedFonts, entries.GetRange(i, 4));
                }
                i += 4;
                continue;
            }
            importedFonts.Add(token);
            i += 1;
        }
        return fonts;
    }

    // Records the mistake when a name that an earlier page added to the
    // resources has another value on this page, the entry: the pages of this
    // document share one resources dictionary, where the name has the value
    // of the first page, and the content of this page, drawn with
    // DrawContents, would draw the resource of the first page.
    private void CheckImportedName(List<String> imported, List<String> entry) {
        int i = imported.IndexOf(entry[0]);
        if (i == -1) {
            return;
        }
        bool same = i + entry.Count <= imported.Count;
        for (int j = 1; same && j < entry.Count; j++) {
            same = imported[i + j].Equals(entry[j]);
        }
        if (same) {
            return;
        }
        Fail(new ArgumentException("The pages of the PDF use the name " + entry[0]
                + " for different resources, and the pages of this document share one resources dictionary."));
    }

    /// <summary>
    /// Returns the entries of a sub-dictionary of the resources, like /XObject,
    /// without the brackets around them. The sub-dictionary can also be an
    /// object of its own.
    /// </summary>
    private List<String> GetResourceEntries(
            PDFobj resources, String name, List<PDFobj> objects) {
        List<String> entries = new List<String>();
        List<String> dict = resources.GetDict();
        int i = dict.IndexOf(name) + 1;
        if (i == 0 || i >= dict.Count) {
            return entries;
        }
        if (IsInteger(dict[i])) {   // "/XObject 12 0 R"
            int number = ToInteger(dict[i]);
            if (number < 1 || number > objects.Count) {
                return entries;     // An object that the PDF does not have.
            }
            dict = objects[number - 1].GetDict();
            i = dict.IndexOf("<<");
            if (i == -1) {
                return entries;
            }
        }
        if (!dict[i].Equals("<<")) {
            return entries;
        }
        int level = 1;
        while (++i < dict.Count) {
            String token = dict[i];
            if (token.Equals("<<")) {
                ++level;
            } else if (token.Equals(">>") && --level == 0) {
                break;
            }
            entries.Add(token);
        }
        return entries;
    }

    private static bool IsInteger(String token) {
        if (token.Length == 0) {
            return false;
        }
        foreach (char c in token) {
            if (c < '0' || c > '9') {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns the numbers of the objects that "number 0 R" references in the
    /// tokens refer to. A number that is too large for an int is left out, as
    /// in the other ports.
    /// </summary>
    private List<Int32> GetReferences(List<String> tokens) {
        List<Int32> numbers = new List<Int32>();
        for (int i = 0; i + 2 < tokens.Count; i++) {
            if (tokens[i + 2].Equals("R")
                    && IsInteger(tokens[i]) && IsInteger(tokens[i + 1])) {
                int number = ToInteger(tokens[i]);
                if (number >= 0) {
                    numbers.Add(number);
                }
                i += 2;
            }
        }
        return numbers;
    }

    /// <summary>
    /// Collects the object with the given number and every object it refers to,
    /// directly or through other objects, like the color space of an image or
    /// the resources of a form XObject. The page tree is not followed. The
    /// objects are found with a list of the numbers still to visit, and not by
    /// recursion, as a chain of a hundred thousand objects, each referring to
    /// the next, overflowed the stack and ended the process.
    /// </summary>
    private void AddObjectTree(
            int objNumber, List<PDFobj> objects, HashSet<Int32> numbers, List<PDFobj> resources) {
        List<int> stack = new List<int>();
        stack.Add(objNumber);
        while (stack.Count > 0) {
            int number = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (number <= 0 || number > objects.Count || !numbers.Add(number)) {
                continue;
            }
            PDFobj obj = objects[number - 1];
            String type = obj.GetValue("/Type");
            if (obj.dict.Count == 0
                    || type.Equals("/Page") || type.Equals("/Pages") || type.Equals("/Catalog")) {
                continue;
            }
            resources.Add(obj);
            List<Int32> references = GetReferences(obj.dict);
            for (int i = references.Count - 1; i >= 0; i--) {
                stack.Add(references[i]);
            }
        }
    }

    /// <summary>
    /// Collects the images and forms in the /XObject resources, with the
    /// objects they use, and adds their names to the resources object.
    /// </summary>
    private void AddXObjects(
            PDFobj resObj, List<PDFobj> objects, HashSet<Int32> numbers, List<PDFobj> resources) {
        List<String> entries = GetResourceEntries(resObj, "/XObject", objects);
        int i = 0;
        while (i < entries.Count) {
            String token = entries[i];
            if (token.StartsWith("/", StringComparison.Ordinal) && (i + 3) < entries.Count
                    && entries[i + 3].Equals("R")) {
                // Like the fonts, a name that an earlier page added is kept.
                if (!importedXObjects.Contains(token)) {
                    importedXObjects.AddRange(entries.GetRange(i, 4));
                    AddObjectTree(ToInteger(entries[i + 1]), objects, numbers, resources);
                } else {
                    CheckImportedName(importedXObjects, entries.GetRange(i, 4));
                }
                i += 4;
            } else {
                i += 1;
            }
        }
    }

    /// <summary>
    /// Adds the fonts, images and graphics states used by the pages to this
    /// document. The objects keep their numbers and are written as they are,
    /// so they are added before any font, image or page of this document, and
    /// an encrypted PDF cannot take them.
    /// </summary>
    public void AddResourceObjects(List<PDFobj> objects) {
        List<PDFobj> resources = new List<PDFobj>();
        HashSet<Int32> numbers = new HashSet<Int32>();
        CheckObjects(objects);
        List<PDFobj> pages = GetPageObjects(objects);
        foreach (PDFobj page in pages) {
            PDFobj resObj = page.GetResourcesObject(objects);
            if (resObj == null) {
                continue;       // A page without resources of its own.
            }
            // A font is copied with every object that it refers to: its
            // descriptor and font file, and also the widths, the encoding and
            // the other entries that can be objects of their own.
            foreach (PDFobj font in GetFontObjects(resObj, objects)) {
                AddObjectTree(font.number, objects, numbers, resources);
            }
            AddXObjects(resObj, objects, numbers, resources);
            AddExtGStates(resObj, objects);
            // The /ExtGState entries are copied as they are, so the objects
            // that they refer to have to be copied too.
            foreach (int number in GetReferences(GetResourceEntries(resObj, "/ExtGState", objects))) {
                AddObjectTree(number, objects, numbers, resources);
            }
        }
        resources.Sort(delegate(PDFobj o1, PDFobj o2){
            return o1.number.CompareTo(o2.number);
        });
        // An object can be collected twice, like a font that a form XObject
        // uses too, and must be written once.
        List<PDFobj> unique = new List<PDFobj>();
        foreach (PDFobj obj in resources) {
            if (unique.Count == 0 || unique[unique.Count - 1].number != obj.number) {
                unique.Add(obj);
            }
        }
        AddObjectsToPDF(unique);
    }

    private void AddObjectsToPDF(List<PDFobj> objects) {
        foreach (PDFobj obj in objects) {
            if (obj.dict.Count == 0 && obj.stream == null) {
                // A number that the PDF that was read has no object for stays
                // a free entry of the cross-reference table.
                continue;
            }
            if (obj.offset == 0) {
                // Create new object.
                SetObjOffset(obj.number, byteCount);
                Append(obj.number);
                Append(Token.NewObj);
                if (obj.dict != null) {
                    foreach (String token in obj.dict) {
                        AppendToken(token);
                        Append(' ');
                    }
                }
                if (obj.stream != null) {
                    if (obj.dict.Count == 0) {
                        Append("<< /Length ");
                        Append(obj.stream.Length);
                        Append(" >>");
                    }
                    Append(Token.Newline);
                    Append(Token.Stream);
                    Append(obj.stream, 0, obj.stream.Length);
                    Append(Token.EndStream);
                }
                Append(Token.EndObj);
            } else {
                SetObjOffset(obj.number, byteCount);
                int n = obj.dict.Count;
                String token = null;
                for (int i = 0; i < n; i++) {
                    token = obj.dict[i];
                    AppendToken(token);
                    if (i < (n - 1)) {
                        Append(Token.Space);
                    } else {
                        Append(Token.Newline);
                    }
                }
                if (obj.stream != null) {
                    Append(obj.stream, 0, obj.stream.Length);
                    Append(Token.EndStream);
                }
                if (token == null || !token.Equals("endobj")) {
                    Append(Token.EndObj);
                }
            }
        }
    }
}   // End of PDF.cs
}   // End of namespace PDFjet.NET
