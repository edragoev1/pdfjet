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

    // The stroke and fill colors the content sets, as their operators, for
    // example "1 1 1 rg", in their order.
    private static System.Collections.Generic.List<string> Colors(string content) {
        var colors = new System.Collections.Generic.List<string>();
        foreach (string line in content.Split('\n')) {
            if (line.EndsWith(" RG") || line.EndsWith(" rg")) {
                colors.Add(line);
            }
        }
        return colors;
    }

    // Draws each linear barcode but Code 128 in each direction, with its text,
    // on a page of its own, in the module color when it is not null, and
    // returns the names and the contents of the pages.
    private static System.Collections.Generic.List<string[]> DrawLinearBarcodes(int? moduleColor) {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        object[][] cases = {
            new object[] {"UPC-A", Barcode.UPC_A, "01234567890"},
            new object[] {"EAN-13", Barcode.EAN_13, "012345678901"},
            new object[] {"Code 39", Barcode.CODE_39, "HELLO-39"},
            new object[] {"ITF-14", Barcode.ITF_14, "1540014128876"},
        };
        var drawn = new System.Collections.Generic.List<string[]>();
        foreach (object[] c in cases) {
            foreach (Direction direction in Enum.GetValues<Direction>()) {
                PDF pdf = TestSupport.NewPDF();
                Page page = new Page(pdf, Letter.PORTRAIT);
                Barcode barcode = new Barcode((int) c[1], (string) c[2]).SetFont(TestSupport.Helvetica(pdf));
                barcode.SetDirection(direction);
                barcode.SetLocation(50f, 50f);
                if (moduleColor != null) {
                    barcode.SetModuleColor(moduleColor.Value);
                }
                barcode.DrawOn(page);
                drawn.Add(new string[] {c[0] + ", " + direction, TestSupport.Content(page)});
            }
        }
        return drawn;
    }

    // UPC-A, EAN-13, Code 39 and ITF-14 draw their bars and their text in
    // their color in every direction, as Code 128 does: only Code 128, from
    // left to right, was tested (the review of 28 October 2026).
    [Fact]
    public void EveryLinearBarcodeDrawsInItsColorInEveryDirection() {
        foreach (string[] drawn in DrawLinearBarcodes(NAVY)) {
            System.Collections.Generic.List<string> colors = Colors(drawn[1]);
            Assert.True(colors.Contains(NAVY_STROKE) && colors.Contains(NAVY_FILL) && drawn[1].Contains("] TJ"),
                    drawn[0] + ": not drawn in navy with its text: " + string.Join(", ", colors));
            foreach (string op in colors) {
                Assert.True(op == NAVY_STROKE || op == NAVY_FILL, drawn[0] + ": " + op);
            }
        }
    }

    // Unless their color is set, they are black: drawn as they are with black
    // set.
    [Fact]
    public void EveryLinearBarcodeIsBlackUnlessSet() {
        System.Collections.Generic.List<string[]> unset = DrawLinearBarcodes(null);
        foreach (string[] drawn in unset) {
            System.Collections.Generic.List<string> colors = Colors(drawn[1]);
            Assert.True(colors.Contains("0 0 0 RG") && colors.Contains("0 0 0 rg"),
                    drawn[0] + ": not drawn in black: " + string.Join(", ", colors));
            foreach (string op in colors) {
                Assert.True(op == "0 0 0 RG" || op == "0 0 0 rg", drawn[0] + ": " + op);
            }
        }
        System.Collections.Generic.List<string[]> black = DrawLinearBarcodes(Color.black);
        for (int i = 0; i < unset.Count; i++) {
            Assert.True(unset[i][1] == black[i][1], unset[i][0] + ": black set draws another barcode");
        }
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
