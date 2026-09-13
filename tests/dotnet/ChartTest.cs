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
        Assert.Contains(TestSupport.Hex("-5.00"), content);
        Assert.Contains(TestSupport.Hex("-1.00"), content);
        Assert.DoesNotContain("NaN", content);
    }

    [Fact]
    public void FlatDataIsDrawnWithoutNaN() {
        string content = Draw(Series(3f, 3f, 3f));
        Assert.DoesNotContain("NaN", content);
        Assert.Contains(TestSupport.Hex("3.00"), content);
        Assert.Contains(TestSupport.Hex("4.00"), content);
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
