/*
 * FontTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using Xunit;

namespace PDFjet.NET {
public class FontTest {
    [Fact]
    public void CoreFontWidthsComeFromTheAfmMetrics() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Assert.Equal("Helvetica", font.GetName());
        Assert.Equal(12f, font.GetSize());
        // H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
        TestSupport.AssertNear(27.336f, font.StringWidth(12f, "Hello"), 0.001f);
        TestSupport.AssertNear(27.336f, font.StringWidth("Hello"), 0.001f);
        font.SetSize(24f);
        TestSupport.AssertNear(54.672f, font.StringWidth("Hello"), 0.001f);
    }

    [Fact]
    public void TheWidthOfNoTextIsZero() {
        Assert.Equal(0f, TestSupport.Helvetica(TestSupport.NewPDF()).StringWidth(null));
    }

    [Fact]
    public void KerningPairsNarrowTheText() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TestSupport.AssertNear(16.008f, font.StringWidth(12f, "AV"), 0.001f);
        font.SetKernPairs(true);
        // KPX A V -70
        TestSupport.AssertNear(15.168f, font.StringWidth(12f, "AV"), 0.001f);
    }

    [Fact]
    public void CoreFontVerticalMetrics() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TestSupport.AssertNear(11.172f, font.GetAscent(12f), 0.001f);
        TestSupport.AssertNear(2.7f, font.GetDescent(12f), 0.001f);
        TestSupport.AssertNear(13.872f, font.GetBodyHeight(12f), 0.001f);
    }

    [Fact]
    public void GetFitCharsCountsTheCharactersThatFit() {
        Assert.Equal(5, TestSupport.Helvetica(TestSupport.NewPDF()).GetFitChars("Hello world", 30f));
    }

    [Fact]
    public void EveryCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce() {
        Font font = new Font(TestSupport.NewPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
        Assert.Equal(20f, font.StringWidth(10f, "日本"));
        Assert.Equal(10f, font.StringWidth(10f, "𠀋"));
    }

    [Fact]
    public void ReadsAStreamFont() {
        string path = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
        if (!File.Exists(TestSupport.RepoPath(path))) {
            return;     // The fonts directory is not here.
        }
        using (Stream stream = TestSupport.Open(path)) {
            Font font = new Font(TestSupport.NewPDF(), stream);
            Assert.Equal("IBMPlexSans", font.GetName());
            TestSupport.AssertNear(28.32f, font.StringWidth(12f, "Hello"), 0.001f);
        }
    }

    [Fact]
    public void ACoreFontNumberOutsideTheFourteenIsRejected() {
        PDF pdf = TestSupport.NewPDF();
        Assert.Throws<ArgumentException>(() => new Font(pdf, 0));
        Assert.Throws<ArgumentException>(() => new Font(pdf, 15));
    }
}
}
