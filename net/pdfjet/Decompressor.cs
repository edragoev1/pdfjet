/**
 * Decompressor.cs
 *
©2025 PDFjet Software

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
using System.IO;
using System.IO.Compression;

namespace PDFjet.NET {
class Decompressor {
    internal static byte[] inflate(byte[] data) {
        MemoryStream outStream = new MemoryStream();
        MemoryStream inStream = new MemoryStream(data, 2, data.Length - 6);
        DeflateStream dsStream = new DeflateStream(
                inStream, CompressionMode.Decompress, true);
        byte[] buf = new byte[4096];
        int count;
        while ((count = dsStream.Read(buf, 0, buf.Length)) > 0) {
            outStream.Write(buf, 0, count);
        }
        dsStream.Dispose();
        return outStream.ToArray();
    }
}   // End of Decompressor.cs
}   // End of package PDFjet.NET
