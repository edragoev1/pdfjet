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

    // A BMP file of the 2 by 2 pixels with a header of the size, 40 bytes or
    // more, the bits per pixel, the compression, the masks after the first 40
    // bytes of the header, the palette of 0xRRGGBB colors, and the pixels of
    // the bottom row and then the top row, each padded to 4 bytes.
    private static byte[] Bmp(int headerSize, int bitsPerPixel, int compression,
            int[] masks, int[] palette, byte[] bottomRow, byte[] topRow) {
        int colors = (palette == null) ? 0 : palette.Length;
        int afterHeader = (headerSize == 40 && masks != null) ? 12 : 0;
        int offset = 14 + headerSize + afterHeader + 4 * colors;
        int rowSize = (bottomRow.Length + 3) & ~3;
        byte[] bmp = new byte[offset + 2 * rowSize];
        BinaryWriter writer = new BinaryWriter(new MemoryStream(bmp));   // Little endian
        writer.Write((byte) 'B'); writer.Write((byte) 'M');
        writer.Write(bmp.Length); writer.Write(0); writer.Write(offset);
        writer.Write(headerSize); writer.Write(2); writer.Write(2);
        writer.Write((short) 1); writer.Write((short) bitsPerPixel);
        writer.Write(compression); writer.Write(2 * rowSize); writer.Write(2835); writer.Write(2835);
        writer.Write(colors); writer.Write(0);
        if (masks != null) {
            writer.Write(masks[0]); writer.Write(masks[1]); writer.Write(masks[2]);
        }
        writer.Seek(offset - 4 * colors, SeekOrigin.Begin);     // The rest of a larger header is 0
        for (int i = 0; i < colors; i++) {
            writer.Write((byte) palette[i]); writer.Write((byte) (palette[i] >> 8));
            writer.Write((byte) (palette[i] >> 16)); writer.Write((byte) 0);
        }
        writer.Write(bottomRow);
        writer.Seek(offset + rowSize, SeekOrigin.Begin);
        writer.Write(topRow);
        return bmp;
    }

    private static byte[] Decode(byte[] bmp) {
        return Decompressor.Inflate(new BMPImage(new MemoryStream(bmp)).GetData());
    }

    // The pixels as little endian 16 bit values.
    private static byte[] Shorts(params int[] pixels) {
        byte[] bytes = new byte[2 * pixels.Length];
        for (int i = 0; i < pixels.Length; i++) {
            bytes[2 * i] = (byte) pixels[i];
            bytes[2 * i + 1] = (byte) (pixels[i] >> 8);
        }
        return bytes;
    }

    private static byte[] Bytes(params int[] values) {
        byte[] bytes = new byte[values.Length];
        for (int i = 0; i < values.Length; i++) {
            bytes[i] = (byte) values[i];
        }
        return bytes;
    }

    [Fact]
    public void AThirtyTwoBitPixelIsBlueGreenRedAndAByteThatIsNotAColor() {
        byte[] bmp = Bmp(40, 32, 0, null, null,
                Bytes(255, 0, 0, 0, 255, 255, 255, 0), Bytes(0, 0, 255, 0, 0, 255, 0, 0));
        Assert.Equal(RGB, Decode(bmp));
    }

    [Fact]
    public void TheMasksOfSixteenAndThirtyTwoBitPixelsAreRead() {
        // 5 bits a color without masks; 31 of 5 bits is 255.
        Assert.Equal(RGB, Decode(Bmp(40, 16, 0, null, null,
                Shorts(0x001F, 0x7FFF), Shorts(0x7C00, 0x03E0))));
        // 5, 6 and 5 bits.
        Assert.Equal(RGB, Decode(Bmp(40, 16, 3, new int[] {0xF800, 0x07E0, 0x001F}, null,
                Shorts(0x001F, 0xFFFF), Shorts(0xF800, 0x07E0))));
        // Red in the low byte, in a 108 byte header.
        Assert.Equal(RGB, Decode(Bmp(108, 32, 3, new int[] {0x000000FF, 0x0000FF00, 0x00FF0000}, null,
                Bytes(0, 0, 255, 0, 255, 255, 255, 0), Bytes(255, 0, 0, 0, 0, 255, 0, 0))));
    }

    [Fact]
    public void ThePaletteFollowsAHeaderOfAnySize() {
        int[] palette = {0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF};
        Assert.Equal(RGB, Decode(Bmp(40, 8, 0, null, palette, Bytes(2, 3), Bytes(0, 1))));
        Assert.Equal(RGB, Decode(Bmp(124, 8, 0, null, palette, Bytes(2, 3), Bytes(0, 1))));
    }

    [Fact]
    public void RejectsACompressedImage() {
        int[] palette = {0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF};
        // RLE8: a run of 1 pixel of color 2, 1 of color 3, the end of the bitmap
        Assert.Equal("Compressed BMP images are not supported.",
                DecodeError(Bmp(40, 8, 1, null, palette, Bytes(1, 2, 1, 3), Bytes(0, 1, 0, 0))));
    }
}
}
