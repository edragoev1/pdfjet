/*
 * JPGImageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using Xunit;

namespace PDFjet.NET {

/// <summary>
/// A CMYK JPEG that Adobe software wrote, as images/cmyk.jpg is, has an APP14
/// marker and stores its inks inverted, so the image is written with a Decode
/// array that inverts them back; one without the marker is written as it is.
/// </summary>
public sealed class JPGImageTest {
    private const string CMYK = "images/cmyk.jpg";

    // Returns the JPEG without its APP14 segment.
    private static byte[] WithoutAPP14(byte[] jpeg) {
        for (int i = 2; i + 3 < jpeg.Length; i++) {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == 0xEE) {
                int end = i + 2 + ((jpeg[i + 2] << 8) | jpeg[i + 3]);
                byte[] stripped = new byte[jpeg.Length - (end - i)];
                Array.Copy(jpeg, 0, stripped, 0, i);
                Array.Copy(jpeg, end, stripped, i, jpeg.Length - end);
                return stripped;
            }
        }
        return jpeg;
    }

    private static string PdfWith(byte[] jpeg) {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        new Image(pdf, new MemoryStream(jpeg)).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        return TestSupport.Latin1(output.ToArray());
    }

    [Fact]
    public void TheInksOfACmykJpegAreInvertedBackOnlyWhenAdobeSoftwareWroteIt() {
        if (!File.Exists(TestSupport.RepoPath(CMYK))) {
            return;     // The images directory is not here.
        }
        byte[] jpeg = File.ReadAllBytes(TestSupport.RepoPath(CMYK));
        Assert.True(new JPGImage(new MemoryStream(jpeg)).IsAdobe());
        Assert.Contains("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]", PdfWith(jpeg));

        byte[] plain = WithoutAPP14(jpeg);
        Assert.False(new JPGImage(new MemoryStream(plain)).IsAdobe());
        string raw = PdfWith(plain);
        Assert.Contains("/DeviceCMYK", raw);
        Assert.DoesNotContain("/Decode [", raw);
    }

    // The frame header of the JPEG is what PDFjet reads it for, so a marker
    // before it that is read as one more segment hides it.

    // Returns a JPEG of 8 by 8 pixels: the SOI marker, the bytes given, and a
    // frame header of the number of color components.
    private static byte[] JpegOf(byte[] before, int components) {
        MemoryStream jpeg = new MemoryStream();
        jpeg.Write(new byte[] {0xFF, 0xD8});
        jpeg.Write(before);
        int length = 8 + 3*components;
        jpeg.Write(new byte[] {0xFF, 0xC0, (byte) (length >> 8), (byte) length,
                8, 0, 8, 0, 8, (byte) components});
        for (int i = 0; i < components; i++) {
            jpeg.Write(new byte[] {(byte) (i + 1), 0x11, 0});
        }
        return jpeg.ToArray();
    }

    // Returns an APP14 segment of the bytes.
    private static byte[] App14(params int[] payload) {
        byte[] segment = new byte[4 + payload.Length];
        segment[0] = 0xFF;
        segment[1] = 0xEE;
        segment[2] = (byte) ((payload.Length + 2) >> 8);
        segment[3] = (byte) (payload.Length + 2);
        for (int i = 0; i < payload.Length; i++) {
            segment[4 + i] = (byte) payload[i];
        }
        return segment;
    }

    // The segment Adobe software writes: "Adobe", the version, two flags and
    // the color transform.
    private static readonly byte[] ADOBE_APP14 =
            App14('A', 'd', 'o', 'b', 'e', 0, 100, 0, 0, 0, 0, 2);

    [Fact]
    public void TheMarkersWithoutAParameterSegmentAreNotReadAsSegments() {
        byte[][] markers = {
                new byte[] {0xFF, 0x00, 0x30},  // A stuffed 0xFF byte
                new byte[] {0xFF, 0xD3},        // A restart marker
                new byte[] {0xFF, 0x01},        // A TEM marker
                new byte[] {0xFF, 0xD8},        // A nested SOI marker
                new byte[] {0xFF, 0xFF},        // A fill byte
        };
        foreach (byte[] before in markers) {
            JPGImage image = new JPGImage(new MemoryStream(JpegOf(before, 3)));
            Assert.Equal(8, image.GetWidth());
            Assert.Equal(8, image.GetHeight());
            Assert.Equal(3, image.GetColorComponents());
        }
    }

    [Fact]
    public void AJPEGThatEndsBeforeItsFrameHeaderFails() {
        Assert.Throws<IOException>(() => new JPGImage(
                new MemoryStream(new byte[] {0xFF, 0xD8, 0xFF, 0xD9})));
    }

    [Fact]
    public void AJPEGOfOtherThanEightBitsPerComponentFails() {
        // A PDF image stream of DCTDecode data delivers eight bit samples, and
        // PDFjet writes 8 as the bits per component, so a 12-bit JPEG would be
        // drawn as noise rather than refused.
        foreach (int precision in new int[] {0, 12, 16}) {
            byte[] jpeg = JpegOf(new byte[0], 3);
            jpeg[6] = (byte) precision;     // After the SOI marker, the SOF0 marker and the length
            Assert.Throws<IOException>(() => new JPGImage(new MemoryStream(jpeg)));
        }
        new JPGImage(new MemoryStream(JpegOf(new byte[0], 3)));
    }

    [Fact]
    public void OnlyTheWholeAdobeAPP14SegmentMarksTheImage() {
        Assert.True(new JPGImage(new MemoryStream(JpegOf(ADOBE_APP14, 4))).IsAdobe());

        // Adobe's segment is twelve bytes; a shorter one is another APP14.
        Assert.False(new JPGImage(new MemoryStream(
                JpegOf(App14('A', 'd', 'o', 'b', 'e'), 4))).IsAdobe());
    }

    [Fact]
    public void TheAdobeAPP14SegmentIsFoundAfterTheFrameHeader() {
        // libjpeg reads the markers of a header to the scan, so an APP14 segment
        // between the frame header and the scan marks the image too; one after
        // the scan, which no header reads, does not.
        MemoryStream after = new MemoryStream();
        after.Write(JpegOf(new byte[0], 4));
        after.Write(ADOBE_APP14);
        Assert.True(new JPGImage(new MemoryStream(after.ToArray())).IsAdobe());

        MemoryStream afterTheScan = new MemoryStream();
        afterTheScan.Write(JpegOf(new byte[0], 4));
        afterTheScan.Write(new byte[] {0xFF, 0xDA, 0x00, 0x02});
        afterTheScan.Write(ADOBE_APP14);
        Assert.False(new JPGImage(new MemoryStream(afterTheScan.ToArray())).IsAdobe());
    }

    [Fact]
    public void TheComponentsOfTheFrameHeaderAreNotReadAsMarkers() {
        // The component specifications fill the rest of the frame header and can
        // hold any bytes, the 0xFF of a marker among them; here they are the
        // bytes of an Adobe APP14 segment, which is none.
        byte[] jpeg = JpegOf(new byte[0], 4);
        byte[] components = {0xFF, 0xEE, 0x00, 0x0E,
                (byte) 'A', (byte) 'd', (byte) 'o', (byte) 'b', (byte) 'e', 0x00, 0x64, 0x00};
        Array.Copy(components, 0, jpeg, jpeg.Length - 12, 12);
        MemoryStream whole = new MemoryStream();
        whole.Write(jpeg);
        whole.Write(new byte[] {0, 0, 0, 0, 0xFF, 0xD9});
        Assert.False(new JPGImage(new MemoryStream(whole.ToArray())).IsAdobe());
    }

    [Fact]
    public void AnotherAPP14SegmentDoesNotUnmarkAnAdobeImage() {
        MemoryStream before = new MemoryStream();
        before.Write(ADOBE_APP14);
        before.Write(App14('N', 'o', 't', ' ', 'A', 'd', 'o', 'b', 'e', 0, 0, 0));
        Assert.True(new JPGImage(new MemoryStream(JpegOf(before.ToArray(), 4))).IsAdobe());
    }
}
}
