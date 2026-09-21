/*
 * UtilTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>Content and Util: reading text and binary files and streams.</summary>
public sealed class UtilTest : IDisposable {
    private readonly TestSupport.TempDir tempDir = new TestSupport.TempDir();

    public void Dispose() {
        tempDir.Dispose();
    }

    /// <summary>A stream that returns at most three bytes from each read.</summary>
    private sealed class SlowStream : MemoryStream {
        public SlowStream(byte[] data) : base(data) {
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return base.Read(buffer, offset, Math.Min(count, 3));
        }

        public override int Read(Span<byte> buffer) {
            return base.Read(buffer.Slice(0, Math.Min(buffer.Length, 3)));
        }
    }

    [Fact]
    public void OfTextFileReadsUtf8AndDropsAByteOrderMark() {
        string file = tempDir.Write("bom.txt", Encoding.UTF8.GetBytes("﻿hello\nwörld"));
        Assert.Equal("hello\nwörld", Content.OfTextFile(file));
    }

    [Fact]
    public void ReadLinesDropsAByteOrderMarkAndKeepsEmptyLines() {
        string file = tempDir.Write("lines.txt", Encoding.UTF8.GetBytes("﻿a\n\nb"));
        Assert.Equal(new List<string> {"a", "", "b"}, Content.LinesOfTextFile(file));
    }

    [Fact]
    public void ReadingAMissingFileThrows() {
        string missing = System.IO.Path.Combine(tempDir.Path, "missing.txt");
        Assert.ThrowsAny<IOException>(() => Content.OfTextFile(missing));
        Assert.ThrowsAny<Exception>(() => Content.OfBinaryFile(missing));
    }

    [Fact]
    public void GetFromStreamReadsAStreamThatReturnsFewBytesAtATime() {
        byte[] data = new byte[10000];
        new Random(7).NextBytes(data);
        Assert.Equal(data, Content.GetFromStream(new SlowStream(data)));
        Assert.Equal(data, Content.GetFromStream(new MemoryStream(data), 17));
    }

    [Fact]
    public void OfBinaryFileReadsTheBytes() {
        byte[] data = {0, 1, 2, 0xFF};
        Assert.Equal(data, Content.OfBinaryFile(tempDir.Write("data.bin", data)));
    }

    [Fact]
    public void ToHexStringWritesLowerCaseDigits() {
        Assert.Equal("00abff", Util.ToHexString(new byte[] {0x00, 0xAB, 0xFF}));
    }

    [Fact]
    public void SplitCutsTheLineAtTheDelimiterAndKeepsTheEmptyFields() {
        Assert.Equal(new String[] {"a", "b", "c"}, Util.Split("a,b,c", ","));
        Assert.Equal(new String[] {"", "a", ""}, Util.Split(",a,", ","));
        Assert.Equal(new String[] {""}, Util.Split("", ","));
        Assert.Equal(new String[] {"a", "b"}, Util.Split("a||b", "||"));
        Assert.Equal(new String[] {"a,b"}, Util.Split("a,b", ""));
    }

    [Fact]
    public void SplitReadsAQuotedFieldAsRfc4180Does() {
        Assert.Equal(new String[] {"Smith, John", "42"}, Util.Split("\"Smith, John\",42", ","));
        Assert.Equal(new String[] {"a\"b"}, Util.Split("\"a\"\"b\"", ","));
        Assert.Equal(new String[] {"", "x", ""}, Util.Split("\"\",x,\"\"", ","));
        Assert.Equal(new String[] {"one\ttwo", "three"}, Util.Split("\"one\ttwo\"\tthree", "\t"));
    }

    [Fact]
    public void SplitLeavesTheQuotesOfAFieldThatDoesNotStartWithOne() {
        Assert.Equal(new String[] {"5\" pipe", "b"}, Util.Split("5\" pipe,b", ","));
        Assert.Equal(new String[] {"a\"b\"c"}, Util.Split("a\"b\"c", ","));
    }

    // Reads the first record of the text, as the data file readers do.
    private static String[] FirstRecord(String text) {
        StringReader reader = new StringReader(text);
        return Util.ReadRecord(reader.ReadLine(), reader, ",");
    }

    [Fact]
    public void AQuotedFieldGoesOnOverItsLineBreaksAsSpaces() {
        Assert.Equal(new String[] {"a", "12 Main St Apt 4", "b"}, FirstRecord("a,\"12 Main St\nApt 4\",b\nnext,line"));
        Assert.Equal(new String[] {"x\" y"}, FirstRecord("\"x\"\"\ny\""));
        Assert.Equal(new String[] {"a b", "c d"}, FirstRecord("\"a\nb\",\"c\nd\""));
        Assert.Equal(new String[] {"", " ", ""}, FirstRecord(",\"\n\",\nnext"));
        Assert.Equal(new String[] {"a", "b"}, FirstRecord("a,b\n\"c\nd\""));
    }

    [Fact]
    public void AQuotedFieldThatIsNeverClosedIsRefused() {
        ArgumentException end = Assert.Throws<ArgumentException>(() => FirstRecord("a,\"b\nc\nd"));
        Assert.Equal("A quoted field is not closed by the end of the data file: a,\"b\nc\nd", end.Message);
        StringBuilder text = new StringBuilder("\"a");
        for (int i = 0; i < Util.MAX_LINES_IN_RECORD; i++) {
            text.Append("\nb");
        }
        ArgumentException limit = Assert.Throws<ArgumentException>(() => FirstRecord(text.ToString()));
        Assert.Equal("A quoted field is not closed within 10000 lines of the data file: "
                + text.ToString().Substring(0, 60) + "...", limit.Message);
    }

    [Fact]
    public void LineBreaksAreDrawnAsSpaces() {
        Assert.Equal("a b c d", Util.LineBreaksToSpaces("a\r\nb\rc\nd"));
        String plain = "no breaks";
        Assert.Same(plain, Util.LineBreaksToSpaces(plain));
    }

    [Fact]
    public void SplitRefusesALineItCannotRead() {
        Assert.Throws<System.ArgumentException>(() => Util.Split("a,\"b,c", ","));
        Assert.Throws<System.ArgumentException>(() => Util.Split("\"a\"b,c", ","));
    }

    [Fact]
    public void NoLineOfChineseOrJapaneseTextStartsWithAClosingMarkOrEndsWithAnOpeningOne() {
        Assert.Equal(3, Util.CjkLineEnd("あいう", "え"));
        Assert.Equal(2, Util.CjkLineEnd("あいう", "。"));  // う moves down with 。
        Assert.Equal(1, Util.CjkLineEnd("あい」", "。"));  // and so does 」
        Assert.Equal(2, Util.CjkLineEnd("あい「", "う"));  // 「 moves down
        Assert.Equal(1, Util.CjkLineEnd("中文", ","));
        Assert.Equal(1, Util.CjkLineEnd("」", "。"));      // no other place to break
    }

    [Fact]
    public void OfTextFileReplacesWhatIsNotUtf8() {
        // The bytes and the text they read as: the four ports replace the maximal
        // subparts of an ill formed UTF-8 sequence with U+FFFD, the substitution
        // the Unicode Standard recommends in section 3.9. The same table is in
        // the tests of the other three ports.
        string[][] cases = {
            new string[] {"68656C6C6F", "hello"},                                  // Hello
            new string[] {"77C3B6726C64", "w\u00F6rld"},                           // Wörld
            new string[] {"E697A5E69CAC", "\u65E5\u672C"},                         // 日本, Japanese
            new string[] {"F09F9880", "\uD83D\uDE00"},                             // A code point outside the plane
            new string[] {"EFBFBD", "\uFFFD"},                                     // The replacement character itself
            new string[] {"EFBFBE", "\uFFFE"},                                     // A noncharacter is well formed
            new string[] {"E28282", "\u2082"},                                     // Well formed, subscript two
            new string[] {"EDA080", "\uFFFD\uFFFD\uFFFD"},                         // An encoded surrogate, U+D800
            new string[] {"EDBFBF", "\uFFFD\uFFFD\uFFFD"},                         // U+DFFF
            new string[] {"EDA080EDB080", "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD"}, // An encoded surrogate pair
            new string[] {"EDA0", "\uFFFD\uFFFD"},                                 // Two bytes of an encoded surrogate
            new string[] {"ED", "\uFFFD"},
            new string[] {"C080", "\uFFFD\uFFFD"},                                 // The overlong encodings
            new string[] {"E08080", "\uFFFD\uFFFD\uFFFD"},
            new string[] {"F0828282", "\uFFFD\uFFFD\uFFFD\uFFFD"},
            new string[] {"F4908080", "\uFFFD\uFFFD\uFFFD\uFFFD"},                 // Past U+10FFFF
            new string[] {"F5808080", "\uFFFD\uFFFD\uFFFD\uFFFD"},
            new string[] {"FE", "\uFFFD"},
            new string[] {"FF", "\uFFFD"},
            new string[] {"80", "\uFFFD"},                                         // A byte of a sequence, alone
            new string[] {"BF", "\uFFFD"},
            new string[] {"C2", "\uFFFD"},                                         // A sequence cut short is one replacement,
            new string[] {"C2C2", "\uFFFD\uFFFD"},                                 // However many of its bytes are there
            new string[] {"E282", "\uFFFD"},
            new string[] {"E0A0", "\uFFFD"},
            new string[] {"F09080", "\uFFFD"},
            new string[] {"41C2", "A\uFFFD"},
            new string[] {"61EDA08062", "a\uFFFD\uFFFD\uFFFDb"},                   // Between well formed text
        };
        foreach (string[] item in cases) {
            string file = tempDir.Write("utf8.txt", HexBytes(item[0]));
            Assert.Equal(item[1], Content.OfTextFile(file));
        }
    }

    private static byte[] HexBytes(string hex) {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++) {
            bytes[i] = byte.Parse(hex.Substring(2 * i, 2), NumberStyles.HexNumber);
        }
        return bytes;
    }
}
}
