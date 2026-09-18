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
}
}
