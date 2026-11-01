/*
 * UnicodeCategoriesTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The table of categories of the .NET Framework 4.8 build,
/// net48/Compat/UnicodeCategories.cs, whose own data is of Unicode 8.0 (third
/// review, K3): the build is not tested, so its table is, here.
/// </summary>
public class UnicodeCategoriesTest {
    // Every code point has the category .NET 8 gives it: checked on .NET 8
    // alone, as a later .NET has the data of a later Unicode, which assigns
    // new code points and changes some categories (U+0897 and U+1171E in
    // .NET 10, of Unicode 16).
    [Fact]
    public void EveryCodePointHasTheCategoryOfNet8() {
#if !NET9_0_OR_GREATER
        for (int c = 0; c <= 0x10FFFF; c++) {
            UnicodeCategory want = CharUnicodeInfo.GetUnicodeCategory(c);
            if (Compat.UnicodeCategories.Of(c) != want) {
                Assert.Fail("U+" + c.ToString("X4", CultureInfo.InvariantCulture) + " is " + Compat.UnicodeCategories.Of(c) + ", not " + want);
            }
        }
#endif
        Assert.Equal(UnicodeCategory.OtherNotAssigned, Compat.UnicodeCategories.Of(-1));
        Assert.Equal(UnicodeCategory.OtherNotAssigned, Compat.UnicodeCategories.Of(0x110000));
    }

    // The source asks CharUnicodeInfo, which the .NET Framework 4.8 build
    // answers with the table, and never char itself, which it does not.
    [Fact]
    public void TheLibraryAsksTheTableForCategories() {
        foreach (string file in System.IO.Directory.GetFiles(TestSupport.RepoPath("net/pdfjet"), "*.cs")) {
            string source = System.IO.File.ReadAllText(file);
            Assert.False(source.Contains("char.GetUnicodeCategory(") || source.Contains("char.IsDigit("), file);
        }
    }

    // The white space of a value of SVG is that of Unicode, as in the other ports.
    [Fact]
    public void TheWhiteSpaceOfSVGIsThatOfUnicode() {
        Assert.Equal("x", SVGState.Trim("\u00A0\u2003x\u3000\u0085\u000B"));
        Assert.False(SVGState.IsSpace('\u180E'));
        Assert.False(SVGState.IsSpace('x'));
    }

    [Fact]
    public void AShortColorIsAnArgumentError() {
        Assert.Throws<ArgumentException>(() => Util.ToPackedRGB(new float[] {0.5f}));
    }

    // The Arabic mark U+08D4, of Unicode 9, is a mark, which the data of
    // .NET Framework 4.8 does not know, and laid it out as a spacing glyph.
    [Fact]
    public void AMarkNewerThanUnicode8IsAMark() {
        Assert.Equal(UnicodeCategory.NonSpacingMark, Compat.UnicodeCategories.Of(0x08D4));
    }
}
}
