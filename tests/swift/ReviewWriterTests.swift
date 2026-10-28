/**
 * ReviewWriterTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The tests of the writer: the catalog, the structure tree, the annotations,
/// the metadata and the files a document carries.
@Suite struct ReviewWriterTests {
    /// Writes a document of the compliance with a heading on its one page,
    /// drawn with draw, and returns it. The heading is in a core font, which
    /// the tests of what a document of PDF/A or PDF/UA tags find by its
    /// encoding: complete() refuses one otherwise.
    private func document(_ compliance: Compliance, _ draw: ((PDF, Page) throws -> Void)? = nil) throws -> String {
        let memory = MemoryPDF(compliance)
        memory.pdf.coreFontsInCompliance = true
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(memory.pdf), "Heading")
                .setStructureType(StructElem.H1).setLocation(50, 50).drawOn(page)
        try draw?(memory.pdf, page)
        try memory.pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    private func count(_ text: String, _ part: String) -> Int {
        return text.components(separatedBy: part).count - 1
    }

    /// Returns the message complete() throws, or "" when it completes the PDF.
    private func completeMessage(_ pdf: PDF) -> String {
        do {
            try pdf.complete()
            return ""
        } catch {
            return TestSupport.message(error)
        }
    }

    @Test func aPDFAOfLevelBIsNotTagged() throws {
        for compliance in [Compliance.PDF_A_1B, Compliance.PDF_A_2B, Compliance.PDF_A_3B] {
            let raw = try document(compliance)
            for entry in ["/StructTreeRoot", "/MarkInfo", "/Tabs /S", "/StructParents", "/StructElem"] {
                #expect(!raw.contains(entry), "\(compliance) has \(entry)")
            }
            for entry in ["/Lang <", "/DisplayDocTitle true"] {
                #expect(raw.contains(entry), "\(compliance) has no \(entry)")
            }
        }
        for compliance in [Compliance.PDF_A_1A, Compliance.PDF_A_2A, PDF.complianceA3A,
                Compliance.PDF_UA_1, Compliance.PDF_A_3A_UA_1] {
            let raw = try document(compliance)
            for entry in ["/StructTreeRoot", "/MarkInfo <</Marked true>>", "/Tabs /S", "/StructParents 0"] {
                #expect(raw.contains(entry), "\(compliance) has no \(entry)")
            }
        }
    }

    @Test func theNoticeOfAFontIsEscapedInItsMetadata() throws {
        let memory = MemoryPDF()
        _ = memory.pdf.addMetadataObject("Copyright A & B <c>", true)
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.contains("Copyright A &amp; B &lt;c&gt;"), "\(raw)")

        var objects = [PDFobj]()
        let font = TestSupport.helvetica(TestSupport.newPDF())
        font.info = "Copyright A & B"
        let number = FontObjects.addMetadataObject(&objects, font)
        let xml = String(decoding: objects[number - 1].stream ?? [], as: UTF8.self)
        #expect(xml.contains("Copyright A &amp; B"), "\(xml)")
    }

    /// Draws a square, a circle, a polygon, a note and a file.
    private func annotations(_ pdf: PDF, _ page: Page) throws {
        let square = SquareAnnotation()
        square.setLocation(100, 100)
        square.setSize(50, 50)
        square.setContents("A square")
        _ = square.drawOn(page)
        let circle = CircleAnnotation()
        circle.setLocation(200, 100)
        circle.setSize(80, 40)
        circle.setContents("A circle")
        _ = circle.drawOn(page)
        let polygon = PolygonAnnotation().setVertices([0, 0, 50, 0, 25, 40])
        polygon.setLocation(300, 300)
        polygon.setContents("A polygon")
        _ = polygon.drawOn(page)
        let note = TextAnnotation()
        note.setLocation(100, 400)
        note.setSize(20, 20)
        note.setContents("A note")
        _ = note.drawOn(page)
        // A file attached to a page, which PDF/A refuses: in a PDF/A document
        // the four others alone
        if pdf.isPDFA() {
            return
        }
        let file = try EmbeddedFile(pdf, "a.txt", InputStream(data: Data("A file".utf8)), false)
        let attachment = FileAttachment(file)
        attachment.setLocation(200, 400)
        attachment.setContents("A file")
        attachment.drawOn(page)
    }

    @Test func everyAnnotationIsPrintedAndHasAnAppearance() throws {
        for compliance in [Compliance.PDF_1_7, Compliance.PDF_A_2B, PDF.complianceA3A] {
            let raw = try document(compliance, annotations)
            let want = compliance == Compliance.PDF_1_7 ? 5 : 4    // No file attached in PDF/A
            #expect(count(raw, "/Type /Annot\n") == want, "\(compliance)")
            #expect(count(raw, "/F 4\n") == want, "\(compliance)")
            #expect(count(raw, "/AP <</N ") == want, "\(compliance)")
            #expect(count(raw, "/Subtype /Form\n") == want, "\(compliance)")
        }
        // The square is drawn in its fill color in its box, which is its rectangle.
        let raw = try document(Compliance.PDF_1_7, annotations)
        #expect(raw.contains("/BBox [100 642 150 692]\n/Length 34\n>>\nstream\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n"),
                "\(raw)")
        // The note and the file are drawn as their icons, scaled to their boxes.
        #expect(raw.contains("q\n20 0 0 20 100 372 cm\n" + Annotation.noteIcon + "Q\n"))
        #expect(raw.contains("q\n24 0 0 24 200 368 cm\n" + Annotation.pushPinIcon + "Q\n"))
    }

    @Test func theRectangleOfAnAnnotationIsFromItsLowerLeftCorner() throws {
        let raw = try document(Compliance.PDF_1_7) { pdf, page in
            try annotations(pdf, page)
            TextLine(TestSupport.helvetica(pdf), "Link").setURIAction("https://pdfjet.com")
                    .setLocation(50, 100).drawOn(page)
        }
        for rect in ["/Rect [100 642 150 692]", "/Rect [200 652 280 692]", "/Rect [300 452 350 492]",
                "/Rect [100 372 120 392]", "/Rect [200 368 224 392]"] {
            #expect(raw.contains(rect), "\(rect)")
        }
        let regex = try NSRegularExpression(pattern: "/Rect \\[(\\S+) (\\S+) (\\S+) (\\S+)\\]")
        let matches = regex.matches(in: raw, range: NSRange(raw.startIndex..., in: raw))
        #expect(matches.count == 6)
        for match in matches {
            let v = (1...4).map { Float(raw[Range(match.range(at: $0), in: raw)!])! }
            #expect(v[0] <= v[2] && v[1] <= v[3], "\(v)")
        }
    }

    @Test func aShapeThatIsNotOpaqueIsDrawnWithItsOpacity() throws {
        let raw = try document(Compliance.PDF_1_7) { _, page in
            let square = SquareAnnotation()
            square.setLocation(100, 100)
            square.setSize(50, 50)
            square.setOpacity(0.5)
            square.setContents("A square")
            _ = square.drawOn(page)
        }
        #expect(raw.contains("/BBox [100 642 150 692]\n/Resources <</ExtGState <</GS0 <</CA 0.5 /ca 0.5>>>>>>\n" +
                "/Length 42\n>>\nstream\n/GS0 gs\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n"), "\(raw)")
    }

    @Test func aPDFA1MapsTheElementOfAnAnnotationToASpan() throws {
        // PDF/A-1, of PDF 1.4, does not know the type Annot, of PDF 1.5: it is
        // role-mapped there (veraPDF, PDF/A-1 6.8.3.4), and not in PDF/A-2.
        for level in [Compliance.PDF_A_1A, Compliance.PDF_A_2A] {
            let raw = try document(level) { _, page in
                let square = SquareAnnotation()
                square.setLocation(100, 100)
                square.setSize(50, 50)
                square.setContents("A square")
                _ = square.drawOn(page)
            }
            #expect(raw.contains("/Annot /Span") == (level == Compliance.PDF_A_1A), "\(level)")
        }
    }

    @Test func aPDFA1DrawsAShapeThatIsNotOpaqueOpaque() throws {
        // PDF/A-1 has no transparency: the opacity is ignored there, and kept
        // in the other levels.
        for level in [Compliance.PDF_A_1B, Compliance.PDF_A_1A, Compliance.PDF_A_2B, Compliance.PDF_1_7] {
            let raw = try document(level) { _, page in
                let square = SquareAnnotation()
                square.setLocation(100, 100)
                square.setSize(50, 50)
                square.setOpacity(0.5)
                square.setContents("A square")
                _ = square.drawOn(page)
            }
            let transparent = level != Compliance.PDF_A_1B && level != Compliance.PDF_A_1A
            for entry in ["/CA ", "/ExtGState", "/GS0 gs"] {
                #expect(raw.contains(entry) == transparent, "\(level): \(entry)")
            }
            if !transparent {
                #expect(raw.contains("/Length 34\n>>\nstream\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n"), "\(raw)")
            }
        }
    }

    @Test func aLinkToADestinationTheDocumentDoesNotHaveIsRefused() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(pdf), "Nowhere").setGoToAction("missing").setLocation(50, 100).drawOn(page)
        #expect(completeMessage(pdf) == "The link goes to the destination missing, which the document does not have.")
    }

    @Test func theTypesOfPDF20AreMappedInTheRoleMap() throws {
        #expect(!(try document(Compliance.PDF_UA_1)).contains("/RoleMap"))
        let raw = try document(Compliance.PDF_UA_1) { pdf, page in
            let font = TestSupport.helvetica(pdf)
            TextLine(font, "Strong").setStructureType(StructElem.STRONG).setLocation(50, 100).drawOn(page)
            TextLine(font, "Title").setStructureType(StructElem.TITLE).setLocation(50, 120).drawOn(page)
        }
        #expect(raw.contains("/RoleMap << /Title /P /Strong /Span >>\n"), "\(raw)")
    }

    @Test func aTextLineOfTheTypeArtifactIsAnArtifact() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(memory.pdf), "Header")
                .setStructureType(StructElem.ARTIFACT).setLocation(50, 50).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("/Artifact BMC\n"), "\(content)")
        #expect(!content.contains("/Artifact <<"), "\(content)")
        #expect(page.structures.isEmpty)

        let pdf2 = MemoryPDF(Compliance.PDF_UA_1).pdf
        let page2 = Page(pdf2, Letter.PORTRAIT)
        page2.beginStructElement(StructElem.ARTIFACT)
        #expect(pdf2.error == "An artifact is not a structure element: draw it between addArtifactBMC and addEMC.")
    }

    @Test func anAnnotationOnAWrittenPageOfATaggedDocumentIsRefused() {
        let pdf = MemoryPDF(Compliance.PDF_UA_1).pdf
        pdf.setTitle("Test")
        let page1 = Page(pdf, Letter.PORTRAIT)
        _ = Page(pdf, Letter.PORTRAIT)
        let square = SquareAnnotation()
        square.setContents("Late")
        _ = square.drawOn(page1)
        #expect(pdf.error == "The page was already written to the PDF: "
                + "draw on a page before creating the next page or completing the PDF.")
        #expect(page1.annots.isEmpty)
    }

    @Test func aPDFUADocumentNeedsATitle() {
        for compliance in [Compliance.PDF_UA_1, Compliance.PDF_A_3A_UA_1] {
            let pdf = MemoryPDF(compliance).pdf
            pdf.setTitle(" ")
            _ = Page(pdf, Letter.PORTRAIT)
            #expect(completeMessage(pdf) == "A PDF/UA document needs a title: use setTitle.")
        }
    }

    @Test func anAnnotationOfATaggedDocumentNeedsADescription() throws {
        let pdf = MemoryPDF(Compliance.PDF_UA_1).pdf
        pdf.setTitle("Test")
        let page = Page(pdf, Letter.PORTRAIT)
        _ = SquareAnnotation().drawOn(page)
        #expect(pdf.error == "An annotation of a tagged document, PDF/UA or PDF/A of level A, "
                + "needs contents, a title or an alternative description.")
        // One that is not tagged needs none.
        let pdf2 = TestSupport.newPDF()
        _ = SquareAnnotation().drawOn(Page(pdf2, Letter.PORTRAIT))
        try pdf2.complete()
    }

    @Test func theInformationSaysWhatTheMetadataSays() throws {
        let memory = MemoryPDF(Compliance.PDF_A_2B)
        memory.pdf.setTitle("A\u{1}B\u{FFFE}C\u{FFFF}D\tE").setAuthor("F\u{2}G")
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        let raw = String(decoding: memory.bytes, as: UTF8.self)
        #expect(raw.contains("<rdf:li xml:lang=\"x-default\">ABCD\tE</rdf:li>"), "\(raw)")
        #expect(raw.contains("/Title <" + memory.pdf.textString("ABCD\tE") + ">\n"), "\(raw)")
        #expect(raw.contains("<rdf:li>FG</rdf:li>"), "\(raw)")
        #expect(raw.contains("/Author <" + memory.pdf.textString("FG") + ">\n"), "\(raw)")
    }

    @Test func anEmptyTextIsTheByteOrderMarkAlone() {
        #expect(TestSupport.newPDF().textString("") == "feff")
    }

    @Test func theDateOfAnEmbeddedFileIsEncrypted() throws {
        let memory = MemoryPDF()
        _ = memory.pdf.setEncryption(Encryption(memory.pdf, Passwords(), Permissions()))
        let date = memory.pdf.getDate()
        _ = try EmbeddedFile(memory.pdf, "a.xml", InputStream(data: Data("<a/>".utf8)),
                false, "text/xml", Relationship.DATA, nil)
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(!raw.contains(date), "the date is not encrypted")
        #expect(!raw.contains(memory.pdf.toHex(Array(date.utf8))), "the date is not encrypted")
        #expect(raw.contains("/ModDate <"), "\(raw)")
    }

    @Test func completeClosesTheStreamWhenItFails() {
        let stream = OutputStream(toMemory: ())
        let pdf = PDF(stream)
        #expect(completeMessage(pdf) == "A PDF needs at least one page.")
        #expect(stream.streamStatus == .closed, "the stream is open")
    }

    @Test func theLayersAreOrderedByTheirUTF16Names() throws {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        var groups = [OptionalContentGroup]()
        for name in ["\u{E000}", "b", "a", "\u{1F600}", "a"] {
            let group = OptionalContentGroup(memory.pdf, name)
            _ = group.add(Rect(10, 10, 20, 20))
            group.drawOn(page)
            groups.append(group)
        }
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        var order = "/Order ["
        for i in [2, 4, 1, 3, 0] {
            order += " \(groups[i].objNumber) 0 R "
        }
        #expect(raw.contains(order + "]\n"), "\(raw)")
    }

    @Test func aPDFA3FileNeedsAMediaType() throws {
        for compliance in [PDF.complianceA3A, Compliance.PDF_A_3B, Compliance.PDF_A_3A_UA_1] {
            let pdf = MemoryPDF(compliance).pdf
            pdf.addAssociatedFile(try EmbeddedFile(pdf, "a.xml", InputStream(data: Data("<a/>".utf8)),
                    false, nil, Relationship.DATA, "Data."))
            #expect(pdf.error == "The file a.xml was embedded without a media type, "
                    + "which a file of a document of PDF/A-3 needs.")
        }
        let pdf = TestSupport.newPDF()
        pdf.addAssociatedFile(try EmbeddedFile(pdf, "a.xml", InputStream(data: Data("<a/>".utf8)),
                false, nil, Relationship.DATA, "Data."))
        #expect(pdf.error == nil)
    }

    @Test func theProducerIsTheVersionOfTheLibrary() throws {
        let memory = MemoryPDF()
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("/Producer <" + memory.pdf.textString("PDFjet v9.1.5") + ">"))
    }

    @Test func aNumberIsWrittenWithItsSign() throws {
        let memory = MemoryPDF()
        for number in [0, 7, -3, 1234567890, Int.min] {
            memory.pdf.append(number)
            memory.pdf.append(" ")
        }
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("0 7 -3 1234567890 \(Int.min) "))
    }

    @Test func theHexadecimalOfATextIsItsUTF16() {
        #expect(Page.toUTF16Hex("Az\u{e9}\u{1F600}") == "FEFF0041007A00E9D83DDE00")
    }

    @Test func theFontFileRefersToItsMetadataWhoseNoticeIsEscaped() throws {
        let memory = MemoryPDF(Compliance.PDF_A_2B)
        let font = try Font(memory.pdf, TestSupport.path("fonts/NotoSansJP/NotoSansJP-Regular.ttf"))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        _ = TextLine(font, "Hello").setLocation(50, 50).drawOn(page)
        try memory.pdf.complete()
        let raw = String(decoding: memory.bytes, as: UTF8.self)
        let fontFile = try NSRegularExpression(pattern:
                "\\d+ 0 obj\n<<\n/Filter /FlateDecode\n/Length1 \\d+\n/Metadata \\d+ 0 R\n/Length \\d+\n>>\nstream\n")
        #expect(fontFile.firstMatch(in: raw, range: NSRange(raw.startIndex..., in: raw)) != nil,
                "the font file does not refer to its metadata")
        #expect(raw.contains("<xmpRights:UsageTerms>") && raw.contains(" &amp; "), "the notice is not escaped")
    }

    @Test func theLanguageOfAWordInAParagraphIsNotEncryptedTwice() throws {
        // The /Lang of a word in a paragraph's marked content was encrypted as
        // a string, and the content stream encrypted again, as a whole (the
        // review of 9 October 2026): the hexadecimal of its UTF-8 bytes, as in
        // the other ports.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.coreFontsInCompliance = true    // Draws in a core font
        memory.pdf.setTitle("Test")
        _ = memory.pdf.setEncryption(Encryption(memory.pdf, Passwords(), Permissions()))
        let font = TestSupport.helvetica(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        CompositeTextLine(50, 50)
                .addComponent(TextLine(font, "Hello "))
                .addComponent(TextLine(font, "bonjour").setLanguage("fr"))
                .drawOn(page)
        #expect(TestSupport.content(page).contains("/Lang <6672>"), "\(TestSupport.content(page))")
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes, "")
        let content = TestSupport.latin1(try #require(TestSupport.pageObjects(objects).first?
                .getContentObject(objects)).getData())
        #expect(content.contains("/Lang <6672>"), "\(content)")
    }

    /// A document of PDF/A or PDF/UA refuses a font that is not embedded, and
    /// one of PDF/A a file attached to a page or embedded but not as an
    /// associated file of PDF/A-3: each was written, and the document was not
    /// what it said (review of 11 October 2026, #5 and #6).
    @Test func aCompliantDocumentRefusesWhatItCannotBe() throws {
        func complete(_ level: Compliance, _ draw: (PDF, Page) throws -> Void) throws -> String {
            let memory = MemoryPDF(level)
            memory.pdf.setTitle("Test")
            let page = Page(memory.pdf, Letter.PORTRAIT)
            try draw(memory.pdf, page)
            return completeMessage(memory.pdf)
        }
        func text(_ font: @escaping (PDF) throws -> Font) -> (PDF, Page) throws -> Void {
            return { pdf, page in
                TextLine(try font(pdf), "Text").setLocation(50, 50).drawOn(page)
            }
        }
        let helvetica: (PDF) throws -> Font = { TestSupport.helvetica($0) }
        let plexSans: (PDF) throws -> Font = { try TestSupport.plexSans($0) }
        for level in [Compliance.PDF_UA_1, Compliance.PDF_A_1B, PDF.complianceA3A] {
            let message = try complete(level, text(helvetica))
            #expect(message.contains("The font Helvetica is not embedded"), "\(level): Helvetica: \(message)")
            #expect(try complete(level, text(plexSans)) == "", "\(level): IBM Plex Sans")
        }
        let attach: (PDF, Page) throws -> Void = { pdf, page in
            try text(plexSans)(pdf, page)
            let file = try EmbeddedFile(pdf, "a.txt", InputStream(data: Data("A file".utf8)), false)
            FileAttachment(file).drawOn(page)
        }
        let embed: (PDF, Page) throws -> Void = { pdf, page in
            try text(plexSans)(pdf, page)
            _ = try EmbeddedFile(pdf, "a.txt", InputStream(data: Data("A file".utf8)), false)
        }
        for level in [Compliance.PDF_A_1B, Compliance.PDF_A_2B, Compliance.PDF_A_3B] {
            let attached = try complete(level, attach)
            #expect(attached.contains("cannot have a file attached to a page"), "\(level): a file attached: \(attached)")
            let embedded = try complete(level, embed)
            #expect(embedded.contains("but as an associated file"), "\(level): a file embedded: \(embedded)")
        }
        // Outside PDF/A a file is attached as before
        #expect(try complete(Compliance.PDF_UA_1, attach) == "", "PDF/UA: a file attached")
        // A file of PDF/A-3 attached to a page is taken when the document
        // carries it as an associated file too (review of 12 October 2026, R24)
        let associated: (PDF, Page) throws -> Void = { pdf, page in
            try text(plexSans)(pdf, page)
            let file = try EmbeddedFile(pdf, "data.xml", InputStream(data: Data("<a/>".utf8)), false,
                    "text/xml", Relationship.DATA, "The data")
            pdf.addAssociatedFile(file)
            FileAttachment(file).drawOn(page)
        }
        #expect(try complete(Compliance.PDF_A_3B, associated) == "", "PDF/A-3: an associated file attached")
        // An associated file of another PDF is refused, as an attached one is
        // (review of 12 October 2026, R11)
        let foreign = try EmbeddedFile(TestSupport.newPDF(), "data.xml", InputStream(data: Data("<a/>".utf8)), false,
                "text/xml", Relationship.DATA, "The data")
        let pdf = MemoryPDF(Compliance.PDF_A_3B).pdf
        pdf.addAssociatedFile(foreign)
        #expect(pdf.error == "The embedded file belongs to another PDF.")
    }

    /// Reads the PDF, encrypted with no user password, and returns the
    /// strings after the key in its objects, as a reader decrypts them.
    private func decryptedStrings(_ pdf: [UInt8], _ key: String) throws -> [String] {
        var values = [String]()
        for obj in try TestSupport.read(pdf, "") {
            var i = 0
            while i + 1 < obj.dict.count {
                if obj.dict[i] == key {
                    values.append(TestSupport.latin1(Decryptor.toBytes(obj.dict[i + 1])))
                }
                i += 1
            }
        }
        return values
    }

    /// Returns a document of the compliance, written to memory, encrypted
    /// with no passwords.
    private func encryptedDoc(_ compliance: Compliance) -> MemoryPDF {
        let memory = MemoryPDF(compliance)
        memory.pdf.setTitle("Test")
        _ = memory.pdf.setEncryption(Encryption(memory.pdf, Passwords(), Permissions()))
        return memory
    }

    /// The strings of the system of characters of a CJK font, Adobe and the
    /// ordering, are encrypted in an encrypted document: written as they
    /// were, a reader decrypted them to nothing, and the CMap matched no font
    /// (review of 11 October 2026, #19).
    @Test func theCharactersOfACJKFontAreEncrypted() throws {
        let memory = encryptedDoc(Compliance.PDF_1_7)
        let font = TestSupport.cjkFont(memory.pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR)
        TextLine(font, "\u{65E5}\u{672C}").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        #expect(try decryptedStrings(memory.bytes, "/Registry") == ["Adobe"])
        #expect(try decryptedStrings(memory.bytes, "/Ordering") == ["Japan1"])
    }

    /// The name of the configuration of the layers of a document of PDF/UA
    /// is encrypted in an encrypted document, where a reader decrypted it to
    /// nothing (review of 11 October 2026, #20).
    @Test func theNameOfTheLayersIsEncrypted() throws {
        let memory = encryptedDoc(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(try TestSupport.plexSans(memory.pdf), "Heading")
                .setStructureType(StructElem.H1).setLocation(50, 50).drawOn(page)
        let group = OptionalContentGroup(memory.pdf, "Layer").setVisible(true)
        _ = group.add(Rect(10, 100, 20, 20))
        group.drawOn(page)
        try memory.pdf.complete()
        #expect(try decryptedStrings(memory.bytes, "/Name").contains("Default"))
    }

    /// The XMP packet's header begins with the byte order mark, which ISO
    /// 16684-1 asks of it, in the document's metadata and a font's, written
    /// as UTF-8 once (review of 11 October 2026, #28).
    @Test func theXMPPacketHasItsBeginAttribute() throws {
        let raw = try document(Compliance.PDF_UA_1) { pdf, page in
            TextLine(try TestSupport.plexSans(pdf), "Text").setLocation(50, 100).drawOn(page)
        }
        // The bytes EF BB BF, read as Latin-1.
        let header = "<?xpacket begin=\"\u{EF}\u{BB}\u{BF}\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>"
        #expect(count(raw, header) == 2, "the document's packet and the font's")
        #expect(count(raw, "<?xpacket ") == 4, "a packet without a begin attribute")
    }
}
