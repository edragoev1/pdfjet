/**
 * CompressionTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The content of the pages is compressed unless setCompression(false) is
/// called, and then written as it is, encrypted or not.
@Suite struct CompressionTests {
    // A PDF of one page that draws the text, compressed or not, encrypted or
    // not.
    private func pdf(_ compression: Bool, _ encrypted: Bool) throws -> [UInt8] {
        let memory = MemoryPDF()
        if encrypted {
            _ = memory.pdf.setEncryption(Encryption(memory.pdf, Passwords(), Permissions()))
        }
        memory.pdf.setCompression(compression)
        // Forty lines, as a stream too short to gain is stored as it is
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(memory.pdf)
        for i in 0..<40 {
            TextLine(font, "Readable").setLocation(50, 50 + 15 * Float(i)).drawOn(page)
        }
        try memory.pdf.complete()
        return memory.bytes
    }

    // The content of the page, read back from the PDF.
    private func content(_ bytes: [UInt8]) throws -> String {
        let objects = try TestSupport.read(bytes, "")
        let page = PDF().getPageObjects(from: objects)[0]
        return TestSupport.latin1(page.getContentObject(objects)?.getData() ?? [])
    }

    @Test func theContentOfThePagesIsCompressedUnlessSetOtherwise() throws {
        let compressed = TestSupport.latin1(try pdf(true, false))
        #expect(compressed.contains("/Filter /FlateDecode"))
        #expect(!compressed.contains(TestSupport.hex("Readable")))

        let plain = TestSupport.latin1(try pdf(false, false))
        #expect(!plain.contains("/Filter /FlateDecode"), "the fonts are core fonts, so nothing else is compressed")
        #expect(plain.contains(TestSupport.hex("Readable")), "the text is in the file as it is drawn")
        #expect(plain.contains("\nBT\n"))
    }

    // An encrypted document numbers its font after the encryption, so each is
    // compared with the one compressed the other way.
    @Test func uncompressedContentIsReadBackTheSameEncryptedOrNot() throws {
        let plain = try content(pdf(true, false))
        #expect(plain.contains(TestSupport.hex("Readable")))
        #expect(try content(pdf(false, false)) == plain)
        let encrypted = try content(pdf(true, true))
        #expect(encrypted.contains(TestSupport.hex("Readable")))
        #expect(try content(pdf(false, true)) == encrypted)
    }
}
