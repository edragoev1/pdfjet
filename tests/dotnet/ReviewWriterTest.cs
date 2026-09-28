/*
 * ReviewWriterTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
// The tests of the writer: the catalog, the structure tree, the annotations,
// the metadata and the files a document carries.
public class ReviewWriterTest {
    // Writes a document of the compliance with a heading on its one page,
    // drawn with draw, and returns it.
    private static string Document(Compliance compliance, Action<PDF, Page> draw) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, compliance).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Heading")
                .SetStructureType(StructElem.H1).SetLocation(50f, 50f).DrawOn(page);
        if (draw != null) {
            draw(pdf, page);
        }
        pdf.Complete();
        return TestSupport.Latin1(stream.ToArray());
    }

    private static int Count(string text, string part) {
        return text.Split(part).Length - 1;
    }

    [Fact]
    public void APDFAOfLevelBIsNotTagged() {
        foreach (Compliance compliance in new Compliance[] {
                Compliance.PDF_A_1B, Compliance.PDF_A_2B, Compliance.PDF_A_3B}) {
            string raw = Document(compliance, null);
            foreach (string entry in new string[] {"/StructTreeRoot", "/MarkInfo", "/Tabs /S", "/StructParents", "/StructElem"}) {
                Assert.False(raw.Contains(entry), compliance + " has " + entry);
            }
            foreach (string entry in new string[] {"/Lang <", "/DisplayDocTitle true"}) {
                Assert.True(raw.Contains(entry), compliance + " has no " + entry);
            }
        }
        foreach (Compliance compliance in new Compliance[] {
#pragma warning disable CS0618 // PDF_A_3A is deprecated, and still written
                Compliance.PDF_A_1A, Compliance.PDF_A_2A, Compliance.PDF_A_3A,
#pragma warning restore CS0618
                Compliance.PDF_UA_1, Compliance.PDF_A_3A_UA_1}) {
            string raw = Document(compliance, null);
            foreach (string entry in new string[] {"/StructTreeRoot", "/MarkInfo <</Marked true>>", "/Tabs /S", "/StructParents 0"}) {
                Assert.True(raw.Contains(entry), compliance + " has no " + entry);
            }
        }
    }

    [Fact]
    public void TheNoticeOfAFontIsEscapedInItsMetadata() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.AddMetadataObject("Copyright A & B <c>", true);
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Contains("Copyright A &amp; B &lt;c&gt;", raw);

        List<PDFobj> objects = new List<PDFobj>();
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        font.info = "Copyright A & B";
        int number = FontStream2.AddMetadataObject(objects, font);
        string xml = Encoding.UTF8.GetString(objects[number - 1].stream);
        Assert.Contains("Copyright A &amp; B", xml);
    }

    // Draws a square, a circle, a polygon, a note and a file.
    private static void Annotations(PDF pdf, Page page) {
        SquareAnnotation square = new SquareAnnotation();
        square.SetLocation(100f, 100f);
        square.SetSize(50f, 50f);
        square.SetContents("A square");
        square.DrawOn(page);
        CircleAnnotation circle = new CircleAnnotation();
        circle.SetLocation(200f, 100f);
        circle.SetSize(80f, 40f);
        circle.SetContents("A circle");
        circle.DrawOn(page);
        PolygonAnnotation polygon = new PolygonAnnotation().SetVertices(new float[] {0f, 0f, 50f, 0f, 25f, 40f});
        polygon.SetLocation(300f, 300f);
        polygon.SetContents("A polygon");
        polygon.DrawOn(page);
        TextAnnotation note = new TextAnnotation();
        note.SetLocation(100f, 400f);
        note.SetSize(20f, 20f);
        note.SetContents("A note");
        note.DrawOn(page);
        EmbeddedFile file = new EmbeddedFile(pdf, "a.txt", new MemoryStream(Encoding.UTF8.GetBytes("A file")), false);
        FileAttachment attachment = new FileAttachment(file);
        attachment.SetLocation(200f, 400f);
        attachment.SetContents("A file");
        attachment.DrawOn(page);
    }

    [Fact]
    public void EveryAnnotationIsPrintedAndHasAnAppearanceInPDFA() {
#pragma warning disable CS0618 // PDF_A_3A is deprecated, and still written
        foreach (Compliance compliance in new Compliance[] {Compliance.PDF_1_7, Compliance.PDF_A_2B, Compliance.PDF_A_3A}) {
#pragma warning restore CS0618
            string raw = Document(compliance, Annotations);
            Assert.Equal(5, Count(raw, "/Type /Annot\n"));
            Assert.Equal(5, Count(raw, "/F 4\n"));
            int appearances = (compliance == Compliance.PDF_1_7) ? 0 : 5;
            Assert.Equal(appearances, Count(raw, "/AP <</N "));
            Assert.Equal(appearances, Count(raw, "/Subtype /Form\n"));
        }
        // The square is drawn in its fill color in its box, which is its rectangle.
        string pdfa = Document(Compliance.PDF_A_2B, Annotations);
        Assert.Contains("/BBox [100 642 150 692]\n/Length 34\n>>\nstream\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n", pdfa);
    }

    [Fact]
    public void ALinkToADestinationTheDocumentDoesNotHaveIsRefused() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Nowhere").SetGoToAction("missing").SetLocation(50f, 100f).DrawOn(page);
        Assert.Equal("The link goes to the destination missing, which the document does not have.",
                Assert.Throws<InvalidOperationException>(() => pdf.Complete()).Message);
    }

    [Fact]
    public void TheTypesOfPDF20AreMappedInTheRoleMap() {
        Assert.DoesNotContain("/RoleMap", Document(Compliance.PDF_UA_1, null));
        string raw = Document(Compliance.PDF_UA_1, (pdf, page) => {
            Font font = TestSupport.Helvetica(pdf);
            new TextLine(font, "Strong").SetStructureType(StructElem.STRONG).SetLocation(50f, 100f).DrawOn(page);
            new TextLine(font, "Title").SetStructureType(StructElem.TITLE).SetLocation(50f, 120f).DrawOn(page);
        });
        Assert.Contains("/RoleMap << /Title /P /Strong /Span >>\n", raw);
    }

    [Fact]
    public void ATextLineOfTheTypeArtifactIsAnArtifact() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Header")
                .SetStructureType(StructElem.ARTIFACT).SetLocation(50f, 50f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("/Artifact BMC\n", content);
        Assert.DoesNotContain("/Artifact <<", content);
        Assert.Empty(page.structures);

        PDF pdf2 = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        Page page2 = new Page(pdf2, Letter.PORTRAIT);
        Assert.Equal("An artifact is not a structure element: draw it between AddArtifactBMC and AddEMC.",
                Assert.Throws<InvalidOperationException>(() => page2.BeginStructElement(StructElem.ARTIFACT)).Message);
    }

    [Fact]
    public void AnAnnotationOnAWrittenPageOfATaggedDocumentIsRefused() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new Page(pdf, Letter.PORTRAIT);
        SquareAnnotation square = new SquareAnnotation();
        square.SetContents("Late");
        Assert.Equal("The page was already written to the PDF: "
                + "draw on a page before creating the next page or completing the PDF.",
                Assert.Throws<InvalidOperationException>(() => square.DrawOn(page1)).Message);
        Assert.Empty(page1.annots);
    }

    [Fact]
    public void APDFUADocumentNeedsATitle() {
        foreach (Compliance compliance in new Compliance[] {Compliance.PDF_UA_1, Compliance.PDF_A_3A_UA_1}) {
            PDF pdf = new PDF(new MemoryStream(), compliance).SetTitle(" ");
            new Page(pdf, Letter.PORTRAIT);
            Assert.Equal("A PDF/UA document needs a title: use SetTitle.",
                    Assert.Throws<InvalidOperationException>(() => pdf.Complete()).Message);
        }
    }

    [Fact]
    public void AnAnnotationOfATaggedDocumentNeedsADescription() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        Assert.Equal("An annotation of a tagged document, PDF/UA or PDF/A of level A, "
                + "needs contents, a title or an alternative description.",
                Assert.Throws<InvalidOperationException>(() => new SquareAnnotation().DrawOn(page)).Message);
        // One that is not tagged needs none.
        PDF pdf2 = TestSupport.NewPDF();
        new SquareAnnotation().DrawOn(new Page(pdf2, Letter.PORTRAIT));
        pdf2.Complete();
    }

    // Returns the text as the writer writes a text string that is not encrypted.
    private static string TextString(string text) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        int start = (int) stream.Length;
        pdf.AppendTextString(text);
        return TestSupport.Latin1(stream.ToArray()).Substring(start);
    }

    [Fact]
    public void TheInformationSaysWhatTheMetadataSays() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_A_2B);
        pdf.SetTitle("A\u0001B\uD800C￾D\tE").SetAuthor("F\u0002G");
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        string raw = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("<rdf:li xml:lang=\"x-default\">ABCD\tE</rdf:li>", raw);
        Assert.Contains("/Title " + TextString("ABCD\tE") + "\n", raw);
        Assert.Contains("<rdf:li>FG</rdf:li>", raw);
        Assert.Contains("/Author " + TextString("FG") + "\n", raw);
    }

    [Fact]
    public void TheDateOfAnEmbeddedFileIsEncrypted() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.SetEncryption(new Encryption(pdf, new Passwords(), new Permissions()));
        string date = pdf.GetDate();
        new EmbeddedFile(pdf, "a.xml", new MemoryStream(Encoding.UTF8.GetBytes("<a/>")),
                false, "text/xml", Relationship.DATA, null);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.DoesNotContain(date, raw);
        Assert.DoesNotContain(Util.ToHexString(Encoding.ASCII.GetBytes(date)), raw);
        Assert.Contains("/ModDate <", raw);
    }

    // A stream that says whether it was closed.
    private sealed class ClosingStream : MemoryStream {
        internal bool closed = false;

        public override void Close() {
            closed = true;
            base.Close();
        }
    }

    [Fact]
    public void CompleteClosesTheStreamWhenItFails() {
        ClosingStream stream = new ClosingStream();
        PDF pdf = new PDF(stream);
        Assert.Throws<InvalidOperationException>(() => pdf.Complete());
        Assert.True(stream.closed, "the stream is open");
    }

    [Fact]
    public void TheLayersAreOrderedByTheirUTF16Names() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        List<OptionalContentGroup> groups = new List<OptionalContentGroup>();
        foreach (string name in new string[] {"", "b", "a", "😀", "a"}) {
            OptionalContentGroup group = new OptionalContentGroup(pdf, name);
            group.Add(new Rect(10f, 10f, 20f, 20f));
            group.DrawOn(page);
            groups.Add(group);
        }
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        StringBuilder order = new StringBuilder("/Order [");
        foreach (int i in new int[] {2, 4, 1, 3, 0}) {
            order.Append(' ').Append(groups[i].objNumber.ToString(CultureInfo.InvariantCulture)).Append(" 0 R ");
        }
        Assert.Contains(order + "]\n", raw);
    }

    [Fact]
    public void APDFA3FileNeedsAMediaType() {
#pragma warning disable CS0618 // PDF_A_3A is deprecated, and still written
        foreach (Compliance compliance in new Compliance[] {Compliance.PDF_A_3A, Compliance.PDF_A_3B, Compliance.PDF_A_3A_UA_1}) {
#pragma warning restore CS0618
            PDF pdf = new PDF(new MemoryStream(), compliance);
            EmbeddedFile file = new EmbeddedFile(pdf, "a.xml", new MemoryStream(Encoding.UTF8.GetBytes("<a/>")),
                    false, null, Relationship.DATA, "Data.");
            Assert.Equal("The file a.xml was embedded without a media type, "
                    + "which a file of a document of PDF/A-3 needs.",
                    Assert.Throws<ArgumentException>(() => pdf.AddAssociatedFile(file)).Message);
        }
        PDF pdf2 = TestSupport.NewPDF();
        pdf2.AddAssociatedFile(new EmbeddedFile(pdf2, "a.xml", new MemoryStream(Encoding.UTF8.GetBytes("<a/>")),
                false, null, Relationship.DATA, "Data."));
    }

    [Fact]
    public void TheProducerIsTheVersionOfTheLibrary() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        new Page(pdf, Letter.PORTRAIT);
        pdf.Complete();
        Assert.Contains("/Producer " + TextString("PDFjet v9.0.2"), TestSupport.Latin1(stream.ToArray()));
    }

    [Fact]
    public void ANegativeNumberHasAnASCIIMinusInEveryCulture() {
        CultureInfo culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            MemoryStream stream = new MemoryStream();
            PDF pdf = new PDF(stream);
            int start = (int) stream.Length;
            pdf.Append(-3);
            Assert.Equal("-3", TestSupport.Latin1(stream.ToArray()).Substring(start));
        } finally {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void AnEndStructElementWithoutABeginIsRecorded() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Assert.Equal("EndStructElement was called without a matching BeginStructElement.",
                Assert.Throws<InvalidOperationException>(() => page.EndStructElement()).Message);
        Assert.Equal("The PDF was not completed because of an earlier error: "
                + "EndStructElement was called without a matching BeginStructElement.",
                Assert.Throws<InvalidOperationException>(() => pdf.Complete()).Message);
    }

    [Fact]
    public void TheHexadecimalOfATextIsItsUTF16() {
        Assert.Equal("FEFF0041007A00E9D83DDE00", Page.ToUTF16Hex("Azé😀"));
        Assert.Equal("00ff7f10", Util.ToHexString(new byte[] {0, 0xFF, 0x7F, 0x10}));
    }

    [Fact]
    public void TheFontFileRefersToItsMetadataWhoseNoticeIsEscaped() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_A_2B);
        Font font = new Font(pdf, TestSupport.RepoPath("fonts/NotoSansJP/NotoSansJP-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Hello").SetLocation(50f, 50f).DrawOn(page);
        pdf.Complete();
        string raw = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Matches(@"\d+ 0 obj\n<<\n/Filter /FlateDecode\n/Length1 \d+\n/Metadata \d+ 0 R\n/Length \d+\n>>\nstream\n", raw);
        Assert.Contains("<xmpRights:UsageTerms>", raw);
        Assert.Contains(" &amp; ", raw);
    }
}
}
