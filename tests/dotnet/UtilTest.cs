/*
 * UtilTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
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

    [Fact]
    public void SplitRefusesALineItCannotRead() {
        Assert.Throws<System.ArgumentException>(() => Util.Split("a,\"b,c", ","));
        Assert.Throws<System.ArgumentException>(() => Util.Split("\"a\"b,c", ","));
    }
}
}
