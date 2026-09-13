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

    [Fact]
    public void ATruncatedFileThrows() {
        byte[] truncated = Bmp24(false)[..60];
        Assert.ThrowsAny<Exception>(() => new BMPImage(new MemoryStream(truncated)));
    }
}
}
