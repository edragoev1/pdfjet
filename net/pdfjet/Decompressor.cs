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
        using var bos = new MemoryStream(data.Length * 2);
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