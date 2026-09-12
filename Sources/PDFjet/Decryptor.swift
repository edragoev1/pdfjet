/**
 * Decryptor.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// The error when an encrypted PDF cannot be decrypted.
///
enum DecryptorError: Error, CustomStringConvertible {
    case notSupported(String)

    var description: String {
        switch self {
        case .notSupported(let message):
            return message
        }
    }
}

///
/// Decrypts the strings and streams of a PDF that is encrypted with the
/// standard security handler and an empty user password, like PDFs that open
/// without a password but restrict printing or copying. RC4 and AES-128
/// (revisions 2 to 4) and AES-256 (revisions 5 and 6) are supported.
///
final class Decryptor {
    private static let passwordPadding: [UInt8] = [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41,
        0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80,
        0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A,
    ]

    // The methods of the crypt filters.
    private enum Method {
        case identity
        case rc4
        case aes128
        case aes256
    }

    /// The object number of the encryption dictionary.
    let objNumber: Int
    private let key: [UInt8]
    private let streamMethod: Method
    private let stringMethod: Method
    private let encryptMetadata: Bool

    ///
    /// Returns the decryptor of the PDF, or nil when it is not encrypted. The
    /// trailer is the cross-reference stream object when there is no trailer.
    ///
    static func getDecryptor(_ trailer: PDFobj?, _ objects: [PDFobj]) throws -> Decryptor? {
        guard let trailer = trailer,
                let i = trailer.dict.firstIndex(of: "/Encrypt"),
                i + 1 < trailer.dict.count else {
            return nil
        }
        var found: PDFobj?
        if trailer.dict[i + 1] == "<<" {
            // The dictionary is in the trailer, so there is no object to skip.
            let inline = PDFobj()
            inline.number = -1
            var level = 0
            for j in (i + 1)..<trailer.dict.count {
                let token = trailer.dict[j]
                inline.dict.append(token)
                if token == "<<" {
                    level += 1
                } else if token == ">>" {
                    level -= 1
                    if level == 0 {
                        break
                    }
                }
            }
            found = inline
        } else {
            for obj in objects where String(obj.number) == trailer.dict[i + 1] {
                found = obj         // The last one is the newest.
            }
        }
        guard let encrypt = found else {
            throw DecryptorError.notSupported("The encryption dictionary of the PDF was not found.")
        }
        var id = [UInt8]()
        if let k = trailer.dict.firstIndex(of: "/ID"), k + 2 < trailer.dict.count,
                trailer.dict[k + 1] == "[" {
            id = toBytes(trailer.dict[k + 2])
        }
        return try Decryptor(encrypt, id)
    }

    private init(_ encrypt: PDFobj, _ id: [UInt8]) throws {
        if encrypt.getValue("/Filter") != "/Standard" {
            throw DecryptorError.notSupported(
                    "The security handler of the PDF is not supported: " + encrypt.getValue("/Filter"))
        }
        let v = Decryptor.getInt(encrypt, "/V")
        let r = Decryptor.getInt(encrypt, "/R")
        let encryptMetadata = encrypt.getValue("/EncryptMetadata") != "false"
        if v == 1 || v == 2 {
            self.streamMethod = .rc4
            self.stringMethod = .rc4
        } else if v == 4 || v == 5 {
            self.streamMethod = Decryptor.getMethod(encrypt, encrypt.getValue("/StmF"))
            self.stringMethod = Decryptor.getMethod(encrypt, encrypt.getValue("/StrF"))
        } else {
            throw DecryptorError.notSupported("The encryption of the PDF is not supported: /V \(v)")
        }
        let u = Decryptor.toBytes(encrypt.getValue("/U"))
        if r == 5 || r == 6 {
            self.key = try Decryptor.getKey(r, u, Decryptor.toBytes(encrypt.getValue("/UE")))
        } else if r >= 2 && r <= 4 {
            let length = (v == 1 || r == 2) ? 5 : (v == 4) ? 16 : Decryptor.getInt(encrypt, "/Length") / 8
            self.key = try Decryptor.getKey(r, max(5, min(length, 16)),
                    Decryptor.toBytes(encrypt.getValue("/O")), u,
                    Decryptor.getInt(encrypt, "/P"), id, encryptMetadata)
        } else {
            throw DecryptorError.notSupported("The encryption of the PDF is not supported: /R \(r)")
        }
        self.objNumber = encrypt.number
        self.encryptMetadata = encryptMetadata
    }

    // Returns the method of the crypt filter with the name in the /CF dictionary.
    private static func getMethod(_ encrypt: PDFobj, _ name: String) -> Method {
        let dict = encrypt.dict
        guard let cf = dict.firstIndex(of: "/CF") else {
            return .identity
        }
        var level = 0
        var i = cf + 1
        while i < dict.count {
            let token = dict[i]
            if token == "<<" {
                level += 1
            } else if token == ">>" {
                level -= 1
                if level == 0 {
                    break
                }
            } else if level == 1 && token == name {
                var j = i + 1
                while j + 1 < dict.count && dict[j] != ">>" {
                    if dict[j] == "/CFM" {
                        switch dict[j + 1] {
                        case "/V2":
                            return .rc4
                        case "/AESV2":
                            return .aes128
                        case "/AESV3":
                            return .aes256
                        default:
                            return .identity
                        }
                    }
                    j += 1
                }
                break
            }
            i += 1
        }
        return .identity            // Like /Identity, the default.
    }

    // Algorithm 2 of ISO 32000-2 computes the key of revisions 2 to 4 from the
    // password, which is checked against /U with algorithms 4 and 5.
    private static func getKey(
            _ r: Int,
            _ length: Int,
            _ o: [UInt8],
            _ u: [UInt8],
            _ p: Int,
            _ id: [UInt8],
            _ encryptMetadata: Bool) throws -> [UInt8] {
        var input = passwordPadding + o
        input += [UInt8(truncatingIfNeeded: p), UInt8(truncatingIfNeeded: p >> 8),
                UInt8(truncatingIfNeeded: p >> 16), UInt8(truncatingIfNeeded: p >> 24)]
        input += id
        if r >= 4 && !encryptMetadata {
            input += [0xFF, 0xFF, 0xFF, 0xFF]
        }
        var hash = Cryptography.md5(input)
        if r >= 3 {
            for _ in 0..<50 {
                hash = Cryptography.md5(Array(hash[0..<length]))
            }
        }
        let key = Array(hash[0..<length])
        var check: [UInt8]
        let n: Int
        if r == 2 {
            check = Cryptography.rc4(key, passwordPadding)
            n = 32
        } else {
            check = Cryptography.rc4(key, Cryptography.md5(passwordPadding + id))
            for i in 1...19 {
                check = Cryptography.rc4(key.map { $0 ^ UInt8(i) }, check)
            }
            n = 16
        }
        if u.count < n || check[0..<n] != u[0..<n] {
            throw DecryptorError.notSupported(
                    "The PDF can only be opened with a password, which is not supported.")
        }
        return key
    }

    // Algorithms 2.A and 11 of ISO 32000-2 get the key of revisions 5 and 6
    // from /UE, after checking the password against /U.
    private static func getKey(_ r: Int, _ u: [UInt8], _ ue: [UInt8]) throws -> [UInt8] {
        if u.count < 48 || ue.count < 32 {
            throw DecryptorError.notSupported("The encryption dictionary of the PDF is not valid.")
        }
        if getHash(r, Array(u[32..<40])) != Array(u[0..<32]) {
            throw DecryptorError.notSupported(
                    "The PDF can only be opened with a password, which is not supported.")
        }
        return Cryptography.aesDecryptCBC(
                Array(ue[0..<32]), getHash(r, Array(u[40..<48])), [UInt8](repeating: 0, count: 16))
    }

    // Returns the hash of the empty password and the salt, which is SHA-256 in
    // revision 5, and algorithm 2.B of ISO 32000-2 in revision 6.
    private static func getHash(_ r: Int, _ salt: [UInt8]) -> [UInt8] {
        if r == 5 {
            return Cryptography.sha256(salt)
        }
        return Cryptography.hash2B([], salt, [])
    }

    ///
    /// Decrypts the strings in the dictionary of the object, which become
    /// hexadecimal strings.
    ///
    func decryptStrings(_ obj: PDFobj) {
        if stringMethod == .identity {
            return
        }
        let digits = Array("0123456789abcdef")
        for i in 0..<obj.dict.count {
            let token = obj.dict[i]
            if token.hasPrefix("(") || (token.hasPrefix("<") && token != "<<") {
                var hex = "<"
                for b in decrypt(Decryptor.toBytes(token), stringMethod, obj) {
                    hex.append(digits[Int(b >> 4)])
                    hex.append(digits[Int(b & 0x0F)])
                }
                hex.append(">")
                obj.dict[i] = hex
            }
        }
    }

    ///
    /// Returns the decrypted stream of the object.
    ///
    func decryptStream(_ obj: PDFobj, _ stream: [UInt8]) -> [UInt8] {
        if !encryptMetadata && obj.getValue("/Type") == "/Metadata" {
            return stream
        }
        return decrypt(stream, streamMethod, obj)
    }

    // Algorithm 1 of ISO 32000-2 decrypts with a key for each object in
    // revisions 2 to 4, and revisions 5 and 6 use the file key.
    private func decrypt(_ data: [UInt8], _ method: Method, _ obj: PDFobj) -> [UInt8] {
        if method == .identity {
            return data
        }
        var objectKey = key
        if method != .aes256 {
            let number = obj.number
            let generation = (obj.dict.count > 1) ? (Int(obj.dict[1]) ?? 0) : 0
            var input = key
            input += [UInt8(truncatingIfNeeded: number), UInt8(truncatingIfNeeded: number >> 8),
                    UInt8(truncatingIfNeeded: number >> 16),
                    UInt8(truncatingIfNeeded: generation), UInt8(truncatingIfNeeded: generation >> 8)]
            if method == .aes128 {
                input += [0x73, 0x41, 0x6C, 0x54]       // "sAlT"
            }
            objectKey = Array(Cryptography.md5(input)[0..<min(key.count + 5, 16)])
        }
        if method == .rc4 {
            return Cryptography.rc4(objectKey, data)
        }
        // The data starts with the initialization vector, and is padded to whole blocks.
        if data.count < 32 {
            return []
        }
        let decrypted = Cryptography.aesDecryptCBC(
                Array(data[16..<(16 + (data.count - 16) / 16 * 16)]), objectKey, Array(data[0..<16]))
        let padding = Int(decrypted[decrypted.count - 1])
        if padding >= 1 && padding <= 16 {
            return Array(decrypted[0..<(decrypted.count - padding)])
        }
        return decrypted
    }

    // Returns the integer value of the key, or 0. /P can be written as an
    // unsigned number.
    private static func getInt(_ obj: PDFobj, _ key: String) -> Int {
        if let value = Int64(obj.getValue(key)) {
            return Int(Int32(truncatingIfNeeded: value))
        }
        return 0
    }

    ///
    /// Returns the bytes of a literal string like (a\)b) or of a hexadecimal
    /// string like <612962>. Each character of the token is a byte.
    ///
    static func toBytes(_ token: String) -> [UInt8] {
        let chars = token.unicodeScalars.map { UInt8(truncatingIfNeeded: $0.value) }
        var bytes = [UInt8]()
        bytes.reserveCapacity(chars.count)
        if chars.first == 0x3C {            // "<"
            var high = -1
            for c in chars.dropFirst() {
                let digit = hexValue(c)
                if digit == -1 {
                    continue
                }
                if high == -1 {
                    high = digit
                } else {
                    bytes.append(UInt8(high << 4 | digit))
                    high = -1
                }
            }
            if high != -1 {
                bytes.append(UInt8(high << 4))
            }
        } else if chars.first == 0x28 {     // "("
            let end = (chars.last == 0x29) ? chars.count - 1 : chars.count
            var i = 1
            while i < end {
                let c = chars[i]
                if c == 0x5C && i + 1 < end {   // "\\"
                    i += 1
                    let escaped = chars[i]
                    switch escaped {
                    case 0x6E:                  // "n"
                        bytes.append(0x0A)
                    case 0x72:                  // "r"
                        bytes.append(0x0D)
                    case 0x74:                  // "t"
                        bytes.append(0x09)
                    case 0x62:                  // "b"
                        bytes.append(0x08)
                    case 0x66:                  // "f"
                        bytes.append(0x0C)
                    case 0x30...0x37:           // An octal number of up to three digits
                        var value = Int(escaped - 0x30)
                        var n = 1
                        while n < 3 && i + 1 < end && chars[i + 1] >= 0x30 && chars[i + 1] <= 0x37 {
                            i += 1
                            value = value * 8 + Int(chars[i] - 0x30)
                            n += 1
                        }
                        bytes.append(UInt8(truncatingIfNeeded: value))
                    case 0x0D:
                        // A backslash at the end of a line continues the string.
                        if i + 1 < end && chars[i + 1] == 0x0A {
                            i += 1
                        }
                    case 0x0A:
                        break
                    default:
                        bytes.append(escaped)   // Like \( \) and \\
                    }
                } else if c == 0x0D {
                    // An end of line in a string is a line feed.
                    if i + 1 < end && chars[i + 1] == 0x0A {
                        i += 1
                    }
                    bytes.append(0x0A)
                } else {
                    bytes.append(c)
                }
                i += 1
            }
        }
        return bytes
    }
}
