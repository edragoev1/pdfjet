/*
 * EmbeddedFile.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Used to embed file objects.
/// The file objects must added to the PDF before drawing on the first page.
/// </summary>
public class EmbeddedFile {
    internal int objNumber = -1;
    internal String fileName = null;
    // The PDF the file is embedded in.
    internal PDF pdf;
    // How the file relates to the document, for a file of a document of
    // PDF/A-3, and null for a file that is only attached to a page.
    internal Relationship? relationship;
    // What the file holds, like "text/xml", or null.
    internal String mediaType;

    /// <summary>Embeds the file with the specified name into the PDF, compressed with Flate when compress is true.</summary>
    public EmbeddedFile(PDF pdf, String fileName, bool compress) :
        this(pdf, fileName.Substring(fileName.LastIndexOf('/') + 1),
                new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read)), compress) {
    }

    /// <summary>Embeds a file read from the stream into the PDF under the specified name.</summary>
    public EmbeddedFile(PDF pdf, String fileName, Stream stream, bool compress) :
        this(pdf, fileName, stream, compress, null, null, null) {
    }

    /// <summary>
    /// Embeds the file and says what it holds, how it relates to the document
    /// and what it is: the media type, such as "text/xml", the relationship and
    /// the description, in the words of a person. A document of PDF/A-3 needs
    /// all three of them for each file it carries, and so does a document that
    /// carries the XML of an invoice. PDF.AddAssociatedFile adds the embedded
    /// file to the document.
    /// </summary>
    public EmbeddedFile(PDF pdf, String fileName, Stream stream, bool compress,
            String mediaType, Relationship? relationship, String description) {
        this.pdf = pdf;
        this.fileName = fileName;
        this.relationship = relationship;
        this.mediaType = mediaType;
        byte[] buf = Content.GetFromStream(stream);
        // The file is the PDF's once it is read: one that could not be read
        // was left in the list, with no object (review of 17 October 2026)
        pdf.embeddedFiles.Add(this);
        int size = buf.Length;

        if (compress) {
            buf = Compressor.Deflate(buf);
        }

        if (pdf.encryption != null) {
            buf = AES256.Encrypt(buf, pdf.encryption.GetKey());
        }

        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /EmbeddedFile\n");
        if (!String.IsNullOrEmpty(mediaType)) {
            // What the file holds, as a name: /text#2Fxml for "text/xml".
            pdf.Append("/Subtype ");
            pdf.Append(ToName(mediaType));
            pdf.Append(Token.Newline);
            // The size before compression and the date, which PDF/A-3 asks
            // for. The date is the one the document itself carries, since the
            // file is written as the document is.
            pdf.Append("/Params <</Size ");
            pdf.Append(size);
            pdf.Append(" /ModDate ");
            pdf.AppendByteString(Encoding.ASCII.GetBytes(pdf.GetDate()));
            pdf.Append(">>\n");
        }
        if (compress) {
            pdf.Append("/Filter /FlateDecode\n");
        }
        pdf.Append(Token.Length);
        pdf.Append(buf.Length);
        pdf.Append(Token.Newline);
        pdf.Append(Token.EndDictionary);
        pdf.Append(Token.Stream);
        pdf.Append(buf);
        pdf.Append(Token.EndStream);
        pdf.EndObj();

        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /Filespec\n");

        // /UF is the file name as a text string, the one that readers of PDF
        // 1.7 use, and /F is the file specification string that older readers
        // know, a string of bytes (ISO 32000-2, 7.11.3, Table 43); PDF/A-3
        // requires both.
        pdf.Append("/F ");
        pdf.AppendByteString(FileSpecBytes(fileName));
        pdf.Append("\n");
        pdf.Append("/UF ");
        pdf.AppendTextString(fileName);
        pdf.Append("\n");

        if (relationship != null) {
            pdf.Append("/AFRelationship /");
            pdf.Append(relationship.Value.ToPDFName());
            pdf.Append(Token.Newline);
        }
        if (description != null && description.Length > 0) {
            pdf.Append("/Desc ");
            pdf.AppendTextString(description);
            pdf.Append("\n");
        }

        pdf.Append("/EF <</F ");
        pdf.Append(pdf.GetObjNumber() - 1);
        pdf.Append(" 0 R /UF ");
        pdf.Append(pdf.GetObjNumber() - 1);
        pdf.Append(" 0 R>>\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        this.objNumber = pdf.GetObjNumber();
    }

    // The file name as the bytes of /F, a file specification string of one
    // component, the file name (ISO 32000-2, 7.11.2): a solidus in the name
    // is preceded by a reverse solidus, which tells it from a separator of
    // components, so that "in/out" is "in\/out"; a reverse solidus in the
    // name is an underscore, as a reader removes each reverse solidus and
    // 7.11.2 gives no way to write one that stays, so that "C:\temp\a.txt"
    // is "C:_temp_a.txt"; each other character from space to tilde (0x20 to
    // 0x7E) is as it is, and each other one, one Unicode code point, an
    // underscore, so that "résumé.txt" is "r_sum_.txt". The full name is in
    // /UF.
    internal static byte[] FileSpecBytes(String fileName) {
        List<byte> bytes = new List<byte>(fileName.Length);
        for (int i = 0; i < fileName.Length; i++) {
            char ch = fileName[i];
            if (ch == '/') {
                bytes.Add((byte) '\\');
                bytes.Add((byte) '/');
            } else if (ch == '\\') {
                bytes.Add((byte) '_');
            } else if (ch >= 0x20 && ch <= 0x7E) {
                bytes.Add((byte) ch);
            } else {
                // A pair of surrogates is one code point.
                if (Char.IsHighSurrogate(ch) && i + 1 < fileName.Length &&
                        Char.IsLowSurrogate(fileName[i + 1])) {
                    i++;
                }
                bytes.Add((byte) '_');
            }
        }
        return bytes.ToArray();
    }

    // The media type as a name of PDF: the characters a name cannot hold
    // written as a number sign and two hexadecimal digits, so that "text/xml"
    // is /text#2Fxml.
    private static String ToName(String mediaType) {
        StringBuilder sb = new StringBuilder("/");
        foreach (byte b in Encoding.UTF8.GetBytes(mediaType)) {
            int ch = b & 0xFF;
            if (ch > 0x20 && ch < 0x7F && "()<>[]{}/%#".IndexOf((char) ch) == -1) {
                sb.Append((char) ch);
            } else {
                sb.Append('#').AppendFormat(CultureInfo.InvariantCulture, "{0:X2}", ch);
            }
        }
        return sb.ToString();
    }

    /// <summary>Returns the name of the embedded file.</summary>
    public String GetFileName() {
        return fileName;
    }
}   // End of EmbeddedFile.cs
}   // End of namespace PDFjet.NET
