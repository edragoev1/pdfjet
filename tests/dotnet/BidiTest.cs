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
    // Text pasted from a PDF often has presentation forms, and Persian and
    // Urdu text the letters of Arabic Supplement.
    [Fact]
    public void TheOtherLettersOfRightToLeftScriptsAreRightToLeft() {
        Assert.Equal("\uFB2C\uFB2A abc", Bidi.ReorderVisually("abc \uFB2A\uFB2C"));   // Hebrew presentation forms
        Assert.Equal("\uFEE2\uFE8D abc", Bidi.ReorderVisually("abc \uFE8D\uFEE2"));   // Arabic presentation forms B
        Assert.Equal("\uFB51\uFB50 abc", Bidi.ReorderVisually("abc \uFB50\uFB51"));   // Arabic presentation forms A
        Assert.Equal("\u0751\u0750 abc", Bidi.ReorderVisually("abc \u0750\u0751"));   // Arabic Supplement
        Assert.Equal("\u08A1\u08A0 abc", Bidi.ReorderVisually("abc \u08A0\u08A1"));   // Arabic Extended-A
        Assert.Equal("\u07CB\u07CA abc", Bidi.ReorderVisually("abc \u07CA\u07CB"));   // NKo
        Assert.Equal("\U00010801\U00010800 abc", Bidi.ReorderVisually("abc \U00010800\U00010801"));
    }

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
