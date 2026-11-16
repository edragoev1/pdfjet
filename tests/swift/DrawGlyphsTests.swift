/**
 * DrawGlyphsTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// drawGlyphs draws glyphs another engine shaped, by their numbers, as
/// outlines read from the font file: paths, no text and no font in the
/// document; a glyph drawn more than once a form XObject placed wherever it
/// is; in a tagged document marked content with the text as its actual text.
@Suite struct DrawGlyphsTests {
    private static let devanagari = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf"
    private static let cascadia = "tests/fonts/CascadiaMono-Regular.ttf"

    private func count(_ text: String, _ part: String) -> Int {
        text.components(separatedBy: part).count - 1
    }

    // The index of the bytes of the text in the document from the index, or nil.
    private func find(_ doc: [UInt8], _ text: String, _ from: Int) -> Int? {
        let part = Array(text.utf8)
        var i = from
        while i + part.count <= doc.count {
            if doc[i] == part[0] && Array(doc[i..<(i + part.count)]) == part {
                return i
            }
            i += 1
        }
        return nil
    }

    // The content of each form XObject of the document, inflated.
    private func forms(_ doc: [UInt8]) throws -> [String] {
        var forms = [String]()
        var from = 0
        while let form = find(doc, "/Subtype /Form", from) {
            let start = find(doc, "stream\n", form)! + 7
            let end = find(doc, "\nendstream", start)!
            forms.append(TestSupport.latin1(try TestSupport.inflate(Array(doc[start..<end]))))
            from = end
        }
        return forms
    }

    @Test func theGlyphsAreOutlinesAndNoFontIsWritten() throws {
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        font.setSize(12)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let a = font.unicodeToGID[0x41]
        let b = font.unicodeToGID[0x42]
        page.drawGlyphs(font, [a, b], [50, 100, 60, 100], "AB", "en")
        let content = TestSupport.content(page)
        // Each placed by the matrix of the em at its place
        #expect(content.contains("q 0.012 0 0 0.012 50 692 cm /Fm"), "\(content)")
        #expect(content.contains("q 0.012 0 0 0.012 60 692 cm /Fm"), "\(content)")
        #expect(!content.contains("BT"), "\(content)")
        #expect(!content.contains("Tf"), "\(content)")
        #expect(!content.contains("BDC"), "\(content)")     // Untagged: just the shapes
        try memory.pdf.complete()
        let doc = TestSupport.latin1(memory.bytes)
        #expect(!doc.contains("/FontFile"), "no font program")
        #expect(!doc.contains("/Type /Font"), "no font")
        #expect(!doc.contains("/ToUnicode"), "no ToUnicode map")
        // The outlines: paths in the font's units, filled, no text
        let forms = try forms(memory.bytes)
        #expect(forms.count == 2)
        for form in forms {
            #expect(form.contains(" m\n") && form.contains(" l\n") && form.hasSuffix("f\n"), "\(form)")
            #expect(!form.contains("BT") && !form.contains("Tj"), "\(form)")
        }
    }

    @Test func aGlyphDrawnTwiceIsOneStampPlacedTwice() throws {
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        font.setSize(10)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let a = font.unicodeToGID[0x41]
        let b = font.unicodeToGID[0x42]
        let space = font.unicodeToGID[0x20]
        page.drawGlyphs(font, [a, space, b, a], [50, 100, 60, 100, 65, 100, 70, 100], "A BA", nil)
        var content = TestSupport.content(page)
        let regex = try NSRegularExpression(pattern: "q 0.01 0 0 0.01 (50|70) 692 cm (/Fm\\d+) Do Q\n")
        let matches = regex.matches(in: content, range: NSRange(content.startIndex..., in: content))
        #expect(matches.count == 2, "\(content)")
        let name = String(content[Range(matches[0].range(at: 2), in: content)!])
        #expect(String(content[Range(matches[1].range(at: 2), in: content)!]) == name)
        #expect(count(content, " Do Q\n") == 3, "\(content)")   // The space draws nothing
        // Drawn again, in another run and at another size, A is placed again
        font.setSize(20)
        page.drawGlyphs(font, [a], [80, 100], "A", nil)
        content = TestSupport.content(page)
        #expect(content.contains("q 0.02 0 0 0.02 80 692 cm " + name + " Do Q\n"), "\(content)")
        try memory.pdf.complete()
        let doc = TestSupport.latin1(memory.bytes)
        #expect(count(doc, "/Subtype /Form") == 2, "A and B, each once")
        #expect(doc.contains(name + " "), "the stamp is a resource")
    }

    @Test func inATaggedParagraphTheGlyphsCarryTheirText() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let a = font.unicodeToGID[0x41]
        page.addBDC(StructElem.P, "hi", nil, nil)
        page.drawGlyphs(font, [a], [50, 100], "\u{0915}", "hi")
        page.addEMC()
        let content = TestSupport.content(page)
        #expect(content.contains("/P <</MCID 0>>\nBDC\n/Span <</Lang <" + memory.pdf.toHex("hi")
                + "> /ActualText <" + Page.toUTF16Hex("\u{0915}") + ">>> BDC\nq "), "\(content)")
        #expect(content.contains(" Do Q\nEMC\nEMC\n"), "\(content)")
        #expect(count(content, "BDC\n") == 2, "\(content)")
        try memory.pdf.complete()
        #expect(!TestSupport.latin1(memory.bytes).contains("/FontFile"), "no font program")
    }

    @Test func outsideAnyContentTheGlyphsAreAParagraphOfTheirOwn() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let a = font.unicodeToGID[0x41]
        page.drawGlyphs(font, [a], [50, 100], "A", nil)
        let content = TestSupport.content(page)
        #expect(content.contains("/P <</MCID 0>>\nBDC\n/Span <</ActualText <" + Page.toUTF16Hex("A")
                + ">>> BDC\n"), "\(content)")
        #expect(memory.pdf.error == nil)
        // A tagged document asks for the text the glyphs stand for
        let next = Page(memory.pdf, Letter.PORTRAIT)
        next.drawGlyphs(font, [a], [50, 100], nil, nil)
        #expect(memory.pdf.error != nil)
    }

    @Test func aFontUsedForTextTooIsWritten() throws {
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let a = font.unicodeToGID[0x41]
        page.drawGlyphs(font, [a], [50, 100], "A", nil)
        TextLine(font, "Text").setLocation(50, 200).drawOn(page)
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("/FontFile3"), "the font of the text line")
    }

    @Test func trueTypeOutlinesAreCubicCurvesAndCompositesHaveTheirParts() throws {
        let pdf = TestSupport.newPDF()
        let cascadia = try Font(pdf, TestSupport.open(DrawGlyphsTests.cascadia))
        let outline = try GlyphOutline(cascadia.program!.font, false)
        #expect(abs(outline.matrixOf(1)[0] - 1.0 / 2048) < 1e-12)
        // O: two contours of quadratic curves, drawn as cubic ones
        let o = try outline.outline(cascadia.unicodeToGID[0x4F]).operators
        #expect(count(o, " m\n") == 2, "\(o)")
        #expect(o.contains(" c\n"), "\(o)")
        // Aacute, a composite glyph: the contours of A and of the acute
        let aacute = cascadia.unicodeToGID[0x00C1]
        let capitalA = try outline.outline(cascadia.unicodeToGID[0x41]).operators
        let composite = try outline.outline(aacute).operators
        #expect(count(composite, " m\n") > count(capitalA, " m\n"), "\(composite)")
        #expect(composite.hasPrefix(capitalA) || composite.hasSuffix(capitalA), "\(composite)")

        let devanagari = try Font(pdf, TestSupport.open(DrawGlyphsTests.devanagari))
        let noto = try GlyphOutline(devanagari.program!.font, false)
        let ii = try noto.outline(12).operators     // iideva, a composite glyph
        #expect(ii.contains(" m\n") && ii.contains(" c\n"), "\(ii)")
        #expect(try noto.outline(devanagari.unicodeToGID[0x20]).operators == "")
    }

    @Test func cffOutlinesHaveTheirSubroutinesAndMatrix() throws {
        let pdf = TestSupport.newPDF()
        let plex = try TestSupport.plexSans(pdf)
        let outline = try GlyphOutline(plex.program!.font, true)
        #expect(abs(outline.matrixOf(1)[0] - 0.001) < 1e-12)
        let o = try outline.outline(plex.unicodeToGID[0x4F])
        #expect(count(o.operators, " m\n") == 2, "\(o.operators)")
        #expect(o.yMin < 0 && o.yMax > 600, "\(o.yMin) \(o.yMax)")
        #expect(try outline.outline(plex.unicodeToGID[0x20]).operators == "")
    }

    @Test func cffOutlinesOfAFontMatrixOfTheirOwnAreThoseOfTheTrueTypeEdition() throws {
        // Cascadia Mono's .otf has 2,048 units to the em, in its FontMatrix,
        // 0.00048828: its glyphs, of the numbers of those of its .ttf, are of
        // their size and in their place
        let otf = try Font(TestSupport.newPDF(), "fonts/CascadiaMono/CascadiaMono-Regular.otf")
        // In a PDF of its own: a font of the name and the glyphs of one read
        // before is that one
        let ttf = try Font(TestSupport.newPDF(), TestSupport.open(DrawGlyphsTests.cascadia))
        let cff = try GlyphOutline(otf.program!.font, true)
        let glyf = try GlyphOutline(ttf.program!.font, false)
        #expect(otf.program!.cff && !ttf.program!.cff)
        #expect(abs(cff.matrixOf(1)[0] - 1.0 / 2048) < 1e-8, "\(cff.matrixOf(1))")
        for c in "OAgé".unicodeScalars {
            let gid = otf.unicodeToGID[Int(c.value)]
            #expect(gid != 0 && gid == ttf.unicodeToGID[Int(c.value)], "\(c): glyph \(gid)")
            let a = try cff.outline(gid)
            let b = try glyf.outline(gid)
            #expect(a.xMin == b.xMin && a.yMin == b.yMin && a.xMax == b.xMax && a.yMax == b.yMax,
                    "\(c): \(a.xMin) \(a.yMin) \(a.xMax) \(a.yMax), the .ttf \(b.xMin) \(b.yMin) \(b.xMax) \(b.yMax)")
        }
    }

    @Test func numbersAreHundredthsRoundedHalfAwayFromZero() {
        #expect(GlyphOutline.decimals(-0.004, 2) == "0")
        #expect(GlyphOutline.decimals(1.5, 2) == "1.5")
        #expect(GlyphOutline.decimals(-8.0 / 3, 2) == "-2.67")
        #expect(GlyphOutline.decimals(12.0 / 2048, 7) == "0.0058594")
        #expect(GlyphOutline.decimals(99.999, 2) == "100")
    }

    @Test func attachTextCarriesTheTextAsAnAssociatedFile() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(font, "Text").setLocation(50, 200).drawOn(page)
        try memory.pdf.attachText("article-1.txt", "\u{0938}\u{092d}\u{0940} text")
        try memory.pdf.complete()
        let doc = TestSupport.latin1(memory.bytes)
        #expect(doc.contains("/Subtype /text#2Fplain\n"), "the media type, with no parameters for PDF/A-3")
        #expect(doc.contains("/AFRelationship /Source"), "the relationship")
        #expect(doc.contains("/AF ["), "an associated file")
        #expect(doc.contains("/Desc "), "the description")
    }

    @Test func attachTextIsRefusedInPDFA1AndPDFA2() throws {
        for compliance in [Compliance.PDF_A_1B, Compliance.PDF_A_2B] {
            let memory = MemoryPDF(compliance)
            #expect(throws: (any Error).self) {
                try memory.pdf.attachText("a.txt", "text")
            }
            #expect(memory.pdf.error != nil)
        }
    }

    @Test func misuseIsReported() throws {
        func check(_ draw: (Page, Font, Font, Int) -> Void) throws {
            let memory = MemoryPDF()
            memory.pdf.coreFontsInCompliance = true
            let plex = try TestSupport.plexSans(memory.pdf)
            let core = TestSupport.helvetica(memory.pdf)
            let page = Page(memory.pdf, Letter.PORTRAIT)
            draw(page, plex, core, plex.unicodeToGID[0x41])
            #expect(memory.pdf.error != nil)
        }
        try check { page, _, core, _ in page.drawGlyphs(core, [1], [0, 0], "A", nil) }
        try check { page, plex, _, a in page.drawGlyphs(plex, [a], [0], "A", nil) }
        try check { page, plex, _, _ in page.drawGlyphs(plex, [plex.advanceWidth.count], [0, 0], "A", nil) }
        try check { page, plex, _, _ in page.drawGlyphs(plex, [-1], [0, 0], "A", nil) }
    }

    @Test func aGlyphPastTheHorizontalMetricsIsDrawn() throws {
        // Cascadia Mono has 3,183 horizontal metrics and 4,319 glyphs: the
        // glyphs past the metrics, U+2427 among them, have the advance of the
        // last, as OpenType has it. They were refused as glyphs the font has
        // not, and the whole document failed.
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TestSupport.open(DrawGlyphsTests.cascadia))
        font.setSize(20)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let gid = font.unicodeToGID[0x2427]
        #expect(gid >= font.advanceWidth.count, "glyph \(gid)")
        page.drawGlyphs(font, [gid], [50, 100], "\u{2427}", nil)
        #expect(memory.pdf.error == nil)
        try memory.pdf.complete()
        let forms = try forms(memory.bytes)
        #expect(forms.count == 1 && forms[0].hasSuffix("f\n"), "\(forms)")
    }
}
