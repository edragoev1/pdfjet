/**
 * EmbeddedFile.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/**
 * Used to embed file objects.
 * The file objects must added to the PDF before drawing on the first page.
 */
public class EmbeddedFile {
    internal int objNumber = -1;
    internal String fileName = null;

    public EmbeddedFile(PDF pdf, String fileName, Compress compress) :
        this(pdf, fileName.Substring(fileName.LastIndexOf("/") + 1),
                new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read)), compress) {
    }

    public EmbeddedFile(PDF pdf, String fileName, Stream stream, Compress compress) {
        this.fileName = fileName;
        byte[] buf = Content.GetFromStream(stream);

        if (compress == Compress.YES) {
            buf = Compressor.Deflate(buf);
        }

        if (pdf.encryption != null) {
            buf = AES256.Encrypt(buf, pdf.encryption.GetKey());
        }

        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /EmbeddedFile\n");
        if (compress == Compress.YES) {
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

        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        if (pdf.encryption != null) {
            fileNameBytes = AES256.Encrypt(fileNameBytes, pdf.encryption.GetKey());
        }
        pdf.Append("/F <");
        pdf.Append(Util.ToHexString(fileNameBytes));
        pdf.Append(">\n");

        pdf.Append("/EF <</F ");
        pdf.Append(pdf.GetObjNumber() - 1);
        pdf.Append(" 0 R>>\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        this.objNumber = pdf.GetObjNumber();
    }

    public String GetFileName() {
        return fileName;
    }
}   // End of EmbeddedFile.cs
}   // End of namespace PDFjet.NET
