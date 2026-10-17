/*
 * CryptographyTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// The Swift port's own ciphers and hashes, which Foundation does not have on
/// Linux, checked against the known answers of their standards: MD5 against
/// RFC 1321, SHA-256, SHA-384 and SHA-512 against FIPS 180-4, AES against
/// FIPS 197 and NIST SP 800-38A, and RC4 against RFC 6229 and the usual test
/// strings. The other ports use their platform's; these say that Swift's agree
/// with the standards, not only with themselves (9.1.1).
@Suite struct CryptographyTests {
    private func hex(_ bytes: [UInt8]) -> String {
        return bytes.map { ($0 < 16 ? "0" : "") + String($0, radix: 16) }.joined()
    }

    private func bytes(_ hex: String) -> [UInt8] {
        var result = [UInt8]()
        var index = hex.startIndex
        while index < hex.endIndex {
            let next = hex.index(index, offsetBy: 2)
            result.append(UInt8(hex[index..<next], radix: 16)!)
            index = next
        }
        return result
    }

    @Test func md5OfRFC1321() {
        let answers: [(String, String)] = [
            ("", "d41d8cd98f00b204e9800998ecf8427e"),
            ("a", "0cc175b9c0f1b6a831c399e269772661"),
            ("abc", "900150983cd24fb0d6963f7d28e17f72"),
            ("message digest", "f96b697d7cb7938d525a2f31aaf161d0"),
            ("abcdefghijklmnopqrstuvwxyz", "c3fcd3d76192e4007dfb496cca67e13b"),
            ("12345678901234567890123456789012345678901234567890123456789012345678901234567890",
             "57edf4a22be3c955ac49da2e2107b67a"),
        ]
        for (message, digest) in answers {
            #expect(hex(Cryptography.md5(Array(message.utf8))) == digest, "MD5 of \(message)")
        }
    }

    @Test func sha2OfFIPS180() {
        let two = "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq"
        #expect(hex(Cryptography.sha256([])) ==
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")
        #expect(hex(Cryptography.sha256(Array("abc".utf8))) ==
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")
        #expect(hex(Cryptography.sha256(Array(two.utf8))) ==
            "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1")
        // A million a's: the padding of many blocks
        #expect(hex(Cryptography.sha256([UInt8](repeating: 0x61, count: 1_000_000))) ==
            "cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0")
        #expect(hex(Cryptography.sha384(Array("abc".utf8))) ==
            "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7")
        #expect(hex(Cryptography.sha512(Array("abc".utf8))) ==
            "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f")
    }

    @Test func aesBlocksOfFIPS197() {
        let plain = bytes("00112233445566778899aabbccddeeff")
        // Appendix C.1 and C.3: AES-128 and AES-256
        #expect(hex(Cryptography.aesEncryptBlock(plain, bytes("000102030405060708090a0b0c0d0e0f"))) ==
            "69c4e0d86a7b0430d8cdb78070b4c55a")
        #expect(hex(Cryptography.aesEncryptBlock(plain,
            bytes("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"))) ==
            "8ea2b7ca516745bfeafc49904b496089")
    }

    @Test func aesCBCOfSP800_38A() {
        let iv = bytes("000102030405060708090a0b0c0d0e0f")
        let plain = bytes("6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e51")
        // F.2.1 and F.2.2: CBC-AES128
        let key128 = bytes("2b7e151628aed2a6abf7158809cf4f3c")
        let cipher128 = "7649abac8119b246cee98e9b12e9197d5086cb9b507219ee95db113a917678b2"
        #expect(hex(Cryptography.aesEncryptCBC(plain, key128, iv)) == cipher128)
        #expect(Cryptography.aesDecryptCBC(bytes(cipher128), key128, iv) == plain)
        // F.2.5 and F.2.6: CBC-AES256
        let key256 = bytes("603deb1015ca71be2b73aef0857d77811f352c073b6108d72d9810a30914dff4")
        let cipher256 = "f58c4c04d6e5f1ba779eabfb5f7bfbd69cfc4e967edb808d679f777bc6702c7d"
        #expect(hex(Cryptography.aesEncryptCBC(plain, key256, iv)) == cipher256)
        #expect(Cryptography.aesDecryptCBC(bytes(cipher256), key256, iv) == plain)
    }

    @Test func rc4OfRFC6229AndTheTestStrings() {
        // RFC 6229: the first 16 bytes of the key stream of the key 0102030405
        #expect(hex(Cryptography.rc4(bytes("0102030405"), [UInt8](repeating: 0, count: 16))) ==
            "b2396305f03dc027ccc3524a0a1118a8")
        let answers: [(String, String, String)] = [
            ("Key", "Plaintext", "bbf316e8d940af0ad3"),
            ("Wiki", "pedia", "1021bf0420"),
            ("Secret", "Attack at dawn", "45a01f645fc35b383552544b9bf5"),
        ]
        for (key, plain, cipher) in answers {
            #expect(hex(Cryptography.rc4(Array(key.utf8), Array(plain.utf8))) == cipher, "RC4 of \(plain)")
        }
    }
}
