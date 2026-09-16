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
        let characters = Array(line)
        let separator = Array(delimiter)
        var fields = [String]()
        var field = [Character]()
        var i = 0
        while true {
            if i < characters.count && characters[i] == "\"" {
                i += 1                              // The quote that opens the field
                while true {
                    guard let quote = Util.indexOf(characters, "\"", i) else {
                        fatalError("A quoted field is not closed on this line of the data file: "
                                + Util.excerpt(line))
                    }
                    field.append(contentsOf: characters[i..<quote])
                    i = quote + 1
                    if i < characters.count && characters[i] == "\"" {
                        field.append("\"")          // Two quotes stand for one
                        i += 1
                    } else {
                        break                       // The quote that closes the field
                    }
                }
                if i < characters.count && !Util.startsWith(characters, separator, i) {
                    fatalError("A quoted field is followed by text on this line of the data file: "
                            + Util.excerpt(line))
                }
            } else {
                var end = i
                while end < characters.count && !Util.startsWith(characters, separator, end) {
                    end += 1
                }
                field.append(contentsOf: characters[i..<end])
                i = end
            }
            fields.append(String(field))
            field.removeAll(keepingCapacity: true)
            if i == characters.count {
                break
            }
            i += separator.count                    // Step over the delimiter
            if i == characters.count {              // The line ends on a delimiter
                fields.append("")
                break
            }
        }
        return fields
    }

    private static func indexOf(_ characters: [Character], _ ch: Character, _ from: Int) -> Int? {
        var i = from
        while i < characters.count {
            if characters[i] == ch {
                return i
            }
            i += 1
        }
        return nil
    }

    private static func startsWith(_ characters: [Character], _ separator: [Character], _ at: Int) -> Bool {
        if at + separator.count > characters.count {
            return false
        }
        for k in 0..<separator.count where characters[at + k] != separator[k] {
            return false
        }
        return true
    }

    // The start of the line, for the message of a file that cannot be read.
    private static func excerpt(_ line: String) -> String {
        return (line.count <= 60) ? line : (String(line.prefix(60)) + "...")
    }

}   // End of Util.swift
