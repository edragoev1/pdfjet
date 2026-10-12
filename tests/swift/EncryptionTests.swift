/**
 * EncryptionTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// AES-256 encryption with the standard security handler, read back with the passwords.
@Suite struct EncryptionTests {
    private func encrypted(_ compliance: Compliance, _ passwords: Passwords, _ permissions: Permissions) throws -> [UInt8] {
        let memory = MemoryPDF(compliance)
        memory.pdf.coreFontsInCompliance = true    // Helvetica, in PDF/UA too
        _ = memory.pdf.setTitle("Encrypted title")
        _ = memory.pdf.setEncryption(Encryption(memory.pdf, passwords, permissions))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(try Font(memory.pdf, CoreFont.HELVETICA), "Secret text").setLocation(50, 50).drawOn(page)
        try memory.pdf.complete()
        return memory.bytes
    }

    private func encrypted(_ user: String, _ owner: String) throws -> [UInt8] {
        return try encrypted(Compliance.PDF_1_7,
                Passwords().setUserPassword(user).setOwnerPassword(owner),
                Permissions().grant(UserAccess.PRINT))
    }

    private func accessValue(_ pdf: [UInt8], sourceLocation: SourceLocation = #_sourceLocation) throws -> Int {
        let match = try #require(TestSupport.latin1(pdf).firstMatch(of: /\/P (-?\d+)/), sourceLocation: sourceLocation)
        return Int(match.1)!
    }

    private func title(_ objects: [PDFobj]) -> String {
        return TestSupport.utf16Hex(TestSupport.findObject(objects, "/Producer")?.getValue("/Title") ?? "")
    }

    @Test func readsBackWithTheUserOrTheOwnerPassword() throws {
        let pdf = try encrypted("hello", "world")
        for password in ["hello", "world"] {
            let objects = try TestSupport.read(pdf, password)
            #expect(TestSupport.pageObjects(objects).count == 1)
            #expect(title(objects) == "Encrypted title")
        }
    }

    @Test func theTitleIsNotInTheFileInPlainText() throws {
        let raw = TestSupport.latin1(try encrypted("hello", "world"))
        #expect(!raw.contains("feff0045006e0063"))
        #expect(!raw.lowercased().contains("feff0045006e0063"))
        #expect(raw.contains("/Encrypt "))
    }

    @Test func aWrongOrMissingPasswordFailsWithAMessage() throws {
        let pdf = try encrypted("hello", "world")
        let wrong = #expect(throws: (any Error).self) { _ = try TestSupport.read(pdf, "wrong") }
        #expect(TestSupport.message(wrong) == "The password of the PDF is not correct.")
        let missing = #expect(throws: (any Error).self) { _ = try TestSupport.read(pdf) }
        #expect(TestSupport.message(missing) == "The PDF needs a password.")
    }

    @Test func anEmptyUserPasswordOpensWithoutAPassword() throws {
        #expect(title(try TestSupport.read(try encrypted("", "owner"))) == "Encrypted title")
    }

    @Test func aNonAsciiPasswordIsUtf8() throws {
        let pdf = try encrypted("пароль", "owner")
        #expect(title(try TestSupport.read(pdf, "пароль")) == "Encrypted title")
    }

    @Test func passwordsAreCutAt127Bytes() throws {
        let password = String(repeating: "a", count: 200)
        let pdf = try encrypted(password, "owner")
        #expect(title(try TestSupport.read(pdf, String(password.prefix(127)))) == "Encrypted title")
        #expect(throws: (any Error).self) { _ = try TestSupport.read(pdf, String(password.prefix(126))) }
    }

    @Test func theDictionarySaysTheKeyIs256BitsForAcrobat() throws {
        // Adobe Acrobat opens no AES-256 file without /Length, though PDF 2.0
        // leaves it optional for V 5 (found on 1 October 2026, with Example_30)
        #expect(TestSupport.latin1(try encrypted("", "world")).contains("/R 6\n/Length 256\n"))
    }

    @Test func permissionsAreANegativeNumberWithTheReservedBitsSet() throws {
        #expect(try accessValue(try encrypted("hello", "world")) == -3900)
    }

    @Test func pdfUaGrantsExtractionForAccessibility() throws {
        let pdf = try encrypted(Compliance.PDF_UA_1,
                Passwords().setUserPassword("hello").setOwnerPassword("world"),
                Permissions().grant(UserAccess.PRINT))
        let access = try accessValue(pdf)
        #expect(access == -3388)
        #expect(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.isSetIn(Permissions(access).getAccess()))
    }

    @Test func aCryptFilterThatIsAnObjectOfItsOwnIsFollowed() {
        // The /CF dictionary and the filter in it as objects of their own, as
        // pdf.js tests them in issue7665: the method was not found, and the
        // streams and the strings of the PDF were left encrypted.
        func object(_ number: Int, _ raw: String) -> PDFobj {
            let obj = PDFobj()
            obj.dict = raw.split(separator: " ").map(String.init)
            obj.number = number
            return obj
        }
        let objects = [
            object(7, "7 0 obj << /StdCF 8 0 R >> endobj"),
            object(8, "8 0 obj << /AuthEvent /DocOpen /CFM /AESV3 /Length 32 >> endobj"),
            object(9, "9 0 obj << /StdCF << /CFM /AESV2 >> >> endobj"),
        ]
        let cases: [(String, Decryptor.Method)] = [
            ("7 0 R", .aes256),
            ("9 0 R", .aes128),
            ("<< /StdCF 8 0 R >>", .aes256),
            ("<< /StdCF << /CFM /V2 >> >>", .rc4),
            ("99 0 R", .identity),
        ]
        for (cf, want) in cases {
            let encrypt = object(6, "6 0 obj << /Filter /Standard /V 5 /CF " + cf + " /StmF /StdCF >> endobj")
            #expect(Decryptor.getCryptMethod(encrypt, "/StdCF", objects) == want, "/CF \(cf)")
        }
    }
    // A stamp's stream is encrypted as every other stream of an encrypted
    // document, and reads back as it was drawn (review of 11 October 2026, #1).
    @Test func encryptsTheStreamOfAStamp() throws {
        let memory = MemoryPDF(Compliance.PDF_1_7)
        _ = memory.pdf.setEncryption(Encryption(memory.pdf,
                Passwords().setUserPassword("hello").setOwnerPassword("world"),
                Permissions().grant(UserAccess.PRINT)))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let stamp = Stamp(memory.pdf)
        stamp.fillRect(0, 0, 20, 10)
        try stamp.complete()
        stamp.drawOn(page)
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes, "hello")
        let form = try #require(objects.last { $0.getValue("/Subtype") == "/Form" }, "no form XObject")
        let data = TestSupport.latin1(form.getData())
        #expect(data.contains("20 0 l"), "the stamp's stream reads back as \(data)")
    }

    // A password is prepared with SASLprep, as PDF 2.0 asks of AES-256: the
    // PDF opens with an accented letter typed as one character or as a letter
    // and an accent, with a full-width letter or its ASCII one, and a password
    // of a character PDF 2.0 does not allow is refused (review of 11 October
    // 2026, #27).
    @Test func thePasswordIsPreparedWithSASLprep() throws {
        for (set, open) in [
            ("cafe\u{301}", "caf\u{E9}"),
            ("caf\u{E9}", "cafe\u{301}"),
            ("\u{FF30}\u{FF21}\u{FF33}\u{FF33}", "PASS"),
            ("I\u{AD}X", "\u{2168}"),
        ] {
            let pdf = try encrypted(set, "owner")
            #expect(title(try TestSupport.read(pdf, open)) == "Encrypted title")
            #expect(title(try TestSupport.read(pdf, set)) == "Encrypted title")
        }
        let memory = MemoryPDF(Compliance.PDF_1_7)
        _ = Encryption(memory.pdf, Passwords().setUserPassword("bell\u{7}"), Permissions())
        let refused = #expect(throws: (any Error).self) { try memory.pdf.complete() }
        #expect(TestSupport.message(refused).contains("U+0007"))
    }

    // A PDF whose password was taken as it was, before 9.0.5 and by other
    // writers, still opens with it: the password's own bytes are tried after
    // the prepared ones.
    @Test func aPasswordTakenAsItWasStillOpens() {
        let got = Decryptor.aes256Passwords("cafe\u{301}")
        #expect(got == [Array("caf\u{E9}".utf8), Array("cafe\u{301}".utf8)])
        #expect(Decryptor.aes256Passwords("ascii").count == 1)
    }
}
