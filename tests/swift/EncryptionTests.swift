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
        _ = memory.pdf.setTitle("Encrypted title")
        _ = memory.pdf.setEncryption(Encryption(memory.pdf, passwords, permissions))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(Font(memory.pdf, CoreFont.HELVETICA), "Secret text").setLocation(50, 50).drawOn(page)
        try memory.pdf.complete()
        return memory.bytes
    }

    private func encrypted(_ user: String, _ owner: String) throws -> [UInt8] {
        return try encrypted(Compliance.PDF_1_7,
                Passwords().setUserPassword(user).setOwnerPassword(owner),
                Permissions().grant(UserAccess.PRINT.getValue()))
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

    @Test func permissionsAreANegativeNumberWithTheReservedBitsSet() throws {
        #expect(try accessValue(try encrypted("hello", "world")) == -3900)
    }

    @Test func pdfUaGrantsExtractionForAccessibility() throws {
        let pdf = try encrypted(Compliance.PDF_UA_1,
                Passwords().setUserPassword("hello").setOwnerPassword("world"),
                Permissions().grant(UserAccess.PRINT.getValue()))
        let access = try accessValue(pdf)
        #expect(access == -3388)
        #expect(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.isSetIn(access))
    }
}
