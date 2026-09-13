/*
 * BidiTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
/// <summary>Right to left text in visual order: the Unicode Bidirectional Algorithm and Arabic shaping.</summary>
public class BidiTest {
    [Fact]
    public void LeftToRightTextIsUnchanged() {
        Assert.Equal("abc", Bidi.ReorderVisually("abc"));
    }

    [Fact]
    public void HebrewIsReversed() {
        Assert.Equal("םולש", Bidi.ReorderVisually("שלום"));
    }

    [Fact]
    public void DigitsKeepTheirOrder() {
        Assert.Equal("123 םולש", Bidi.ReorderVisually("שלום 123"));
    }

    [Fact]
    public void ALineWithRightToLeftTextIsLaidOutRightToLeft() {
        // The README: a line that has right to left text is a right to left line.
        Assert.Equal("םולש abc", Bidi.ReorderVisually("abc שלום"));
    }

    [Fact]
    public void BracketsAroundLeftToRightTextAreKeptWithItBetweenMarks() {
        Assert.Equal("‎(abc)‎ םולש",
                Bidi.ReorderVisually("שלום (abc)"));
        Assert.Equal("‏(ש‏)", Bidi.ReorderVisually("(ש)"));
    }

    [Fact]
    public void ArabicIsShapedWithLigaturesAndReversed() {
        // seen initial, lam alef final ligature, meem isolated
        Assert.Equal("ﻡﻼﺳ", Bidi.ReorderVisually("سلام"));
    }

    [Fact]
    public void ZeroWidthNonJoinerIsKept() {
        string shaped = Bidi.ReorderVisually("می‌خواهم");
        Assert.Equal("ﻢﻫﺍﻮﺧ‌ﯽﻣ", shaped);
    }

    [Fact]
    public void ARangeIsReorderedInTheContextOfTheWholeString() {
        Assert.Equal("ול", Bidi.ReorderVisually("שלום", 1, 3));
    }

    [Fact]
    public void IsArabicTellsArabicLettersApart() {
        Assert.True(Bidi.IsArabic(0x0633));
        Assert.False(Bidi.IsArabic('a'));
        Assert.False(Bidi.IsArabic(0x05E9));
    }
}
}
