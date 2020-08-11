/**
 *  CalendarMonth.java
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
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
package com.pdfjet;


import java.lang.*;
import java.io.*;
import java.util.*;


public class CalendarMonth implements Drawable {

    Font f1 = null;
    Font f2 = null;

    float x1;
    float y1;
    float dx;
    float dy;
    float f1Ascent;
    float f2Ascent;

    String[] days = {"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"};
    // DAY_OF_WEEK     1     2     3     4     5     6     7

    int daysInMonth;
    int dayOfWeek;


    public CalendarMonth(Font f1, Font f2, int year, int month) {
        this.f1 = f1;
        this.f2 = f2;
        daysInMonth = getDaysInMonth(year, month - 1);
        Calendar calendar = Calendar.getInstance();
        calendar.set(Calendar.YEAR, year);
        calendar.set(Calendar.MONTH, month - 1);
        calendar.set(Calendar.DAY_OF_MONTH, 1);
        dayOfWeek = calendar.get(Calendar.DAY_OF_WEEK);
        for (String day : days) {
            float w = 2*f1.stringWidth(day);
            if (w > dx) {
                dx = w;
            }
        }
        dy = dx;
    }


    public void setHeadFont(Font font) {
        this.f1 = font;
    }


    public void setBodyFont(Font font) {
        this.f2 = font;
    }


    public void setCellWidth(float width) {
        this.dx = width;
    }


    public void setCellHeight(float height) {
        this.dy = height;
    }


    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    public void setPosition(double x, double y) {
        setLocation(x, y);
    }

    public CalendarMonth setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    public CalendarMonth setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }


    public float[] drawOn(Page page) throws Exception {
        for (int row = 0; row < 7; row++) {
            for (int col = 0; col < 7; col++) {
                if (row == 0) {
                    float offset = (dx - f1.stringWidth(days[col])) / 2;
                    TextLine text = new TextLine(f1, days[col]);
                    text.setLocation(x1 + col*dx + offset, y1 + (dy/2) - f1.descent);
                    text.drawOn(page);
                    // Draw the line separating the title from the dates.
                    Line line = new Line(
                            x1,
                            y1 + dy/2 + f1.descent,
                            x1 + 7*dx,
                            y1 + dy/2 + f1.descent);
                    line.drawOn(page);
                }
                else {
                    int dayOfMonth = ((7*row + col) - 6) - (dayOfWeek - 1);
                    if (dayOfMonth > 0 && dayOfMonth <= daysInMonth) {
                        String s1 = String.valueOf(dayOfMonth);
                        float offset = (dx - f2.stringWidth(s1)) / 2;
                        TextLine text = new TextLine(f2, s1);
                        text.setLocation(x1 + col*dx + offset, y1 + row*dy + f2.ascent);
                        text.drawOn(page);

                        page.setPenWidth(1.25f);
                        page.setPenColor(Color.blue);
                        page.drawEllipse(
                                x1 + col*dx + dx/2,
                                y1 + row*dy + (f2.getHeight()/2),
                                dx/2.5,
                                dy/2.5);
                    }
                }
            }
        }
        return new float[] {this.x1 + 7*this.dx, this.y1 + 7*this.dy};
    }


    private boolean isLeapYear(int year) {
        return (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
    }


    private int getDaysInMonth(
            int year,
            int month) {
        int daysInFebruary = isLeapYear(year) ? 29 : 28;
        int[] daysInMonth = {31, daysInFebruary, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
        return daysInMonth[month];
    }

}
