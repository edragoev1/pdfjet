/**
 *  CalendarMonth.swift
 *
Copyright 2023 Innovatics Inc.

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
import Foundation


public class CalendarMonth : Drawable {

    var f1: Font?
    var f2: Font?

    var x1: Float = 75.0
    var y1: Float = 75.0
    var dx: Float = 23.0
    var dy: Float = 20.0
    var f1Ascent: Float = 0.0
    var f2Ascent: Float = 0.0

    let days = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"]
    //            1     2     3     4     5     6     7     // DAY_OF_WEEK

    var daysInMonth: Int?
    var dayOfWeek: Int?


    public init(_ f1: Font, _ f2: Font, _ year: Int, _ month: Int) {
        self.f1 = f1
        self.f2 = f2
        daysInMonth = getDaysInMonth(year, month - 1)
        let calendar = Calendar.current
        let components = DateComponents(
                calendar: calendar, year: year, month: month, day: 0)
        dayOfWeek = calendar.component(.weekday, from: components.date!)

        var w: Float = 0.0
        for day in days {
            w = 2*Float(f1.stringWidth(day))
            if (w > dx) {
                dx = w
            }
        }
        dy = dx;
    }


    public func setHeadFont(_ font: Font) {
        self.f1 = font
    }


    public func setBodyFont(_ font: Font) {
        self.f2 = font
    }

    public func setCellWidth(_ width: Float) {
        self.dx = width
    }


    public func setCellHeight(_ height: Float) {
        self.dy = height
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    public func setLocation(_ x: Float, _ y: Float) {
        self.x1 = x
        self.y1 = y
    }


    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        for row in 0..<7 {
            for col in 0..<7 {
                if row == 0 {
                    let offset = (dx - f1!.stringWidth(days[col])) / 2
                    let text = TextLine(f1!, days[col])
                    text.setLocation(x1 + Float(col)*dx + offset, y1 + (dy/2) - f1!.descent)
                    if page != nil {
                        text.drawOn(page!)
                        // Draw the line separating the title from the dates.
                        let line = Line(
                                x1,
                                y1 + dy/2 + f1!.descent,
                                x1 + 7*dx,
                                y1 + dy/2 + f1!.descent)
                        // line.setWidth(0.5) // only changes width of first line
                        line.drawOn(page!)
                    }
                }
                else {
                    let dayOfMonth = ((7*row + col) - 6) - dayOfWeek!
                    if dayOfMonth > 0 && dayOfMonth <= daysInMonth! {
                        let s1 = String(dayOfMonth)
                        let offset = (dx - f2!.stringWidth(s1)) / 2
                        let text = TextLine(f2!, s1)
                        text.setLocation(x1 + Float(col)*dx + offset, y1 + Float(row)*dy + f2!.ascent)
                        if page != nil {
                            text.drawOn(page!)
                            page!.setPenWidth(1.25)
                            page!.setPenColor(Color.blue)
                            page!.drawEllipse(
                                    x1 + Float(col)*dx + dx/2,
                                    y1 + Float(row)*dy + f2!.getHeight()/2,
                                    dx/2.5,
                                    dy/2.5)
                        }
                    }
                }
            }
        }
        return [self.x1 + 7*self.dx, self.y1 + 7*self.dy]
    }


    private func isLeapYear(_ year: Int) -> Bool {
        return (year % 4 == 0 && year % 100 != 0) || year % 400 == 0
    }


    private func getDaysInMonth(
            _ year: Int,
            _ month: Int) -> Int {
        let daysInFebruary = isLeapYear(year) ? 29 : 28
        let daysInMonth = [31, daysInFebruary, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
        return daysInMonth[month]
    }

}
