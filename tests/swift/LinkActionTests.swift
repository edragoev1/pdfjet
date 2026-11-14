/**
 * LinkActionTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The links to one address share one action, which holds the address: each
/// link carried it, and twenty links to an address of 20,000 characters added
/// about 800 KB. Each link keeps its own description.
@Suite struct LinkActionTests {
    @Test func linksToOneAddressShareOneAction() throws {
        let address = "https://pdfjet.com/" + String(repeating: "a", count: 300)
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        _ = TextLine(font, "One").setURIAction(address).setLocation(50, 100).drawOn(page)
        _ = TextLine(font, "Two").setURIAction(address).setLocation(50, 120).drawOn(page)
        _ = TextLine(font, "Site").setURIAction("https://pdfjet.com").setLocation(50, 140).drawOn(page)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/Subtype /Link").count - 1 == 3)
        #expect(raw.components(separatedBy: "/S /URI\n").count - 1 == 2)
        let regex = try NSRegularExpression(pattern: "/A (\\d+) 0 R\n")
        let range = NSRange(raw.startIndex..., in: raw)
        let links = regex.matches(in: raw, range: range).map { (raw as NSString).substring(with: $0.range(at: 1)) }
        #expect(links.count == 3)
        #expect(Set(links).count == 2)
    }
}
