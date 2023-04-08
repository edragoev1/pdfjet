/**
 *  Content.cs
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
using System.Text;

namespace PDFjet.NET {
public class Content {
    public static String OfTextFile(String fileName) {
        StringBuilder sb = new StringBuilder(2048);
        BufferedStream stream = new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read));
        StreamReader reader = new StreamReader(stream);
        char[] buffer = new char[4096];
        int count = 0;
        while ((count = reader.Read(buffer, 0, buffer.Length)) > 0) {
            sb.Append(buffer, 0, count);
        }
        reader.Close();
        stream.Close();
        return sb.ToString();
    }

    public static byte[] OfBinaryFile(String fileName) {
        MemoryStream ms = new MemoryStream();
        BufferedStream stream = null;
        try {
            stream = new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            byte[] buffer = new byte[4096];
            int count = 0;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
                ms.Write(buffer, 0, count);
            }
        } finally {
            stream.Close();
        }
        return ms.ToArray();
    }
}   // End of Content.cs
}   // End of namespace PDFjet.NET
