/*
 * Compressor.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.IO.Compression;

namespace PDFjet.NET {
class Compressor {
    internal static byte[] Deflate(byte[] data) {
        return Deflate(data, 0, data.Length);
    }

    // The zlib stream of no bytes, as .NET 8 writes it. .NET 10's ZLibStream
    // writes nothing at all for no bytes, which is no zlib stream, and the
    // stream of an empty page, a /FlateDecode stream of length 0, could not
    // be decoded.
    private static readonly byte[] EMPTY = { 0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01 };

    internal static byte[] Deflate(byte[] data, int off, int len) {
        if (len == 0) {
            return (byte[]) EMPTY.Clone();
        }
        // Page content usually compresses to less than an eighth of its size.
        using var ms = new MemoryStream(len / 8 + 64);
        using (var zlib = new ZLibStream(ms, CompressionMode.Compress, leaveOpen: true)) {
            zlib.Write(data, off, len);
        }
        return ms.ToArray();
    }

    // Deflates the samples of an image as they are written, to the bytes
    // Deflate makes of them written whole, so that an image is compressed a
    // row at a time, its samples never in memory whole (21 November 2026). It
    // tells whether every byte written was one value, as a soft mask all of
    // 255 is left out.
    internal sealed class ImageWriter : IDisposable {
        private readonly MemoryStream ms = new MemoryStream();
        private ZLibStream zlib;
        private long written = 0;
        private int same = -1;  // The one value of every byte, -1 for none yet, -2 for more

        internal ImageWriter() {
            zlib = new ZLibStream(ms, CompressionMode.Compress, leaveOpen: true);
        }

        // Deflates the bytes, which it does not keep.
        internal void Write(byte[] data) {
            written += data.Length;
            if (same != -2) {
                foreach (byte b in data) {
                    if (same == -1) {
                        same = b;
                    } else if (b != same) {
                        same = -2;
                        break;
                    }
                }
            }
            zlib.Write(data, 0, data.Length);
        }

        internal bool IsEmpty() {
            return written == 0;
        }

        // Whether bytes were written and every one of them was the value.
        internal bool AllAre(int value) {
            return written > 0 && same == value;
        }

        // Ends the stream and returns it.
        internal byte[] ToArray() {
            zlib.Dispose();
            zlib = null;
            if (written == 0) {
                return (byte[]) EMPTY.Clone();
            }
            return ms.ToArray();
        }

        public void Dispose() {
            zlib?.Dispose();
            zlib = null;
        }
    }
}   // End of Compressor.cs
}   // End of package PDFjet.NET
