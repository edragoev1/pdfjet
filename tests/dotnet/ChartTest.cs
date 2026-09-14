/*
 * ChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace PDFjet.NET {
public class ChartTest {
    private static List<List<Point>> Series(params float[] ys) {
        List<Point> points = new List<Point>();
        for (int i = 0; i < ys.Length; i++) {
            points.Add(new Point(i + 1f, ys[i]));
        }
        return new List<List<Point>> {points};
    }

    private static string Draw(List<List<Point>> data) {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        new Chart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f).SetData(data).DrawOn(page);
        return TestSupport.Content(page);
    }

    [Fact]
    public void AChartWithoutPointsDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        Chart chart = new Chart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f);
        chart.SetData(new List<List<Point>>());
        TestSupport.AssertXY(350f, 250f, chart.DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void AllNegativeDataGetsNegativeAxisLabels() {
        string content = Draw(Series(-5f, -2.5f, -1f));
        Assert.Contains(TestSupport.Hex("-5.0"), content);
        Assert.Contains(TestSupport.Hex("-1.0"), content);
        Assert.DoesNotContain("NaN", content);
    }

    [Fact]
    public void FlatDataIsDrawnWithoutNaN() {
        string content = Draw(Series(3f, 3f, 3f));
        Assert.DoesNotContain("NaN", content);
        Assert.Contains(TestSupport.Hex("3.0"), content);
        Assert.Contains(TestSupport.Hex("4.0"), content);
    }

    [Fact]
    public void WholeNumberStepsGetWholeNumberLabels() {
        string content = Draw(Series(10f, 60f, 35f));
        Assert.Contains(TestSupport.Hex("60"), content);
        Assert.DoesNotContain(TestSupport.Hex("60.00"), content);
    }

    [Fact]
    public void APathSeriesKeepsItsStrokeWidthAndWritesItsText() {
        Point p1 = new Point(1f, 2f).SetDrawPath(true).SetShape(Point.INVISIBLE);
        p1.SetStrokeWidth(20f).SetStrokeColor(Color.blue).SetText("label");
        Point p2 = new Point(3f, 2f).SetShape(Point.INVISIBLE);
        List<Point> path = new List<Point> {p1, p2};
        List<List<Point>> data = new List<List<Point>> {path};
        string content = Draw(data);
        Assert.Contains("20 w", content);
        Assert.Contains(TestSupport.Hex("label"), content);
    }

    [Fact]
    public void LabelsUseAPeriodWhateverTheDefaultLocale() {
        CultureInfo saved = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try {
            string content = Draw(Series(1f, 2f, 3f));
            Assert.Contains(TestSupport.Hex("1.25"), content);
            Assert.DoesNotContain(TestSupport.Hex("1,25"), content);
        } finally {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
}
