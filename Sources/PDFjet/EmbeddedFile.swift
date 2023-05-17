/**
 *  EmbeddedFile.swift
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

/**
 *  Used to embed file objects.
 *  The file objects must added to the PDF before drawing on the first page.
 */
public class EmbeddedFile {
    var objNumber: Int = -1
    var fileName: String?

    public convenience init(
            _ pdf: PDF,
            _ filePath: String,
            _ compress: Bool) throws {
        var fileName = ""
        for scalar in filePath.unicodeScalars {
            if scalar == "/" {
                fileName = ""
            } else {
                fileName += String(scalar)
            }
        }
        try self.init(pdf, fileName, InputStream(fileAtPath: filePath)!, compress)
    }

    public init(
            _ pdf: PDF,
            _ fileName: String,
            _ stream: InputStream,
            _ compress: Bool) throws {
        self.fileName = fileName
        var buf = try Contents.getFromStream(stream)
        if compress {
            var buf2 = [UInt8]()
            FlateEncode(&buf2, buf)
            // LZWEncode(&buf2, buf)
            buf = buf2
        }

        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /EmbeddedFile\n")
        if compress {
            pdf.append("/Filter /FlateDecode\n")
            // pdf.append("/Filter /LZWDecode\n")
        }
        pdf.append(Token.length)
        pdf.append(buf.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf)
        pdf.append(Token.endstream)
        pdf.endobj()

        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /Filespec\n")
        pdf.append("/F (")
        pdf.append(fileName)
        pdf.append(")\n")
        pdf.append("/EF <</F ")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R>>\n")
        pdf.append(Token.endDictionary)
        pdf.endobj()

        self.objNumber = pdf.getObjNumber()
    }

    public func getFileName() -> String {
        return self.fileName!
    }
}   // End of EmbeddedFile.swift
