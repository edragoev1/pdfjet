/*
 * SASLprepTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
/// <summary>SASLprep, RFC 4013, of the password of AES-256.</summary>
public class SASLprepTest {
    // The examples of RFC 4013, section 3.
    [Fact]
    public void PrepareTheExamplesOfRFC4013() {
        var cases = new (string input, string want)[] {
            ("I­X", "IX"),
            ("user", "user"),
            ("USER", "USER"),
            ("ª", "a"),
            ("Ⅸ", "IX"),
        };
        foreach (var c in cases) {
            Assert.Equal((c.input, c.want), (c.input, SASLprep.Prepare(c.input)));
        }
        foreach (string input in new[] {"\u0007", "ا1"}) {
            Assert.Throws<ArgumentException>(() => SASLprep.Prepare(input));
        }
        // Right to left text that starts and ends with a right to left character
        SASLprep.Prepare("ا1ب");
    }

    // The password as an accented letter is typed on one keyboard and
    // another, a full-width letter, a no-break space and a Hangul syllable
    // written as its letters come out the same.
    [Fact]
    public void PrepareNormalizes() {
        var cases = new (string input, string want)[] {
            ("café", "café"),
            ("café", "café"),
            ("ＡＢ", "AB"),
            ("a b", "a b"),
            ("각", "각"),
            ("각", "각"),
            ("ﬁ", "fi"),
            ("x²", "x2"),
            // Marks put in the order of their classes: dot below (220) before
            // the acute accent (230), the q with no composite of either
            ("q̣́", "q̣́"),
            // A blocked mark: the ring above stays apart from the A that the
            // acute accent of the same class, before it, did not compose with
            ("Á̊", "Á̊"),
        };
        foreach (var c in cases) {
            Assert.Equal((c.input, c.want), (c.input, SASLprep.Prepare(c.input)));
        }
        // A private use character is refused
        Assert.Throws<ArgumentException>(() => SASLprep.Prepare("pass"));
    }

    [Fact]
    public void TruncateCutsBeforeACharacterThatDoesNotFit() {
        string password = new string('a', 126) + "é";
        Assert.Equal(126, SASLprep.Truncate(password).Length);
        Assert.Equal(127, SASLprep.Truncate(new string('a', 200)).Length);
    }
}
}   // End of namespace PDFjet.NET
