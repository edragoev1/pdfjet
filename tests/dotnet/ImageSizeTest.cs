/*
 * ImageSizeTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace PDFjet.NET {
public class ImageSizeTest {
    // The size read from the header against Image: an image Image takes has
    // the size it draws it at, and an image whose header is refused Image
    // refuses too. Returns whether Image took it.
    static bool Agrees(String name, byte[] data, List<String> problems) {
        ImageSize size = null;
        Exception sizeError = null;
        try {
            size = ImageSize.Read(new MemoryStream(data));
        } catch (Exception e) {
            sizeError = e;
        }
        Image image;
        try {
            image = new Image(new PDF(new MemoryStream()), new MemoryStream(data));
        } catch (Exception) {
            return false;
        }
        if (sizeError != null) {
            problems.Add(name + ": Image takes it, ImageSize refuses it: " + sizeError.Message);
            return true;
        }
        int pixelWidth = Pixels(image, "pixelWidth");
        int pixelHeight = Pixels(image, "pixelHeight");
        if (size.GetWidth() != image.GetWidth() || size.GetHeight() != image.GetHeight()
                || size.GetPixelWidth() != pixelWidth || size.GetPixelHeight() != pixelHeight) {
            problems.Add(name + ": " + size.GetWidth() + " by " + size.GetHeight() + ", "
                    + size.GetPixelWidth() + " by " + size.GetPixelHeight() + " pixels; Image "
                    + image.GetWidth() + " by " + image.GetHeight() + ", " + pixelWidth + " by " + pixelHeight);
        }
        return true;
    }

    static int Pixels(Image image, String field) {
        FieldInfo f = typeof(Image).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        return (int) f.GetValue(image);
    }

    [Fact]
    public void TheSizeIsTheOneImageDrawsAt() {
        List<String> files = new List<String>();
        foreach (String dir in new String[] {"images", "tests/data", ".images"}) {
            String root = TestSupport.RepoPath(dir);
            if (!Directory.Exists(root)) {
                continue;
            }
            foreach (String f in Directory.GetFiles(root, "*", SearchOption.AllDirectories)) {
                String ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp") {
                    files.Add(f);
                }
            }
        }
        List<String> problems = new List<String>();
        int taken = 0;
        foreach (String f in files) {
            if (Agrees(f, File.ReadAllBytes(f), problems)) {
                taken++;
            }
        }
        Assert.True(problems.Count == 0, String.Join("\n", problems.GetRange(0, Math.Min(20, problems.Count))));
        Assert.True(taken >= 20, taken + " images taken, of " + files.Count);
    }

    [Fact]
    public void AHugePNGIsRefusedFromItsHeader() {
        MemoryStream png = new MemoryStream();
        png.Write(new byte[] {0x89, (byte) 'P', (byte) 'N', (byte) 'G', 0x0D, 0x0A, 0x1A, 0x0A}, 0, 8);
        Chunk(png, "IHDR", new byte[] {0, 1, 0x86, 0xA0, 0, 1, 0x86, 0xA0, 8, 6, 0, 0, 0});
        Chunk(png, "IEND", new byte[0]);
        Exception e = Assert.ThrowsAny<Exception>(() => ImageSize.Read(new MemoryStream(png.ToArray())));
        Assert.Contains("larger than", e.Message);
    }

    [Fact]
    public void AJPEGTurnedIsItsHeightByItsWidth() {
        foreach (String orientation in new String[] {"1", "3", "6", "8"}) {
            byte[] data = File.ReadAllBytes(TestSupport.RepoPath("tests/data/jpeg/orientation-" + orientation + ".jpg"));
            ImageSize size = ImageSize.Read(new MemoryStream(data));
            Assert.Equal(32f, size.GetWidth());
            Assert.Equal(16f, size.GetHeight());
        }
    }

    [Fact]
    public void RefusesWhatImageRefusesInTheHeader() {
        // The review of 9 October 2026: an 8-bit BMP of 1,000 colors and a PNG
        // whose one IDAT is empty were sizes, and Image refused them.
        byte[] bmp = new byte[54];
        bmp[0] = (byte) 'B';
        bmp[1] = (byte) 'M';
        void Le32(int at, int v) {
            bmp[at] = (byte) v;
            bmp[at + 1] = (byte) (v >> 8);
            bmp[at + 2] = (byte) (v >> 16);
            bmp[at + 3] = (byte) (v >> 24);
        }
        Le32(10, 54);
        Le32(14, 40);
        Le32(18, 1);
        Le32(22, 1);
        bmp[26] = 1;
        bmp[28] = 8;
        Le32(46, 1000);
        Exception e = Assert.ThrowsAny<Exception>(() => ImageSize.Read(new MemoryStream(bmp)));
        Assert.Contains("palette", e.Message);
        Assert.ThrowsAny<Exception>(() => new Image(new PDF(new MemoryStream()), new MemoryStream(bmp)));

        MemoryStream png = new MemoryStream();
        png.Write(new byte[] {0x89, (byte) 'P', (byte) 'N', (byte) 'G', 0x0D, 0x0A, 0x1A, 0x0A}, 0, 8);
        Chunk(png, "IHDR", new byte[] {0, 0, 0, 1, 0, 0, 0, 1, 8, 0, 0, 0, 0});
        Chunk(png, "IDAT", new byte[0]);
        Chunk(png, "IEND", new byte[0]);
        Assert.ThrowsAny<Exception>(() => ImageSize.Read(new MemoryStream(png.ToArray())));
        Assert.ThrowsAny<Exception>(() => new Image(new PDF(new MemoryStream()), new MemoryStream(png.ToArray())));

        // A JPEG that ends inside its frame header, after the size, is a size,
        // as in the other ports: its data is Image's to refuse.
        byte[] jpg = {0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03};
        ImageSize size = ImageSize.Read(new MemoryStream(jpg));
        Assert.Equal(32, size.GetPixelWidth());
        Assert.Equal(16, size.GetPixelHeight());
    }

    static void Chunk(MemoryStream output, String type, byte[] data) {
        int n = data.Length;
        output.Write(new byte[] {(byte) (n >> 24), (byte) (n >> 16), (byte) (n >> 8), (byte) n}, 0, 4);
        byte[] t = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(t, 0, 4);
        output.Write(data, 0, data.Length);
        CRC32 crc = new CRC32();
        crc.Update(t, 0, 4);
        crc.Update(data, 0, data.Length);
        long c = crc.GetValue();
        output.Write(new byte[] {(byte) (c >> 24), (byte) (c >> 16), (byte) (c >> 8), (byte) c}, 0, 4);
    }
}
}
