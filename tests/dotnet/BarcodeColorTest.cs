/*
 * BarcodeColorTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
/// <summary>The color of the barcodes, SetModuleColor, black unless set.</summary>
public class BarcodeColorTest {
    private const int NAVY = 0x1F3A5F;
    private const string NAVY_STROKE = "0.12 0.23 0.37 RG";
    private const string NAVY_FILL = "0.12 0.23 0.37 rg";

    [Fact]
    public void ALinearBarcodeDrawsItsBarsAndItsTextInItsColor() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Barcode barcode = new Barcode(Barcode.CODE_128, "A1").SetFont(TestSupport.Helvetica(pdf));
        barcode.SetLocation(50f, 50f);
        barcode.SetModuleColor(NAVY).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.Contains(NAVY_STROKE), content);
        Assert.Equal(NAVY_FILL, TestSupport.FillColorBefore(content, "A1"));
    }

    [Fact]
    public void ALinearBarcodeIsBlackUnlessSet() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.SetPenColor(Color.red);
        Barcode barcode = new Barcode(Barcode.CODE_128, "A1").SetFont(TestSupport.Helvetica(pdf));
        barcode.SetLocation(50f, 50f);
        barcode.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.Contains("0 0 0 RG"), content);
    }

    [Fact]
    public void APDF417DrawsInItsColor() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new PDF417("A1").SetLocation(50f, 50f).SetModuleColor(NAVY).DrawOn(page);
        Assert.True(TestSupport.Content(page).Contains(NAVY_STROKE), TestSupport.Content(page));
    }

    [Fact]
    public void TheTwoDimensionalCodesDrawInTheirColor() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        QRCode qr = new QRCode("A1", ErrorCorrectionLevel.M).SetModuleColor(NAVY);
        qr.SetLocation(50f, 50f);
        qr.DrawOn(page);
        Assert.True(TestSupport.Content(page).Contains(NAVY_FILL), TestSupport.Content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        DataMatrix dm = new DataMatrix("A1").SetModuleColor(NAVY);
        dm.SetLocation(50f, 50f);
        dm.DrawOn(page);
        Assert.True(TestSupport.Content(page).Contains(NAVY_FILL), TestSupport.Content(page));
    }
}
}
