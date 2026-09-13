/*
 * TextBoxTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class TextBoxTest {
    [Fact]
    public void MeasuringDoesNotFixTheHeight() {
        TextBox box = new TextBox(TestSupport.Helvetica(TestSupport.NewPDF()),
                "one two three four five six seven eight nine ten");
        box.SetLocation(0f, 0f);
        box.SetWidth(60f);
        TestSupport.AssertXY(60f, 83.232f, box.DrawOn(null));
        TestSupport.AssertNear(83.232f, box.GetHeight(), TestSupport.DELTA);

        box.SetText("one two three four five six seven eight nine ten eleven twelve thirteen fourteen");
        TestSupport.AssertXY(60f, 124.848f, box.DrawOn(null));
        TestSupport.AssertNear(124.848f, box.GetHeight(), TestSupport.DELTA);
    }

    [Fact]
    public void BordersAreOffByDefaultAndCanBeRemovedOneByOne() {
        TextBox box = new TextBox(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        Assert.False(box.GetBorder(Border.TOP));
        box.SetBorders(true);
        box.SetBorder(Border.TOP, false);
        Assert.False(box.GetBorder(Border.TOP));
        Assert.True(box.GetBorder(Border.LEFT));
        Assert.True(box.GetBorder(Border.RIGHT));
        Assert.True(box.GetBorder(Border.BOTTOM));
    }

    [Fact]
    public void ColorGettersReturnCopies() {
        TextBox box = new TextBox(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        box.SetTextColor(0x0000FF);
        box.GetTextColor()[2] = 0f;
        TestSupport.AssertRGB(0f, 0f, 1f, box.GetTextColor());
        box.SetBorderColor(0xFF0000);
        TestSupport.AssertRGB(1f, 0f, 0f, box.GetBorderColor());
    }
}
}
