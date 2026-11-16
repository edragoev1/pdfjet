/*
 * QRCodeTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
public class QRCodeTest {
    private static bool Dark(bool?[][] modules, int row, int column) {
        return modules[row][column] == true;
    }

    [Fact]
    public void ShortDataKeepsTheSymbolAt33ModulesAtEveryLevel() {
        foreach (ErrorCorrectionLevel level in Enum.GetValues(typeof(ErrorCorrectionLevel))) {
            bool?[][] modules = new QRCode("Hello", level).GetModules();
            Assert.True(modules.Length == 33, level + " rows");
            Assert.True(modules[0].Length == 33, level + " columns");
        }
    }

    [Fact]
    public void LongerDataMakesALargerSymbol() {
        Assert.Equal(33, new QRCode(new string('a', 78), ErrorCorrectionLevel.L).GetModules().Length);     // version 4
        Assert.Equal(37, new QRCode(new string('a', 79), ErrorCorrectionLevel.L).GetModules().Length);     // version 5
        Assert.Equal(177, new QRCode(new string('a', 2953), ErrorCorrectionLevel.L).GetModules().Length);  // version 40
        Assert.Equal(177, new QRCode(new string('a', 1273), ErrorCorrectionLevel.H).GetModules().Length);  // version 40
    }

    [Fact]
    public void VersionsFrom7CarryTheirVersionNumber() {
        // 120 bytes at level M need version 7, 45 modules, whose version information is 0x07C94.
        bool?[][] modules = new QRCode(new string('a', 120), ErrorCorrectionLevel.M).GetModules();
        Assert.Equal(45, modules.Length);
        for (int i = 0; i < 18; i++) {
            bool bit = ((0x07C94 >> i) & 1) == 1;
            Assert.True(bit == Dark(modules, i / 3, i % 3 + 45 - 11), "top right, bit " + i);
            Assert.True(bit == Dark(modules, i % 3 + 45 - 11, i / 3), "bottom left, bit " + i);
        }
    }

    [Fact]
    public void FinderPatternsAreInThreeCorners() {
        bool?[][] modules = new QRCode("Hello", ErrorCorrectionLevel.L).GetModules();
        for (int i = 0; i < 7; i++) {
            Assert.True(Dark(modules, 0, i), "top left, top row");
            Assert.True(Dark(modules, 0, 32 - i), "top right, top row");
            Assert.True(Dark(modules, 32, i), "bottom left, bottom row");
            Assert.True(Dark(modules, i, 0), "top left, left column");
        }
        Assert.False(Dark(modules, 0, 7), "separator");
        Assert.False(Dark(modules, 7, 0), "separator");
    }

    [Fact]
    public void DataThatDoesNotFitVersion40Throws() {
        Assert.Throws<ArgumentException>(() => new QRCode(new string('a', 2954), ErrorCorrectionLevel.L));
        Assert.Throws<ArgumentException>(() => new QRCode(new string('a', 1274), ErrorCorrectionLevel.H));
    }

    [Fact]
    public void DrawOnReturnsTheSameCornerEveryTime() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        QRCode qr = new QRCode("Hello", ErrorCorrectionLevel.L).SetLocation(10f, 10f).SetModuleLength(2f);
        TestSupport.AssertXY(76f, 76f, qr.DrawOn(page));
        TestSupport.AssertXY(76f, 76f, qr.DrawOn(page));
    }
    [Fact]
    public void ADescribedQRCodeIsAFigure() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        QRCode qr = new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M);
        qr.SetAltDescription("https://pdfjet.com");
        qr.SetLocation(50f, 50f);
        qr.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\n", content);
        Assert.DoesNotContain("/Artifact", content);
        // Not described, it is decoration, as before.
        Page plain = new Page(pdf, Letter.PORTRAIT);
        new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M).DrawOn(plain);
        Assert.StartsWith("/Artifact BMC\n", TestSupport.Content(plain));
    }

    // Fits says what the constructor would do, at the edge: the longest text
    // that fits, and one byte more, with and without the ECI
    [Fact]
    public void FitsTellsAtTheEdge() {
        Assert.True(QRCode.Fits(new string('a', 2331), ErrorCorrectionLevel.M));
        Assert.False(QRCode.Fits(new string('a', 2332), ErrorCorrectionLevel.M));
        Assert.True(QRCode.Fits(new string('\u20AC', 776), ErrorCorrectionLevel.M));    // 2,328 bytes and the ECI
        Assert.False(QRCode.Fits(new string('\u20AC', 777), ErrorCorrectionLevel.M));   // 2,331 bytes and the ECI
        Assert.True(QRCode.Fits(new string('a', 2953), ErrorCorrectionLevel.L));
        Assert.False(QRCode.Fits(new string('a', 1274), ErrorCorrectionLevel.H));
        Assert.True(QRCode.Fits("", ErrorCorrectionLevel.H));
        // A level that is not one fits nothing, as in Go, where the
        // constructor throws (review of 16 November 2026)
        Assert.False(QRCode.Fits("x", (ErrorCorrectionLevel) 7));
        Assert.False(QRCode.Fits("x", (ErrorCorrectionLevel) 9));
        Assert.False(QRCode.Fits("", (ErrorCorrectionLevel) (-1)));
        foreach (ErrorCorrectionLevel level in new[] {ErrorCorrectionLevel.L, ErrorCorrectionLevel.M,
                ErrorCorrectionLevel.Q, ErrorCorrectionLevel.H}) {
            foreach (int n in new[] {1270, 1273, 1274, 1663, 1664, 2330, 2331, 2332, 2953, 2954}) {
                string text = new string('a', n);
                bool made = true;
                try {
                    new QRCode(text, level);
                } catch (ArgumentException) {
                    made = false;
                }
                Assert.True(made == QRCode.Fits(text, level), level + " " + n);
            }
        }
    }

}
}
