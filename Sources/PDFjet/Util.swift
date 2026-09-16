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
    /// A file that cannot be read this way is refused rather than guessed at.
    ///
    static func split(_ line: String, _ delimiter: String) -> [String] {
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
                        fatalError("A quoted field is not closed on this line of the data file: "
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
                    fatalError("A quoted field is followed by text on this line of the data file: "
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

    // The start of the line, for the message of a file that cannot be read.
    private static func excerpt(_ line: String) -> String {
        return (line.count <= 60) ? line : (String(line.prefix(60)) + "...")
    }

}   // End of Util.swift
