/*
 * JPGOrientationTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using Xunit;

namespace PDFjet.NET {

/// <summary>
/// A JPEG with an Exif orientation is drawn as it is meant to be seen:
/// tests/data/jpeg/orientation-N.jpg is stored turned so that it is seen as
/// 32 by 16 pixels, as orientation-1.jpg is; 6 and 8 are 16 by 32 as stored.
/// </summary>
public sealed class JPGOrientationTest {
    private static byte[] OrientationJPEG(string orientation) {
        return File.ReadAllBytes(TestSupport.RepoPath(
                "tests/data/jpeg/orientation-" + orientation + ".jpg"));
    }

    // The content stream of a page the JPEG is drawn on, then the PDF.
    private static string[] Drawn(byte[] jpeg, out Image image) {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        Page page = new Page(pdf, Letter.PORTRAIT);
        image = new Image(pdf, new MemoryStream(jpeg));
        image.SetLocation(10f, 20f);
        image.DrawOn(page);
        string content = TestSupport.Content(page);
        pdf.Complete();
        return new string[] {content, TestSupport.Latin1(output.ToArray())};
    }

    [Theory]
    [InlineData("1", 32, 16, "")]
    [InlineData("3", 32, 16, "-1 0 0 -1 1 1 cm\n")]
    [InlineData("6", 16, 32, "0 -1 1 0 0 1 cm\n")]
    [InlineData("8", 16, 32, "0 1 -1 0 1 0 cm\n")]
    public void TheSizeAndTheMatrixAreAsSeen(string orientation, int width, int height, string matrix) {
        string[] drawn = Drawn(OrientationJPEG(orientation), out Image image);
        Assert.Equal(32f, image.GetWidth());
        Assert.Equal(16f, image.GetHeight());
        // The image object keeps the pixels as stored
        Assert.Contains("/Width " + width + "\n", drawn[1]);
        Assert.Contains("/Height " + height + "\n", drawn[1]);
        Assert.Contains("32 0 0 16 10 756 cm\n" + matrix + "/Im", drawn[0]);
    }

    // Orientation 1, upright as stored, draws the page as a JPEG without Exif.
    [Fact]
    public void OrientationOneIsDrawnAsWithoutExif() {
        byte[] data = OrientationJPEG("1");
        string withExif = Drawn(data, out _)[0];
        string withoutExif = Drawn(WithoutSegment(data, 0xE1), out _)[0];
        Assert.Equal(withoutExif, withExif);
    }

    // Returns the JPEG without its first segment of the marker.
    private static byte[] WithoutSegment(byte[] jpeg, int marker) {
        for (int i = 2; i + 3 < jpeg.Length; i++) {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == marker) {
                int end = i + 2 + ((jpeg[i + 2] << 8) | jpeg[i + 3]);
                byte[] stripped = new byte[jpeg.Length - (end - i)];
                Array.Copy(jpeg, 0, stripped, 0, i);
                Array.Copy(jpeg, end, stripped, i, jpeg.Length - end);
                return stripped;
            }
        }
        return jpeg;
    }

    // An Exif segment, without its marker and length, with one entry in IFD0,
    // in either byte order.
    private static byte[] Exif(bool bigEndian, int tag, int kind, int count, int value) {
        MemoryStream segment = new MemoryStream();
        segment.Write(new byte[] {(byte) 'E', (byte) 'x', (byte) 'i', (byte) 'f', 0, 0});
        segment.WriteByte(bigEndian ? (byte) 'M' : (byte) 'I');
        segment.WriteByte(bigEndian ? (byte) 'M' : (byte) 'I');
        U16(segment, bigEndian, 42);
        U32(segment, bigEndian, 8);
        U16(segment, bigEndian, 1);
        U16(segment, bigEndian, tag);
        U16(segment, bigEndian, kind);
        U32(segment, bigEndian, count);
        U16(segment, bigEndian, value);
        U16(segment, bigEndian, 0);
        U32(segment, bigEndian, 0);
        return segment.ToArray();
    }

    private static void U16(MemoryStream output, bool bigEndian, int v) {
        if (bigEndian) {
            output.WriteByte((byte) (v >> 8));
            output.WriteByte((byte) v);
        } else {
            output.WriteByte((byte) v);
            output.WriteByte((byte) (v >> 8));
        }
    }

    private static void U32(MemoryStream output, bool bigEndian, int v) {
        if (bigEndian) {
            U16(output, true, (int) ((uint) v >> 16));
            U16(output, true, v & 0xFFFF);
        } else {
            U16(output, false, v & 0xFFFF);
            U16(output, false, (int) ((uint) v >> 16));
        }
    }

    [Fact]
    public void TheOrientationIsReadInBothByteOrders() {
        foreach (bool bigEndian in new bool[] {false, true}) {
            for (int value = 1; value <= 8; value++) {
                Assert.Equal(value, JPGImage.ExifOrientation(Exif(bigEndian, 0x0112, 3, 1, value)));
            }
        }
    }

    // A malformed Exif segment is passed over, never read out of its bounds.
    [Fact]
    public void TheOrientationOfAMalformedExifIsPassedOver() {
        byte[] notExif = Exif(false, 0x0112, 3, 1, 6);
        notExif[3] = (byte) 'v';
        byte[] otherOrder = Exif(false, 0x0112, 3, 1, 6);
        otherOrder[7] = (byte) 'M';
        byte[] before8 = Exif(false, 0x0112, 3, 1, 6);
        before8[10] = 4;
        byte[] pastTheEnd = Exif(false, 0x0112, 3, 1, 6);
        pastTheEnd[10] = 0xFF;
        pastTheEnd[11] = 0xFF;
        pastTheEnd[12] = 0xFF;
        pastTheEnd[13] = 0xFF;
        byte[][] segments = {
            Exif(false, 0x0112, 3, 1, 0),
            Exif(true, 0x0112, 3, 1, 9),
            Exif(false, 0x0112, 4, 1, 6),
            Exif(true, 0x0112, 3, 2, 6),
            Exif(false, 0x0110, 3, 1, 6),
            notExif, otherOrder, before8, pastTheEnd,
        };
        foreach (byte[] segment in segments) {
            Assert.Equal(0, JPGImage.ExifOrientation(segment));
        }
        byte[] whole = Exif(true, 0x0112, 3, 1, 6);
        for (int end = 0; end < whole.Length - 4; end++) {
            Assert.Equal(0, JPGImage.ExifOrientation(whole[..end]));
        }
        // The JPEG with a malformed Exif is drawn as stored
        byte[] broken = OrientationJPEG("6");
        for (int i = 0; i + 3 < broken.Length; i++) {
            if (broken[i] == 'M' && broken[i + 1] == 'M' && broken[i + 2] == 0 && broken[i + 3] == 0x2A) {
                broken[i + 3] = 0x2B;
                break;
            }
        }
        Drawn(broken, out Image image);
        Assert.Equal(16f, image.GetWidth());
        Assert.Equal(32f, image.GetHeight());
    }
}
}
