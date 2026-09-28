/**
 * Util.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Utility methods.
///
class Util {
    /// Returns the color as a 0xRRGGBB value, each component rounded to the nearest
    /// of 256 steps and kept between 0.0 and 1.0, or -1 if the color is nil.
    static func toPackedRGB(_ color: [Float]?) -> Int32 {
        guard let color else {
            return -1
        }
        return (toByte(color[0]) << 16) | (toByte(color[1]) << 8) | toByte(color[2])
    }

    private static func toByte(_ component: Float) -> Int32 {
        let value = max(0.0, min(1.0, component))
        return Int32((value * 255.0 + 0.5).rounded(.down))   // Rounds half up, as Java's Math.round
    }

    /// Returns the red, green and blue components, from 0.0 to 1.0, of a 0xRRGGBB color.
    static func toRGB(_ color: Int32) -> [Float] {
        return [Float((color >> 16) & 0xff)/255.0, Float((color >> 8) & 0xff)/255.0, Float(color & 0xff)/255.0]
    }

    ///
    /// Splits one line of a delimited data file into its fields, as RFC 4180
    /// reads them: a field that starts with a quote runs to the closing quote,
    /// a doubled quote inside it stands for one quote, and a delimiter inside
    /// it is part of the text. A field that does not start with a quote keeps
    /// any quotes it holds. The quotes around a field are not part of it.
    /// A file that cannot be read this way is refused rather than guessed at:
    /// it throws the error that says why.
    ///
    static func split(_ line: String, _ delimiter: String) throws -> [String] {
        return try split(line, delimiter, open: false)!
    }

    // The most lines a record may take, so that a quote that is never closed
    // fails instead of reading the rest of the file into one field.
    static let maxLinesInRecord = 10000

    ///
    /// Returns the fields of the record of a delimited data file that starts with the line.
    /// When a quoted field holds line breaks, the record goes on over the lines that nextLine
    /// returns, and each line break in the field is a space, as a table cell is drawn on one line.
    /// Only a record of several lines is looked at for them, so a line costs nothing more to
    /// read. nextLine returns nil at the end of the file. A file that cannot be read this way
    /// throws the error that says why.
    ///
    static func readRecord(_ line: String, _ delimiter: String, _ nextLine: () -> String?) throws -> [String] {
        if let fields = try split(line, delimiter, open: true) {
            return fields
        }
        // Each line is read once, to see whether the record ends on it, and
        // the record is split when it does: a record whose every line closes
        // a quoted field and opens another was split again at each of its
        // lines, which took time that grew with the square of its lines.
        var record = line
        var lines = 1
        while true {
            guard let next = nextLine() else {
                throw PDFjetError(message:
                        "A quoted field is not closed by the end of the data file: " + Util.excerpt(record))
            }
            lines += 1
            if lines > maxLinesInRecord {
                throw PDFjetError(message:
                        "A quoted field is not closed within \(maxLinesInRecord) lines of the data file: "
                        + Util.excerpt(record))
            }
            record += "\n"
            record += next
            // The record ends on the line that leaves no quoted field open.
            if !endsInQuotedField(next, delimiter), let fields = try split(record, delimiter, open: true) {
                return fields.map { lineBreaksToSpaces($0) }
            }
        }
    }

    // Returns true when the line, which starts inside a quoted field, ends
    // inside one: that field, or one that opens after it. A quote closes the
    // field unless it is one of a doubled pair, and a quote opens a field when
    // it starts one, after a delimiter.
    static func endsInQuotedField(_ line: String, _ delimiter: String) -> Bool {
        if delimiter.isEmpty {
            return false
        }
        let bytes = Array(line.utf8)
        let separator = Array(delimiter.utf8)
        let quote = UInt8(ascii: "\"")
        var quoted = true
        var fieldStart = false
        var i = 0
        while i < bytes.count {
            if quoted {
                guard let next = Util.indexOf(bytes, quote, i) else {
                    return true
                }
                i = next + 1
                if i < bytes.count && bytes[i] == quote {
                    i += 1                          // Two quotes stand for one
                } else {
                    quoted = false
                    fieldStart = false
                }
            } else if fieldStart && bytes[i] == quote {
                quoted = true
                i += 1
            } else {
                while i < bytes.count && !Util.startsWith(bytes, separator, i) {
                    i += 1
                }
                if i == bytes.count {
                    return false
                }
                i += separator.count
                fieldStart = true
            }
        }
        return quoted
    }

    ///
    /// Returns the text with each line break, "\r\n", "\r" or "\n", replaced by a space, for a
    /// table cell that is drawn on one line.
    ///
    static func lineBreaksToSpaces(_ text: String) -> String {
        let utf8 = text.utf8
        if !utf8.contains(where: { $0 == 10 || $0 == 13 }) {
            return text
        }
        var bytes = [UInt8]()
        bytes.reserveCapacity(utf8.count)
        var previous: UInt8 = 0
        for byte in utf8 {
            if !(byte == 10 && previous == 13) {    // "\r\n" is one space
                bytes.append((byte == 10 || byte == 13) ? 32 : byte)
            }
            previous = byte
        }
        return String(decoding: bytes, as: UTF8.self)
    }

    // With open, a line that ends inside a quoted field returns nil, for
    // readRecord to read on; without it the line is refused.
    private static func split(_ line: String, _ delimiter: String, open: Bool) throws -> [String]? {
        if delimiter.isEmpty {
            return [line]
        }
        // The line is read as its UTF-8 bytes, which is where components(
        // separatedBy:) was: a quote is ASCII, and a UTF-8 sequence, the
        // delimiter included, matches only where a character starts. A field
        // is decoded from its bytes; the one further copy is of a quoted field
        // that holds a doubled quote. BigTable reads every line of its file
        // through here.
        let bytes = Array(line.utf8)
        let separator = Array(delimiter.utf8)
        let quote = UInt8(ascii: "\"")
        var fields = [String]()
        var i = 0
        while true {
            if i < bytes.count && bytes[i] == quote {
                i += 1                              // The quote that opens the field
                let start = i
                while true {
                    guard let next = Util.indexOf(bytes, quote, i) else {
                        if open {
                            return nil
                        }
                        throw PDFjetError(message: "A quoted field is not closed on this line of the data file: "
                                + Util.excerpt(line))
                    }
                    i = next + 1
                    if i < bytes.count && bytes[i] == quote {
                        i += 1                      // Two quotes stand for one; the closing quote is the first single one
                    } else {
                        break                       // The quote that closes the field
                    }
                }
                // The text between the quotes, where every quote is doubled.
                let field = String(decoding: bytes[start..<(i - 1)], as: UTF8.self)
                fields.append(field.contains("\"\"") ? field.replacingOccurrences(of: "\"\"", with: "\"") : field)
                if i < bytes.count && !Util.startsWith(bytes, separator, i) {
                    throw PDFjetError(message: "A quoted field is followed by text on this line of the data file: "
                            + Util.excerpt(line))
                }
            } else {
                var end = i
                while end < bytes.count && !Util.startsWith(bytes, separator, end) {
                    end += 1
                }
                fields.append(String(decoding: bytes[i..<end], as: UTF8.self))
                i = end
            }
            if i == bytes.count {
                break
            }
            i += separator.count                    // Step over the delimiter
            if i == bytes.count {                   // The line ends on a delimiter
                fields.append("")
                break
            }
        }
        return fields
    }

    private static func indexOf(_ bytes: [UInt8], _ byte: UInt8, _ from: Int) -> Int? {
        var i = from
        while i < bytes.count {
            if bytes[i] == byte {
                return i
            }
            i += 1
        }
        return nil
    }

    private static func startsWith(_ bytes: [UInt8], _ separator: [UInt8], _ at: Int) -> Bool {
        if at + separator.count > bytes.count {
            return false
        }
        for k in 0..<separator.count where bytes[at + k] != separator[k] {
            return false
        }
        return true
    }

    ///
    /// Returns the whole part of the value as Java casts a float to an int: NaN is 0, and a
    /// value past the range of a 32-bit int is the end of the range, where Int(_:) would trap.
    /// An infinite count of lines, from a division by a leading of 0, is then as many lines
    /// as there can be.
    ///
    static func saturatingInt(_ value: Float) -> Int {
        if value.isNaN {
            return 0
        }
        if value >= Float(Int32.max) {
            return Int(Int32.max)
        }
        if value <= Float(Int32.min) {
            return Int(Int32.min)
        }
        return Int(value)
    }

    // The start of the line, for the message of a file that cannot be read.
    private static func excerpt(_ line: String) -> String {
        return (line.count <= 60) ? line : (String(line.prefix(60)) + "...")
    }

}

///
/// The lines of a data file, read as UTF-8, after the byte order mark at its start, if there
/// is one, with the bytes that are not valid UTF-8 replaced with U+FFFD, as the Java and C#
/// readers do. A line ends at a line feed, a carriage return or the two together, as Java's
/// readLine ends it, so that a file splits into the same lines in every port, and a file that
/// ends with a line break has no empty line after it. The file is read a block at a time,
/// from where the last line ended, so that a line is looked at once.
///
final class DataFileLines {
    private let file: FileHandle
    private var buffer = [UInt8]()
    private var offset = 0          // Where the next line starts in the buffer
    private var scanned = 0         // How far the buffer has no line break, from offset
    private var atStart = true
    private var atEnd = false

    init(_ fileName: String) throws {
        self.file = try FileHandle(forReadingFrom: URL(fileURLWithPath: fileName))
    }

    deinit {
        file.closeFile()
    }

    /// Returns the next line, without its line break, or nil at the end of the file.
    func next() -> String? {
        while true {
            var i = max(offset, scanned)
            while i < buffer.count && buffer[i] != 10 && buffer[i] != 13 {
                i += 1
            }
            if i < buffer.count && !(buffer[i] == 13 && i + 1 == buffer.count && !atEnd) {
                // Decoded as the other ports do: String(data:encoding:) would
                // drop a byte order mark at the start of every line, and skip a
                // line that is not valid UTF-8 where the others draw U+FFFD.
                let line = String(decoding: buffer[offset..<i], as: UTF8.self)
                var end = i + 1
                if buffer[i] == 13 && end < buffer.count && buffer[end] == 10 {
                    end += 1
                }
                offset = end
                scanned = end
                return line
            }
            scanned = i         // A line feed may follow a carriage return in the next block
            if atEnd {
                // The last line, without a line break
                if offset == buffer.count {
                    return nil
                }
                let line = String(decoding: buffer[offset...], as: UTF8.self)
                offset = buffer.count
                return line
            }
            let chunk = file.readData(ofLength: 65536)
            if chunk.isEmpty {
                atEnd = true
                continue
            }
            if offset > 0 {
                buffer.removeSubrange(0..<offset)
                scanned -= offset
                offset = 0
            }
            buffer.append(contentsOf: chunk)
            if atStart {
                // A byte order mark at the start of the file is not part of the text.
                if buffer.starts(with: [0xEF, 0xBB, 0xBF]) {
                    buffer.removeSubrange(0..<3)
                    scanned = 0
                }
                atStart = false
            }
        }
    }
}   // End of Util.swift
