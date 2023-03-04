/**
 *  EmbeddedFile.cs
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
