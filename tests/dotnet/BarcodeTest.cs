/*
 * BarcodeTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
public class BarcodeTest {
    // type, text, direction, with font, corner x, corner y
    private static readonly object[][] CORNERS = {
        new object[] {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, false, 171.25f, 145.5f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, true, 171.25f, 151.31f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, false, 145.5f, 171.25f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, false, 145.5f, 171.25f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, true, 145.5f, 171.25f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, false, 171.25f, 145.5f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, true, 179.59f, 151.31f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, false, 145.5f, 171.25f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, false, 145.5f, 171.25f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, true, 145.5f, 179.59f},
        new object[] {Barcode.CODE_128, "Hello", Direction.LEFT_TO_RIGHT, false, 167.5f, 137.5f},
        new object[] {Barcode.CODE_128, "Hello", Direction.LEFT_TO_RIGHT, true, 167.5f, 154.072f},
        new object[] {Barcode.CODE_128, "Hello", Direction.BOTTOM_TO_TOP, false, 137.5f, 167.5f},
        new object[] {Barcode.CODE_128, "Hello", Direction.BOTTOM_TO_TOP, true, 154.072f, 167.5f},
        new object[] {Barcode.CODE_128, "Hello", Direction.TOP_TO_BOTTOM, false, 137.5f, 167.5f},
        new object[] {Barcode.CODE_128, "Hello", Direction.TOP_TO_BOTTOM, true, 137.5f, 167.5f},
        new object[] {Barcode.CODE_39, "HELLO-39", Direction.LEFT_TO_RIGHT, false, 219.25f, 137.5f},
        new object[] {Barcode.CODE_39, "HELLO-39", Direction.LEFT_TO_RIGHT, true, 219.25f, 154.072f},
        new object[] {Barcode.CODE_39, "HELLO-39", Direction.BOTTOM_TO_TOP, false, 137.5f, 219.25f},
        new object[] {Barcode.CODE_39, "HELLO-39", Direction.BOTTOM_TO_TOP, true, 154.072f, 219.25f},
        new object[] {Barcode.CODE_39, "HELLO-39", Direction.TOP_TO_BOTTOM, false, 137.5f, 219.25f},
        new object[] {Barcode.CODE_39, "HELLO-39", Direction.TOP_TO_BOTTOM, true, 137.5f, 219.25f},
    };

    [Fact]
    public void DrawOnReturnsTheCornerOfTheBarsAndTheTextInEveryDirection() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        foreach (object[] row in CORNERS) {
            Barcode barcode = new Barcode((int) row[0], (string) row[1])
                    .SetLocation(100f, 100f).SetDirection((Direction) row[2]);
            if ((bool) row[3]) {
                barcode.SetFont(font);
            }
            string name = row[0] + " " + row[2] + " font " + row[3];
            float[] first = barcode.DrawOn(page);
            TestSupport.AssertNear((float) row[4], first[0], TestSupport.DELTA, name + " x");
            TestSupport.AssertNear((float) row[5], first[1], TestSupport.DELTA, name + " y");
            float[] second = barcode.DrawOn(page);
            TestSupport.AssertNear(first[0], second[0], 0f, name + " drawn again x");
            TestSupport.AssertNear(first[1], second[1], 0f, name + " drawn again y");
            TestSupport.AssertNear((bool) row[3] ? 51.372f : 37.5f, barcode.GetHeight(), TestSupport.DELTA, name + " height");
        }
    }

    [Fact]
    public void Code39RejectsCharactersItCannotEncode() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Exception e = Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.CODE_39, "hello").DrawOn(page));
        Assert.Equal("The input string '*hello*' contains characters that are invalid in a Code39 barcode.", e.Message);
    }

    [Fact]
    public void UpcAndEanNeedTheirNumberOfDigits() {
        Exception upc = Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.UPC_A, "123"));
        Assert.Equal("UPC-A barcodes must have exactly 11 digits!", upc.Message);
        Exception ean = Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.EAN_13, "0123456789012"));
        Assert.Equal("EAN-13 barcodes must have exactly 12 digits!", ean.Message);
    }
}
}
