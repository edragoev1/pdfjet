/*
 * CalendarMonth.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;

namespace PDFjet.NET {
/// <summary>
/// Draws a calendar for one month: the names of the days in the header font,
/// a line under them, and the dates in the body font, each in a blue circle,
/// in up to six weeks. The week starts on Sunday unless SetFirstDayOfWeek
/// sets another day.
/// </summary>
public class CalendarMonth : IDrawable {
    private Font f1;
    private Font f2;

    private float x1;
    private float y1;
    private float dx;
    private float dy;

    private static readonly String[] DAYS = {"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"};

    private readonly int daysInMonth;
    // The day of the week of the first day of the month and the first day of
    // the week, from 0 for Sunday to 6 for Saturday.
    private readonly int firstDayOfMonth;
    private int firstDayOfWeek = 0;

    private static readonly int[] DAYS_IN_MONTH = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};

    /// <summary>
    /// Creates a calendar for the specified month.
    /// </summary>
    /// <param name="f1">the header font.</param>
    /// <param name="f2">the body font.</param>
    /// <param name="year">the year.</param>
    /// <param name="month">the month, from 1 to 12.</param>
    public CalendarMonth(Font f1, Font f2, int year, int month) {
        this.f1 = f1;
        this.f2 = f2;
        // The Gregorian calendar, whatever the culture, of any year as Java's:
        // DateTime refused the years past 9999 (review of 17 October 2026)
        if (month < 1 || month > 12) {
            throw new ArgumentOutOfRangeException(nameof(month), "The month " + month + " is not one: from 1 to 12.");
        }
        bool leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        daysInMonth = month == 2 ? (leap ? 29 : 28) : DAYS_IN_MONTH[month - 1];
        // The days since 1 January 1970, a Thursday, of the first of the month
        long y = month <= 2 ? (long) year - 1 : year;
        long era = (y >= 0 ? y : y - 399) / 400;
        long yearOfEra = y - era * 400;
        long dayOfYear = (153 * (month > 2 ? month - 3 : month + 9) + 2) / 5;
        long dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        long days = era * 146097 + dayOfEra - 719468;
        firstDayOfMonth = (int) (((days + 4) % 7 + 7) % 7);
        foreach (String day in DAYS) {
            float w = 2*f1.StringWidth(day);
            if (w > dx) {
                dx = w;
            }
        }
        dy = dx;
    }

    /// <summary>Sets the header font.</summary>
    public CalendarMonth SetHeadFont(Font font) {
        this.f1 = font;
        return this;
    }

    /// <summary>Sets the body font.</summary>
    public CalendarMonth SetBodyFont(Font font) {
        this.f2 = font;
        return this;
    }

    /// <summary>Sets the width of the day cells.</summary>
    public CalendarMonth SetCellWidth(float width) {
        this.dx = width;
        return this;
    }

    /// <summary>Sets the height of the day cells.</summary>
    public CalendarMonth SetCellHeight(float height) {
        this.dy = height;
        return this;
    }

    /// <summary>
    /// Sets the day the weeks start on, in the first column. The default is
    /// Sunday; many countries start the week on Monday.
    /// </summary>
    public CalendarMonth SetFirstDayOfWeek(DayOfWeek day) {
        // Any number was taken, and drew the days out of their columns
        this.firstDayOfWeek = ((int) day % 7 + 7) % 7;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of the top left corner of the calendar.</summary>
    public CalendarMonth SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    /// Draws this calendar on the specified page. The line and the circles are
    /// drawn as an artifact, and the pen of the page is left as it was.
    /// Returns the x and y coordinates of the bottom right corner of the calendar.
    /// </summary>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x1 + 7*dx, y1 + 7*dy};     // Measured, not drawn
        }
        // The first day of the month is in this column of the first week.
        int firstColumn = (firstDayOfMonth - firstDayOfWeek + 7) % 7;
        for (int col = 0; col < 7; col++) {
            String day = DAYS[(firstDayOfWeek + col) % 7];
            float offset = (dx - f1.StringWidth(day)) / 2;
            new TextLine(f1, day).SetLocation(x1 + col*dx + offset, y1 + dy/2 - f1.GetDescent()).DrawOn(page);
        }
        for (int dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth++) {
            int cell = firstColumn + dayOfMonth - 1;
            float x = x1 + (cell % 7)*dx;
            float y = y1 + (cell / 7 + 1)*dy;
            String date = dayOfMonth.ToString(CultureInfo.InvariantCulture);
            float offset = (dx - f2.StringWidth(date)) / 2;
            new TextLine(f2, date).SetLocation(x + offset, y + f2.GetAscent()).DrawOn(page);
        }

        page.AddArtifactBMC();
        page.SaveGraphicsState();
        page.SetStrokeDashPattern("[] 0");
        page.SetLineCapStyle(CapStyle.BUTT);
        // The line separating the names of the days from the dates
        page.SetPenColor(Color.black);
        page.SetPenWidth(0f);
        page.DrawLine(x1, y1 + dy/2 + f1.GetDescent(), x1 + 7*dx, y1 + dy/2 + f1.GetDescent());
        page.SetPenColor(Color.blue);
        page.SetPenWidth(1.25f);
        for (int dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth++) {
            int cell = firstColumn + dayOfMonth - 1;
            page.DrawEllipse(
                    x1 + (cell % 7)*dx + dx/2,
                    y1 + (cell / 7 + 1)*dy + f2.GetBodyHeight()/2,
                    dx/2.5f,
                    dy/2.5f);
        }
        page.RestoreGraphicsState();
        page.AddEMC();
        return new float[] {x1 + 7*dx, y1 + 7*dy};
    }
}   // End of CalendarMonth.cs
}   // End of namespace PDFjet.NET
