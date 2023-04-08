/**
 *  Content.swift
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
public class Content {
    public static func ofTextFile( _ fileName: String) throws -> String {
        return try String(contentsOfFile: fileName, encoding: String.Encoding.utf8)
    }

    public static func ofTextFile( _ fileName: String) throws -> [UInt8] {
        var content = [UInt8]()
        let stream = InputStream(fileAtPath: fileName)!
        stream.open()
        let bufferSize = 4096
        var buffer = [UInt8](repeating: 0, count: bufferSize)
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: bufferSize)
            if (read == 0) {
                break
            }
            content.append(contentsOf: buffer[0..<read])
        }
        stream.close()
        return content
    }

    public static func ofInputStream( _ stream: InputStream) throws -> [UInt8] {
        var content = [UInt8]()
        stream.open()
        let bufferSize = 4096
        var buffer = [UInt8](repeating: 0, count: bufferSize)
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: bufferSize)
            if (read == 0) {
                break
            }
            content.append(contentsOf: buffer[0..<read])
        }
        stream.close()
        return content
    }
}   // End of Content.swift
