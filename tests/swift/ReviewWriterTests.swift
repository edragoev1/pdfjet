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
    /// drawn with draw, and returns it.
    private func document(_ compliance: Compliance, _ draw: ((PDF, Page) throws -> Void)? = nil) throws -> String {
        let memory = MemoryPDF(compliance)
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
        let file = try EmbeddedFile(pdf, "a.txt", InputStream(data: Data("A file".utf8)), false)
        let attachment = FileAttachment(file)
        attachment.setLocation(200, 400)
        attachment.setContents("A file")
        attachment.drawOn(page)
    }

    @Test func everyAnnotationIsPrintedAndHasAnAppearance() throws {
        for compliance in [Compliance.PDF_1_7, Compliance.PDF_A_2B, PDF.complianceA3A] {
            let raw = try document(compliance, annotations)
            #expect(count(raw, "/Type /Annot\n") == 5, "\(compliance)")
            #expect(count(raw, "/F 4\n") == 5, "\(compliance)")
            #expect(count(raw, "/AP <</N ") == 5, "\(compliance)")
            #expect(count(raw, "/Subtype /Form\n") == 5, "\(compliance)")
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
        #expect(TestSupport.latin1(memory.bytes).contains("/Producer <" + memory.pdf.textString("PDFjet v9.0.3") + ">"))
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
}
