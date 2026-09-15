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
        return Page(TestSupport.newPDF(), Letter.PORTRAIT)
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
}
