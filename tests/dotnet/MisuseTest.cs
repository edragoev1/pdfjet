/*
 * MisuseTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// A program that uses the API the wrong way gets an exception, and Complete()
/// then refuses to finish the document, so no broken PDF is written.
/// </summary>
public class MisuseTest {
    private const string EARLIER = "The PDF was not completed because of an earlier error: ";

    private static void AssertRefused(PDF pdf, string message) {
        InvalidOperationException e = Assert.Throws<InvalidOperationException>(() => pdf.Complete());
        Assert.Equal(EARLIER + message, e.Message);
    }

    [Fact]
    public void ANumberThatIsNotFiniteOrTooLargeIsRefused() {
        foreach (float bad in new float[] {float.NaN, float.PositiveInfinity, -1e30f, 2147483648f}) {
            PDF pdf = TestSupport.NewPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            string message = Assert.Throws<ArgumentException>(() => page.DrawLine(10f, 10f, bad, 200f)).Message;
            Assert.Equal("A coordinate, size or width is NaN, infinite or too large for a PDF.", message);
            AssertRefused(pdf, message);
        }
        Page page2 = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page2.DrawLine(0f, 0f, 100000000f, 0f);
        Assert.Contains("100000000 792 l\n", TestSupport.Content(page2));
    }

    [Fact]
    public void ANaNFontSizeIsRefused() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        font.SetSize(float.NaN);
        Assert.Throws<ArgumentException>(() => new TextLine(font, "Hello").SetLocation(50f, 50f).DrawOn(page));
    }

    [Fact]
    public void ADashPatternIsAnArrayAndAPhase() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        page.SetStrokeDashPattern("[] 0");
        page.SetStrokeDashPattern("[3 3] 0");
        page.SetStrokeDashPattern(" [2.5 .5 1]  0.5 ");
        Assert.EndsWith(" [2.5 .5 1]  0.5  d\n", TestSupport.Content(page));
        foreach (string bad in new string[] {"3 3", "[3 3]", "[a] 0", "[0 0] 0", "[-1 2] 0", "[3-3] 0", "[1.5.5] 0", "[3 3] 0 ET", ""}) {
            PDF pdf = TestSupport.NewPDF();
            Page page2 = new Page(pdf, Letter.PORTRAIT);
            string message = Assert.Throws<ArgumentException>(() => page2.SetStrokeDashPattern(bad)).Message;
            Assert.Equal("The dash pattern \"" + bad + "\" is not an array of non-negative numbers, "
                    + "not all zero, followed by a phase, such as \"[3 3] 0\".", message);
            Assert.Equal("", TestSupport.Content(page2));
        }
    }

    [Fact]
    public void ANegativePenWidthIsRefused() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Assert.Equal("The pen width cannot be negative.",
                Assert.Throws<ArgumentException>(() => page.SetPenWidth(-2f)).Message);
        page.SetPenWidth(0f);
    }

    [Fact]
    public void TheGraphicsStatesMustBePaired() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string message = Assert.Throws<InvalidOperationException>(() => page.RestoreGraphicsState()).Message;
        Assert.Equal("RestoreGraphicsState was called without a matching SaveGraphicsState.", message);
        Assert.Equal("", TestSupport.Content(page));

        PDF pdf2 = TestSupport.NewPDF();
        new Page(pdf2, Letter.PORTRAIT).SaveGraphicsState();
        message = Assert.Throws<InvalidOperationException>(() => new Page(pdf2, Letter.PORTRAIT)).Message;
        Assert.Equal("A page ends with a SaveGraphicsState that has no RestoreGraphicsState.", message);

        PDF pdf3 = TestSupport.NewPDF();
        new Page(pdf3, Letter.PORTRAIT).SaveGraphicsState();
        Assert.Equal("A page ends with a SaveGraphicsState that has no RestoreGraphicsState.",
                Assert.Throws<InvalidOperationException>(() => pdf3.Complete()).Message);
    }

    [Fact]
    public void TheMarkedContentMustBePaired() {
        foreach (Compliance compliance in new Compliance[] {Compliance.PDF_1_7, Compliance.PDF_UA_1}) {
            PDF pdf = new PDF(new MemoryStream(), compliance);
            Page page = new Page(pdf, Letter.PORTRAIT);
            string message = Assert.Throws<InvalidOperationException>(() => page.AddEMC()).Message;
            Assert.Equal("AddEMC was called without a matching AddBDC or AddArtifactBMC.", message);

            PDF pdf2 = new PDF(new MemoryStream(), compliance);
            new Page(pdf2, Letter.PORTRAIT).AddBDC(StructElem.P, "x", "x");
            Assert.Equal("A page ends with an AddBDC or AddArtifactBMC that has no AddEMC.",
                    Assert.Throws<InvalidOperationException>(() => pdf2.Complete()).Message);

            PDF pdf3 = new PDF(new MemoryStream(), compliance);
            Page page3 = new Page(pdf3, Letter.PORTRAIT);
            page3.AddArtifactBMC();
            page3.AddEMC();
            pdf3.Complete();
        }
    }

    [Fact]
    public void AWrittenPageCannotBeDrawnOn() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new Page(pdf, Letter.PORTRAIT);
        string message = Assert.Throws<InvalidOperationException>(
                () => new TextLine(font, "Late").SetLocation(50f, 50f).DrawOn(page1)).Message;
        Assert.Equal("The page was already written to the PDF: draw on a page before "
                + "creating the next page or completing the PDF.", message);
        AssertRefused(pdf, message);
    }

    [Fact]
    public void CompleteFinishesADocumentOnce() {
        PDF pdf = TestSupport.NewPDF();
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        Assert.Equal("Complete() was already called.",
                Assert.Throws<InvalidOperationException>(() => pdf.Complete()).Message);
        Assert.Equal("The PDF was already completed.",
                Assert.Throws<InvalidOperationException>(() => new Page(pdf, Letter.PORTRAIT)).Message);
    }

    [Fact]
    public void ADocumentNeedsAPage() {
        PDF pdf = TestSupport.NewPDF();
        Assert.Equal("A PDF needs at least one page.",
                Assert.Throws<InvalidOperationException>(() => pdf.Complete()).Message);
    }

    [Fact]
    public void APageIsAddedOnceAndToItsOwnDocument() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pdf.AddPage(page);
        Assert.Equal("The page was already added to the PDF.",
                Assert.Throws<InvalidOperationException>(() => pdf.AddPage(page)).Message);

        PDF pdf2 = TestSupport.NewPDF();
        Page foreign = new Page(TestSupport.NewPDF(), Letter.PORTRAIT, Page.DETACHED);
        Assert.Equal("The page belongs to another PDF.",
                Assert.Throws<ArgumentException>(() => pdf2.AddPage(foreign)).Message);
    }

    [Fact]
    public void FontsImagesStampsAndGroupsBelongToOneDocument() {
        PDF other = TestSupport.NewPDF();
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);

        Font font = TestSupport.Helvetica(other);
        Assert.Equal("The font belongs to another PDF.", Assert.Throws<ArgumentException>(
                () => new TextLine(font, "Hello").SetLocation(50f, 50f).DrawOn(page)).Message);

        Image image = new Image(other, TestSupport.Open("PngSuite/BASN2C08.PNG"));
        Assert.Equal("The image belongs to another PDF.", Assert.Throws<ArgumentException>(
                () => image.SetLocation(50f, 50f).DrawOn(page)).Message);

        Stamp stamp = new Stamp(other).SetSize(50f, 50f);
        stamp.Complete();
        Assert.Equal("The stamp belongs to another PDF.",
                Assert.Throws<ArgumentException>(() => stamp.DrawOn(page)).Message);
        Stamp stamp2 = new Stamp(pdf).SetSize(50f, 50f);
        Assert.Equal("The font belongs to another PDF.",
                Assert.Throws<ArgumentException>(() => stamp2.AddFont(font)).Message);

        OptionalContentGroup group = new OptionalContentGroup(other, "Layer");
        group.Add(new Rect(0f, 0f, 1f, 1f));
        Assert.Equal("The optional content group belongs to another PDF.",
                Assert.Throws<ArgumentException>(() => group.DrawOn(page)).Message);
    }

    [Fact]
    public void StampTextNeedsAFontAndAText() {
        PDF pdf = TestSupport.NewPDF();
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        foreach (TextParameters parameters in new TextParameters[] {
                new TextParameters().SetText("Paid"), new TextParameters().SetFont(TestSupport.Helvetica(pdf))}) {
            Assert.Equal("Stamp text needs a font and a text.",
                    Assert.Throws<ArgumentException>(() => stamp.DrawText(parameters)).Message);
        }
    }

    [Fact]
    public void AStampIsCompletedOnceBeforeItIsDrawn() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = new Stamp(pdf).SetSize(50f, 50f);
        Assert.Equal("Call Complete() on the stamp before drawing it.",
                Assert.Throws<InvalidOperationException>(() => stamp.DrawOn(page)).Message);

        PDF pdf2 = TestSupport.NewPDF();
        Stamp stamp2 = new Stamp(pdf2).SetSize(50f, 50f);
        stamp2.Complete();
        Assert.Equal("Complete() was already called on the stamp.",
                Assert.Throws<InvalidOperationException>(() => stamp2.Complete()).Message);
        Assert.Equal("The stamp was already completed.",
                Assert.Throws<InvalidOperationException>(() => stamp2.DrawRect(0f, 0f, 10f, 10f)).Message);
    }

    [Fact]
    public void StampTextAddsItsFont() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Font core = TestSupport.Helvetica(pdf);
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        Assert.Equal("A stamp draws text with an embedded font, not a core or CJK font.",
                Assert.Throws<ArgumentException>(() => stamp.DrawText(core, 12f, 5f, 20f, "Paid")).Message);
        stamp.DrawText(font, 12f, 5f, 20f, "Paid");
        stamp.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Contains("/Font <<\n/F" + font.objNumber + " " + font.objNumber + " 0 R\n", raw);
    }

    [Fact]
    public void EncryptionAndComplianceComeBeforeTheContent() {
        PDF pdf = TestSupport.NewPDF();
        TestSupport.Helvetica(pdf);
        Assert.Equal("Set the encryption before adding fonts, images or pages to the PDF.",
                Assert.Throws<InvalidOperationException>(
                        () => new Encryption(pdf, new Passwords(), new Permissions())).Message);
        Assert.Equal("Set the compliance before adding fonts, images or pages to the PDF.",
                Assert.Throws<InvalidOperationException>(() => pdf.SetCompliance(Compliance.PDF_UA_1)).Message);
        pdf.SetCompliance(Compliance.PDF_1_7);   // No change.

        PDF pdf2 = TestSupport.NewPDF();
        new Page(pdf2, Letter.PORTRAIT, Page.DETACHED);
        Assert.Equal("Set the compliance before adding fonts, images or pages to the PDF.",
                Assert.Throws<InvalidOperationException>(() => pdf2.SetCompliance(Compliance.PDF_UA_1)).Message);

        PDF pdf3 = TestSupport.NewPDF();
        Encryption encryption = new Encryption(pdf3, new Passwords(), new Permissions());
        TestSupport.Helvetica(pdf3);
        Assert.Equal("Set the encryption before adding fonts, images or pages to the PDF.",
                Assert.Throws<InvalidOperationException>(() => pdf3.SetEncryption(encryption)).Message);
    }

    [Fact]
    public void APageIsFromThreeTo14400PointsWideAndHigh() {
        new Page(TestSupport.NewPDF(), new PageSize(3f, 3f));
        new Page(TestSupport.NewPDF(), new PageSize(14400f, 14400f));
        foreach (PageSize bad in new PageSize[] {
                new PageSize(0f, 0f), new PageSize(612f, 2f), new PageSize(14401f, 792f), new PageSize(float.NaN, 792f)}) {
            PDF pdf = TestSupport.NewPDF();
            Assert.Equal("A page must be from 3 to 14400 points wide and high.",
                    Assert.Throws<ArgumentException>(() => new Page(pdf, bad)).Message);
        }
    }

    [Fact]
    public void TheXmpMetadataLeavesOutControlCharacters() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Report\u0001 2026\uFFFF \uD800");
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        string raw = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("<rdf:li xml:lang=\"x-default\">Report 2026 </rdf:li>", raw);
    }

    [Fact]
    public void AZeroSizeImageStampOrContainerDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Image image = new Image(pdf, TestSupport.Open("PngSuite/BASN2C08.PNG"));
        image.ScaleBy(0f);
        image.SetLocation(50f, 50f).DrawOn(page);
        Stamp stamp = new Stamp(pdf).SetSize(50f, 50f);
        stamp.Complete();
        stamp.ScaleBy(0f).SetLocation(50f, 50f).DrawOn(page);
        Container container = new Container(100f, 100f);
        container.Add(new Rect(0f, 0f, 10f, 10f));
        container.ScaleBy(0f).SetLocation(50f, 50f).DrawOn(page);
        Assert.DoesNotContain(" cm\n", TestSupport.Content(page));
    }

    // The objects of a small PDF, as Read returns them.
    private static List<PDFobj> ExistingObjects() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        new TextLine(TestSupport.Helvetica(pdf), "Existing").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        return TestSupport.Read(stream.ToArray());
    }

    [Fact]
    public void TheObjectsOfAnExistingPdfComeBeforeTheContent() {
        List<PDFobj> objects = ExistingObjects();
        string message = "Add the objects of an existing PDF before fonts, images or pages "
                + "are added to the PDF: object 1 is already written.";

        PDF pdf = TestSupport.NewPDF();
        TestSupport.Helvetica(pdf);     // Object 1, which the objects would replace.
        Assert.Equal(message,
                Assert.Throws<InvalidOperationException>(() => pdf.AddResourceObjects(objects)).Message);
        AssertRefused(pdf, message);

        PDF pdf2 = TestSupport.NewPDF();
        TestSupport.Helvetica(pdf2);
        Assert.Equal(message,
                Assert.Throws<InvalidOperationException>(() => pdf2.AddObjects(objects)).Message);

        // The objects first, and the font after them.
        PDF pdf3 = TestSupport.NewPDF();
        pdf3.AddResourceObjects(objects);
        TestSupport.Helvetica(pdf3);
        new Page(pdf3, Letter.PORTRAIT);
        pdf3.Complete();
    }

    [Fact]
    public void TheObjectsOfAnExistingPdfCannotBeAddedToAnEncryptedPdf() {
        List<PDFobj> objects = ExistingObjects();
        PDF pdf = TestSupport.NewPDF();
        pdf.SetEncryption(new Encryption(pdf, new Passwords(), new Permissions()));
        string message = "The objects of an existing PDF cannot be added to an encrypted PDF.";
        Assert.Equal(message,
                Assert.Throws<InvalidOperationException>(() => pdf.AddResourceObjects(objects)).Message);
        AssertRefused(pdf, message);
    }

    [Fact]
    public void APdfACannotBeEncrypted() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_A_2B);
        Encryption encryption = new Encryption(pdf, new Passwords(), new Permissions());
        string message = "A PDF/A document cannot be encrypted.";
        Assert.Equal(message,
                Assert.Throws<InvalidOperationException>(() => pdf.SetEncryption(encryption)).Message);
        AssertRefused(pdf, message);

        // A PDF/UA document can be.
        PDF ua = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        ua.SetEncryption(new Encryption(ua, new Passwords(), new Permissions()));
    }
}
}
