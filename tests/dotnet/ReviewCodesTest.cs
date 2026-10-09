/*
 * ReviewCodesTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The charts, the barcodes, the QR codes, Data Matrix and PDF417, and the
/// encryption, as the Go tests in review_codes_test.go have them.
/// </summary>
public class ReviewCodesTest {
    // Returns the bounding box of the structure element, in the coordinates of the PDF.
    private static float[] BBox(StructElement element) {
        Match match = Regex.Match(element.attributes ?? "", "/BBox \\[(\\S+) (\\S+) (\\S+) (\\S+)\\]");
        Assert.True(match.Success, "no /BBox in " + element.attributes);
        float[] box = new float[4];
        for (int i = 0; i < 4; i++) {
            box[i] = float.Parse(match.Groups[i + 1].Value, CultureInfo.InvariantCulture);
        }
        return box;
    }

    private static PDF TaggedPDF() {
        return new PDF(new MemoryStream(), Compliance.PDF_UA_1);
    }

    [Fact]
    public void FlatDataOfLargeValuesHasARange() {
        foreach (float value in new float[] {2e7f, -3e7f, 1e30f}) {
            PDF pdf = TestSupport.NewPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.Helvetica(pdf);
            Chart chart = new Chart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f);
            chart.AddSeries("").AddPoint(value, value).AddPoint(value, value);
            chart.DrawOn(page);
            Assert.DoesNotContain("NaN", TestSupport.Content(page));
            pdf.Complete();
        }
    }

    [Fact]
    public void TheRoundedRangeOfFlatDataHasAGridLine() {
        foreach (float value in new float[] {0f, 5f, 2e7f, -3e7f, 1e30f}) {
            Round round = Chart.RoundMaxAndMinValues(value, value);
            Assert.True(round.maxValue > round.minValue, value + ": " + round.minValue + " to " + round.maxValue);
            Assert.True(round.numOfGridLines >= 1, value + ": " + round.numOfGridLines + " grid lines");
        }
    }

    [Fact]
    public void ABarChartValueThatIsNotANumberHasNoBar() {
        foreach (bool stacked in new bool[] {false, true}) {
            PDF pdf = TestSupport.NewPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.Helvetica(pdf);
            BarChart chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                    .SetCategories("a", "b", "c").SetStacked(stacked).SetDrawValueLabels(true);
            chart.AddSeries("", new float[] {float.NaN, 10f, float.PositiveInfinity});
            TestSupport.AssertXY(350f, 250f, chart.DrawOn(page));
            string content = TestSupport.Content(page);
            Assert.DoesNotContain("NaN", content);
            Assert.DoesNotContain(TestSupport.Hex("NaN"), content);
            Assert.Contains(TestSupport.Hex("10"), content);
            pdf.Complete();
        }
    }

    [Fact]
    public void Code128TakesACharacterFrom128To159AsFNC4ShiftAndTheControlCharacter() {
        List<int> list = Barcode.Code128Codewords("\u0085x\u009f");
        List<int> want = new List<int> {Code128Table.START_B,
                Code128Table.FNC_4, Code128Table.SHIFT, 0x05 + 64, 'x' - 32,
                Code128Table.FNC_4, Code128Table.SHIFT, 0x1f + 64};
        Assert.Equal(want, list);
        // Each takes three of the 48 codewords
        new Barcode(Barcode.CODE_128, new string('\u0080', 16)).DrawOn(new Page(TestSupport.NewPDF(), Letter.PORTRAIT));
        Exception e = Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.CODE_128, new string('\u0080', 17)));
        Assert.Contains("one from 128 to 159 three", e.Message);
    }

    [Fact]
    public void TheBarcodeFigureHasTheBoxOfTheBarsAndTheDigits() {
        PDF pdf = TaggedPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        float height = page.height;
        object[][] cases = {
            new object[] {Barcode.EAN_13, "400638133393", Direction.LEFT_TO_RIGHT},
            new object[] {Barcode.UPC_A, "03600029145", Direction.LEFT_TO_RIGHT},
            new object[] {Barcode.UPC_A, "03600029145", Direction.TOP_TO_BOTTOM},
            new object[] {Barcode.CODE_128, "1111111111111111", Direction.LEFT_TO_RIGHT},
        };
        for (int i = 0; i < cases.Length; i++) {
            Direction direction = (Direction) cases[i][2];
            Barcode barcode = new Barcode((int) cases[i][0], (string) cases[i][1]);
            barcode.SetDirection(direction);
            barcode.SetFont(font);
            barcode.SetAltDescription((string) cases[i][1]);
            barcode.SetLocation(100f, 100f);
            float[] xy = barcode.DrawOn(page);
            float[] box = BBox(page.structures[i]);
            // The first digit of EAN-13 and UPC-A is left of the bars, and the
            // digits of a barcode drawn top to bottom left of them, the first
            // above them; the digits of Code 128 are wider than its bars.
            Assert.True(box[0] < 100f, "case " + i + ": the box starts at x " + box[0]);
            float top = height - box[3];
            if (direction == Direction.TOP_TO_BOTTOM) {
                Assert.True(top < 100f, "case " + i + ": the box has its top at " + top);
            } else {
                Assert.Equal(100f, top, 2);
            }
            Assert.Equal(xy[0], box[2], 2);
            Assert.Equal(height - xy[1], box[1], 2);
        }
    }

    [Fact]
    public void TheDonutChartFigureHasTheBoxOfTheLabels() {
        PDF pdf = TaggedPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        new DonutChart(font, font).SetLocation(100f, 100f).SetRadii(100f, 50f)
                .AddSlice(new Slice(25f, Color.red, "Apples and pears"))
                .AddSlice(new Slice(75f, Color.blue, "Oranges")).DrawOn(page);
        float[] box = BBox(page.structures[0]);
        // The circle is from 100 to 300; the labels are right and left of it
        Assert.True(box[0] < 100f && box[2] > 300f, "the box is not wider than the circle");
    }

    private static PDFobj Obj(int number, string raw) {
        PDFobj obj = new PDFobj();
        obj.number = number;
        obj.dict.AddRange(raw.Split(' '));
        return obj;
    }

    [Fact]
    public void AnAESKeyThatIsTooShortIsRefused() {
        PDFobj encrypt = Obj(6, "6 0 obj << /Filter /Standard /V 5 /R 4 /Length 40 " +
                "/CF << /StdCF << /CFM /AESV2 >> >> /StmF /StdCF /StrF /StdCF /O <00> /U <00> /P -4 >> endobj");
        Exception e = Assert.ThrowsAny<Exception>(() => new Decryptor(encrypt, new List<PDFobj>(), null, ""));
        Assert.Equal("The encryption of the PDF is not valid: /R 4 with a key of 40 bits for AES", e.Message);
        // A decryptor of such a key decrypts to nothing
        Decryptor decryptor = new Decryptor(new byte[5], Decryptor.AES_128, Decryptor.AES_128);
        Assert.Empty(decryptor.Decrypt(new byte[48], Decryptor.AES_128, encrypt));
    }

    [Fact]
    public void TheContentsOfASignatureAreNotDecrypted() {
        Decryptor decryptor = new Decryptor(new byte[16], Decryptor.RC4, Decryptor.RC4);
        string signature = "<0123456789ABCDEF>";
        foreach (string raw in new string[] {
                "7 0 obj << /Type /Sig /Filter /Adobe.PPKLite /Contents " + signature + " >> endobj",
                "7 0 obj << /ByteRange [ 0 10 20 30 ] /Contents " + signature + " >> endobj",
                "7 0 obj << /FT /Sig /V << /Type /Sig /Contents " + signature + " >> /T (Signature1) >> endobj"}) {
            PDFobj obj = Obj(7, raw);
            decryptor.DecryptStrings(obj);
            Assert.Contains("/Contents " + signature, string.Join(" ", obj.dict));
        }
        // The /Contents of another dictionary is decrypted
        PDFobj note = Obj(7, "7 0 obj << /Type /Annot /Contents (Note) >> endobj");
        decryptor.DecryptStrings(note);
        Assert.DoesNotContain("(Note)", string.Join(" ", note.dict));
    }

    [Fact]
    public void ThePermissionsOfTheCallerAreNotChanged() {
        Permissions permissions = new Permissions().SetAccess(UserAccess.PRINT);
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Encrypted title");
        pdf.SetEncryption(new Encryption(pdf,
                new Passwords().SetUserPassword("").SetOwnerPassword("owner"), permissions));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(new Font(pdf, CoreFont.HELVETICA), "Secret text").SetLocation(50f, 50f).DrawOn(page);
        pdf.Complete();
        Assert.Equal(UserAccess.PRINT, permissions.GetAccess());
        // The PDF grants the extraction for accessibility all the same
        Match match = Regex.Match(TestSupport.Latin1(stream.ToArray()), "/P (-?\\d+)");
        Assert.True(match.Success);
        int access = int.Parse(match.Groups[1].Value);
        Assert.True((access & (int) UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY) != 0);
    }

    [Fact]
    public void QRTextThatIsNotASCIIStartsWithTheECIOfUTF8() {
        // ECI 0111, the assignment number 26 in 8 bits, then byte mode 0100.
        // Version 4 at level L has one block, so the data codewords come first.
        byte[] data = new QRCode("Grüße", ErrorCorrectionLevel.L).CreateData(ErrorCorrectionLevel.L);
        Assert.Equal(0x71, data[0]);
        Assert.Equal(0xA4, data[1]);
        // ASCII starts with byte mode, as before
        data = new QRCode("Hello", ErrorCorrectionLevel.L).CreateData(ErrorCorrectionLevel.L);
        Assert.Equal(0x40, data[0]);
        Assert.Equal(0x54, data[1]);
    }

    [Fact]
    public void QRTheECIIsCountedInTheCapacity() {
        // 2,953 bytes fit at level L, and 2,952 when they are not all ASCII
        string fits = new StringBuilder().Insert(0, "é", 1476).ToString();
        Assert.Equal(177, new QRCode(fits, ErrorCorrectionLevel.L).GetModules().Length);
        Exception e = Assert.ThrowsAny<Exception>(() => new QRCode(fits + "a", ErrorCorrectionLevel.L));
        Assert.Equal("The data is too long for a QR code at level L: 2953 bytes, at most 2952.", e.Message);
    }

    private static bool[][] Light(int n) {
        bool[][] matrix = new bool[n][];
        for (int i = 0; i < n; i++) {
            matrix[i] = new bool[n];
        }
        return matrix;
    }

    [Fact]
    public void QRThePenaltyIsThatOfISO18004() {
        // 5 by 5 light modules: N1 10 runs of 5, 30; N2 16 blocks, 48; N4 no
        // dark module, 100.
        Assert.Equal(178, QRUtil.GetLostPoint(Light(5)));
        // 7 by 7 with 1011101 in the first row: N1 60; N2 30 blocks, 90; N3 the
        // pattern after the light quiet zone, 40; N4 5 of 49 dark, 70.
        bool[][] matrix = Light(7);
        bool[] row = {true, false, true, true, true, false, true};
        for (int col = 0; col < 7; col++) {
            matrix[0][col] = row[col];
        }
        Assert.Equal(260, QRUtil.GetLostPoint(matrix));
    }

    [Fact]
    public void QRTheDarkModulesOfARowAreOneRectangle() {
        Page page = new Page(TaggedPDF(), Letter.PORTRAIT);
        page.SetBrushColor(Color.blue);
        QRCode qr = new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M).SetModuleColor(Color.red);
        qr.DrawOn(page);
        string content = TestSupport.Content(page);
        bool?[][] modules = qr.GetModules();
        int runs = 0;
        foreach (bool?[] r in modules) {
            for (int col = 0; col < r.Length; col++) {
                if (r[col] == true && (col == 0 || r[col - 1] != true)) {
                    runs++;
                }
            }
        }
        Assert.Equal(runs, Regex.Matches(content, " re\n").Count);
        // The brush is saved and restored around the modules
        Assert.Contains("/Artifact BMC\nq\n", content);
        Assert.EndsWith("Q\nEMC\n", content);
        // The page knows the brush is blue again, and sets red when asked
        page.SetBrushColor(Color.red);
        Assert.EndsWith("EMC\n1 0 0 rg\n", TestSupport.Content(page));
    }

    [Fact]
    public void ADescribedDataMatrixIsAFigure() {
        PDF pdf = TaggedPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new DataMatrix("Hello, World!").SetAltDescription("Hello, World!").SetLocation(50f, 50f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\nq\n", content);
        Assert.DoesNotContain("/Artifact", content);
        // Not described, it is decoration, as before
        Page plain = new Page(pdf, Letter.PORTRAIT);
        new DataMatrix("Hello, World!").DrawOn(plain);
        Assert.StartsWith("/Artifact BMC\nq\n", TestSupport.Content(plain));
    }

    [Fact]
    public void TheDataMatrixKeepsTheBrushOfThePage() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetBrushColor(Color.blue);
        new DataMatrix("Hello").SetModuleColor(Color.red).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.EndsWith("Q\n", content);
        // The page knows the brush is blue again, and sets red when asked
        page.SetBrushColor(Color.blue);
        page.SetBrushColor(Color.red);
        Assert.Equal("1 0 0 rg\n", TestSupport.Content(page).Substring(content.Length));
    }

    [Fact]
    public void APDF417ControlCharacterIsShiftedToByteCompaction() {
        // G S in alpha, then the pad and GS after the shift 913, then s e p
        // after the latch to lower case
        List<int> want = new List<int> {30*6 + 18, 30*26 + 29, 913, 0x1D, 30*27 + 18, 30*4 + 15};
        Assert.Equal(want, new PDF417("GS \u001dsep").DataCodewords());
        // HT, LF and CR are in text compaction
        Assert.DoesNotContain(913, new PDF417("a\tb\nc\r").DataCodewords());
    }

    [Fact]
    public void ThePDF417BarsAreOneBlackArtifact() {
        Page page = new Page(TaggedPDF(), Letter.PORTRAIT);
        page.SetPenColor(Color.red);
        new PDF417("Hello, World!").DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.StartsWith("1 0 0 RG\n/Artifact BMC\nq\n0 0 0 RG\n", content);
        Assert.EndsWith("Q\nEMC\n", content);
        Assert.Single(Regex.Matches(content, "BMC"));
        // The page knows the pen is red again
        page.SetPenColor(Color.red);
        Assert.Equal(content, TestSupport.Content(page));
    }

    [Fact]
    public void ADescribedPDF417IsAFigure() {
        Page page = new Page(TaggedPDF(), Letter.PORTRAIT);
        new PDF417("Hello, World!").SetAltDescription("Hello, World!").SetLocation(50f, 50f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\nq\n", content);
        Assert.DoesNotContain("/Artifact", content);
    }
}
}   // End of namespace PDFjet.NET
