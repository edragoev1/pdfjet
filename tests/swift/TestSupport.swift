/**
 * TestSupport.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Helpers shared by the unit tests. The tests read the PngSuite images and
/// the fonts from the repository root, found by walking up from this file.
enum TestSupport {
    /// The tolerance for coordinates and widths, a hundredth of a point.
    static let delta: Float = 0.01

    static let root: URL = {
        var url = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
        while url.path != "/" {
            if FileManager.default.fileExists(atPath: url.appendingPathComponent("PngSuite").path) {
                return url
            }
            url = url.deletingLastPathComponent()
        }
        return URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
    }()

    static func path(_ relative: String) -> String {
        return root.appendingPathComponent(relative).path
    }

    static func exists(_ relative: String) -> Bool {
        return FileManager.default.fileExists(atPath: path(relative))
    }

    static func open(_ relative: String) -> InputStream {
        return InputStream(fileAtPath: path(relative))!
    }

    static func newPDF() -> PDF {
        return PDF(OutputStream(toMemory: ()))
    }

    static func helvetica(_ pdf: PDF) -> Font {
        return try! Font(pdf, CoreFont.HELVETICA) // The number is valid.
    }

    static func latin1(_ bytes: [UInt8]) -> String {
        var scalars = String.UnicodeScalarView()
        for byte in bytes {
            scalars.append(Unicode.Scalar(byte))
        }
        return String(scalars)
    }

    static func bytes(_ text: String) -> [UInt8] {
        return Array(text.utf8)
    }

    /// Returns the content stream of the page, which is not compressed yet.
    static func content(_ page: Page) -> String {
        return latin1(page.getContent())
    }

    /// Returns the text as a core font draws it: a byte per character in upper case hexadecimal.
    static func hex(_ text: String) -> String {
        let digits = Array("0123456789ABCDEF")
        var result = ""
        for scalar in text.unicodeScalars {
            let byte = Int(scalar.value) & 0xFF
            result.append(digits[byte >> 4])
            result.append(digits[byte & 0x0F])
        }
        return result
    }

    /// Decodes a PDF text string written as a hexadecimal string with a UTF-16BE byte order mark.
    static func utf16Hex(_ value: String) -> String {
        let digits = value.unicodeScalars.filter { !"<> \n\r\t".unicodeScalars.contains($0) }.map { Character($0) }
        var units = [UInt16]()
        var i = 0
        while i + 3 < digits.count {
            units.append(UInt16(String(digits[i..<i + 4]), radix: 16)!)
            i += 4
        }
        if units.first == 0xFEFF {
            units.removeFirst()
        }
        return String(decoding: units, as: UTF16.self)
    }

    static func read(_ pdf: [UInt8]) throws -> [PDFobj] {
        return try PDF().read(from: InputStream(data: Data(pdf)))
    }

    static func read(_ pdf: [UInt8], _ password: String) throws -> [PDFobj] {
        return try PDF().read(from: InputStream(data: Data(pdf)), password: password)
    }

    static func pageObjects(_ objects: [PDFobj]) -> [PDFobj] {
        return PDF().getPageObjects(from: objects)
    }

    /// Returns the first /ID of the trailer.
    static func trailerID(_ pdf: [UInt8]) -> String {
        let raw = latin1(pdf)
        let start = raw.range(of: "/ID[<", options: .backwards)!.upperBound
        let end = raw[start...].firstIndex(of: ">")!
        return String(raw[start..<end])
    }

    /// Returns the object that holds the key, or nil.
    static func findObject(_ objects: [PDFobj], _ key: String) -> PDFobj? {
        return objects.first { !$0.getValue(key).isEmpty }
    }

    static func inflate(_ data: [UInt8]) throws -> [UInt8] {
        var output = [UInt8]()
        var input = data
        _ = try Puff(output: &output, input: &input)
        return output
    }

    static func deflate(_ data: [UInt8]) -> [UInt8] {
        var output = [UInt8]()
        FlateEncode(&output, data)
        return output
    }

    static func crc32(_ data: [UInt8]) -> String {
        var crc: UInt32 = 0xFFFFFFFF
        for byte in data {
            crc ^= UInt32(byte)
            for _ in 0..<8 {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1
            }
        }
        let value = String(crc ^ 0xFFFFFFFF, radix: 16)
        return String(repeating: "0", count: 8 - value.count) + value
    }

    /// Deterministic pseudo-random bytes.
    static func randomBytes(_ count: Int, _ seed: UInt64) -> [UInt8] {
        var state = seed
        var result = [UInt8](repeating: 0, count: count)
        for i in 0..<count {
            state = state &* 6364136223846793005 &+ 1442695040888963407
            result[i] = UInt8(truncatingIfNeeded: state >> 33)
        }
        return result
    }

    static func expectNear(_ expected: Float, _ actual: Float, _ tolerance: Float = delta,
            _ comment: Comment? = nil, sourceLocation: SourceLocation = #_sourceLocation) {
        #expect(abs(expected - actual) <= tolerance, "expected \(expected), got \(actual) \(comment?.description ?? "")",
                sourceLocation: sourceLocation)
    }

    static func expectXY(_ x: Float, _ y: Float, _ xy: [Float]?, sourceLocation: SourceLocation = #_sourceLocation) {
        guard let xy = xy, xy.count >= 2 else {
            Issue.record("no corner", sourceLocation: sourceLocation)
            return
        }
        expectNear(x, xy[0], delta, "x", sourceLocation: sourceLocation)
        expectNear(y, xy[1], delta, "y", sourceLocation: sourceLocation)
    }

    static func expectRGB(_ r: Float, _ g: Float, _ b: Float, _ rgb: [Float]?, sourceLocation: SourceLocation = #_sourceLocation) {
        guard let rgb = rgb, rgb.count == 3 else {
            Issue.record("not an RGB color: \(String(describing: rgb))", sourceLocation: sourceLocation)
            return
        }
        expectNear(r, rgb[0], 0.0001, "red", sourceLocation: sourceLocation)
        expectNear(g, rgb[1], 0.0001, "green", sourceLocation: sourceLocation)
        expectNear(b, rgb[2], 0.0001, "blue", sourceLocation: sourceLocation)
    }

    static func message(_ error: (any Error)?) -> String {
        if let error = error as? PDFjetError {
            return error.message
        }
        return error.map { String(describing: $0) } ?? ""
    }
}

/// A PDF that writes to memory, with the bytes available after complete().
final class MemoryPDF {
    let stream: OutputStream
    let pdf: PDF

    init(_ compliance: Compliance = Compliance.PDF_1_7) {
        stream = OutputStream(toMemory: ())
        pdf = PDF(stream, compliance)
    }

    var bytes: [UInt8] {
        let data = stream.property(forKey: .dataWrittenToMemoryStreamKey) as? Data
        return data.map { [UInt8]($0) } ?? []
    }
}
