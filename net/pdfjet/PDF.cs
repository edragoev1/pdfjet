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
using System.Reflection;

namespace PDFjet.NET {
/// <summary>
/// Used to create PDF objects that represent PDF documents.
/// </summary>
public class PDF {
    internal List<Font> fonts = new List<Font>();
    internal List<Image> images = new List<Image>();
    internal List<OptionalContentGroup> groups = new List<OptionalContentGroup>();
    internal Dictionary<String, Int32> states = new Dictionary<String, Int32>();
    internal List<Stamp> stamps = new List<Stamp>();
    internal List<StructElem> structElements = new List<StructElem>();
    internal static readonly CultureInfo culture_en_us = new CultureInfo("en-US");
    internal Compliance compliance = Compliance.PDF_1_7;
    internal Bookmark toc = null;
    internal Encryption encryption = null;

    private int metadataObjNumber = 0;
    private int outputIntentObjNumber = 0;
    private List<Page> pages = new List<Page>();
    private Dictionary<String, Destination> destinations = new Dictionary<String, Destination>();
    private String uuid = (new Salsa20()).GetID();
    private Stream os = null;
    private readonly List<Int32> objOffset = new List<Int32>(); // Required by the xref section
    private String producer = "PDFjet v8.7.0";
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
    private List<String> importedFonts = new List<String>();
    private List<String> importedXObjects = new List<String>();
    private String extGState = "";
    private Page prevPage = null;
    private bool contentStreamsCompression = true;

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
    /// <param name="compliance">must be: Compliance.PDF_UA_1 or Compliance.PDF_A_1A to Compliance.PDF_A_3B</param>
    public PDF(Stream os, Compliance compliance) {
        this.compliance = compliance;
        SetOutputStream(os);
    }

    /// <summary>Sets the stream the document is written to.</summary>
    public PDF SetOutputStream(Stream os) {
        this.os = os;

        DateTime date = new DateTime(DateTime.Now.Ticks);
        SimpleDateFormat sdf1 = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
        createDate = sdf1.Format(date);     // XMP metadata

        Append("%PDF-1.7\n");
        Append('%');
        Append((byte) 0xF2);
        Append((byte) 0xF3);
        Append((byte) 0xF4);
        Append((byte) 0xF5);
        Append((byte) 0xF6);
        Append(Token.Newline);
        return this;
    }

    /// <summary>Returns the stream the document is written to.</summary>
    public Stream GetOutputStream() {
        return this.os;
    }

    /// <summary>Sets the PDF/UA or PDF/A compliance of this document.</summary>
    public PDF SetCompliance(Compliance compliance) {
        this.compliance = compliance;
        return this;
    }

