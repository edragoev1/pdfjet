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

    // The content of a rect 20 high with the corner radius, drawn on a page of its own.
    private static string RoundRect(float radius) {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Rect rect = new Rect(10f, 20f, 100f, 20f);
        rect.SetCornerRadius(radius);
        rect.SetBorderColor(Color.black);
        rect.DrawOn(page);
        return TestSupport.Content(page);
    }

    [Fact]
    public void RectACornerRadiusOverHalfTheShorterSideIsDrawnAsHalfOfIt() {
        // A radius of 30 is drawn as one of 10, a pill, rather than with the
        // sides turned back as crossed loops.
        Assert.Equal(RoundRect(10f), RoundRect(30f));
    }

    [Fact]
    public void ArcDrawOnReturnsTheBottomRightCornerOfItsCircle() {
        Arc arc = new Arc().SetLocation(100f, 100f).SetRadius(20f).SetStartAngle(0f).SetSweep(90f);
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
        Font font = TestSupport.Helvetica(page.pdf);
        RadioButton radio = new RadioButton(font, "rb");
        radio.SetLocation(1f, 2f);
        TestSupport.AssertXY(45.184f, 15.872f, radio.DrawOn(page));
        TestSupport.AssertXY(47.188f, 15.872f, new CheckBox(font, "cb").SetLocation(1f, 2f).DrawOn(page));
    }

    [Fact]
    public void CalendarMonthStartsAtTheOriginWithCellsFromTheDayNames() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        // February and March 2026 start on a Sunday.
        TestSupport.AssertXY(252f, 252f, new CalendarMonth(font, font, 2026, 2).DrawOn(page));
        TestSupport.AssertXY(252f, 252f, new CalendarMonth(font, font, 2026, 3).DrawOn(page));
    }

    [Fact]
    public void CalendarMonthStartsTheWeekOnTheFirstDayOfTheWeek() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        new CalendarMonth(font, font, 2026, 2).SetFirstDayOfWeek(System.DayOfWeek.Monday).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.IndexOf("<4D6F>") < content.IndexOf("<5375>"), "Mo before Su");
        // Sunday, February 1, ends the first week, and Monday, February 2, starts the second.
        Assert.True(content.Contains("230.66 744.83 Td\n[<31>] TJ"), "the 1st in the last column");
        Assert.True(content.Contains("14.66 708.83 Td\n[<32>] TJ"), "the 2nd in the first column");
    }

    [Fact]
    public void CalendarMonthLeavesThePenOfThePageAsItWas() {
        Page page = NewPage();
        Font font = TestSupport.Helvetica(page.pdf);
        new CalendarMonth(font, font, 2026, 2).DrawOn(page);
        int start = TestSupport.Content(page).Length;
        // The blue pen 1.25 wide of the circles is not the pen of the page.
        page.SetPenColor(Color.blue);
        page.SetPenWidth(1.25f);
        page.DrawLine(10f, 10f, 50f, 10f);
        string line = TestSupport.Content(page).Substring(start);
        Assert.True(line.Contains("0 0 1 RG\n") && line.Contains("1.25 w\n"), line);
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

    // An arc that starts at the end of a line finds its center and its start
    // angle with the precision of Go's port, double where Go computes in
    // double: in float, 4 of 2,000 random arcs were drawn a hundredth apart
    // (review of 11 October 2026, #25). The expected paths are Go's.
    [Fact]
    public void AnArcAtTheEndOfALineIsDrawnAsTheGoPortDrawsIt() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Arc().SetRadiusX(158.77583f).SetRadiusY(189.22208f)
                .SetStartAngle(-28.21344f).SetSweep(186.41852f)
                .SetStartPointToEndOf(new Line(2.7493358f, 91.162384f, 462.71942f, 1.1617184f))
                .SetStrokeColor(0x000000).SetLocation(487.1806f, 508.14288f).DrawOn(page);
        Assert.Contains("655.27 663.63 661.99 588.89 637.16 525.3 c\n", TestSupport.Content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        new Arc().SetRadiusX(151.92659f).SetRadiusY(46.18809f)
                .SetStartAngle(23.969238f).SetSweep(100.79004f)
                .SetStartPointToEndOf(new Line(21.53635f, 363.53027f, 226.3573f, 534.09174f))
                .SetStrokeColor(0x000000).SetLocation(7.1584225f, 76.66633f).DrawOn(page);
        Assert.Contains("271.85 238.26 m\n", TestSupport.Content(page));
    }

    // An Arc, and so an Ellipse, given a stroke width and no color is drawn a
    // black line of that width: it was drawn 0 wide, a hairline, whatever the
    // width; with no width it is 0 wide as before.
    [Fact]
    public void AnArcOfAWidthAndNoColorIsDrawnThatWide() {
        foreach (Arc arc in new Arc[] {new Arc(), new Ellipse()}) {
            PDF pdf = TestSupport.NewPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            arc.SetLocation(100f, 100f).SetRadius(40f).SetSweep(270f).SetStrokeWidth(4f).DrawOn(page);
            Assert.Contains("\n4 w\n", TestSupport.Content(page));
        }
        PDF pdf2 = TestSupport.NewPDF();
        Page page2 = new Page(pdf2, Letter.PORTRAIT);
        new Arc().SetLocation(100f, 100f).SetRadius(40f).SetSweep(90f).DrawOn(page2);
        Assert.Contains("\n0 w\n", TestSupport.Content(page2));
    }
}
}
