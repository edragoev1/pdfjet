/**
 * OptionalContentGroupTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct OptionalContentGroupTests {
    private func layer(_ visible: Bool) throws -> String {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let group = OptionalContentGroup(memory.pdf, "Layer").setVisible(visible).setPrintable(true)
        _ = group.add(Rect(10, 10, 20, 20))
        TestSupport.expectXY(30, 30, group.drawOn(page))
        try memory.pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    @Test func aHiddenLayerHasTheViewStateOff() throws {
        let pdf = try layer(false)
        #expect(pdf.contains("/OCProperties"))
        #expect(pdf.contains("/View << /ViewState /OFF >>"))
        #expect(pdf.contains("/Print << /PrintState /ON >>"))
        // The default configuration lists it too, for the viewers that do not apply the usage
        #expect(pdf.contains("/OFF ["))
    }

    @Test func aVisibleLayerHasTheViewStateOn() throws {
        let pdf = try layer(true)
        #expect(pdf.contains("/View << /ViewState /ON >>"))
        #expect(!pdf.contains("/ViewState /OFF"))
        #expect(!pdf.contains("/OFF ["))
        // Export is off unless setExportable(true) is called.
        #expect(pdf.contains("/Export << /ExportState /OFF >>"))
    }

    @Test func aGroupWrapsItsContentInMarkedContentAndIsAPageProperty() throws {
        let memory = MemoryPDF()
        let page1 = Page(memory.pdf, Letter.PORTRAIT)
        let map = OptionalContentGroup(memory.pdf, "Map").setVisible(true)
        map.add(Rect(10, 10, 20, 20)).drawOn(page1)
        let notes = OptionalContentGroup(memory.pdf, "Notes")
        notes.add(Line(0, 0, 10, 10)).drawOn(page1)
        let content1 = TestSupport.content(page1)
        #expect(content1.contains("/OC /OC1 BDC\n"), "\(content1)")
        #expect(content1.contains("/OC /OC2 BDC\n"), "\(content1)")
        #expect(content1.components(separatedBy: "EMC").count - 1 == 2, "\(content1)")
        // The same group on a second page is the same object
        let page2 = Page(memory.pdf, Letter.PORTRAIT)
        map.drawOn(page2)
        #expect(TestSupport.content(page2).contains("/OC /OC1 BDC\n"))
        try memory.pdf.complete()
        let file = TestSupport.latin1(memory.bytes)
        #expect(file.components(separatedBy: "/Type /OCG").count - 1 == 2, "one object per group")
        for s in ["/Properties", "/OC1 ", "/OC2 ", "/OCGs [", "/Order ["] {
            #expect(file.contains(s), "\(s)")
        }
    }

    @Test func clearRemovesTheDrawables() {
        let group = OptionalContentGroup(TestSupport.newPDF(), "Layer")
        _ = group.add(Rect(0, 0, 1, 1)).add(Line(0, 0, 1, 1))
        #expect(group.getComponents().count == 2)
        #expect(group.clear().getComponents().isEmpty)
        #expect(group.getName() == "Layer")
    }
}
