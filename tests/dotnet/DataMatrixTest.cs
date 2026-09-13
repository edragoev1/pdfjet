/*
 * DataMatrixTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class DataMatrixTest {
    [Fact]
    public void SixDigitsFitTheSmallestSquareWithItsFinderPattern() {
        bool[][] modules = new DataMatrix("123456").GetModules();
        Assert.Equal(10, modules.Length);
        Assert.Equal(10, modules[0].Length);
        for (int i = 0; i < 10; i++) {
            Assert.True(modules[0][i] == (i % 2 == 0), "top row alternates");
            Assert.True(modules[9][i], "bottom row is solid");
            Assert.True(modules[i][0], "left column is solid");
        }
    }

    [Fact]
    public void LongerDataGetsALargerSymbol() {
        Assert.Equal(32, new DataMatrix(new string('Z', 60)).GetModules().Length);
    }

    [Fact]
    public void TheRectangleShapeIsWiderThanTall() {
        bool[][] modules = new DataMatrix("Hello, World!", DataMatrix.RECTANGLE).GetModules();
        Assert.Equal(12, modules.Length);
        Assert.Equal(26, modules[0].Length);
    }

    [Fact]
    public void DrawOnReturnsTheCornerOfTheModules() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        DataMatrix dm = new DataMatrix("123456").SetLocation(5f, 5f).SetModuleLength(3f);
        TestSupport.AssertXY(35f, 35f, dm.DrawOn(page));
    }
}
}
