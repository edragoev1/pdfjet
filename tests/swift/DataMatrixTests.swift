/**
 * DataMatrixTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct DataMatrixTests {
    @Test func sixDigitsFitTheSmallestSquareWithItsFinderPattern() {
        let modules = DataMatrix("123456").getModules()
        #expect(modules.count == 10)
        #expect(modules[0].count == 10)
        for i in 0..<10 {
            #expect(modules[0][i] == (i % 2 == 0), "top row alternates")
            #expect(modules[9][i], "bottom row is solid")
            #expect(modules[i][0], "left column is solid")
        }
    }

    @Test func longerDataGetsALargerSymbol() {
        #expect(DataMatrix(String(repeating: "Z", count: 60)).getModules().count == 32)
    }

    @Test func theRectangleShapeIsWiderThanTall() {
        let modules = DataMatrix("Hello, World!", DataMatrix.RECTANGLE).getModules()
        #expect(modules.count == 12)
        #expect(modules[0].count == 26)
    }

    @Test func drawOnReturnsTheCornerOfTheModules() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let dm = DataMatrix("123456").setLocation(5, 5).setModuleLength(3)
        TestSupport.expectXY(35, 35, dm.drawOn(page))
    }

    /// The GS1 symbols below were read back with ZXing, which gave the
    /// symbology identifier ]d2 of GS1 DataMatrix and these element strings.
    @Test func gs1WritesTheElementStringWithGSAfterAFieldOfNoSetLength() throws {
        let cases = [
            ("(01)09506000134352(17)261231(10)ABC123(21)XYZ-42", "01095060001343521726123110ABC123\u{1d}21XYZ-42"),
            ("(10)BATCH7(21)SN001(01)09506000134352", "10BATCH7\u{1d}21SN001\u{1d}0109506000134352"),
            ("(00)106141412345678908", "00106141412345678908"),
            ("(01)09506000134352(3103)000750(15)270101", "0109506000134352310300075015270101"),
        ]
        for (data, want) in cases {
            #expect(try GS1.elementString(data) == want)
        }
    }

    @Test func gs1TakesTheSymbolOfItsCodewords() throws {
        // FNC1 and 12 codewords, as the plain text takes 13 with GS: 18 by 18 holds 18
        #expect(try DataMatrix(gs1: "(01)09506000134352(10)ABC").getModules().count == 18)
        #expect(DataMatrix("0109506000134352\u{1d}10ABC").getModules().count == 18)
        let rect = try DataMatrix(gs1: "(01)09506000134352", DataMatrix.RECTANGLE).getModules()
        #expect(rect.count < rect[0].count)
    }

    @Test func gs1RefusesDataThatIsNotGS1() throws {
        let format =
                "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!"
        let cases: [(String, String)] = [
            ("", format),
            ("01)09506000134352", format),
            ("(01", format),
            ("(1)5", "The Application Identifier (1) is not two to four digits!"),
            ("(12345)5", "The Application Identifier (12345) is not two to four digits!"),
            ("(A1)5", "The Application Identifier (A1) is not two to four digits!"),
            ("(10)(21)SN", "The Application Identifier (10) has no data!"),
            ("(10)AB C", "The data of (10) has a character that GS1 does not allow!"),
            ("(10)AB)C", "The data of (10) has a character that GS1 does not allow!"),
            ("(10)Gr\u{fc}\u{df}e", "The data of (10) has a character that GS1 does not allow!"),
            ("(91)" + String(repeating: "X", count: 91) + "", "The data of (91) is longer than 90 characters!"),
            ("(01)0950600013435", "The data of (01) must be 14 digits!"),
            ("(17)2612A1", "The data of (17) must be 6 digits!"),
            ("(3103)00075", "The data of (3103) must be 6 digits!"),
            ("(01)09506000134353", "The check digit of (01) is wrong!"),
            ("(00)106141412345678909", "The check digit of (00) is wrong!"),
            ("(414)9506000134353", "The check digit of (414) is wrong!"),
        ]
        for (data, message) in cases {
            let error = #expect(throws: PDFjetError.self) { _ = try DataMatrix(gs1: data) }
            #expect(error?.message == message, "\(data)")
        }
        // Fields of no set length, such as (10) and (21), take any data GS1
        // allows, and (418), a GLN of no check digit here, takes any 13 digits
        _ = try DataMatrix(gs1: "(10)!\"%&'*+,-./:;<=>?_az(21)1(418)1234567890123")
    }
}
