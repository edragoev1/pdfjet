// textbox_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/border"
)

func TestTextBoxMeasuringDoesNotFixTheHeight(t *testing.T) {
	box := NewTextBoxWithText(testHelvetica(testNewPDF()), testTenWords)
	box.SetLocation(0, 0)
	box.SetWidth(60)
	testAssertXY(t, 60, 83.232, box.DrawOn(nil))
	testNear(t, "height", 83.232, box.GetHeight(), testDelta)

	box.SetText("one two three four five six seven eight nine ten eleven twelve thirteen fourteen")
	testAssertXY(t, 60, 124.848, box.DrawOn(nil))
	testNear(t, "height", 124.848, box.GetHeight(), testDelta)
}

func TestTextBoxBordersAreOffByDefaultAndCanBeRemovedOneByOne(t *testing.T) {
	box := NewTextBoxWithText(testHelvetica(testNewPDF()), "x")
	if box.GetBorder(border.Top) {
		t.Error("a new text box has a top border")
	}
	box.SetBorders(true)
	box.SetBorder(border.Top, false)
	if box.GetBorder(border.Top) {
		t.Error("the top border was not removed")
	}
	if !box.GetBorder(border.Left) || !box.GetBorder(border.Right) || !box.GetBorder(border.Bottom) {
		t.Error("another border was removed")
	}
}

func TestTextBoxColorGettersReturnCopies(t *testing.T) {
	box := NewTextBoxWithText(testHelvetica(testNewPDF()), "x")
	box.SetTextColor(0x0000FF)
	color := box.GetTextColor()
	color[2] = 0
	testAssertRGB(t, 0, 0, 1, box.GetTextColor())
	box.SetBorderColor(0xFF0000)
	borderColor := box.GetBorderColor()
	if borderColor == nil {
		t.Fatal("no border color")
	}
	borderColor[0] = 0
	testAssertRGB(t, 1, 0, 0, *box.GetBorderColor())
}
