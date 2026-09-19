/*
 * FontStreamTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace PDFjet.NET {
// The stream fonts that are not valid, as the Go fuzz targets of the stream
// fonts found them: each fails with a message, and none reads past its data
// or allocates what it does not have.
public class FontStreamTest {
    // The metrics of a stream font: the units per em, the first and the last
    // character, the advance widths and the character map.
    private static byte[] Metrics(int unitsPerEm, int firstChar, int lastChar, int widths, int cmap) {
        MemoryStream buf = new MemoryStream();
        foreach (int v in new int[] {unitsPerEm, 0, -200, 1000, 800, 800, -200, firstChar, lastChar, 700, -100, 50}) {
            WriteInt32(buf, v);
        }
        WriteInt32(buf, widths);
        buf.Write(new byte[2*widths], 0, 2*widths);
        WriteInt32(buf, cmap);
        buf.Write(new byte[2*cmap], 0, 2*cmap);
        return buf.ToArray();
    }

    // A stream font with the name and the metrics, and a font file of 4 bytes.
    private static byte[] Stream(string name, byte[] metrics) {
        MemoryStream buf = new MemoryStream();
        buf.WriteByte((byte) name.Length);
        foreach (char c in name) {
            buf.WriteByte((byte) c);
        }
        buf.Write(new byte[3], 0, 3);   // No license text
        MemoryStream compressed = new MemoryStream();
        using (ZLibStream zlib = new ZLibStream(compressed, CompressionLevel.Optimal, true)) {
            zlib.Write(metrics, 0, metrics.Length);
        }
        WriteInt32(buf, (int) compressed.Length);
        compressed.WriteTo(buf);
        buf.WriteByte((byte) 'N');
        WriteInt32(buf, 8);
        WriteInt32(buf, 4);
        buf.Write(new byte[4], 0, 4);
        return buf.ToArray();
    }

    private static void WriteInt32(MemoryStream buf, int v) {
        buf.WriteByte((byte) (v >> 24));
        buf.WriteByte((byte) (v >> 16));
        buf.WriteByte((byte) (v >> 8));
        buf.WriteByte((byte) v);
    }

    private static Exception Error(byte[] stream) {
        return Assert.ThrowsAny<Exception>(() => new Font(TestSupport.NewPDF(), new MemoryStream(stream)));
    }

    [Fact]
    public void AMarkAfterAGlyphPastTheAdvanceWidthsIsDrawn() {
        // IBM Plex Sans JP maps ↺ and 14 other arrows to glyphs past the end
        // of its advance widths, which the offsets of the marks looked up.
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"));
        new TextLine(font, "↺́ x").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
    }

    [Fact]
    public void LengthsThatTheStreamDoesNotHaveTakeNoMemory() {
        // The metrics say they are 4 GB long, and the stream ends.
        byte[] stream = {1, (byte) 'A', 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF};
        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.IsType<EndOfStreamException>(Error(stream));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 1024*1024);
    }

    [Fact]
    public void MetricsItCannotDrawWithAreRejected() {
        Assert.Equal("Invalid font stream: the units per em.",
                Error(Stream("A", Metrics(0, 32, 126, 1, 0x10000))).Message);
        Assert.Equal("Invalid font stream: the first or last character.",
                Error(Stream("A", Metrics(1000, -1, 126, 1, 0x10000))).Message);
        Assert.Equal("Invalid font stream: the first or last character.",
                Error(Stream("A", Metrics(1000, 32, 0x10000, 1, 0x10000))).Message);
        Assert.Equal("Invalid font stream: no advance widths.",
                Error(Stream("A", Metrics(1000, 32, 126, 0, 0x10000))).Message);
        Assert.Equal("Invalid font stream: the character map.",
                Error(Stream("A", Metrics(1000, 32, 126, 1, 0x100))).Message);
        byte[] cut = new byte[100];
        Array.Copy(Metrics(1000, 32, 126, 1, 0x10000), cut, 100);
        Assert.Equal("Invalid font stream: the metrics end too soon.", Error(Stream("A", cut)).Message);

        byte[] valid = Stream("A", Metrics(1000, 32, 126, 1, 0x10000));
        new Font(TestSupport.NewPDF(), new MemoryStream(valid));
        new Font(new List<PDFobj>(), new MemoryStream(valid));
    }

    [Fact]
    public void ANameThatIsNotAPDFNameIsRejected() {
        foreach (string name in new string[] {"", "Noto Sans", "Noto/Sans", "Noto(Sans", "Noto#20Sans"}) {
            Assert.Equal("Invalid font stream: the font name.",
                    Error(Stream(name, Metrics(1000, 32, 126, 1, 0x10000))).Message);
        }
    }

    [Fact]
    public void AFontFileShorterThanItsSizeIsRejected() {
        byte[] stream = Stream("A", Metrics(1000, 32, 126, 1, 0x10000));
        Array.Resize(ref stream, stream.Length - 1);
        Assert.IsType<EndOfStreamException>(Error(stream));
    }

    [Fact]
    public void AGlyphPastTheAdvanceWidthsHasTheWidthOfTheLastOne() {
        // Two advance widths, 500 and 700, and "A" maps to glyph 5 past them.
        byte[] metrics = Metrics(1000, 32, 126, 2, 0x10000);
        metrics[52] = (byte) (500 >> 8);
        metrics[53] = 500 & 0xFF;
        metrics[54] = (byte) (700 >> 8);
        metrics[55] = 700 & 0xFF;
        metrics[61 + 2*'A'] = 5;
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        Font font = new Font(pdf, new MemoryStream(Stream("A", metrics)));
        TestSupport.AssertNear(7f, font.StringWidth(10f, "A"), 0.001f);
        new TextLine(font, "A").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        Assert.Contains("/DW 700\n", TestSupport.Latin1(output.ToArray()));
    }
}
}   // End of namespace PDFjet.NET
