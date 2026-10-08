/*
 * Puff.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/*
  puff.c
  Copyright 2002-2013 Mark Adler, all rights reserved
  version 2.3, 21 Jan 2013

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the author be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Mark Adler    madler@alumni.caltech.edu
 */

// A C# translation of puff.c, Mark Adler's reference decoder of Deflate,
// for the .NET Framework 4.8 build alone, which has no ZLibStream: it decodes raw Deflate data and tells how many bytes of
// the input it took, so that what follows, the checksum of zlib, is found
// exactly. Altered from the original: the output grows as it is written, up
// to a limit, and the errors are kept as Puff.Result.

using System;

namespace PDFjet.NET {

internal sealed class Puff {
    internal enum Result { Done, OutOfInput, TooLong, Invalid }

    private const int MAXBITS = 15;
    private const int MAXLCODES = 286;
    private const int MAXDCODES = 30;
    private const int MAXCODES = MAXLCODES + MAXDCODES;
    private const int FIXLCODES = 288;

    private sealed class OutOfInputException : Exception { }
    private sealed class InvalidException : Exception { }
    private sealed class TooLongException : Exception { }

    private readonly byte[] input;
    private readonly int inputEnd;
    private int inputNext;
    private int bitBuffer;
    private int bitCount;
    private byte[] output = new byte[4096];
    private int outputNext;
    private readonly int maxLength;

    private sealed class Huffman {
        internal readonly short[] count = new short[MAXBITS + 1];
        internal readonly short[] symbol;
        internal Huffman(int symbols) {
            symbol = new short[symbols];
        }
    }

    private Puff(byte[] input, int offset, int length, int maxLength) {
        this.input = input;
        inputNext = offset;
        inputEnd = offset + length;
        this.maxLength = maxLength;
    }

    /// <summary>
    /// Decodes the raw Deflate data from offset, to at most maxLength bytes:
    /// the bytes decoded, so far when it stops early, and the bytes of the
    /// input taken to the end of the last block, whole bytes.
    /// </summary>
    internal static Result Inflate(byte[] input, int offset, int length, int maxLength,
            out byte[] decoded, out int taken) {
        var puff = new Puff(input, offset, length, maxLength);
        Result result = Result.Done;
        try {
            int last;
            do {
                last = puff.Bits(1);
                int type = puff.Bits(2);
                switch (type) {
                    case 0: puff.Stored(); break;
                    case 1: puff.Fixed(); break;
                    case 2: puff.Dynamic(); break;
                    default: throw new InvalidException();
                }
            } while (last == 0);
        } catch (OutOfInputException) {
            result = Result.OutOfInput;
        } catch (TooLongException) {
            result = Result.TooLong;
        } catch (InvalidException) {
            result = Result.Invalid;
        }
        decoded = new byte[puff.outputNext];
        Array.Copy(puff.output, decoded, puff.outputNext);
        taken = puff.inputNext - offset;
        return result;
    }

    private void Put(int value) {
        if (outputNext == maxLength) {
            throw new TooLongException();
        }
        if (outputNext == output.Length) {
            Array.Resize(ref output, (int) Math.Min((long) output.Length * 2, maxLength));
        }
        output[outputNext++] = (byte) value;
    }

    private int Bits(int need) {
        long value = bitBuffer;
        while (bitCount < need) {
            if (inputNext == inputEnd) {
                throw new OutOfInputException();
            }
            value |= (long) input[inputNext++] << bitCount;
            bitCount += 8;
        }
        bitBuffer = (int) (value >> need);
        bitCount -= need;
        return (int) (value & ((1L << need) - 1));
    }

    private void Stored() {
        // Discard the leftover bits, and the block is whole bytes
        bitBuffer = 0;
        bitCount = 0;
        if (inputNext + 4 > inputEnd) {
            throw new OutOfInputException();
        }
        int len = input[inputNext] | input[inputNext + 1] << 8;
        int complement = input[inputNext + 2] | input[inputNext + 3] << 8;
        inputNext += 4;
        if (len != (~complement & 0xffff)) {
            throw new InvalidException();
        }
        if (inputNext + len > inputEnd) {
            // The bytes there are, then out of input
            while (inputNext < inputEnd) {
                Put(input[inputNext++]);
            }
            throw new OutOfInputException();
        }
        while (len-- > 0) {
            Put(input[inputNext++]);
        }
    }

    private int Decode(Huffman h) {
        int code = 0;
        int first = 0;
        int index = 0;
        for (int len = 1; len <= MAXBITS; len++) {
            code |= Bits(1);
            int count = h.count[len];
            if (code - count < first) {
                return h.symbol[index + (code - first)];
            }
            index += count;
            first += count;
            first <<= 1;
            code <<= 1;
        }
        throw new InvalidException();
    }

