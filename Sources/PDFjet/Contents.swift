/**
 *  Contents.swift
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
import Foundation

///
/// Some really useful helper methods.
///
public class Contents {
    public static func ofTextFile( _ fileName: String) throws -> String {
        let contents = try String(contentsOfFile: fileName, encoding: String.Encoding.utf8)
        var buffer = String()
        for scalar in contents.unicodeScalars {
            if scalar == "\r" {
                continue
            }
            buffer.append(String(scalar))
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
}   // End of Contents.swift
