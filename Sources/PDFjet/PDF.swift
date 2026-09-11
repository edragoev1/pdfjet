/**
 * PDF.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create PDF objects that represent PDF documents.
///
public class PDF {
    var fonts = [Font]()
    var images = [Image]()
    var pages = [Page]()
    var destinations = [String : Destination]()
    var groups = [OptionalContentGroup]()
    var states = [String : Int]()
    var stamps = [Stamp]()
    var compliance = Compliance.PDF_17
    var toc: Bookmark?
    var importedFonts = [String]()
    var importedXObjects = [String]()
    var extGState = ""
    let floatFormat = "%.2f"

    private var metadataObjNumber = 0
    private var outputIntentObjNumber = 0
    private var os: BufferedOutputStream?
    private var objOffset = [Int]()
    private var producer = "PDFjet v8.7.0"
    private var title: String?
    private var author: String?
    private var subject: String?
    private var keywords: String?
    private var creator: String?
    private var createDate: String?
    private var byteCount = 0
    private var pagesObjNumber = 0
    private var pageLayout: String?
    private var pageMode: String?
    private var language: String = "en-US"
    private var uuid: String = Salsa20().getID()
    private var prevPage: Page?
    var structElements = [StructElem]()
    private var contentStreamsCompression = true

    // The OCG type that will be stored in the list/array.
    struct OCG {
        let objNumber: Int
        let name: String
    }

    ///
    /// The default constructor - use when reading PDF files.
    ///
    public init() {
    }

    ///
    /// Creates a PDF object that represents a PDF document.
    ///
    /// - Parameter os: the associated output stream.
    ///
    public convenience init(_ os: OutputStream) {
        self.init(os, Compliance.PDF_17)
    }

    /// Here is the layout of the PDF document:
    ///
    /// Metadata Object
    /// Output Intent Object
    /// Fonts
    /// Images
    /// Resources Object
    /// Content1
    /// Content2
    /// ...
    /// ContentN
    /// Annot1
    /// Annot2
    /// ...
    /// AnnotN
    /// Page1
    /// Page2
    /// ...
    /// PageN
    /// Pages
    /// StructElem1
    /// StructElem2
    /// ...
    /// StructElemN
    /// StructTreeRoot
    /// Root
    /// xref table
    /// Trailer
    ///
    /// Creates a PDF object that represents a PDF document.
    /// Use this constructor to create PDF/A compliant PDF documents.
    /// Please note: PDF/A compliance requires all fonts to be embedded in the PDF.
    ///
    /// - Parameter os: the associated output stream.
    /// - Parameter compliance: must be: Compliance.PDF_UA_1 or Compliance.PDF_A_1A to Compliance.PDF_A_3B
    ///
    public init(_ os: OutputStream, _ compliance: Compliance) {
        os.open()
        self.os = BufferedOutputStream(os)
        self.compliance = compliance

        let date = Date()
        let dateFormatter1 = DateFormatter()
        dateFormatter1.dateFormat = "yyyy-MM-dd'T'HH:mm:ss"
        self.createDate = dateFormatter1.string(from: date)

        append("%PDF-1.7\n")
        append("%")
        append(UInt8(0xF2))
        append(UInt8(0xF3))
        append(UInt8(0xF4))
        append(UInt8(0xF5))
        append(UInt8(0xF6))
        append(Token.newline)
    }

    /// Sets the PDF/UA or PDF/A compliance of this document.
    @discardableResult
    public func setCompliance(_ compliance: Compliance) -> PDF {
        self.compliance = compliance
        return self
    }

    func newobj() {
        objOffset.append(byteCount)
        append(objOffset.count)
        append(Token.newObj)
    }

    func endobj() {
        append(Token.endObj)
    }

    func getObjNumber() -> Int {
        return objOffset.count
    }

    /// Records the offset of an object that carries its own number, growing the
    /// table with placeholders for any number that has no object yet.
    private func setObjOffset(_ number: Int, _ offset: Int) {
        if number <= 0 {    // No number of its own - just append.
            objOffset.append(offset)
            return
        }
        while objOffset.count < number {
            objOffset.append(0)
        }
        objOffset[number - 1] = offset
    }

    func addMetadataObject(_ notice: String, _ fontMetadataObject: Bool) -> Int {
        var sb = String()
        sb.append("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n")
        sb.append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"\n")
        sb.append("    x:xmptk=\"Adobe XMP Core 5.4-c005 78.147326, 2012/08/23-13:03:03\">\n")
        sb.append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n")

        if fontMetadataObject {
            sb.append("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n")
            sb.append("<xmpRights:UsageTerms>\n")
            sb.append("<rdf:Alt>\n")
            sb.append("<rdf:li xml:lang=\"x-default\">\n")
            sb.append(String(notice.utf8))
            sb.append("</rdf:li>\n")
            sb.append("</rdf:Alt>\n")
            sb.append("</xmpRights:UsageTerms>\n")
            sb.append("</rdf:Description>\n")
        } else {
            sb.append("<rdf:Description rdf:about=\"\"\n")
            sb.append("    xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n")
            sb.append("    xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n")
            sb.append("    xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n")
            sb.append("    xmlns:xapMM=\"http://ns.adobe.com/xap/1.0/mm/\"\n")
            sb.append("    xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n")
            sb.append("    xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\">\n")

            sb.append("    <dc:format>application/pdf</dc:format>\n")
            if compliance == Compliance.PDF_UA_1 {
                sb.append("  <pdfuaid:part>1</pdfuaid:part>\n")
            } else if compliance == Compliance.PDF_A_1A {
                sb.append("  <pdfaid:part>1</pdfaid:part>\n")
                sb.append("  <pdfaid:conformance>A</pdfaid:conformance>\n")
            } else if compliance == Compliance.PDF_A_1B {
                sb.append("  <pdfaid:part>1</pdfaid:part>\n")
                sb.append("  <pdfaid:conformance>B</pdfaid:conformance>\n")
            } else if compliance == Compliance.PDF_A_2A {
                sb.append("  <pdfaid:part>2</pdfaid:part>\n")
                sb.append("  <pdfaid:conformance>A</pdfaid:conformance>\n")
            } else if compliance == Compliance.PDF_A_2B {
                sb.append("  <pdfaid:part>2</pdfaid:part>\n")
                sb.append("  <pdfaid:conformance>B</pdfaid:conformance>\n")
            } else if compliance == Compliance.PDF_A_3A {
                sb.append("  <pdfaid:part>3</pdfaid:part>\n")
                sb.append("  <pdfaid:conformance>A</pdfaid:conformance>\n")
            } else if compliance == Compliance.PDF_A_3B {
                sb.append("  <pdfaid:part>3</pdfaid:part>\n")
                sb.append("  <pdfaid:conformance>B</pdfaid:conformance>\n")
            }

            sb.append("  <pdf:Producer>")
            sb.append(producer)
            sb.append("</pdf:Producer>\n")

            if title != nil {
                sb.append("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">")
                sb.append(title!)
                sb.append("</rdf:li></rdf:Alt></dc:title>\n")
            }

            if author != nil {
                sb.append("  <dc:creator><rdf:Seq><rdf:li>")
                sb.append(author!)
                sb.append("</rdf:li></rdf:Seq></dc:creator>\n")
            }

            if subject != nil {
                sb.append("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">")
                sb.append(subject!)
                sb.append("</rdf:li></rdf:Alt></dc:description>\n")
            }

            if keywords != nil {
                sb.append("  <pdf:Keywords>")
                sb.append(keywords!)
                sb.append("</pdf:Keywords>\n")
            }

            if creator != nil {
                sb.append("  <xmp:CreatorTool>")
                sb.append(creator!)
                sb.append("</xmp:CreatorTool>\n")
            }

            sb.append("  <xmp:CreateDate>")
            sb.append(createDate! + "-05:00")   // Append the time zone.
            sb.append("</xmp:CreateDate>\n")

            sb.append("  <xapMM:DocumentID>uuid:")
            sb.append(uuid)
            sb.append("</xapMM:DocumentID>\n")

            sb.append("  <xapMM:InstanceID>uuid:")
            sb.append(uuid)
            sb.append("</xapMM:InstanceID>\n")

            sb.append("</rdf:Description>\n")
        }

        if !fontMetadataObject {
            // Add the recommended 2000 bytes padding
            for _ in 0..<20 {
                for _ in 0..<10 {
                    sb.append("          ")
                }
                sb.append("\n")
            }
        }

        sb.append("</rdf:RDF>\n")
        sb.append("</x:xmpmeta>\n")
        sb.append("<?xpacket end=\"w\"?>")

        let buf = [UInt8](sb.utf8)

        // This is the metadata object
        newobj()
        append(Token.beginDictionary)
        append("/Type /Metadata\n")
        append("/Subtype /XML\n")
        append(Token.length)
        append(buf.count)
        append(Token.newline)
        append(Token.endDictionary)
        append(Token.stream)
        append(buf)
        append(Token.endStream)
        endobj()

        return self.getObjNumber()
    }

    func addOutputIntentObject() -> Int {
        newobj()
        append(Token.beginDictionary)
        append("/N 3\n")

        append(Token.length)
        append(ICCBlackScaled.profile.count)
        append(Token.newline)

        append("/Filter /FlateDecode\n")
        append(Token.endDictionary)
        append(Token.stream)
        append(ICCBlackScaled.profile, 0, ICCBlackScaled.profile.count)
        append(Token.endStream)
        endobj()

        // OutputIntent object
        newobj()
        append(Token.beginDictionary)
        append("/Type /OutputIntent\n")
        append("/S /GTS_PDFA1\n")
        append("/OutputCondition (sRGB IEC61966-2.1)\n")
        append("/OutputConditionIdentifier (sRGB IEC61966-2.1)\n")
        append("/Info (sRGB IEC61966-2.1)\n")
        append("/DestOutputProfile ")
        append(getObjNumber() - 1)
        append(Token.objRef)
        append(Token.endDictionary)
        endobj()

        return self.getObjNumber()
    }

    // Appends a token of a PDF that was read. Each of its characters is a byte
    // of the PDF, so a string or name with bytes of 0x80 or more is copied
    // unchanged, where UTF-8 would write two bytes for each of them.
    private func appendToken(_ token: String) {
        append(token.unicodeScalars.map { UInt8(truncatingIfNeeded: $0.value) })
    }

    /// Writes the "/Name number 0 R" entries collected from a PDF that was read.
    private func appendImportedEntries(_ tokens: [String]) {
        for token in tokens {
            appendToken(token)
            if token == "R" {
                append(Token.newline)
            } else {
                append(Token.space)
            }
        }
    }

    private func addResourcesObject() -> Int {
        newobj()
        append(Token.beginDictionary)
        if extGState != "" {
            appendToken(extGState)
        }
        if fonts.count > 0 || importedFonts.count > 0 {
            append("/Font\n")
            append(Token.beginDictionary)
            appendImportedEntries(importedFonts)
            for font in fonts {
                append("/F")
                append(font.objNumber)
                append(Token.space)
                append(font.objNumber)
                append(Token.objRef)
            }
            append(Token.endDictionary)
        }
        if images.count > 0 || stamps.count > 0 || importedXObjects.count > 0 {
            append("/XObject\n")
            append(Token.beginDictionary)
            appendImportedEntries(importedXObjects)
            for image in images {
                append("/Im")
                append(image.objNumber!)
                append(Token.space)
                append(image.objNumber!)
                append(Token.objRef)
            }
            for stamp in stamps {
                append("/Fm")
                append(stamp.objNumber!)
                append(Token.space)
                append(stamp.objNumber!)
                append(Token.objRef)
            }
            append(Token.endDictionary)
        }
        if groups.count > 0 {
            append("/Properties\n")
            append(Token.beginDictionary)
            for i in 0..<groups.count {
                let ocg = groups[i]
                append("/OC")
                append(i + 1)
                append(Token.space)
                append(ocg.objNumber)
                append(Token.objRef)
            }
            append(Token.endDictionary)
        }
        // String state = "/CA 0.5 /ca 0.5"
        if states.count > 0 {
            append("/ExtGState <<\n")
            for (state, number) in states.sorted(by: { $0.value < $1.value }) {
                append("/GS")
                append(number)
                append(" <<")
                append(state)
                append(Token.endDictionary)
            }
            append(Token.endDictionary)
        }
        append(Token.endDictionary)
        endobj()
        return getObjNumber()
    }

    private func addPagesObject() {
        newobj()
        append(Token.beginDictionary)
        append("/Type /Pages\n")
        append("/Kids [\n")
        for page in pages {
            if compliance != Compliance.PDF_17 {
                page.setStructElementsPageObjNumber(page.objNumber)
            }
            append(page.objNumber)
            append(Token.objRef)
        }
        append("]\n")
        append("/Count ")
        append(pages.count)
        append(Token.newline)
        append(Token.endDictionary)
        endobj()
    }

    private func addStructTreeRootObject() -> Int {
        newobj()
        append(Token.beginDictionary)
        append("/Type /StructTreeRoot\n")
        append("/ParentTree ")
        append(getObjNumber() + 1)
        append(Token.objRef)
        append("/K [\n")
        append(getObjNumber() + 2)
        append(Token.objRef)
        append("]\n")
        append(Token.endDictionary)
        endobj()
        return getObjNumber()
    }

    @discardableResult
    private func addStructDocumentObject(_ parent: Int) -> Int {
        newobj()
        append(Token.beginDictionary)
        append("/Type /StructElem\n")
        append("/S /Document\n")
        append("/P ")
        append(parent)
        append(" 0 R\n")
        append("/K [\n")
        for structElement in self.structElements {
            append(structElement.objNumber!)
            append(Token.objRef)
        }
        append("]\n")
        append(Token.endDictionary)
        endobj()
        return getObjNumber()
    }

    private func addStructElementObjects() {
        var structTreeRootObjNumber = getObjNumber() + 1
        structTreeRootObjNumber += self.structElements.count
        for element in self.structElements {
            newobj()
            element.objNumber = getObjNumber()
            append("<<\n/Type /StructElem /S /")
            append(element.structure!)
            append("\n/P ")
            append(structTreeRootObjNumber + 2)  // Use the document struct as parent!
            append(" 0 R /Pg ")
            // A detached page is never added to pages, so its elements never
            // get an object number. Fall back to 0 rather than trapping, which
            // is what the other ports do.
            append(element.pageObjNumber ?? 0)
            append(Token.objRef)

            if element.annotation != nil {
                append("/K <</Type /OBJR /Obj ")
                append(element.annotation!.objNumber)
                append(" 0 R>>\n")
            } else {
                append("/K ")
                append(element.mcid)
                append("\n")
            }

            if let actualText = element.actualText, !actualText.isEmpty,
                    let altDescription = element.altDescription, !altDescription.isEmpty {
                let language = element.language ?? self.language

                append("/Lang <")
                append(toHex(language))
                append(">\n")

                append("/ActualText <")
                append(toHex(actualText))
                append(">\n")

                append("/Alt <")
                append(toHex(altDescription))
                append(">\n")
            }

            append(">>\n")
            endobj()
        }
    }

    private func addNumsParentTree() {
        var buffer = String()

        objOffset.append(byteCount)
        buffer.append(String(objOffset.count))
        buffer.append(" 0 obj\n")

        buffer.append("<<\n")
        buffer.append("/Nums [\n")
        // The keys must be listed in increasing order, so the page entries -
        // whose keys are the /StructParents values 0 .. pages.count-1 - come
        // first. Each value is the array of struct elements of that page,
        // indexed by the MCID they were marked with.
        for (i, page) in pages.enumerated() {
            buffer.append(String(i))
            buffer.append(" [")
            for element in page.structures where element.annotation == nil {
                buffer.append(" ")
                buffer.append(String(element.objNumber!))
                buffer.append(" 0 R")
            }
            buffer.append("]\n")
        }
        // The annotations follow, keyed by the /StructParent values handed out
        // by addAnnotDictionaries(), which continue where the pages left off.
        var index = pages.count
        for element in self.structElements {
            if element.annotation != nil {
                buffer.append(String(index))
                buffer.append(" ")
                buffer.append(String(element.objNumber!))
                buffer.append(" 0 R\n")
                index += 1
            }
        }
        buffer.append("]\n")
        buffer.append(">>\n")
        buffer.append("endobj\n")
        append(buffer)
    }

    private func addRootObject(
            _ structTreeRootObjNumber: Int,
            _ outlineDictNumber: Int) -> Int {
        // Add the root object
        newobj()
        append(Token.beginDictionary)
        append("/Type /Catalog\n")
        if compliance != Compliance.PDF_17 {
            append("/Lang (")
            append(language)
            append(")\n")

            append("/StructTreeRoot ")
            append(structTreeRootObjNumber)
            append(Token.objRef)

            append("/MarkInfo <</Marked true>>\n")
            append("/ViewerPreferences <</DisplayDocTitle true>>\n")
        }

        if pageLayout != nil {
            append("/PageLayout /")
            append(pageLayout!)
            append(Token.newline)
        }

        if pageMode != nil {
            append("/PageMode /")
            append(pageMode!)
            append(Token.newline)
        }

        if !groups.isEmpty {
            addOCProperties()
        }

        append("/Pages ")
        append(pagesObjNumber)
        append(Token.objRef)

        if compliance != Compliance.PDF_17 {
            append("/Metadata ")
            append(metadataObjNumber)
            append(Token.objRef)

            append("/OutputIntents [")
            append(outputIntentObjNumber)
            append(" 0 R]\n")
        }

        if outlineDictNumber > 0 {
            append("/Outlines ")
            append(outlineDictNumber)
            append(Token.objRef)
        }

        append(Token.endDictionary)
        endobj()
        return getObjNumber()
    }

    private func addPageBox(
            _ buffer: inout String,
            _ boxName: String,
            _ page: Page,
            _ rect: [Float]) {
        buffer.append("/")
        buffer.append(boxName)
        buffer.append(" [")
        buffer.append(String(rect[0]))
        buffer.append(" ")
        buffer.append(String(page.height - rect[3]))
        buffer.append(" ")
        buffer.append(String(rect[2]))
        buffer.append(" ")
        buffer.append(String(page.height - rect[1]))
        buffer.append("]\n")
    }

    private func setDestinationObjNumbers() {
        var numberOfAnnotations = 0
        for page in pages {
            numberOfAnnotations += page.annots.count
        }
        for i in 0..<pages.count {
            let page = pages[i]
            for destination in page.destinations {
                destination.pageObjNumber = getObjNumber() + numberOfAnnotations + i + 1
                destinations[destination.name!] = destination
            }
        }
    }

    private func addAllPages(_ resObjNumber: Int) {
        setDestinationObjNumbers()
        addAnnotDictionaries()
        // Calculate the object number of the Pages object
        pagesObjNumber = getObjNumber() + pages.count + 1
        for (i, page) in pages.enumerated() {
            // Page object
            newobj()
            var buffer = String()
            page.objNumber = getObjNumber()
            buffer.append("<<\n")
            buffer.append("/Type /Page\n")
            buffer.append("/Parent ")
            buffer.append(String(pagesObjNumber))
            buffer.append(" 0 R\n")
            buffer.append("/MediaBox [0 0 ")
            buffer.append(String(Int(page.width)))
            buffer.append(" ")
            buffer.append(String(Int(page.height)))
            buffer.append("]\n")

            if page.cropBox != nil {
                addPageBox(&buffer, "CropBox", page, page.cropBox!)
            }
            if page.bleedBox != nil {
                addPageBox(&buffer, "BleedBox", page, page.bleedBox!)
            }
            if page.trimBox != nil {
                addPageBox(&buffer, "TrimBox", page, page.trimBox!)
            }
            if page.artBox != nil {
                addPageBox(&buffer, "ArtBox", page, page.artBox!)
            }

            buffer.append("/Resources ")
            buffer.append(String(resObjNumber))
            buffer.append(" 0 R\n")

            buffer.append("/Contents [ ")
            for n in page.contents {
                buffer.append(String(n))
                buffer.append(" 0 R ")
            }
            buffer.append("]\n")
            if page.annots.count > 0 {
                buffer.append("/Annots [ ")
                for annot in page.annots {
                    buffer.append(String(annot.objNumber))
                    buffer.append(" 0 R ")
                }
                buffer.append("]\n")
            }
            if compliance != Compliance.PDF_17 {
                buffer.append("/Tabs /S\n")
                buffer.append("/StructParents ")
                buffer.append(String(i))
                buffer.append("\n")
            }
            buffer.append(">>\n")
            append(buffer)
            endobj()
        }
    }

    private func addPageContent(_ page: Page) {
        if contentStreamsCompression {
            var buffer = [UInt8]()
            FlateEncode(&buffer, page.buf)
            page.buf.removeAll()   // Release the page content memory!

            newobj()
            append(Token.beginDictionary)
            append("/Filter /FlateDecode\n")
            append(Token.length)
            append(buffer.count)
            append(Token.newline)
            append(Token.endDictionary)
            append(Token.stream)
            append(buffer)
            append(Token.endStream)
            endobj()
            page.contents.append(getObjNumber())
        } else {    // No compression. Used for diagnostics
            newobj()
            append(Token.beginDictionary)
            append(Token.length)
            append(page.buf.count)
            append(Token.newline)
            append(Token.endDictionary)
            append(Token.stream)
            append(page.buf)
            append(Token.endStream)
            endobj()
            page.contents.append(getObjNumber())
        }
    }

    @discardableResult
    func addAnnotationObject(_ annot: Annotation, _ index: Int) -> Int {
        var index = index

        newobj()
        annot.objNumber = getObjNumber()

        append(Token.beginDictionary)
        append("/Type /Annot\n")
        append("/Subtype /")
        append(annot.annotationType!)
        append("\n")

        append("/Rect [")
        append(annot.x1)
        append(" ")
        append(annot.y1)
        append(" ")
        append(annot.x2)
        append(" ")
        append(annot.y2)
        append("]\n")

        append("/Border [0 0 0]\n")

        if annot.annotationType == Annotation.FileAttachment {
            append("/FS ")
            append(annot.fileAttachment!.embeddedFile!.objNumber)
            append(" 0 R\n")

            append("/Name /")
            append(annot.fileAttachment!.icon)
            append("\n")

            if annot.fileAttachment != nil {
                append("/T <")
                append(toHex(annot.fileAttachment!.title))
                append(">\n")
                append("/Contents <")
                append(toHex(annot.fileAttachment!.contents))
                append(">\n")
            }
        } else if annot.annotationType == Annotation.Link {
            // PDF/UA requires a link to carry an alternate description in its
            // Contents key.
            var description = annot.contents
            if description == nil || description!.isEmpty {
                description = annot.altDescription
            }
            if description == nil || description!.isEmpty {
                description = annot.uri
            }
            if description == nil || description!.isEmpty {
                description = annot.key
            }
            if let description = description, !description.isEmpty {
                append("/Contents <")
                append(toHex(description))
                append(">\n")
            }
            if let uri = annot.uri {
                append("/F 4\n")
                append("/A <<\n")
                append("/S /URI\n")
                append("/URI <")
                append(toHex(uri))
                append(">\n")
                append(">>\n")
            } else if let key = annot.key,
                      let destination = destinations[key] {
                append("/F 4\n")
                append("/Dest [")
                append(destination.pageObjNumber)
                append(" 0 R /XYZ ")
                append(destination.xPosition)
                append(" ")
                append(destination.yPosition)
                append(" 0]\n")
            }
        } else if annot.annotationType == Annotation.Polygon {
            append("/Vertices [ ")
            var i = 0
            while i < annot.vertices!.count {
                append(annot.x1 + annot.vertices![i])
                append(" ")
                append(annot.y1 - annot.vertices![i + 1])
                append(" ")
                i += 2
            }
            append("]\n")

            append("/IC [")
            append(annot.fillColor![0])
            append(" ")
            append(annot.fillColor![1])
            append(" ")
            append(annot.fillColor![2])
            append("]\n")

            append("/CA ")
            append(annot.transparency)
            append("\n")

            if let title = annot.title {
                append("/T <")
                append(toHex(title))
                append(">\n")
            }

            if let contents = annot.contents {
                append("/Contents <")
                append(toHex(contents))
                append(">\n")
            }
        } else if annot.annotationType == Annotation.Square ||
                  annot.annotationType == Annotation.Circle {
            append("/IC [")
            append(annot.fillColor![0])
            append(" ")
            append(annot.fillColor![1])
            append(" ")
            append(annot.fillColor![2])
            append("]\n")

            append("/CA ")
            append(annot.transparency)
            append("\n")

            if let title = annot.title {
                append("/T <")
                append(toHex(title))
                append(">\n")
            }

            if let contents = annot.contents {
                append("/Contents <")
                append(toHex(contents))
                append(">\n")
            }
        } else if annot.annotationType == Annotation.Text {
            append("/Name /Comment\n")
            if let title = annot.title {
                append("/T <")
                append(toHex(title))
                append(">\n")
            }

            if let contents = annot.contents {
                append("/Contents <")
                append(toHex(contents))
                append(">\n")
            }
        }

        if index != -1 {
            append("/StructParent ")
            append(index)
            append("\n")
            index += 1
        }

        append(Token.endDictionary)
        endobj()

        return index
    }

    private func addAnnotDictionaries() {
        var index = self.pages.count
        for element in self.structElements {
            if element.annotation != nil {
                index = addAnnotationObject(element.annotation!, index)
                element.annotation!.structParentWritten = true
            }
        }

        for page in self.pages {
            for annotation in page.annots {
                // Skip the annotations that were already written above -
                // writing them twice would leave the page referencing a copy
                // that has no /StructParent key.
                if !annotation.structParentWritten {
                    addAnnotationObject(annotation, -1)
                }
            }
        }
    }

    private func addOCProperties() {
        var list = [OCG]()
        var buf = String()
        for ocg in self.groups {
            buf.append(" ")
            buf.append(String(ocg.objNumber))
            buf.append(" 0 R")
            list.append(OCG(objNumber: ocg.objNumber, name: ocg.name!))
        }
        list.sort { $0.name < $1.name }

        append("/OCProperties\n")
        append(Token.beginDictionary)
        append("/OCGs [")
        append(buf)
        append(" ]\n")
        append("/D <<\n")

        append("/AS [\n")
        append("<< /Event /View /Category [/View] /OCGs [")
        append(buf)
        append(" ] >>\n")
        append("<< /Event /Print /Category [/Print] /OCGs [")
        append(buf)
        append(" ] >>\n")
        append("<< /Event /Export /Category [/Export] /OCGs [")
        append(buf)
        append(" ] >>\n")
        append("]\n")

        append("/Order [")
        for ocg in list {
            append(" ")
            append(ocg.objNumber)
            append(" 0 R ")
        }
        append("]\n")

        append(Token.endDictionary)
        append(Token.endDictionary)
    }

    /// Adds the page to this document.
    public func addPage(_ page: Page) {
        pages.append(page)
        if prevPage != nil {
            addPageContent(prevPage!)
        }
        prevPage = page
    }

    /// Adds the pages to this document.
    public func addPages(_ pages: [Page]) {
        for page in pages {
            addPage(page)
        }
    }

    ///
    /// Completes the construction of the PDF and writes it to the output stream.
    /// The output stream is then automatically closed.
    ///
    public func complete() {
        if prevPage != nil {
            addPageContent(prevPage!)
        }
        if compliance != Compliance.PDF_17 {
            metadataObjNumber = addMetadataObject("", false)
            outputIntentObjNumber = addOutputIntentObject()
        }

        if pagesObjNumber == 0 {
            addAllPages(addResourcesObject())
            addPagesObject()
        }

        var structTreeRootObjNumber = 0
        if compliance != Compliance.PDF_17 {
            addStructElementObjects()
            structTreeRootObjNumber = addStructTreeRootObject()
            addNumsParentTree()
            addStructDocumentObject(structTreeRootObjNumber)
        }

        var outlineDictNum = 0
        if toc != nil && toc!.getChildren() != nil {
            let list: [Bookmark] = toc!.toArrayList()
            outlineDictNum = addOutlineDict(toc!)
            var i = 1
            while i < list.count {
                let bookmark = list[i]
                addOutlineItem(outlineDictNum, i, bookmark)
                i += 1
            }
        }

        let rootObjNumber = addRootObject(structTreeRootObjNumber, outlineDictNum)
        let startxref = byteCount

        // Create the xref table
        append("xref\n")
        append("0 ")
        append(rootObjNumber + 1)
        append("\n")
        append("0000000000 65535 f \n")
        var buffer = String()
        for offset in objOffset {
            if offset == 0 {    // A number that no object was written for.
                buffer.append("0000000000 65535 f \n")
                continue
            }
            let str = String(offset)
            for _ in 0..<(10 - str.count) {
                buffer.append("0")
            }
            buffer.append(str)
            buffer.append(" 00000 n \n")
        }
        append(buffer)
        append("trailer\n")
        append(Token.beginDictionary)
        append("/Size ")
        append(rootObjNumber + 1)
        append(Token.newline)

        append("/ID[<")
        append(uuid)
        append("><")
        append(uuid)
        append(">]\n")

        append("/Root ")
        append(rootObjNumber)
        append(Token.objRef)

        append(Token.endDictionary)
        append("startxref\n")
        append(startxref)
        append(Token.newline)
        append("%%EOF\n")

        os!.close()
    }

    ///
    /// Set the "Language" document property of the PDF file.
    /// - Parameter language: The language of this document.
    ///
    @discardableResult
    public func setLanguage(_ language: String) -> PDF {
        self.language = language
        return self
    }

    ///
    /// Set the "Title" document property of the PDF file.
    /// - Parameter title: The title of this document.
    ///
    @discardableResult
    public func setTitle(_ title: String) -> PDF {
        self.title = title
        return self
    }

    ///
    /// Set the "Author" document property of the PDF file.
    /// - Parameter author: The author of this document.
    ///
    @discardableResult
    public func setAuthor(_ author: String) -> PDF {
        self.author = author
        return self
    }

    ///
    /// Set the "Subject" document property of the PDF file.
    /// - Parameter subject: The subject of this document.
    ///
    @discardableResult
    public func setSubject(_ subject: String) -> PDF {
        self.subject = subject
        return self
    }

    ///
    /// Set the "Keywords" document property of the PDF file.
    /// - Parameter keywords: The author of this document.
    ///
    @discardableResult
    public func setKeywords(_ keywords: String) -> PDF {
        self.keywords = keywords
        return self
    }

    ///
    /// Set the "Creator" document property of the PDF file.
    /// - Parameter creator: The author of this document.
    ///
    @discardableResult
    public func setCreator(_ creator: String) -> PDF {
        self.creator = creator
        return self
    }

    /// Sets the page layout used when the document is opened. See PageLayout.
    @discardableResult
    public func setPageLayout(_ pageLayout: String) -> PDF {
        self.pageLayout = pageLayout
        return self
    }

    /// Sets the page mode used when the document is opened. See PageMode.
    @discardableResult
    public func setPageMode(_ pageMode: String) -> PDF {
        self.pageMode = pageMode
        return self
    }

    func append(_ number: UInt8) {
        append([UInt8](arrayLiteral: number))
    }

    func append(_ number: UInt16) {
        append(String(number))
    }

    func append(_ number: Int32) {
        append(String(number))
    }

    func append(_ number: UInt32) {
        append(String(number))
    }

    func append(_ number: Int) {
        append(String(number))
    }

    func append(_ val: Float) {
        append(FastFloat.toByteArray(val))
    }

    func append(_ str: String) {
        if str.count == 0 {
            return
        }
        append(Array(str.utf8))
    }

    func append(_ buf: [UInt8], _ off: Int, _ len: Int) {
        os!.write(buf, off, len)
        self.byteCount += len
    }

    func append(_ buf: [UInt8]) {
        os!.write(buf)
        self.byteCount += buf.count
    }

    func getSortedObjects(_ objects: [PDFobj]) -> [PDFobj] {
        var sorted = [PDFobj]()
        var maxObjNumber = 0
        for obj in objects {
            if obj.number > maxObjNumber {
                maxObjNumber = obj.number
            }
        }
        for number in 1...maxObjNumber {
            let obj = PDFobj()
            obj.setNumber(number)
            sorted.append(obj)
        }
        for obj in objects {
            sorted[obj.number - 1] = obj
        }
        return sorted
    }

    ///
    /// Returns a list of objects of type PDFobj read from input stream.
    /// An encrypted PDF is decrypted when it opens without a password.
    ///
    /// - Parameter stream: the PDF input stream.
    ///
    /// - Returns: [PDFobj] the list of PDF objects.
    ///
    public func read(from stream: InputStream) throws -> [PDFobj] {
        var buffer1 = try Content.getFromStream(stream)
        var objects1 = [PDFobj]()
        let startXRef = getStartXRef(buffer1)
        var trailer: PDFobj?
        do {
            trailer = try getObjects(&buffer1, startXRef, &objects1, 0)
        } catch {
            trailer = nil       // A cross-reference stream that cannot be decoded.
        }
        if trailer == nil || objects1.isEmpty {
            // The cross-reference table is missing or wrong, like in a PDF that
            // was changed without updating it.
            objects1.removeAll()
            trailer = getObjectsByScanning(buffer1, &objects1)
        }
        let decryptor = try Decryptor.getDecryptor(trailer, objects1)

        var objects2 = [PDFobj]()
        for obj in objects1 {
            let type = obj.getValue("/Type")
            if type == "/XRef" {
                continue        // Skip the cross-reference streams.
            }
            if let decryptor = decryptor {
                if obj.number == decryptor.objNumber {
                    continue    // Skip the encryption dictionary.
                }
                decryptor.decryptStrings(obj)
            }
            if obj.dict.contains("stream") {
                try obj.setStreamAndData(&buffer1, obj.getLength(&objects1)!, decryptor)
            }
            if type == "/ObjStm" {
                let first = Int(obj.getValue("/First"))!
                let o2 = getObject(obj.data, 0, first)
                var i = 0
                while i < o2.dict.count {
                    let num = o2.dict[i]
                    let off = o2.dict[i + 1]
                    var end = obj.data.count
                    if i <= o2.dict.count - 4 {
                        end = first + Int(o2.dict[i + 3])!
                    }
                    let o3 = getObject(obj.data, first + Int(off)!, end)
                    o3.number = Int(num)!
                    o3.dict.insert(contentsOf: [num, "0", "obj"], at: 0)
                    objects2.append(o3)
                    i += 2
                }
            } else {
                objects2.append(obj)
            }
        }
        return getSortedObjects(objects2)
    }

    private func process(
            _ obj: PDFobj,
            _ token: inout [UInt8],
            _ buffer: [UInt8],
            _ offset: Int) -> Bool {
        // Like trim() in Java, which removes the characters up to the space.
        var start = 0
        var end = token.count
        while start < end && token[start] <= 0x20 {
            start += 1
        }
        while end > start && token[end - 1] <= 0x20 {
            end -= 1
        }
        if start == end {
            token.removeAll()
            return false
        }
        // ISO Latin 1 keeps each byte of a string as a character.
        let str = String(bytes: token[start..<end], encoding: .isoLatin1)!
        obj.dict.append(str)
        token.removeAll()
        if str == "endobj" {
            return true
        }
        if str == "stream" {
            obj.streamOffset = offset
            if offset < buffer.count && buffer[offset] == 0x0A {   // "\n"
                obj.streamOffset += 1
            }
            return true
        }
        if str == "startxref" {
            return true
        }
        return false
    }

    private func getObject(_ buf: [UInt8], _ off: Int) -> PDFobj {
        if off < 0 || off >= buf.count {
            return PDFobj()     // An offset outside of the PDF has no tokens.
        }
        return getObject(buf, off, buf.count)
    }

    private func getObject(_ buf: [UInt8], _ off: Int, _ len: Int) -> PDFobj {
        var offset = off
        let obj = PDFobj()
        obj.offset = offset
        var token = [UInt8]()

        var p = 0               // The nesting level of the parentheses in a literal string
        var done = false
        while !done && offset < len {
            let c2 = buf[offset]
            offset += 1
            if p > 0 {
                // A literal string is one token, with its white space and
                // delimiters. A backslash escapes the character after it.
                token.append(c2)
                if c2 == 0x5C {             // "\\"
                    if offset < len {
                        token.append(buf[offset])
                        offset += 1
                    }
                } else if c2 == 0x28 {      // "("
                    p += 1
                } else if c2 == 0x29 {      // ")"
                    p -= 1
                    if p == 0 {
                        done = process(obj, &token, buf, offset)
                    }
                }
            } else if c2 == 0x28 {          // "("
                done = process(obj, &token, buf, offset)
                if !done {
                    token.append(c2)
                    p = 1
                }
            } else if isWhiteSpace(c2) {
                done = process(obj, &token, buf, offset)
            } else if c2 == 0x2F {          // "/"
                done = process(obj, &token, buf, offset)
                if !done {
                    token.append(c2)
                }
            } else if c2 == 0x3C || c2 == 0x3E {    // "<" or ">"
                done = process(obj, &token, buf, offset)
                if !done {
                    if offset < len && buf[offset] == c2 {
                        obj.dict.append((c2 == 0x3C) ? "<<" : ">>")
                        offset += 1
                    } else if c2 == 0x3C {
                        // A hexadecimal string is one token, without its white space.
                        token.append(c2)
                        while offset < len && buf[offset] != 0x3E {
                            if !isWhiteSpace(buf[offset]) {
                                token.append(buf[offset])
                            }
                            offset += 1
                        }
                        token.append(0x3E)
                        offset += 1
                        done = process(obj, &token, buf, offset)
                    } else {
                        obj.dict.append(">")
                    }
                }
            } else if c2 == 0x25 {          // "%"
                // A comment ends at the end of the line.
                done = process(obj, &token, buf, offset)
                while !done && offset < len && buf[offset] != 0x0A && buf[offset] != 0x0D {
                    offset += 1
                }
            } else if c2 == 0x5B ||         // "["
                    c2 == 0x5D ||           // "]"
                    c2 == 0x7B ||           // "{"
                    c2 == 0x7D {            // "}"
                done = process(obj, &token, buf, offset)
                if !done {
                    if c2 == 0x5B {
                        obj.dict.append("[")
                    } else if c2 == 0x5D {
                        obj.dict.append("]")
                    } else if c2 == 0x7B {
                        obj.dict.append("{")
                    } else {
                        obj.dict.append("}")
                    }
                }
            } else {
                token.append(c2)
            }
        }
        if !done {
            _ = process(obj, &token, buf, offset)   // The last token, at the end of the data.
        }

        return obj
    }

    private func isWhiteSpace(_ c: UInt8) -> Bool {
        return c == 0x00            // Null
            || c == 0x09            // Horizontal Tab
            || c == 0x0A            // Line Feed (LF)
            || c == 0x0C            // Form Feed
            || c == 0x0D            // Carriage Return (CR)
            || c == 0x20            // Space
    }

    ///
    /// Converts an array of bytes to an integer.
    /// - Parameter buf: byte[]
    /// - Returns: int
    ///
    private func toInt(
            _ buf: [UInt8],
            _ off: Int,
            _ len: Int) -> Int {
        var n = 0
        var i = 0
        while i < len {
            n |= Int(buf[off + i]) & 0xFF
            if i < (len - 1) {
                n = n &<< 8
            }
            i += 1
        }
        return n
    }

    // Returns the value of the token, or -1 when it is not an integer.
    private func toInteger(_ token: String) -> Int {
        if isInteger(token), let value = Int32(token) {
            return Int(value)
        }
        return -1
    }

    // Returns true when the tokens of the object start with "number generation
    // obj", where the number is above 0, and is the number that is given unless
    // that is -1.
    private func isObject(_ obj: PDFobj, _ number: Int) -> Bool {
        if obj.dict.count < 3 || obj.dict[2] != "obj" || !isInteger(obj.dict[1]) {
            return false
        }
        let n = toInteger(obj.dict[0])
        return n > 0 && (number == -1 || n == number)
    }

    ///
    /// Adds the objects of the cross-reference section at the offset to the
    /// list, after the objects of the sections before it, so that the newest
    /// version of an object that was updated comes last. A section is a
    /// cross-reference table, which can have an /XRefStm stream for the
    /// objects in object streams, or a cross-reference stream.
    ///
    /// - Returns: the trailer of the section, which is the cross-reference
    ///   stream object when there is no table, or nil when an offset in the
    ///   section is not that of its object.
    ///
    private func getObjects(
            _ buf: inout [UInt8],
            _ offset: Int,
            _ objects: inout [PDFobj],
            _ depth: Int) throws -> PDFobj? {
        let xref = getObject(buf, offset)
        let table = !xref.dict.isEmpty && xref.dict[0] == "xref"
        if depth > 1000 || (!table && !isObject(xref, -1)) {
            return nil
        }
        let prev = xref.getValue("/Prev")
        if !prev.isEmpty {
            if try getObjects(&buf, toInteger(prev), &objects, depth + 1) == nil {
                return nil
            }
        }
        if table {
            // The objects in the table replace those in the /XRefStm stream.
            let xrefStm = xref.getValue("/XRefStm")
            if !xrefStm.isEmpty {
                let stream = getObject(buf, toInteger(xrefStm))
                if try !getStreamObjects(&buf, stream, &objects) {
                    return nil
                }
            }
            if !getTableObjects(buf, xref, &objects) {
                return nil
            }
        } else if try !getStreamObjects(&buf, xref, &objects) {
            return nil
        }
        return xref
    }

    // Adds the objects in use of a cross-reference table, and returns false
    // when an offset is not that of its object.
    private func getTableObjects(_ buf: [UInt8], _ xref: PDFobj, _ objects: inout [PDFobj]) -> Bool {
        let dict = xref.dict
        var i = 1
        // Each subsection starts with its first object number and the number of entries.
        while i + 1 < dict.count && isInteger(dict[i]) {
            var number = toInteger(dict[i])
            let count = toInteger(dict[i + 1])
            i += 2
            var j = 0
            while j < count {
                if i + 2 >= dict.count {
                    return false
                }
                // The entry is the offset, the generation number and n for an object in use.
                if dict[i + 2] == "n" {
                    let obj = getObject(buf, toInteger(dict[i]))
                    if !isObject(obj, number) {
                        return false
                    }
                    obj.number = number
                    objects.append(obj)
                }
                j += 1
                number += 1
                i += 3
            }
        }
        return i < dict.count && dict[i] == "trailer"
    }

    // Adds the objects of a cross-reference stream that are not in object
    // streams, and returns false when an offset is not that of its object.
    private func getStreamObjects(
            _ buf: inout [UInt8],
            _ xref: PDFobj,
            _ objects: inout [PDFobj]) throws -> Bool {
        if !isObject(xref, -1) || xref.getValue("/Type") != "/XRef" || !xref.dict.contains("stream") {
            return false
        }
        // See page 50 in PDF32000_2008.pdf
        let dict = xref.dict
        guard let w = dict.firstIndex(of: "/W"), w + 4 < dict.count else {
            return false
        }
        let n1 = toInteger(dict[w + 2])     // Field 1 number of bytes
        let n2 = toInteger(dict[w + 3])     // Field 2 number of bytes
        let n3 = toInteger(dict[w + 4])     // Field 3 number of bytes
        let length = toInteger(xref.getValue("/Length"))
        if n1 < 0 || n2 < 0 || n3 < 0 || n1 + n2 + n3 == 0 ||
                length < 0 || xref.streamOffset + length > buf.count {
            return false
        }
        // The /Index array has the first object number and the number of
        // entries of each subsection, and is [0 /Size] when it is missing.
        var index = [Int]()
        if let k = dict.firstIndex(of: "/Index"), k + 1 < dict.count, dict[k + 1] == "[" {
            var i = k + 2
            while i + 1 < dict.count && isInteger(dict[i]) {
                index.append(toInteger(dict[i]))
                index.append(toInteger(dict[i + 1]))
                i += 2
            }
        } else {
            index.append(0)
            index.append(toInteger(xref.getValue("/Size")))
        }

        // setStreamAndData undoes the predictor, so each entry is a row of the data.
        try xref.setStreamAndData(&buf, length)
        let n = n1 + n2 + n3    // Number of bytes per entry
        var offset = 0
        var s = 0
        while s + 1 < index.count {
            var number = index[s]
            var j = 0
            while j < index[s + 1] && offset + n <= xref.data.count {
                // Process the entries in a cross-reference stream.
                // Page 51 in PDF32000_2008.pdf
                let type = (n1 == 0) ? 1 : toInt(xref.data, offset, n1)
                if type == 1 {
                    let obj = getObject(buf, toInt(xref.data, offset + n1, n2))
                    if !isObject(obj, number) {
                        return false
                    }
                    obj.number = number
                    objects.append(obj)
                }
                number += 1
                offset += n
                j += 1
            }
            s += 2
        }
        return true
    }

    ///
    /// Adds the objects of the PDF to the list by looking for "number
    /// generation obj" in it, for when the cross-reference table is missing or
    /// wrong. The objects of incremental updates are later in the PDF, so the
    /// newest version of an object comes last.
    ///
    /// - Returns: the last trailer, or the last cross-reference stream object
    ///   when there is no trailer, or nil when there is neither.
    ///
    private func getObjectsByScanning(_ buf: [UInt8], _ objects: inout [PDFobj]) -> PDFobj? {
        let trailerKeyword = Array("trailer".utf8)
        let endStreamKeyword = Array("endstream".utf8)
        var trailer: PDFobj?
        var xrefStream: PDFobj?
        var i = 0
        while i < buf.count {
            if isObjectStart(buf, i) {
                let obj = getObject(buf, i)
                if isObject(obj, -1) {
                    obj.number = toInteger(obj.dict[0])
                    objects.append(obj)
                    if obj.getValue("/Type") == "/XRef" {
                        xrefStream = obj
                    }
                    if obj.dict.contains("stream") {
                        // Skip the stream, as its bytes can look like an object.
                        let end = indexOf(buf, endStreamKeyword, obj.streamOffset)
                        i = (end == -1) ? buf.count : end
                        continue
                    }
                }
            } else if startsWith(buf, i, trailerKeyword) {
                trailer = getObject(buf, i)
            }
            i += 1
        }
        return trailer ?? xrefStream
    }

    // Returns true when "number generation obj" starts at the offset, after
    // white space or at the start of the PDF.
    private func isObjectStart(_ buf: [UInt8], _ off: Int) -> Bool {
        if off > 0 && !isWhiteSpace(buf[off - 1]) {
            return false
        }
        var i = off
        while i < buf.count && buf[i] >= 0x30 && buf[i] <= 0x39 {
            i += 1
        }
        if i == off {
            return false
        }
        var j = i
        while j < buf.count && isWhiteSpace(buf[j]) {
            j += 1
        }
        var k = j
        while k < buf.count && buf[k] >= 0x30 && buf[k] <= 0x39 {
            k += 1
        }
        var m = k
        while m < buf.count && isWhiteSpace(buf[m]) {
            m += 1
        }
        return j > i && k > j && m > k && m + 3 <= buf.count &&
                buf[m] == 0x6F && buf[m + 1] == 0x62 && buf[m + 2] == 0x6A     // "obj"
    }

    private func startsWith(_ buf: [UInt8], _ off: Int, _ str: [UInt8]) -> Bool {
        if off < 0 || off + str.count > buf.count {
            return false
        }
        for i in 0..<str.count where buf[off + i] != str[i] {
            return false
        }
        return true
    }

    private func indexOf(_ buf: [UInt8], _ str: [UInt8], _ from: Int) -> Int {
        var i = max(from, 0)
        while i + str.count <= buf.count {
            if startsWith(buf, i, str) {
                return i
            }
            i += 1
        }
        return -1
    }

    // Returns the offset after the last startxref, or -1 when there is none.
    private func getStartXRef(_ buf: [UInt8]) -> Int {
        let keyword = Array("startxref".utf8)
        var i = buf.count - keyword.count
        while i >= 0 {
            if startsWith(buf, i, keyword) {
                var j = i + keyword.count
                while j < buf.count && isWhiteSpace(buf[j]) {
                    j += 1
                }
                var offset = 0
                var k = j
                while k < buf.count && buf[k] >= 0x30 && buf[k] <= 0x39 && offset <= Int(Int32.max) {
                    offset = offset * 10 + Int(buf[k] - 0x30)
                    k += 1
                }
                return (k > j && offset <= Int(Int32.max)) ? offset : -1
            }
            i -= 1
        }
        return -1
    }

    /// Adds the outline dictionary for the bookmarks and returns its object number.
    public func addOutlineDict(_ toc: Bookmark) -> Int {
        let numOfChildren = getNumOfChildren(0, toc)
        newobj()
        append(Token.beginDictionary)
        append("/Type /Outlines\n")
        append("/First ")
        append(getObjNumber() + 1)
        append(" 0 R\n")
        append("/Last ")
        append(getObjNumber() + numOfChildren)
        append(" 0 R\n")
        append("/Count ")
        append(numOfChildren)
        append(Token.newline)
        append(Token.endDictionary)
        endobj()
        return getObjNumber()
    }

    /// Adds an outline item for the specified bookmark.
    public func addOutlineItem(
            _ parent: Int,
            _ i: Int,
            _ bm1: Bookmark) {
        var prev = 0
        if bm1.getPrevBookmark() != nil {
            prev = parent + (i - 1)
        }
        var next = 0
        if bm1.getNextBookmark() != nil {
            next = parent + (i + 1)
        }

        var first = 0
        var last = 0
        var count = 0
        if bm1.getChildren() != nil && bm1.getChildren()!.count > 0 {
            first = parent + bm1.getFirstChild()!.objNumber
            last  = parent + bm1.getLastChild()!.objNumber
            count = (-1) * getNumOfChildren(0, bm1)
        }

        newobj()
        append(Token.beginDictionary)
        append("/Title <")
        append(toHex(bm1.getTitle()))
        append(">\n")
        append("/Parent ")
        append(parent)
        append(" 0 R\n")
        if prev > 0 {
            append("/Prev ")
            append(prev)
            append(" 0 R\n")
        }
        if next > 0 {
            append("/Next ")
            append(next)
            append(" 0 R\n")
        }
        if first > 0 {
            append("/First ")
            append(first)
            append(" 0 R\n")
        }
        if last > 0 {
            append("/Last ")
            append(last)
            append(" 0 R\n")
        }
        if count != 0 {
            append("/Count ")
            append(count)
            append("\n")
        }
        append("/F 4\n")        // No Zoom
        append("/Dest [")
        append(bm1.getDestination()!.pageObjNumber)
        append(" 0 R /XYZ ")
        append(bm1.getDestination()!.xPosition)
        append(Token.space)
        append(bm1.getDestination()!.yPosition)
        append(" 0]\n")
        append(Token.endDictionary)
        endobj()
    }

    private func getNumOfChildren(
            _ numOfChildren: Int,
            _ bm1: Bookmark) -> Int {
        var numberOfChildren = numOfChildren
        if let children = bm1.getChildren() {
            for bm2 in children {
                numberOfChildren += 1
                numberOfChildren = getNumOfChildren(numberOfChildren, bm2)
            }
        }
        return numberOfChildren
    }

    /// Adds objects read from an existing PDF to this document.
    public func addObjects(_ objects: inout [PDFobj]) {
        self.pagesObjNumber = Int(getPagesObject(objects)!.dict[0])!
        addObjectsToPDF(&objects)
    }

    /// Returns the root pages object.
    public func getPagesObject(
            _ objects: [PDFobj]) -> PDFobj? {
        for object in objects {
            if object.getValue("/Type") == "/Pages" &&
                    object.getValue("/Parent").isEmpty {
                return object
            }
        }
        return nil
    }

    /// Returns the page objects.
    public func getPageObjects(from objects: [PDFobj]) -> [PDFobj] {
        var pageObjects = [PDFobj]()
        let pagesObject = getPagesObject(objects)!
        getPageObjects(pagesObject, &pageObjects, objects)
        return pageObjects
    }

    private func getPageObjects(
            _ pdfObj: PDFobj,
            _ pages: inout [PDFobj],
            _ objects: [PDFobj]) {
        let kids = pdfObj.getObjectNumbers("/Kids")
        for number in kids {
            let object = objects[number - 1]
            if isPageObject(object) {
                pages.append(object)
            } else {
                getPageObjects(object, &pages, objects)
            }
        }
    }

    private func isPageObject(_ object: PDFobj) -> Bool {
        var isPage = false
        for i in 0..<max(object.dict.count - 1, 0) {
            if object.dict[i] == "/Type" &&
                    object.dict[i + 1] == "/Page" {
                isPage = true
            }
        }
        return isPage
    }

    private func getExtGState(_ resources: PDFobj) -> String {
        var buf = String()
        let dict = resources.getDict()
        var level = 0
        var i = 0
        while i < dict.count {
            if dict[i] == "/ExtGState" {
                buf.append("/ExtGState << ")
                i += 1
                level += 1
                while level > 0 {
                    i += 1
                    let token = dict[i]
                    if token == "<<" {
                        level += 1
                    } else if token == ">>" {
                        level -= 1
                    }
                    buf.append(token)
                    if level > 0 {
                        buf.append(" ")
                    } else {
                        buf.append("\n")
                    }
                }
                break
            }
            i += 1
        }
        return buf
    }

    private func getFontObjects(
            _ resources: PDFobj,
            _ objects: inout [PDFobj]) -> [PDFobj] {
        var fonts = [PDFobj]()
        let dict = resources.getDict()
        var i = 0
        while i < dict.count && dict[i] != "/Font" {
            i += 1
        }
        i += 2  // Skip over "/Font" and the "<<" that follows it.

        // The sub-dictionary holds one "/Name <number> 0 R" entry per font.
        // Every one of them is re-emitted in the resources object, so every
        // one of them has to be collected here - taking only the first left
        // the rest of the references dangling.
        while i < dict.count && dict[i] != ">>" {
            let token = dict[i]
            if token.hasPrefix("/") && (i + 3) < dict.count && dict[i + 3] == "R" {
                // Pages can carry separate resource dictionaries that name the
                // same fonts. They are merged into one /Font dictionary here,
                // so a name that is already present must not be added twice.
                if importedFonts.contains(token) {
                    i += 4
                    continue
                }
                importedFonts.append(token)
                importedFonts.append(dict[i + 1])
                importedFonts.append(dict[i + 2])
                importedFonts.append(dict[i + 3])
                if let number = Int(dict[i + 1]), number > 0, number <= objects.count {
                    fonts.append(objects[number - 1])
                }
                i += 4
                continue
            }
            importedFonts.append(token)
            i += 1
        }

        return fonts
    }

    private func getDescendantFonts(
            _ font: PDFobj,
            _ objects: inout [PDFobj]) -> [PDFobj] {
        var descendantFonts = [PDFobj]()
        let dict = font.getDict()
        for i in 0..<max(dict.count - 2, 0) {
            if dict[i] == "/DescendantFonts" {
                let token = dict[i + 2]
                if token != "]" {
                    let object = objects[Int(token)! - 1]
                    descendantFonts.append(object)
                }
            }
        }
        return descendantFonts
    }

    private func getObject(
            _ name: String,
            _ object: PDFobj,
            _ objects: inout [PDFobj]) -> PDFobj? {
        let dict = object.getDict()
        for i in 0..<max(dict.count - 1, 0) {
            if dict[i] == name {
                let token = dict[i  + 1]
                return objects[Int(token)! - 1]
            }
        }
        return nil
    }

    /// Collects the font descriptor of the given font, together with whichever
    /// embedded font program it carries.
    private func addFontDescriptor(
            _ font: PDFobj,
            _ objects: inout [PDFobj],
            _ resources: inout [PDFobj]) {
        guard let descriptor = getObject("/FontDescriptor", font, &objects) else {
            return
        }
        resources.append(descriptor)
        for key in ["/FontFile", "/FontFile2", "/FontFile3"] {
            if let fontFile = getObject(key, descriptor, &objects) {
                resources.append(fontFile)
            }
        }
    }

    ///
    /// Returns the entries of a sub-dictionary of the resources, like /XObject,
    /// without the brackets around them. The sub-dictionary can also be an
    /// object of its own.
    ///
    private func getResourceEntries(
            _ resources: PDFobj,
            _ name: String,
            _ objects: inout [PDFobj]) -> [String] {
        var entries = [String]()
        var dict = resources.getDict()
        guard var i = dict.firstIndex(of: name) else {
            return entries
        }
        i += 1
        if i >= dict.count {
            return entries
        }
        if isInteger(dict[i]) {     // "/XObject 12 0 R"
            guard let number = Int(dict[i]), number > 0, number <= objects.count else {
                return entries
            }
            dict = objects[number - 1].getDict()
            guard let index = dict.firstIndex(of: "<<") else {
                return entries
            }
            i = index
        }
        if dict[i] != "<<" {
            return entries
        }
        var level = 1
        i += 1
        while i < dict.count {
            let token = dict[i]
            if token == "<<" {
                level += 1
            } else if token == ">>" {
                level -= 1
                if level == 0 {
                    break
                }
            }
            entries.append(token)
            i += 1
        }
        return entries
    }

    private func isInteger(_ token: String) -> Bool {
        return !token.isEmpty && token.allSatisfy { $0 >= "0" && $0 <= "9" }
    }

    ///
    /// Returns the numbers of the objects that "number 0 R" references in the
    /// tokens refer to.
    ///
    private func getReferences(_ tokens: [String]) -> [Int] {
        var numbers = [Int]()
        var i = 0
        while i + 2 < tokens.count {
            if tokens[i + 2] == "R" && isInteger(tokens[i]) && isInteger(tokens[i + 1]),
                    let number = Int(tokens[i]) {
                numbers.append(number)
                i += 3
            } else {
                i += 1
            }
        }
        return numbers
    }

    ///
    /// Collects the object with the given number and every object it refers to,
    /// directly or through other objects, like the color space of an image or
    /// the resources of a form XObject. The page tree is not followed.
    ///
    private func addObjectTree(
            _ number: Int,
            _ objects: inout [PDFobj],
            _ numbers: inout Set<Int>,
            _ resources: inout [PDFobj]) {
        if number <= 0 || number > objects.count || !numbers.insert(number).inserted {
            return
        }
        let object = objects[number - 1]
        let type = object.getValue("/Type")
        if object.dict.isEmpty || type == "/Page" || type == "/Pages" || type == "/Catalog" {
            return
        }
        resources.append(object)
        for reference in getReferences(object.dict) {
            addObjectTree(reference, &objects, &numbers, &resources)
        }
    }

    ///
    /// Collects the images and forms in the /XObject resources, with the
    /// objects they use, and adds their names to the resources object.
    ///
    private func addXObjects(
            _ resObj: PDFobj,
            _ objects: inout [PDFobj],
            _ numbers: inout Set<Int>,
            _ resources: inout [PDFobj]) {
        let entries = getResourceEntries(resObj, "/XObject", &objects)
        var i = 0
        while i < entries.count {
            let token = entries[i]
            if token.hasPrefix("/") && (i + 3) < entries.count && entries[i + 3] == "R" {
                // Like the fonts, a name that an earlier page added is kept.
                if !importedXObjects.contains(token) {
                    importedXObjects.append(contentsOf: entries[i..<i + 4])
                    if let number = Int(entries[i + 1]) {
                        addObjectTree(number, &objects, &numbers, &resources)
                    }
                }
                i += 4
            } else {
                i += 1
            }
        }
    }

    /// Adds the fonts, images and graphics states used by the pages to this document.
    public func addResourceObjects(_ objects: inout [PDFobj]) {
        var resources = [PDFobj]()
        var numbers = Set<Int>()
        let pages = getPageObjects(from: objects)
        for page in pages {
            let resObj = page.getResourcesObject(&objects)!
            let fonts = getFontObjects(resObj, &objects)
            for font in fonts {
                resources.append(font)
                if let obj = getObject("/ToUnicode", font, &objects) {
                    resources.append(obj)
                }
                // A simple font carries its descriptor directly; only a
                // composite one puts it on the descendant.
                addFontDescriptor(font, &objects, &resources)
                let descendantFonts = getDescendantFonts(font, &objects)
                for descendantFont in descendantFonts {
                    resources.append(descendantFont)
                    addFontDescriptor(descendantFont, &objects, &resources)
                }
            }
            addXObjects(resObj, &objects, &numbers, &resources)
            extGState = getExtGState(resObj)
            // The /ExtGState dictionary is copied as it is, so the objects
            // that its entries refer to have to be copied too.
            for number in getReferences(getResourceEntries(resObj, "/ExtGState", &objects)) {
                addObjectTree(number, &objects, &numbers, &resources)
            }
        }
        resources.sort(by: { $0.number < $1.number })
        // An object can be collected twice, like a font that a form XObject
        // uses too, and must be written once.
        var unique = [PDFobj]()
        for obj in resources {
            if unique.isEmpty || unique.last!.number != obj.number {
                unique.append(obj)
            }
        }
        addObjectsToPDF(&unique)
    }

    private func addObjectsToPDF(_ objects: inout [PDFobj]) {
        for obj in objects {
            if obj.offset == 0 {
                setObjOffset(obj.number, byteCount)
                append(obj.number)
                append(" 0 obj\n")
                if !obj.dict.isEmpty {
                    for token in obj.dict {
                        appendToken(token)
                        append(Token.space)
                    }
                }
                if obj.stream != nil {
                    if obj.dict.count == 0 {
                        append("<< /Length ")
                        append(obj.stream!.count)
                        append(" >>")
                    }
                    append(Token.newline)
                    append(Token.stream)
                    append(obj.stream!, 0, obj.stream!.count)
                    append(Token.endStream)
                }
                append(Token.endObj)
            } else {
                setObjOffset(obj.number, byteCount)
                let n = obj.dict.count

                var buffer = String()
                var token: String?
                for i in 0..<n {
                    token = obj.dict[i]
                    buffer.append(token!)
                    if i < (n - 1) {
                        buffer.append(" ")
                    } else {
                        buffer.append("\n")
                    }
                }
                appendToken(buffer)
                if obj.stream != nil {
                    append(obj.stream!)
                    append(Token.endStream)
                }
                if token == nil || token! != "endobj" {
                    append(Token.endObj)
                }
            }
        }
    }

    private let HEX: [UInt8] = Array("0123456789ABCDEF".utf8)

    /// Returns the UTF-8 bytes of the string as uppercase hexadecimal digits.
    public func toHex(_ str: String?) -> String {
        guard let str = str, !str.isEmpty else {
            return ""
        }
        // Java hex encodes the UTF-8 bytes of the string, so a character
        // outside ASCII must be written as its UTF-8 byte sequence - not as
        // its code point - or a PDF reader decodes it as the wrong character.
        var result: [UInt8] = []
        for byte in Array(str.utf8) {
            result.append(HEX[Int((byte >> 4) & 0xF)])
            result.append(HEX[Int(byte        & 0xF)])
        }
        return String(decoding: result, as: UTF8.self)
    }

}   // End of PDF.swift
