/**
 *  PDF.cs
 *
©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

[assembly:AssemblyVersionAttribute("8.0.0.0")]

/**
 *  Used to create PDF objects that represent PDF documents.
 *
 */
namespace PDFjet.NET {
public class PDF {
    private bool eval = false;

    internal int metadataObjNumber = 0;
    internal int outputIntentObjNumber = 0;
    internal List<Font> fonts = new List<Font>();
    internal List<Image> images = new List<Image>();
    internal List<Page> pages = new List<Page>();
    internal Dictionary<String, Destination> destinations = new Dictionary<String, Destination>();
    internal List<OptionalContentGroup> groups = new List<OptionalContentGroup>();
    internal Dictionary<String, Int32> states = new Dictionary<String, Int32>();
    internal static readonly CultureInfo culture_en_us = new CultureInfo("en-US");
    internal int compliance = 0;

    private Stream os = null;
    private readonly List<Int32> objOffset = new List<Int32>();
    private String title = "";
    private String author = "";
    private String subject = "";
    private String keywords = "";
    private String producer = "PDFjet v8.0.4";
    private String creator;
    private String createDate;      // XMP metadata
    private String creationDate;    // PDF Info Object
    private int byteCount = 0;
    private int pagesObjNumber = 0;
    private String pageLayout = null;
    private String pageMode = null;
    private String language = "en-US";
    private String uuid = null;

    internal Bookmark toc = null;
    internal List<String> importedFonts = new List<String>();
    internal String extGState = "";
    internal Page prevPage = null;
    internal String floatFormat = "0.###";

    /**
     * The default constructor - use when reading PDF files.
     */
    public PDF() {
        this.uuid = (new Salsa20()).GetID();
    }

    public PDF(Stream os) : this(os, 0) {}

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
    public PDF(Stream os, int compliance) {
        this.os = os;
        this.compliance = compliance;
        this.uuid = (new Salsa20()).GetID();
        this.creator = this.producer;

        DateTime date = new DateTime(DateTime.Now.Ticks);

        SimpleDateFormat sdf1 = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
        createDate = sdf1.Format(date);     // XMP metadata

        SimpleDateFormat sdf2 = new SimpleDateFormat("yyyyMMddHHmmss");
        creationDate = sdf2.Format(date);   // PDF Info Object

        Append("%PDF-1.5\n");
        Append('%');
        Append((byte) 0x00F2);
        Append((byte) 0x00F3);
        Append((byte) 0x00F4);
        Append((byte) 0x00F5);
        Append((byte) 0x00F6);
        Append(Token.newline);
    }

    public void SetCompliance(int compliance) {
        this.compliance = compliance;
    }

    internal void Newobj() {
        objOffset.Add(byteCount);
        Append(objOffset.Count);
        Append(Token.newobj);
    }

    internal void Endobj() {
        Append(Token.endobj);
    }