    // The canonical Huffman code of the lengths; the number of codes left
    // unused, 0 for a complete code, negative for one over-subscribed
    private static int Construct(Huffman h, short[] length, int offset, int n) {
        for (int len = 0; len <= MAXBITS; len++) {
            h.count[len] = 0;
        }
        for (int symbol = 0; symbol < n; symbol++) {
            h.count[length[offset + symbol]]++;
        }
        if (h.count[0] == n) {
            return 0;
        }
        int left = 1;
        for (int len = 1; len <= MAXBITS; len++) {
            left <<= 1;
            left -= h.count[len];
            if (left < 0) {
                return left;
            }
        }
        var offs = new short[MAXBITS + 1];
        for (int len = 1; len < MAXBITS; len++) {
            offs[len + 1] = (short) (offs[len] + h.count[len]);
        }
        for (int symbol = 0; symbol < n; symbol++) {
            if (length[offset + symbol] != 0) {
                h.symbol[offs[length[offset + symbol]]++] = (short) symbol;
            }
        }
        return left;
    }

    private static readonly short[] LBASE = {
        3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
        35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258};
    private static readonly short[] LEXT = {
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
        3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0};
    private static readonly short[] DBASE = {
        1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
        257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577};
    private static readonly short[] DEXT = {
        0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
        7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13};

    private void Codes(Huffman lencode, Huffman distcode) {
        int symbol;
        do {
            symbol = Decode(lencode);
            if (symbol < 0) {
                throw new InvalidException();
            }
            if (symbol < 256) {
                Put(symbol);
            } else if (symbol > 256) {
                symbol -= 257;
                if (symbol >= 29) {
                    throw new InvalidException();
                }
                int len = LBASE[symbol] + Bits(LEXT[symbol]);
                symbol = Decode(distcode);
                if (symbol < 0 || symbol >= 30) {
                    throw new InvalidException();
                }
                int dist = DBASE[symbol] + Bits(DEXT[symbol]);
                if (dist > outputNext) {
                    throw new InvalidException();
                }
                while (len-- > 0) {
                    Put(output[outputNext - dist]);
                }
            }
        } while (symbol != 256);
    }

    private static Huffman fixedLencode;
    private static Huffman fixedDistcode;

    private void Fixed() {
        if (fixedLencode == null) {
            var lencode = new Huffman(FIXLCODES);
            var distcode = new Huffman(MAXDCODES);
            var lengths = new short[FIXLCODES];
            int symbol = 0;
            for (; symbol < 144; symbol++) lengths[symbol] = 8;
            for (; symbol < 256; symbol++) lengths[symbol] = 9;
            for (; symbol < 280; symbol++) lengths[symbol] = 7;
            for (; symbol < FIXLCODES; symbol++) lengths[symbol] = 8;
            Construct(lencode, lengths, 0, FIXLCODES);
            for (symbol = 0; symbol < MAXDCODES; symbol++) lengths[symbol] = 5;
            Construct(distcode, lengths, 0, MAXDCODES);
            fixedDistcode = distcode;
            fixedLencode = lencode;
        }
        Codes(fixedLencode, fixedDistcode);
    }

    private static readonly short[] ORDER = {16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15};

    private void Dynamic() {
        var lengths = new short[MAXCODES];
        int nlen = Bits(5) + 257;
        int ndist = Bits(5) + 1;
        int ncode = Bits(4) + 4;
        if (nlen > MAXLCODES || ndist > MAXDCODES) {
            throw new InvalidException();
        }
        int index = 0;
        for (; index < ncode; index++) {
            lengths[ORDER[index]] = (short) Bits(3);
        }
        for (; index < 19; index++) {
            lengths[ORDER[index]] = 0;
        }
        var lencode = new Huffman(MAXLCODES);
        var distcode = new Huffman(MAXDCODES);
        if (Construct(lencode, lengths, 0, 19) != 0) {
            throw new InvalidException();
        }
        index = 0;
        while (index < nlen + ndist) {
            int symbol = Decode(lencode);
            if (symbol < 0) {
                throw new InvalidException();
            }
            if (symbol < 16) {
                lengths[index++] = (short) symbol;
            } else {
                int len = 0;
                if (symbol == 16) {
                    if (index == 0) {
                        throw new InvalidException();
                    }
                    len = lengths[index - 1];
                    symbol = 3 + Bits(2);
                } else if (symbol == 17) {
                    symbol = 3 + Bits(3);
                } else {
                    symbol = 11 + Bits(7);
                }
                if (index + symbol > nlen + ndist) {
                    throw new InvalidException();
                }
                while (symbol-- > 0) {
                    lengths[index++] = (short) len;
                }
            }
        }
        if (lengths[256] == 0) {
            throw new InvalidException();
        }
        int err = Construct(lencode, lengths, 0, nlen);
        if (err < 0 || (err > 0 && nlen - lencode.count[0] != 1)) {
            throw new InvalidException();
        }
        err = Construct(distcode, lengths, nlen, ndist);
        if (err < 0 || (err > 0 && ndist - distcode.count[0] != 1)) {
            throw new InvalidException();
        }
        Codes(lencode, distcode);
    }
}

}
