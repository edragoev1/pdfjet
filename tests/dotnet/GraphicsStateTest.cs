/*
 * GraphicsStateTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The blend mode of a graphics state, a state set again on a page that was
/// read, and the fields of a form that is drawn on (from Stefan Ostermann's
/// fork of PDFjet, 19 October 2026).
/// </summary>
public class GraphicsStateTest {
    [Fact]
    public void TheBlendModeIsWrittenInTheState() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.SetGraphicsState(new GraphicsState().SetAlphaNonStroking(0.5f).SetBlendMode(BlendMode.MULTIPLY));
        page.SetGraphicsState(new GraphicsState().SetAlphaNonStroking(0.5f));
        page.SetGraphicsState(new GraphicsState().SetAlphaNonStroking(0.5f).SetBlendMode(BlendMode.MULTIPLY));
        page.SetGraphicsState(new GraphicsState().SetBlendMode(BlendMode.COLOR_DODGE));
        Assert.Equal("/GS1 gs\n/GS2 gs\n/GS1 gs\n/GS3 gs\n",
                Regex.Replace(TestSupport.Content(page), "(?s)^.*?(/GS1 gs)", "$1"));
        pdf.Complete();
        string output = TestSupport.Latin1(stream.ToArray());
        Assert.Contains("/GS1 <</CA 1 /ca 0.5 /BM /Multiply>>", output);
        Assert.Contains("/GS2 <</CA 1 /ca 0.5>>", output);
        Assert.Contains("/GS3 <</CA 1 /ca 1 /BM /ColorDodge>>", output);
    }

    [Fact]
    public void AStateWithoutABlendModeIsWrittenAsBefore() {
        GraphicsState gs = new GraphicsState().SetAlphaStroking(0.25f);
        Assert.Null(gs.GetBlendMode());
        Assert.Equal("/CA 0.25 /ca 1", gs.Entries());
        gs.SetBlendMode(BlendMode.MULTIPLY).SetBlendMode(null);
        Assert.Equal("/CA 0.25 /ca 1", gs.Entries());
    }

    [Fact]
    public void PDFA1TakesOnlyTheNormalBlendMode() {
        Action<PDF, Page> multiply = (pdf, page) =>
                page.SetGraphicsState(new GraphicsState().SetBlendMode(BlendMode.MULTIPLY));
        Assert.Equal("A document of PDF_A_1B cannot draw with the blend mode Multiply: "
                + "PDF/A-1 has no transparency.",
                ReviewMediaTest.PDFADrawError(Compliance.PDF_A_1B, multiply)[0]);
        Assert.Equal("", ReviewMediaTest.PDFADrawError(Compliance.PDF_A_2B, multiply)[0]);
        Assert.Equal("", ReviewMediaTest.PDFADrawError(Compliance.PDF_A_1B, (pdf, page) =>
                page.SetGraphicsState(new GraphicsState().SetBlendMode(BlendMode.NORMAL)))[0]);
    }

    [Fact]
    public void TheSameStateSetAgainOnAPageThatWasReadIsReused() {
        List<PDFobj> objects = TestSupport.Read(PDFTest.PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] "
                    + "/Resources << /ExtGState << /Gs1 << /CA 1 /ca 1 >> >> >> /Contents 4 0 R >>",
            "<< /Length 7 >>\nstream\n/Gs1 gs\nendstream",
        }));
        PDFobj page = new PDF(new MemoryStream()).GetPageObjects(objects)[0];
        GraphicsState highlighter = new GraphicsState().SetAlphaNonStroking(0.5f).SetBlendMode(BlendMode.MULTIPLY);
        page.SetGraphicsState(highlighter, objects);
        page.SetGraphicsState(new GraphicsState().SetAlphaNonStroking(0.5f), objects);
        page.SetGraphicsState(highlighter, objects);
        string dict = string.Join(" ", page.dict);
        Assert.Contains("/GS1 << /CA 1 /ca 0.5 /BM /Multiply >>", dict);
        Assert.Contains("/GS2 << /CA 1 /ca 0.5 >>", dict);
        Assert.DoesNotContain("/GS3", dict);
        Assert.Contains("/Gs1 << /CA 1 /ca 1 >>", dict);
    }

    // A filled-in form that is drawn on keeps its fields: the catalog of the
    // document carries over the /AcroForm of the one that was read, which a
    // viewer needs to show the values. Without one, there is none.
    [Fact]
    public void AFormThatIsDrawnOnKeepsItsFields() {
        string[] form = {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R] /NeedAppearances true >> >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Annots [4 0 R] /Resources << >> /Contents 5 0 R >>",
            "<< /Type /Annot /Subtype /Widget /FT /Tx /T (name) /V (Alice) /Rect [20 100 200 130] /P 3 0 R >>",
            "<< /Length 5 >>\nstream\nBT ET\nendstream",
        };
        string[] byReference = (string[]) form.Clone();
        byReference[0] = "<< /Type /Catalog /Pages 2 0 R /AcroForm 6 0 R >>";
        string[] noForm = (string[]) form.Clone();
        noForm[0] = "<< /Type /Catalog /Pages 2 0 R >>";
        // References of another generation take 0, as every object's (review of
        // 20 October 2026)
        string[] generationOne = (string[]) form.Clone();
        generationOne[0] = "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 1 R] /NeedAppearances true >> >>";
        string[] referenceOne = (string[]) form.Clone();
        referenceOne[0] = "<< /Type /Catalog /Pages 2 0 R /AcroForm 6 1 R >>";
        string[][] cases = {form, byReference, noForm, generationOne, referenceOne};
        string[] expected = {"/AcroForm << /Fields [ 4 0 R ] /NeedAppearances true >>\n", "/AcroForm 6 0 R\n", null, "/AcroForm << /Fields [ 4 0 R ] /NeedAppearances true >>\n", "/AcroForm 6 0 R\n"};
        for (int c = 0; c < cases.Length; c++) {
            List<PDFobj> objects = TestSupport.Read(PDFTest.PdfWithObjects(cases[c]));
            PDFobj page = new PDF().GetPageObjects(objects)[0];
            page.AddContent(Encoding.ASCII.GetBytes("0 0 m 10 10 l S\n"), objects);
            MemoryStream stream = new MemoryStream();
            PDF pdf = new PDF(stream);
            pdf.AddObjects(objects);
            pdf.Complete();
            string output = TestSupport.Latin1(stream.ToArray());
            Match root = Regex.Match(output, "/Root (\\d+) 0 R");
            Assert.True(root.Success, output);
            int start = output.IndexOf("\n" + root.Groups[1].Value + " 0 obj\n", StringComparison.Ordinal) + 1;
            string catalog = output.Substring(start, output.IndexOf("endobj", start, StringComparison.Ordinal) - start);
            if (expected[c] == null) {
                Assert.DoesNotContain("/AcroForm", catalog);
            } else {
                Assert.Contains(expected[c], catalog);
            }
        }
    }
}
}   // End of namespace PDFjet.NET
