/*
 * ReviewPageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The drawing of a page: shapes, text lines, containers and annotations, as
/// the review of the page drawing found them.
/// </summary>
public class ReviewPageTest {
    private const float DELTA = 0.01f;

    private static Page TaggedPage() {
        return new Page(new PDF(new MemoryStream(), Compliance.PDF_UA_1), Letter.PORTRAIT);
    }

    private static Page NewPage() {
        return new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
    }

    private static int Count(string text, string part) {
        int n = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i != -1;
                i = text.IndexOf(part, i + part.Length, StringComparison.Ordinal)) {
            n++;
        }
        return n;
    }

    [Fact]
    public void APolygonInATurnedContainerIsTurned() {
        Page page = NewPage();
        float[] vertices = new float[] {0f, 0f, 20f, 0f, 0f, 10f};
        PolygonAnnotation polygon = new PolygonAnnotation();
        polygon.SetLocation(10f, 10f);
        polygon.SetVertices(vertices);
        Container container = new Container(100f, 100f);
        container.SetLocation(100f, 100f);
        container.SetRotation(90);
        container.Add(polygon);
        // Drawn twice, the container turns the polygon the same both times
        container.DrawOn(page);
        container.DrawOn(page);
        foreach (Annotation annot in page.annots) {
            // The first vertex is 40 left of and above the center, 150 and
            // 150, and a quarter turn clockwise puts it 40 right of it and above it.
            TestSupport.AssertNear(190f, annot.x1, DELTA);
            TestSupport.AssertNear(792f - 110f, annot.y1, DELTA);
            float[] want = new float[] {0f, 0f, 0f, 20f, -10f, 0f};
            for (int i = 0; i < want.Length; i++) {
                TestSupport.AssertNear(want[i], annot.vertices[i], DELTA);
            }
        }
        Assert.Equal(20f, vertices[2]);
        Assert.Equal(0f, vertices[3]);
    }

    [Fact]
    public void APointTwiceInAPathIsOffsetOnce() {
        Page page = NewPage();
        Point point = new Point(10f, 10f);
        Path path = new Path();
        path.Add(point);
        path.Add(new Point(50f, 10f));
        path.Add(point);
        path.SetLocation(100f, 0f);
        float[] xy = path.DrawOn(page);
        Assert.Contains("110 782 m\n150 782 l\n110 782 l\n", TestSupport.Content(page));
        Assert.Equal(10f, point.x);
        Assert.Equal(10f, point.y);
        TestSupport.AssertXY(150f, 10f, xy);
    }

    [Fact]
    public void APathThatEndsOnAControlPointIsRefused() {
        Page page = NewPage();
        List<Point> path = new List<Point> {
                new Point(10f, 10f),
                new Point(20f, 20f, Point.CONTROL_POINT_C),
                new Point(30f, 20f, Point.CONTROL_POINT_C)};
        string message = Assert.Throws<ArgumentException>(() => page.DrawPath(path, PathOperator.STROKE)).Message;
        Assert.Equal(Page.PATH_ENDS_ON_CONTROL_POINT, message);
        Assert.Equal("", TestSupport.Content(page));

        Stamp stamp = new Stamp(TestSupport.NewPDF()).SetSize(50f, 50f);
        message = Assert.Throws<ArgumentException>(() => stamp.DrawPath(path, PathOperator.STROKE)).Message;
        Assert.Equal(Page.PATH_ENDS_ON_CONTROL_POINT, message);
    }

    [Fact]
    public void TheLinkOfATurnedTextLineCoversTheText() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "Turned");
        line.SetTextRotation(90);
        line.SetURIAction("https://pdfjet.com");
        line.SetLocation(100f, 200f);
        line.DrawOn(page);
        Annotation annot = page.annots[0];
        // A quarter turn clockwise runs the text down the page from its location.
        TestSupport.AssertNear(100f - font.GetDescent(12f), annot.x1, DELTA);
        TestSupport.AssertNear(100f + font.GetAscent(12f), annot.x2, DELTA);
        TestSupport.AssertNear(792f - 200f, annot.y1, DELTA);
        TestSupport.AssertNear(792f - (200f + font.StringWidth(12f, "Turned")), annot.y2, DELTA);
    }

    [Fact]
    public void ASuperscriptIsRaisedOnce() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        composite.SetFontSize(12f);
        composite.AddFormula(font, "x^2");
        composite.DrawOn(page);
        TextLine two = composite.GetTextLine(1);
        // Raised by the superscript position of the base font size, 0.35 of 12
        TestSupport.AssertNear(792f - (100f - 4.2f), TestSupport.PositionOf(TestSupport.Content(page), "2")[1], DELTA);
        // Measured where it is drawn, the superscript reaches no higher than the x
        float top = Math.Min(100f - font.GetAscent(12f), 100f - 4.2f - font.GetAscent(two.GetFontSize()));
        TestSupport.AssertNear(top, composite.GetMinMaxY()[0], DELTA);
        TestSupport.AssertNear(100f - top, composite.GetAscent(), DELTA);
        TestSupport.AssertNear(composite.GetWidth() + 100f, composite.DrawOn(null)[0], DELTA);
        TestSupport.AssertNear(100f - 4.2f, two.DrawOn(null)[1], DELTA);
    }

    [Fact]
    public void AHighlightedWordKeepsItsCombiningMarks() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "the cafe\u0301 is open");
        Dictionary<String, int> colors = new Dictionary<String, int>();
        colors["cafe\u0301"] = Color.red;
        line.SetHighlightColors(colors);
        line.SetLocation(100f, 100f);
        line.DrawOn(page);
        // The mark is drawn with its letter, in the color of the word; a core
        // font has no mark, and draws a space for it.
        Assert.Equal("1 0 0 rg", TestSupport.FillColorBefore(TestSupport.Content(page), "cafe "));
    }

    [Fact]
    public void AShapeWithNoDescriptionIsAnArtifact() {
        Page page = TaggedPage();
        new Line(10f, 10f, 100f, 10f).DrawOn(page);
        Arc arc = new Arc();
        arc.SetLocation(50f, 50f);
        arc.SetRadius(10f);
        arc.SetSweep(90f);
        arc.DrawOn(page);
        new CheckBox(TestSupport.Helvetica(page.pdf), "").SetLocation(10f, 100f).DrawOn(page);
        Stamp stamp = new Stamp(page.pdf).SetSize(20f, 20f);
        stamp.DrawRect(0f, 0f, 20f, 20f);
        stamp.Complete();
        stamp.SetLocation(200f, 200f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain("/P <<", content);
        Assert.Equal(4, Count(content, "/Artifact BMC"));

        // Described, a line is read
        Page page2 = TaggedPage();
        new Line(10f, 10f, 100f, 10f).SetAltDescription("A rule").DrawOn(page2);
        Assert.Contains("/P <</MCID 0>>", TestSupport.Content(page2));
    }

    [Fact]
    public void ARunningFooterAndAWatermarkAreArtifacts() {
        Page page = TaggedPage();
        Font font = TestSupport.Helvetica(page.pdf);
        page.AddFooter(new TextLine(font, "Page 1"));
        page.AddHeader(new TextLine(font, "Report"));
        page.AddWatermark(font, "DRAFT");
        string content = TestSupport.Content(page);
        foreach (string subtype in new string[] {"Footer", "Header", "Watermark"}) {
            Assert.Contains("/Artifact <</Type /Pagination /Subtype /" + subtype + ">> BDC\n", content);
        }
        Assert.DoesNotContain("/P <<", content);
    }

    [Fact]
    public void ARotationOfOneDegreeIsWrittenAsOne() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "Turned");
        line.SetTextRotation(1);
        line.SetLocation(100f, 100f);
        line.DrawOn(page);
        Assert.Contains("0.99985 -0.01745 0.01745 0.99985 100 692 Tm\n", TestSupport.Content(page));

        Page page2 = NewPage();
        Container container = new Container(10f, 10f);
        container.SetRotation(1);
        container.DrawOn(page2);
        Assert.Contains("0.99985 -0.01745 0.01745 0.99985 0 0 cm\n", TestSupport.Content(page2));
    }

    [Fact]
    public void PreciseNumbersHaveFiveDecimals() {
        Assert.Equal("0.01745", TestSupport.Latin1(FastFloat.ToPreciseByteArray((float) Math.Sin(Math.PI / 180))));
        Assert.Equal("0.99985", TestSupport.Latin1(FastFloat.ToPreciseByteArray((float) Math.Cos(Math.PI / 180))));
        Assert.Equal("-0.70711", TestSupport.Latin1(FastFloat.ToPreciseByteArray((float) -Math.Sqrt(0.5))));
        Assert.Equal("1", TestSupport.Latin1(FastFloat.ToPreciseByteArray(1f)));
        Assert.Equal("-1", TestSupport.Latin1(FastFloat.ToPreciseByteArray(-1f)));
        Assert.Equal("0.5", TestSupport.Latin1(FastFloat.ToPreciseByteArray(0.5f)));
        Assert.Equal("0.0001", TestSupport.Latin1(FastFloat.ToPreciseByteArray(0.0001f)));
        Assert.Equal("0", TestSupport.Latin1(FastFloat.ToPreciseByteArray((float) Math.Cos(Math.PI / 2))));
        Assert.Equal("0", TestSupport.Latin1(FastFloat.ToPreciseByteArray(-0f)));
        Assert.Equal("0", TestSupport.Latin1(FastFloat.ToPreciseByteArray(-0.000004f)));
        Assert.Equal("12.25", TestSupport.Latin1(FastFloat.ToPreciseByteArray(12.25f)));
        Assert.Equal("0", TestSupport.Latin1(FastFloat.ToPreciseByteArray(float.NaN)));
    }

    [Fact]
    public void TheSizeOfAnAnnotationFollowsItsLocation() {
        Page page = NewPage();
        SquareAnnotation square = new SquareAnnotation();
        square.SetSize(60f, 30f);
        square.SetLocation(100f, 200f);
        TestSupport.AssertXY(160f, 230f, square.DrawOn(page));
        Annotation annot = page.annots[0];
        TestSupport.AssertNear(160f, annot.x2, DELTA);
        TestSupport.AssertNear(792f - 230f, annot.y2, DELTA);
    }

    [Fact]
    public void ACheckBoxIsTheSizeOfItsFont() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        CheckBox checkBox = new CheckBox(font, "Yes");
        checkBox.SetFontSize(24f);
        checkBox.SetLocation(100f, 100f);
        float[] xy = checkBox.DrawOn(null);
        TestSupport.AssertNear(100f + 3f*font.GetAscent(24f) + font.StringWidth(24f, "Yes"), xy[0], DELTA);
    }

    [Fact]
    public void ShapesLeaveThePenAsTheyFoundIt() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        page.SetPenWidth(2f);
        page.SetPenColor(Color.red);
        Path path = new Path();
        path.Add(new Point(10f, 10f));
        path.Add(new Point(20f, 20f));
        path.SetStrokeWidth(5f);
        path.DrawOn(page);
        TextLine line = new TextLine(font, "Underlined");
        line.SetUnderline(true);
        line.SetStrikeout(true);
        line.SetLocation(10f, 50f);
        line.DrawOn(page);
        new CheckBox(font, "Check").SetLocation(10f, 80f).DrawOn(page);
        new RadioButton(font, "Radio").SetLocation(10f, 110f).DrawOn(page);
        Assert.Equal(2f, page.GetPenWidth());
        TestSupport.AssertRGB(1f, 0f, 0f, page.GetPenColor());
        string content = TestSupport.Content(page);
        Assert.Equal(4, Count(content, "q\n"));
        Assert.Equal(4, Count(content, "Q\n"));
    }

    [Fact]
    public void AnArcOfNoSweepOrOfTooMuchIsBounded() {
        Page page = NewPage();
        Arc arc = new Arc();
        arc.SetLocation(50f, 50f);
        arc.SetRadius(10f);
        arc.SetSweep(0f);
        arc.DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));
        // A sweep of a billion degrees is a full turn, four curves
        arc.SetSweep(1e9f);
        arc.DrawOn(page);
        Assert.Equal(4, Count(TestSupport.Content(page), " c\n"));

        Page page2 = NewPage();
        foreach (float sweep in new float[] {float.NaN, float.PositiveInfinity}) {
            arc.SetSweep(sweep);
            string message = Assert.Throws<ArgumentException>(() => arc.DrawOn(page2)).Message;
            Assert.Equal("The sweep of an arc must be a finite number of degrees.", message);
            Assert.Throws<ArgumentException>(() => page2.AddArcToPath(50f, 50f, 10f, 10f, 0f, sweep));
        }
        Assert.Equal("", TestSupport.Content(page2));
    }

    [Fact]
    public void TheBoundingBoxAfterAFigureIsNotItsOwn() {
        Page page = TaggedPage();
        page.AddBDC(StructElem.FIGURE, null, null, "A figure");
        page.SetFigureBoundingBox(10f, 10f, 20f, 20f);
        page.AddEMC();
        page.SetFigureBoundingBox(50f, 50f, 5f, 5f);
        Assert.Equal("<</O /Layout /BBox [10 762 30 782]>>", page.structures[0].attributes);
    }

    [Fact]
    public void AnEmptyTextLineAddsItsDestination() {
        Page page = NewPage();
        TextLine line = new TextLine(TestSupport.Helvetica(page.pdf), "");
        line.SetDestination("top");
        line.SetLocation(10f, 100f);
        line.DrawOn(page);
        Assert.Single(page.destinations);
        Assert.Equal("top", page.destinations[0].name);
    }

    [Fact]
    public void AFormWithoutItsFontsIsRefused() {
        Page page = NewPage();
        List<Field> fields = new List<Field>();
        fields.Add(new Field(0f, "Name", "Value"));
        Form form = new Form(fields);
        form.SetLocation(10f, 10f);
        string message = Assert.Throws<InvalidOperationException>(() => form.DrawOn(page)).Message;
        Assert.Equal("A form needs a label font and a value font: SetLabelFont and SetValueFont.", message);
    }

    [Fact]
    public void TheLinksInNestedContainersAreWhereTheirTextIs() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "Link");
        line.SetURIAction("https://pdfjet.com");
        line.SetLocation(1f, 20f);
        Container inner = new Container(50f, 50f);
        inner.SetLocation(5f, 5f);
        inner.Add(line);
        Container middle = new Container(100f, 100f);
        middle.SetLocation(10f, 10f);
        middle.Add(inner);
        Container outer = new Container(200f, 200f);
        outer.SetLocation(100f, 100f);
        outer.Add(middle);
        outer.DrawOn(page);
        Annotation annot = page.annots[0];
        TestSupport.AssertNear(116f, annot.x1, DELTA);
        TestSupport.AssertNear(792f - (135f - font.GetAscent(12f)), annot.y1, DELTA);

        // A figure is where it is drawn
        Page page2 = TaggedPage();
        Barcode barcode = new Barcode(Barcode.CODE_128, "12345");
        barcode.SetAltDescription("12345");
        barcode.SetLocation(110f, 110f);
        barcode.DrawOn(page2);
        Page page3 = TaggedPage();
        barcode = new Barcode(Barcode.CODE_128, "12345");
        barcode.SetAltDescription("12345");
        barcode.SetLocation(10f, 10f);
        Container container = new Container(100f, 100f);
        container.Add(barcode);
        container.SetLocation(100f, 100f);
        container.DrawOn(page3);
        Assert.Equal(page2.structures[0].attributes, page3.structures[0].attributes);

        // Turned a quarter, the link of a text line is turned with it
        Page page4 = new Page(page.pdf, Letter.PORTRAIT);
        TextLine link = new TextLine(font, "Link");
        link.SetURIAction("https://pdfjet.com");
        link.SetLocation(0f, 50f);
        Container turned = new Container(100f, 100f);
        turned.SetLocation(100f, 100f);
        turned.SetRotation(90);
        turned.Add(link);
        turned.DrawOn(page4);
        annot = page4.annots[0];
        // The text runs down the page, from 50 left of the center to 50 right of it
        TestSupport.AssertNear(150f - font.GetDescent(12f), annot.x1, DELTA);
        TestSupport.AssertNear(792f - 100f, annot.y1, DELTA);
        TestSupport.AssertNear(150f + font.GetAscent(12f), annot.x2, DELTA);
        TestSupport.AssertNear(792f - (100f + font.StringWidth(12f, "Link")), annot.y2, DELTA);
    }

    [Fact]
    public void ADashPatternWithAVerticalTabIsRefused() {
        Page page = NewPage();
        string message = Assert.Throws<ArgumentException>(() => page.SetStrokeDashPattern("[3\v3] 0")).Message;
        Assert.Equal("The dash pattern \"[3\v3] 0\" is not an array of non-negative numbers, "
                + "not all zero, followed by a phase, such as \"[3 3] 0\".", message);
    }

    [Fact]
    public void TheSpacesOfUnicodeAreSpaces() {
        Page page = TaggedPage();
        Font font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "\u3000Annual\u3000 report\u00a0");
        line.SetStructureType(StructElem.H1);
        line.SetLocation(10f, 50f);
        line.DrawOn(page);
        Assert.Equal("Annual report", page.pdf.headings[0].title);
        string message = Assert.Throws<InvalidOperationException>(
                () => page.AddBDC(StructElem.FIGURE, null, null, "\u3000")).Message;
        Assert.Equal("A figure of a tagged document, PDF/UA or PDF/A of level A, needs an alternative description.",
                message);
    }

    [Fact]
    public void AnEmptyURIIsNoLink() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "Text");
        line.SetURIAction("");
        line.SetGoToAction("");
        line.SetLocation(10f, 50f);
        line.DrawOn(page);
        TextBlock block = new TextBlock(font, "Text");
        block.SetURIAction("");
        block.SetLocation(10f, 100f);
        block.DrawOn(page);
        Assert.Empty(page.annots);
    }

    [Fact]
    public void AKeywordIsLowerCasedWhateverTheCulture() {
        CultureInfo culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try {
            Page page = NewPage();
            Font font = TestSupport.Helvetica(page.pdf);
            TextLine line = new TextLine(font, "LINK");
            Dictionary<String, int> colors = new Dictionary<String, int>();
            colors["link"] = Color.red;
            line.SetHighlightColors(colors);
            line.SetLocation(10f, 50f);
            line.DrawOn(page);
            Assert.Equal("1 0 0 rg", TestSupport.FillColorBefore(TestSupport.Content(page), "LINK"));
        } finally {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void AKeywordIsMatchedByItsCodePoints() {
        // A keyword of an e with an acute accent is not the same word as one
        // of an e and a combining accent.
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        TextLine line = new TextLine(font, "the cafe\u0301 is open");
        Dictionary<String, int> colors = new Dictionary<String, int>();
        colors["caf\u00e9"] = Color.red;
        line.SetHighlightColors(colors);
        line.SetLocation(100f, 100f);
        line.DrawOn(page);
        Assert.Equal("0 0 0 rg", TestSupport.FillColorBefore(TestSupport.Content(page), "cafe "));

        // Nor is a description of the one the text of the other
        Page page2 = TaggedPage();
        TextLine described = new TextLine(TestSupport.Helvetica(page2.pdf), "cafe\u0301");
        described.SetAltDescription("caf\u00e9");
        described.SetLocation(100f, 100f);
        described.DrawOn(page2);
        Assert.Equal("caf\u00e9", page2.structures[0].altDescription);
    }

    [Fact]
    public void AShortColorOrTransformIsRefused() {
        Page page = NewPage();
        string message = Assert.Throws<ArgumentException>(() => page.SetPenColor(new float[] {1f, 0f})).Message;
        Assert.Equal(Page.RGB_COUNT, message);
        Page page2 = NewPage();
        message = Assert.Throws<ArgumentException>(() => page2.SetBrushColor(new float[] {1f})).Message;
        Assert.Equal(Page.RGB_COUNT, message);
        Page page3 = NewPage();
        message = Assert.Throws<ArgumentException>(() => page3.Transform(new float[] {1f, 0f, 0f})).Message;
        Assert.Equal(Page.TRANSFORM_COUNT, message);
        Assert.Equal("", TestSupport.Content(page3));
        // The most negative and the most positive angles are angles too
        Page page4 = NewPage();
        page4.SetTextRotation(int.MinValue);
        page4.SetTextRotation(int.MaxValue);
    }

    [Fact]
    public void ALoneSurrogateIsDrawnAsNotdef() {
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "a\uD800b").SetLocation(10f, 50f).DrawOn(page);
        // As Java draws it: the glyph of the font's .notdef, with the
        // surrogate as its actual text
        Assert.Contains("<0001> Tj\n/Span <</ActualText <FEFFD800>>> BDC\n<0000> Tj\nEMC\n<0003> Tj\n",
                TestSupport.Content(page));
    }
}
}   // End of namespace PDFjet.NET
