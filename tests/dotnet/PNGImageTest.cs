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
        new[] {"BGAN6A08", "32", "32", "6", "8", "3072", "a9b0c6b5", "1024", "fa6029ad"},
        new[] {"F01N0G08", "32", "32", "0", "8", "1024", "1868217f", "", ""},
        new[] {"F02N0G08", "32", "32", "0", "8", "1024", "79b9c9de", "", ""},
        new[] {"F03N0G08", "32", "32", "0", "8", "1024", "a373c644", "", ""},
        new[] {"OI1N0G16", "32", "32", "0", "16", "2048", "9362f0f0", "", ""},
        new[] {"OI1N2C16", "32", "32", "2", "16", "6144", "c278125a", "", ""},
        new[] {"OI2N0G16", "32", "32", "0", "16", "2048", "9362f0f0", "", ""},
        new[] {"OI2N2C16", "32", "32", "2", "16", "6144", "c278125a", "", ""},
        new[] {"OI4N0G16", "32", "32", "0", "16", "2048", "9362f0f0", "", ""},
        new[] {"OI4N2C16", "32", "32", "2", "16", "6144", "c278125a", "", ""},
        new[] {"OI9N0G16", "32", "32", "0", "16", "2048", "9362f0f0", "", ""},
        new[] {"OI9N2C16", "32", "32", "2", "16", "6144", "c278125a", "", ""},
        new[] {"S02N3P01", "2", "2", "3", "1", "12", "9e931d85", "", ""},
        new[] {"S03N3P01", "3", "3", "3", "1", "27", "6916380e", "", ""},
        new[] {"S04N3P01", "4", "4", "3", "1", "48", "c2e0d49b", "", ""},
        new[] {"S06N3P02", "6", "6", "3", "2", "108", "d7589540", "", ""},
        new[] {"S07N3P02", "7", "7", "3", "2", "147", "d2ccf489", "", ""},
        new[] {"S08N3P02", "8", "8", "3", "2", "192", "2ba1b03e", "", ""},
        new[] {"S09N3P02", "9", "9", "3", "2", "243", "9762d2ed", "", ""},
        new[] {"S32N3P04", "32", "32", "3", "4", "3072", "ad01f44d", "", ""},
        new[] {"S33N3P04", "33", "33", "3", "4", "3267", "d2f4ae68", "", ""},
        new[] {"S34N3P04", "34", "34", "3", "4", "3468", "bbeda3f7", "", ""},
        new[] {"S35N3P04", "35", "35", "3", "4", "3675", "99293acf", "", ""},
        new[] {"S36N3P04", "36", "36", "3", "4", "3888", "f51a96e0", "", ""},
        new[] {"S37N3P04", "37", "37", "3", "4", "4107", "920758a4", "", ""},
        new[] {"S38N3P04", "38", "38", "3", "4", "4332", "eb3bf324", "", ""},
        new[] {"S39N3P04", "39", "39", "3", "4", "4563", "c06d7da1", "", ""},
        new[] {"S40N3P04", "40", "40", "3", "4", "4800", "0d4658a0", "", ""},
        new[] {"TBBN3P08", "32", "32", "3", "8", "3072", "8b0a6c2c", "1024", "f83b2838"},
        new[] {"TBGN3P08", "32", "32", "3", "8", "3072", "8b0a6c2c", "1024", "f83b2838"},
        new[] {"TBWN3P08", "32", "32", "3", "8", "3072", "8b0a6c2c", "1024", "f83b2838"},
        new[] {"TBYN3P08", "32", "32", "3", "8", "3072", "8b0a6c2c", "1024", "f83b2838"},
        new[] {"Z00N2C08", "32", "32", "2", "8", "3072", "f8f7d651", "", ""},
        new[] {"Z03N2C08", "32", "32", "2", "8", "3072", "f8f7d651", "", ""},
        new[] {"Z06N2C08", "32", "32", "2", "8", "3072", "f8f7d651", "", ""},
        new[] {"Z09N2C08", "32", "32", "2", "8", "3072", "f8f7d651", "", ""},
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
        return Png(width, height, bitDepth, colorType, 0, 0, palette, idat);
    }

    // The same file with the compression and the filter method of its IHDR
    // chunk, which are 0 in every PNG file that is defined.
    private static byte[] Png(int width, int height, int bitDepth, int colorType,
            int compression, int filter, byte[] palette, byte[] idat) {
        MemoryStream ms = new MemoryStream();
        ms.Write(new byte[] {0x89, (byte) 'P', (byte) 'N', (byte) 'G', (byte) '\r', (byte) '\n', 0x1A, (byte) '\n'});
        byte[] ihdr = new byte[13];
        PutInt(ihdr, 0, width);
        PutInt(ihdr, 4, height);
        ihdr[8] = (byte) bitDepth;
        ihdr[9] = (byte) colorType;
        ihdr[10] = (byte) compression;
        ihdr[11] = (byte) filter;
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
    public void APaletteIndexPastThePaletteIsBlack() {
        // A palette of 2 colors, and the indexes 1, 2 and 255.
        byte[] palette = {10, 20, 30, 40, 50, 60};
        PNGImage png = new PNGImage(new MemoryStream(
                Png(3, 1, 8, 3, palette, Compressor.Deflate(new byte[] {0, 1, 2, 255}))));
        Assert.Equal(new byte[] {40, 50, 60, 0, 0, 0, 0, 0, 0}, Decompressor.Inflate(png.GetData()));
    }

    [Fact]
    public void RejectsAPaletteOfNoColorsOrMoreThan256() {
        byte[] idat = Compressor.Deflate(new byte[] {0, 0});
        Assert.Equal("Incorrect palette length.", DecodeError(Png(1, 1, 8, 3, new byte[0], idat)));
        Assert.Equal("Incorrect palette length.", DecodeError(Png(1, 1, 8, 3, new byte[3*257], idat)));
        Assert.Equal("Incorrect palette length.", DecodeError(Png(1, 1, 8, 3, new byte[4], idat)));
    }

    [Fact]
    public void RejectsAnUnknownCompressionOrFilterMethod() {
        // Only the deflate compression method and the adaptive filter method
        // are defined. libpng refuses a file of another one, and so does
        // Pillow for the filter method, where the rows of this one would be
        // read as if it were 0.
        byte[] idat = Compressor.Deflate(new byte[] {0, 0});
        Assert.Equal("Unknown PNG compression method.",
                DecodeError(Png(1, 1, 8, 0, 1, 0, null, idat)));
        Assert.Equal("Unknown PNG filter method.",
                DecodeError(Png(1, 1, 8, 0, 0, 1, null, idat)));
        new PNGImage(new MemoryStream(Png(1, 1, 8, 0, 0, 0, null, idat)));
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
    // A truecolor PNG with a pHYs chunk of the given pixels per unit and unit:
    // 1 is the metre and 0 is a ratio of the axes with no size.
    private static byte[] PngWithPhys(int width, int height, int x, int y, int unit) {
        MemoryStream ms = new MemoryStream();
        ms.Write(new byte[] {0x89, (byte) 'P', (byte) 'N', (byte) 'G', (byte) '\r', (byte) '\n', 0x1A, (byte) '\n'});
        byte[] ihdr = new byte[13];
        PutInt(ihdr, 0, width);
        PutInt(ihdr, 4, height);
        ihdr[8] = 8;
        ihdr[9] = 2;
        WriteChunk(ms, "IHDR", ihdr);
        byte[] phys = new byte[9];
        PutInt(phys, 0, x);
        PutInt(phys, 4, y);
        phys[8] = (byte) unit;
        WriteChunk(ms, "pHYs", phys);
        WriteChunk(ms, "IDAT", Compressor.Deflate(new byte[height*(1 + 3*width)]));
        WriteChunk(ms, "IEND", new byte[0]);
        return ms.ToArray();
    }

    [Fact]
    public void ThePhysicalSizeChunkGivesTheSizeTheImageIsDrawnAt() {
        // A pHYs chunk whose unit is the metre says how large the image is
        // meant to be, so it is drawn that size rather than one point for each
        // of its pixels. 11811 pixels per metre is 300 dots per inch, and 1520
        // by 400 of them are 364.8 by 96 points.
        PNGImage png = new PNGImage(TestSupport.Open("images/rgba-8bit-chunks.png"));
        Assert.Equal(1520, png.GetWidth());
        Assert.Equal(400, png.GetHeight());
        Assert.True(Math.Abs(png.GetPhysicalWidth() - 364.8f) < 0.01f);
        Assert.True(Math.Abs(png.GetPhysicalHeight() - 96f) < 0.01f);

        PDF pdf = TestSupport.NewPDF();
        Image image = new Image(pdf, TestSupport.Open("images/rgba-8bit-chunks.png"));
        Assert.True(Math.Abs(image.GetWidth() - 364.8f) < 0.01f,
                "the image is not drawn at the size it asks for");
        Assert.True(Math.Abs(image.GetHeight() - 96f) < 0.01f,
                "the image is not drawn at the size it asks for");
    }

    [Fact]
    public void ThePhysicalSizeLeavesThePixelsOfTheImageObjectAlone() {
        // The size the image is drawn at is not the size of its samples: the
        // image object of the PDF holds the pixels, whatever the chunk says.
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Image image = new Image(pdf, TestSupport.Open("images/rgba-8bit-chunks.png"));
        image.SetLocation(0f, 0f);
        image.DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Contains("/Width 1520\n", raw);
        Assert.Contains("/Height 400\n", raw);
    }

    [Fact]
    public void AnImageWithNoPhysicalSizeIsDrawnAtOnePointForEachPixel() {
        // Most PNG files carry no pHYs chunk, and are drawn as they were.
        PNGImage png = new PNGImage(TestSupport.Open("PngSuite/BASN2C08.PNG"));
        Assert.Equal(0f, png.GetPhysicalWidth());
        Assert.Equal(0f, png.GetPhysicalHeight());
        PDF pdf = TestSupport.NewPDF();
        Image image = new Image(pdf, TestSupport.Open("PngSuite/BASN2C08.PNG"));
        Assert.True(Math.Abs(image.GetWidth() - 32f) < 0.01f);
        Assert.True(Math.Abs(image.GetHeight() - 32f) < 0.01f);
    }

    [Fact]
    public void APhysicalSizeChunkThatGivesNoSizeIsPassedOver() {
        // Unit 0 is the ratio of the two axes and says nothing about how large
        // the image is, and a count of zero pixels gives no size either.
        Assert.Equal(0f, new PNGImage(new MemoryStream(PngWithPhys(8, 8, 1, 4, 0))).GetPhysicalWidth());
        Assert.Equal(0f, new PNGImage(new MemoryStream(PngWithPhys(8, 8, 0, 4724, 1))).GetPhysicalWidth());
        Assert.Equal(0f, new PNGImage(new MemoryStream(PngWithPhys(8, 8, 4724, 0, 1))).GetPhysicalWidth());
    }

    [Fact]
    public void AnImageOfPixelsThatAreNotSquareIsDrawnWithEachAxisOfItsOwnSize() {
        // The chunk gives the pixels per metre of each axis on its own, so an
        // image of 4724 across and 2362 down is twice as tall as it is wide
        // for the same count of pixels.
        PNGImage png = new PNGImage(new MemoryStream(PngWithPhys(20, 20, 4724, 2362, 1)));
        Assert.True(Math.Abs(png.GetPhysicalWidth() - 12f) < 0.01f);
        Assert.True(Math.Abs(png.GetPhysicalHeight() - 24f) < 0.01f);
    }
    // The name of a file whose samples must be those of another, and why: the
    // PngSuite images of these groups are one image written in several ways,
    // so a decoder that reads them all gets the same pixels from each.
    private static readonly string[][] SAME = {
        new[] {"BASN0G16", "OI1N0G16", "OI2N0G16", "OI4N0G16", "OI9N0G16"},
        new[] {"BASN2C16", "OI1N2C16", "OI2N2C16", "OI4N2C16", "OI9N2C16"},
        new[] {"Z00N2C08", "Z03N2C08", "Z06N2C08", "Z09N2C08"},
        new[] {"TP1N3P08", "TBBN3P08", "TBGN3P08", "TBWN3P08", "TBYN3P08"},
        new[] {"BASN6A08", "BGAN6A08"},
    };

    [Fact]
    public void TheImagesThatAreOneImageWrittenSeveralWaysDecodeAlike() {
        // The IDAT chunks of an image may be split any way the writer likes,
        // its data deflated at any level, and a background color or a
        // background with alpha carried beside it; none of that is the image.
        // These are the groups of PngSuite that say so, and each of them is
        // one image: a decoder that reads the four OI files differently, or
        // the four Z files, has read the chunks and not the image.
        foreach (string[] group in SAME) {
            string first = TestSupport.Crc32(Decompressor.Inflate(Decode(group[0]).GetData()));
            for (int i = 1; i < group.Length; i++) {
                Assert.True(first == TestSupport.Crc32(Decompressor.Inflate(Decode(group[i]).GetData())),
                        group[i] + " does not have the samples of " + group[0]);
            }
        }
    }

    [Fact]
    public void AnImageOfEverySizeFromOneToFortyPixelsIsDecodedWhole() {
        // The rows of a palette image of 1, 2 or 4 bits end in the bits that
        // pad them to a byte, and a width that is not a whole number of bytes
        // is where a decoder reads the padding as pixels or loses the last
        // ones. PngSuite has a file of every such width.
        string[] names = {"S01N3P01", "S02N3P01", "S03N3P01", "S04N3P01",
                "S05N3P02", "S06N3P02", "S07N3P02", "S08N3P02", "S09N3P02",
                "S32N3P04", "S33N3P04", "S34N3P04", "S35N3P04", "S36N3P04",
                "S37N3P04", "S38N3P04", "S39N3P04", "S40N3P04"};
        foreach (string name in names) {
            PNGImage png = Decode(name);
            // The number in the name is the width and the height of the file.
            int size = int.Parse(name.Substring(1, 2));
            Assert.Equal(size, png.GetWidth());
            Assert.Equal(size, png.GetHeight());
            // A palette image is three bytes a pixel, whatever its bit depth.
            Assert.Equal(3*size*size, Decompressor.Inflate(png.GetData()).Length);
        }
    }
}
}
