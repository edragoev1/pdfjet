/**
 * ShapesTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// Line, Rect, Arc, Path, RadioButton, CheckBox and CalendarMonth locations and corners.
@Suite struct ShapesTests {
    private func page() -> Page {
        return TestSupport.newPage()
    }

    @Test func lineSetLocationMovesTheWholeLine() {
        let line = Line(10, 10, 50, 30).setLocation(100, 100)
        #expect(line.getStartPoint().getX() == 100)
        #expect(line.getStartPoint().getY() == 100)
        #expect(line.getEndPoint().getX() == 140)
        #expect(line.getEndPoint().getY() == 120)
        TestSupport.expectXY(140, 120, line.drawOn(page()))
    }

    @Test func rectScaleByKeepsTheLocation() {
        TestSupport.expectXY(70, 100, Rect(10, 20, 30, 40).scaleBy(2).drawOn(page()))
    }

    /// The content of a rect 20 high with the corner radius, drawn on a page of its own.
    private func roundRect(_ radius: Float) -> String {
        let page = page()
        let rect = Rect(10, 20, 100, 20)
        rect.setCornerRadius(radius)
        rect.setBorderColor(Color.black)
        rect.drawOn(page)
        return TestSupport.content(page)
    }

    @Test func rectACornerRadiusOverHalfTheShorterSideIsDrawnAsHalfOfIt() {
        // A radius of 30 is drawn as one of 10, a pill, rather than with the
        // sides turned back as crossed loops.
        #expect(roundRect(30) == roundRect(10))
    }

    @Test func arcDrawOnReturnsTheBottomRightCornerOfItsCircle() {
        let arc = Arc().setLocation(100, 100).setRadius(20).setStartAngle(0).setSweep(90)
        TestSupport.expectXY(120, 120, arc.drawOn(page()))
    }

    @Test func pathSetLocationSetsTheOffsetInsteadOfAddingToIt() {
        let path = Path().add(Point(0, 0)).add(Point(10, 20))
        _ = path.setLocation(5, 5)
        _ = path.setLocation(5, 5)
        TestSupport.expectXY(15, 25, path.drawOn(page()))
    }

    @Test func radioButtonAndCheckBoxCorners() {
        // Java tests setLocation(double, double) here; Swift has the Float form only.
        let page = page()
        let font = TestSupport.helvetica(page.pdf)
        let radio = RadioButton(font, "rb")
        _ = radio.setLocation(1, 2)
        TestSupport.expectXY(45.184, 15.872, radio.drawOn(page))
        TestSupport.expectXY(47.188, 15.872, CheckBox(font, "cb").setLocation(1, 2).drawOn(page))
    }

    @Test func calendarMonthStartsAtTheOriginWithCellsFromTheDayNames() {
        let page = page()
        let font = TestSupport.helvetica(page.pdf)
        // February and March 2026 start on a Sunday.
        TestSupport.expectXY(252, 252, CalendarMonth(font, font, 2026, 2).drawOn(page))
        TestSupport.expectXY(252, 252, CalendarMonth(font, font, 2026, 3).drawOn(page))
    }

    @Test func calendarMonthStartsTheWeekOnTheFirstDayOfTheWeek() {
        let page = self.page()
        let font = TestSupport.helvetica(page.pdf)
        CalendarMonth(font, font, 2026, 2).setFirstDayOfWeek(2).drawOn(page)   // Monday
        let content = TestSupport.content(page)
        #expect(content.range(of: "<4D6F>")!.lowerBound < content.range(of: "<5375>")!.lowerBound, "Mo before Su")
        // Sunday, February 1, ends the first week, and Monday, February 2, starts the second.
        #expect(content.contains("230.66 744.83 Td\n[<31>] TJ"), "the 1st in the last column")
        #expect(content.contains("14.66 708.83 Td\n[<32>] TJ"), "the 2nd in the first column")
    }

    @Test func calendarMonthLeavesThePenOfThePageAsItWas() {
        let page = self.page()
        let font = TestSupport.helvetica(page.pdf)
        CalendarMonth(font, font, 2026, 2).drawOn(page)
        let start = TestSupport.content(page).utf8.count
        // The blue pen 1.25 wide of the circles is not the pen of the page.
        page.setPenColor(Color.blue)
        page.setPenWidth(1.25)
        page.drawLine(10, 10, 50, 10)
        let line = String(TestSupport.content(page).utf8.dropFirst(start))!
        #expect(line.contains("0 0 1 RG\n") && line.contains("1.25 w\n"), "\(line)")
    }

    @Test func colorsAreSetAsAnIntOrAsAnArray() {
        let page = self.page()
        Line(10, 10, 50, 10).setStrokeColor(Color.red).drawOn(page)
        Line(10, 20, 50, 20).setStrokeColor([0.0, 0.0, 1.0]).drawOn(page)
        Path().add(Point(10, 30)).add(Point(50, 30)).setStrokeColor([0.0, 1.0, 0.0]).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("1 0 0 RG"), "\(content)")
        #expect(content.contains("0 0 1 RG"), "\(content)")
        #expect(content.contains("0 1 0 RG"), "\(content)")
    }

    // An arc that starts at the end of a line finds its center and its start
    // angle with the precision of Go's port, double where Go computes in
    // double: in float, 12 of 2,000 random arcs were drawn a hundredth apart
    // (review of 11 October 2026, #25). The expected paths are Go's.
    @Test func anArcAtTheEndOfALineIsDrawnAsTheGoPortDrawsIt() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        Arc().setRadiusX(119.10132).setRadiusY(84.51296)
                .setStartAngle(-44.814667).setSweep(352.7614)
                .setStartPointToEndOf(Line(457.52744, 348.75574, 36.200237, 153.82916))
                .setStrokeColor(0x000000).setLocation(358.057, 83.32647).drawOn(page)
        #expect(TestSupport.content(page).contains("200.81 765.98 221.56 716.04 191.69 675.62 c\n"))

        page = Page(pdf, Letter.PORTRAIT)
        Arc().setRadiusX(179.07474).setRadiusY(46.57458)
                .setStartAngle(11.943497).setSweep(338.10748)
                .setStartPointToEndOf(Line(397.40942, 538.2834, 232.55406, 318.26773))
                .setStrokeColor(0x000000).setLocation(182.30334, 248.89684).drawOn(page)
        #expect(TestSupport.content(page).contains("376.51 449.26 275.7 456.2 225.54 476.35 c\n"))
    }

    // An Arc, and so an Ellipse, given a stroke width and no color is drawn a
    // black line of that width: it was drawn 0 wide, a hairline, whatever the
    // width; with no width it is 0 wide as before.
    @Test func anArcOfAWidthAndNoColorIsDrawnThatWide() {
        for (name, arc) in [("arc", Arc()), ("ellipse", Ellipse())] {
            let page = self.page()
            arc.setLocation(100, 100)
            _ = arc.setRadius(40).setSweep(270).setStrokeWidth(4)
            arc.drawOn(page)
            let content = TestSupport.content(page)
            #expect(content.contains("\n4 w\n"), "\(name): not drawn 4 wide: \(content)")
        }
        let page = self.page()
        let arc = Arc()
        arc.setLocation(100, 100)
        _ = arc.setRadius(40).setSweep(90)
        arc.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("\n0 w\n") && !content.contains(" d\n"),
                "an arc of no width is not 0 wide and solid: \(content)")
        // And dashed when it has a dash (review of 12 October 2026, R21)
        let dashedPage = self.page()
        let dashed = Arc()
        dashed.setLocation(100, 100)
        _ = dashed.setRadius(40).setSweep(120).setStrokeWidth(3).setStrokeDashPattern("[6 3] 0")
        dashed.drawOn(dashedPage)
        let dashedContent = TestSupport.content(dashedPage)
        #expect(dashedContent.contains("[6 3] 0 d\n"), "an arc of no color has no dash: \(dashedContent)")
    }
}
