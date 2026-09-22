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
    // The identity of the PDF the file is embedded in.
    var pdfIdentity: UUID?
    // How the file relates to the document, for a file of a document of
    // PDF/A-3, and nil for a file that is only attached to a page.
    var relationship: Relationship?

    /// Embeds the file at the specified path into the PDF, compressed with Flate when compress is true.
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
        guard let stream = InputStream(fileAtPath: filePath),
                FileManager.default.fileExists(atPath: filePath) else {
            throw EmbeddedFileError.fileNotFound(filePath)
        }
        try self.init(pdf, fileName, stream, compress)
    }

    /// Embeds a file read from the stream into the PDF under the specified name.
    public convenience init(
            _ pdf: PDF,
            _ fileName: String,
            _ stream: InputStream,
            _ compress: Bool) throws {
        try self.init(pdf, fileName, stream, compress, nil, nil, nil)
    }

    ///
    /// Embeds the file and says what it holds and how it relates to the
    /// document. A document of PDF/A-3 needs all three of them for each file it
    /// carries, and so does a document that carries the XML of an invoice.
    /// PDF.addAssociatedFile adds the embedded file to the document.
    ///
    /// - Parameter pdf: the PDF.
    /// - Parameter fileName: the file name, such as "factur-x.xml".
    /// - Parameter stream: the input stream.
    /// - Parameter compress: true to compress the file with Flate.
    /// - Parameter mediaType: what the file holds, such as "text/xml", or nil.
    /// - Parameter relationship: how the file relates to the document, or nil.
    /// - Parameter description: what the file is, in the words of a person, or nil.
    ///
    public init(
            _ pdf: PDF,
            _ fileName: String,
            _ stream: InputStream,
            _ compress: Bool,
            _ mediaType: String?,
            _ relationship: Relationship?,
            _ description: String?) throws {
        self.pdfIdentity = pdf.identity
        self.fileName = fileName
        self.relationship = relationship
        var buf = try Content.getFromStream(stream)
        let size = buf.count
        if compress {
            var buf2 = [UInt8]()
            FlateEncode(&buf2, buf)
            buf = buf2
        }
        buf = pdf.encrypted(buf)

        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /EmbeddedFile\n")
        if let mediaType = mediaType {
            // What the file holds, as a name: /text#2Fxml for "text/xml".
            pdf.append("/Subtype ")
            pdf.append(EmbeddedFile.toName(mediaType))
            pdf.append(Token.newline)
            // The size before compression and the date, which PDF/A-3 asks
            // for. The date is the one the document itself carries, since the
            // file is written as the document is.
            pdf.append("/Params <</Size ")
            pdf.append(size)
            pdf.append(" /ModDate (")
            pdf.append(pdf.getDate())
            pdf.append(")>>\n")
        }
        if compress {
            pdf.append("/Filter /FlateDecode\n")
        }
        pdf.append(Token.length)
        pdf.append(buf.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf)
        pdf.append(Token.endStream)
        pdf.endObj()

        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /Filespec\n")
        // The file name as a text string, which every reader decodes the same
        // way. /UF is the name that readers of PDF 1.7 look for first, and /F
        // is the one that older readers know; PDF/A-3 requires both.
        pdf.append("/F <")
        pdf.append(pdf.textString(fileName))
        pdf.append(">\n")
        pdf.append("/UF <")
        pdf.append(pdf.textString(fileName))
        pdf.append(">\n")

        if let relationship = relationship {
            pdf.append("/AFRelationship ")
            pdf.append(relationship.rawValue)
            pdf.append(Token.newline)
        }
        if let description = description, !description.isEmpty {
            pdf.append("/Desc <")
            pdf.append(pdf.textString(description))
            pdf.append(">\n")
        }

        pdf.append("/EF <</F ")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R /UF ")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R>>\n")
        pdf.append(Token.endDictionary)
        pdf.endObj()

        self.objNumber = pdf.getObjNumber()
    }

    // The media type as a name of PDF: the characters a name cannot hold
    // written as a number sign and two hexadecimal digits, so that "text/xml"
    // is /text#2Fxml.
    private static func toName(_ mediaType: String) -> String {
        let digits = Array("0123456789ABCDEF")
        let delimiters = Array("()<>[]{}/%#".utf8)
        var name = "/"
        for ch in mediaType.utf8 {
            if ch > 0x20 && ch < 0x7F && !delimiters.contains(ch) {
                name.append(Character(Unicode.Scalar(ch)))
            } else {
                name.append("#")
                name.append(digits[Int(ch >> 4)])
                name.append(digits[Int(ch & 0x0F)])
            }
        }
        return name
    }

    /// Returns the name of the embedded file.
    public func getFileName() -> String {
        return self.fileName!
    }
}   // End of EmbeddedFile.swift

enum EmbeddedFileError: Error {
    case fileNotFound(String)
}
