/*
 * DonutChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class DonutChartTest {
    private static DonutChart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new DonutChart(font, font).SetLocation(100f, 100f).SetRadii(100f, 50f);
    }

    [Fact]
    public void AChartWithoutValuesDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf).AddSlice(new Slice(0f, Color.red, "none"));
        TestSupport.AssertXY(300f, 300f, chart.DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void SlicesArePercentagesOfTheSumOfTheValues() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf);
        chart.AddSlice(new Slice(1f, Color.red, "a"));
        chart.AddSlice(new Slice(1f, Color.green, "b"));
        chart.AddSlice(new Slice(2f, Color.blue, "c"));
        TestSupport.AssertXY(300f, 300f, chart.DrawOn(page));
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("25%"), content);
        Assert.Contains(TestSupport.Hex("50%"), content);
        Assert.DoesNotContain(TestSupport.Hex("100%"), content);
    }

    [Fact]
    public void APieChartHasAnInnerRadiusOfZero() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf).SetRadii(100f, 0f);
        chart.AddSlice(new Slice(3f, Color.red, "a")).AddSlice(new Slice(1f, Color.blue, "b"));
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("75%"), content);
        Assert.Contains("200 592 l", content);   // the center, in PDF coordinates
    }
}
}
