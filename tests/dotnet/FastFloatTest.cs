/*
 * FastFloatTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
/// <summary>The numbers written in content streams, which must be the same bytes in the four ports.</summary>
public class FastFloatTest {
    private static string Format(float value) {
        return TestSupport.Latin1(FastFloat.ToByteArray(value));
    }

    [Fact]
    public void WritesWholeNumbersWithoutDecimals() {
        Assert.Equal("0", Format(0f));
        Assert.Equal("1", Format(1f));
        Assert.Equal("-3", Format(-3f));
        Assert.Equal("100", Format(100f));
    }

    [Fact]
    public void LeavesOutTrailingZeros() {
        Assert.Equal("0.5", Format(0.5f));
        Assert.Equal("1.25", Format(1.25f));
        Assert.Equal("0.1", Format(0.1f));
        Assert.Equal("1.1", Format(1.1f));
    }

    [Fact]
    public void RoundsHundredthsHalfAwayFromZero() {
        Assert.Equal("1.13", Format(1.125f));
        Assert.Equal("-1.13", Format(-1.125f));
        Assert.Equal("2.38", Format(2.375f));
        Assert.Equal("0.13", Format(0.125f));
        Assert.Equal("-0.13", Format(-0.125f));
        Assert.Equal("0.01", Format(0.006f));
        Assert.Equal("100", Format(99.995f));
    }

    [Fact]
    public void WritesZeroForValuesThatRoundToZero() {
        Assert.Equal("0", Format(-0f));
        Assert.Equal("0", Format(-0.001f));
        Assert.Equal("0", Format(0.004f));
    }

    [Fact]
    public void WritesLargeNumbersWithAllTheirDigits() {
        Assert.Equal("8388607.5", Format(8388607.5f));
        Assert.Equal("8388608", Format(8388608f));
        Assert.Equal("21600000", Format(21600000f));
        Assert.Equal("1000000000", Format(1e9f));
        Assert.Equal("-1000000000", Format(-1e9f));
        Assert.Equal("2147483520", Format(2147483520f));
    }

    [Fact]
    public void RefusesNumbersAPdfCannotHold() {
        foreach (float value in new float[] {float.NaN, float.PositiveInfinity, float.NegativeInfinity, 2147483648f, -3.4e38f}) {
            Assert.Throws<System.ArgumentException>(() => FastFloat.ToByteArray(value));
        }
    }
}
}
