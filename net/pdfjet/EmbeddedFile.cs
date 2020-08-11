/**
 *  EmbeddedFile.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.IO;


namespace PDFjet.NET {
/**
 *  Used to embed file objects.
 *  The file objects must added to the PDF before drawing on the first page.
 *
 */
public class EmbeddedFile {

    internal int objNumber = -1;
    internal String fileName = null;


    public EmbeddedFile(PDF pdf, String fileName, Stream stream, bool compress) {
        this.fileName = fileName;

        MemoryStream baos = new MemoryStream();
        byte[] buf = new byte[4096];
        int number;
        while ((number = stream.Read(buf, 0, buf.Length)) > 0) {
            baos.Write(buf, 0, number);
        }
        stream.Dispose();

        if (compress) {
            buf = baos.ToArray();
            baos = new MemoryStream();
            DeflaterOutputStream dos = new DeflaterOutputStream(baos);
            dos.Write(buf, 0, buf.Length);
        }

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /EmbeddedFile\n");
        if (compress) {
            pdf.Append("/Filter /FlateDecode\n");
        }
        pdf.Append("/Length ");
        pdf.Append(baos.Length);
        pdf.Append("\n");
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(baos);
        pdf.Append("\nendstream\n");
        pdf.Endobj();

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Filespec\n");
        pdf.Append("/F (");
        pdf.Append(fileName);
        pdf.Append(")\n");
        pdf.Append("/EF <</F ");
        pdf.Append(pdf.GetObjNumber() - 1);
        pdf.Append(" 0 R>>\n");
        pdf.Append(">>\n");
        pdf.Endobj();

        this.objNumber = pdf.GetObjNumber();
    }


    public String GetFileName() {
        return fileName;
    }

}   // End of EmbeddedFile.cs
}   // End of namespace PDFjet.NET
