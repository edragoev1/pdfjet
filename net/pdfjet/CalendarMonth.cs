/**
 *  CalendarMonth.cs
 *
Copyright 2024 Innovatics Inc.

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
using System;
using System.Globalization;

namespace PDFjet.NET {
public class CalendarMonth : IDrawable {
    Font f1 = null;
    Font f2 = null;

    float x1 = 75f;
    float y1 = 75f;
    float dx = 23f;
    float dy = 20f;

    String[] days = {"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"};
    // DAY_OF_WEEK     1     2     3     4     5     6     7

    int daysInMonth;
    int dayOfWeek;

    public CalendarMonth(Font f1, Font f2, int year, int month) {
        this.f1 = f1;
        this.f2 = f2;
        daysInMonth = GetDaysInMonth(year, month - 1);
        Calendar calendar = new GregorianCalendar();
        DateTime dateTime = new DateTime(year, month, 1, calendar);
        dayOfWeek = (int) calendar.GetDayOfWeek(dateTime);

	    foreach (String day in days) {
            float w = 2*((float) f1.StringWidth(day));
            if (w > dx) {
                dx = w;
            }
        }
        dy = dx;
    }

    public void SetHeadFont(Font font) {
        this.f1 = font;
    }

    public void SetBodyFont(Font font) {
        this.f2 = font;
    }

    public void SetCellWidth(float width) {
        this.dx = width;
    }

    public void SetCellHeight(float height) {
        this.dy = height;
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public CalendarMonth SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    public CalendarMonth SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    public float[] DrawOn(Page page) {
        for (int row = 0; row < 7; row++) {
            for (int col = 0; col < 7; col++) {
                if (row == 0) {
                    float offset = (dx - f1.StringWidth(days[col])) / 2;
                    TextLine text = new TextLine(f1, days[col]);
                    text.SetLocation(x1 + col*dx + offset, y1 + (dy/2) - f1.descent);
                    text.DrawOn(page);
                    // Draw the line separating the title from the dates.
                    Line line = new Line(
                            x1,
                            y1 + dy/2 + f1.descent,
                            x1 + 7*dx,
                            y1 + dy/2 + f1.descent);
                    line.DrawOn(page);
                } else {
                    int dayOfMonth = ((7*row + col) - 6) - dayOfWeek;
                    if (dayOfMonth > 0 && dayOfMonth <= daysInMonth) {
                        String s1 = dayOfMonth.ToString();
                        float offset = (dx - f2.StringWidth(s1)) / 2;
                        TextLine text = new TextLine(f2, s1);
                        text.SetLocation(x1 + col*dx + offset, y1 + row*dy + f2.ascent);
                        text.DrawOn(page);

                        page.SetPenWidth(1.5f);
                        page.SetPenColor(Color.blue);
			            page.DrawEllipse(
                                x1 + col*dx + dx/2,
                                y1 + row*dy + f2.GetHeight()/2,
                                dx/2.5,
                                dy/2.5);
                    }
                }
            }
        }
        return new float[] {this.x1 + 7*this.dx, this.y1 + 7*this.dy};
    }

    private bool IsLeapYear(int year) {
        return (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
    }

    private int GetDaysInMonth(
            int year,
            int month) {
        int daysInFebruary = IsLeapYear(year) ? 29 : 28;
        int[] daysInMonth = {31, daysInFebruary, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
        return daysInMonth[month];
    }
}   // End of CalendarMonth.cs
}   // End of namespace PDFjet.NET
