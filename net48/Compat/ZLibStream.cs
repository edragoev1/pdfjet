/*
 * ZLibStream.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// ZLibStream for the .NET Framework 4.8 build, which has DeflateStream and not
// ZLibStream, as PDFjet's Compressor and Decompressor use it in .NET 8.
//
// Compressing: the zlib header, the data compressed by DeflateStream, and the
// Adler-32 of the data, written when the stream is disposed.
//
// Decompressing, as .NET's ZLibStream does for the Decompressor: the header
// checked, the Deflate data decoded by Puff, and the Adler-32 checked at the
// end, where a wrong one throws InvalidDataException; the input read exactly
// to the end of the zlib stream, so that the bytes after it are not read, and
// a stream cut short reads at the end of the input and returns what it decoded
// so far, without an error. What it decodes is at most the Decompressor's
// MAX_DECODED_LENGTH and one byte, which tells that the stream decodes to more.

using PDFjet.NET;

namespace System.IO.Compression {

internal sealed class ZLibStream : Stream {
    private readonly Stream inner;
    private readonly CompressionMode mode;
    private readonly bool leaveOpen;

    // Compressing: what is written, compressed when the stream is disposed
    private readonly MemoryStream written;

    // Decompressing: the bytes decoded, read from next on; and whether to
    // throw for a wrong checksum once they are all read
    private byte[] decoded;
    private int next;
    private bool wrongChecksum;
    private bool disposed;

    internal ZLibStream(Stream stream, CompressionMode mode) : this(stream, mode, false) {
    }

    internal ZLibStream(Stream stream, CompressionMode mode, bool leaveOpen) {
        inner = stream;
        this.mode = mode;
        this.leaveOpen = leaveOpen;
        if (mode == CompressionMode.Compress) {
            written = new MemoryStream();
        }
    }

    public override bool CanRead => mode == CompressionMode.Decompress && !disposed;
    public override bool CanWrite => mode == CompressionMode.Compress && !disposed;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) {
        if (mode != CompressionMode.Compress) {
            throw new NotSupportedException();
        }
        written.Write(buffer, offset, count);
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (mode != CompressionMode.Decompress) {
            throw new NotSupportedException();
        }
        if (decoded == null) {
            Decode();
        }
        int n = Math.Min(count, decoded.Length - next);
        if (n == 0 && count > 0 && wrongChecksum) {
            throw new InvalidDataException("The checksum of the zlib stream does not match its data");
        }
        Array.Copy(decoded, next, buffer, offset, n);
        next += n;
        return n;
    }

    private void Decode() {
        long start = inner.Position;
        int length = (int) (inner.Length - start);
        // The rest of the input in reads that never get to its end, which
        // a read at the end would tell the Decompressor
        byte[] data = new byte[length];
        int got = 0;
        while (got < length) {
            int n = inner.Read(data, got, length - got);
            if (n <= 0) {
                break;
            }
            got += n;
        }
        if (got < 2) {
            AtTheEnd();
            decoded = new byte[0];
            return;
        }
        int cmf = data[0];
        int flg = data[1];
        if ((cmf & 0x0F) != 8 || (cmf >> 4) > 7 || (cmf << 8 | flg) % 31 != 0 || (flg & 0x20) != 0) {
            throw new InvalidDataException("Invalid zlib header");
        }
        Puff.Result result = Puff.Inflate(data, 2, got - 2, Decompressor.MAX_DECODED_LENGTH + 1,
                out decoded, out int taken);
        if (result == Puff.Result.Invalid) {
            throw new InvalidDataException("Invalid Deflate data");
        }
        if (result == Puff.Result.TooLong) {
            return;     // More than the Decompressor takes, which says so
        }
        int end = 2 + taken;
        if (result == Puff.Result.OutOfInput || end + 4 > got) {
            AtTheEnd();     // Cut short: what was decoded, and a read at the end
            return;
        }
        uint expected = (uint) (data[end] << 24 | data[end + 1] << 16 | data[end + 2] << 8 | data[end + 3]);
        wrongChecksum = Adler32(decoded) != expected;
        // The input as read to the end of the zlib stream, no further
        inner.Position = start + end + 4;
    }

    // A read at the end of the input, as .NET's ZLibStream makes when its
    // input ends before the stream does
    private void AtTheEnd() {
        inner.Position = inner.Length;
        inner.Read(new byte[1], 0, 1);
    }

    private static uint Adler32(byte[] bytes) {
        uint a = 1;
        uint b = 0;
        foreach (byte value in bytes) {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }
        return b << 16 | a;
    }

    protected override void Dispose(bool disposing) {
        if (disposed) {
            return;
        }
        disposed = true;
        try {
            if (disposing && mode == CompressionMode.Compress) {
                byte[] data = written.ToArray();
                // The header of zlib's default level, as .NET's ZLibStream writes it
                inner.WriteByte(0x78);
                inner.WriteByte(0x9C);
                using (var deflate = new DeflateStream(inner, CompressionLevel.Optimal, true)) {
                    deflate.Write(data, 0, data.Length);
                }
                uint adler = Adler32(data);
                inner.WriteByte((byte) (adler >> 24));
                inner.WriteByte((byte) (adler >> 16));
                inner.WriteByte((byte) (adler >> 8));
                inner.WriteByte((byte) adler);
            }
            if (disposing && !leaveOpen) {
                inner.Dispose();
            }
        } finally {
            base.Dispose(disposing);
        }
    }
}

}
