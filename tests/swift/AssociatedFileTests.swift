/**
 * AssociatedFileTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The files a document carries with it, which PDF/A-3 calls associated files.
@Suite struct AssociatedFileTests {
    private let xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<invoice/>\n"

    private func bytes(_ text: String) -> InputStream {
        return InputStream(data: Data(text.utf8))
    }

    private func attach(_ pdf: PDF, _ fileName: String, _ text: String) throws -> EmbeddedFile {
        return try EmbeddedFile(pdf, fileName, bytes(text), false,
                "text/xml", Relationship.ALTERNATIVE, "The invoice, as data.")
    }

    // A document of one page that carries the files.
    @discardableResult
    private func document(_ memory: MemoryPDF, _ fileNames: [String]) throws -> String {
        for fileName in fileNames {
            memory.pdf.addAssociatedFile(try attach(memory.pdf, fileName, xml))
        }
        TextLine(TestSupport.helvetica(memory.pdf), "Invoice")
                .setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    private func document(_ fileNames: String...) throws -> String {
        return try document(MemoryPDF(Compliance.PDF_A_3B), fileNames)
    }

    /// The catalog of the document, which a failed test shows.
    private func catalog(_ raw: String) -> String {
        guard let range = raw.range(of: "/Type /Catalog", options: .backwards) else {
            return raw
        }
        return String(raw[range.lowerBound...])
    }

    @Test func theCatalogSaysWhichFilesTheDocumentCarries() throws {
        let raw = try document("factur-x.xml")
        let files = try #require(raw.firstMatch(of: /\/AF \[(\d+) 0 R\]/), "\(catalog(raw))")
        // The number is the file specification, not the stream of the bytes.
        #expect(raw.contains("\(files.1) 0 obj\n<<\n/Type /Filespec"), "\(files.1)")
    }

    @Test func aReaderFindsTheFileByItsName() throws {
        let raw = try document("factur-x.xml")
        let names = try #require(raw.firstMatch(
                of: /\/Names <<\/EmbeddedFiles <<\/Names \[<([0-9a-fA-F]+)> (\d+) 0 R\]>>>>/),
                "\(catalog(raw))")
        #expect(TestSupport.utf16Hex(String(names.1)) == "factur-x.xml")
        #expect(raw.contains("/AF [\(names.2) 0 R]"))
    }

    @Test func theNamesOfTheFilesAreInOrder() throws {
        let raw = try document("invoice.xml", "data.xml", "Notes.txt")
        let names = try #require(raw.firstMatch(of: /\/Names \[(.+?)\]>>>>/), "\(catalog(raw))")
        var order = ""
        for name in String(names.1).matches(of: /<([0-9a-fA-F]+)>/) {
            order += TestSupport.utf16Hex(String(name.1)) + " "
        }
        #expect(order == "Notes.txt data.xml invoice.xml ")
        // The /AF array is the order the files were added in, which the
        // specification leaves to the writer of the document.
        #expect(raw.components(separatedBy: "/Type /Filespec").count - 1 == 3)
    }

    @Test func theFileSaysWhatItHoldsAndHowItRelatesToTheDocument() throws {
        let raw = try document("factur-x.xml")
        let filespec = try #require(raw.range(of: "/Type /Filespec"))
        let endobj = try #require(raw.range(of: "endobj", range: filespec.lowerBound..<raw.endIndex))
        let dictionary = String(raw[filespec.lowerBound..<endobj.lowerBound])
        #expect(dictionary.contains("/AFRelationship /Alternative\n"), "\(dictionary)")
        #expect(dictionary.contains("/Desc <"), "\(dictionary)")
        let desc = try #require(dictionary.firstMatch(of: /\/Desc <([0-9a-fA-F]+)>/), "\(dictionary)")
        #expect(TestSupport.utf16Hex(String(desc.1)) == "The invoice, as data.")
        // Readers of PDF 1.7 look at /UF first and older ones at /F, and
        // PDF/A-3 asks for both, in the file specification and in /EF.
        #expect(dictionary.contains("/F <"), "\(dictionary)")
        #expect(dictionary.contains("/UF <"), "\(dictionary)")
        let stream = try #require(dictionary.firstMatch(
                of: /\/EF <<\/F (\d+) 0 R \/UF (\d+) 0 R>>/), "\(dictionary)")
        #expect(stream.1 == stream.2)

        let file = try #require(raw.range(of: "\(stream.1) 0 obj\n<<\n/Type /EmbeddedFile"))
        let begin = try #require(raw.range(of: "stream\n", range: file.lowerBound..<raw.endIndex))
        let embedded = String(raw[file.lowerBound..<begin.lowerBound])
        #expect(embedded.contains("/Subtype /text#2Fxml\n"), "\(embedded)")
        #expect(embedded.contains("/Params <</Size \(xml.utf8.count) /ModDate (D:"), "\(embedded)")
        #expect(embedded.contains("/Length \(xml.utf8.count)\n"), "\(embedded)")
    }

    @Test func theSizeOfACompressedFileIsTheSizeItHadBeforeItWasCompressed() throws {
        let memory = MemoryPDF(Compliance.PDF_A_3B)
        var text = ""
        for _ in 0..<1000 {
            text += "<line>The same line, over and over.</line>\n"
        }
        memory.pdf.addAssociatedFile(try EmbeddedFile(memory.pdf, "long.xml", bytes(text), true,
                "text/xml", Relationship.DATA, "A long file."))
        TextLine(TestSupport.helvetica(memory.pdf), "x")
                .setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.contains("/Params <</Size \(text.utf8.count) /ModDate (D:"), "the size")
        #expect(raw.contains("/Filter /FlateDecode\n"), "the filter")
        let embedded = try #require(raw.range(of: "/Type /EmbeddedFile"))
        let length = try #require(String(raw[embedded.lowerBound...]).firstMatch(of: /\/Length (\d+)\n/))
        #expect(Int(length.1)! < text.utf8.count / 10, "\(length.1)")
    }

    @Test func theMediaTypeIsWrittenAsANameWhateverItHolds() throws {
        let memory = MemoryPDF(Compliance.PDF_A_3B)
        memory.pdf.addAssociatedFile(try EmbeddedFile(memory.pdf, "sheet.ods", bytes("x"), false,
                "application/vnd.oasis.opendocument.spreadsheet", Relationship.SOURCE, "A sheet."))
        memory.pdf.addAssociatedFile(try EmbeddedFile(memory.pdf, "odd.bin", bytes("x"), false,
                "application/x-(odd) #1", Relationship.SUPPLEMENT, "Something else."))
        TextLine(TestSupport.helvetica(memory.pdf), "x")
                .setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.contains(
                "/Subtype /application#2Fvnd.oasis.opendocument.spreadsheet\n"), "the sheet")
        #expect(raw.contains("/Subtype /application#2Fx-#28odd#29#20#231\n"), "the odd one")
        #expect(raw.contains("/AFRelationship /Source\n"), "the source")
        #expect(raw.contains("/AFRelationship /Supplement\n"), "the supplement")
    }

    @Test func aDocumentThatCarriesNoFilesSaysNothingAboutThem() throws {
        let memory = MemoryPDF(Compliance.PDF_A_3B)
        TextLine(TestSupport.helvetica(memory.pdf), "x")
                .setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(!raw.contains("/AF ["), "the array")
        #expect(!raw.contains("/EmbeddedFiles"), "the names")
    }

    @Test func aFileThatSaysNothingAboutItselfIsNotOneTheDocumentCanCarry() throws {
        let memory = MemoryPDF(Compliance.PDF_A_3B)
        memory.pdf.addAssociatedFile(
                try EmbeddedFile(memory.pdf, "factur-x.xml", bytes(xml), false))
        let problem = try #require(memory.pdf.error)
        #expect(problem.contains("factur-x.xml"), "\(problem)")
        // The file it does not carry is still embedded, for an annotation of a
        // page to point at, and is written without the entries of PDF/A-3.
        #expect(!problem.contains("/AFRelationship"), "\(problem)")
    }

    @Test func theLevelOfPdfA3aAndPdfUA1IsBoth() throws {
        let memory = MemoryPDF(Compliance.PDF_A_3A_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(memory.pdf), "Invoice").setLocation(50, 50).drawOn(page)
        // Tagged, as PDF/UA asks, where PDF/A-3a alone is not
        #expect(TestSupport.content(page).contains("BDC"), "the text is not tagged")
        let raw = try document(memory, ["factur-x.xml"])
        for want in ["<pdfuaid:part>1</pdfuaid:part>", "<pdfaid:part>3</pdfaid:part>", "<pdfaid:conformance>A</pdfaid:conformance>", "<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>", "/StructTreeRoot", "/OutputIntents", "/AF ["] {
            #expect(raw.contains(want), "\(want)")
        }
    }

    @Test func theMetadataHasOneListOfExtensionSchemas() throws {
        // A document of PDF/A-3a and PDF/UA-1 that adds a list of its own, as
        // Factur-X does, has the PDF/UA identification schema in that list
        for own in [false, true] {
            let memory = MemoryPDF(Compliance.PDF_A_3A_UA_1)
            if own {
                _ = memory.pdf.addMetadata("<rdf:Description rdf:about=\"\" xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\">\n" +
                        "  <pdfaExtension:schemas>\n" +
                        "    <rdf:Bag>\n" +
                        "    </rdf:Bag>\n" +
                        "  </pdfaExtension:schemas>\n" +
                        "</rdf:Description>\n")
            }
            let raw = try document(memory, ["factur-x.xml"])
            #expect(raw.components(separatedBy: "<pdfaExtension:schemas>").count - 1 == 1, "own list \(own)")
            #expect(raw.contains("<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>"), "own list \(own)")
        }
    }

    @Test func theDocumentsThatCannotCarryAFileSayTheyCannot() throws {
        for compliance in [Compliance.PDF_A_1A, Compliance.PDF_A_1B,
                Compliance.PDF_A_2A, Compliance.PDF_A_2B] {
            let memory = MemoryPDF(compliance)
            memory.pdf.addAssociatedFile(try attach(memory.pdf, "factur-x.xml", xml))
            let problem = try #require(memory.pdf.error)
            #expect(problem.contains("\(compliance)"), "\(problem)")
        }
        // The documents that can: PDF/A-3, and the ones of no profile at all.
        for compliance in [Compliance.PDF_A_3A, Compliance.PDF_A_3B,
                Compliance.PDF_1_7, Compliance.PDF_UA_1] {
            let memory = MemoryPDF(compliance)
            #expect(try document(memory, ["factur-x.xml"]).contains("/AF ["), "\(compliance)")
        }
    }

    @Test func theMetadataCarriesTheDescriptionsOfTheStandardsOfTheDocument() throws {
        let memory = MemoryPDF(Compliance.PDF_A_3B)
        memory.pdf.addMetadata("<rdf:Description rdf:about=\"\" xmlns:fx=\"urn:test:1p0#\">\n"
                + "  <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>\n"
                + "</rdf:Description>")
        let raw = try document(memory, ["factur-x.xml"])
        let description = try #require(
                raw.range(of: "<fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>"),
                "the property is written")
        // Inside the metadata: after the description of the document and
        // before the end of the RDF.
        #expect(raw.range(of: "</pdfaid:conformance>", options: .backwards,
                range: raw.startIndex..<description.lowerBound) != nil, "after the document")
        #expect(raw.range(of: "</rdf:RDF>",
                range: description.upperBound..<raw.endIndex) != nil, "before the end")
        #expect(raw.range(of: "</x:xmpmeta>",
                range: description.upperBound..<raw.endIndex) != nil, "inside the metadata")
    }
}
