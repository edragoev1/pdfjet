/**
 * Encryption.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Encrypts a PDF with 256-bit AES as defined in ISO 32000-2 (PDF 2.0).
/// Pass the encryption object to PDF.setEncryption.
///
/// Please see Example_30.
///
public class Encryption {
    private let fileEncryptionKey: [UInt8]
    private let objNumber: Int

    ///
    /// Creates a new encryption dictionary and adds it to the PDF.
    ///
    /// - Parameter pdf: the PDF to encrypt.
    /// - Parameter passwords: the user and owner passwords.
    /// - Parameter permissions: the permissions granted to the user.
    ///
    public init(_ pdf: PDF, _ passwords: Passwords, _ permissions: Permissions) {
        // A random 256-bit (32-byte) file encryption key.
        fileEncryptionKey = Cryptography.randomBytes(32)

        let userPassword = Encryption.toBytes(passwords.getUserPassword())
        let ownerPassword = Encryption.toBytes(passwords.getOwnerPassword())

        // Algorithm 8: the U and UE values.
        let userValidationSalt = Cryptography.randomBytes(8)
        let userKeySalt = Cryptography.randomBytes(8)
        let u = Cryptography.hash2B(userPassword, userValidationSalt, []) +
                userValidationSalt + userKeySalt
        let ue = Encryption.encryptKey(fileEncryptionKey,
                Cryptography.hash2B(userPassword, userKeySalt, []))

        // Algorithm 9: the O and OE values.
        let ownerValidationSalt = Cryptography.randomBytes(8)
        let ownerKeySalt = Cryptography.randomBytes(8)
        let o = Cryptography.hash2B(ownerPassword, ownerValidationSalt, u) +
                ownerValidationSalt + ownerKeySalt
        let oe = Encryption.encryptKey(fileEncryptionKey,
                Cryptography.hash2B(ownerPassword, ownerKeySalt, u))

        // The flags specifying which operations shall be permitted, with the
        // reserved bits 7, 8 and 13 to 32 set as ISO 32000-2 Table 22 requires,
        // so the value is negative.
        let p = Int(Int32(bitPattern: UInt32(permissions.getRawValue()) | 0xFFFFF0C0))

        // Algorithm 10: the Perms value.
        var perms = [UInt8](repeating: 0xFF, count: 8)   // P extended to 64 bits
        for i in 0..<4 {
            perms[i] = UInt8(truncatingIfNeeded: p >> (8 * i))    // Little-endian
        }
        perms += Array("Fadb".utf8)         // 'F' for EncryptMetadata false
        perms += Cryptography.randomBytes(4)
        let encryptedPerms = Cryptography.aesEncryptBlock(perms, fileEncryptionKey)

        // The encryption dictionary.
        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Filter /Standard\n")
        pdf.append("/V 5\n")            // Algorithm 2.A / 2.B
        pdf.append("/R 6\n")            // Security revision 6 (strong password hashing)
        pdf.append("/CF <<\n")
        pdf.append("/StdCF <<\n")
        pdf.append("/CFM /AESV3\n")     // AESV3 = AES-256 in CBC
        pdf.append("/Length 32\n")      // 32 bytes = 256-bit file key
        pdf.append("/AuthEvent /DocOpen\n")
        pdf.append(">>\n")
        pdf.append(">>\n")
        pdf.append("/StmF /StdCF\n")
        pdf.append("/StrF /StdCF\n")

        pdf.append("/U <")              // User key
        pdf.append(Encryption.toHex(u))
        pdf.append(">\n")

        pdf.append("/O <")              // Owner key
        pdf.append(Encryption.toHex(o))
        pdf.append(">\n")

        pdf.append("/UE <")             // User encryption key
        pdf.append(Encryption.toHex(ue))
        pdf.append(">\n")

        pdf.append("/OE <")             // Owner encryption key
        pdf.append(Encryption.toHex(oe))
        pdf.append(">\n")

        pdf.append("/EncryptMetadata false\n")

        // A set of flags specifying which operations shall be permitted
        pdf.append("/P ")
        pdf.append(p)
        pdf.append("\n")

        pdf.append("/Perms <")
        pdf.append(Encryption.toHex(encryptedPerms))
        pdf.append(">\n")

        pdf.append(Token.endDictionary)
        pdf.endobj()

        objNumber = pdf.getObjNumber()
    }

    ///
    /// Returns the randomly generated file encryption key.
    ///
    /// - Returns: the 32-byte file encryption key.
    ///
    public func getKey() -> [UInt8] {
        return fileEncryptionKey
    }

    ///
    /// Returns the object number of the encryption dictionary.
    ///
    /// - Returns: the object number.
    ///
    public func getObjNumber() -> Int {
        return objNumber
    }

    // Encrypts the file encryption key with the hash of a password, with a zero
    // IV and no padding, which gives the UE or OE value.
    private static func encryptKey(_ key: [UInt8], _ hash: [UInt8]) -> [UInt8] {
        return Cryptography.aesEncryptCBC(key, hash, [UInt8](repeating: 0, count: 16))
    }

    // Returns the UTF-8 bytes of a password, at most 127 of them.
    private static func toBytes(_ password: String) -> [UInt8] {
        let bytes = Array(password.utf8)
        return bytes.count > 127 ? Array(bytes[0..<127]) : bytes
    }

    private static func toHex(_ bytes: [UInt8]) -> String {
        let digits = Array("0123456789abcdef".utf8)
        var hex = [UInt8]()
        hex.reserveCapacity(2 * bytes.count)
        for byte in bytes {
            hex.append(digits[Int(byte >> 4)])
            hex.append(digits[Int(byte & 0xF)])
        }
        return String(decoding: hex, as: UTF8.self)
    }
}   // End of Encryption.swift
