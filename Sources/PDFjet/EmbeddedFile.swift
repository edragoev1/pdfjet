/**
 * EmbeddedFile.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Used to embed file objects.
 * The file objects must added to the PDF before drawing on the first page.
 */
public class EmbeddedFile {
    var objNumber: Int = -1
    var fileName: String?

    public convenience init(
            _ pdf: PDF,
            _ filePath: String,
            _ compress: Compress) throws {
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
            _ compress: Compress) throws {
        self.fileName = fileName
        var buf = try Content.getFromStream(stream)
        if compress == Compress.YES {
            var buf2 = [UInt8]()
            FlateEncode(&buf2, buf)
            buf = buf2
        }

        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /EmbeddedFile\n")
        if compress == Compress.NO {
            pdf.append("/Filter /FlateDecode\n")
        }
        pdf.append(Token.length)
        pdf.append(buf.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf)
        pdf.append(Token.endStream)
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