    internal int GetObjNumber() {
        return objOffset.Count;
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
            sb.Append(Encoding.UTF8.GetBytes(notice));
            sb.Append("</rdf:li>\n");
            sb.Append("</rdf:Alt>\n");
            sb.Append("</xmpRights:UsageTerms>\n");
            sb.Append("</rdf:Description>\n");
        } else {
            sb.Append("<rdf:Description rdf:about=\"\"\n");
            sb.Append("    xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n");
            sb.Append("    xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n");
            sb.Append("    xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
            sb.Append("    xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
            sb.Append("    xmlns:xapMM=\"http://ns.adobe.com/xap/1.0/mm/\"\n");
            sb.Append("    xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\">\n");

            sb.Append("  <dc:format>application/pdf</dc:format>\n");
            if (compliance == Compliance.PDF_UA) {
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

            sb.Append("  <pdf:Keywords>");
            sb.Append(keywords);
            sb.Append("</pdf:Keywords>\n");

            sb.Append("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">");
            sb.Append(title);
            sb.Append("</rdf:li></rdf:Alt></dc:title>\n");

            sb.Append("  <dc:creator><rdf:Seq><rdf:li>");
            sb.Append(author);
            sb.Append("</rdf:li></rdf:Seq></dc:creator>\n");

            sb.Append("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">");
            sb.Append(subject);
            sb.Append("</rdf:li></rdf:Alt></dc:description>\n");

            sb.Append("  <xmp:CreatorTool>");
            sb.Append(creator);
            sb.Append("</xmp:CreatorTool>\n");

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

        byte[] xml = (new System.Text.UTF8Encoding()).GetBytes(sb.ToString());

        // This is the metadata object
        Newobj();
        Append(Token.beginDictionary);
        Append("/Type /Metadata\n");
        Append("/Subtype /XML\n");
        Append("/Length ");
        Append(xml.Length);
        Append(Token.newline);
        Append(Token.endDictionary);
        Append(Token.stream);
        Append(xml, 0, xml.Length);
        Append(Token.endstream);
        Endobj();

        return GetObjNumber();
    }

    private int AddOutputIntentObject() {
        Newobj();
        Append(Token.beginDictionary);
        Append("/N 3\n");

        Append("/Length ");
        Append(ICCBlackScaled.profile.Length);
        Append("\n");

        Append("/Filter /FlateDecode\n");
        Append(Token.endDictionary);
        Append(Token.stream);
        Append(ICCBlackScaled.profile, 0, ICCBlackScaled.profile.Length);
        Append(Token.endstream);
        Endobj();

        // OutputIntent object
        Newobj();
        Append(Token.beginDictionary);
        Append("/Type /OutputIntent\n");
        Append("/S /GTS_PDFA1\n");
        Append("/OutputCondition (sRGB IEC61966-2.1)\n");
        Append("/OutputConditionIdentifier (sRGB IEC61966-2.1)\n");
        Append("/Info (sRGB IEC61966-2.1)\n");
        Append("/DestOutputProfile ");
        Append(GetObjNumber() - 1);
        Append(Token.objRef);
        Append(Token.endDictionary);
        Endobj();

        return GetObjNumber();
    }

    private int AddResourcesObject() {
        Newobj();
        Append(Token.beginDictionary);
        if (!extGState.Equals("")) {
            Append(extGState);
        }
        if (fonts.Count > 0 || importedFonts.Count > 0) {
            Append("/Font\n");
            Append(Token.beginDictionary);
            foreach (String token in importedFonts) {
                Append(token);
                if (token.Equals("R")) {
                    Append(Token.newline);
                } else {
                    Append(Token.space);
                }
            }
            foreach (Font font in fonts) {
                Append("/F");
                Append(font.objNumber);
                Append(Token.space);
                Append(font.objNumber);
                Append(Token.objRef);
            }
            Append(Token.endDictionary);
        }
        if (images.Count > 0) {
            Append("/XObject\n");
            Append(Token.beginDictionary);
            foreach (Image image in images) {
                Append("/Im");
                Append(image.objNumber);
                Append(Token.space);
                Append(image.objNumber);
                Append(Token.objRef);
            }
            Append(Token.endDictionary);
        }
        if (groups.Count > 0) {
            Append("/Properties\n");
            Append(Token.beginDictionary);
            for (int i = 0; i < groups.Count; i++) {
                OptionalContentGroup ocg = groups[i];
                Append("/OC");
                Append(i + 1);
                Append(Token.space);
                Append(ocg.objNumber);
                Append(Token.objRef);
            }
            Append(Token.endDictionary);
        }
        // String state = "/CA 0.5 /ca 0.5";
        if (states.Count > 0) {
            Append("/ExtGState <<\n");
            foreach (String state in states.Keys) {
                Append("/GS");
                Append(states[state]);
                Append(" <<");
                Append(state);
                Append(Token.endDictionary);
            }
            Append(Token.endDictionary);
        }
        Append(Token.endDictionary);
        Endobj();
        return GetObjNumber();
    }

    private int AddPagesObject() {
        Newobj();
        Append(Token.beginDictionary);
        Append("/Type /Pages\n");
        Append("/Kids [\n");
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            if (compliance == Compliance.PDF_UA ||
                    compliance == Compliance.PDF_A_1A ||
                    compliance == Compliance.PDF_A_1B ||
                    compliance == Compliance.PDF_A_2A ||
                    compliance == Compliance.PDF_A_2B ||
                    compliance == Compliance.PDF_A_3A ||
                    compliance == Compliance.PDF_A_3B) {
                page.SetStructElementsPageObjNumber(page.objNumber);
            }
            Append(page.objNumber);
            Append(" 0 R\n");
        }
        Append("]\n");
        Append("/Count ");
        Append(pages.Count);
        Append(Token.newline);
        Append(Token.endDictionary);
        Endobj();
        return GetObjNumber();
    }

    private int AddInfoObject() {
        // Add the info object
        Newobj();
        Append(Token.beginDictionary);
        Append("/Title (");
        Append(title);
        Append(")\n");
        Append("/Author (");
        Append(author);
        Append(")\n");
        Append("/Subject (");
        Append(subject);
        Append(")\n");
        Append("/Producer (");
        Append(producer);
        Append(")\n");
        Append("/Creator (");
        Append(creator);
        Append(")\n");
        Append("/CreationDate (D:");
        Append(creationDate.Substring(0, creationDate.Length - 1)); // Remove the 'Z'
        Append("-05'00')\n");
        Append(Token.endDictionary);
        Endobj();
        return GetObjNumber();
    }

    private int AddStructTreeRootObject() {
        Newobj();
        Append(Token.beginDictionary);
        Append("/Type /StructTreeRoot\n");
        Append("/ParentTree ");
        Append(GetObjNumber() + 1);
        Append(Token.objRef);
        Append("/K [\n");
        Append(GetObjNumber() + 2);
        Append(Token.objRef);
        Append("]\n");
        Append(Token.endDictionary);
        Endobj();
        return GetObjNumber();
    }

    private int AddStructDocumentObject(int parent) {
        Newobj();
        Append(Token.beginDictionary);
        Append("/Type /StructElem\n");
        Append("/S /Document\n");
        Append("/P ");
        Append(parent);
        Append(Token.objRef);
        Append("/K [\n");
        foreach (Page page in pages) {
            foreach (StructElem structElement in page.structures) {
                Append(structElement.objNumber);
                Append(" 0 R\n");
            }
        }
        Append("]\n");
        Append(Token.endDictionary);
        Endobj();
        return GetObjNumber();
    }

    private void AddStructElementObjects() {
        int structTreeRootObjNumber = GetObjNumber() + 1;
        foreach (Page page in pages) {
            structTreeRootObjNumber += page.structures.Count;
        }
        foreach (Page page in pages) {
            foreach (StructElem element in page.structures) {
                Newobj();
                element.objNumber = GetObjNumber();
                Append("<<\n/Type /StructElem /S /");
                Append(element.structure);
                Append("\n/P ");
                Append(structTreeRootObjNumber + 2);    // Use the document struct as parent!
                Append(" 0 R\n/Pg ");
                Append(element.pageObjNumber);
                Append(Token.objRef);
                if (element.annotation != null) {
                    Append("/K <</Type /OBJR /Obj ");
                    Append(element.annotation.objNumber);
                    Append(" 0 R>>");
                } else {
                    Append("/K ");
                    Append(element.mcid);
                }
                Append("\n/Lang (");
                if (element.language != null) {
                    Append(element.language);
                } else {
                    Append(language);
                }
                Append(")\n/ActualText <");
                Append(ToHex(element.actualText));
                Append(">\n/Alt <");
                Append(ToHex(element.altDescription));
                Append(">\n>>\n");
                Endobj();
            }
        }
    }

    private String ToHex(String str) {
        StringBuilder buf = new StringBuilder();
        if (str != null) {
            buf.Append("FEFF");
            for (int i = 0; i < str.Length; i++) {
                buf.Append(((int) str[i]).ToString("X4"));
            }
        }
        return buf.ToString();
    }

    private void AddNumsParentTree() {
        Newobj();
        Append(Token.beginDictionary);
        Append("/Nums [\n");
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            Append(i);
            Append(" [\n");
            foreach (StructElem element in page.structures) {
                if (element.annotation == null) {
                    Append(element.objNumber);
                    Append(Token.objRef);
                }
            }
            Append("]\n");
        }
        int index = pages.Count;
        foreach (Page page in pages) {
            foreach (StructElem element in page.structures) {
                if (element.annotation != null) {
                    Append(index);
                    Append(Token.space);
                    Append(element.objNumber);
                    Append(Token.objRef);
                    index++;
                }
            }
        }
        Append("]\n");
        Append(Token.endDictionary);
        Endobj();
    }

    private int AddRootObject(int structTreeRootObjNumber, int outlineDictNum) {
        // Add the root object
        Newobj();
        Append(Token.beginDictionary);
        Append("/Type /Catalog\n");

        if (compliance == Compliance.PDF_UA ||
                compliance == Compliance.PDF_A_1A ||
                compliance == Compliance.PDF_A_1B ||
                compliance == Compliance.PDF_A_2A ||
                compliance == Compliance.PDF_A_2B ||
                compliance == Compliance.PDF_A_3A ||
                compliance == Compliance.PDF_A_3B) {
            Append("/Lang (");
            Append(language);
            Append(")\n");

            Append("/StructTreeRoot ");
            Append(structTreeRootObjNumber);
            Append(" 0 R\n");

            Append("/MarkInfo <</Marked true>>\n");
            Append("/ViewerPreferences <</DisplayDocTitle true>>\n");
        }

        if (pageLayout != null) {
            Append("/PageLayout /");
            Append(pageLayout);
            Append(Token.newline);
        }

        if (pageMode != null) {
            Append("/PageMode /");
            Append(pageMode);
            Append(Token.newline);
        }

        AddOCProperties();

        Append("/Pages ");
        Append(pagesObjNumber);
        Append(" 0 R\n");

        if (compliance == Compliance.PDF_UA ||
                compliance == Compliance.PDF_A_1A ||
                compliance == Compliance.PDF_A_1B ||
                compliance == Compliance.PDF_A_2A ||
                compliance == Compliance.PDF_A_2B ||
                compliance == Compliance.PDF_A_3A ||
                compliance == Compliance.PDF_A_3B) {
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

        Append(Token.endDictionary);
        Endobj();
        return GetObjNumber();
    }

    private void AddPageBox(String boxName, Page page, float[] rect) {
        Append("/");
        Append(boxName);
        Append(" [");
        Append(rect[0]);
        Append(Token.space);
        Append(page.height - rect[3]);
        Append(Token.space);
        Append(rect[2]);
        Append(Token.space);
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
            Newobj();
            page.objNumber = GetObjNumber();
            Append(Token.beginDictionary);
            Append("/Type /Page\n");
            Append("/Parent ");
            Append(pagesObjNumber);
            Append(" 0 R\n");
            Append("/MediaBox [0 0 ");
            Append(page.width);
            Append(' ');
            Append(page.height);
            Append("]\n");

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

            if (compliance == Compliance.PDF_UA ||
                    compliance == Compliance.PDF_A_1A ||
                    compliance == Compliance.PDF_A_1B ||
                    compliance == Compliance.PDF_A_2A ||
                    compliance == Compliance.PDF_A_2B ||
                    compliance == Compliance.PDF_A_3A ||
                    compliance == Compliance.PDF_A_3B) {
                Append("/Tabs /S\n");
                Append("/StructParents ");
                Append(i);
                Append(Token.newline);
            }

            Append(Token.endDictionary);
            Endobj();
        }
    }

    private void AddPageContent(Page page) {
        if (eval && fonts.Count > 0) {
            Font f1 = fonts[0];
            float fontSize = f1.GetSize();
            f1.SetSize(8.0f);
            float[] tm = page.tm;
            float[] brushColor = page.GetBrushColor();

            page.SetTextDirection(0);
            page.SetBrushColor(Color.blue);
            String message1 =
                    "This document was created with the evaluation version of PDFjet";
            String message2 =
                    "To acquire a license please visit http://pdfjet.com";
            page.DrawString(
                    f1,
                    message1,
                    (page.width - f1.StringWidth(message1))/2,
                    10.0f);
            page.DrawString(
                    f1,
                    message2,
                    (page.width - f1.StringWidth(message2))/2,
                    20.0f);

            // Revert back to the original values:
            f1.SetSize(fontSize);
            page.tm = tm;
            page.SetBrushColor(brushColor);
        }

        MemoryStream baos = new MemoryStream();
        DeflaterOutputStream dos = new DeflaterOutputStream(baos);
        byte[] buf = page.buf.ToArray();
        dos.Write(buf, 0, buf.Length);
        page.buf = null;    // Release the page content memory!

        Newobj();
        Append(Token.beginDictionary);
        Append("/Filter /FlateDecode\n");
        Append("/Length ");
        Append(baos.Length);
        Append(Token.newline);
        Append(Token.endDictionary);
        Append(Token.stream);
        Append(baos);
        Append(Token.endstream);
        Endobj();
        page.contents.Add(GetObjNumber());
    }
/*
    // Use this method on systems that don't have Deflater stream or when troubleshooting.
    private void AddPageContent(Page page) {
        Newobj();
        Append(Token.beginDictionary);
        Append("/Length ");
        Append(page.buf.Length);
        Append(Token.newline);
        Append(Token.endDictionary);
        Append(Token.stream);
        Append(page.buf);
        Append(Token.endstream);
        Endobj();
        page.buf = null;    // Release the page content memory!
        page.contents.Add(GetObjNumber());
    }
*/
    private int AddAnnotationObject(Annotation annot, int index) {
        Newobj();
        annot.objNumber = GetObjNumber();
        Append(Token.beginDictionary);
        Append("/Type /Annot\n");
        if (annot.fileAttachment != null) {
            Append("/Subtype /FileAttachment\n");
            Append("/T (");
            Append(annot.fileAttachment.title);
            Append(")\n");
            Append("/Contents (");
            Append(annot.fileAttachment.contents);
            Append(")\n");
            Append("/FS ");
            Append(annot.fileAttachment.embeddedFile.objNumber);
            Append(" 0 R\n");
            Append("/Name /");
            Append(annot.fileAttachment.icon);
            Append("\n");
        } else {
            Append("/Subtype /Link\n");
        }
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
        if (annot.uri != null) {
            Append("/F 4\n");
            Append("/A <<\n");
            Append("/S /URI\n");
            Append("/URI (");
            Append(annot.uri);
            Append(")\n");
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
        if (index != -1) {
            Append("/StructParent ");
            Append(index++);
            Append("\n");
        }
        Append(Token.endDictionary);
        Endobj();

        return index;
    }

    private void AddAnnotDictionaries() {
        int index = pages.Count;
        foreach (Page page in pages) {
            if (page.structures.Count > 0) {
                foreach (StructElem element in page.structures) {
                    if (element.annotation != null) {
                        AddAnnotationObject(element.annotation, index);
                    }
                }
            } else if (page.annots.Count > 0) {
                foreach (Annotation annotation in page.annots) {
                    if (annotation != null) {
                        AddAnnotationObject(annotation, -1);
                    }
                }
            }
        }
    }

    private void AddOCProperties() {
        if (groups.Count > 0) {
            StringBuilder buf = new StringBuilder();
            foreach (OptionalContentGroup ocg in this.groups) {
                buf.Append(' ');
                buf.Append(ocg.objNumber);
                buf.Append(" 0 R");
            }

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

            Append("/Order [[ ()");
            Append(buf.ToString());
            Append(" ]]\n");

            Append(">>\n");
            Append(">>\n");
        }
    }

    public void AddPage(Page page) {
        pages.Add(page);
        if (prevPage != null) {
            AddPageContent(prevPage);
        }
        prevPage = page;
    }

    /**
     * Completes the construction of the PDF and writes it to the output stream.
     * The output stream is then automatically closed.
     */
    public void Complete() {
        if (prevPage != null) {
            AddPageContent(prevPage);
        }
        if (compliance == Compliance.PDF_UA ||
                compliance == Compliance.PDF_A_1A ||
                compliance == Compliance.PDF_A_1B ||
                compliance == Compliance.PDF_A_2A ||
                compliance == Compliance.PDF_A_2B ||
                compliance == Compliance.PDF_A_3A ||
                compliance == Compliance.PDF_A_3B) {
            metadataObjNumber = AddMetadataObject("", false);
            outputIntentObjNumber = AddOutputIntentObject();
        }

        if (pagesObjNumber == 0) {
            AddAllPages(AddResourcesObject());
            pagesObjNumber = AddPagesObject();
        }

        int structTreeRootObjNumber = 0;
        if (compliance == Compliance.PDF_UA ||
                compliance == Compliance.PDF_A_1A ||
                compliance == Compliance.PDF_A_1B ||
                compliance == Compliance.PDF_A_2A ||
                compliance == Compliance.PDF_A_2B ||
                compliance == Compliance.PDF_A_3A ||
                compliance == Compliance.PDF_A_3B) {
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

        int infoObjNumber = AddInfoObject();
        int rootObjNumber = AddRootObject(structTreeRootObjNumber, outlineDictNum);

        int startxref = byteCount;

        // Create the xref table
        Append("xref\n");
        Append("0 ");
        Append(rootObjNumber + 1);
        Append('\n');

        Append("0000000000 65535 f \n");
        for (int i = 0; i < objOffset.Count; i++) {
            int offset = objOffset[i];
            String str = offset.ToString();
            for (int j = 0; j < 10 - str.Length; j++) {
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

        Append("/ID[<");
        Append(uuid);
        Append("><");
        Append(uuid);
        Append(">]\n");

        Append("/Info ");
        Append(infoObjNumber);
        Append(" 0 R\n");

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

    /**
     *  Set the "Title" document property of the PDF file.
     *  @param title The title of this document.
     */
    public void SetTitle(String title) {
        this.title = title;
    }

    /**
     *  Set the "Author" document property of the PDF file.
     *  @param author The author of this document.
     */
    public void SetAuthor(String author) {
        this.author = author;
    }

    /**
     *  Set the "Subject" document property of the PDF file.
     *  @param subject The subject of this document.
     */
    public void SetSubject(String subject) {
        this.subject = subject;
    }

    public void SetKeywords(String keywords) {
        this.keywords = keywords;
    }

    public void SetCreator(String creator) {
        this.creator = creator;
    }

    public void SetPageLayout(String pageLayout) {
        this.pageLayout = pageLayout;
    }

    public void SetPageMode(String pageMode) {
        this.pageMode = pageMode;
    }

    internal void Append(int num) {
        Append(num.ToString());
    }

    internal void Append(float val) {
        Append(val.ToString(floatFormat, PDF.culture_en_us));
    }

    internal void Append(String str) {
        int len = str.Length;
        for (int i = 0; i < len; i++) {
            os.WriteByte((byte) str[i]);
        }
        byteCount += len;
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

    public List<PDFobj> Read(Stream inputStream) {
        byte[] buf = Contents.GetFromStream(inputStream);

        List<PDFobj> objects1 = new List<PDFobj>();
        int xref = GetStartXRef(buf);
        PDFobj obj1 = GetObject(buf, xref);
        if (obj1.dict[0].Equals("xref")) {
            GetObjects1(buf, obj1, objects1);
        } else {
            GetObjects2(buf, obj1, objects1);
        }

        List<PDFobj> objects2 = new List<PDFobj>();
        foreach (PDFobj obj in objects1) {
            if (obj.dict.Contains("stream")) {
                obj.SetStreamAndData(buf, obj.GetLength(objects1));
            }

            if (obj.GetValue("/Type").Equals("/ObjStm")) {
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
            } else if (obj.GetValue("/Type").Equals("/XRef")) {
                // Skip the stream XRef object.
            } else {
                objects2.Add(obj);
            }
        }

        return GetSortedObjects(objects2);
    }

    private bool Process(
            PDFobj obj, StringBuilder sb1, byte[] buf, int off) {
        String str = sb1.ToString().Trim();
        if (!str.Equals("")) {
            obj.dict.Add(str);
        }
        sb1.Length = 0;
        if (str.Equals("endobj")) {
            return true;
        } else if (str.Equals("stream")) {
            obj.streamOffset = off;
            if (buf[off] == '\n') {
                obj.streamOffset += 1;
            }
            return true;
        } else if (str.Equals("startxref")) {
            return true;
        }
        return false;
    }

    private PDFobj GetObject(byte[] buf, int off) {
        return GetObject(buf, off, buf.Length);
    }

    private PDFobj GetObject(byte[] buf, int off, int len) {
        PDFobj obj = new PDFobj();
        obj.offset = off;
        StringBuilder token = new StringBuilder();

        int p = 0;
        char c1 = ' ';
        bool done = false;
        while (!done && off < len) {
            char c2 = (char) buf[off++];
            if (c1 == '\\') {
                token.Append(c2);
                c1 = c2;
                continue;
            }

            if (c2 == '(') {
                if (p == 0) {
                    done = Process(obj, token, buf, off);
                }
                if (!done) {
                    token.Append(c2);
                    c1 = c2;
                    ++p;
                }
            } else if (c2 == ')') {
                token.Append(c2);
                c1 = c2;
                --p;
                if (p == 0) {
                    done = Process(obj, token, buf, off);
                }
            } else if (c2 == 0x00       // Null
                    || c2 == 0x09       // Horizontal Tab
                    || c2 == 0x0A       // Line Feed (LF)
                    || c2 == 0x0C       // Form Feed
                    || c2 == 0x0D       // Carriage Return (CR)
                    || c2 == 0x20) {    // Space
                done = Process(obj, token, buf, off);
                if (!done) {
                    c1 = ' ';
                }
            } else if (c2 == '/') {
                done = Process(obj, token, buf, off);
                if (!done) {
                    token.Append(c2);
                    c1 = c2;
                }
            } else if (c2 == '<' || c2 == '>' || c2 == '%') {
                if (p > 0) {
                    token.Append(c2);
                    c1 = c2;
                } else {
                    if (c2 != c1) {
                        done = Process(obj, token, buf, off);
                        if (!done) {
                            token.Append(c2);
                            c1 = c2;
                        }
                    } else {
                        token.Append(c2);
                        done = Process(obj, token, buf, off);
                        if (!done) {
                            c1 = ' ';
                        }
                    }
                }
            } else if (c2 == '[' || c2 == ']' || c2 == '{' || c2 == '}') {
                if (p > 0) {
                    token.Append(c2);
                    c1 = c2;
                } else {
                    done = Process(obj, token, buf, off);
                    if (!done) {
                        obj.dict.Add(c2.ToString());
                        c1 = c2;
                    }
                }
            } else {
                token.Append(c2);
                c1 = c2;
            }
        }

        return obj;
    }

    /**
     * Converts an array of bytes to an integer.
     * @param buf byte[]
     * @return int
     */
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

    private void GetObjects1(
            byte[] buf,
            PDFobj obj,
            List<PDFobj> objects) {
        String xref = obj.GetValue("/Prev");
        if (!xref.Equals("")) {
            GetObjects1(
                    buf,
                    GetObject(buf, Int32.Parse(xref)),
                    objects);
        }

        int i = 1;
        while (true) {
            String token = obj.dict[i++];
            if (token.Equals("trailer")) {
                break;
            }

            int n = Int32.Parse(obj.dict[i++]);     // Number of entries
            for (int j = 0; j < n; j++) {
                String offset = obj.dict[i++];      // Object offset
                String number = obj.dict[i++];      // Generation number
                String status = obj.dict[i++];      // Status keyword
                if (!status.Equals("f")) {
                    PDFobj o2 = GetObject(buf, Int32.Parse(offset));
                    o2.number = Int32.Parse(o2.dict[0]);
                    objects.Add(o2);
                }
            }
        }
    }

    private void GetObjects2(
            byte[] buf,
            PDFobj obj,
            List<PDFobj> objects) {
        String prev = obj.GetValue("/Prev");
        if (!prev.Equals("")) {
            GetObjects2(
                    buf,
                    GetObject(buf, Int32.Parse(prev)),
                    objects);
        }

        int predictor = 0;  // The predictor
        int n1 = 0;         // Field 1 number of bytes
        int n2 = 0;         // Field 2 number of bytes
        int n3 = 0;         // Field 3 number of bytes
        int length = 0;
        for (int i = 0; i < obj.dict.Count; i++) {
            String token = obj.dict[i];
            if (token.Equals("/Predictor")) {
                predictor = Int32.Parse(obj.dict[i + 1]);
            } else if (token.Equals("/Length")) {
                length = Int32.Parse(obj.dict[i + 1]);
            } else if (token.Equals("/W")) {
                // "/W [ 1 3 1 ]"
                n1 = Int32.Parse(obj.dict[i + 2]);
                n2 = Int32.Parse(obj.dict[i + 3]);
                n3 = Int32.Parse(obj.dict[i + 4]);
            }
        }

        obj.SetStreamAndData(buf, length);
        int n = n1 + n2 + n3;   // Number of bytes per entry
        if (predictor > 0) {
            n += 1;
        }

        byte[] entry = new byte[n];
        for (int i = 0; i < obj.data.Length; i += n) {
            if (predictor == 12) {
                // Apply the 'Up' filter.
                for (int j = 1; j < n; j++) {
                    entry[j] += obj.data[i + j];
                }
            } else {
                for (int j = 0; j < n; j++) {
                    entry[j] = obj.data[i + j];
                }
            }
            // Process the entries in a cross-reference stream
            // Page 51 in PDF32000_2008.pdf
            if (predictor > 0) {
                if (entry[1] == 1) {    // Type 1 entry
                    PDFobj o2 = GetObject(buf, ToInt(entry, 1 + n1, n2));
                    o2.number = Int32.Parse(o2.dict[0]);
                    objects.Add(o2);
                }
            } else {
                if (entry[0] == 1) {    // Type 1 entry
                    PDFobj o2 = GetObject(buf, ToInt(entry, n1, n2));
                    o2.number = Int32.Parse(o2.dict[0]);
                    objects.Add(o2);
                }
            }
        }
    }

    private int GetStartXRef(byte[] buf) {
        StringBuilder sb = new StringBuilder();
        for (int i = (buf.Length - 10); i > 10; i--) {
            if (buf[i] == 's' &&
                    buf[i + 1] == 't' &&
                    buf[i + 2] == 'a' &&
                    buf[i + 3] == 'r' &&
                    buf[i + 4] == 't' &&
                    buf[i + 5] == 'x' &&
                    buf[i + 6] == 'r' &&
                    buf[i + 7] == 'e' &&
                    buf[i + 8] == 'f') {
                i += 10;                // Skip over "startxref" and the first EOL character
                while (buf[i] < 0x30) { // Skip over possible second EOL character and spaces
                    i += 1;
                }
                while (Char.IsDigit((char) buf[i])) {
                    sb.Append((char) buf[i]);
                    i += 1;
                }
                break;
            }
        }
        return Int32.Parse(sb.ToString());
    }

    public int AddOutlineDict(Bookmark toc) {
        int numOfChildren = GetNumOfChildren(0, toc);
        Newobj();
        Append(Token.beginDictionary);
        Append("/Type /Outlines\n");
        Append("/First ");
        Append(GetObjNumber() + 1);
        Append(" 0 R\n");
        Append("/Last ");
        Append(GetObjNumber() + numOfChildren);
        Append(" 0 R\n");
        Append("/Count ");
        Append(numOfChildren);
        Append(Token.newline);
        Append(Token.endDictionary);
        Endobj();
        return GetObjNumber();
    }

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

        Newobj();
        Append(Token.beginDictionary);
        Append("/Title <");
        Append(ToHex(bm1.GetTitle()));
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
        Append(Token.endDictionary);
        Endobj();
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

    public void AddObjects(List<PDFobj> objects) {
        this.pagesObjNumber = Int32.Parse(GetPagesObject(objects).dict[0]);
        AddObjectsToPDF(objects);
    }

    public PDFobj GetPagesObject(List<PDFobj> objects) {
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Type").Equals("/Pages") &&
                    obj.GetValue("/Parent").Equals("")) {
                return obj;
            }
        }
        return null;
    }

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
        for (int i = 0; i < obj.dict.Count; i++) {
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
        for (; i < dict.Count; i++) {
            if (dict[i].Equals("/Font")) {
                if (!dict[i + 2].Equals(">>")) {
                    String token = dict[i + 3];
                    fonts.Add(objects[Int32.Parse(token) - 1]);
                }
            }
        }

        if (fonts.Count == 0) {
            return null;
        }

        i = 4;
        while (true) {
            if (dict[i].Equals("/Font")) {
                i += 2;
                break;
            }
            i += 1;
        }
        while (!dict[i].Equals(">>")) {
            importedFonts.Add(dict[i]);
            i += 1;
        }

        return fonts;
    }

    private List<PDFobj> GetDescendantFonts(PDFobj font, List<PDFobj> objects) {
        List<PDFobj> descendantFonts = new List<PDFobj>();
        List<String> dict = font.GetDict();
        for (int i = 0; i < dict.Count; i++) {
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
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals(name)) {
                String token = dict[i + 1];
                return objects[Int32.Parse(token) - 1];
            }
        }
        return null;
    }

    public void AddResourceObjects(List<PDFobj> objects) {
        List<PDFobj> resources = new List<PDFobj>();
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
                    List<PDFobj> descendantFonts = GetDescendantFonts(font, objects);
                    foreach (PDFobj descendantFont in descendantFonts) {
                        resources.Add(descendantFont);
                        obj = GetObject("/FontDescriptor", descendantFont, objects);
                        if (obj != null) {
                            resources.Add(obj);
                            obj = GetObject("/FontFile2", obj, objects);
                            if (obj != null) {
                                resources.Add(obj);
                            }
                        }
                    }
                }
            }
            extGState = GetExtGState(resObj);
        }
        resources.Sort(delegate(PDFobj o1, PDFobj o2){
            return o1.number.CompareTo(o2.number);
        });
        AddObjectsToPDF(resources);
    }

    private void AddObjectsToPDF(List<PDFobj> objects) {
        foreach (PDFobj obj in objects) {
            if (obj.offset == 0) {
                // Create new object.
                objOffset.Add(byteCount);
                Append(obj.number);
                Append(Token.newobj);
                if (obj.dict != null) {
                    for (int i = 0; i < obj.dict.Count; i++) {
                        Append(obj.dict[i]);
                        Append(' ');
                    }
                }
                if (obj.stream != null) {
                    if (obj.dict.Count == 0) {
                        Append("<< /Length ");
                        Append(obj.stream.Length);
                        Append(" >>");
                    }
                    Append(Token.newline);
                    Append(Token.stream);
                    Append(obj.stream, 0, obj.stream.Length);
                    Append(Token.endstream);
                }
                Append(Token.endobj);
            } else {
                objOffset.Add(byteCount);
                bool link = false;
                int n = obj.dict.Count;
                String token = null;
                for (int i = 0; i < n; i++) {
                    token = obj.dict[i];
                    Append(token);
                    if (token.StartsWith("(http:")) {
                        link = true;
                    } else if (link == true && token.EndsWith(")")) {
                        link = false;
                    }
                    if (i < (n - 1)) {
                        if (!link) {
                            Append(Token.space);
                        }
                    } else {
                        Append(Token.newline);
                    }
                }
                if (obj.stream != null) {
                    Append(obj.stream, 0, obj.stream.Length);
                    Append(Token.endstream);
                }
                if (!token.Equals("endobj")) {
                    Append(Token.endobj);
                }
            }
        }
    }
}   // End of PDF.cs
}   // End of namespace PDFjet.NET
