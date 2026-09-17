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
}
}
