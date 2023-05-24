/**
 *  DeflaterOutputStream.cs
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
using System.IO;
using System.IO.Compression;

namespace PDFjet.NET {
public class DeflaterOutputStream {
    private MemoryStream buf1 = null;
    private MemoryStream buf2 = null;
    private DeflateStream ds1 = null;
    private const uint prime = 65521;

    public DeflaterOutputStream(MemoryStream buf1) {
        this.buf1 = buf1;
        this.buf2 = new MemoryStream();
        this.buf2.WriteByte(0x58);   // These are the correct values for
        this.buf2.WriteByte(0x85);   // CMF and FLG according to Microsoft
        this.ds1 = new DeflateStream(buf2, CompressionMode.Compress, true);
    }

    public void Write(byte[] buffer, int off, int len) {
        // Compress the data in the buffer
        ds1.Write(buffer, off, len);
        ds1.Dispose();
        buf2.WriteTo(buf1);

        // Calculate the Adler-32 checksum
        ulong s1 = 1L;
        ulong s2 = 0L;
        for (int i = 0; i < len; i++) {
            s1 = (s1 + buffer[off + i]) % prime;
            s2 = (s2 + s1) % prime;
        }
        appendAdler((s2 << 16) + s1);
    }

    private void appendAdler(ulong adler) {
        buf1.WriteByte((byte) (adler >> 24));
        buf1.WriteByte((byte) (adler >> 16));
        buf1.WriteByte((byte) (adler >>  8));
        buf1.WriteByte((byte) (adler));
        buf1.Flush();
    }
}   // End of DeflaterOutputStream.cs
}   // End of package PDFjet.NET
