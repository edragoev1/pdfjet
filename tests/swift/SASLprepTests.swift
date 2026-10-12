/**
 * SASLprepTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// The passwords of AES-256 prepared with SASLprep, RFC 4013. The results are
/// compared as code points: Swift compares Strings canonically.
@Suite struct SASLprepTests {
    private func codePoints(_ s: String) -> [UInt32] {
        return s.unicodeScalars.map { $0.value }
    }

    // The examples of RFC 4013, section 3.
    @Test func prepareTheExamplesOfRFC4013() throws {
        for (input, want) in [
            ("I\u{AD}X", "IX"),
            ("user", "user"),
            ("USER", "USER"),
            ("\u{AA}", "a"),
            ("\u{2168}", "IX"),
        ] {
            #expect(codePoints(try SASLprep.prepare(input)) == codePoints(want), "\(codePoints(input))")
        }
        for input in ["\u{7}", "\u{627}1"] {
            #expect(throws: (any Error).self, "\(codePoints(input))") { _ = try SASLprep.prepare(input) }
        }
        // Right to left text that starts and ends with a right to left character
        _ = try SASLprep.prepare("\u{627}1\u{628}")
    }

    // The password as an accented letter is typed on one keyboard and another,
    // a full-width letter, a no-break space and a Hangul syllable written as
    // its letters come out the same.
    @Test func prepareNormalizes() throws {
        for (input, want) in [
            ("cafe\u{301}", "caf\u{E9}"),
            ("caf\u{E9}", "caf\u{E9}"),
            ("\u{FF21}\u{FF22}", "AB"),
            ("a\u{A0}b", "a b"),
            ("\u{1100}\u{1161}\u{11A8}", "\u{AC01}"),
            ("\u{AC01}", "\u{AC01}"),
            ("\u{FB01}", "fi"),
            ("x\u{B2}", "x2"),
            // Marks put in the order of their classes: dot below (220) before
            // the acute accent (230), the q with no composite of either
            ("q\u{301}\u{323}", "q\u{323}\u{301}"),
            // A blocked mark: the ring above stays apart from the A that the
            // acute accent of the same class, before it, did not compose with
            ("A\u{301}\u{30A}", "\u{C1}\u{30A}"),
        ] {
            #expect(codePoints(try SASLprep.prepare(input)) == codePoints(want), "\(codePoints(input))")
        }
        #expect(throws: (any Error).self, "a private use character is not refused") {
            _ = try SASLprep.prepare("pass\u{E000}")
        }
    }

    @Test func truncateCutsBeforeACharacterThatDoesNotFit() {
        let password = String(repeating: "a", count: 126) + "\u{E9}"
        #expect(SASLprep.truncate(password).count == 126)
        #expect(SASLprep.truncate(String(repeating: "a", count: 200)).count == 127)
    }
}
