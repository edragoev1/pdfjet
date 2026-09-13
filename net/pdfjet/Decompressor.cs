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
    /// The largest number of bytes that a stream, or the samples of an image,
    /// may decode to: 256 MiB. A few kilobytes of Flate, LZW or RunLength data
    /// can decode to gigabytes, so a decoder that would go past it throws.
    /// </summary>
    internal const int MAX_DECODED_LENGTH = 256 * 1024 * 1024;

    // Throws when the data decoded so far is longer than the limit.
    private static void CheckLength(long length, int maxLength, String filter) {
        if (length > maxLength) {
            throw new InvalidDataException(
                    filter + " data decodes to more than " + maxLength + " bytes");
        }
    }

    /// <summary>
    /// Decodes the data of an LZWDecode stream, with the default EarlyChange
    /// of 1: the codes get one bit longer one code before the table needs it.
    /// Data that ends without the end code, or with an invalid code, returns
    /// what was decoded up to there, as a missing end is common in real files.
    /// </summary>
    internal static byte[] LZWDecode(byte[] data) {
        return LZWDecode(data, MAX_DECODED_LENGTH);
    }

    internal static byte[] LZWDecode(byte[] data, int maxLength) {
        using var bos = new MemoryStream((int) Math.Min(data.Length * 2L, maxLength));
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
                CheckLength(bos.Length, maxLength, "LZW");
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

    /// <summary>
    /// Undoes the predictor of the /DecodeParms of a stream: 2 is the TIFF
    /// predictor, and 10 to 15 are the PNG predictors, where each row starts
    /// with the type of the PNG filter of that row.
    /// </summary>
    internal static byte[] ApplyPredictor(
            byte[] data,
            int predictor,
            int colors,
            int bitsPerComponent,
            int columns) {
        // Larger values are not in real files, and would overflow the row length.
        if (colors < 1 || colors > 256 ||
                bitsPerComponent < 1 || bitsPerComponent > 16 ||
                columns < 1 || columns > (1 << 18)) {
            return data;
        }
        if (predictor == 2) {
            return ApplyTIFFPredictor(data, colors, bitsPerComponent, columns);
        } else if (predictor >= 10) {
            return ApplyPNGPredictor(
                    data,
                    (colors * bitsPerComponent + 7) / 8,
                    (colors * bitsPerComponent * columns + 7) / 8);
        }
        return data;
    }

    // Each sample adds the sample of the same color to its left.
    private static byte[] ApplyTIFFPredictor(
            byte[] data, int colors, int bitsPerComponent, int columns) {
        byte[] decoded = (byte[]) data.Clone();
        int rowLength = (colors * bitsPerComponent * columns + 7) / 8;
        for (int row = 0; row < decoded.Length; row += rowLength) {
            if (bitsPerComponent == 8) {
                int end = Math.Min(row + rowLength, decoded.Length);
                for (int i = row + colors; i < end; i++) {
                    decoded[i] += decoded[i - colors];
                }
            } else {
                int samples = Math.Min(
                        colors * columns, (decoded.Length - row) * 8 / bitsPerComponent);
                for (int i = colors; i < samples; i++) {
                    int left = GetSample(decoded, row, (i - colors) * bitsPerComponent, bitsPerComponent);
                    int sample = GetSample(decoded, row, i * bitsPerComponent, bitsPerComponent);
                    SetSample(decoded, row, i * bitsPerComponent, bitsPerComponent, left + sample);
                }
            }
        }
        return decoded;
    }

    private static int GetSample(byte[] data, int row, int bit, int bitsPerComponent) {
        int sample = 0;
        for (int i = bit; i < bit + bitsPerComponent; i++) {
            sample = (sample << 1) | ((data[row + (i >> 3)] >> (7 - (i & 7))) & 1);
        }
        return sample;
    }

    // Sets the low bitsPerComponent bits of the sample.
    private static void SetSample(
            byte[] data, int row, int bit, int bitsPerComponent, int sample) {
        for (int i = bit + bitsPerComponent - 1; i >= bit; i--) {
            int k = row + (i >> 3);
            int mask = 1 << (7 - (i & 7));
            data[k] = (byte) (((sample & 1) != 0) ? (data[k] | mask) : (data[k] & ~mask));
            sample >>= 1;
        }
    }

    // Only the last row can be shorter than rowLength.
    private static byte[] ApplyPNGPredictor(byte[] data, int bytesPerPixel, int rowLength) {
        int rows = (data.Length + rowLength) / (rowLength + 1);
        byte[] decoded = new byte[data.Length - rows];
        int j = 0;              // The index in decoded
        for (int i = 0; i < data.Length; i += rowLength + 1) {
            int filter = data[i];
            int n = Math.Min(rowLength, data.Length - i - 1);
            for (int x = 0; x < n; x++, j++) {
                int left = (x >= bytesPerPixel) ? decoded[j - bytesPerPixel] : 0;
                int up = (j >= rowLength) ? decoded[j - rowLength] : 0;
                int upLeft = (x >= bytesPerPixel && j >= rowLength) ?
                        decoded[j - rowLength - bytesPerPixel] : 0;
                int value = data[i + 1 + x];
                if (filter == 1) {          // Sub
                    value += left;
                } else if (filter == 2) {   // Up
                    value += up;
                } else if (filter == 3) {   // Average
                    value += (left + up) / 2;
                } else if (filter == 4) {   // Paeth
                    value += Paeth(left, up, upLeft);
                }                           // 0 is None, and so are unknown types.
                decoded[j] = (byte) value;
            }
        }
        return decoded;
    }

    private static int Paeth(int left, int up, int upLeft) {
        int p = left + up - upLeft;
        int pLeft = Math.Abs(p - left);
        int pUp = Math.Abs(p - up);
        int pUpLeft = Math.Abs(p - upLeft);
        if (pLeft <= pUp && pLeft <= pUpLeft) {
            return left;
        }
        return (pUp <= pUpLeft) ? up : upLeft;
    }

    /// <summary>
    /// Decodes the data of an ASCIIHexDecode stream. White space is skipped,
    /// &gt; ends the data, and a last digit without a pair is followed by 0.
    /// </summary>
    internal static byte[] ASCIIHexDecode(byte[] data) {
        using var bos = new MemoryStream(data.Length / 2);
        int high = -1;
        foreach (byte b in data) {
            if (b == '>') {
                break;
            }
            int digit = HexValue(b);
            if (digit == -1) {
                continue;       // White space, or a character that is not valid.
            }
            if (high == -1) {
                high = digit;
            } else {
                bos.WriteByte((byte) ((high << 4) | digit));
                high = -1;
            }
        }
        if (high != -1) {
            bos.WriteByte((byte) (high << 4));
        }
        return bos.ToArray();
    }

    /// <summary>
    /// Returns the value of a hexadecimal digit, or -1 when it is not one.
    /// </summary>
    internal static int HexValue(int c) {
        if (c >= '0' && c <= '9') {
            return c - '0';
        } else if (c >= 'a' && c <= 'f') {
            return c - 'a' + 10;
        } else if (c >= 'A' && c <= 'F') {
            return c - 'A' + 10;
        }
        return -1;
    }

    /// <summary>
    /// Decodes the data of an ASCII85Decode stream. Each group of five
    /// characters from ! to u is four bytes, z is four zero bytes, and ~&gt;
    /// ends the data. White space is skipped, and a last group of n
    /// characters is n - 1 bytes.
    /// </summary>
    internal static byte[] ASCII85Decode(byte[] data) {
        using var bos = new MemoryStream(data.Length * 4 / 5 + 4);
        long value = 0;
        int count = 0;
        int i = 0;
        if (data.Length >= 2 && data[0] == '<' && data[1] == '~') {
            i = 2;              // The start of the data in PostScript.
        }
        for (; i < data.Length; i++) {
            int c = data[i];
            if (c == '~') {
                break;
            } else if (c == 'z' && count == 0) {
                bos.WriteByte(0);
                bos.WriteByte(0);
                bos.WriteByte(0);
                bos.WriteByte(0);
            } else if (c >= '!' && c <= 'u') {
                value = value * 85 + (c - '!');
                if (++count == 5) {
                    for (int j = 24; j >= 0; j -= 8) {
                        bos.WriteByte((byte) (value >> j));
                    }
                    value = 0;
                    count = 0;
                }
            }                   // White space, or a character that is not valid.
        }
        if (count > 1) {
            for (int j = count; j < 5; j++) {
                value = value * 85 + 84;
            }
            for (int j = 0; j < count - 1; j++) {
                bos.WriteByte((byte) (value >> (24 - 8 * j)));
            }
        }
        return bos.ToArray();
    }

    /// <summary>
    /// Decodes the data of a RunLengthDecode stream. A length byte from 0 to
    /// 127 is followed by that many plus one bytes to copy, one from 129 to
    /// 255 by a byte to repeat 257 minus that many times, and 128 ends the data.
    /// </summary>
    internal static byte[] RunLengthDecode(byte[] data) {
        return RunLengthDecode(data, MAX_DECODED_LENGTH);
    }

    internal static byte[] RunLengthDecode(byte[] data, int maxLength) {
        using var bos = new MemoryStream((int) Math.Min(data.Length * 2L, maxLength));
        int i = 0;
        while (i < data.Length) {
            int length = data[i++];
            if (length < 128) {
                int n = Math.Min(length + 1, data.Length - i);
                bos.Write(data, i, n);
                i += n;
            } else if (length > 128 && i < data.Length) {
                byte b = data[i++];
                for (int j = 0; j < 257 - length; j++) {
                    bos.WriteByte(b);
                }
            } else {
                break;
            }
            CheckLength(bos.Length, maxLength, "RunLength");
        }
        return bos.ToArray();
    }

    /// <summary>
    /// Decodes a zlib stream. A stream that ends before its end, as a
    /// truncated one does, throws; bytes after the end of the stream are
    /// ignored, as in the other ports.
    /// </summary>
    internal static byte[] Inflate(byte[] data) {
        return Inflate(data, MAX_DECODED_LENGTH, false);
    }

    /// <summary>Decodes a zlib stream, which must end and decode to at most maxLength bytes.</summary>
    internal static byte[] Inflate(byte[] data, int maxLength) {
        return Inflate(data, maxLength, false);
    }

    /// <summary>
    /// Returns the first length bytes that a zlib stream decodes to, or all of
    /// them when there are fewer, and ignores the rest of the stream. A stream
    /// that ends in the middle, before those bytes, throws.
    /// </summary>
    internal static byte[] InflatePrefix(byte[] data, int length) {
        return Inflate(data, length, true);
    }

    private static byte[] Inflate(byte[] data, int maxLength, bool prefix) {
        using var outStream = new MemoryStream();
        var inStream = new EndOfInputStream(data);
        using (var zlib = new ZLibStream(inStream, CompressionMode.Decompress)) {
            // Not CopyTo: ZLibStream.CopyTo reads all of its input, whether the
            // stream ends before it or not.
            byte[] buffer = new byte[65536];
            while (!(prefix && outStream.Length == maxLength)) {
                // At most one byte more than the limit, which tells that the
                // stream decodes to more.
                int count = zlib.Read(
                        buffer, 0, (int) Math.Min(buffer.Length, (long) maxLength - outStream.Length + 1));
                if (count <= 0) {
                    break;
                }
                if (outStream.Length + count > maxLength) {
                    if (prefix) {
                        outStream.Write(buffer, 0, (int) (maxLength - outStream.Length));
                        break;
                    }
                    CheckLength(outStream.Length + count, maxLength, "Flate");
                }
                outStream.Write(buffer, 0, count);
            }
        }
        // ZLibStream returns the bytes decoded so far, without an error, when
        // its input ends first. Its Read reads the input only until the stream
        // ends, so a read at the end of the input means the stream was cut short;
        // a prefix that got all of its bytes does not need the rest.
        if (inStream.ReadAtEnd && !(prefix && outStream.Length == maxLength)) {
            throw new InvalidDataException("Truncated or invalid Flate stream");
        }
        return outStream.ToArray();
    }

    /// <summary>The input of Inflate, which remembers a read at its end.</summary>
    private sealed class EndOfInputStream : MemoryStream {
        internal bool ReadAtEnd { get; private set; }

        internal EndOfInputStream(byte[] data) : base(data, false) {
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int read = base.Read(buffer, offset, count);
            if (read == 0 && count > 0) {
                ReadAtEnd = true;
            }
            return read;
        }

        public override int Read(Span<byte> buffer) {
            int read = base.Read(buffer);
            if (read == 0 && buffer.Length > 0) {
                ReadAtEnd = true;
            }
            return read;
        }

        public override int ReadByte() {
            int value = base.ReadByte();
            if (value == -1) {
                ReadAtEnd = true;
            }
            return value;
        }
    }
}   // End of Decompressor.cs
}   // End of package PDFjet.NET