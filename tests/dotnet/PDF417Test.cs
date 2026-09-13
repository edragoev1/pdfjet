/*
 * PDF417Test.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class PDF417Test {
    [Fact]
    public void DrawingTwiceDoesNotMoveTheSymbol() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        PDF417 symbol = new PDF417("Hello, World!");
        TestSupport.AssertXY(281.25f, 11.25f, symbol.DrawOn(page));
        TestSupport.AssertXY(281.25f, 11.25f, symbol.DrawOn(page));
    }
}
}
