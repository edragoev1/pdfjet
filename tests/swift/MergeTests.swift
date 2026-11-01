/**
 * MergeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Merging the pages of documents that were read.
@Suite struct MergeTests {
    // Returns a document with one page for each text, drawn with Helvetica.
    private func document(_ texts: String...) throws -> [UInt8] {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        for text in texts {
            let page = Page(memory.pdf, Letter.PORTRAIT)
            TextLine(font, text).setLocation(50, 50).drawOn(page)
        }
        try memory.pdf.complete()
        return memory.bytes
    }

    // Returns the decoded content of each page, in the order of the pages.
    private func pageContents(_ objects: [PDFobj]) -> [String] {
        return PDF().getPageObjects(from: objects).map {
            TestSupport.latin1($0.getContentObject(objects)?.getData() ?? [])
        }
    }

    private func isDigits(_ token: String) -> Bool {
        return !token.isEmpty && token.unicodeScalars.allSatisfy { $0.value >= 0x30 && $0.value <= 0x39 }
    }

    // Checks that every reference of the document is to an object it has.
    private func expectReferencesResolve(_ objects: [PDFobj], sourceLocation: SourceLocation = #_sourceLocation) {
        for obj in objects {
            let dict = obj.dict
            var i = 0
            while i + 2 < dict.count {
                if dict[i + 2] == "R" && isDigits(dict[i]) && isDigits(dict[i + 1]) {
                    let number = Int(dict[i])!
                    let resolves = number >= 1 && number <= objects.count && !objects[number - 1].dict.isEmpty
                    #expect(resolves, "object \(obj.number) refers to the missing object \(number)",
                            sourceLocation: sourceLocation)
                }
                i += 1
            }
        }
    }

    private func expectMessage(
            _ message: String,
            sourceLocation: SourceLocation = #_sourceLocation,
            _ body: () throws -> Void) {
        do {
            try body()
            Issue.record("no error was thrown", sourceLocation: sourceLocation)
        } catch {
            #expect(TestSupport.message(error) == message, sourceLocation: sourceLocation)
        }
    }

    @Test func mergesDocumentsInTheirOrder() throws {
        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(document("A1", "A2")))
        try memory.pdf.merge(TestSupport.read(document("B1")))
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 3)
        #expect(contents[0].contains(TestSupport.hex("A1")))
        #expect(contents[1].contains(TestSupport.hex("A2")))
        #expect(contents[2].contains(TestSupport.hex("B1")))
        expectReferencesResolve(objects)
    }

    @Test func drawnPagesKeepTheirPlace() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        TextLine(font, "G1").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.merge(TestSupport.read(document("B1")))
        TextLine(font, "G2").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 3)
        #expect(contents[0].contains(TestSupport.hex("G1")))
        #expect(contents[1].contains(TestSupport.hex("B1")))
        #expect(contents[2].contains(TestSupport.hex("G2")))
        expectReferencesResolve(objects)
    }

    @Test func aPageOfSeveralContentStreamsHasAllOfThem() throws {
        let first = "BT /F1 24 Tf 20 300 Td (First) Tj ET"
        let second = "BT /F1 24 Tf 20 200 Td (Second) Tj ET"
        let source = "%PDF-1.4\n"
                + "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n"
                + "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 300 400] >> endobj\n"
                + "3 0 obj << /Type /Page /Parent 2 0 R /Contents [5 0 R 6 0 R]"
                + " /Resources << /Font << /F1 4 0 R >> >> >> endobj\n"
                + "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
                + "5 0 obj << /Length \(first.utf8.count) >>\nstream\n" + first + "\nendstream\nendobj\n"
                + "6 0 obj << /Length \(second.utf8.count) >>\nstream\n" + second + "\nendstream\nendobj\n"
                + "trailer << /Root 1 0 R >>\n%%EOF\n"
        let objects = try TestSupport.read(Array(source.utf8))
        let read = try #require(pageContents(objects).first)
        #expect(read.contains("(First) Tj") && read.contains("(Second) Tj"), "\(read)")

        let memory = MemoryPDF()
        try memory.pdf.merge(objects)
        try memory.pdf.complete()
        let merged = try #require(pageContents(try TestSupport.read(memory.bytes)).first)
        #expect(merged.contains("(First) Tj") && merged.contains("(Second) Tj"), "\(merged)")
    }

    @Test func aMergedPageInheritsFromThePageTree() throws {
        let content = "BT /F1 24 Tf 20 300 Td (Inherited) Tj ET"
        let source = "%PDF-1.4\n"
                + "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n"
                + "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 300 400] /Rotate 90"
                + " /Resources << /Font << /F1 4 0 R >> >> >> endobj\n"
                + "3 0 obj << /Type /Page /Parent 2 0 R /Contents 5 0 R >> endobj\n"
                + "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
                + "5 0 obj << /Length \(content.utf8.count) >>\nstream\n" + content + "\nendstream\nendobj\n"
                + "trailer << /Root 1 0 R >>\n%%EOF\n"
        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(Array(source.utf8)))
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        let pages = PDF().getPageObjects(from: objects)
        try #require(pages.count == 1)
        let page = pages[0]
        #expect(page.getPageSize().getWidth() == 300)
        #expect(page.getPageSize().getHeight() == 400)
        #expect(page.getValue("/Rotate") == "90")
        #expect(page.dict.contains("/F1"))
        let parent = try #require(page.getObjectNumbers("/Parent").first)
        #expect(objects[parent - 1].getValue("/Type") == "/Pages")
        #expect(pageContents(objects)[0].contains("(Inherited) Tj"))
        expectReferencesResolve(objects)
    }

    @Test func aReferenceWhoseNumberHasLeadingZerosIsMerged() throws {
        // "0000000003 0 R", as pdf.js tests it in issue10491: a number of ten
        // digits was not a reference, so the /Parent of the page left "0 R"
        // behind, which MuPDF cannot read, and the page lost its contents.
        let content = "BT /F1 24 Tf 20 300 Td (Zeros) Tj ET"
        let source = "%PDF-1.4\n"
                + "0000000001 0 obj << /Type /Catalog /Pages 0000000002 0 R >> endobj\n"
                + "0000000002 0 obj << /Type /Pages /Kids [0000000003 0 R] /Count 1 >> endobj\n"
                + "0000000003 0 obj << /Type /Page /Parent 0000000002 0 R /MediaBox [0 0 300 400]"
                + " /Resources << /Font << /F1 0000000004 0 R >> >> /Contents 0000000005 0 R >> endobj\n"
                + "0000000004 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
                + "0000000005 0 obj << /Length \(content.utf8.count) >>\nstream\n" + content + "\nendstream\nendobj\n"
                + "trailer << /Root 0000000001 0 R >>\n%%EOF\n"
        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(Array(source.utf8)))
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        let pages = PDF().getPageObjects(from: objects)
        try #require(pages.count >= 1)
        let page = pages[0]
        let value = PDF.valueOf(page)
        try #require(value.count >= 4)
        #expect(value[1..<4].joined(separator: " ") == "/Type /Page /MediaBox")
        #expect(page.getPageSize().getWidth() == 300)
        #expect(pageContents(objects).first?.contains("(Zeros) Tj") == true, "the content was not merged")
        expectReferencesResolve(objects)
    }

    @Test func linksPointAtTheMergedPages() throws {
        let source = MemoryPDF()
        let font1 = TestSupport.helvetica(source.pdf)
        let page1 = Page(source.pdf, Letter.PORTRAIT)
        TextLine(font1, "Go").setGoToAction("there").setLocation(50, 50).drawOn(page1)
        let page2 = Page(source.pdf, Letter.PORTRAIT)
        page2.addDestination("there", 30, 100)
        try source.pdf.complete()

        let memory = MemoryPDF()
        TextLine(TestSupport.helvetica(memory.pdf), "Cover").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.merge(TestSupport.read(source.bytes))
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        let pages = PDF().getPageObjects(from: objects)
        try #require(pages.count == 3)
        let link = try #require(objects.last(where: { $0.getValue("/Subtype") == "/Link" }))
        // The link on the second page points at the third page and is listed by the second.
        let dest = try #require(link.dict.firstIndex(of: "/Dest"))
        #expect(link.dict[dest + 1] == "[")
        #expect(link.dict[dest + 2] == String(pages[2].number))
        #expect(link.dict[dest + 4] == "R")
        #expect(pages[1].getObjectNumbers("/Annots").contains(link.number))
        expectReferencesResolve(objects)
    }

    @Test func mergesTheListedPagesInTheirOrder() throws {
        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(document("A1", "A2", "A3")), [3, 1])
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 2)
        #expect(contents[0].contains(TestSupport.hex("A3")))
        #expect(contents[1].contains(TestSupport.hex("A1")))
        expectReferencesResolve(objects)
    }

    @Test func splitsADocumentIntoOnePDFPerPage() throws {
        let source = try TestSupport.read(document("A1", "A2", "A3"))
        for i in 1...3 {
            let part = MemoryPDF()
            try part.pdf.merge(source, [i])
            try part.pdf.complete()
            let objects = try TestSupport.read(part.bytes)
            let contents = pageContents(objects)
            try #require(contents.count == 1)
            #expect(contents[0].contains(TestSupport.hex("A\(i)")))
            expectReferencesResolve(objects)
        }
    }

    @Test func aLinkToAPageThatIsNotMergedLeadsNowhere() throws {
        let source = MemoryPDF()
        let page1 = Page(source.pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(source.pdf), "Go").setGoToAction("there").setLocation(50, 50).drawOn(page1)
        let page2 = Page(source.pdf, Letter.PORTRAIT)
        page2.addDestination("there", 30, 100)
        try source.pdf.complete()

        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(source.bytes), [1])
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        #expect(PDF().getPageObjects(from: objects).count == 1)
        let link = try #require(objects.last(where: { $0.getValue("/Subtype") == "/Link" }))
        let dest = try #require(link.dict.firstIndex(of: "/Dest"))
        #expect(link.dict[dest + 1] == "[")
        #expect(link.dict[dest + 2] == "null")
        expectReferencesResolve(objects)
    }

    @Test func anEncryptedDocumentMergesThePagesEncrypted() throws {
        let memory = MemoryPDF()
        _ = memory.pdf.setEncryption(Encryption(memory.pdf, Passwords(), Permissions()))
        try memory.pdf.merge(TestSupport.read(document("Secret A", "Secret B")))
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("/Encrypt "))
        let objects = try TestSupport.read(memory.bytes, "")
        let contents = pageContents(objects)
        try #require(contents.count == 2)
        #expect(contents[0].contains(TestSupport.hex("Secret A")))
        #expect(contents[1].contains(TestSupport.hex("Secret B")))
        expectReferencesResolve(objects)
    }

    @Test func anEncryptedDocumentIsMergedDecrypted() throws {
        let source = MemoryPDF()
        _ = source.pdf.setEncryption(Encryption(source.pdf, Passwords(), Permissions()))
        TextLine(TestSupport.helvetica(source.pdf), "Plain").setLocation(50, 50).drawOn(Page(source.pdf, Letter.PORTRAIT))
        try source.pdf.complete()

        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(source.bytes, ""))
        try memory.pdf.complete()
        #expect(!TestSupport.latin1(memory.bytes).contains("/Encrypt "))
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 1)
        #expect(contents[0].contains(TestSupport.hex("Plain")))
        expectReferencesResolve(objects)
    }

    @Test func mergesTheTestDocuments() throws {
        let memory = MemoryPDF()
        for name in ["scanned-linearized.pdf", "sample-document.pdf", "PDFjetLogo.pdf"] {
            try memory.pdf.merge(PDF().read(from: TestSupport.open("resources/data/testPDFs/" + name)))
        }
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let pages = PDF().getPageObjects(from: objects)
        #expect(pages.count == 8)
        for page in pages {
            #expect(page.getContentObject(objects) != nil)
        }
        expectReferencesResolve(objects)
    }

    @Test func mergeIsRefusedWhereItWouldBreakTheDocument() throws {
        let objects = try TestSupport.read(document("A"))

        let ua = MemoryPDF(Compliance.PDF_UA_1)
        ua.pdf.setTitle("Test")
        expectMessage("Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document.") {
            try ua.pdf.merge(objects)
        }

        let completed = MemoryPDF()
        _ = Page(completed.pdf, Letter.PORTRAIT)
        try completed.pdf.complete()
        expectMessage("The PDF was already completed.") {
            try completed.pdf.merge(objects)
        }

        let rewritten = MemoryPDF()
        try rewritten.pdf.addObjects(TestSupport.read(document("B")))
        expectMessage("merge and addObjects cannot be used on the same PDF.") {
            try rewritten.pdf.merge(objects)
        }

        let merged = MemoryPDF()
        try merged.pdf.merge(objects)
        let other = try TestSupport.read(document("C"))
        expectMessage("merge and addObjects cannot be used on the same PDF.") {
            try merged.pdf.addObjects(other)
        }

        let empty = MemoryPDF()
        expectMessage("The objects have no root /Pages object.") {
            try empty.pdf.merge([PDFobj]())
        }

        let split = MemoryPDF()
        expectMessage("The document has no page 0.") {
            try split.pdf.merge(objects, [0])
        }
        expectMessage("The document has no page 2.") {
            try split.pdf.merge(objects, [2])
        }
        expectMessage("Page 1 is listed twice.") {
            try split.pdf.merge(objects, [1, 1])
        }
    }

    @Test func aFontAddedToTheObjectsIsWritten() throws {
        // A font added to the objects of a PDF, and drawn on one of their pages,
        // was left empty when the objects were merged rather than added (the
        // review of 9 October 2026), and its text was lost.
        var objects = try TestSupport.read(document("Existing"))
        let font = try Font(&objects, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let memory = MemoryPDF()
        let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
        page.addResource(font, &objects)
        page.drawString(font, nil, 12, "Hello", 72, 72)
        page.complete(&objects)
        try memory.pdf.merge(objects)
        try memory.pdf.complete()
        let merged = try TestSupport.read(memory.bytes)
        #expect(TestSupport.findObject(merged, "/Length1") != nil, "no font program")
        #expect(merged.contains {
            $0.getValue("/Subtype") == "/Type0" && $0.getValue("/BaseFont").hasSuffix("+IBMPlexSans")
        }, "no Type0 font of a subset of IBM Plex Sans")
        expectReferencesResolve(merged)
    }

    @Test func whatWasDrawnOnAPageThatWasReadIsMerged() throws {
        // The content streams that Page.complete adds have no dictionary, and
        // merge took them for missing: /Contents [ null 3 0 R null ], and both
        // the drawing and the text of the page were lost (review of 30 October
        // 2026, B1).
        var objects = try TestSupport.read(document("Existing"))
        let font = try Font(&objects, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let memory = MemoryPDF()
        let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
        page.addResource(font, &objects)
        page.drawString(font, nil, 12, "Hello", 72, 72)
        page.complete(&objects)
        try memory.pdf.merge(objects)
        try memory.pdf.complete()
        let merged = try TestSupport.read(memory.bytes)
        let pageObj = memory.pdf.getPageObjects(from: merged)[0]
        #expect(!pageObj.dict.contains("null"), "\(pageObj.dict)")
        var text = ""
        for obj in merged where obj.stream != nil {
            text += String(decoding: obj.getData(), as: UTF8.self)
        }
        #expect(text.contains(TestSupport.hex("Existing")), "the text of the page is lost")
        #expect(text.contains("72 720 Td"), "the drawing is lost")
    }

    // Returns a PDF of one page with the entries, a content stream as its
    // object 4, and the other objects after it.
    private func onePagePDF(_ entries: String, _ others: String...) -> [UInt8] {
        return TestSupport.pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R \(entries) >>",
            "<< /Length 25 >>\nstream\n0 0 1 rg 10 10 50 50 re f\nendstream"] + others)
    }

    // Returns the value of the name in the dictionary of the resources of the
    // first page of the PDF, the tokens of the object it refers to when it is
    // a reference, or nothing.
    private func resource(_ buf: [UInt8], _ category: String, _ name: String) throws -> [String] {
        let objects = try TestSupport.read(buf)
        let page = MemoryPDF().pdf.getPageObjects(from: objects)[0]
        var dict = page.getResourcesObject(objects)!.dict
        guard let i = dict.firstIndex(of: category) else {
            return []
        }
        if PDF.isReference(dict, i + 1) {
            dict = objects[Int(dict[i + 1])! - 1].dict
        } else {
            dict = Array(dict[i...])
        }
        guard let k = dict.firstIndex(of: name) else {
            return []
        }
        if PDF.isReference(dict, k + 1) {
            return objects[Int(dict[k + 1])! - 1].dict
        }
        return Array(dict[(k + 1)...])
    }

    @Test func aGraphicsStateOnAPageThatWasReadIsInItsResources() throws {
        // The content named a /GS that the resources of the page did not have,
        // so the alpha was lost; the name has to be one the page does not use
        // (review of 30 October 2026, B2).
        for merge in [true, false] {
            var objects = try TestSupport.read(
                    onePagePDF("/Resources << /ExtGState << /GS1 << /ca 0.1 >> >> >>"))
            let memory = MemoryPDF()
            let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
            let state = GraphicsState()
            state.setAlphaNonStroking(0.5)
            page.setGraphicsState(state)
            page.fillRect(300, 300, 100, 100)
            page.complete(&objects)
            if merge {
                try memory.pdf.merge(objects)
            } else {
                try memory.pdf.addObjects(objects)
            }
            try memory.pdf.complete()
            let value = try resource(memory.bytes, "/ExtGState", "/GS2")
            #expect(value.count > 4 && value[0] == "<<" && value[3] == "/ca" && value[4] == "0.5", "\(value)")
            let first = try resource(memory.bytes, "/ExtGState", "/GS1")
            #expect(first.count > 2 && first[2] == "0.1", "\(first)")
            #expect(try TestSupport.read(memory.bytes).contains {
                $0.stream != nil && String(decoding: $0.getData(), as: UTF8.self).contains("/GS2 gs\n")
            }, "the content does not use /GS2")
        }
    }

    @Test func aStampAndOptionalContentOnAPageThatWasReadAreInItsResources() throws {
        // They are objects of the PDF being written, and the page has to refer
        // to them by their numbers there (review of 30 October 2026, B2).
        var objects = try TestSupport.read(
                onePagePDF("/Resources << /XObject 5 0 R >>", "<< /Fm9 4 0 R >>"))
        let memory = MemoryPDF()
        let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
        let stamp = Stamp(memory.pdf)
        stamp.setSize(100, 100)
        try stamp.complete()
        stamp.setLocation(200, 200)
        stamp.drawOn(page)
        let group = OptionalContentGroup(memory.pdf, "Layer")
        group.add(Rect(300, 300, 100, 100))
        group.drawOn(page)
        page.complete(&objects)
        try memory.pdf.merge(objects)
        try memory.pdf.complete()
        let stamped = try resource(memory.bytes, "/XObject", "/Fm\(9 + stamp.objNumber!)")
        #expect(stamped.contains("/Form"), "\(stamped)")
        #expect(try resource(memory.bytes, "/XObject", "/Fm9").contains("/Length"))
        #expect(try resource(memory.bytes, "/Properties", "/OC10").contains("/OCG"))
    }

    @Test func aPageWithNoResourcesGetsThem() throws {
        // A font added to it did nothing, and the text was lost with no error
        // (review of 30 October 2026, B5).
        var objects = try TestSupport.read(onePagePDF(""))
        let font = try Font(&objects, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let memory = MemoryPDF()
        let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
        page.addResource(font, &objects)
        page.drawString(font, nil, 12, "Hello", 72, 72)
        page.complete(&objects)
        try memory.pdf.addObjects(objects)
        try memory.pdf.complete()
        let value = try resource(memory.bytes, "/Font", "/F\(font.objNumber)")
        #expect(value.contains("/Type0"), "\(value)")

        objects = try TestSupport.read(onePagePDF(""))
        let pageObj = memory.pdf.getPageObjects(from: objects)[0]
        let state = GraphicsState()
        state.setAlphaNonStroking(0.5)
        pageObj.setGraphicsState(state, &objects)
        #expect(pageObj.getResourcesObject(objects)?.dict.contains("/GS1") == true)
    }

    @Test func anImageOnAPageThatWasReadIsInItsResources() throws {
        var objects = try TestSupport.read(onePagePDF(""))
        let image = try Image(&objects, TestSupport.open("tests/data/PngSuite/TBRN2C08.PNG"))
        let memory = MemoryPDF()
        let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
        page.addResource(image, &objects)
        image.setLocation(72, 72)
        image.drawOn(page)
        page.complete(&objects)
        try memory.pdf.addObjects(objects)
        try memory.pdf.complete()
        let name = "/Im\(image.objNumber!)"
        let value = try resource(memory.bytes, "/XObject", name)
        #expect(value.contains("/Image"), "\(value)")
        let content = try pageContents(TestSupport.read(memory.bytes))[0]
        #expect(content.contains(name + " Do"), "\(content)")
    }

    @Test func thePagesOfAnArrayOfKidsThatIsAnObjectAndOfALeafWithNoType() throws {
        // Both gave a PDF of no pages, and no error (review of 30 October 2026,
        // B3). The two are read and merged as the pages they are.
        let pdfs = [
            TestSupport.pdfWithObjects([
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids 5 0 R /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 500] >>",
                "[3 0 R 4 0 R]"]),
            TestSupport.pdfWithObjects([
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
                "<< /Parent 2 0 R /MediaBox [0 0 200 300] >>",
                "<< /Parent 2 0 R /MediaBox [0 0 400 500] >>"])]
        for buf in pdfs {
            let objects = try TestSupport.read(buf)
            let memory = MemoryPDF()
            let pages = memory.pdf.getPageObjects(from: objects)
            #expect(pages.count == 2)
            #expect(pages.count == 2 && pages[1].getPageSize().getHeight() == 500)
            try memory.pdf.merge(objects)
            try memory.pdf.complete()
            #expect(memory.pdf.getPageObjects(from: try TestSupport.read(memory.bytes)).count == 2)
        }
    }

    // Returns a PDF of pages under one node that holds an inline /Resources
    // of the fonts, which the pages inherit.
    private func inheritedResources(_ pages: Int, _ fonts: Int) -> [UInt8] {
        var resources = ["/Resources << /Font <<"]
        for i in 1...fonts {
            resources.append("/F\(i) 3 0 R")
        }
        resources.append(">> >>")
        let kids = (0..<pages).map { "\($0 + 4) 0 R" }
        var objects = [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [\(kids.joined(separator: " "))] /Count \(pages)"
                    + " /MediaBox [0 0 612 792] \(resources.joined(separator: " ")) >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ]
        for _ in 0..<pages {
            objects.append("<< /Type /Page /Parent 2 0 R >>")
        }
        var source = "%PDF-1.4\n"
        for (i, object) in objects.enumerated() {
            source += "\(i + 1) 0 obj \(object) endobj\n"
        }
        source += "trailer << /Root 1 0 R >>\n%%EOF\n"
        return Array(source.utf8)
    }

    @Test func aLargeInheritedResourcesIsWrittenOnce() throws {
        // A large /Resources that pages inherit is shared by them, not copied
        // into each, and merge writes it once, an object that each page
        // refers to: 1,000 pages under a node of 20,000 fonts took 2.9 GB,
        // and merge wrote 189 MB (review of 11 October 2026, #34).
        let source = inheritedResources(1000, 20000)
        let objects = try TestSupport.read(source)
        let start = Date()
        let pages = PDF().getPageObjects(from: objects)
        try #require(pages.count == 1000)
        #expect((pages[0].inheritedResources?.count ?? 0) >= 80000, "the pages do not share their resources")
        #expect(pages[999].inheritedResources != nil, "the pages do not share their resources")
        let resources = pages[0].getResourcesObject(objects)
        #expect(resources?.getDict().contains("/F20000") == true, "the resources of a page are not read")
        let memory = MemoryPDF()
        try memory.pdf.merge(objects)
        try memory.pdf.complete()
        let merged = memory.bytes
        let elapsed = Date().timeIntervalSince(start)
        #expect(elapsed < 5, "pages and merge took \(elapsed) s")
        #expect(merged.count <= 2 * source.count,
                "the merged PDF is \(merged.count) bytes, of a source of \(source.count)")
        let written = try TestSupport.read(merged)
        let writtenPages = PDF().getPageObjects(from: written)
        try #require(writtenPages.count == 1000)
        #expect(writtenPages[999].getResourcesObject(written)?.getDict().contains("/F20000") == true,
                "a merged page has lost its resources")
        expectReferencesResolve(written)
    }

    @Test func aSmallInheritedResourcesIsCopiedAndASharedOneIsCopiedWhenChanged() throws {
        // A small /Resources that pages inherit is copied into each, as
        // before, and a resource added to a page that shares a large one is
        // the page's alone.
        var objects = try TestSupport.read(inheritedResources(2, 3))
        var pages = PDF().getPageObjects(from: objects)
        #expect(pages[0].inheritedResources == nil && pages[0].getDict().contains("/F3"),
                "a small /Resources is not copied into the page")
        objects = try TestSupport.read(inheritedResources(2, 100))
        pages = PDF().getPageObjects(from: objects)
        _ = try pages[0].addResource(CoreFont.HELVETICA, &objects)
        #expect(pages[0].inheritedResources == nil && pages[0].getDict().contains("/F100"),
                "the page that a font was added to has no resources of its own")
        #expect(pages[1].inheritedResources != nil
                && !pages[1].inheritedResources!.contains("/HELVETICA"),
                "the font added to one page is in the resources of the other")
        #expect(pages[0].getDict().contains("/HELVETICA"), "the font is not in the page's resources")
    }
}
