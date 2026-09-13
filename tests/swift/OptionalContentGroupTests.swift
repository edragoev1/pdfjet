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
    }

    @Test func aVisibleLayerHasTheViewStateOn() throws {
        let pdf = try layer(true)
        #expect(pdf.contains("/View << /ViewState /ON >>"))
        #expect(!pdf.contains("/ViewState /OFF"))
        // Export is off unless setExportable(true) is called.
        #expect(pdf.contains("/Export << /ExportState /OFF >>"))
    }

    @Test func clearRemovesTheDrawables() {
        let group = OptionalContentGroup(TestSupport.newPDF(), "Layer")
        _ = group.add(Rect(0, 0, 1, 1)).add(Line(0, 0, 1, 1))
        #expect(group.getComponents().count == 2)
        #expect(group.clear().getComponents().isEmpty)
        #expect(group.getName() == "Layer")
    }
}
