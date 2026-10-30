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
        // A missing file is told before a stream is opened: the error of a
        // stream on Linux has Foundation look for its networking library,
        // which logs the symbols it does not find.
        guard FileManager.default.fileExists(atPath: fileName),
                let stream = InputStream(fileAtPath: fileName) else {
            throw PDFjetError(message: "Cannot open file: " + fileName)
        }
        return try getFromStream(stream)
    }

    /// Returns all the bytes read from the stream, reading bufferSize bytes at a time.
    public static func getFromStream( _ stream: InputStream, _ bufferSize: Int) throws -> [UInt8] {
        // The pieces as they are read, joined once at the end: an array grown
        // as it is read is copied each time it doubles (28 October 2026).
        var pieces = [[UInt8]]()
        var total = 0
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
            pieces.append(Array(buffer[0..<read]))
            total += read
        }
        if stream.streamStatus == .error {
            throw stream.streamError ?? PDFjetError(message: "Cannot read the stream.")
        }
        if pieces.count == 1 {
            return pieces[0]
        }
        var contents = [UInt8]()
        contents.reserveCapacity(total)
        for piece in pieces {
            contents.append(contentsOf: piece)
        }
        return contents
    }

    /// Returns all the bytes read from the stream.
    public static func getFromStream( _ stream: InputStream) throws -> [UInt8] {
        // 64 KB at a time: a font of 200 KB in 4 KB reads was 14% of the time
        // of a page in three fonts, most of it in the reads (10 October 2026)
        try self.getFromStream(stream, 65536)
    }

    /// Returns the lines of a UTF-8 text file, without a byte order mark and
    /// without the line separators. An empty line is an empty string.
    public static func linesOfTextFile(_ fileName: String) throws -> [String] {
        var lines = [String]()
        let contents = try ofTextFile(fileName)
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
}   // End of Content.swift
