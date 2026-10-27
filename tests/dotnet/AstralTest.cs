/*
 * AstralTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The characters past U+FFFF, as the Chinese characters of Hong Kong, 𨋢 the
/// lift among them, which IBM Plex Sans TC maps in its format 12 subtable.
/// </summary>
public class AstralTest {
    [Fact]
    public void TheGlyphMapGoesPastTheBasicMultilingualPlane() {
        GlyphMap m = new GlyphMap();
        m.Set('A', 3);
        m.Set(0x282E2, 7);
        m.SetIfAbsent(0x282E2, 9);  // Kept
        m.SetIfAbsent(0x20021, 5);
        m.Set(0x110000, 4);         // Not a character
        Assert.Equal(3, m.Get('A'));
        Assert.Equal(7, m.Get(0x282E2));
        Assert.Equal(5, m.Get(0x20021));
        Assert.Equal(0, m.Get(0x110000));
        Assert.Equal(0, m.Get(0x20000));
        List<int> order = new List<int>();
        m.Each((c, gid) => order.Add(c));
        Assert.Equal(new List<int> { 'A', 0x20021, 0x282E2 }, order);
        // A font of a million characters past U+FFFF is read no further than
        // MAX_ASTRAL
        GlyphMap big = new GlyphMap();
        for (int c = 0x10000; c < 0x10000 + 2 * GlyphMap.MAX_ASTRAL; c++) {
            big.SetIfAbsent(c, 1);
        }
        Assert.Equal(GlyphMap.MAX_ASTRAL, big.astral.Count);
    }

    [Fact]
    public void ACharacterPastU0xFFFFIsDrawnInTheFallbackFont() {
        PDF pdf = TestSupport.NewPDF();
        Font plex = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Font tc = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.ttf"));
        Assert.True(tc.HasGlyph(0x282E2), "TC has 𨋢");
        Assert.False(plex.HasGlyph(0x282E2), "Plex Sans has 𨋢");
        int gid = tc.unicodeToGID.Get(0x282E2);
        Assert.True(gid != 0 && gid != tc.unicodeToGID.Get('乘'), "𨋢 is glyph " + gid);
        // Each a Chinese character of TC, one em wide, none the box of Plex Sans
        Assert.Equal(3000f, plex.StringWidth(tc, 1000f, "乘𨋢到"));
        // Copied out of the PDF as 𨋢: in UTF-16, the two halves of a surrogate
        // pair
        Assert.Contains("<d860dee2>", FontWriter.ToUnicodeCMap(tc, null));
        Assert.Equal("d860dee2", FontWriter.UnicodeHex(0x282E2));
        Assert.Equal("0041", FontWriter.UnicodeHex('A'));
    }
}
}   // End of namespace PDFjet.NET
