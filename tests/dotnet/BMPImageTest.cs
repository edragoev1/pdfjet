/*
 * BMPImageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using Xunit;

namespace PDFjet.NET {
public class BMPImageTest {
    // Red, green on the top row; blue, white on the bottom row.
    private static readonly int[][][] PIXELS = {
        new[] {new[] {255, 0, 0}, new[] {0, 255, 0}},
        new[] {new[] {0, 0, 255}, new[] {255, 255, 255}},
    };

    private static readonly byte[] RGB = {255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255};

    private static byte[] Bmp24(bool topDown) {
        int width = 2;
        int height = 2;
        int rowSize = (width * 3 + 3) & ~3;
        MemoryStream ms = new MemoryStream();
        BinaryWriter w = new BinaryWriter(ms);     // little endian
        w.Write((byte) 'B');
        w.Write((byte) 'M');
        w.Write(54 + rowSize * height);
        w.Write(0);
        w.Write(54);
        w.Write(40);
        w.Write(width);
        w.Write(topDown ? -height : height);
        w.Write((short) 1);
        w.Write((short) 24);
        w.Write(0);
        w.Write(rowSize * height);
        w.Write(2835);
        w.Write(2835);
        w.Write(0);
        w.Write(0);
        for (int i = 0; i < height; i++) {
            int[][] row = PIXELS[topDown ? i : height - 1 - i];
            foreach (int[] pixel in row) {
                w.Write((byte) pixel[2]);
                w.Write((byte) pixel[1]);
                w.Write((byte) pixel[0]);
            }
            for (int pad = width * 3; pad < rowSize; pad++) {
                w.Write((byte) 0);
            }
        }
        w.Flush();
        return ms.ToArray();
    }

    [Fact]
    public void BottomUpRowsAreReadTopRowFirst() {
        BMPImage bmp = new BMPImage(new MemoryStream(Bmp24(false)));
        Assert.Equal(2, bmp.GetWidth());
        Assert.Equal(2, bmp.GetHeight());
        Assert.Equal(RGB, Decompressor.Inflate(bmp.GetData()));
    }

    [Fact]
    public void TopDownRowsAreReadTheSame() {
        Assert.Equal(RGB, Decompressor.Inflate(new BMPImage(new MemoryStream(Bmp24(true))).GetData()));
    }

    // The 54 byte header of a BMP file of the size, bits per pixel and palette colors.
    private static byte[] Header(int width, int height, int bitsPerPixel, int colors) {
        MemoryStream ms = new MemoryStream();
        BinaryWriter w = new BinaryWriter(ms);     // little endian
        w.Write((byte) 'B');
        w.Write((byte) 'M');
        w.Write(54);
        w.Write(0);
        w.Write(54);
        w.Write(40);
        w.Write(width);
        w.Write(height);
        w.Write((short) 1);
        w.Write((short) bitsPerPixel);
        w.Write(0);
        w.Write(0);
        w.Write(2835);
        w.Write(2835);
        w.Write(colors);
        w.Write(0);
        w.Flush();
        return ms.ToArray();
    }

    private static string DecodeError(byte[] bmp) {
        return Assert.ThrowsAny<Exception>(() => new BMPImage(new MemoryStream(bmp))).Message;
    }

    [Fact]
    public void RejectsAnInvalidSize() {
        Assert.Equal("Invalid BMP image size.", DecodeError(Header(0, 2, 24, 0)));
        Assert.Equal("Invalid BMP image size.", DecodeError(Header(2, 0, 24, 0)));
        Assert.Equal("Invalid BMP image size.", DecodeError(Header(-2, 2, 24, 0)));
        Assert.Equal("Invalid BMP image size.", DecodeError(Header(2, int.MinValue, 24, 0)));
    }

    [Fact]
    public void RejectsAnImageLargerThanTheLimitBeforeReadingIt() {
        // 20000 x 20000 pixels are 1.2 GB of RGB; the file has only the header.
        Assert.Equal("The BMP image is larger than 268435456 bytes.", DecodeError(Header(20000, 20000, 24, 0)));
        // The largest size, where the sizes multiplied overflow a long.
        int max = int.MaxValue;
        Assert.Equal("The BMP image is larger than 268435456 bytes.", DecodeError(Header(max, max, 32, 0)));
        Assert.Equal("The BMP image is larger than 268435456 bytes.", DecodeError(Header(max, 1, 32, 0)));
    }

    [Fact]
    public void RejectsAnUnsupportedBitDepthOrALargePalette() {
        Assert.Equal("Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images",
                DecodeError(Header(2, 2, 2, 0)));
        Assert.Equal("Invalid BMP palette size 2147483647.", DecodeError(Header(2, 2, 8, 0x7FFFFFFF)));
    }

    [Fact]
    public void ATruncatedFileThrows() {
        byte[] truncated = Bmp24(false)[..60];
        Assert.ThrowsAny<Exception>(() => new BMPImage(new MemoryStream(truncated)));
    }
}
}
