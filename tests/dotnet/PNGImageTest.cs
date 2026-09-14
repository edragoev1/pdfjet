/*
 * PNGImageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// PNG decoding against the PngSuite images. The samples are compared as the
/// CRC-32 of the decompressed data. MuPDF decodes the 8-bit, filtered and
/// palette images to the same samples; the 1, 2, 4 and 16 bit grayscale and 16
/// bit RGB samples keep their bit depth here, and MuPDF premultiplies alpha.
/// </summary>
public class PNGImageTest {
    // name, width, height, color type, bit depth, sample bytes, sample CRC, alpha bytes, alpha CRC
    private static readonly string[][] SUITE = {
        new[] {"BASN0G01", "32", "32", "0", "1", "128", "b71a0667", "", ""},
        new[] {"BASN0G02", "32", "32", "0", "2", "256", "c429db1d", "", ""},
        new[] {"BASN0G04", "32", "32", "0", "4", "512", "8089a6e9", "", ""},
        new[] {"BASN0G08", "32", "32", "0", "8", "1024", "784b4a4e", "", ""},
        new[] {"BASN0G16", "32", "32", "0", "16", "2048", "9362f0f0", "", ""},
        new[] {"BASN2C08", "32", "32", "2", "8", "3072", "7855b9bf", "", ""},
        new[] {"BASN2C16", "32", "32", "2", "16", "6144", "c278125a", "", ""},
        new[] {"BASN3P01", "32", "32", "3", "1", "3072", "31ec284b", "", ""},
        new[] {"BASN3P02", "32", "32", "3", "2", "3072", "279a463a", "", ""},
        new[] {"BASN3P04", "32", "32", "3", "4", "3072", "3a9e038e", "", ""},
        new[] {"BASN3P08", "32", "32", "3", "8", "3072", "ff6e2940", "", ""},
        new[] {"BASN6A08", "32", "32", "6", "8", "3072", "a9b0c6b5", "1024", "fa6029ad"},
        new[] {"TP1N3P08", "32", "32", "3", "8", "3072", "8b0a6c2c", "1024", "f83b2838"},
        new[] {"F00N2C08", "32", "32", "2", "8", "3072", "3f1d66ad", "", ""},
        new[] {"F01N2C08", "32", "32", "2", "8", "3072", "11c1b27e", "", ""},
        new[] {"F02N2C08", "32", "32", "2", "8", "3072", "7f1ca785", "", ""},
        new[] {"F03N2C08", "32", "32", "2", "8", "3072", "31645d89", "", ""},
        new[] {"F04N2C08", "32", "32", "2", "8", "3072", "77056a6f", "", ""},
        new[] {"F00N0G08", "32", "32", "0", "8", "1024", "1f18265f", "", ""},
        new[] {"F04N0G08", "32", "32", "0", "8", "1024", "b8006228", "", ""},
        new[] {"S01N3P01", "1", "1", "3", "1", "3", "d243369f", "", ""},
        new[] {"S05N3P02", "5", "5", "3", "2", "75", "1242b6fb", "", ""},
    };

    private static PNGImage Decode(string name) {
        using (Stream stream = TestSupport.Open("PngSuite/" + name + ".PNG")) {
            return new PNGImage(stream);
        }
    }

    [Fact]
    public void DecodesThePngSuiteImages() {
        foreach (string[] row in SUITE) {
            PNGImage png = Decode(row[0]);
            string name = row[0];
            Assert.True(int.Parse(row[1]) == png.GetWidth(), name + " width");
            Assert.True(int.Parse(row[2]) == png.GetHeight(), name + " height");
            Assert.True(int.Parse(row[3]) == png.GetColorType(), name + " color type");
            Assert.True(int.Parse(row[4]) == png.GetBitDepth(), name + " bit depth");
            byte[] samples = Decompressor.Inflate(png.GetData());
            Assert.True(int.Parse(row[5]) == samples.Length, name + " sample bytes " + samples.Length);
            Assert.True(row[6] == TestSupport.Crc32(samples), name + " sample CRC " + TestSupport.Crc32(samples));
            if (row[7].Length == 0) {
                Assert.True(png.GetAlpha() == null, name + " has alpha");
            } else {
                byte[] alpha = Decompressor.Inflate(png.GetAlpha());
                Assert.True(int.Parse(row[7]) == alpha.Length, name + " alpha bytes " + alpha.Length);
                Assert.True(row[8] == TestSupport.Crc32(alpha), name + " alpha CRC " + TestSupport.Crc32(alpha));
            }
        }
    }

    [Fact]
    public void TruecolorTransparencyIsIgnored() {
        // tRNS applies to palette images only; TBRN2C08 has the samples of TP1N3P08.
        PNGImage png = Decode("TBRN2C08");
        Assert.Equal("8b0a6c2c", TestSupport.Crc32(Decompressor.Inflate(png.GetData())));
        Assert.Null(png.GetAlpha());
    }

    [Fact]
    public void Rejects16BitRgbaWithAMessage() {
        Exception e = Assert.ThrowsAny<Exception>(() => Decode("BASN6A16"));
        Assert.Equal("Image with unsupported bit depth == 16", e.Message);
    }

    [Fact]
    public void RejectsDataThatIsNotAPng() {
        Assert.ThrowsAny<Exception>(() => new PNGImage(new MemoryStream(Encoding.ASCII.GetBytes("not a png file"))));
    }

    [Fact]
    public void AnImageFromAPngHasItsSize() {
        using (Stream stream = TestSupport.Open("PngSuite/BASN2C08.PNG")) {
            Image image = new Image(TestSupport.NewPDF(), stream);
            Assert.Equal(32f, image.GetWidth());
            Assert.Equal(32f, image.GetHeight());
        }
    }

    // A PNG file with the IHDR of the size, bit depth and color type, a PLTE
    // chunk when there is a palette, and one IDAT chunk.
    private static byte[] Png(int width, int height, int bitDepth, int colorType, byte[] palette, byte[] idat) {
        MemoryStream ms = new MemoryStream();
        ms.Write(new byte[] {0x89, (byte) 'P', (byte) 'N', (byte) 'G', (byte) '\r', (byte) '\n', 0x1A, (byte) '\n'});
        byte[] ihdr = new byte[13];
        PutInt(ihdr, 0, width);
        PutInt(ihdr, 4, height);
        ihdr[8] = (byte) bitDepth;
        ihdr[9] = (byte) colorType;
        WriteChunk(ms, "IHDR", ihdr);
        if (palette != null) {
            WriteChunk(ms, "PLTE", palette);
        }
        if (idat != null) {
            WriteChunk(ms, "IDAT", idat);
        }
        WriteChunk(ms, "IEND", new byte[0]);
        return ms.ToArray();
    }

    private static void PutInt(byte[] buf, int offset, int value) {
        buf[offset] = (byte) (value >> 24);
        buf[offset + 1] = (byte) (value >> 16);
        buf[offset + 2] = (byte) (value >> 8);
        buf[offset + 3] = (byte) value;
    }

    private static void WriteChunk(MemoryStream ms, string type, byte[] data) {
        byte[] name = Encoding.ASCII.GetBytes(type);
        CRC32 crc = new CRC32();
        crc.Update(name, 0, 4);
        crc.Update(data, 0, data.Length);
        byte[] length = new byte[4];
        PutInt(length, 0, data.Length);
        ms.Write(length);
        ms.Write(name);
        ms.Write(data);
        byte[] checksum = new byte[4];
        PutInt(checksum, 0, unchecked((int) crc.GetValue()));
        ms.Write(checksum);
    }

    private static string DecodeError(byte[] png) {
        return Assert.ThrowsAny<Exception>(() => new PNGImage(new MemoryStream(png))).Message;
    }

    /// <summary>A stream that returns at most 3 bytes from each read.</summary>
    private sealed class FewBytesStream : MemoryStream {
        internal FewBytesStream(byte[] data) : base(data) {
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return base.Read(buffer, offset, Math.Min(count, 3));
        }

        public override int Read(Span<byte> buffer) {
            return base.Read(buffer[..Math.Min(buffer.Length, 3)]);
        }
    }

    [Fact]
    public void DecodesTheRowsOfTheImageAndIgnoresDataAfterThem() {
        byte[] rgb = {1, 2, 3, 4, 5, 6};
        byte[] rows = {0, 1, 2, 3, 4, 5, 6};
        byte[] longer = {0, 1, 2, 3, 4, 5, 6, 9, 9, 9};
        foreach (byte[] data in new[] {rows, longer}) {
            PNGImage png = new PNGImage(new MemoryStream(Png(2, 1, 8, 2, null, Compressor.Deflate(data))));
            Assert.Equal(rgb, Decompressor.Inflate(png.GetData()));
        }
    }

    [Fact]
    public void RejectsImageDataShorterThanTheImage() {
        byte[] rows = {0, 1, 2, 3};
        Assert.Equal("The PNG image data is shorter than the image.",
                DecodeError(Png(2, 1, 8, 2, null, Compressor.Deflate(rows))));
    }

    [Fact]
    public void RejectsAnImageLargerThanTheLimitBeforeDecodingIt() {
        // 20000 x 20000 RGB samples are 1.2 GB.
        Assert.Equal("The PNG image is larger than 268435456 bytes.",
                DecodeError(Png(20000, 20000, 8, 2, null, Compressor.Deflate(new byte[1]))));
        // 10000 x 10000 palette indexes of 1 bit are 12.5 MB, but 400 MB of RGB and alpha.
        Assert.Equal("The PNG image is larger than 268435456 bytes.",
                DecodeError(Png(10000, 10000, 1, 3, new byte[6], Compressor.Deflate(new byte[1]))));
        // The largest size, where the sizes multiplied overflow a long.
        int max = int.MaxValue;
        Assert.Equal("The PNG image is larger than 268435456 bytes.",
                DecodeError(Png(max, max, 16, 6, null, Compressor.Deflate(new byte[1]))));
        Assert.Equal("The PNG image is larger than 268435456 bytes.",
                DecodeError(Png(max, max, 1, 3, new byte[6], Compressor.Deflate(new byte[1]))));
        Assert.Equal("The PNG image is larger than 268435456 bytes.",
                DecodeError(Png(max, 1, 16, 6, null, Compressor.Deflate(new byte[1]))));
    }

    [Fact]
    public void RejectsAnInvalidSizeBitDepthColorTypeOrPalette() {
        byte[] idat = Compressor.Deflate(new byte[] {0, 0, 0, 0});
        Assert.Equal("Invalid PNG image size.", DecodeError(Png(0, 1, 8, 2, null, idat)));
        Assert.Equal("Invalid PNG image size.", DecodeError(Png(-1, 1, 8, 2, null, idat)));
        Assert.Equal("Invalid PNG bit depth 4 for color type 2.", DecodeError(Png(1, 1, 4, 2, null, idat)));
        Assert.Equal("Invalid PNG color type 5.", DecodeError(Png(1, 1, 8, 5, null, idat)));
        Assert.Equal("Invalid PNG color type 200.", DecodeError(Png(1, 1, 8, 200, null, idat)));
        Assert.Equal("Invalid PNG bit depth 200 for color type 2.", DecodeError(Png(1, 1, 200, 2, null, idat)));
        Assert.Equal("The PNG palette image has no PLTE chunk.", DecodeError(Png(1, 1, 8, 3, null, idat)));
        Assert.Equal("The PNG image has no image data.", DecodeError(Png(1, 1, 8, 2, null, null)));
    }

    [Fact]
    public void RejectsAChunkLengthThatTheFileDoesNotHave() {
        byte[] valid = Png(1, 1, 8, 2, null, Compressor.Deflate(new byte[] {0, 0, 0, 0}));
        // The IDAT chunk starts after the signature and the 25 bytes of the IHDR chunk.
        byte[] lying = valid[..(33 + 18)];
        lying[33] = 0x7F;
        lying[34] = 0xFF;
        lying[35] = 0xFF;
        lying[36] = 0xF0;
        Assert.Equal("Unexpected end of the PNG stream.", DecodeError(lying));
    }

    [Fact]
    public void ReadsAStreamThatReturnsFewBytesAtATime() {
        byte[] file = File.ReadAllBytes(TestSupport.RepoPath("PngSuite/BASN2C08.PNG"));
        PNGImage png = new PNGImage(new FewBytesStream(file));
        Assert.Equal("7855b9bf", TestSupport.Crc32(Decompressor.Inflate(png.GetData())));
    }

    [Fact]
    public void ATruecolorImageWithASuggestedPaletteIsDecodedAsTruecolor() {
        PNGImage png = Decode("PS1N2C16");
        Assert.Equal(2, png.GetColorType());
        Assert.Equal(6144, Decompressor.Inflate(png.GetData()).Length);
    }

    [Fact]
    public void DecodesGrayscaleWithAlpha() {
        PNGImage png = Decode("BASN4A08");
        Assert.Equal(32, png.GetWidth());
        Assert.Equal(32, png.GetHeight());
        Assert.Equal(4, png.GetColorType());
        Assert.Equal(8, png.GetBitDepth());
        byte[] gray = Decompressor.Inflate(png.GetData());
        Assert.Equal(1024, gray.Length);
        Assert.Equal("bfc7e22b", TestSupport.Crc32(gray));
        byte[] alpha = Decompressor.Inflate(png.GetAlpha());
        Assert.Equal(1024, alpha.Length);
        Assert.Equal("fa6029ad", TestSupport.Crc32(alpha));
    }

    [Fact]
    public void AnImageFromAGrayscalePngWithAlphaIsGrayWithASoftMask() {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        using (Stream stream = TestSupport.Open("PngSuite/BASN4A08.PNG")) {
            Image image = new Image(pdf, stream);
            image.DrawOn(new Page(pdf, Letter.PORTRAIT));
        }
        pdf.Complete();
        string raw = TestSupport.Latin1(output.ToArray());
        Assert.Contains("DeviceGray", raw);
        Assert.Contains("/SMask", raw);
        Assert.DoesNotContain("DeviceRGB", raw);
    }

    [Fact]
    public void Rejects16BitGrayscaleWithAlphaWithAMessage() {
        Exception e = Assert.ThrowsAny<Exception>(() => Decode("BASN4A16"));
        Assert.Equal("Image with unsupported bit depth == 16", e.Message);
    }

    [Fact]
    public void RejectsInterlacedImagesWithAClearError() {
        Exception e = Assert.ThrowsAny<Exception>(() => Decode("BASI0G08"));
        Assert.Equal("Interlaced PNG images are not supported.\n"
                + "Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png", e.Message);
    }
}
}
