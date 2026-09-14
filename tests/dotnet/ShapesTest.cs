/*
 * ShapesTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
/// <summary>Line, Rect, Arc, Path, RadioButton, CheckBox and CalendarMonth locations and corners.</summary>
public class ShapesTest {
    private static Page NewPage() {
        return new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
    }

    [Fact]
    public void LineSetLocationMovesTheWholeLine() {
        Line line = new Line(10f, 10f, 50f, 30f).SetLocation(100f, 100f);
        Assert.Equal(100f, line.GetStartPoint().GetX());
        Assert.Equal(100f, line.GetStartPoint().GetY());
        Assert.Equal(140f, line.GetEndPoint().GetX());
        Assert.Equal(120f, line.GetEndPoint().GetY());
        TestSupport.AssertXY(140f, 120f, line.DrawOn(NewPage()));
    }

    [Fact]
    public void RectScaleByKeepsTheLocation() {
        TestSupport.AssertXY(70f, 100f, new Rect(10f, 20f, 30f, 40f).ScaleBy(2f).DrawOn(NewPage()));
    }

    [Fact]
    public void ArcDrawOnReturnsTheBottomRightCornerOfItsCircle() {
        Arc arc = new Arc().SetLocation(100f, 100f).SetRadius(20f).SetStartAngle(0f).SetSweepDegreesCW(90f);
        TestSupport.AssertXY(120f, 120f, arc.DrawOn(NewPage()));
    }

    [Fact]
    public void PathSetLocationSetsTheOffsetInsteadOfAddingToIt() {
        Path path = new Path().Add(new Point(0f, 0f)).Add(new Point(10f, 20f));
        path.SetLocation(5f, 5f);
        path.SetLocation(5f, 5f);
        TestSupport.AssertXY(15f, 25f, path.DrawOn(NewPage()));
    }

    [Fact]
    public void RadioButtonAndCheckBoxCorners() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        RadioButton radio = new RadioButton(font, "rb");
        radio.SetLocation(1f, 2f);
        TestSupport.AssertXY(45.184f, 15.872f, radio.DrawOn(page));
        TestSupport.AssertXY(47.188f, 15.872f, new CheckBox(font, "cb").SetLocation(1f, 2f).DrawOn(page));
    }

    [Fact]
    public void CalendarMonthStartsAtTheOriginWithCellsFromTheDayNames() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        // February and March 2026 start on a Sunday.
        TestSupport.AssertXY(252f, 252f, new CalendarMonth(font, font, 2026, 2).DrawOn(NewPage()));
        TestSupport.AssertXY(252f, 252f, new CalendarMonth(font, font, 2026, 3).DrawOn(NewPage()));
    }

    [Fact]
    public void ColorsAreSetAsAnIntOrAsAnArray() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(10f, 10f, 50f, 10f).SetStrokeColor(Color.red).DrawOn(page);
        new Line(10f, 20f, 50f, 20f).SetStrokeColor(new float[] {0f, 0f, 1f}).DrawOn(page);
        Path path = new Path();
        path.Add(new Point(10f, 30f));
        path.Add(new Point(50f, 30f));
        path.SetStrokeColor(new float[] {0f, 1f, 0f}).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("1 0 0 RG", content);
        Assert.Contains("0 0 1 RG", content);
        Assert.Contains("0 1 0 RG", content);
    }
}
}
