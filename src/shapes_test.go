// shapes_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/color"
)

// Line, Rect, Arc, Path, RadioButton, CheckBox and CalendarMonth locations and corners.

func TestShapesLineSetLocationMovesTheWholeLine(t *testing.T) {
	line := NewLine(10, 10, 50, 30)
	line.SetLocation(100, 100)
	testNear(t, "start x", 100, line.GetStartPoint().GetX(), 0)
	testNear(t, "start y", 100, line.GetStartPoint().GetY(), 0)
	testNear(t, "end x", 140, line.GetEndPoint().GetX(), 0)
	testNear(t, "end y", 120, line.GetEndPoint().GetY(), 0)
	testAssertXY(t, 140, 120, line.DrawOn(testNewPage()))
}

func TestShapesRectScaleByKeepsTheLocation(t *testing.T) {
	testAssertXY(t, 70, 100, NewRect(10, 20, 30, 40).ScaleBy(2).DrawOn(testNewPage()))
}

func TestShapesArcDrawOnReturnsTheBottomRightCornerOfItsCircle(t *testing.T) {
	arc := NewArc().SetRadius(20).SetStartAngle(0).SetSweep(90)
	arc.SetLocation(100, 100)
	testAssertXY(t, 120, 120, arc.DrawOn(testNewPage()))
}

func TestShapesPathSetLocationSetsTheOffsetInsteadOfAddingToIt(t *testing.T) {
	path := NewPath().Add(NewPoint(0, 0)).Add(NewPoint(10, 20))
	path.SetLocation(5, 5)
	path.SetLocation(5, 5)
	testAssertXY(t, 15, 25, path.DrawOn(testNewPage()))
}

func TestShapesRadioButtonAndCheckBoxCorners(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	radio := NewRadioButton(font, "rb")
	radio.SetLocation(1, 2)
	testAssertXY(t, 45.184, 15.872, radio.DrawOn(page))
	checkBox := NewCheckBox(font, "cb")
	checkBox.SetLocation(1, 2)
	testAssertXY(t, 47.188, 15.872, checkBox.DrawOn(page))
}

func TestShapesCalendarMonthStartsAtTheOriginWithCellsFromTheDayNames(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	// February and March 2026 start on a Sunday.
	testAssertXY(t, 252, 252, NewCalendarMonth(font, font, 2026, 2).DrawOn(page))
	testAssertXY(t, 252, 252, NewCalendarMonth(font, font, 2026, 3).DrawOn(page))
}

func TestShapesCalendarMonthStartsTheWeekOnTheFirstDayOfTheWeek(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	NewCalendarMonth(font, font, 2026, 2).SetFirstDayOfWeek(time.Monday).DrawOn(page)
	content := testContent(page)
	if strings.Index(content, "<4D6F>") > strings.Index(content, "<5375>") {
		t.Error("Su is before Mo")
	}
	// Sunday, February 1, ends the first week, and Monday, February 2, starts the second.
	if !strings.Contains(content, "230.66 744.83 Td\n[<31>] TJ") {
		t.Error("the 1st is not in the last column")
	}
	if !strings.Contains(content, "14.66 708.83 Td\n[<32>] TJ") {
		t.Error("the 2nd is not in the first column")
	}
}

func TestShapesCalendarMonthLeavesThePenOfThePageAsItWas(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	NewCalendarMonth(font, font, 2026, 2).DrawOn(page)
	start := len(testContent(page))
	// The blue pen 1.25 wide of the circles is not the pen of the page.
	page.SetPenColor(color.Blue)
	page.SetPenWidth(1.25)
	page.DrawLine(10, 10, 50, 10)
	line := testContent(page)[start:]
	if !strings.Contains(line, "0 0 1 RG\n") || !strings.Contains(line, "1.25 w\n") {
		t.Errorf("the line is drawn with the pen of the calendar: %q", line)
	}
}

func TestShapesColorsAreSetAsAnIntOrAsAnArray(t *testing.T) {
	page := testNewPage()
	NewLine(10, 10, 50, 10).SetStrokeColor(color.Red).DrawOn(page)
	NewLine(10, 20, 50, 20).SetStrokeColorRGB([3]float32{0, 0, 1}).DrawOn(page)
	NewPath().Add(NewPoint(10, 30)).Add(NewPoint(50, 30)).SetStrokeColorRGB([3]float32{0, 1, 0}).DrawOn(page)
	content := testContent(page)
	for _, want := range []string{"1 0 0 RG", "0 0 1 RG", "0 1 0 RG"} {
		if !strings.Contains(content, want) {
			t.Errorf("content lacks %q: %s", want, content)
		}
	}
}
