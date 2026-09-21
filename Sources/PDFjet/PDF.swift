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
public final class PDF {
    var fonts = [Font]()
    var images = [Image]()
    var pages = [Page]()
    var destinations = [String : Destination]()
    var groups = [OptionalContentGroup]()
    var states = [String : Int]()
    var stamps = [Stamp]()
    var compliance = Compliance.PDF_1_7
    var toc: Bookmark?
    var importedFonts = [String]()
    var importedXObjects = [String]()
    var importedExtGStates = [String]()

    private var metadataObjNumber = 0
    private var outputIntentObjNumber = 0
    private var os: BufferedOutputStream?
    private var objOffset = [Int]()
    private var producer = "PDFjet v9.0.1"
    private var title: String?
    private var author: String?
    private var subject: String?
    private var keywords: String?
    private var creator: String?
    private var createDate: String?
    private var byteCount = 0
    private var pagesObjNumber = 0
    private var pageLayout: PageLayout?
    private var pageMode: PageMode?
    private var language: String = "en-US"
    // The document ID for the trailer and the XMP metadata: 16 random bytes as
    // 32 hexadecimal digits, so documents made at the same time get different IDs.
    private var uuid: String = Cryptography.randomBytes(16).map {
        ($0 < 16 ? "0" : "") + String($0, radix: 16)
    }.joined()
    private var prevPage: Page?
    // The structure elements of the pages of the document, in page order.
    // complete() collects them, so a detached page that was never added has
    // no part in the structure tree.
    private var annotElements = [StructElement]()
    // The elements of the Document element, by number.
    private var documentKids = [Int]()
    // The structure tree root, the parent tree and the Document element take
    // three numbers reserved before the first element is written.
    private var structTreeRootNumber = 0
    private var parentTreeNumber = 0
    private var documentElementNumber = 0
    private var contentStreamsCompression = true
    var encryption: Encryption?

