/*
 * ChartRoomTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
// A chart too small for its plot is an error, where the chart drew nothing
// and said nothing (the owner, 4 November 2026: "we can create more space if
// the user tries to jam too much info"). The same chart with room is drawn.
public class ChartRoomTest {
    private static void NoRoom(string name, IDrawable chart) {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string message = Assert.Throws<ArgumentException>(() => chart.DrawOn(page)).Message;
        Assert.True(message.Contains(name) && message.Contains("has no room for its plot"), message);
    }

    [Fact]
    public void AChartWithoutRoomIsAnError() {
        PDF pdf = TestSupport.NewPDF();
        Font f = TestSupport.Helvetica(pdf);
        NoRoom("SimpleLineChart", new SimpleLineChart(f, f).SetTitle("Sales")
                .AddLine("a", new float[] {1f, 5f, 3f}).SetSize(40f, 30f));
        NoRoom("SimpleScatterChart", new SimpleScatterChart(f, f).SetTitle("Points")
                .Add(1f, 2f).Add(3f, 4f).SetSize(40f, 30f));
        NoRoom("BarChart", new BarChart(f, f).SetCategories("a", "b")
                .AddSeries("s", new float[] {1f, 2f}).SetSize(30f, 20f));
        NoRoom("SimpleBarChart", new SimpleBarChart(f, f).SetWidth(90f).Add("A long name", 1234567f));

        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(f, f).SetTitle("Sales").AddLine("a", new float[] {1f, 5f, 3f})
                .SetSize(300f, 200f).DrawOn(page);
        new SimpleScatterChart(f, f).SetTitle("Points").Add(1f, 2f).Add(3f, 4f).SetSize(300f, 200f).DrawOn(page);
        new BarChart(f, f).SetCategories("a", "b").AddSeries("s", new float[] {1f, 2f})
                .SetSize(300f, 200f).DrawOn(page);
        new SimpleBarChart(f, f).SetWidth(300f).Add("A long name", 1234567f).DrawOn(page);
    }
}
}
