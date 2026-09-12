// calendarmonth.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/color"
)

// CalendarMonth describes calendar month object.
type CalendarMonth struct {
	f1, f2      *Font
	x1          float32
	y1          float32
	dx          float32
	dy          float32
	days        []string
	daysInMonth int
	dayOfWeek   int
}

// NewCalendarMonth creates a calendar for the specified month.
// @param f1 the header font.
// @param f2 the body font.
// @param year the year.
// @param month the month, from 1 to 12.
func NewCalendarMonth(f1, f2 *Font, year, month int) *CalendarMonth {
	calendarMonth := new(CalendarMonth)
	calendarMonth.f1 = f1
	calendarMonth.f2 = f2
	calendarMonth.x1 = 75.0
	calendarMonth.y1 = 75.0
	calendarMonth.dx = 23.0
	calendarMonth.dy = 20.0
	calendarMonth.days = []string{"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"}
	calendarMonth.daysInMonth = calendarMonth.getDaysInMonth(year, month-1)
	// The day of the week of the first day of the month, from 1 (Sunday) to 7.
	firstDay := time.Date(year, time.Month(month), 1, 0, 0, 0, 0, time.UTC)
	calendarMonth.dayOfWeek = int(firstDay.Weekday()) + 1
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

// DrawOn draws the calendar month on the page.
func (calendarMonth *CalendarMonth) DrawOn(page *Page) [2]float32 {
	for row := 0; row < 7; row++ {
		for col := 0; col < 7; col++ {
			if row == 0 {
				offset := (calendarMonth.dx -
					calendarMonth.f1.StringWidth(calendarMonth.f1.size, calendarMonth.days[col])) / 2.0
				text := NewTextLine(calendarMonth.f1, calendarMonth.days[col])
				text.SetLocation(
					calendarMonth.x1+float32(col)*calendarMonth.dx+offset,
					calendarMonth.x1+float32(row)*calendarMonth.dy)
				text.DrawOn(page)
				// Draw the line separating the title from the dates.
				line := NewLine(
					calendarMonth.x1,
					calendarMonth.y1+calendarMonth.dx/4,
					calendarMonth.x1+7*calendarMonth.dx,
					calendarMonth.y1+calendarMonth.dx/4)
				line.SetWidth(0.5)
				line.DrawOn(page)
			} else {
				dayOfMonth := ((7*row + col) - 6) - (calendarMonth.dayOfWeek - 1)
				if dayOfMonth > 0 && dayOfMonth <= calendarMonth.daysInMonth {
					s1 := strconv.Itoa(dayOfMonth)
					offset := (calendarMonth.dx - calendarMonth.f2.StringWidth(calendarMonth.f2.size, s1)) / 2
					text := NewTextLine(calendarMonth.f2, s1)
					text.SetLocation(calendarMonth.x1+float32(col)*calendarMonth.dx+offset, calendarMonth.y1+float32(row)*calendarMonth.dy)
					text.DrawOn(page)

					page.SetPenWidth(1.5)
					page.SetPenColor(color.Blue)
					page.DrawEllipse(
						calendarMonth.x1+float32(col)*calendarMonth.dx+calendarMonth.dx/2,
						calendarMonth.y1+float32(row)*calendarMonth.dy-calendarMonth.dy/5, 8.0, 8.0)
				}
			}
		}
	}
	return [2]float32{calendarMonth.x1 + 7*calendarMonth.dx, calendarMonth.y1 + 7*calendarMonth.dy}
}

func (calendarMonth *CalendarMonth) isLeapYear(year int) bool {
	return (year%4 == 0 && year%100 != 0) || year%400 == 0
}

func (calendarMonth *CalendarMonth) getDaysInMonth(year, month int) int {
	daysInFebruary := 28
	if calendarMonth.isLeapYear(year) {
		daysInFebruary = 29
	}
	daysInMonth := []int{31, daysInFebruary, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31}
	return daysInMonth[month]
}
