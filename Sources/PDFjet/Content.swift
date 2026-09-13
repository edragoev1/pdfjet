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
    /// Returns the contents of the specified text file, without carriage returns.
    public static func ofTextFile( _ fileName: String) throws -> String {
        // Bytes that are not valid UTF-8 are replaced with U+FFFD, as the
        // Java, C# and Go readers do.
        let contents = String(decoding: try ofBinaryFile(fileName), as: UTF8.self)
        var buffer = String.UnicodeScalarView()
        for scalar in contents.unicodeScalars {
            if scalar != "\r" {
                buffer.append(scalar)
            }
        }
        // A byte order mark at the start of the file is not part of the text.
        if buffer.first == "\u{FEFF}" {
            buffer.removeFirst()
        }
        return String(buffer)
    }

    /// Returns the contents of the specified file as bytes.
    public static func ofBinaryFile( _ fileName: String) throws -> [UInt8] {
        guard let stream = InputStream(fileAtPath: fileName) else {
            throw PDFjetError(message: "Cannot open file: " + fileName)
        }
        return try getFromStream(stream)
    }

    /// Returns all the bytes read from the stream, reading bufferSize bytes at a time.
    public static func getFromStream( _ stream: InputStream, _ bufferSize: Int) throws -> [UInt8] {
        var contents = [UInt8]()
        var buffer = [UInt8](repeating: 0, count: bufferSize)
        stream.open()
        defer { stream.close() }
        // A stream that cannot be opened, like one of a missing file, is in
        // the error state, and so is one that fails while it is read.
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: bufferSize)
            if read <= 0 {
                break
            }
            contents.append(contentsOf: buffer[0..<read])
        }
        if stream.streamStatus == .error {
            throw stream.streamError ?? PDFjetError(message: "Cannot read the stream.")
        }
        return contents
    }

    /// Returns all the bytes read from the stream.
    public static func getFromStream( _ stream: InputStream) throws -> [UInt8] {
        try self.getFromStream(stream, 4096)
    }
}   // End of Content.swift
