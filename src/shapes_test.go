// shapes_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "testing"

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
	arc := NewArc().SetRadius(20).SetStartAngle(0).SetSweepDegreesCW(90)
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
	font := testHelvetica(testNewPDF())
	radio := NewRadioButton(font, "rb")
	radio.SetLocation(1, 2)
	testAssertXY(t, 45.184, 15.872, radio.DrawOn(page))
	checkBox := NewCheckBox(font, "cb")
	checkBox.SetLocation(1, 2)
	testAssertXY(t, 47.188, 15.872, checkBox.DrawOn(page))
}

func TestShapesCalendarMonthStartsAtTheOriginWithCellsFromTheDayNames(t *testing.T) {
	font := testHelvetica(testNewPDF())
	// February and March 2026 start on a Sunday.
	testAssertXY(t, 252, 252, NewCalendarMonth(font, font, 2026, 2).DrawOn(testNewPage()))
	testAssertXY(t, 252, 252, NewCalendarMonth(font, font, 2026, 3).DrawOn(testNewPage()))
}
