/*
 * DrawableTest.cs
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
/// Every IDrawable measures itself with DrawOn(null): it draws nothing, returns
/// the corner that drawing returns, and does not change what drawing draws.
/// </summary>
public class DrawableTest {
    internal static List<KeyValuePair<string, Func<PDF, Font, IDrawable>>> Drawables() {
        var m = new List<KeyValuePair<string, Func<PDF, Font, IDrawable>>>();
        void Add(string name, Func<PDF, Font, IDrawable> make) {
            m.Add(new KeyValuePair<string, Func<PDF, Font, IDrawable>>(name, make));
        }
        Add("TextBlock", (pdf, font) => new TextBlock(font, "Hello world, this is a text block that wraps.").SetWidth(100f));
        Add("TextColumn", (pdf, font) => new TextColumn().SetWidth(120f)
                .AddParagraph(new Paragraph(new TextLine(font, "Hello column text that wraps around here"))));
        Add("TextFrame", (pdf, font) => new TextFrame(font, new List<string> {"Hello frame text"}).SetWidth(120f).SetHeight(200f));
        Add("TextLine", (pdf, font) => new TextLine(font, "Hello line"));
        Add("CompositeTextLine", (pdf, font) => new CompositeTextLine(0f, 0f).AddComponent(new TextLine(font, "Composite")));
        Add("Title", (pdf, font) => new Title(font, "Title", 0f, 0f));
        Add("Image", (pdf, font) => new Image(pdf, TestSupport.Open("images/up-arrow.png")));
        Add("SVGImage", (pdf, font) => new SVGImage(TestSupport.Open("images/svg/arrow_forward_FILL0_wght400_GRAD0_opsz48.svg")));
        Add("Barcode", (pdf, font) => new Barcode(Barcode.CODE_128, "Hello"));
        Add("QRCode", (pdf, font) => new QRCode("Hello", ErrorCorrectionLevel.M));
        Add("PDF417", (pdf, font) => new PDF417("Hello"));
        Add("DataMatrix", (pdf, font) => new DataMatrix("Hello"));
        Add("Chart", (pdf, font) => {
            Chart chart = new Chart(font, font);
            chart.SetSize(200f, 150f);
            chart.AddSeries("s").AddPoint(0f, 0f).AddPoint(1f, 2f);
            return chart;
        });
        Add("BarChart", (pdf, font) => new BarChart(font, font).SetSize(200f, 150f).AddSeries("s", new float[] {1f, 2f, 3f}));
        Add("DonutChart", (pdf, font) => new DonutChart(font, font).SetRadii(50f, 20f)
                .AddSlice(new Slice(1f, Color.red, "a")).AddSlice(new Slice(2f, Color.green, "b")));
        Add("Table", (pdf, font) => {
            List<List<Cell>> data = new List<List<Cell>>();
            for (int r = 0; r < 3; r++) {
                List<Cell> row = new List<Cell>();
                for (int c = 0; c < 2; c++) {
                    row.Add(new Cell(font, "r" + r + "c" + c));
                }
                data.Add(row);
            }
            return new Table().SetTableData(data, 1);
        });
        Add("Rect", (pdf, font) => new Rect().SetSize(50f, 30f).SetBorderColor(Color.black));
        Add("Container", (pdf, font) => new Container(80f, 40f).Add(new Rect().SetSize(80f, 40f).SetBorderColor(Color.black)));
        Add("CalendarMonth", (pdf, font) => new CalendarMonth(font, font, 2026, 9));
        Add("Form", (pdf, font) => new Form(new List<Field> {new Field(0f, "Name", "John")}).SetLabelFont(font).SetValueFont(font));
        Add("CheckBox", (pdf, font) => new CheckBox(font, "Check"));
        Add("RadioButton", (pdf, font) => new RadioButton(font, "Radio"));
        Add("Line", (pdf, font) => new Line(0f, 0f, 50f, 20f));
        Add("Arc", (pdf, font) => new Arc().SetRadius(20f).SetSweep(90f));
        Add("Point", (pdf, font) => new Point(0f, 0f).SetRadius(5f));
        Add("Path", (pdf, font) => new Path().Add(new Point(0f, 0f)).Add(new Point(30f, 20f)));
        Add("Stamp", (pdf, font) => {
            Stamp stamp = new Stamp(pdf).SetSize(50f, 30f);
            stamp.Complete();
            return stamp;
        });
        Add("SquareAnnotation", (pdf, font) => new SquareAnnotation().SetSize(30f, 20f));
        Add("FileAttachment", (pdf, font) => new FileAttachment(new EmbeddedFile(
                pdf, "hello.txt", new MemoryStream(Encoding.UTF8.GetBytes("Hello")), false)));
        return m;
    }

    private static float[] Draw(Func<PDF, Font, IDrawable> make, bool measureFirst, StringBuilder content) {
        PDF pdf = TestSupport.NewPDF();
        IDrawable drawable = make(pdf, TestSupport.Helvetica(pdf));
        drawable.SetLocation(40f, 60f);
        if (measureFirst) {
            drawable.DrawOn(null);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] xy = drawable.DrawOn(page);
        content.Append(TestSupport.Content(page));
        return xy;
    }

    [Fact]
    public void DrawOnNullMeasuresWithoutDrawing() {
        List<string> failures = new List<string>();
        foreach (var entry in Drawables()) {
            string name = entry.Key;
            try {
                PDF pdf = TestSupport.NewPDF();
                IDrawable drawable = entry.Value(pdf, TestSupport.Helvetica(pdf));
                drawable.SetLocation(40f, 60f);
                float[] measured = drawable.DrawOn(null);

                StringBuilder plain = new StringBuilder();
                float[] drawn = Draw(entry.Value, false, plain);
                StringBuilder afterMeasuring = new StringBuilder();
                float[] drawnAfterMeasuring = Draw(entry.Value, true, afterMeasuring);

                if (Math.Abs(measured[0] - drawn[0]) > TestSupport.DELTA
                        || Math.Abs(measured[1] - drawn[1]) > TestSupport.DELTA) {
                    failures.Add(name + ": measured " + measured[0] + ", " + measured[1] + ", drawn " + drawn[0] + ", " + drawn[1]);
                }
                if (plain.ToString() != afterMeasuring.ToString()
                        || drawn[0] != drawnAfterMeasuring[0] || drawn[1] != drawnAfterMeasuring[1]) {
                    failures.Add(name + ": measuring first changes what is drawn");
                }
            } catch (Exception e) {
                failures.Add(name + ": " + e.GetType().Name + " " + e.Message);
            }
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void TablesAndTextColumnsReturnTheirRightEdge() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Table table = (Table) Drawables().Find(e => e.Key == "Table").Value(pdf, font);
        table.SetLocation(40f, 60f);
        Assert.Equal(40f + table.GetWidth(), table.DrawOn(new Page(pdf, Letter.PORTRAIT))[0], 2);
        IDrawable column = Drawables().Find(e => e.Key == "TextColumn").Value(pdf, font).SetLocation(40f, 60f);
        Assert.Equal(160f, column.DrawOn(new Page(pdf, Letter.PORTRAIT))[0], 2);
    }
    [Fact]
    public void AContainerDrawsItsAnnotationsInTheSamePlaceEveryTime() {
        // The container moved the corners of an annotation by its own
        // location on every drawing, so the annotation of the second page
        // ended up that far from what the container drew there.
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Container container = new Container(200f, 100f).SetLocation(50f, 60f);
        SquareAnnotation square = new SquareAnnotation();
        square.SetLocation(10f, 10f);
        square.SetSize(80f, 40f);
        container.Add(square);
        for (int i = 0; i < 3; i++) {
            container.DrawOn(new Page(pdf, Letter.PORTRAIT));
        }
        pdf.Complete();
        List<string> rects = new List<string>();
        foreach (Match m in Regex.Matches(TestSupport.Latin1(stream.ToArray()),
                @"/Rect \[([-0-9. ]+)\]")) {
            rects.Add(m.Groups[1].Value.Trim());
        }
        Assert.Equal(3, rects.Count);
        Assert.Equal(rects[0], rects[1]);
        Assert.Equal(rects[0], rects[2]);
    }
    [Fact]
    public void EveryDrawableDrawsTheSameThingEveryTime() {
        // A drawable is drawn where it is put, however often it is drawn: a
        // header, a watermark or a logo goes on every page of a document. The
        // two that hold their place in a text are named below.
        List<string> failures = new List<string>();
        foreach (var entry in Drawables()) {
            string name = entry.Key;
            PDF pdf = TestSupport.NewPDF();
            IDrawable drawable = entry.Value(pdf, TestSupport.Helvetica(pdf));
            drawable.SetLocation(40f, 60f);
            Page page1 = new Page(pdf, Letter.PORTRAIT);
            drawable.DrawOn(page1);
            string first = TestSupport.Content(page1);
            Page page2 = new Page(pdf, Letter.PORTRAIT);
            drawable.DrawOn(page2);
            bool same = first == TestSupport.Content(page2);
            // A Table and a TextFrame draw what is left of their rows and
            // their text, which is what makes them flow from page to page.
            bool flows = name == "TextFrame" || name == "Table";
            if (same == flows) {
                failures.Add(name + (same ? " draws the same thing twice" : " draws something else"));
            }
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
    [Fact]
    public void AChartLeavesThePageWithThePenItFoundOnIt() {
        // The charts set the pen to their own and then to the default of a
        // page, so a line drawn after one came out in the default pen rather
        // than the pen the caller had set. A Stamp and a CalendarMonth keep
        // the pen of the caller; the charts do now too.
        foreach (string name in new string[] {"Chart", "BarChart", "DonutChart", "Stamp"}) {
            foreach (var entry in Drawables()) {
                if (entry.Key != name) {
                    continue;
                }
                PDF pdf = TestSupport.NewPDF();
                Page page = new Page(pdf, Letter.PORTRAIT);
                page.SetPenColor(Color.red);
                page.SetPenWidth(3f);
                IDrawable drawable = entry.Value(pdf, TestSupport.Helvetica(pdf));
                drawable.SetLocation(300f, 400f);
                drawable.DrawOn(page);
                int drawn = page.GetContent().Length;
                // Setting the same pen again writes nothing when it is still set.
                page.SetPenColor(Color.red);
                page.SetPenWidth(3f);
                Assert.True(drawn == page.GetContent().Length,
                        name + " left the page with another pen");
            }
        }
    }
}
}
