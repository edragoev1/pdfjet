/**
 * StampInContentTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// A stamp with no description of its own, drawn inside the real content of
/// a tagged page, is part of that content, not an artifact inside it (ISO
/// 14289-1, 7.1): letters drawn once as stamps and placed in a paragraph.
@Suite struct StampInContentTests {
    private func letter(_ pdf: PDF) throws -> Stamp {
        let stamp = Stamp(pdf).setSize(6, 8)
        _ = stamp.moveTo(0, 8).lineTo(3, 0).lineTo(6, 8).fillPath()
        try stamp.complete()
        return stamp
    }

    private func count(_ text: String, _ part: String) -> Int {
        text.components(separatedBy: part).count - 1
    }

    @Test func anUndescribedStampInAParagraphIsPartOfIt() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let stamp = try letter(memory.pdf)
        page.addBDC(StructElem.P, "km", "AAA", "AAA")
        for i in 0..<3 {
            _ = stamp.setLocation(50 + 7 * Float(i), 50)
            _ = stamp.drawOn(page)
        }
        page.addEMC()
        let content = TestSupport.content(page)
        #expect(!content.contains("/Artifact BMC"), "\(content)")
        #expect(count(content, "BDC\n") == 1, "\(content)")
        #expect(count(content, "EMC\n") == 1, "\(content)")
        #expect(count(content, " Do\n") == 3, "\(content)")
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("/ActualText"), "the paragraph keeps its text")
    }

    @Test func anUndescribedStampOutsideContentIsStillAnArtifact() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let stamp = try letter(memory.pdf)
        _ = stamp.setLocation(50, 50)
        _ = stamp.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("/Artifact BMC\n"), "\(content)")
        #expect(count(content, "EMC\n") == 1, "\(content)")
    }

    @Test func aDescribedStampInAParagraphIsAnElementOfItsOwn() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let stamp = try letter(memory.pdf)
        _ = stamp.setAltDescription("A logo")
        page.addBDC(StructElem.P, "en", "Text", "Text")
        _ = stamp.setLocation(50, 50)
        _ = stamp.drawOn(page)
        page.addEMC()
        #expect(count(TestSupport.content(page), "EMC\n") == 2)
    }

    @Test func anUntaggedPageMarksNothing() throws {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let stamp = try letter(memory.pdf)
        page.addBDC(StructElem.P, "km", "A", "A")
        _ = stamp.setLocation(50, 50)
        _ = stamp.drawOn(page)
        page.addEMC()
        let content = TestSupport.content(page)
        #expect(!(content.contains("BMC") || content.contains("BDC") || content.contains("EMC")), "\(content)")
    }
}
