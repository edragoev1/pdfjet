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
public class Util {
    ///
    /// Reads the lines of a UTF-8 text file, without carriage returns.
    ///
    /// - Parameter filePath: the path of the text file.
    /// - Returns: the lines.
    ///
    public static func readLines(_ filePath: String) throws -> [String] {
        var lines = [String]()
        let contents = try Content.ofTextFile(filePath)
        var buffer = String()
        for scalar in contents.unicodeScalars {
            if scalar == "\n" {
                lines.append(buffer)
                buffer = ""
            } else {
                buffer.append(String(scalar))
            }
        }
        if !buffer.isEmpty {
            lines.append(buffer)
        }
        return lines
    }
}   // End of Util.swift
