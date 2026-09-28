/**
 * ReviewCodesTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The charts, the barcodes, the QR codes, Data Matrix, PDF417 and the
/// encryption, as the Go tests of review_codes_test.go have them.
@Suite struct ReviewCodesTests {
    // Returns the bounding box of the structure element, in the coordinates of the PDF.
    private func bbox(_ element: StructElement, sourceLocation: SourceLocation = #_sourceLocation) throws -> [Float] {
        let attributes = element.attributes ?? ""
        let match = try #require(attributes.firstMatch(of: /\/BBox \[(\S+) (\S+) (\S+) (\S+)\]/),
                sourceLocation: sourceLocation)
        return [Float(match.1)!, Float(match.2)!, Float(match.3)!, Float(match.4)!]
    }

    private func count(_ content: String, _ text: String) -> Int {
        return content.components(separatedBy: text).count - 1
    }

    @Test func chartFlatDataOfLargeValuesHasARange() throws {
        for value: Float in [2e7, -3e7, 1e30] {
            let memory = MemoryPDF()
            let page = Page(memory.pdf, Letter.PORTRAIT)
            let font = TestSupport.helvetica(memory.pdf)
            let chart = Chart(font, font).setLocation(50, 50).setSize(300, 200)
            chart.addSeries("").addPoint(value, value).addPoint(value, value)
            chart.drawOn(page)
            #expect(!TestSupport.content(page).contains("NaN"), "\(value)")
            #expect(throws: Never.self, "\(value)") { try memory.pdf.complete() }
        }
    }

    @Test func chartTheRoundedRangeOfFlatDataHasAGridLine() {
        for value: Float in [0, 5, 2e7, -3e7, 1e30] {
            let round = Chart.roundMaxAndMinValues(value, value)
            #expect(round.maxValue > round.minValue && round.numOfGridLines >= 1, "\(value)")
        }
    }

    @Test func barChartAValueThatIsNotANumberHasNoBar() throws {
        for stacked in [false, true] {
            let memory = MemoryPDF()
            let page = Page(memory.pdf, Letter.PORTRAIT)
            let font = TestSupport.helvetica(memory.pdf)
            let chart = BarChart(font, font).setLocation(50, 50).setSize(300, 200)
            chart.setCategories("a", "b", "c").setStacked(stacked).setDrawValueLabels(true)
            chart.addSeries("", [Float.nan, 10, Float.infinity])
            #expect(chart.drawOn(page) == [350, 250])
            let content = TestSupport.content(page)
            #expect(!content.contains("NaN") && !content.contains(TestSupport.hex("NaN")), "stacked \(stacked)")
            #expect(content.contains(TestSupport.hex("10")), "stacked \(stacked)")
            #expect(throws: Never.self, "stacked \(stacked)") { try memory.pdf.complete() }
        }
    }

    @Test func code128TakesACharacterFrom128To159AsFNC4ShiftAndTheControlCharacter() throws {
        let want: [UInt16] = [UInt16(Code128Table.START_B),
                UInt16(Code128Table.FNC_4), UInt16(Code128Table.SHIFT), 0x05 + 64, 0x78 - 32,
                UInt16(Code128Table.FNC_4), UInt16(Code128Table.SHIFT), 0x1f + 64]
        #expect(Barcode.code128Codewords("\u{85}x\u{9f}").codewords == want)
        // Each takes three of the 48 codewords
        _ = try Barcode(Barcode.CODE_128, String(repeating: "\u{80}", count: 16)).drawOn(Page(TestSupport.newPDF(), Letter.PORTRAIT))
        let error = #expect(throws: PDFjetError.self) {
            _ = try Barcode(Barcode.CODE_128, String(repeating: "\u{80}", count: 17))
        }
        #expect(error?.message.contains("one from 128 to 159 three") == true)
    }

    @Test func barcodeTheFigureHasTheBoxOfTheBarsAndTheDigits() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let height = page.height
        let cases: [(Int, String, Direction)] = [
            (Barcode.EAN_13, "400638133393", Direction.LEFT_TO_RIGHT),
            (Barcode.UPC_A, "03600029145", Direction.LEFT_TO_RIGHT),
            (Barcode.UPC_A, "03600029145", Direction.TOP_TO_BOTTOM),
            (Barcode.CODE_128, "1111111111111111", Direction.LEFT_TO_RIGHT),
        ]
        for (i, c) in cases.enumerated() {
            let barcode = try Barcode(c.0, c.1).setDirection(c.2)
            barcode.setFont(font)
            barcode.setAltDescription(c.1)
            _ = barcode.setLocation(100, 100)
            let xy = barcode.drawOn(page)
            let box = try bbox(page.structures[i])
            // The first digit of EAN-13 and UPC-A is left of the bars, and the
            // digits of a barcode drawn top to bottom left of them, the first
            // above them; the digits of Code 128 are wider than its bars.
            #expect(box[0] < 100, "case \(i)")
            let top = height - box[3]
            if c.2 == Direction.TOP_TO_BOTTOM {
                #expect(top < 100, "case \(i)")
            } else {
                #expect(abs(top - 100) <= TestSupport.delta, "case \(i)")
            }
            #expect(abs(box[2] - xy[0]) <= TestSupport.delta && abs(box[1] - (height - xy[1])) <= TestSupport.delta,
                    "case \(i)")
        }
    }

    @Test func donutChartTheFigureHasTheBoxOfTheLabels() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(memory.pdf)
        DonutChart(font, font).setLocation(100, 100).setRadii(100, 50)
                .addSlice(Slice(25, Color.red, "Apples and pears"))
                .addSlice(Slice(75, Color.blue, "Oranges")).drawOn(page)
        let box = try bbox(page.structures[0])
        // The circle is from 100 to 300; the labels are right and left of it
        #expect(box[0] < 100 && box[2] > 300)
    }

    private func object(_ number: Int, _ raw: String) -> PDFobj {
        let obj = PDFobj()
        obj.number = number
        obj.dict = raw.components(separatedBy: " ")
        return obj
    }

    @Test func decryptorAnAESKeyThatIsTooShortIsRefused() {
        let encrypt = object(6, "6 0 obj << /Filter /Standard /V 5 /R 4 /Length 40 " +
                "/CF << /StdCF << /CFM /AESV2 >> >> /StmF /StdCF /StrF /StdCF /O <00> /U <00> /P -4 >> endobj")
        let error = #expect(throws: DecryptorError.self) { _ = try Decryptor(encrypt, [], [], "") }
        #expect(error?.description == "The encryption of the PDF is not valid: /R 4 with a key of 40 bits for AES")
        // AES with a key of such a length decrypts to nothing
        #expect(Cryptography.aesDecryptCBC([UInt8](repeating: 0, count: 32), [UInt8](repeating: 0, count: 10),
                [UInt8](repeating: 0, count: 16)).isEmpty)
    }

    @Test func decryptorTheContentsOfASignatureAreNotDecrypted() {
        for raw in ["7 0 obj << /Type /Sig /Filter /Adobe.PPKLite /Contents <0123> >> endobj",
                    "7 0 obj << /ByteRange [ 0 10 20 30 ] /Contents <0123> >> endobj",
                    "7 0 obj << /FT /Sig /V << /Type /Sig /Contents <0123> >> /T (Signature1) >> endobj"] {
            let dict = raw.components(separatedBy: " ")
            #expect(Decryptor.isSignatureDict(dict, dict.firstIndex(of: "<0123>")!), "\(raw)")
        }
        // The /Contents of another dictionary is decrypted
        let dict = "7 0 obj << /Type /Annot /Contents (Note) >> endobj".components(separatedBy: " ")
        #expect(!Decryptor.isSignatureDict(dict, dict.firstIndex(of: "(Note)")!))
    }

    @Test func encryptionThePermissionsOfTheCallerAreNotChanged() throws {
        let permissions = Permissions().setAccess(UserAccess.PRINT)
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Encrypted title")
        _ = memory.pdf.setEncryption(Encryption(memory.pdf,
                Passwords().setUserPassword("").setOwnerPassword("owner"), permissions))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(try Font(memory.pdf, CoreFont.HELVETICA), "Secret text").setLocation(50, 50).drawOn(page)
        try memory.pdf.complete()
        #expect(permissions.getAccess() == UserAccess.PRINT)
        // The PDF grants the extraction for accessibility all the same
        let match = try #require(TestSupport.latin1(memory.bytes).firstMatch(of: /\/P (-?\d+)/))
        #expect(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.isSetIn(Permissions(Int(match.1)!).getAccess()))
    }

    @Test func qrCodeTextThatIsNotASCIIStartsWithTheECIOfUTF8() throws {
        // ECI 0111, the assignment number 26 in 8 bits, then byte mode 0100.
        // Version 4 at level L has one block, so the data codewords come first.
        let data = try QRCode("Grüße", ErrorCorrectionLevel.L).createData(ErrorCorrectionLevel.L)
        #expect(data[0] == 0x71 && data[1] >> 4 == 0xA && data[1] & 0x0F == 0x4)
        // ASCII starts with byte mode, as before
        let ascii = try QRCode("Hello", ErrorCorrectionLevel.L).createData(ErrorCorrectionLevel.L)
        #expect(ascii[0] == 0x40 && ascii[1] == 0x54)
    }

    @Test func qrCodeTheECIIsCountedInTheCapacity() throws {
        // 2,953 bytes fit at level L, and 2,952 when they are not all ASCII
        let fits = String(repeating: "é", count: 1476)
        #expect(try QRCode(fits, ErrorCorrectionLevel.L).getModules()?.count == 177)
        let error = #expect(throws: PDFjetError.self) { _ = try QRCode(fits + "a", ErrorCorrectionLevel.L) }
        #expect(error?.message == "The data is too long for a QR code at level L: 2953 bytes, at most 2952.")
    }

    @Test func qrCodeThePenaltyIsThatOfISO18004() {
        let qrutil = QRUtil()
        // 5 by 5 light modules: N1 10 runs of 5, 30; N2 16 blocks, 48; N4 no
        // dark module, 100.
        #expect(qrutil.getLostPoint(QRCode.newMatrix(5)) == 178)
        // 7 by 7 with 1011101 in the first row: N1 60; N2 30 blocks, 90; N3
        // the pattern after the light quiet zone, 40; N4 5 of 49 dark, 70.
        var matrix = QRCode.newMatrix(7)
        matrix[0] = [true, false, true, true, true, false, true]
        #expect(qrutil.getLostPoint(matrix) == 260)
    }

    @Test func qrCodeTheFormatInformationHasTheMaskOfTheLowestPenalty() throws {
        let qr = try QRCode("https://pdfjet.com", ErrorCorrectionLevel.M)
        let qrutil = QRUtil()
        // The mask of the format information, read back from the modules
        var bits = 0
        for i in 0..<15 {
            let row = (i >= 8) ? qr.moduleCount - 15 + i : (i >= 6) ? i + 1 : i
            if qr.modules[row][8] {
                bits |= 1 << i
            }
        }
        let mask = ((bits ^ qrutil.G15_MASK) >> 10) & 7
        // The penalty of each mask, with the modules made again unmasked
        qr.modules = QRCode.newMatrix(qr.moduleCount)
        qr.reserved = QRCode.newMatrix(qr.moduleCount)
        qr.setupPositionProbePattern(0, 0)
        qr.setupPositionProbePattern(qr.moduleCount - 7, 0)
        qr.setupPositionProbePattern(0, qr.moduleCount - 7)
        qr.setupPositionAdjustPattern()
        qr.setupTimingPattern()
        qr.setupTypeInfo(&qr.modules, 0)
        qr.mapData(try qr.createData(qr.errorCorrectionLevel))
        let penalty = (0..<8).map { qrutil.getLostPoint(qr.applyMask($0)) }
        // Every other mask has a higher penalty, or the same and a higher number
        for i in 0..<8 {
            #expect(!(penalty[i] < penalty[mask] || (penalty[i] == penalty[mask] && i < mask)),
                    "mask \(i) has the penalty \(penalty[i]), mask \(mask) of the symbol \(penalty[mask])")
        }
    }

    @Test func qrCodeTheDarkModulesOfARowAreOneRectangle() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        page.setBrushColor(Color.blue)
        let qr = try QRCode("https://pdfjet.com", ErrorCorrectionLevel.M).setModuleColor(Color.red)
        qr.drawOn(page)
        let content = TestSupport.content(page)
        var runs = 0
        for row in qr.modules {
            for col in 0..<row.count where row[col] && (col == 0 || !row[col - 1]) {
                runs += 1
            }
        }
        #expect(count(content, " re\n") == runs)
        // The brush is saved and restored around the modules
        #expect(content.contains("/Artifact BMC\nq\n") && content.hasSuffix("Q\nEMC\n"))
        // The page knows the brush is blue again, and sets red when asked
        page.setBrushColor(Color.red)
        #expect(TestSupport.content(page).hasSuffix("EMC\n1 0 0 rg\n"))
    }

    @Test func dataMatrixADescribedBarcodeIsAFigure() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let dm = DataMatrix("Hello, World!").setAltDescription("Hello, World!")
        _ = dm.setLocation(50, 50)
        dm.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\nq\n") && !content.contains("/Artifact"))
        // Not described, it is decoration, as before
        let plain = Page(memory.pdf, Letter.PORTRAIT)
        DataMatrix("Hello, World!").drawOn(plain)
        #expect(TestSupport.content(plain).hasPrefix("/Artifact BMC\nq\n"))
    }

    @Test func dataMatrixTheBrushOfThePageIsKept() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setBrushColor(Color.blue)
        DataMatrix("Hello").setModuleColor(Color.red).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasSuffix("Q\n"))
        // The page knows the brush is blue again, and sets red when asked
        page.setBrushColor(Color.blue)
        page.setBrushColor(Color.red)
        #expect(String(TestSupport.content(page).dropFirst(content.count)) == "1 0 0 rg\n")
    }

    @Test func pdf417AControlCharacterIsShiftedToByteCompaction() throws {
        // G S in alpha, then the pad and GS after the shift 913, then s e p
        // after the latch to lower case
        #expect(try PDF417("GS \u{1d}sep").dataCodewords() ==
                [30*6 + 18, 30*26 + 29, 913, 0x1D, 30*27 + 18, 30*4 + 15])
        // HT, LF and CR are in text compaction
        #expect(!(try PDF417("a\tb\nc\r").dataCodewords()).contains(913))
    }

    @Test func pdf417TheBarsAreOneBlackArtifact() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        page.setPenColor(Color.red)
        try PDF417("Hello, World!").drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("1 0 0 RG\n/Artifact BMC\nq\n0 0 0 RG\n") && content.hasSuffix("Q\nEMC\n"))
        #expect(count(content, "BMC") == 1)
        // The page knows the pen is red again
        page.setPenColor(Color.red)
        #expect(TestSupport.content(page) == content)
    }

    @Test func pdf417ADescribedBarcodeIsAFigure() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let barcode = try PDF417("Hello, World!").setAltDescription("Hello, World!")
        _ = barcode.setLocation(50, 50)
        barcode.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\nq\n") && !content.contains("/Artifact"))
    }
}