    /// <summary>Sets the encryption applied to this document.</summary>
    public PDF SetEncryption(Encryption encryption) {
        this.encryption = encryption;
        return this;
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
    private void SetObjOffset(int number, int offset) {
        if (number <= 0) {          // No number of its own - just append.
            objOffset.Add(offset);
            return;
        }
        while (objOffset.Count < number) {
            objOffset.Add(0);
        }
        objOffset[number - 1] = offset;
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
            sb.Append(notice);
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
            } else if (compliance == Compliance.PDF_A_3A) {
                sb.Append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>A</pdfaid:conformance>\n");
            } else if (compliance == Compliance.PDF_A_3B) {
                sb.Append("  <pdfaid:part>3</pdfaid:part>\n");
                sb.Append("  <pdfaid:conformance>B</pdfaid:conformance>\n");
            }

            sb.Append("  <pdf:Producer>");
            sb.Append(producer);
            sb.Append("</pdf:Producer>\n");

            if (title != null) {
                sb.Append("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.Append(title);
                sb.Append("</rdf:li></rdf:Alt></dc:title>\n");
            }

            if (author != null) {
                sb.Append("  <dc:creator><rdf:Seq><rdf:li>");
                sb.Append(author);
                sb.Append("</rdf:li></rdf:Seq></dc:creator>\n");
            }

            if (subject != null) {
                sb.Append("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                sb.Append(subject);
                sb.Append("</rdf:li></rdf:Alt></dc:description>\n");
            }

            if (keywords != null) {
                sb.Append("  <pdf:Keywords>");
                sb.Append(keywords);
                sb.Append("</pdf:Keywords>\n");
            }

            if (creator != null) {
                sb.Append("  <xmp:CreatorTool>");
                sb.Append(creator);
                sb.Append("</xmp:CreatorTool>\n");
            }

            sb.Append("  <xmp:CreateDate>");
            sb.Append(createDate + "-05:00");       // Append the time zone.
            sb.Append("</xmp:CreateDate>\n");

            sb.Append("  <xapMM:DocumentID>uuid:");
            sb.Append(uuid);
            sb.Append("</xapMM:DocumentID>\n");

            sb.Append("  <xapMM:InstanceID>uuid:");
            sb.Append(uuid);
            sb.Append("</xapMM:InstanceID>\n");

            sb.Append("</rdf:Description>\n");
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
        if (!extGState.Equals("")) {
            AppendToken(extGState);
        }

        if (fonts.Count > 0 || importedFonts.Count > 0) {
            Append("/Font <<\n");
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

        // Check if we have any XObjects
        if (images.Count > 0 || stamps.Count > 0 || importedXObjects.Count > 0) {
            Append("/XObject <<\n"); // Write the key and open the dictionary ONCE
            AppendImportedEntries(importedXObjects);
            // Add all Images to the same dictionary
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
            Append(">>\n"); // Close the dictionary
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
        // String state = "/CA 0.5 /ca 0.5";
        if (states.Count > 0) {
            Append("/ExtGState <<\n");
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
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /Pages\n");
        Append("/Kids [\n");
        foreach (Page page in pages) {
            if (compliance != Compliance.PDF_1_7) {
                page.SetStructElementsPageObjNumber(page.objNumber);
            }
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

    private int AddStructTreeRootObject() {
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /StructTreeRoot\n");
        Append("/ParentTree ");
        Append(GetObjNumber() + 1);
        Append(Token.ObjRef);
        Append("/K [\n");
        Append(GetObjNumber() + 2);
        Append(Token.ObjRef);
        Append("]\n");
        Append(Token.EndDictionary);
        EndObj();
        return GetObjNumber();
    }

    private int AddStructDocumentObject(int parent) {
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /StructElem\n");
        Append("/S /Document\n");
        Append("/P ");
        Append(parent);
        Append(Token.ObjRef);
        Append("/K [\n");
        foreach (StructElem structElement in this.structElements) {
            Append(structElement.objNumber);
            Append(" 0 R\n");
        }
        Append("]\n");
        Append(Token.EndDictionary);
        EndObj();
        return GetObjNumber();
    }

    private void AddStructElementObjects() {
        int structTreeRootObjNumber = GetObjNumber() + 1;
        structTreeRootObjNumber += this.structElements.Count;

        foreach (StructElem element in this.structElements) {
            NewObj();
            element.objNumber = GetObjNumber();
            Append("<<\n/Type /StructElem /S /");
            Append(element.structure);
            Append("\n/P ");
            Append(structTreeRootObjNumber + 2);    // Use the document struct as parent!
            Append(" 0 R\n/Pg ");
            Append(element.pageObjNumber);
            Append(Token.ObjRef);

            if (element.annotation != null) {
                Append("/K <</Type /OBJR /Obj ");
                Append(element.annotation.objNumber);
                Append(" 0 R>>\n");
            } else {
                Append("/K ");
                Append(element.mcid);
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
                byte[] actualTextBytes = Encoding.UTF8.GetBytes(element.actualText);
                if (encryption != null) {
                    actualTextBytes = AES256.Encrypt(actualTextBytes, encryption.GetKey());
                }
                Append("/ActualText <");
                Append(Util.ToHexString(actualTextBytes));
                Append(">\n");
            }

            if (hasAltDescription) {
                byte[] altDescriptionBytes = Encoding.UTF8.GetBytes(element.altDescription);
                if (encryption != null) {
                    altDescriptionBytes = AES256.Encrypt(altDescriptionBytes, encryption.GetKey());
                }
                Append("/Alt <");
                Append(Util.ToHexString(altDescriptionBytes));
                Append(">\n");
            }

            Append(">>\n");
            EndObj();
        }
    }

    private void AddNumsParentTree() {
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Nums [\n");
        // The keys must be listed in increasing order, so the page entries -
        // whose keys are the /StructParents values 0 .. pages.Count-1 - come
        // first. Each value is the array of struct elements of that page,
        // indexed by the MCID they were marked with.
        for (int i = 0; i < pages.Count; i++) {
            Append(i);
            Append(" [");
            foreach (StructElem element in pages[i].structures) {
                if (element.annotation == null) {
                    Append(Token.Space);
                    Append(element.objNumber);
                    Append(" 0 R");
                }
            }
            Append("]\n");
        }
        // The annotations follow, keyed by the /StructParent values handed out
        // by AddAnnotDictionaries(), which continue where the pages left off.
        int structParent = pages.Count;
        foreach (StructElem element in this.structElements) {
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

            Append("/StructTreeRoot ");
            Append(structTreeRootObjNumber);
            Append(" 0 R\n");

            Append("/MarkInfo <</Marked true>>\n");
            Append("/ViewerPreferences <</DisplayDocTitle true>>\n");
        }

        if (pageLayout != null) {
            Append("/PageLayout /");
            Append(pageLayout);
            Append(Token.Newline);
        }

        if (pageMode != null) {
            Append("/PageMode /");
            Append(pageMode);
            Append(Token.Newline);
        }

        AddOCProperties();

        Append("/Pages ");
        Append(pagesObjNumber);
        Append(" 0 R\n");

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

    private void SetDestinationObjNumbers() {
        int numberOfAnnotations = 0;
        foreach (Page page in pages) {
            numberOfAnnotations += page.annots.Count;
        }
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            foreach (Destination destination in page.destinations) {
                destination.pageObjNumber =
                        GetObjNumber() + numberOfAnnotations + i + 1;
                destinations[destination.name] = destination;
            }
        }
    }

    private void AddAllPages(int resObjNumber) {
        SetDestinationObjNumbers();
        AddAnnotDictionaries();

        // Calculate the object number of the Pages object
        pagesObjNumber = GetObjNumber() + pages.Count + 1;

        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];

            // Page object
            NewObj();
            page.objNumber = GetObjNumber();
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

            if (compliance != Compliance.PDF_1_7) {
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
        if (contentStreamsCompression) {
            byte[] buf = Compressor.Deflate(page.buf.ToArray());
            if (encryption != null) {
                buf = AES256.Encrypt(buf, encryption.GetKey());
            }
            page.buf.Dispose();

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
            page.buf.Dispose();

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
            page.buf = null;    // Release the page content memory!
            page.contents.Add(GetObjNumber());
        }
    }

    private int AddAnnotationObject(Annotation annot, int index) {
        NewObj();
        annot.objNumber = GetObjNumber();
        Append(Token.BeginDictionary);
        Append("/Type /Annot\n");
        Append("/Subtype /");
        Append(annot.annotationType);
        Append("\n");

        Append("/Rect [");
        Append(annot.x1);
        Append(' ');
        Append(annot.y1);
        Append(' ');
        Append(annot.x2);
        Append(' ');
        Append(annot.y2);
        Append("]\n");
        Append("/Border [0 0 0]\n");

        if (annot.annotationType.Equals(Annotation.FileAttachment)) {
            Append("/FS ");
            Append(annot.fileAttachment.embeddedFile.objNumber);
            Append(" 0 R\n");
            Append("/Name /");
            Append(annot.fileAttachment.icon);
            Append("\n");

            if (annot.fileAttachment.title != null) {
                byte[] title = Encoding.UTF8.GetBytes(annot.fileAttachment.title);
                if (encryption != null) {
                    title = AES256.Encrypt(title, encryption.GetKey());
                }
                Append("/T <");
                Append(Util.ToHexString(title));
                Append(">\n");
            }

            if (annot.fileAttachment.contents != null) {
                byte[] contents = Encoding.UTF8.GetBytes(annot.fileAttachment.contents);
                if (encryption != null) {
                    contents = AES256.Encrypt(contents, encryption.GetKey());
                }
                Append("/Contents <");
                Append(Util.ToHexString(contents));
                Append(">\n");
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
                byte[] bytes = Encoding.UTF8.GetBytes(description);
                if (encryption != null) {
                    bytes = AES256.Encrypt(bytes, encryption.GetKey());
                }
                Append("/Contents <");
                Append(Util.ToHexString(bytes));
                Append(">\n");
            }
            if (annot.uri != null) {
                Append("/F 4\n");
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
            } else if (annot.key != null) {
                Destination destination = destinations[annot.key];
                if (destination != null) {
                    Append("/F 4\n");
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

            Append("/CA ");
            Append(annot.transparency);
            Append("\n");

            if (annot.title != null) {
                byte[] title = Encoding.UTF8.GetBytes(annot.title);
                if (encryption != null) {
                    title = AES256.Encrypt(title, encryption.GetKey());
                }
                Append("/T <");
                Append(Util.ToHexString(title));
                Append(">\n");
            }

            if (annot.contents != null) {
                byte[] contents = Encoding.UTF8.GetBytes(annot.contents);
                if (encryption != null) {
                    contents = AES256.Encrypt(contents, encryption.GetKey());
                }
                Append("/Contents <");
                Append(Util.ToHexString(contents));
                Append(">\n");
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

            Append("/CA ");
            Append(annot.transparency);
            Append("\n");

            if (annot.title != null) {
                byte[] title = Encoding.UTF8.GetBytes(annot.title);
                if (encryption != null) {
                    title = AES256.Encrypt(title, encryption.GetKey());
                }
                Append("/T <");
                Append(Util.ToHexString(title));
                Append(">\n");
            }

            if (annot.contents != null) {
                byte[] contents = Encoding.UTF8.GetBytes(annot.contents);
                if (encryption != null) {
                    contents = AES256.Encrypt(contents, encryption.GetKey());
                }
                Append("/Contents <");
                Append(Util.ToHexString(contents));
                Append(">\n");
            }
        } else if (annot.annotationType.Equals(Annotation.Text)) {
            Append("/Name /Comment\n");

            if (annot.title != null) {
                byte[] title = Encoding.UTF8.GetBytes(annot.title);
                if (encryption != null) {
                    title = AES256.Encrypt(title, encryption.GetKey());
                }
                Append("/T <");
                Append(Util.ToHexString(title));
                Append(">\n");
            }

            if (annot.contents != null) {
                byte[] contents = Encoding.UTF8.GetBytes(annot.contents);
                if (encryption != null) {
                    contents = AES256.Encrypt(contents, encryption.GetKey());
                }
                Append("/Contents <");
                Append(Util.ToHexString(contents));
                Append(">\n");
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
        foreach (StructElem element in this.structElements) {
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
            list.Sort((x, y) => x.name.CompareTo(y.name));

            Append("/OCProperties\n");
            Append("<<\n");
            Append("/OCGs [");
            Append(buf.ToString());
            Append(" ]\n");
            Append("/D <<\n");

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
        pages.Add(page);
        if (prevPage != null) {
            AddPageContent(prevPage);
        }
        prevPage = page;
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
        if (prevPage != null) {
            AddPageContent(prevPage);
        }
        if (compliance != Compliance.PDF_1_7) {
            metadataObjNumber = AddMetadataObject("", false);
            outputIntentObjNumber = AddOutputIntentObject();
        }

        if (pagesObjNumber == 0) {
            AddAllPages(AddResourcesObject());
            AddPagesObject();
        }

        int structTreeRootObjNumber = 0;
        if (compliance != Compliance.PDF_1_7) {
            AddStructElementObjects();
            structTreeRootObjNumber = AddStructTreeRootObject();
            AddNumsParentTree();
            AddStructDocumentObject(structTreeRootObjNumber);
        }

        int outlineDictNum = 0;
        if (toc != null && toc.GetChildren() != null) {
            List<Bookmark> list = toc.ToArrayList();
            outlineDictNum = AddOutlineDict(toc);
            for (int i = 1; i < list.Count; i++) {
                Bookmark bookmark = list[i];
                AddOutlineItem(outlineDictNum, i, bookmark);
            }
        }

        int rootObjNumber = AddRootObject(structTreeRootObjNumber, outlineDictNum);
        int startxref = byteCount;

        // Create the xref table
        Append("xref\n");
        Append("0 ");
        Append(rootObjNumber + 1);
        Append('\n');

        Append("0000000000 65535 f \n");
        foreach (int offset in objOffset) {
            if (offset == 0) {      // A number that no object was written for.
                Append("0000000000 65535 f \n");
                continue;
            }
            String str = offset.ToString();
            for (int i = 0; i < 10 - str.Length; i++) {
                Append('0');
            }
            Append(str);
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

        Append("/Root ");
        Append(rootObjNumber);
        Append(" 0 R\n");

        Append(">>\n");
        Append("startxref\n");
        Append(startxref);
        Append('\n');
        Append("%%EOF\n");

        os.Close();
    }

    /// <summary>
    /// Set the "Title" document property of the PDF file.
    /// </summary>
    /// <param name="title">The title of this document.</param>
    /// <returns>this PDF object.</returns>
    public PDF SetTitle(String title) {
        this.title = title;
        return this;
    }

    /// <summary>
    /// Set the "Author" document property of the PDF file.
    /// </summary>
    /// <param name="author">The author of this document.</param>
    /// <returns>this PDF object.</returns>
    public PDF SetAuthor(String author) {
        this.author = author;
        return this;
    }

    /// <summary>
    /// Set the "Subject" document property of the PDF file.
    /// </summary>
    /// <param name="subject">The subject of this document.</param>
    /// <returns>this PDF object.</returns>
    public PDF SetSubject(String subject) {
        this.subject = subject;
        return this;
    }

    /// <summary>Sets the keywords in the document metadata.</summary>
    public PDF SetKeywords(String keywords) {
        this.keywords = keywords;
        return this;
    }

    /// <summary>Sets the creator in the document metadata.</summary>
    public PDF SetCreator(String creator) {
        this.creator = creator;
        return this;
    }

    /// <summary>Sets the page layout used when the document is opened. See PageLayout.</summary>
    public PDF SetPageLayout(String pageLayout) {
        this.pageLayout = pageLayout;
        return this;
    }

    /// <summary>Sets the page mode used when the document is opened. See PageMode.</summary>
    public PDF SetPageMode(String pageMode) {
        this.pageMode = pageMode;
        return this;
    }

    internal void Append(int num) {
        Append(num.ToString());
    }

    internal void Append(float f) {
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
        byteCount += (int) baos.Length;
    }

    internal List<PDFobj> GetSortedObjects(List<PDFobj> objects) {
        List<PDFobj> sorted = new List<PDFobj>();

        int maxObjNumber = 0;
        foreach (PDFobj obj in objects) {
            if (obj.number > maxObjNumber) {
                maxObjNumber = obj.number;
            }
        }

        for (int number = 1; number <= maxObjNumber; number++) {
            PDFobj obj = new PDFobj();
            obj.SetNumber(number);
            sorted.Add(obj);
        }

        foreach (PDFobj obj in objects) {
            sorted[obj.number - 1] = obj;
        }

        return sorted;
    }

    /// <summary>
    /// Reads the objects of an existing PDF from the stream. An encrypted PDF
    /// is decrypted when it opens without a password.
    /// </summary>
    public List<PDFobj> Read(Stream inputStream) {
        byte[] buf = Content.GetFromStream(inputStream);

        List<PDFobj> objects1 = new List<PDFobj>();
        PDFobj trailer = null;
        try {
            trailer = GetObjects(buf, GetStartXRef(buf), objects1, 0);
        } catch (Exception) {
            trailer = null;     // A cross-reference stream that cannot be decoded.
        }
        if (trailer == null || objects1.Count == 0) {
            // The cross-reference table is missing or wrong, like in a PDF
            // that was changed without updating it.
            objects1.Clear();
            trailer = GetObjectsByScanning(buf, objects1);
        }
        Decryptor decryptor = Decryptor.GetDecryptor(trailer, objects1);

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
                obj.SetStreamAndData(buf, obj.GetLength(objects1), decryptor);
            }

            if (type.Equals("/ObjStm")) {
                int first = Int32.Parse(obj.GetValue("/First"));
                PDFobj o2 = GetObject(obj.data, 0, first);
                int count = o2.dict.Count;
                for (int i = 0; i < count; i += 2) {
                    String num = o2.dict[i];
                    int off = Int32.Parse(o2.dict[i + 1]);
                    int end = obj.data.Length;
                    if (i <= count - 4) {
                        end = first + Int32.Parse(o2.dict[i + 3]);
                    }
                    PDFobj o3 = GetObject(obj.data, first + off, end);
                    o3.SetNumber(Int32.Parse(num));
                    o3.dict.Insert(0, "obj");
                    o3.dict.Insert(0, "0");
                    o3.dict.Insert(0, num);
                    objects2.Add(o3);
                }
            } else {
                objects2.Add(obj);
            }
        }

        return GetSortedObjects(objects2);
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
            obj.streamOffset = off;
            if (off < buf.Length && buf[off] == '\n') {
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

    private PDFobj GetObject(byte[] buf, int off) {
        if (off < 0 || off >= buf.Length) {
            return new PDFobj();    // An offset outside of the PDF has no tokens.
        }
        return GetObject(buf, off, buf.Length);
    }

    private PDFobj GetObject(byte[] buf, int off, int len) {
        PDFobj obj = new PDFobj();
        obj.offset = off;
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

        return obj;
    }

    private static bool IsWhiteSpace(int c) {
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
        return (IsInteger(token) && Int32.TryParse(token, out int value)) ? value : -1;
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
    // of its object.
    private PDFobj GetObjects(byte[] buf, int offset, List<PDFobj> objects, int depth) {
        PDFobj xref = GetObject(buf, offset);
        bool table = xref.dict.Count > 0 && xref.dict[0].Equals("xref");
        if (depth > 1000 || (!table && !IsObject(xref, -1))) {
            return null;
        }
        String prev = xref.GetValue("/Prev");
        if (!prev.Equals("") && GetObjects(buf, ToInteger(prev), objects, depth + 1) == null) {
            return null;
        }
        if (table) {
            // The objects in the table replace those in the /XRefStm stream.
            String xrefStm = xref.GetValue("/XRefStm");
            if (!xrefStm.Equals("") &&
                    !GetStreamObjects(buf, GetObject(buf, ToInteger(xrefStm)), objects)) {
                return null;
            }
            if (!GetTableObjects(buf, xref, objects)) {
                return null;
            }
        } else if (!GetStreamObjects(buf, xref, objects)) {
            return null;
        }
        return xref;
    }

    // Adds the objects in use of a cross-reference table, and returns false
    // when an offset is not that of its object.
    private bool GetTableObjects(byte[] buf, PDFobj xref, List<PDFobj> objects) {
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
                // The entry is the offset, the generation number and n for an object in use.
                if (dict[i + 2].Equals("n")) {
                    PDFobj obj = GetObject(buf, ToInteger(dict[i]));
                    if (!IsObject(obj, number)) {
                        return false;
                    }
                    obj.number = number;
                    objects.Add(obj);
                }
            }
        }
        return i < dict.Count && dict[i].Equals("trailer");
    }

    // Adds the objects of a cross-reference stream that are not in object
    // streams, and returns false when an offset is not that of its object.
    private bool GetStreamObjects(byte[] buf, PDFobj xref, List<PDFobj> objects) {
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
        xref.SetStreamAndData(buf, length);
        int n = n1 + n2 + n3;   // Number of bytes per entry
        int offset = 0;
        for (int s = 0; s + 1 < index.Count; s += 2) {
            int number = index[s];
            for (int j = 0; j < index[s + 1] && offset + n <= xref.data.Length; j++) {
                // Process the entries in a cross-reference stream.
                // Page 51 in PDF32000_2008.pdf
                int type = (n1 == 0) ? 1 : ToInt(xref.data, offset, n1);
                if (type == 1) {
                    PDFobj obj = GetObject(buf, ToInt(xref.data, offset + n1, n2));
                    if (!IsObject(obj, number)) {
                        return false;
                    }
                    obj.number = number;
                    objects.Add(obj);
                }
                number++;
                offset += n;
            }
        }
        return true;
    }

    // Adds the objects of the PDF to the list by looking for "number
    // generation obj" in it, for when the cross-reference table is missing
    // or wrong. The objects of incremental updates are later in the PDF, so
    // the newest version of an object comes last. Returns the last trailer,
    // or the last cross-reference stream object when there is no trailer, or
    // null when there is neither.
    private PDFobj GetObjectsByScanning(byte[] buf, List<PDFobj> objects) {
        PDFobj trailer = null;
        PDFobj xrefStream = null;
        int i = 0;
        while (i < buf.Length) {
            if (IsObjectStart(buf, i)) {
                PDFobj obj = GetObject(buf, i);
                if (IsObject(obj, -1)) {
                    obj.number = ToInteger(obj.dict[0]);
                    objects.Add(obj);
                    if (obj.GetValue("/Type").Equals("/XRef")) {
                        xrefStream = obj;
                    }
                    if (obj.dict.Contains("stream")) {
                        // Skip the stream, as its bytes can look like an object.
                        int end = IndexOf(buf, "endstream", obj.streamOffset);
                        i = (end == -1) ? buf.Length : end;
                        continue;
                    }
                }
            } else if (StartsWith(buf, i, "trailer")) {
                trailer = GetObject(buf, i);
            }
            i++;
        }
        return (trailer != null) ? trailer : xrefStream;
    }

    // Returns true when "number generation obj" starts at the offset, after
    // white space or at the start of the PDF.
    private static bool IsObjectStart(byte[] buf, int off) {
        if (off > 0 && !IsWhiteSpace(buf[off - 1])) {
            return false;
        }
        int i = off;
        while (i < buf.Length && buf[i] >= '0' && buf[i] <= '9') {
            i++;
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
        return i > off && j > i && k > j && m > k && StartsWith(buf, m, "obj");
    }

    private static bool StartsWith(byte[] buf, int off, String str) {
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

    private static int IndexOf(byte[] buf, String str, int from) {
        for (int i = Math.Max(from, 0); i + str.Length <= buf.Length; i++) {
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

    /// <summary>Adds the outline dictionary for the bookmarks and returns its object number.</summary>
    public int AddOutlineDict(Bookmark toc) {
        int numOfChildren = GetNumOfChildren(0, toc);
        NewObj();
        Append(Token.BeginDictionary);
        Append("/Type /Outlines\n");
        Append("/First ");
        Append(GetObjNumber() + 1);
        Append(" 0 R\n");
        Append("/Last ");
        Append(GetObjNumber() + numOfChildren);
        Append(" 0 R\n");
        Append("/Count ");
        Append(numOfChildren);
        Append(Token.Newline);
        Append(Token.EndDictionary);
        EndObj();
        return GetObjNumber();
    }

    /// <summary>Adds an outline item for the specified bookmark.</summary>
    public void AddOutlineItem(int parent, int i, Bookmark bm1) {
        int prev = (bm1.GetPrevBookmark() == null) ? 0 : parent + (i - 1);
        int next = (bm1.GetNextBookmark() == null) ? 0 : parent + (i + 1);

        int first = 0;
        int last  = 0;
        int count = 0;
        if (bm1.GetChildren() != null && bm1.GetChildren().Count > 0) {
            first = parent + bm1.GetFirstChild().objNumber;
            last  = parent + bm1.GetLastChild().objNumber;
            count = (-1) * GetNumOfChildren(0, bm1);
        }

        byte[] title = Encoding.UTF8.GetBytes(bm1.GetTitle());
        if (encryption != null) {
            title = AES256.Encrypt(title, encryption.GetKey());
        }

        NewObj();
        Append(Token.BeginDictionary);
        Append("/Title <");
        Append(Util.ToHexString(title));
        Append(">\n");
        Append("/Parent ");
        Append(parent);
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
        Append("/F 4\n");       // No Zoom
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

    private int GetNumOfChildren(int numOfChildren, Bookmark bm1) {
        List<Bookmark> children = bm1.GetChildren();
        if (children != null) {
            foreach (Bookmark bm2 in children) {
                numOfChildren = GetNumOfChildren(++numOfChildren, bm2);
            }
        }
        return numOfChildren;
    }

    /// <summary>Adds objects read from an existing PDF to this document.</summary>
    public void AddObjects(List<PDFobj> objects) {
        this.pagesObjNumber = Int32.Parse(GetPagesObject(objects).dict[0]);
        AddObjectsToPDF(objects);
    }

    /// <summary>Returns the root pages object.</summary>
    public PDFobj GetPagesObject(List<PDFobj> objects) {
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Type").Equals("/Pages") &&
                    obj.GetValue("/Parent").Equals("")) {
                return obj;
            }
        }
        return null;
    }

    /// <summary>Returns the page objects.</summary>
    public List<PDFobj> GetPageObjects(List<PDFobj> objects) {
        List<PDFobj> pages = new List<PDFobj>();
        GetPageObjects(GetPagesObject(objects), objects, pages);
        return pages;
    }

    private void GetPageObjects(
            PDFobj pdfObj,
            List<PDFobj> objects,
            List<PDFobj> pages) {
        List<Int32> kids = pdfObj.GetObjectNumbers("/Kids");
        foreach (Int32 number in kids) {
            PDFobj obj =  objects[number - 1];
            if (IsPageObject(obj)) {
                pages.Add(obj);
            } else {
                GetPageObjects(obj, objects, pages);
            }
        }
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

    private String GetExtGState(PDFobj resources) {
        StringBuilder buf = new StringBuilder();
        List<String> dict = resources.GetDict();
        int level = 0;
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/ExtGState")) {
                buf.Append("/ExtGState << ");
                ++i;
                ++level;
                while (level > 0) {
                    String token = dict[++i];
                    if (token.Equals("<<")) {
                        ++level;
                    } else if (token.Equals(">>")) {
                        --level;
                    }
                    buf.Append(token);
                    if (level > 0) {
                        buf.Append(' ');
                    } else {
                        buf.Append('\n');
                    }
                }
                break;
            }
        }
        return buf.ToString();
    }

    private List<PDFobj> GetFontObjects(
            PDFobj resources, List<PDFobj> objects) {
        List<PDFobj> fonts = new List<PDFobj>();

        List<String> dict = resources.GetDict();
        int i = 0;
        while (i < dict.Count && !dict[i].Equals("/Font")) {
            i += 1;
        }
        i += 2;     // Skip over "/Font" and the "<<" that follows it.

        // The sub-dictionary holds one "/Name <number> 0 R" entry per font.
        // Every one of them is re-emitted in the resources object, so every
        // one of them has to be collected here - taking only the first left
        // the rest of the references dangling.
        while (i < dict.Count && !dict[i].Equals(">>")) {
            String token = dict[i];
            if (token.StartsWith("/") && (i + 3) < dict.Count
                    && dict[i + 3].Equals("R")) {
                // Pages can carry separate resource dictionaries that name the
                // same fonts. They are merged into one /Font dictionary here,
                // so a name that is already present must not be added twice.
                if (importedFonts.Contains(token)) {
                    i += 4;
                    continue;
                }
                importedFonts.Add(token);
                importedFonts.Add(dict[i + 1]);
                importedFonts.Add(dict[i + 2]);
                importedFonts.Add(dict[i + 3]);
                int number = Int32.Parse(dict[i + 1]);
                if (number > 0 && number <= objects.Count) {
                    fonts.Add(objects[number - 1]);
                }
                i += 4;
                continue;
            }
            importedFonts.Add(token);
            i += 1;
        }

        if (fonts.Count == 0) {
            return null;
        }
        return fonts;
    }

    private List<PDFobj> GetDescendantFonts(PDFobj font, List<PDFobj> objects) {
        List<PDFobj> descendantFonts = new List<PDFobj>();
        List<String> dict = font.GetDict();
        for (int i = 0; i < dict.Count - 2; i++) {
            if (dict[i].Equals("/DescendantFonts")) {
                String token = dict[i + 2];
                if (!token.Equals("]")) {
                    descendantFonts.Add(objects[Int32.Parse(token) - 1]);
                }
            }
        }
        return descendantFonts;
    }

    private PDFobj GetObject(String name, PDFobj obj, List<PDFobj> objects) {
        List<String> dict = obj.GetDict();
        for (int i = 0; i < dict.Count - 1; i++) {
            if (dict[i].Equals(name)) {
                String token = dict[i + 1];
                return objects[Int32.Parse(token) - 1];
            }
        }
        return null;
    }

    /// <summary>
    /// Collects the font descriptor of the given font, together with whichever
    /// embedded font program it carries.
    /// </summary>
    private void AddFontDescriptor(
            PDFobj font, List<PDFobj> objects, List<PDFobj> resources) {
        PDFobj descriptor = GetObject("/FontDescriptor", font, objects);
        if (descriptor == null) {
            return;
        }
        resources.Add(descriptor);
        foreach (String key in new String[] {"/FontFile", "/FontFile2", "/FontFile3"}) {
            PDFobj fontFile = GetObject(key, descriptor, objects);
            if (fontFile != null) {
                resources.Add(fontFile);
            }
        }
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
            dict = objects[Int32.Parse(dict[i]) - 1].GetDict();
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
    /// tokens refer to.
    /// </summary>
    private List<Int32> GetReferences(List<String> tokens) {
        List<Int32> numbers = new List<Int32>();
        for (int i = 0; i + 2 < tokens.Count; i++) {
            if (tokens[i + 2].Equals("R")
                    && IsInteger(tokens[i]) && IsInteger(tokens[i + 1])) {
                numbers.Add(Int32.Parse(tokens[i]));
                i += 2;
            }
        }
        return numbers;
    }

    /// <summary>
    /// Collects the object with the given number and every object it refers to,
    /// directly or through other objects, like the color space of an image or
    /// the resources of a form XObject. The page tree is not followed.
    /// </summary>
    private void AddObjectTree(
            int number, List<PDFobj> objects, HashSet<Int32> numbers, List<PDFobj> resources) {
        if (number <= 0 || number > objects.Count || !numbers.Add(number)) {
            return;
        }
        PDFobj obj = objects[number - 1];
        String type = obj.GetValue("/Type");
        if (obj.dict.Count == 0
                || type.Equals("/Page") || type.Equals("/Pages") || type.Equals("/Catalog")) {
            return;
        }
        resources.Add(obj);
        foreach (int reference in GetReferences(obj.dict)) {
            AddObjectTree(reference, objects, numbers, resources);
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
            if (token.StartsWith("/") && (i + 3) < entries.Count
                    && entries[i + 3].Equals("R")) {
                // Like the fonts, a name that an earlier page added is kept.
                if (!importedXObjects.Contains(token)) {
                    importedXObjects.AddRange(entries.GetRange(i, 4));
                    AddObjectTree(Int32.Parse(entries[i + 1]), objects, numbers, resources);
                }
                i += 4;
            } else {
                i += 1;
            }
        }
    }

    /// <summary>Adds the fonts, images and graphics states used by the pages to this document.</summary>
    public void AddResourceObjects(List<PDFobj> objects) {
        List<PDFobj> resources = new List<PDFobj>();
        HashSet<Int32> numbers = new HashSet<Int32>();
        List<PDFobj> pages = GetPageObjects(objects);
        foreach (PDFobj page in pages) {
            PDFobj resObj = page.GetResourcesObject(objects);
            List<PDFobj> fonts = GetFontObjects(resObj, objects);
            if (fonts != null) {
                foreach (PDFobj font in fonts) {
                    resources.Add(font);
                    PDFobj obj = GetObject("/ToUnicode", font, objects);
                    if (obj != null) {
                        resources.Add(obj);
                    }
                    // A simple font carries its descriptor directly; only a
                    // composite one puts it on the descendant.
                    AddFontDescriptor(font, objects, resources);
                    List<PDFobj> descendantFonts = GetDescendantFonts(font, objects);
                    foreach (PDFobj descendantFont in descendantFonts) {
                        resources.Add(descendantFont);
                        AddFontDescriptor(descendantFont, objects, resources);
                    }
                }
            }
            AddXObjects(resObj, objects, numbers, resources);
            extGState = GetExtGState(resObj);
            // The /ExtGState dictionary is copied as it is, so the objects
            // that its entries refer to have to be copied too.
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
