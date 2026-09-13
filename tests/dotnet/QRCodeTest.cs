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
    public void TheSymbolIs33ModulesAtEveryLevel() {
        foreach (ErrorCorrectionLevel level in Enum.GetValues(typeof(ErrorCorrectionLevel))) {
            bool?[][] modules = new QRCode("Hello", level).GetModules();
            Assert.True(modules.Length == 33, level + " rows");
            Assert.True(modules[0].Length == 33, level + " columns");
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
    public void DataThatDoesNotFitThrows() {
        Assert.Equal(33, new QRCode(new string('a', 50), ErrorCorrectionLevel.M).GetModules().Length);
        Assert.Throws<ArgumentException>(() => new QRCode(new string('a', 80), ErrorCorrectionLevel.L));
        Assert.Throws<ArgumentException>(() => new QRCode(new string('a', 50), ErrorCorrectionLevel.Q));
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
