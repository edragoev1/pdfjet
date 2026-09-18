// calendarmonth.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
)

var calendarDays = [7]string{"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"}

// CalendarMonth draws a calendar for one month: the names of the days in the
// header font, a line under them, and the dates in the body font, each in a
// blue circle, in up to six weeks. The week starts on Sunday unless
// SetFirstDayOfWeek sets another day.
type CalendarMonth struct {
	f1, f2      *Font
	x1          float32
	y1          float32
	dx          float32
	dy          float32
	daysInMonth int
	// The day of the week of the first day of the month and the first day of
	// the week, from 0 for Sunday to 6 for Saturday.
	firstDayOfMonth int
	firstDayOfWeek  int
}

// NewCalendarMonth creates a calendar for the specified month.
//   - f1: the header font.
//   - f2: the body font.
//   - year: the year.
//   - month: the month, from 1 to 12.
func NewCalendarMonth(f1, f2 *Font, year, month int) *CalendarMonth {
	calendarMonth := new(CalendarMonth)
	calendarMonth.f1 = f1
	calendarMonth.f2 = f2
	first := time.Date(year, time.Month(month), 1, 0, 0, 0, 0, time.UTC)
	// Day 0 of the next month is the last day of this one.
	calendarMonth.daysInMonth = time.Date(year, time.Month(month)+1, 0, 0, 0, 0, 0, time.UTC).Day()
	calendarMonth.firstDayOfMonth = int(first.Weekday())
	for _, day := range calendarDays {
		w := 2 * f1.StringWidth(f1.size, day)
		if w > calendarMonth.dx {
			calendarMonth.dx = w
		}
	}
	calendarMonth.dy = calendarMonth.dx
	return calendarMonth
}

// SetHeadFont sets the font of the calendar header.
func (calendarMonth *CalendarMonth) SetHeadFont(font *Font) *CalendarMonth {
	calendarMonth.f1 = font
	return calendarMonth
}

// SetBodyFont sets the font of the calendar body.
func (calendarMonth *CalendarMonth) SetBodyFont(font *Font) *CalendarMonth {
	calendarMonth.f2 = font
	return calendarMonth
}

// SetLocation sets the location of the top left corner of the calendar.
func (calendarMonth *CalendarMonth) SetLocation(x, y float32) Drawable {
	calendarMonth.x1 = x
	calendarMonth.y1 = y
	return calendarMonth
}

// SetCellWidth sets the width of the day cells.
func (calendarMonth *CalendarMonth) SetCellWidth(width float32) *CalendarMonth {
	calendarMonth.dx = width
	return calendarMonth
}

// SetCellHeight sets the height of the day cells.
func (calendarMonth *CalendarMonth) SetCellHeight(height float32) *CalendarMonth {
	calendarMonth.dy = height
	return calendarMonth
}

// SetFirstDayOfWeek sets the day the weeks start on, in the first column. The
// default is Sunday; many countries start the week on Monday.
func (calendarMonth *CalendarMonth) SetFirstDayOfWeek(day time.Weekday) *CalendarMonth {
	calendarMonth.firstDayOfWeek = (int(day)%7 + 7) % 7
	return calendarMonth
}

// DrawOn draws the calendar on the page and returns the x and y coordinates of
// its bottom right corner. The line and the circles are drawn as an artifact,
// and the pen of the page is left as it was.
func (calendarMonth *CalendarMonth) DrawOn(page *Page) [2]float32 {
	x1, y1 := calendarMonth.x1, calendarMonth.y1
	dx, dy := calendarMonth.dx, calendarMonth.dy
	f1, f2 := calendarMonth.f1, calendarMonth.f2
	if page == nil {
		return [2]float32{x1 + 7*dx, y1 + 7*dy} // Measured, not drawn
	}
	// The first day of the month is in this column of the first week.
	firstColumn := (calendarMonth.firstDayOfMonth - calendarMonth.firstDayOfWeek + 7) % 7
	for col := 0; col < 7; col++ {
		day := calendarDays[(calendarMonth.firstDayOfWeek+col)%7]
		offset := (dx - f1.StringWidth(f1.size, day)) / 2
		text := NewTextLine(f1, day)
		text.SetLocation(x1+float32(col)*dx+offset, y1+dy/2-f1.descent)
		text.DrawOn(page)
	}
	for dayOfMonth := 1; dayOfMonth <= calendarMonth.daysInMonth; dayOfMonth++ {
		cell := firstColumn + dayOfMonth - 1
		x := x1 + float32(cell%7)*dx
		y := y1 + float32(cell/7+1)*dy
		date := strconv.Itoa(dayOfMonth)
		offset := (dx - f2.StringWidth(f2.size, date)) / 2
		text := NewTextLine(f2, date)
		text.SetLocation(x+offset, y+f2.ascent)
		text.DrawOn(page)
	}

	page.AddArtifactBMC()
	page.SaveGraphicsState()
	page.SetStrokeDashPattern("[] 0")
	page.SetLineCapStyle(capstyle.Butt)
	// The line separating the names of the days from the dates
	page.SetPenColor(color.Black)
	page.SetPenWidth(0)
	page.DrawLine(x1, y1+dy/2+f1.descent, x1+7*dx, y1+dy/2+f1.descent)
	page.SetPenColor(color.Blue)
	page.SetPenWidth(1.25)
	for dayOfMonth := 1; dayOfMonth <= calendarMonth.daysInMonth; dayOfMonth++ {
		cell := firstColumn + dayOfMonth - 1
		page.DrawEllipse(
			x1+float32(cell%7)*dx+dx/2,
			y1+float32(cell/7+1)*dy+f2.GetBodyHeight(f2.size)/2,
			dx/2.5,
			dy/2.5)
	}
	page.RestoreGraphicsState()
	page.AddEMC()
	return [2]float32{x1 + 7*dx, y1 + 7*dy}
}
