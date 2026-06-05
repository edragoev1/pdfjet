/**
 * Content.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Some really useful helper methods.
///
public class Content {
    public static func ofTextFile( _ fileName: String) throws -> String {
        let contents = try String(contentsOfFile: fileName, encoding: String.Encoding.utf8)
        var buffer = String()
        for scalar in contents.unicodeScalars {
            if scalar != "\r" {
                buffer.append(String(scalar))
            }
        }
        return buffer
    }

    public static func ofBinaryFile( _ fileName: String) throws -> [UInt8] {
        return try getFromStream(InputStream(fileAtPath: fileName)!)
    }

    public static func getFromStream( _ stream: InputStream, _ bufferSize: Int) throws -> [UInt8] {
        var contents = [UInt8]()
        var buffer = [UInt8](repeating: 0, count: bufferSize)
        stream.open()
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: bufferSize)
            if (read == 0) {
                break
            }
            contents.append(contentsOf: buffer[0..<read])
        }
        stream.close()
        return contents
    }

    public static func getFromStream( _ stream: InputStream) throws -> [UInt8] {
        try self.getFromStream(stream, 4096)
    }
}   // End of Content.swift
