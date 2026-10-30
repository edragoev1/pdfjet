/**
 * GraphicsStateTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The blend mode of a graphics state, a state set again on a page that was
/// read, and the fields of a form that is drawn on (from Stefan Ostermann's
/// fork of PDFjet, 19 October 2026).
@Suite struct GraphicsStateTests {
    @Test func theBlendModeIsWrittenInTheState() throws {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        page.setGraphicsState(GraphicsState().setAlphaNonStroking(0.5).setBlendMode(BlendMode.MULTIPLY))
        page.setGraphicsState(GraphicsState().setAlphaNonStroking(0.5))
        page.setGraphicsState(GraphicsState().setAlphaNonStroking(0.5).setBlendMode(BlendMode.MULTIPLY))
        page.setGraphicsState(GraphicsState().setBlendMode(BlendMode.COLOR_DODGE))
        let content = TestSupport.content(page)
        let start = content.range(of: "/GS1 gs")!.lowerBound
        #expect(String(content[start...]) == "/GS1 gs\n/GS2 gs\n/GS1 gs\n/GS3 gs\n")
        try memory.pdf.complete()
        let output = TestSupport.latin1(memory.bytes)
        #expect(output.contains("/GS1 <</CA 1 /ca 0.5 /BM /Multiply>>"))
        #expect(output.contains("/GS2 <</CA 1 /ca 0.5>>"))
        #expect(output.contains("/GS3 <</CA 1 /ca 1 /BM /ColorDodge>>"))
    }

    @Test func aStateWithoutABlendModeIsWrittenAsBefore() {
        let gs = GraphicsState().setAlphaStroking(0.25)
        #expect(gs.getBlendMode() == nil)
        #expect(gs.entries() == "/CA 0.25 /ca 1")
        gs.setBlendMode(BlendMode.MULTIPLY).setBlendMode(nil)
        #expect(gs.entries() == "/CA 0.25 /ca 1")
    }

    @Test
    func pdfA1TakesOnlyTheNormalBlendMode() throws {
        let failed = "The PDF was not completed because of an earlier error: "
        let multiply: (PDF, Page) throws -> Void = { _, page in
            page.setGraphicsState(GraphicsState().setBlendMode(BlendMode.MULTIPLY))
        }
        #expect(try ReviewMediaTests.pdfADrawError(Compliance.PDF_A_1B, multiply).0 == failed
                + "A document of PDF_A_1B cannot draw with the blend mode Multiply: PDF/A-1 has no transparency.")
        #expect(try ReviewMediaTests.pdfADrawError(Compliance.PDF_A_2B, multiply).0 == "")
        #expect(try ReviewMediaTests.pdfADrawError(Compliance.PDF_A_1B, { _, page in
            page.setGraphicsState(GraphicsState().setBlendMode(BlendMode.NORMAL))
        }).0 == "")
    }

    @Test func theSameStateSetAgainOnAPageThatWasReadIsReused() throws {
        var objects = try TestSupport.read(TestSupport.pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] "
                    + "/Resources << /ExtGState << /Gs1 << /CA 1 /ca 1 >> >> >> /Contents 4 0 R >>",
            "<< /Length 7 >>\nstream\n/Gs1 gs\nendstream",
        ]))
        let page = TestSupport.pageObjects(objects)[0]
        let highlighter = GraphicsState().setAlphaNonStroking(0.5).setBlendMode(BlendMode.MULTIPLY)
        _ = page.setGraphicsState(highlighter, &objects)
        _ = page.setGraphicsState(GraphicsState().setAlphaNonStroking(0.5), &objects)
        _ = page.setGraphicsState(highlighter, &objects)
        let dict = page.dict.joined(separator: " ")
        #expect(dict.contains("/GS1 << /CA 1 /ca 0.5 /BM /Multiply >>"), "\(dict)")
        #expect(dict.contains("/GS2 << /CA 1 /ca 0.5 >>"), "\(dict)")
        #expect(!dict.contains("/GS3"), "\(dict)")
        #expect(dict.contains("/Gs1 << /CA 1 /ca 1 >>"), "\(dict)")
    }

    // A filled-in form that is drawn on keeps its fields: the catalog of the
    // document carries over the /AcroForm of the one that was read, which a
    // viewer needs to show the values. Without one, there is none.
    @Test func aFormThatIsDrawnOnKeepsItsFields() throws {
        let form = [
            "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R] /NeedAppearances true >> >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Annots [4 0 R] /Resources << >> /Contents 5 0 R >>",
            "<< /Type /Annot /Subtype /Widget /FT /Tx /T (name) /V (Alice) /Rect [20 100 200 130] /P 3 0 R >>",
            "<< /Length 5 >>\nstream\nBT ET\nendstream",
        ]
        let byReference = ["<< /Type /Catalog /Pages 2 0 R /AcroForm 6 0 R >>"] + form.dropFirst()
        let noForm = ["<< /Type /Catalog /Pages 2 0 R >>"] + form.dropFirst()
        let cases: [([String], String?)] = [
            (form, "/AcroForm << /Fields [ 4 0 R ] /NeedAppearances true >>\n"),
            (byReference, "/AcroForm 6 0 R\n"),
            (noForm, nil),
            // References of another generation take 0, as every object's
            // (review of 20 October 2026)
            (["<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 1 R] /NeedAppearances true >> >>"] + form.dropFirst(), "/AcroForm << /Fields [ 4 0 R ] /NeedAppearances true >>\n"),
            (["<< /Type /Catalog /Pages 2 0 R /AcroForm 6 1 R >>"] + form.dropFirst(), "/AcroForm 6 0 R\n"),
        ]
        for (objectsOfCase, expected) in cases {
            var objects = try TestSupport.read(TestSupport.pdfWithObjects(objectsOfCase))
            let page = TestSupport.pageObjects(objects)[0]
            var content = Array("0 0 m 10 10 l S\n".utf8)
            page.addContent(&content, &objects)
            let memory = MemoryPDF()
            try memory.pdf.addObjects(objects)
            try memory.pdf.complete()
            let output = TestSupport.latin1(memory.bytes)
            let match = try #require(output.firstMatch(of: /\/Root (\d+) 0 R/))
            let start = output.range(of: "\n\(match.1) 0 obj\n")!.upperBound
            let catalog = String(output[start..<output[start...].range(of: "endobj")!.lowerBound])
            if let expected = expected {
                #expect(catalog.contains(expected), "\(catalog)")
            } else {
                #expect(!catalog.contains("/AcroForm"), "\(catalog)")
            }
        }
    }
}
