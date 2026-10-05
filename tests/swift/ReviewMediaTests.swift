/**
 * ReviewMediaTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

// The fonts, images and SVG images of the review: each test is one of
// review_media_test.go in the Go port.
@Suite struct ReviewMediaTests {
    private let thai = "fonts/NotoSansThai/NotoSansThai-Regular.ttf"
    static let streamFont = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"

    private func font(_ path: String) -> [UInt8] {
        return [UInt8](FileManager.default.contents(atPath: TestSupport.path(path))!)
    }

    private func uint16(_ font: [UInt8], _ offset: Int) -> Int {
        return Int(font[offset]) << 8 | Int(font[offset + 1])
    }

    // Where the directory entry of the table of the name begins.
    private func entry(_ font: [UInt8], _ name: String) -> Int {
        for i in 0..<uint16(font, 4) {
            let entry = 12 + 16*i
            if String(decoding: font[entry..<(entry + 4)], as: UTF8.self) == name {
                return entry
            }
        }
        return -1
    }

    private static func put16(_ bytes: inout [UInt8], _ value: Int) {
        bytes.append(UInt8((value >> 8) & 0xFF))
        bytes.append(UInt8(value & 0xFF))
    }

    private static func put32(_ bytes: inout [UInt8], _ value: Int) {
        put16(&bytes, value >> 16)
        put16(&bytes, value & 0xFFFF)
    }

    // The font with the table of the name spelled differently, so that
    // PDFjet does not read it and the font has none.
    private func without(_ font: [UInt8], _ name: String) -> [UInt8] {
        var patched = font
        patched[entry(font, name)] = UInt8(ascii: "z")
        return patched
    }

    // The font with the table of the name replaced by the bytes, which are put
    // at the end of the font.
    private func withTable(_ font: [UInt8], _ name: String, _ table: [UInt8]) -> [UInt8] {
        let entry = entry(font, name)
        var patched = font + table
        var offset = [UInt8]()
        ReviewMediaTests.put32(&offset, font.count)
        ReviewMediaTests.put32(&offset, table.count)
        patched.replaceSubrange((entry + 8)..<(entry + 16), with: offset)
        return patched
    }

    // Loads the font and draws with it.
    private func draws(_ font: [UInt8]) throws {
        let pdf = TestSupport.newPDF()
        let f = try Font(pdf, InputStream(data: Data(font)))
        TextLine(f, "Ab1 กิ่ x").setLocation(50.0, 50.0).drawOn(Page(pdf, Letter.PORTRAIT))
        try pdf.complete()
    }

    // A GPOS table of one lookup, of the given number of MarkToBase subtables
    // that are all the same one: its marks and its letters are a coverage
    // table of format 2 with one range, of glyph 0 alone, but at the coverage
    // index 65535, and its letters have no classes of marks.
    private func gposBomb(_ subtables: Int) -> [UInt8] {
        var gpos = [UInt8]()
        ReviewMediaTests.put32(&gpos, 0x00010000)
        for value in [0, 0, 10, 1, 4, 4, 0, subtables] {
            ReviewMediaTests.put16(&gpos, value)
        }
        for _ in 0..<subtables {
            ReviewMediaTests.put16(&gpos, 6 + 2*subtables)  // Every subtable is the one after the lookup
        }
        for value in [1, 12, 12, 0, 22, 22, 2, 1, 0, 0, 65535] {
            ReviewMediaTests.put16(&gpos, value)
        }
        return gpos + [UInt8](repeating: 0, count: 16)
    }

    @Test func aGposTableOfSubtablesThatAreOneAnotherLoadsAtOnce() throws {
        // The coverage index of the one glyph makes room for 65,536 glyphs,
        // and each of the 20,000 subtables went through all of them: 40 KB of
        // GPOS table kept a font from loading for a minute.
        let data = withTable(font(thai), "GPOS", gposBomb(20000))
        let start = Date()
        try draws(data)
        #expect(Date().timeIntervalSince(start) < 2.0)
    }

    // A character map of a format 4 subtable alone, for the Windows platform,
    // of the segments of the start and end codes, each of delta 0, followed by
    // the segment of 0xFFFF that ends every table.
    private func cmapOfSegments(_ starts: [Int], _ ends: [Int]) -> [UInt8] {
        let segments = starts.count + 1
        var cmap = [UInt8]()
        for value in [0, 1, 3, 1] {
            ReviewMediaTests.put16(&cmap, value)
        }
        ReviewMediaTests.put32(&cmap, 12)
        for value in [4, 16 + 8*segments, 0, 2*segments, 0, 0, 0] {
            ReviewMediaTests.put16(&cmap, value)
        }
        for end in ends + [0xFFFF] {
            ReviewMediaTests.put16(&cmap, end)
        }
        ReviewMediaTests.put16(&cmap, 0)    // The reserved pad
        for start in starts + [0xFFFF] {
            ReviewMediaTests.put16(&cmap, start)
        }
        for _ in 0..<(2*segments) {
            ReviewMediaTests.put16(&cmap, 0)    // The deltas and the range offsets
        }
        return cmap
    }

    @Test func theCharacterMapIsReadInOnePassOverItsSegments() throws {
        // 32,766 segments of the character 0xFFFE alone, before the last one:
        // a search of every segment for every character took 2^31
        // comparisons. The font has no OS/2 table, so that every character is
        // looked up.
        let data = without(font(thai), "OS/2")
        let same = [Int](repeating: 0xFFFE, count: 32766)
        let start = Date()
        var otf = try OTF(InputStream(data: Data(withTable(data, "cmap", cmapOfSegments(same, same)))))
        #expect(Date().timeIntervalSince(start) < 2.0)
        // The first segment of the character is the one it is in.
        #expect(otf.unicodeToGID[0xFFFE] == 0xFFFE)
        #expect(otf.unicodeToGID[0x41] == 0)
        // Segments of 'A' to 'C' and 'X' to 'Z', with a delta of 0, map each
        // character to the glyph of its code, and none between them.
        otf = try OTF(InputStream(data: Data(withTable(font(thai), "cmap",
                cmapOfSegments([0x41, 0x58], [0x43, 0x5A])))))
        let want = [0x40: 0, 0x41: 0x41, 0x43: 0x43, 0x44: 0, 0x57: 0, 0x58: 0x58, 0x5A: 0x5A, 0x5B: 0]
        for (ch, gid) in want {
            #expect(otf.unicodeToGID[ch] == gid, "character \(ch)")
        }
    }

    @Test func aFontWithoutAnOS2TableHasItsCharacters() throws {
        // The OS/2 table says the first and the last character of the font; a
        // font without one has every character of its character map, where it
        // had none and drew every character as .notdef.
        let data = font(thai)
        let with = try OTF(InputStream(data: Data(data)))
        let noOS2 = try OTF(InputStream(data: Data(without(data, "OS/2"))))
        for ch in [0x41, 0x7A, 0x0E01] {
            #expect(noOS2.unicodeToGID[ch] != 0 && noOS2.unicodeToGID[ch] == with.unicodeToGID[ch])
        }
    }

    // The error of completing a document of the compliance that holds the
    // image of the file, or "" when it completes.
    private func pdfAError(_ level: Compliance, _ path: String) throws -> String {
        return try ReviewMediaTests.pdfAImageError(level, InputStream(fileAtPath: TestSupport.path(path))!)
    }

    // The error of completing a document of the compliance that holds the
    // image, or "" when it completes.
    static func pdfAImageError(_ level: Compliance, _ stream: InputStream) throws -> String {
        let memory = MemoryPDF(level)
        let pdf = memory.pdf
        _ = pdf.setTitle("Title")
        let font = try Font(pdf, TestSupport.open(ReviewMediaTests.streamFont))
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "Text").setLocation(50, 50).drawOn(page)
        try Image(pdf, stream).setAltDescription("An image")
                .setLocation(50, 100).drawOn(page)
        do {
            try pdf.complete()
        } catch {
            return TestSupport.message(error)
        }
        return ""
    }

    @Test(.enabled(if: TestSupport.exists(streamFont), "the fonts directory is not here"))
    func aPDFADocumentHoldsNoImageItsLevelHasNot() throws {
        // The output intent of PDF/A is sRGB, so its images are not CMYK;
        // PDF/A-1 has no soft masks, and 8 bits per component at most.
        let failed = "The PDF was not completed because of an earlier error: "
        #expect(try pdfAError(Compliance.PDF_A_2B, "images/cmyk.jpg") == failed
                + "A document of PDF_A_2B cannot hold a CMYK image: "
                + "its output intent is sRGB, so its images are gray or RGB.")
        #expect(try pdfAError(Compliance.PDF_A_1B, "PngSuite/BASN6A08.PNG") == failed
                + "A document of PDF_A_1B cannot hold an image with transparency: "
                + "PDF/A-1 has no soft masks, so its images are opaque.")
        #expect(try pdfAError(Compliance.PDF_A_1A, "PngSuite/BASN2C16.PNG") == failed
                + "A document of PDF_A_1A cannot hold an image of 16 bits per component: "
                + "PDF/A-1 has 8 at most.")
        // PDF/A-2 and PDF/A-3 hold both, and a document that is not PDF/A all three.
        #expect(try pdfAError(Compliance.PDF_A_2B, "PngSuite/BASN6A08.PNG") == "")
        #expect(try pdfAError(Compliance.PDF_A_3B, "PngSuite/BASN2C16.PNG") == "")
        #expect(try pdfAError(Compliance.PDF_UA_1, "images/cmyk.jpg") == "")
        #expect(try pdfAError(Compliance.PDF_A_1B, "PngSuite/BASN2C08.PNG") == "")
    }

    private func draw(_ svg: String) throws -> String {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let image = try SVGImage(stream: InputStream(data: Data(svg.utf8)))
        _ = image.setLocation(0, 0)
        image.drawOn(page)
        return TestSupport.content(page)
    }

    @Test func theCommentsOfAStyleSheetAreLeftOutInOnePass() throws {
        let svg = "<svg width=\"10\" height=\"10\"><style>" + String(repeating: "/**/", count: 40000)
                + ".a { fill: /* red */ blue } /* .a { fill: red } */ .b { fill: green } /* not closed .a { fill: red }"
                + "</style><rect class=\"a\" width=\"5\" height=\"5\"/></svg>"
        let start = Date()
        let content = try draw(svg)
        #expect(Date().timeIntervalSince(start) < 2.0)
        #expect(content.hasPrefix("0 0 1 rg\n"), "\(content)")
    }

    @Test func theRulesOfTheClassesOfAnElementAreFoundByClass() throws {
        // 20,000 rules and 20,000 elements of three classes each: every
        // element went through every rule for each of its classes.
        var svg = "<svg width=\"10\" height=\"10\"><style>"
        for i in 0..<20000 {
            svg += ".c\(i){fill:red}"
        }
        svg += "</style>" + String(repeating: "<g class=\"x y z\"/>", count: 20000) + "</svg>"
        let start = Date()
        _ = try SVGImage(stream: InputStream(data: Data(svg.utf8)))
        #expect(Date().timeIntervalSince(start) < 2.0)
        // The rules are those of the style sheet, in its order, whatever the
        // order of the classes, and a class named twice has its rules once.
        let content = try draw("<svg width=\"10\" height=\"10\"><style>.b{fill:red} .a{fill:blue} .b{stroke:green}"
                + "</style><rect class=\"b a b\" width=\"5\" height=\"5\"/></svg>")
        #expect(content.hasPrefix("0 0 1 rg\n") && content.contains("0 0.5 0 RG\n"), "\(content)")
    }

    // The first control points of the cubic curves of the path data, rounded
    // to two decimals.
    private func firstControlPoints(_ data: String) throws -> String {
        var points = [String]()
        for op in try SVG.toPDF(SVG.getOperations(data)) where op.cmd == "C" {
            points.append(String(format: "%.2f,%.2f", Double(op.x1), Double(op.y1)))
        }
        return points.joined(separator: " ")
    }

    @Test func aSmoothCurveReflectsTheControlPointOfACurveOfItsKind() throws {
        // T reflects the control point of the quadratic curve before, Q or T,
        // and else starts from the current point; S reflects the second
        // control point of the cubic curve before, C or S, and else starts
        // from the current point. The first control point of a cubic curve
        // made of a quadratic one is two thirds of the way to the quadratic
        // control point.
        let cases = [
            // The second T reflects (15, -10), the control point of the first.
            ("M0 0 Q 5 10 10 0 T 20 0 T 30 0", "3.33,6.67 13.33,-6.67 23.33,6.67"),
            ("M0 0 Q 5 10 10 0 t 10 0 t 10 0", "3.33,6.67 13.33,-6.67 23.33,6.67"),
            ("M0 0 C 0 10 10 10 10 0 T 20 0", "0.00,10.00 10.00,0.00"),
            ("M0 0 Q 5 10 10 0 S 20 10 20 0", "3.33,6.67 10.00,0.00"),
            ("M0 0 C 0 10 10 10 10 0 S 20 -10 20 0", "0.00,10.00 10.00,-10.00"),
            ("M0 0 S 10 10 20 0 S 30 -10 40 0", "0.00,0.00 30.00,-10.00"),
        ]
        for (data, want) in cases {
            #expect(try firstControlPoints(data) == want, "\(data)")
        }
    }

    @Test func aCommandAfterZStartsAtTheStartOfTheSubpathItClosed() throws {
        // A line after Z, with no moveto, starts where the closed subpath did;
        // the path is stroked once, and filled with every subpath in place.
        let content = try draw("<svg width=\"100\" height=\"100\">"
                + "<path d=\"M10 10 L50 10 L50 50 Z L 90 90\" fill=\"red\" stroke=\"black\"/></svg>")
        #expect(content == "1 0 0 rg\n10 782 m\n50 782 l\n50 742 l\n10 782 m\n90 702 l\nf\n"
                + "0 0 0 RG\n1 w\n10 782 m\n50 782 l\n50 742 l\nh\n10 782 m\n90 702 l\nS\n")
    }

    private static func bigEndian32(_ value: UInt32, _ bytes: inout [UInt8]) {
        bytes.append(contentsOf: [
            UInt8(value >> 24), UInt8((value >> 16) & 0xFF), UInt8((value >> 8) & 0xFF), UInt8(value & 0xFF)])
    }

    private func chunk(_ bytes: inout [UInt8], _ type: String, _ data: [UInt8]) {
        let name = Array(type.utf8)
        ReviewMediaTests.bigEndian32(UInt32(data.count), &bytes)
        bytes.append(contentsOf: name)
        bytes.append(contentsOf: data)
        ReviewMediaTests.bigEndian32(UInt32(TestSupport.crc32(name + data), radix: 16)!, &bytes)
    }

    // A PNG file of the size, bit depth and color type, with a pHYs chunk when
    // one is given, and one IDAT chunk.
    private func png(_ width: Int, _ height: Int, _ bitDepth: UInt8, _ colorType: UInt8,
            _ idat: [UInt8], _ phys: [UInt8]? = nil) -> [UInt8] {
        var bytes: [UInt8] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        var ihdr = [UInt8]()
        ReviewMediaTests.bigEndian32(UInt32(width), &ihdr)
        ReviewMediaTests.bigEndian32(UInt32(height), &ihdr)
        ihdr.append(contentsOf: [bitDepth, colorType, 0, 0, 0])
        chunk(&bytes, "IHDR", ihdr)
        if let phys = phys {
            chunk(&bytes, "pHYs", phys)
        }
        chunk(&bytes, "IDAT", idat)
        chunk(&bytes, "IEND", [])
        return bytes
    }

    @Test func anUnknownPNGFilterTypeIsRefused() {
        // Filter types 0 to 4 are all PNG defines; libpng refuses a row of
        // another one, which was read as if it had no filter.
        let error = #expect(throws: (any Error).self) {
            _ = try PNGImage(InputStream(data: Data(png(2, 1, 8, 2, TestSupport.deflate([5, 1, 2, 3, 4, 5, 6])))))
        }
        #expect(TestSupport.message(error) == "Invalid PNG filter type 5.")
    }

    @Test func thePNGFiltersAreUndone() throws {
        // Two rows of two RGB pixels, the second row with each filter, over a
        // first row of Sub.
        let first: [UInt8] = [1, 10, 20, 30, 5, 5, 5]
        let want: [UInt8] = [10, 20, 30, 15, 25, 35]
        let cases: [([UInt8], [UInt8])] = [
            ([0, 1, 2, 3, 4, 5, 6], [1, 2, 3, 4, 5, 6]),
            ([1, 1, 2, 3, 4, 5, 6], [1, 2, 3, 5, 7, 9]),
            ([2, 1, 2, 3, 4, 5, 6], [11, 22, 33, 19, 30, 41]),
            // (left + above) / 2, with the left one of the first pixel 0.
            ([3, 1, 2, 3, 4, 5, 6], [6, 12, 18, 14, 23, 32]),
            // The first pixel takes the one above, as it is nearest; so does
            // the second, of left 6, above 15 and above on the left 10.
            ([4, 1, 2, 3, 4, 5, 6], [11, 22, 33, 19, 30, 41]),
        ]
        for (row, rowWant) in cases {
            let image = try PNGImage(InputStream(data: Data(png(2, 2, 8, 2, TestSupport.deflate(first + row)))))
            #expect(try TestSupport.inflate(image.getData()) == want + rowWant, "filter \(row[0])")
        }
    }

    @Test func aPhysicalSizeOfMorePixelsThanAPNGNumberHoldsIsPassedOver() throws {
        // The numbers of a PNG chunk are 2^31 - 1 at most; one past it drew
        // the image a thousandth of a point wide.
        for ppm: UInt32 in [0x80000000, 0xFFFFFFFF] {
            var phys = [UInt8]()
            ReviewMediaTests.bigEndian32(ppm, &phys)
            ReviewMediaTests.bigEndian32(4724, &phys)
            phys.append(1)
            let image = try PNGImage(InputStream(data: Data(png(8, 8, 8, 2,
                    TestSupport.deflate([UInt8](repeating: 0, count: 8*(1 + 3*8))), phys))))
            #expect(image.getPhysicalWidth() == 0.0 && image.getPhysicalHeight() == 0.0, "\(ppm)")
        }
    }

    private func jpeg(_ sof: UInt8) -> [UInt8] {
        // The frame header, then a scan of its three components and the EOI
        // marker, which a JPEG cut short has not
        return [0xFF, 0xD8, 0xFF, sof, 0x00, 0x11, 8, 0, 8, 0, 8, 3,
                1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0,
                0xFF, 0xDA, 0x00, 0x0C, 3, 1, 0, 2, 0, 3, 0, 0, 63, 0, 0x12, 0x34, 0xFF, 0xD9]
    }

    @Test func aJPEGAReaderCannotDecodeIsRefused() throws {
        // A lossless, a hierarchical or an arithmetic coded JPEG is not one
        // the DCTDecode filter of a PDF reader decodes.
        for sof: UInt8 in [0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF] {
            let error = #expect(throws: (any Error).self) {
                _ = try JPGImage(InputStream(data: Data(jpeg(sof))))
            }
            #expect(TestSupport.message(error) == "Error: The JPEG is lossless, hierarchical or arithmetic coded (SOF"
                    + "\(sof - 0xC0)), which a PDF reader cannot decode.")
        }
        for sof: UInt8 in [0xC0, 0xC1, 0xC2] {
            _ = try JPGImage(InputStream(data: Data(jpeg(sof))))
        }
    }

    @Test func theErrorsOfAJPEGHaveTheMessagesOfTheOtherPorts() {
        // The errors of this port had no text; each has the message the Java
        // and the C# port throw with.
        let cases: [([UInt8], String)] = [
            ([0xFF, 0xD9], "Error: Invalid JPEG header."),
            ([0xFF, 0xD8, 0xFF, 0xD9], "Error: The JPEG ends before its frame header."),
            ([0xFF, 0xD8, 0xFF, 0xC0, 0x00], "Unexpected end of JPEG data."),
            ([0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x01], "Invalid marker segment length."),
        ]
        for (data, want) in cases {
            let error = #expect(throws: (any Error).self) {
                _ = try JPGImage(InputStream(data: Data(data)))
            }
            #expect(TestSupport.message(error) == want)
            #expect(error?.localizedDescription == want)
        }
        var twelveBits = jpeg(0xC0)
        twelveBits[6] = 12
        let error = #expect(throws: (any Error).self) {
            _ = try JPGImage(InputStream(data: Data(twelveBits)))
        }
        #expect(TestSupport.message(error) == "Error: The JPEG has 12 bits per color component, not 8.")
    }

    @Test func theImageObjectHasEveryPixelOfAWideImage() throws {
        // A Float holds every whole number only up to 2^24, and 2^24 + 1
        // pixels were written as 2^24.
        let width = 1 << 24 + 1
        let memory = MemoryPDF()
        let image = try Image(memory.pdf, InputStream(data: Data(png(width, 1, 1, 0,
                TestSupport.deflate([UInt8](repeating: 0, count: 1 + (width + 7)/8))))))
        _ = Page(memory.pdf, Letter.PORTRAIT)
        image.setLocation(0, 0)
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("/Width 16777217\n"))
    }

    @Test func theLinkOfATurnedImageCoversItAsItIsDrawn() throws {
        // The link covered the image as it is drawn unturned, which a quarter
        // turn makes as wide as it was tall.
        for degrees in [0, 90, 180, 270] {
            let memory = MemoryPDF()
            let page = Page(memory.pdf, Letter.PORTRAIT)
            let image = try Image(memory.pdf, TestSupport.path("images/GLA250.png"))
            try image.setRotation(degrees).setURIAction("https://pdfjet.com").setLocation(10, 20)
            image.drawOn(page)
            try memory.pdf.complete()
            let raw = TestSupport.latin1(memory.bytes)
            let regex = try NSRegularExpression(pattern: "/Rect \\[([\\d.]+) ([\\d.]+) ([\\d.]+) ([\\d.]+)\\]")
            guard let match = regex.firstMatch(in: raw, range: NSRange(raw.startIndex..., in: raw)) else {
                Issue.record("\(degrees): no link")
                continue
            }
            let rect = (1...4).map { Double(raw[Range(match.range(at: $0), in: raw)!])! }
            var w = Double(image.getWidth())
            var h = Double(image.getHeight())
            if degrees == 90 || degrees == 270 {
                swap(&w, &h)
            }
            #expect(rect[0] == 10 && abs(rect[2] - rect[0] - w) <= 0.01 && abs(rect[3] - rect[1] - h) <= 0.01
                    && rect[3] == 792 - 20, "\(degrees): /Rect \(rect)")
        }
    }

    @Test func textFitsInAnyWidthAtAFontSizeOfZero() throws {
        // Text of no size has no width; the size divided the width, which
        // trapped here in a CJK font as it made an Int of infinity.
        let pdf = TestSupport.newPDF()
        let fonts = [TestSupport.helvetica(pdf), try Font(pdf, TestSupport.path(thai)),
                Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT)]
        for f in fonts {
            f.setSize(0)
            #expect(f.getFitChars("Hello", 10) == 5)
            #expect(f.getFitChars("Hello", -1) == 0)
        }
    }

    @Test func aFontIsEmbeddedOnceWhenItIsAddedTwice() throws {
        let memory = MemoryPDF()
        let font1 = try Font(memory.pdf, TestSupport.path(thai))
        let font2 = try Font(memory.pdf, TestSupport.path(thai))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(font1, "A").setLocation(50, 50).drawOn(page)
        TextLine(font2, "B").setLocation(50, 80).drawOn(page)
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).components(separatedBy: "/Length1 ").count - 1 == 1)
    }

    @Test(.enabled(if: TestSupport.exists(streamFont), "the fonts directory is not here"))
    func aFontFileIsToldByItsFirstBytesAndNotByItsName() throws {
        // A stream font of a name that does not end in .stream is read as the
        // stream font it is, as a font read from a stream is.
        let path = NSTemporaryDirectory() + "ReviewMediaTests-\(UUID().uuidString).font"
        try Data(font(ReviewMediaTests.streamFont)).write(to: URL(fileURLWithPath: path))
        defer {
            try? FileManager.default.removeItem(atPath: path)
        }
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, path)
        TextLine(font, "Text").setLocation(50, 50).drawOn(Page(pdf, Letter.PORTRAIT))
        try pdf.complete()
    }
}
