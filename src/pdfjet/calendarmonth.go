package pdfjet

/**
 * calendarmonth.go
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"fmt"
	"github.com/edragoev1/pdfjet/src/pdfjet/color"
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
