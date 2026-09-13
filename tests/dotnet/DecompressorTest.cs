/*
 * DecompressorTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>The stream filters and predictors that reading a PDF needs, and the compressor.</summary>
public class DecompressorTest {
    private static byte[] Bytes(params int[] values) {
        byte[] result = new byte[values.Length];
        for (int i = 0; i < values.Length; i++) {
            result[i] = (byte) values[i];
        }
        return result;
    }

    private static byte[] Ascii(string text) {
        return Encoding.ASCII.GetBytes(text);
    }

    [Fact]
    public void LzwDecodesTheExampleOfTheStandard() {
        // ISO 32000-1, 7.4.4.2: the encoding of "-----A---B".
        byte[] encoded = Bytes(0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01);
        Assert.Equal("-----A---B", TestSupport.Latin1(Decompressor.LZWDecode(encoded)));
    }

    [Fact]
    public void AsciiHexDecodeSkipsWhitespaceAndStopsAtTheEndMarker() {
        Assert.Equal("Hello", TestSupport.Latin1(Decompressor.ASCIIHexDecode(Ascii("48 65 6C6C6F>"))));
    }

    [Fact]
    public void AsciiHexDecodeReadsAMissingLastDigitAsZero() {
        Assert.Equal(Bytes(0x70), Decompressor.ASCIIHexDecode(Ascii("7>")));
    }

    [Fact]
    public void Ascii85DecodesWithAndWithoutThePrefix() {
        // base64.a85encode(b"Hello world") in Python.
        Assert.Equal("Hello world", TestSupport.Latin1(Decompressor.ASCII85Decode(Ascii("87cURD]j7BEbo7~>"))));
        Assert.Equal("Hello world", TestSupport.Latin1(Decompressor.ASCII85Decode(Ascii("<~87cURD]j7BEbo7~>"))));
    }

    [Fact]
    public void Ascii85DecodesZAsFourZeroBytesAndAPartialLastGroup() {
        Assert.Equal(Bytes(0, 0, 0, 0), Decompressor.ASCII85Decode(Ascii("z~>")));
        Assert.Equal(Bytes(0, 0, 0, 0, 'a', 'b'), Decompressor.ASCII85Decode(Ascii("z@:B~>")));
    }

    [Fact]
    public void RunLengthDecodeCopiesLiteralsAndRepeatsRuns() {
        byte[] encoded = Bytes(2, 'a', 'b', 'c', 254, 'x', 128);
        Assert.Equal("abcxxx", TestSupport.Latin1(Decompressor.RunLengthDecode(encoded)));
    }

    [Fact]
    public void PngPredictorsUndoEachRowFilter() {
        // Each row is the filter type and three one byte samples.
        Assert.Equal(Bytes(1, 2, 3), Decompressor.ApplyPredictor(Bytes(1, 1, 1, 1), 11, 1, 8, 3));
        Assert.Equal(Bytes(1, 2, 3, 2, 3, 4), Decompressor.ApplyPredictor(Bytes(0, 1, 2, 3, 2, 1, 1, 1), 12, 1, 8, 3));
        Assert.Equal(Bytes(2, 5, 8), Decompressor.ApplyPredictor(Bytes(3, 2, 4, 6), 13, 1, 8, 3));
        Assert.Equal(Bytes(1, 2, 3), Decompressor.ApplyPredictor(Bytes(4, 1, 1, 1), 14, 1, 8, 3));
    }

    [Fact]
    public void TiffPredictorAddsTheSampleToTheLeft() {
        Assert.Equal(Bytes(1, 2, 3, 5, 5, 5), Decompressor.ApplyPredictor(Bytes(1, 1, 1, 5, 0, 0), 2, 1, 8, 3));
    }

    [Fact]
    public void PredictorOneAndInvalidParametersLeaveTheDataAsItIs() {
        byte[] data = Bytes(9, 8, 7);
        Assert.Equal(data, Decompressor.ApplyPredictor(data, 1, 1, 8, 3));
        Assert.Equal(data, Decompressor.ApplyPredictor(data, 12, 0, 8, 3));
    }

    [Fact]
    public void InflateUndoesDeflate() {
        byte[] data = new byte[100000];
        new Random(42).NextBytes(data);
        Array.Fill(data, (byte) 'x', 50000, 40000);
        Assert.Equal(data, Decompressor.Inflate(Compressor.Deflate(data)));
    }

    [Fact]
    public void InflateRejectsATruncatedStream() {
        byte[] deflated = Compressor.Deflate(Ascii("hello hello hello hello"));
        Assert.ThrowsAny<Exception>(() => Decompressor.Inflate(deflated[..6]));
    }

    [Fact]
    public void InflateIgnoresBytesAfterTheEndOfAStream() {
        byte[] data = Ascii("hello hello hello hello");
        byte[] deflated = Compressor.Deflate(data);
        byte[] padded = new byte[deflated.Length + 3];
        deflated.CopyTo(padded, 0);
        padded[deflated.Length] = (byte) '\r';
        padded[deflated.Length + 1] = (byte) '\n';
        padded[deflated.Length + 2] = 0x42;
        Assert.Equal(data, Decompressor.Inflate(padded));
        Assert.Empty(Decompressor.Inflate(Compressor.Deflate(new byte[0])));
    }

    [Fact]
    public void InflateRejectsEveryTruncationOfAStream() {
        byte[] data = new byte[2000];
        new Random(3).NextBytes(data.AsSpan(0, 1000));
        data.AsSpan(1000).Fill((byte) 'a');
        byte[] deflated = Compressor.Deflate(data);
        Assert.Equal(data, Decompressor.Inflate(deflated));
        // The last 4 bytes are the Adler-32 checksum, which not every port checks.
        for (int length = 0; length < deflated.Length - 4; length++) {
            byte[] truncated = deflated[..length];
            Assert.ThrowsAny<Exception>(() => Decompressor.Inflate(truncated));
        }
    }

    [Fact]
    public void DeflateOfNoBytesIsAnEmptyZlibStream() {
        byte[] deflated = Compressor.Deflate(new byte[0]);
        Assert.Equal(8, deflated.Length);
        Assert.Equal(0x78, deflated[0]);
    }

    [Fact]
    public void InflateAcceptsAnEmptyStream() {
        Assert.Empty(Decompressor.Inflate(Compressor.Deflate(new byte[0])));
    }
}
}
