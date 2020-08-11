package pdfjet

/**
 * calendarmonth.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  calendarMonth list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  calendarMonth list of conditions and the following disclaimer in the documentation
  and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

import (
	"color"
	"fmt"
	"strconv"
	"time"
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

// NewCalendarMonth constructs new calendar month object.
func (calendarMonth *CalendarMonth) NewCalendarMonth(f1, f2 *Font, year, month int) {
	calendarMonth.f1 = f1
	calendarMonth.f2 = f2
	calendarMonth.x1 = 75.0
	calendarMonth.y1 = 75.0
	calendarMonth.dx = 23.0
	calendarMonth.dy = 20.0
	calendarMonth.days = []string{"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"}
	calendarMonth.daysInMonth = calendarMonth.getDaysInMonth(year, month-1)

	now := time.Now()
	fmt.Println(now.Year())
	fmt.Println(now.Month())
	fmt.Println(now.Day())
	fmt.Println(now.Hour())
	fmt.Println(now.Minute())
	fmt.Println(now.Second())
	calendarMonth.dayOfWeek = int(now.Weekday())
}

func (calendarMonth *CalendarMonth) setHeadFont(font *Font) {
	calendarMonth.f1 = font
}

func (calendarMonth *CalendarMonth) setBodyFont(font *Font) {
	calendarMonth.f2 = font
}

func (calendarMonth *CalendarMonth) setLocation(x, y float32) {
	calendarMonth.x1 = x
	calendarMonth.y1 = y
}

func (calendarMonth *CalendarMonth) setCellWidth(width float32) {
	calendarMonth.dx = width
}

func (calendarMonth *CalendarMonth) setCellHeight(height float32) {
	calendarMonth.dy = height
}

// DrawOn draws the calendar month on the page.
func (calendarMonth *CalendarMonth) DrawOn(page *Page) {
	for row := 0; row < 7; row++ {
		for col := 0; col < 7; col++ {
			if row == 0 {
				offset := (float32(calendarMonth.dx) - calendarMonth.f1.stringWidth(calendarMonth.days[col])) / 2.0
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
					offset := (calendarMonth.dx - calendarMonth.f2.stringWidth(s1)) / 2
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
