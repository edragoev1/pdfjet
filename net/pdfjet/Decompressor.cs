/*
 * Decompressor.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.IO.Compression;

namespace PDFjet.NET {
class Decompressor {
    /// <summary>
    /// Decodes the data of an LZWDecode stream, with the default EarlyChange
    /// of 1: the codes get one bit longer one code before the table needs it.
    /// Data that ends without the end code, or with an invalid code, returns
    /// what was decoded up to there, as a missing end is common in real files.
    /// </summary>
    internal static byte[] LZWDecode(byte[] data) {
        using var bos = new MemoryStream(data.Length * 2);
        byte[][] table = new byte[4096][];
        for (int i = 0; i < 256; i++) {
            table[i] = new byte[] {(byte) i};
        }
        int next = 258;         // 256 clears the table and 257 ends the data.
        int codeLength = 9;
        int bits = 0;           // Only the low bitCount bits are still unread.
        int bitCount = 0;
        byte[] previous = null;
        foreach (byte b in data) {
            bits = (bits << 8) | b;
            bitCount += 8;
            while (bitCount >= codeLength) {
                bitCount -= codeLength;
                int code = (bits >> bitCount) & ((1 << codeLength) - 1);
                if (code == 256) {
                    next = 258;
                    codeLength = 9;
                    previous = null;
                    continue;
                }
                byte[] entry;
                if (code < next && code != 257 && table[code] != null) {
                    entry = table[code];
                } else if (code == next && previous != null) {
                    entry = new byte[previous.Length + 1];
                    Array.Copy(previous, entry, previous.Length);
                    entry[previous.Length] = previous[0];
                } else {
                    return bos.ToArray();       // The end code or an invalid one.
                }
                bos.Write(entry, 0, entry.Length);
                if (previous != null && next < 4096) {
                    byte[] added = new byte[previous.Length + 1];
                    Array.Copy(previous, added, previous.Length);
                    added[previous.Length] = entry[0];
                    table[next++] = added;
                }
                previous = entry;
                if (next + 1 >= (1 << codeLength) && codeLength < 12) {
                    codeLength++;
                }
            }
        }
        return bos.ToArray();
    }

    internal static byte[] Inflate(byte[] data) {
        using var outStream = new MemoryStream();
        using var inStream = new MemoryStream(data);
        using var zlib = new ZLibStream(inStream, CompressionMode.Decompress);
        zlib.CopyTo(outStream);
        return outStream.ToArray();
    }
}   // End of Decompressor.cs
}   // End of package PDFjet.NET