/*
 * UnicodeCategoriesTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Globalization;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The table of categories of the .NET Framework 4.8 build,
/// net48/Compat/UnicodeCategories.cs, whose own data is of Unicode 8.0 (third
/// review, K3): the build is not tested, so its table is, here.
/// </summary>
public class UnicodeCategoriesTest {
    // Every code point has the category .NET 8 gives it.
    [Fact]
    public void EveryCodePointHasTheCategoryOfNet8() {
        for (int c = 0; c <= 0x10FFFF; c++) {
            UnicodeCategory want = CharUnicodeInfo.GetUnicodeCategory(c);
            if (Compat.UnicodeCategories.Of(c) != want) {
                Assert.Fail("U+" + c.ToString("X4", CultureInfo.InvariantCulture) + " is " + Compat.UnicodeCategories.Of(c) + ", not " + want);
            }
        }
        Assert.Equal(UnicodeCategory.OtherNotAssigned, Compat.UnicodeCategories.Of(-1));
        Assert.Equal(UnicodeCategory.OtherNotAssigned, Compat.UnicodeCategories.Of(0x110000));
    }

    // The Arabic mark U+08D4, of Unicode 9, is a mark, which the data of
    // .NET Framework 4.8 does not know, and laid it out as a spacing glyph.
    [Fact]
    public void AMarkNewerThanUnicode8IsAMark() {
        Assert.Equal(UnicodeCategory.NonSpacingMark, Compat.UnicodeCategories.Of(0x08D4));
    }
}
}
