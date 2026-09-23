/*
 * DataMatrixTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
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

    [Fact]
    public void GS1TakesTheSymbolOfItsCodewords() {
        // FNC1 and 12 codewords, as the plain text takes 13 with GS: 18 by 18 holds 18
        Assert.Equal(18, DataMatrix.FromGS1("(01)09506000134352(10)ABC").GetModules().Length);
        Assert.Equal(18, new DataMatrix("0109506000134352\u001d10ABC").GetModules().Length);
        bool[][] rect = DataMatrix.FromGS1("(01)09506000134352", DataMatrix.RECTANGLE).GetModules();
        Assert.True(rect.Length < rect[0].Length);
    }

    [Fact]
    public void GS1RefusesDataThatIsNotGS1() {
        const string FORMAT =
                "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!";
        string[,] cases = {
            {"", FORMAT},
            {"01)09506000134352", FORMAT},
            {"(01", FORMAT},
            {"(1)5", "The Application Identifier (1) is not two to four digits!"},
            {"(12345)5", "The Application Identifier (12345) is not two to four digits!"},
            {"(A1)5", "The Application Identifier (A1) is not two to four digits!"},
            {"(10)(21)SN", "The Application Identifier (10) has no data!"},
            {"(10)AB C", "The data of (10) has a character that GS1 does not allow!"},
            {"(10)AB)C", "The data of (10) has a character that GS1 does not allow!"},
            {"(10)Gr\u00fc\u00dfe", "The data of (10) has a character that GS1 does not allow!"},
            {"(91)" + new string('X', 91) + "", "The data of (91) is longer than 90 characters!"},
            {"(01)0950600013435", "The data of (01) must be 14 digits!"},
            {"(17)2612A1", "The data of (17) must be 6 digits!"},
            {"(3103)00075", "The data of (3103) must be 6 digits!"},
            {"(01)09506000134353", "The check digit of (01) is wrong!"},
            {"(00)106141412345678909", "The check digit of (00) is wrong!"},
            {"(414)9506000134353", "The check digit of (414) is wrong!"},
        };
        for (int i = 0; i < cases.GetLength(0); i++) {
            string data = cases[i, 0];
            ArgumentException e = Assert.Throws<ArgumentException>(() => DataMatrix.FromGS1(data));
            Assert.Equal(cases[i, 1], e.Message);
        }
        // Fields of no set length, such as (10) and (21), take any data GS1
        // allows, and (418), a GLN of no check digit here, takes any 13 digits
        DataMatrix.FromGS1("(10)!\"%&'*+,-./:;<=>?_az(21)1(418)1234567890123");
    }
}
}
