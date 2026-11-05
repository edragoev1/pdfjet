// chartroom_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"
)

// A chart too small for its plot is an error: the document is not completed,
// where the chart drew nothing and said nothing (the owner, 4 November 2026:
// "we can create more space if the user tries to jam too much info"). The
// same chart with room is drawn, with no error.
func TestAChartWithoutRoomIsAnError(t *testing.T) {
	charts := []struct {
		name  string
		small func(font *Font) Drawable
		room  func(font *Font) Drawable
	}{
		{"SimpleLineChart",
			func(f *Font) Drawable {
				return NewSimpleLineChart(f, f).SetTitle("Sales").AddLine("a", []float32{1, 5, 3}).SetSize(40, 30)
			},
			func(f *Font) Drawable {
				return NewSimpleLineChart(f, f).SetTitle("Sales").AddLine("a", []float32{1, 5, 3}).SetSize(300, 200)
			}},
		{"SimpleScatterChart",
			func(f *Font) Drawable {
				return NewSimpleScatterChart(f, f).SetTitle("Points").Add(1, 2).Add(3, 4).SetSize(40, 30)
			},
			func(f *Font) Drawable {
				return NewSimpleScatterChart(f, f).SetTitle("Points").Add(1, 2).Add(3, 4).SetSize(300, 200)
			}},
		{"BarChart",
			func(f *Font) Drawable {
				return NewBarChart(f, f).SetCategories("a", "b").AddSeries("s", []float32{1, 2}).SetSize(30, 20)
			},
			func(f *Font) Drawable {
				return NewBarChart(f, f).SetCategories("a", "b").AddSeries("s", []float32{1, 2}).SetSize(300, 200)
			}},
		{"SimpleBarChart",
			func(f *Font) Drawable {
				return NewSimpleBarChart(f, f).SetWidth(90).Add("A long name", 1234567)
			},
			func(f *Font) Drawable {
				return NewSimpleBarChart(f, f).SetWidth(300).Add("A long name", 1234567)
			}},
	}
	for _, c := range charts {
		pdf := testNewPDF()
		font := testHelvetica(pdf)
		c.small(font).DrawOn(NewPage(pdf, testLetterPortrait()))
		if pdf.err == nil || !strings.Contains(pdf.err.Error(), "has no room for its plot") ||
			!strings.Contains(pdf.err.Error(), c.name) {
			t.Errorf("%s too small: %v", c.name, pdf.err)
		}
		pdf = testNewPDF()
		font = testHelvetica(pdf)
		c.room(font).DrawOn(NewPage(pdf, testLetterPortrait()))
		if pdf.err != nil {
			t.Errorf("%s with room: %v", c.name, pdf.err)
		}
	}
}