    // The first misuse of the API. Swift records it where Java throws, and
    // complete() then throws it, as the file would be broken.
    var error: String?
    // True after complete(): the document is written and closed.
    var completed = false
    // The pages made for this document, added or detached.
    var pagesCreated = 0
    // Tells this document apart from the others, for the fonts, images and
    // embedded files, which do not keep a reference to their PDF.
    let identity = UUID()

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
        self.init(os, Compliance.PDF_1_7)
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
    /// Info
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
        // Gregorian year and ASCII digits whatever the user's locale is, as in the other ports.
        dateFormatter1.locale = Locale(identifier: "en_US_POSIX")
        dateFormatter1.calendar = Calendar(identifier: .gregorian)
        dateFormatter1.dateFormat = "yyyy-MM-dd'T'HH:mm:ss"
        // The creation date is in UTC, so the XMP metadata says so with a Z.
        dateFormatter1.timeZone = TimeZone(identifier: "UTC")
        self.createDate = dateFormatter1.string(from: date) + "Z"

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
        // The fonts and the page content are written for the compliance.
        if compliance != self.compliance && (getObjNumber() > 0 || pagesCreated > 0) {
            fail("Set the compliance before adding fonts, images or pages to the PDF.")
            return self
        }
        self.compliance = compliance
        return self
    }

    /// Returns the PDF document compliance.
    public func getCompliance() -> Compliance {
        return compliance
    }

    ///
    /// Sets the encryption applied to this document.
    ///
    /// - Parameter encryption: the encryption.
    /// - Returns: this PDF object.
    ///
    @discardableResult
    public func setEncryption(_ encryption: Encryption) -> PDF {
        // Every object after the encryption dictionary is encrypted.
        if encryption.getObjNumber() != getObjNumber() {
            fail("Set the encryption before adding fonts, images or pages to the PDF.")
            return self
        }
        // ISO 19005 does not allow a PDF/A document to be encrypted.
        if compliance != Compliance.PDF_1_7 && compliance != Compliance.PDF_UA_1 {
            fail("A PDF/A document cannot be encrypted.")
            return self
        }
        self.encryption = encryption
        return self
    }

    /// Returns the bytes of a string or a stream, encrypted if the document is
    /// encrypted.
    func encrypted(_ bytes: [UInt8]) -> [UInt8] {
        guard let encryption = encryption else {
            return bytes
        }
        return Cryptography.aesEncryptCBCPadded(bytes, encryption.getKey())
    }

    /// Returns the UTF-8 bytes of the string, encrypted if the document is
    /// encrypted, as hexadecimal digits, for a string object.
    func toHexString(_ str: String?) -> String {
        guard let str = str, !str.isEmpty else {
            return ""
        }
        return toHex(encrypted(Array(str.utf8)))
    }

    /// Returns a text string, like a bookmark title or an alternate
    /// description, as hexadecimal digits: UTF-16BE with a byte order mark,
    /// encrypted if the document is encrypted. A text string without the mark
    /// is in PDFDocEncoding, so UTF-8 bytes would show as two or three wrong
    /// characters each.
    func textString(_ str: String?) -> String {
        guard let str = str, !str.isEmpty else {
            return ""
        }
        var bytes: [UInt8] = [0xFE, 0xFF]
        for unit in str.utf16 {
            bytes.append(UInt8(unit >> 8))
            bytes.append(UInt8(unit & 0xFF))
        }
        return toHex(encrypted(bytes))
    }

    /// Records the first misuse of the API, which complete() then throws.
    func fail(_ message: String) {
        if error == nil {
            error = message
        }
    }

    func newObj() {
        objOffset.append(byteCount)
        append(objOffset.count)
        append(Token.newObj)
    }

    func endObj() {
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

    // Returns the offset as the 10 digits of an entry of the cross-reference
    // table, which cannot hold an offset of more than 10 digits.
    static func xrefOffset(_ offset: Int) throws -> String {
        let digits = String(offset)
        if digits.count > 10 {
            throw PDFjetError(message: "The PDF is too large for a cross-reference table: "
                    + "an object starts at byte \(offset).")
        }
        return String(repeating: "0", count: 10 - digits.count) + digits
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
                sb.append(escapeXML(title!))
                sb.append("</rdf:li></rdf:Alt></dc:title>\n")
            }

            if author != nil {
                sb.append("  <dc:creator><rdf:Seq><rdf:li>")
                sb.append(escapeXML(author!))
                sb.append("</rdf:li></rdf:Seq></dc:creator>\n")
            }

            if subject != nil {
                sb.append("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">")
                sb.append(escapeXML(subject!))
                sb.append("</rdf:li></rdf:Alt></dc:description>\n")
            }

            if keywords != nil {
                sb.append("  <pdf:Keywords>")
                sb.append(escapeXML(keywords!))
                sb.append("</pdf:Keywords>\n")
            }

            if creator != nil {
                sb.append("  <xmp:CreatorTool>")
                sb.append(escapeXML(creator!))
                sb.append("</xmp:CreatorTool>\n")
            }

            sb.append("  <xmp:CreateDate>")
            sb.append(createDate!)
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

        // The metadata is encrypted like every other stream, and the
        // encryption dictionary says so with /EncryptMetadata true. Readers do
        // not agree on which metadata streams to leave alone when it is false.
        let buf = encrypted([UInt8](sb.utf8))

        // This is the metadata object
        newObj()
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
        endObj()

        return self.getObjNumber()
    }

    // Returns the text with the characters that have a meaning in XML escaped,
    // and without the characters XML does not allow: the control characters
    // other than tab, line feed and carriage return, U+FFFE and U+FFFF, which
    // would make the metadata unreadable.
    private func escapeXML(_ text: String) -> String {
        var result = String.UnicodeScalarView()
        for scalar in text.unicodeScalars {
            switch scalar {
            case "&":
                result.append(contentsOf: "&amp;".unicodeScalars)
            case "<":
                result.append(contentsOf: "&lt;".unicodeScalars)
            case ">":
                result.append(contentsOf: "&gt;".unicodeScalars)
            case "\t", "\n", "\r":
                result.append(scalar)
            default:
                if (scalar.value >= 0x20 && scalar.value <= 0xFFFD) || scalar.value >= 0x10000 {
                    result.append(scalar)
                }
            }
        }
        return String(result)
    }

    func addOutputIntentObject() -> Int {
        newObj()
        append(Token.beginDictionary)
        append("/N 3\n")

        let profile = encrypted(ICCBlackScaled.profile)
        append(Token.length)
        append(profile.count)
        append(Token.newline)

        append("/Filter /FlateDecode\n")
        append(Token.endDictionary)
        append(Token.stream)
        append(profile, 0, profile.count)
        append(Token.endStream)
        endObj()

        let identifier = toHexString("sRGB IEC61966-2.1")
        // OutputIntent object
        newObj()
        append(Token.beginDictionary)
        append("/Type /OutputIntent\n")
        append("/S /GTS_PDFA1\n")

        append("/OutputCondition <")
        append(identifier)
        append(">\n")

        append("/OutputConditionIdentifier <")
        append(identifier)
        append(">\n")

        append("/Info <")
        append(identifier)
        append(">\n")

        append("/DestOutputProfile ")
        append(getObjNumber() - 1)
        append(Token.objRef)
        append(Token.endDictionary)
        endObj()

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
        newObj()
        append(Token.beginDictionary)
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
        // The graphics states of a PDF that was read and those of the pages
        // go in the same dictionary.
        if states.count > 0 || importedExtGStates.count > 0 {
            append("/ExtGState <<\n")
            appendImportedEntries(importedExtGStates)
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
        endObj()
        return getObjNumber()
    }

    private func addPagesObject() {
        setObjOffset(pagesObjNumber, byteCount)
        append(pagesObjNumber)
        append(Token.newObj)
        append(Token.beginDictionary)
        append("/Type /Pages\n")
        append("/Kids [\n")
        for page in pages {
            append(page.objNumber)
            append(Token.objRef)
        }
        append("]\n")
        append("/Count ")
        append(pages.count)
        append(Token.newline)
        append(Token.endDictionary)
        endObj()
    }

    private func addStructTreeRootObject() -> Int {
        setObjOffset(structTreeRootNumber, byteCount)
        append(structTreeRootNumber)
        append(" 0 obj\n")
        append(Token.beginDictionary)
        append("/Type /StructTreeRoot\n")
        append("/ParentTree ")
        append(parentTreeNumber)
        append(Token.objRef)
        append("/K [\n")
        append(documentElementNumber)
        append(Token.objRef)
        append("]\n")
        append(Token.endDictionary)
        endObj()
        return structTreeRootNumber
    }

    // Reserves the numbers of the structure tree root, the parent tree and
    // the Document element, which the elements written with their pages refer
    // to before the three are written.
    func reserveStructTreeNumbers() {
        if structTreeRootNumber == 0 {
            structTreeRootNumber = reserveObjNumber()
            parentTreeNumber = reserveObjNumber()
            documentElementNumber = reserveObjNumber()
        }
    }

    @discardableResult
    private func addStructDocumentObject(_ parent: Int) -> Int {
        setObjOffset(documentElementNumber, byteCount)
        append(documentElementNumber)
        append(" 0 obj\n")
        append(Token.beginDictionary)
        append("/Type /StructElem\n")
        append("/S /Document\n")
        append("/P ")
        append(parent)
        append(" 0 R\n")
        append("/K [\n")
        for number in self.documentKids {
            append(number)
            append(Token.objRef)
        }
        append("]\n")
        append(Token.endDictionary)
        endObj()
        return documentElementNumber
    }

    // Writes the structure elements of a page that is written, so that a
    // document of many pages holds no more of them than the page it is
    // drawing. What it keeps is the elements that are still open and the ones
    // of an annotation, whose object is written when the document is completed.
    private func addPageStructElements(_ page: Page) {
        if compliance == Compliance.PDF_1_7 || page.structures.isEmpty {
            return
        }
        var kept = [StructElement]()
        for element in page.structures {
            if element.mcid >= 0 {
                while page.mcidNumbers.count <= element.mcid {
                    page.mcidNumbers.append(0)
                }
                page.mcidNumbers[element.mcid] = element.objNumber ?? 0
            }
            if element.parent == nil {
                documentKids.append(element.objNumber ?? 0)
            }
            if element.annotation != nil {
                annotElements.append(element)
            }
            if element.open || element.annotation != nil {
                kept.append(element)
                continue
            }
            addStructElementObject(element)
        }
        page.structures = kept
    }

    // Writes one structure element, under the number it was given when it was
    // made.
    private func addStructElementObject(_ element: StructElement) {
        do {
            setObjOffset(element.objNumber ?? 0, byteCount)
            append(element.objNumber ?? 0)
            append(" 0 obj\n")
            append("<<\n/Type /StructElem /S /")
            append(element.structure!)
            append("\n/P ")
            if let parentObjNumber = element.parent?.objNumber {
                append(parentObjNumber)
            } else {
                append(documentElementNumber)
            }
            append(" 0 R /Pg ")
            // The elements get the number of their page when the pages object
            // is written, which addObjects replaces. Fall back to 0 rather
            // than trapping, which is what the other ports do.
            append(element.pageObjNumber ?? 0)
            append(Token.objRef)

            if element.annotation != nil {
                append("/K <</Type /OBJR /Obj ")
                append(element.annotation!.objNumber)
                append(" 0 R>>\n")
            } else if element.mcid >= 0 {
                append("/K ")
                append(element.mcid)
                append("\n")
            } else if !element.kids.isEmpty {
                append("/K [")
                for kid in element.kids {
                    append(kid)
                    append(" 0 R ")
                }
                append("]\n")
            }

            if let attributes = element.attributes {
                append("/A ")
                append(attributes)
                append("\n")
            }

            // The actual text is written only with an alternate description,
            // since a text block and a text box pass the text they draw as the
            // actual text without one.
            let altDescription = element.altDescription ?? ""
            let actualText = altDescription.isEmpty ? "" : element.actualText ?? ""
            var language = element.language ?? ""
            if language.isEmpty && !altDescription.isEmpty {
                language = self.language
            }

            if !language.isEmpty {
                append("/Lang <")
                append(toHexString(language))
                append(">\n")
            }

            if !actualText.isEmpty {
                append("/ActualText <")
                append(textString(actualText))
                append(">\n")
            }

            if !altDescription.isEmpty {
                append("/Alt <")
                append(textString(altDescription))
                append(">\n")
            }

            append(">>\n")
            endObj()
        }
    }

    private func addNumsParentTree() {
        var buffer = String()

        setObjOffset(parentTreeNumber, byteCount)
        buffer.append(String(parentTreeNumber))
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
            for number in page.mcidNumbers {
                buffer.append(" ")
                buffer.append(String(number))
                buffer.append(" 0 R")
            }
            buffer.append("]\n")
            page.mcidNumbers = [Int]()
        }
        // The annotations follow, keyed by the /StructParent values handed out
        // by addAnnotDictionaries(), which continue where the pages left off.
        var index = pages.count
        for element in self.annotElements {
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

    /// Adds the document information dictionary, which readers like pdfinfo
    /// show. It says what the XMP metadata of PDF/A and PDF/UA documents says.
    private func addInfoObject() -> Int {
        newObj()
        append(Token.beginDictionary)
        appendInfoText("/Title", title)
        appendInfoText("/Author", author)
        appendInfoText("/Subject", subject)
        appendInfoText("/Keywords", keywords)
        appendInfoText("/Creator", creator)
        appendInfoText("/Producer", producer)
        // The XMP creation date 2026-01-31T12:00:00Z is D:20260131120000Z.
        let date = "D:" + createDate!
                .replacingOccurrences(of: "-", with: "")
                .replacingOccurrences(of: "T", with: "")
                .replacingOccurrences(of: ":", with: "")
        appendInfoString("/CreationDate", Array(date.utf8))
        append(Token.endDictionary)
        endObj()
        return getObjNumber()
    }

    /// Appends an entry of the information dictionary with the text, unless
    /// the text is nil.
    private func appendInfoText(_ key: String, _ text: String?) {
        guard let text = text else {
            return
        }
        append(key)
        append(" <")
        append(textString(text))
        append(">\n")
    }

    /// Appends an entry of the information dictionary with the bytes of a
    /// string, encrypted if the document is encrypted.
    private func appendInfoString(_ key: String, _ bytes: [UInt8]) {
        append(key)
        append(" <")
        append(toHex(encrypted(bytes)))
        append(">\n")
    }

    private func addRootObject(
            _ structTreeRootObjNumber: Int,
            _ outlineDictNumber: Int) -> Int {
        // Add the root object
        newObj()
        append(Token.beginDictionary)
        append("/Type /Catalog\n")
        if compliance != Compliance.PDF_1_7 {
            append("/Lang <")
            append(toHexString(language))
            append(">\n")

            append("/StructTreeRoot ")
            append(structTreeRootObjNumber)
            append(Token.objRef)

            append("/MarkInfo <</Marked true>>\n")
            append("/ViewerPreferences <</DisplayDocTitle true>>\n")
        }

        if pageLayout != nil {
            append("/PageLayout /")
            append(pageLayout!.rawValue)
            append(Token.newline)
        }

        if pageMode != nil {
            append("/PageMode /")
            append(pageMode!.rawValue)
            append(Token.newline)
        }

        if !groups.isEmpty {
            addOCProperties()
        }

        append("/Pages ")
        append(pagesObjNumber)
        append(Token.objRef)

        if compliance != Compliance.PDF_1_7 {
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
        endObj()
        return getObjNumber()
    }

    private func addPageBox(_ boxName: String, _ page: Page, _ rect: [Float]) {
        append("/")
        append(boxName)
        append(" [")
        append(rect[0])
        append(Token.space)
        append(page.height - rect[3])
        append(Token.space)
        append(rect[2])
        append(Token.space)
        append(page.height - rect[1])
        append("]\n")
    }

    // Gives every destination the object number of the page it is on, which
    // the page was given when it was added.
    private func setDestinationObjNumbers() {
        for page in pages {
            for destination in page.destinations {
                destination.pageObjNumber = page.objNumber
                destinations[destination.name!] = destination
            }
        }
    }

    private func addAllPages(_ resObjNumber: Int) {
        setDestinationObjNumbers()
        addAnnotDictionaries()
        pagesObjNumber = reserveObjNumber()
        for (i, page) in pages.enumerated() {
            if let mergedDict = page.mergedDict {
                var dict = mergedDict
                PDF.setEntry(&dict, "/Parent", [String(pagesObjNumber), "0", "R"])
                setObjOffset(page.objNumber, byteCount)
                append(page.objNumber)
                append(Token.newObj)
                appendTokens(dict)
                append(Token.newline)
                append(Token.endObj)
                continue
            }
            // Page object, under the number it was given when it was added.
            setObjOffset(page.objNumber, byteCount)
            append(page.objNumber)
            append(Token.newObj)
            append(Token.beginDictionary)
            append("/Type /Page\n")
            append("/Parent ")
            append(pagesObjNumber)
            append(Token.objRef)
            append("/MediaBox [0 0 ")
            append(page.width)
            append(Token.space)
            append(page.height)
            append("]\n")

            if page.rotateDegrees != 0.0 {
                append("/Rotate ")
                append(page.rotateDegrees)
                append("\n")
            }

            if let cropBox = page.cropBox {
                addPageBox("CropBox", page, cropBox)
            }
            if let bleedBox = page.bleedBox {
                addPageBox("BleedBox", page, bleedBox)
            }
            if let trimBox = page.trimBox {
                addPageBox("TrimBox", page, trimBox)
            }
            if let artBox = page.artBox {
                addPageBox("ArtBox", page, artBox)
            }

            append("/Resources ")
            append(resObjNumber)
            append(Token.objRef)

            append("/Contents [ ")
            for n in page.contents {
                append(n)
                append(" 0 R ")
            }
            append("]\n")
            if page.annots.count > 0 {
                append("/Annots [ ")
                for annot in page.annots {
                    append(annot.objNumber)
                    append(" 0 R ")
                }
                append("]\n")
            }

            if compliance != Compliance.PDF_1_7 {
                append("/Tabs /S\n")
                append("/StructParents ")
                append(i)
                append("\n")
            }

            append(Token.endDictionary)
            endObj()
        }
    }

    private func addPageContent(_ page: Page) {
        page.checkBalanced()
        if contentStreamsCompression {
            var buffer = [UInt8]()
            FlateEncode(&buffer, page.buf)
            page.buf.removeAll()   // Release the page content memory!
            page.written = true
            buffer = encrypted(buffer)

            newObj()
            append(Token.beginDictionary)
            append("/Filter /FlateDecode\n")
            append(Token.length)
            append(buffer.count)
            append(Token.newline)
            append(Token.endDictionary)
            append(Token.stream)
            append(buffer)
            append(Token.endStream)
            endObj()
            page.contents.append(getObjNumber())
        } else {    // No compression. Used for diagnostics
            let buffer = encrypted(page.buf)
            page.buf.removeAll()   // Release the page content memory!
            page.written = true

            newObj()
            append(Token.beginDictionary)
            append(Token.length)
            append(buffer.count)
            append(Token.newline)
            append(Token.endDictionary)
            append(Token.stream)
            append(buffer)
            append(Token.endStream)
            endObj()
            page.contents.append(getObjNumber())
        }
        addPageStructElements(page)
    }

    @discardableResult
    func addAnnotationObject(_ annot: Annotation, _ index: Int) -> Int {
        var index = index

        newObj()
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

            let title = textString(annot.fileAttachment!.title)
            if !title.isEmpty {
                append("/T <")
                append(title)
                append(">\n")
            }
            let contents = textString(annot.fileAttachment!.contents)
            if !contents.isEmpty {
                append("/Contents <")
                append(contents)
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
                append(textString(description))
                append(">\n")
            }
            if let uri = annot.uri {
                append("/F 4\n")
                append("/A <<\n")
                append("/S /URI\n")
                append("/URI <")
                append(toHexString(uri))
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
            append(annot.opacity)
            append("\n")

            if let title = annot.title, !title.isEmpty {
                append("/T <")
                append(textString(title))
                append(">\n")
            }

            if let contents = annot.contents, !contents.isEmpty {
                append("/Contents <")
                append(textString(contents))
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
            append(annot.opacity)
            append("\n")

            if let title = annot.title, !title.isEmpty {
                append("/T <")
                append(textString(title))
                append(">\n")
            }

            if let contents = annot.contents, !contents.isEmpty {
                append("/Contents <")
                append(textString(contents))
                append(">\n")
            }
        } else if annot.annotationType == Annotation.Text {
            append("/Name /Comment\n")
            if let title = annot.title, !title.isEmpty {
                append("/T <")
                append(textString(title))
                append(">\n")
            }

            if let contents = annot.contents, !contents.isEmpty {
                append("/Contents <")
                append(textString(contents))
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
        endObj()

        return index
    }

    private func addAnnotDictionaries() {
        var index = self.pages.count
        for element in self.annotElements {
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

        // The groups hidden by default, for the viewers that read the
        // configuration and not the usage of each group
        var off = String()
        for ocg in self.groups where !ocg.visible {
            off.append(" ")
            off.append(String(ocg.objNumber))
            off.append(" 0 R")
        }
        if !off.isEmpty {
            append("/OFF [")
            append(off)
            append(" ]\n")
        }

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
        if completed {
            fail("The PDF was already completed.")
            return
        }
        if page.pdf !== self {
            fail("The page belongs to another PDF.")
            return
        }
        if page.added {
            fail("The page was already added to the PDF.")
            return
        }
        page.added = true
        if page.objNumber == 0 {
            page.objNumber = reserveObjNumber()
        }
        // A page that was drawn before it was added has elements of its own.
        if compliance != Compliance.PDF_1_7 {
            page.setStructElementsPageObjNumber(page.objNumber)
        }
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
    /// - Throws: an error if the PDF cannot be written to the output stream.
    ///
    public func complete() throws {
        if completed {
            let message = "complete() was already called."
            fail(message)
            throw PDFjetError(message: message)
        }
        if let error = error {
            throw PDFjetError(message: "The PDF was not completed because of an earlier error: " + error)
        }
        if pages.isEmpty && pagesObjNumber == 0 {
            let message = "A PDF needs at least one page."
            fail(message)
            throw PDFjetError(message: message)
        }
        if prevPage != nil {
            addPageContent(prevPage!)
        }
        if let error = error {  // Found when the last page was written.
            throw PDFjetError(message: error)
        }
        completed = true
        if compliance != Compliance.PDF_1_7 {
            metadataObjNumber = addMetadataObject("", false)
            outputIntentObjNumber = addOutputIntentObject()
        }

        if pagesObjNumber == 0 {
            addAllPages(addResourcesObject())
            addPagesObject()
        }

        var structTreeRootObjNumber = 0
        if compliance != Compliance.PDF_1_7 {
            // The elements of every page are written with it; the ones still
            // open and the ones of the annotations are what is left.
            for page in pages {
                for element in page.structures {
                    addStructElementObject(element)
                }
                page.structures = [StructElement]()
            }
            reserveStructTreeNumbers()
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
                addOutlineItem(outlineDictNum, list[i])
                i += 1
            }
        }

        let infoObjNumber = addInfoObject()
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
            buffer.append(try PDF.xrefOffset(offset))
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

        if let encryption = encryption {
            append("/Encrypt ")
            append(encryption.getObjNumber())
            append(Token.objRef)
        }

        append("/Info ")
        append(infoObjNumber)
        append(Token.objRef)

        append("/Root ")
        append(rootObjNumber)
        append(Token.objRef)

        append(Token.endDictionary)
        append("startxref\n")
        append(startxref)
        append(Token.newline)
        append("%%EOF\n")

        if let error = error {  // Found when the rest of the document was written.
            throw PDFjetError(message: error)
        }
        try os!.close()
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
    // An empty document property is the same as one that was never set: it is
    // not written, as in the Go port, which cannot tell the two apart.
    @discardableResult
    public func setTitle(_ title: String) -> PDF {
        self.title = title.isEmpty ? nil : title
        return self
    }

    ///
    /// Set the "Author" document property of the PDF file.
    /// - Parameter author: The author of this document.
    ///
    @discardableResult
    public func setAuthor(_ author: String) -> PDF {
        self.author = author.isEmpty ? nil : author
        return self
    }

    ///
    /// Set the "Subject" document property of the PDF file.
    /// - Parameter subject: The subject of this document.
    ///
    @discardableResult
    public func setSubject(_ subject: String) -> PDF {
        self.subject = subject.isEmpty ? nil : subject
        return self
    }

    ///
    /// Set the "Keywords" document property of the PDF file.
    /// - Parameter keywords: The keywords of this document.
    ///
    @discardableResult
    public func setKeywords(_ keywords: String) -> PDF {
        self.keywords = keywords.isEmpty ? nil : keywords
        return self
    }

    ///
    /// Set the "Creator" document property of the PDF file.
    /// - Parameter creator: The creator of this document.
    ///
    @discardableResult
    public func setCreator(_ creator: String) -> PDF {
        self.creator = creator.isEmpty ? nil : creator
        return self
    }

    /// Sets the page layout used when the document is opened. See PageLayout.
    @discardableResult
    public func setPageLayout(_ pageLayout: PageLayout) -> PDF {
        self.pageLayout = pageLayout
        return self
    }

    /// Sets the page mode used when the document is opened. See PageMode.
    @discardableResult
    public func setPageMode(_ pageMode: PageMode) -> PDF {
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
        if !FastFloat.isWritable(val) {
            fail(FastFloat.NOT_WRITABLE)
        }
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

    // Returns the objects by their number, with an empty object at the number
    // of every one the PDF does not have, so that the object a reference names
    // is the one at its number. Every object of a PDF takes bytes of its file,
    // so a number larger than the file has bytes is one no PDF can hold, and
    // the empty objects up to it would take the memory a file of a few bytes
    // never names.
    func getSortedObjects(_ objects: [PDFobj], _ size: Int) throws -> [PDFobj] {
        var sorted = [PDFobj]()
        var maxObjNumber = 0
        for obj in objects {
            if obj.number > maxObjNumber {
                maxObjNumber = obj.number
            }
        }
        if maxObjNumber > size {
            throw PDFjetError(message: "The PDF of \(size) bytes cannot hold "
                    + "an object numbered \(maxObjNumber).")
        }
        for number in stride(from: 1, through: maxObjNumber, by: 1) {
            let obj = PDFobj()
            obj.setNumber(number)
            sorted.append(obj)
        }
        for obj in objects where obj.number > 0 {
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
        return try read(from: stream, password: "")
    }

    ///
    /// Returns a list of objects of type PDFobj read from input stream, which
    /// holds a PDF that is encrypted with the standard security handler. The
    /// PDF is decrypted with the password, which is its user or its owner
    /// password.
    ///
    /// - Parameter stream: the PDF input stream.
    /// - Parameter password: the user or owner password of the PDF.
    ///
    /// - Returns: [PDFobj] the list of PDF objects.
    /// - Throws: DecryptorError when the password is not correct.
    ///
    public func read(from stream: InputStream, password: String) throws -> [PDFobj] {
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
        let decryptor = try Decryptor.getDecryptor(trailer, objects1, password)

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
                try obj.setStreamAndData(&buffer1, try obj.getLength(objects1), decryptor)
            }
            if type == "/ObjStm" {
                // A malformed object stream is an error, as in the other
                // ports, and not a reason to stop the program.
                let first = try objectStreamNumber(obj.getValue("/First"))
                let o2 = getObject(obj.data, 0, min(first, obj.data.count))
                var i = 0
                while i + 1 < o2.dict.count {
                    let num = o2.dict[i]
                    let off = try objectStreamNumber(o2.dict[i + 1])
                    var end = obj.data.count
                    if i <= o2.dict.count - 4 {
                        end = min(first + (try objectStreamNumber(o2.dict[i + 3])), obj.data.count)
                    }
                    let o3 = getObject(obj.data, first + off, end)
                    o3.number = try objectStreamNumber(num)
                    o3.dict.insert(contentsOf: [num, "0", "obj"], at: 0)
                    objects2.append(o3)
                    i += 2
                }
            } else {
                objects2.append(obj)
            }
        }
        return try getSortedObjects(objects2, buffer1.count)
    }

    // Returns the number in the header of an object stream.
    private func objectStreamNumber(_ token: String) throws -> Int {
        let number = toInteger(token)
        if number < 0 {
            throw PDFjetError(message: "The object stream of the PDF is malformed: \"\(token)\" is not a number.")
        }
        return number
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
            // The keyword ends with CRLF or LF, and the tokenizer consumed the
            // first of those bytes, so only the LF of a CRLF is left to skip.
            // A data byte that is a line feed, like the first byte of the IV
            // of an encrypted stream, stays.
            obj.streamOffset = offset
            if offset > 0 && buffer[offset - 1] == 0x0D && offset < buffer.count && buffer[offset] == 0x0A {
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

    /// Adds the outline dictionary for the bookmarks and returns its object
    /// number. The bookmarks were numbered by toArrayList(), level by level,
    /// and the object of a bookmark is that many objects after the outline
    /// dictionary.
    func addOutlineDict(_ toc: Bookmark) -> Int {
        newObj()
        append(Token.beginDictionary)
        append("/Type /Outlines\n")
        append("/First ")
        append(getObjNumber() + toc.getFirstChild()!.objNumber)
        append(" 0 R\n")
        append("/Last ")
        append(getObjNumber() + toc.getLastChild()!.objNumber)
        append(" 0 R\n")
        // The items that are visible: those of the first level, as the items
        // with children are closed.
        append("/Count ")
        append(toc.getChildren()!.count)
        append(Token.newline)
        append(Token.endDictionary)
        endObj()
        return getObjNumber()
    }

    /// Adds an outline item for the bookmark, under the outline dictionary
    /// with the given object number.
    func addOutlineItem(_ outlines: Int, _ bm1: Bookmark) {
        var prev = 0
        if let bookmark = bm1.getPrevBookmark() {
            prev = outlines + bookmark.objNumber
        }
        var next = 0
        if let bookmark = bm1.getNextBookmark() {
            next = outlines + bookmark.objNumber
        }

        var first = 0
        var last = 0
        var count = 0
        if let children = bm1.getChildren(), children.count > 0 {
            first = outlines + children[0].objNumber
            last  = outlines + children[children.count - 1].objNumber
            // A closed item: the items that opening it would show.
            count = (-1) * children.count
        }

        newObj()
        append(Token.beginDictionary)
        append("/Title <")
        append(textString(bm1.getTitle()))
        append(">\n")
        // The root of the bookmarks is the outline dictionary itself.
        append("/Parent ")
        append(outlines + (bm1.getParent()?.objNumber ?? 0))
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
        append("/Dest [")
        append(bm1.getDestination()!.pageObjNumber)
        append(" 0 R /XYZ ")
        append(bm1.getDestination()!.xPosition)
        append(Token.space)
        append(bm1.getDestination()!.yPosition)
        append(" 0]\n")
        append(Token.endDictionary)
        endObj()
    }

    ///
    /// Adds all the pages of a document that was read with read(from:) after
    /// the pages of this document, in their order. A PDF can merge several
    /// documents and draw pages of its own before, between and after them.
    ///
    /// The merged pages keep their content, resources, annotations and links.
    /// The parts of the read document that belong to the whole document are
    /// left out: its bookmarks, form fields, tagging, named destinations and
    /// optional content settings. The objects that the pages use are written
    /// at once, so the objects are not needed after the call.
    ///
    /// A PDF/UA or PDF/A document cannot merge pages, which were not made for
    /// its compliance, and merge cannot be used with addObjects.
    ///
    /// - Parameter objects: the objects of the document, as read(from:) returns them.
    /// - Throws: PDFjetError when the pages cannot be merged into this document.
    ///
    public func merge(_ objects: [PDFobj]) throws {
        try checkMerge(objects)
        mergePages(objects, getPageObjects(from: objects))
    }

    ///
    /// Adds the listed pages of a document that was read with read(from:)
    /// after the pages of this document, in the order they are listed. A
    /// document is split by merging each part of it into a PDF of its own: the
    /// objects that read(from:) returned can be merged into any number of PDFs.
    ///
    /// The pages are merged as merge(objects) merges all of them, and a link to
    /// a page that is not merged leads nowhere. A page number that the document
    /// does not have, or one that is listed twice, is refused.
    ///
    /// - Parameter objects: the objects of the document, as read(from:) returns them.
    /// - Parameter pageNumbers: the numbers of the pages, counted from 1.
    /// - Throws: PDFjetError when the pages cannot be merged into this document.
    ///
    public func merge(_ objects: [PDFobj], _ pageNumbers: [Int]) throws {
        try checkMerge(objects)
        let pageObjects = getPageObjects(from: objects)
        var listed = [PDFobj]()
        var seen = Set<Int>()
        for number in pageNumbers {
            if number < 1 || number > pageObjects.count {
                try refuse("The document has no page \(number).")
            }
            if !seen.insert(number).inserted {
                try refuse("Page \(number) is listed twice.")
            }
            listed.append(pageObjects[number - 1])
        }
        mergePages(objects, listed)
    }

    // Refuses a merge that would break this document.
    private func checkMerge(_ objects: [PDFobj]) throws {
        if completed {
            try refuse("The PDF was already completed.")
        }
        if compliance != Compliance.PDF_1_7 {
            try refuse("Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document.")
        }
        if pagesObjNumber != 0 {
            try refuse("merge and addObjects cannot be used on the same PDF.")
        }
        if getPagesObject(objects) == nil {
            try refuse("The objects have no root /Pages object.")
        }
    }

    // Adds the pages, in their order, and every object that they use.
    private func mergePages(_ objects: [PDFobj], _ pageObjects: [PDFobj]) {
        var mergedPages = Set<Int>()
        for page in pageObjects {
            mergedPages.insert(page.number)
        }

        // Each page and every object that it uses, found through the
        // references, gets a number of this document before anything is
        // written, as the objects refer to each other: a page to its
        // annotations, and a link annotation to the page it points at.
        var numbers = [Int: Int]()
        var values = [Int: [String]]()
        var queue = [PDFobj]()
        for page in pageObjects {
            if numbers[page.number] == nil {
                numbers[page.number] = reserveObjNumber()
                queue.append(page)
            }
        }
        var i = 0
        while i < queue.count {
            let obj = queue[i]
            let value = PDF.mergedValue(obj, mergedPages.contains(obj.number), objects)
            values[obj.number] = value
            var j = 0
            while j < value.count {
                if PDF.isReference(value, j) {
                    let number = Int(value[j])!
                    if numbers[number] == nil && isMergedObject(number, objects, mergedPages) {
                        numbers[number] = reserveObjNumber()
                        queue.append(objects[number - 1])
                    }
                    j += 2
                }
                j += 1
            }
            i += 1
        }

        for obj in queue where !mergedPages.contains(obj.number) {
            addMergedObject(obj, numbers[obj.number]!, renumbered(values[obj.number]!, numbers))
        }
        for obj in queue where mergedPages.contains(obj.number) {
            pages.append(Page(self, numbers[obj.number]!, renumbered(values[obj.number]!, numbers)))
        }
    }

    // Records the misuse and throws it.
    private func refuse(_ message: String) throws {
        fail(message)
        throw PDFjetError(message: message)
    }

    // The entries of a page that it can inherit from the page tree.
    private static let inheritedKeys = ["/Resources", "/MediaBox", "/CropBox", "/Rotate"]

    // Reserves the next object number for an object written later.
    func reserveObjNumber() -> Int {
        objOffset.append(0)
        return objOffset.count
    }

    // Returns true when the tokens at index i are a reference: "n g R".
    private static func isReference(_ tokens: [String], _ i: Int) -> Bool {
        return i + 2 < tokens.count
                && tokens[i + 2] == "R"
                && isObjectNumber(tokens[i])
                && isObjectNumber(tokens[i + 1])
    }

    private static func isObjectNumber(_ token: String) -> Bool {
        if token.isEmpty || token.unicodeScalars.count > 9 {
            return false
        }
        for scalar in token.unicodeScalars {
            if scalar.value < 0x30 || scalar.value > 0x39 {
                return false
            }
        }
        return true
    }

    // Returns true for an object that the merged pages can use: not the page
    // tree, the catalog, a page that is not merged or an object that is missing.
    private func isMergedObject(_ number: Int, _ objects: [PDFobj], _ mergedPages: Set<Int>) -> Bool {
        if number < 1 || number > objects.count {
            return false
        }
        let obj = objects[number - 1]
        if obj.dict.isEmpty {
            return false
        }
        let type = obj.getValue("/Type")
        if type == "/Pages" || type == "/Catalog" {
            return false
        }
        return !isPageObject(obj) || mergedPages.contains(number)
    }

    // Returns the value of an object that was read, without its "n g obj" and
    // its "stream" and "endobj" keywords, with a direct /Length for a stream,
    // and for a page with the entries it inherits and without its /Parent.
    private static func mergedValue(_ obj: PDFobj, _ isPage: Bool, _ objects: [PDFobj]) -> [String] {
        var value = valueOf(obj)
        if let stream = obj.stream {
            setEntry(&value, "/Length", [String(stream.count)])
        }
        if isPage {
            for key in inheritedKeys {
                if entryIndex(value, key) == -1 {
                    var inherited = inheritedValue(obj, key, objects)
                    if inherited == nil && key == "/MediaBox" {
                        inherited = ["[", "0", "0", "612", "792", "]"]  // Letter
                    }
                    if let inherited = inherited {
                        setEntry(&value, key, inherited)
                    }
                }
            }
            removeEntry(&value, "/Parent")
        }
        return value
    }

    private static func valueOf(_ obj: PDFobj) -> [String] {
        let dict = obj.dict
        let start = (dict.count >= 3 && dict[2] == "obj") ? 3 : 0
        var end = dict.count
        if end > start && dict[end - 1] == "endobj" {
            end -= 1
        }
        if end > start && dict[end - 1] == "stream" {
            end -= 1
        }
        return Array(dict[start..<end])
    }

    // Returns the value of the entry from the nearest node of the page tree
    // above the page that has it, or nil.
    private static func inheritedValue(_ page: PDFobj, _ key: String, _ objects: [PDFobj]) -> [String]? {
        var node = page
        for _ in 0..<64 {   // A loop in a broken tree ends here.
            let tokens = valueOf(node)
            let i = entryIndex(tokens, "/Parent")
            if i == -1 || !isReference(tokens, i + 1) {
                return nil
            }
            let number = Int(tokens[i + 1])!
            if number < 1 || number > objects.count || objects[number - 1].dict.isEmpty {
                return nil
            }
            node = objects[number - 1]
            let parent = valueOf(node)
            let k = entryIndex(parent, key)
            if k != -1 {
                return Array(parent[(k + 1)..<valueEnd(parent, k + 1)])
            }
        }
        return nil
    }

    // Returns the index after the value that starts at index i.
    private static func valueEnd(_ tokens: [String], _ i: Int) -> Int {
        if i >= tokens.count {
            return tokens.count
        }
        let token = tokens[i]
        if token == "<<" || token == "[" {
            var depth = 0
            for j in i..<tokens.count {
                let t = tokens[j]
                if t == "<<" || t == "[" {
                    depth += 1
                } else if t == ">>" || t == "]" {
                    depth -= 1
                    if depth == 0 {
                        return j + 1
                    }
                }
            }
            return tokens.count
        }
        return isReference(tokens, i) ? i + 3 : i + 1
    }

    // Returns the index of the key of an entry of the dictionary, not of a
    // dictionary inside it, or -1.
    private static func entryIndex(_ tokens: [String], _ key: String) -> Int {
        if tokens.isEmpty || tokens[0] != "<<" {
            return -1
        }
        var i = 1
        while i < tokens.count && tokens[i] != ">>" {
            if tokens[i] == key {
                return i
            }
            i = valueEnd(tokens, i + 1)
        }
        return -1
    }

    // Sets the value of an entry of the dictionary, adding the entry at its end.
    private static func setEntry(_ tokens: inout [String], _ key: String, _ value: [String]) {
        if tokens.isEmpty || tokens[0] != "<<" {
            return
        }
        let i = entryIndex(tokens, key)
        if i != -1 {
            tokens.replaceSubrange((i + 1)..<valueEnd(tokens, i + 1), with: value)
        } else {
            let end = valueEnd(tokens, 0) - 1     // The index of the closing >>
            tokens.insert(contentsOf: [key] + value, at: end)
        }
    }

    private static func removeEntry(_ tokens: inout [String], _ key: String) {
        let i = entryIndex(tokens, key)
        if i != -1 {
            tokens.removeSubrange(i..<valueEnd(tokens, i + 1))
        }
    }

    // Returns the tokens with the references renumbered for this document, a
    // reference to an object that is not merged replaced with null, and the
    // strings encrypted when this document is encrypted.
    private func renumbered(_ tokens: [String], _ numbers: [Int: Int]) -> [String] {
        var result = [String]()
        result.reserveCapacity(tokens.count)
        var i = 0
        while i < tokens.count {
            let token = tokens[i]
            if PDF.isReference(tokens, i) {
                if let number = numbers[Int(token)!] {
                    result.append(String(number))
                    result.append("0")
                    result.append("R")
                } else {
                    result.append("null")
                }
                i += 3
                continue
            }
            if encryption != nil && (token.hasPrefix("(") || (token.hasPrefix("<") && token != "<<")) {
                // Lowercase hexadecimal digits, as Java writes them.
                result.append("<" + toHex(encrypted(Decryptor.toBytes(token))).lowercased() + ">")
            } else {
                result.append(token)
            }
            i += 1
        }
        return result
    }

    private func addMergedObject(_ obj: PDFobj, _ number: Int, _ value: [String]) {
        var value = value
        var stream = obj.stream
        if stream != nil && encryption != nil {
            stream = encrypted(stream!)
            PDF.setEntry(&value, "/Length", [String(stream!.count)])
        }
        setObjOffset(number, byteCount)
        append(number)
        append(Token.newObj)
        appendTokens(value)
        append(Token.newline)
        if let stream = stream {
            append(Token.stream)
            append(stream)
            append(Token.endStream)
        }
        append(Token.endObj)
    }

    private func appendTokens(_ tokens: [String]) {
        for (i, token) in tokens.enumerated() {
            if i > 0 {
                append(Token.space)
            }
            appendToken(token)
        }
    }

    ///
    /// Adds objects read from an existing PDF to this document. The objects
    /// keep their numbers and are written as they are, so they are added
    /// before any font, image or page of this document, and an encrypted PDF
    /// cannot take them.
    ///
    /// - Throws: PDFjetError when the objects cannot be added to this document.
    ///
    public func addObjects(_ objects: [PDFobj]) throws {
        for page in pages where page.mergedDict != nil {
            try refuse("merge and addObjects cannot be used on the same PDF.")
        }
        guard let pagesObject = getPagesObject(objects),
                let number = Int(pagesObject.dict.first ?? "") else {
            try refuse("The objects have no root /Pages object.")
            return
        }
        if let message = checkObjects(objects) {
            try refuse(message)
        }
        self.pagesObjNumber = number
        addObjectsToPDF(objects)
    }

    // Returns why the objects of a PDF that was read cannot be written, or nil.
    // They keep their numbers and are written as they are, so they have to come
    // before the objects that this document numbers itself, and an encrypted
    // document cannot take them.
    private func checkObjects(_ objects: [PDFobj]) -> String? {
        if completed {
            return "The PDF was already completed."
        }
        if encryption != nil {
            return "The objects of an existing PDF cannot be added to an encrypted PDF."
        }
        for obj in objects {
            if obj.number > 0 && obj.number <= objOffset.count && objOffset[obj.number - 1] != 0 {
                return "Add the objects of an existing PDF before fonts, images or pages "
                        + "are added to the PDF: object \(obj.number) is already written."
            }
        }
        return nil
    }

    /// Returns the root pages object.
    func getPagesObject(
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
        if let pagesObject = getPagesObject(objects) {
            var visited = Set<Int>()
            getPageObjects(pagesObject, &pageObjects, objects, &visited)
        }
        return pageObjects
    }

    // The nodes of the page tree that were visited are skipped, as a node of a
    // broken tree can list itself or a node above it as a kid.
    private func getPageObjects(
            _ pdfObj: PDFobj,
            _ pages: inout [PDFobj],
            _ objects: [PDFobj],
            _ visited: inout Set<Int>) {
        if !visited.insert(pdfObj.number).inserted {
            return
        }
        let kids = pdfObj.getObjectNumbers("/Kids")
        for number in kids {
            if number < 1 || number > objects.count {
                continue        // A kid that the document does not have.
            }
            let object = objects[number - 1]
            if isPageObject(object) {
                pages.append(object)
            } else {
                getPageObjects(object, &pages, objects, &visited)
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

    // Adds the entries of the /ExtGState dictionary of the resources, which can
    // be an object of its own, with the names that an earlier page did not add.
    private func addExtGStates(_ resources: PDFobj, _ objects: [PDFobj]) {
        let entries = getResourceEntries(resources, "/ExtGState", objects)
        var i = 0
        while i < entries.count {
            // The value after the name is a dictionary, a reference or one token.
            var end = i + 1
            if end < entries.count && entries[end] == "<<" {
                var level = 0
                repeat {
                    let token = entries[end]
                    end += 1
                    if token == "<<" {
                        level += 1
                    } else if token == ">>" {
                        level -= 1
                    }
                } while level > 0 && end < entries.count
            } else if end + 2 < entries.count && entries[end + 2] == "R" {
                end += 3
            } else {
                end = min(end + 1, entries.count)
            }
            if !importedExtGStates.contains(entries[i]) {
                importedExtGStates.append(contentsOf: entries[i..<end])
            }
            i = end
        }
    }

    private func getFontObjects(
            _ resources: PDFobj,
            _ objects: [PDFobj]) -> [PDFobj] {
        var fonts = [PDFobj]()
        // The /Font dictionary holds one "/Name number 0 R" entry per font, and
        // can be an object of its own. Every entry is written to the resources
        // object, so every font it names is collected here.
        let entries = getResourceEntries(resources, "/Font", objects)
        var i = 0
        while i < entries.count {
            let token = entries[i]
            if token.hasPrefix("/") && (i + 3) < entries.count && entries[i + 3] == "R" {
                // Pages can carry separate resource dictionaries that name the
                // same fonts. They are merged into one /Font dictionary here,
                // so a name that is already present must not be added twice.
                if !importedFonts.contains(token) {
                    importedFonts.append(contentsOf: entries[i..<i + 4])
                    let number = toInteger(entries[i + 1])
                    if number > 0 && number <= objects.count {
                        fonts.append(objects[number - 1])
                    }
                }
                i += 4
                continue
            }
            importedFonts.append(token)
            i += 1
        }
        return fonts
    }

    ///
    /// Returns the entries of a sub-dictionary of the resources, like /XObject,
    /// without the brackets around them. The sub-dictionary can also be an
    /// object of its own.
    ///
    private func getResourceEntries(
            _ resources: PDFobj,
            _ name: String,
            _ objects: [PDFobj]) -> [String] {
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
            _ objects: [PDFobj],
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
            addObjectTree(reference, objects, &numbers, &resources)
        }
    }

    ///
    /// Collects the images and forms in the /XObject resources, with the
    /// objects they use, and adds their names to the resources object.
    ///
    private func addXObjects(
            _ resObj: PDFobj,
            _ objects: [PDFobj],
            _ numbers: inout Set<Int>,
            _ resources: inout [PDFobj]) {
        let entries = getResourceEntries(resObj, "/XObject", objects)
        var i = 0
        while i < entries.count {
            let token = entries[i]
            if token.hasPrefix("/") && (i + 3) < entries.count && entries[i + 3] == "R" {
                // Like the fonts, a name that an earlier page added is kept.
                if !importedXObjects.contains(token) {
                    importedXObjects.append(contentsOf: entries[i..<i + 4])
                    if let number = Int(entries[i + 1]) {
                        addObjectTree(number, objects, &numbers, &resources)
                    }
                }
                i += 4
            } else {
                i += 1
            }
        }
    }

    ///
    /// Adds the fonts, images and graphics states used by the pages to this
    /// document. The objects keep their numbers and are written as they are,
    /// so they are added before any font, image or page of this document, and
    /// an encrypted PDF cannot take them: the mistake is recorded, and
    /// complete() throws it.
    ///
    public func addResourceObjects(from objects: [PDFobj]) {
        if let message = checkObjects(objects) {
            fail(message)
            return
        }
        var resources = [PDFobj]()
        var numbers = Set<Int>()
        let pages = getPageObjects(from: objects)
        for page in pages {
            guard let resObj = page.getResourcesObject(objects) else {
                continue        // A page without resources of its own.
            }
            // A font is copied with every object that it refers to: its
            // descriptor and font file, and also the widths, the encoding and
            // the other entries that can be objects of their own.
            for font in getFontObjects(resObj, objects) {
                addObjectTree(font.number, objects, &numbers, &resources)
            }
            addXObjects(resObj, objects, &numbers, &resources)
            addExtGStates(resObj, objects)
            // The /ExtGState entries are copied as they are, so the objects
            // that they refer to have to be copied too.
            for number in getReferences(getResourceEntries(resObj, "/ExtGState", objects)) {
                addObjectTree(number, objects, &numbers, &resources)
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
        addObjectsToPDF(unique)
    }

    private func addObjectsToPDF(_ objects: [PDFobj]) {
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
    func toHex(_ str: String?) -> String {
        guard let str = str, !str.isEmpty else {
            return ""
        }
        // Java hex encodes the UTF-8 bytes of the string, so a character
        // outside ASCII must be written as its UTF-8 byte sequence - not as
        // its code point - or a PDF reader decodes it as the wrong character.
        return toHex(Array(str.utf8))
    }

    /// Returns the bytes as uppercase hexadecimal digits.
    func toHex(_ bytes: [UInt8]) -> String {
        var result: [UInt8] = []
        result.reserveCapacity(2 * bytes.count)
        for byte in bytes {
            result.append(HEX[Int((byte >> 4) & 0xF)])
            result.append(HEX[Int(byte        & 0xF)])
        }
        return String(decoding: result, as: UTF8.self)
    }

}   // End of PDF.swift
