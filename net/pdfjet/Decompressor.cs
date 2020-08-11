/**
 * Decompressor.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

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
