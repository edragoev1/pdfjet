/**
 * FontCoverageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Every face of the fonts PDFjet carries for Latin text has every letter of
/// modern Greek and of the Cyrillic of Russian, Ukrainian, Belarusian,
/// Bulgarian, Serbian and Macedonian: a document in one of them never shows
/// an empty box (the owner, 29 October 2026, of ѝ, the Bulgarian "her", which
/// JetBrains Mono does not have).
@Suite struct FontCoverageTests {
    private static let families = ["IBMPlexSans", "IBMPlexSerif", "SourceSerif4", "CascadiaMono"]

    // The letters of modern Greek, U+0386 to U+03CE, and of Cyrillic, U+0400
    // to U+045F; what the code points between them are not, as U+0387, a
    // punctuation mark, is skipped by the test.
    private func letters() -> [Unicode.Scalar] {
        var letters = [Unicode.Scalar]()
        for range in [0x0386...0x03CE, 0x0400...0x045F] {
            for c in range {
                let scalar = Unicode.Scalar(UInt32(c))!
                switch scalar.properties.generalCategory {
                case .uppercaseLetter, .lowercaseLetter, .titlecaseLetter, .modifierLetter, .otherLetter:
                    letters.append(scalar)
                default:
                    break
                }
            }
        }
        return letters
    }

    @Test func everyFaceForLatinTextHasGreekAndCyrillic() throws {
        let letters = letters()
        #expect(letters.count == 69 + 96)
        let pdf = TestSupport.newPDF()
        var faces = 0
        for family in FontCoverageTests.families {
            let folder = "resources/fonts/" + family
            for file in try FileManager.default.contentsOfDirectory(atPath: TestSupport.path(folder))
                    where file.hasSuffix(".ttf") {
                let font = try Font(pdf, TestSupport.open(folder + "/" + file))
                var missing = ""
                for letter in letters where font.unicodeToGID[Int(letter.value)] == 0 {
                    missing.unicodeScalars.append(letter)
                }
                #expect(missing == "", "\(file)")
                faces += 1
            }
        }
        #expect(faces == 14 + 14 + 10 + 10)
    }
}   // End of FontCoverageTests.swift
