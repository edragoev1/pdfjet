/*
 * BarcodeTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace PDFjet.NET {
public class BarcodeTest {
    // type, text, direction, with font, corner x, corner y
    private static readonly object[][] CORNERS = {
        new object[] {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, false, 171.25f, 141.25f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, true, 171.25f, 151.31f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, false, 141.25f, 171.25f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, false, 141.25f, 171.25f},
        new object[] {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, true, 141.25f, 171.25f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, false, 171.25f, 141.25f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, true, 179.59f, 151.31f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, false, 141.25f, 171.25f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, false, 141.25f, 171.25f},
        new object[] {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, true, 141.25f, 179.59f},
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
        // The bearer bars of ITF-14 are around the bars and their quiet zones
        new object[] {Barcode.ITF_14, "1540014128876", Direction.LEFT_TO_RIGHT, false, 211.375f, 143.5f},
        new object[] {Barcode.ITF_14, "1540014128876", Direction.LEFT_TO_RIGHT, true, 211.375f, 160.072f},
        new object[] {Barcode.ITF_14, "1540014128876", Direction.BOTTOM_TO_TOP, false, 143.5f, 211.375f},
        new object[] {Barcode.ITF_14, "1540014128876", Direction.BOTTOM_TO_TOP, true, 160.072f, 211.375f},
        new object[] {Barcode.ITF_14, "1540014128876", Direction.TOP_TO_BOTTOM, false, 143.5f, 211.375f},
        new object[] {Barcode.ITF_14, "1540014128876", Direction.TOP_TO_BOTTOM, true, 143.5f, 211.375f},
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
            TestSupport.AssertNear((float) row[5] - 100f, barcode.GetHeight(), TestSupport.DELTA, name + " height");
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

    [Fact]
    public void Code128RefusesATextItCannotHold() {
        string tooLong = "Code 128 barcodes hold at most 48 codewords, and a character below 32 or from 128 to 255 takes two!";
        Assert.Equal(tooLong, Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.CODE_128, new string('A', 49))).Message);
        Assert.Equal(tooLong, Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.CODE_128, new string('\u00e9', 25))).Message);
        Assert.Equal(tooLong, Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.CODE_128, new string('7', 98))).Message);
        Assert.Equal("Code 128 barcodes can only hold characters up to U+00FF!",
                Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.CODE_128, "A\u20ac")).Message);
        // The most a barcode holds is drawn whole: 48 codewords, and the start,
        // the check digit and the stop, of 11 modules each but the stop of 13
        foreach (string text in new string[] {new string('A', 48), new string('\u00e9', 24), new string('7', 96)}) {
            Assert.Equal((11 * 50 + 13) * 0.75f, new Barcode(Barcode.CODE_128, text).DrawOn(null)[0]);
        }
    }

    // ZXing reads the barcodes of these texts back as they are.
    [Fact]
    public void Code128TakesCodeSetCForRunsOfDigits() {
        string[] texts = {"0123456789", "42", "123", "12345", "A1234B", "A12345", "A12B", "12\t34\u00fc5678"};
        int[] codewords = {5, 1, 3, 4, 6, 5, 4, 11};     // Of the data, from the Go port's test
        for (int i = 0; i < texts.Length; i++) {
            float width = new Barcode(Barcode.CODE_128, texts[i]).DrawOn(null)[0];
            Assert.Equal((11 * (codewords[i] + 2) + 13) * 0.75f, width);
        }
    }

    // Where the bars the content draws end, in points from the top of a letter
    // page, each once, in the order of their first bar: a bar is a line moved
    // to and drawn from the top of the bars.
    private static List<float> BarEnds(string content) {
        List<float> ends = new List<float>();
        string[] lines = content.Split('\n');
        for (int i = 0; i + 1 < lines.Length; i++) {
            string[] move = lines[i].Trim().Split(' ');
            string[] line = lines[i + 1].Trim().Split(' ');
            if (move.Length == 3 && move[2] == "m" && line.Length == 3 && line[2] == "l" && move[0] == line[0]) {
                float end = Letter.PORTRAIT.GetHeight() - float.Parse(line[1], CultureInfo.InvariantCulture);
                if (!ends.Contains(end)) {
                    ends.Add(end);
                }
            }
        }
        return ends;
    }

    // How many of the bars the content draws end where the given one does.
    private static int BarsEndingAt(string content, float end) {
        int count = 0;
        string[] lines = content.Split('\n');
        for (int i = 0; i + 1 < lines.Length; i++) {
            string[] move = lines[i].Trim().Split(' ');
            string[] line = lines[i + 1].Trim().Split(' ');
            if (move.Length == 3 && move[2] == "m" && line.Length == 3 && line[2] == "l" && move[0] == line[0]
                    && Letter.PORTRAIT.GetHeight() - float.Parse(line[1], CultureInfo.InvariantCulture) == end) {
                count++;
            }
        }
        return count;
    }

    [Fact]
    public void TheGuardBarsReachFiveModulesBelowTheOthers() {
        // At a module of 2 the bars are 100 long, so the guard bars are 110.
        foreach (int type in new int[] {Barcode.EAN_13, Barcode.UPC_A}) {
            Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
            Barcode barcode = new Barcode(type, type == Barcode.UPC_A ? "01234567890" : "012345678901");
            barcode.SetModuleLength(2f);
            barcode.SetLocation(0f, 0f);
            barcode.DrawOn(page);
            Assert.Equal(new List<float> {110f, 100f}, BarEnds(TestSupport.Content(page)));
        }
    }

    [Fact]
    public void UpcATheBarsOfTheFirstAndTheLastDigitAreAsLongAsTheGuardBars() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Barcode barcode = new Barcode(Barcode.UPC_A, "01234567890");
        barcode.SetModuleLength(2f);
        barcode.SetLocation(0f, 0f);
        barcode.DrawOn(page);
        // The guard bars and the two bars of each of the digits outside them are
        // long: 3 guards of 2 bars and 2 digits of 2 bars.
        Assert.Equal(10, BarsEndingAt(TestSupport.Content(page), 110f));
    }

    [Fact]
    public void IsBlackWhateverPenColorThePageHas() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetPenColor(Color.blue);
        new Barcode(Barcode.CODE_128, "AB").DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.Contains("q\n0 0 0 RG\n") && content.EndsWith("Q\n"), content);
    }

    // The GS1-128 barcodes below were read back with ZXing, which gave the
    // symbology identifier ]C1 of GS1-128.
    [Fact]
    public void GS1128TakesCodeSetCForRunsOfDigits() {
        // Start C, FNC1, 10 codewords for the 20 digits, the check digit and the stop
        Assert.Equal((11 * 13 + 13) * 0.75f, new Barcode(Barcode.GS1_128, "(00)106141412345678908").DrawOn(null)[0]);
        // Start B, FNC1, 1 0 A 1 in code set B, Code C, 23 45, Code B, B,
        // FNC1 after the batch, 2 1 7: 14 codewords, with the start and the check digit 16
        Assert.Equal((11 * 16 + 13) * 0.75f, new Barcode(Barcode.GS1_128, "(10)A12345B(21)7").DrawOn(null)[0]);
    }

    [Fact]
    public void GS1128RefusesDataThatIsNotGS1OrTooLong() {
        new Barcode(Barcode.GS1_128, "(91)" + new string('X', 46));     // 48 characters
        string[,] cases = {
            {"(91)" + new string('X', 47), "GS1-128 barcodes hold at most 48 characters, not counting the separators!"},
            {"(01)09506000134353", "The check digit of (01) is wrong!"},
            {"01095060001343", "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!"},
        };
        for (int i = 0; i < cases.GetLength(0); i++) {
            string data = cases[i, 0];
            Assert.Equal(cases[i, 1], Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.GS1_128, data)).Message);
        }
    }

    [Fact]
    public void ITF14NeedsThirteenDigits() {
        foreach (string text in new string[] {"154001412887", "15400141288763", "154001412887A"}) {
            Assert.Equal("ITF-14 barcodes must have exactly 13 digits!",
                    Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.ITF_14, text)).Message);
        }
    }

    // ZXing reads the ITF-14 barcodes of these GTINs back, their check digits
    // added, in each direction.
    [Fact]
    public void ITF14DrawsTheBarsAndTheBearerBars() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Barcode barcode = new Barcode(Barcode.ITF_14, "1540014128876");
        barcode.SetLocation(100f, 100f);
        barcode.DrawOn(page);
        string content = TestSupport.Content(page);
        // The 39 bars: 2 of the start, 5 of each of the 7 pairs of digits and 2
        // of the stop, then the 4 bearer bars, 3 thick
        Assert.Equal(39 + 4, content.Split(" l\nS\n").Length - 1);
        Assert.Contains("3 w\n", content);
    }
    [Fact]
    public void ADescribedBarcodeIsOneFigure() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1);
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        Barcode barcode = new Barcode(Barcode.EAN_13, "400638133393");
        barcode.SetFont(font);
        barcode.SetAltDescription("EAN-13 4006381333931");
        barcode.SetLocation(50f, 50f);
        barcode.DrawOn(page);
        string content = TestSupport.Content(page);
        // The bars and the digits are the figure, and nothing else is marked.
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\n", content);
        Assert.DoesNotContain("/Artifact", content);
        Assert.DoesNotContain("/P <<", content);
        Assert.Equal(1, content.Split("BDC").Length - 1);
        Assert.Equal(1, content.Split("EMC").Length - 1);

        // Not described, its bars are decoration and its digits text, as before.
        Page plain = new Page(pdf, Letter.PORTRAIT);
        Barcode undescribed = new Barcode(Barcode.EAN_13, "400638133393");
        undescribed.SetFont(font);
        undescribed.SetLocation(50f, 50f);
        undescribed.DrawOn(plain);
        string before = TestSupport.Content(plain);
        Assert.Contains("/Artifact", before);
        Assert.Contains("/P <<", before);
    }

}
}
