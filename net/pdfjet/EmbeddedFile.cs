/*
 * EmbeddedFile.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
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

    /// <summary>Embeds the file with the specified name into the PDF, compressed with Flate when compress is true.</summary>
    public EmbeddedFile(PDF pdf, String fileName, bool compress) :
        this(pdf, fileName.Substring(fileName.LastIndexOf("/") + 1),
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
        byte[] buf = Content.GetFromStream(stream);
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
        if (mediaType != null) {
            // What the file holds, as a name: /text#2Fxml for "text/xml".
            pdf.Append("/Subtype ");
            pdf.Append(ToName(mediaType));
            pdf.Append(Token.Newline);
            // The size before compression and the date, which PDF/A-3 asks
            // for. The date is the one the document itself carries, since the
            // file is written as the document is.
            pdf.Append("/Params <</Size ");
            pdf.Append(size);
            pdf.Append(" /ModDate (");
            pdf.Append(pdf.GetDate());
            pdf.Append(")>>\n");
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

        // The file name as a text string, which every reader decodes the same
        // way. /UF is the name that readers of PDF 1.7 look for first, and /F
        // is the one that older readers know; PDF/A-3 requires both.
        pdf.Append("/F ");
        pdf.AppendTextString(fileName);
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
                sb.Append('#').AppendFormat("{0:X2}", ch);
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
