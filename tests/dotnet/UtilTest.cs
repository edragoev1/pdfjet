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
        Assert.Equal(new List<string> {"a", "", "b"}, Util.ReadLines(file));
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
}
}
