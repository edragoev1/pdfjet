/*
 * CalendarMonth.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.time.DayOfWeek;
import java.time.LocalDate;

/**
 * Draws a calendar for one month: the names of the days in the header font,
 * a line under them, and the dates in the body font, each in a blue circle,
 * in up to six weeks. The week starts on Sunday unless setFirstDayOfWeek
 * sets another day.
 */
public class CalendarMonth implements Drawable {
    private Font f1;
    private Font f2;

    private float x1;
    private float y1;
    private float dx;
    private float dy;

    private static final String[] DAYS = {"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"};

    private final int daysInMonth;
    // The day of the week of the first day of the month and the first day of
    // the week, from 0 for Sunday to 6 for Saturday.
    private final int firstDayOfMonth;
    private int firstDayOfWeek = 0;

    /**
     * Creates a calendar for the specified month.
     *
     * @param f1 the header font.
     * @param f2 the body font.
     * @param year the year.
     * @param month the month, from 1 to 12.
     */
    public CalendarMonth(Font f1, Font f2, int year, int month) {
        this.f1 = f1;
        this.f2 = f2;
        // The ISO calendar, Gregorian whatever the default locale
        LocalDate first = LocalDate.of(year, month, 1);
        daysInMonth = first.lengthOfMonth();
        firstDayOfMonth = first.getDayOfWeek().getValue() % 7;
        for (String day : DAYS) {
            float w = 2*f1.stringWidth(day);
            if (w > dx) {
                dx = w;
            }
        }
        dy = dx;
    }

    /**
     * Sets the header font.
     *
     * @param font the header font.
     * @return this CalendarMonth object.
     */
    public CalendarMonth setHeadFont(Font font) {
        this.f1 = font;
        return this;
    }

    /**
     * Sets the body font.
     *
     * @param font the body font.
     * @return this CalendarMonth object.
     */
    public CalendarMonth setBodyFont(Font font) {
        this.f2 = font;
        return this;
    }

    /**
     * Sets the width of the day cells.
     *
     * @param width the cell width.
     * @return this CalendarMonth object.
     */
    public CalendarMonth setCellWidth(float width) {
        this.dx = width;
        return this;
    }

    /**
     * Sets the height of the day cells.
     *
     * @param height the cell height.
     * @return this CalendarMonth object.
     */
    public CalendarMonth setCellHeight(float height) {
        this.dy = height;
        return this;
    }

    /**
     * Sets the day the weeks start on, in the first column. The default is
     * Sunday; many countries start the week on Monday.
     *
     * @param day the first day of the week.
     * @return this CalendarMonth object.
     */
    public CalendarMonth setFirstDayOfWeek(DayOfWeek day) {
        this.firstDayOfWeek = day.getValue() % 7;
        return this;
    }

    /**
     * Sets the location of the top left corner of the calendar.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this CalendarMonth object.
     */
    public CalendarMonth setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Draws this calendar on the specified page. The line and the circles are
     * drawn as an artifact, and the pen of the page is left as it was.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of the calendar.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        if (page == null) {
            return new float[] {x1 + 7*dx, y1 + 7*dy};     // Measured, not drawn
        }
        // The first day of the month is in this column of the first week.
        int firstColumn = (firstDayOfMonth - firstDayOfWeek + 7) % 7;
        for (int col = 0; col < 7; col++) {
            String day = DAYS[(firstDayOfWeek + col) % 7];
            float offset = (dx - f1.stringWidth(day)) / 2;
            new TextLine(f1, day).setLocation(x1 + col*dx + offset, y1 + dy/2 - f1.descent).drawOn(page);
        }
        for (int dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth++) {
            int cell = firstColumn + dayOfMonth - 1;
            float x = x1 + (cell % 7)*dx;
            float y = y1 + (cell / 7 + 1)*dy;
            String date = String.valueOf(dayOfMonth);
            float offset = (dx - f2.stringWidth(date)) / 2;
            new TextLine(f2, date).setLocation(x + offset, y + f2.ascent).drawOn(page);
        }

        page.addArtifactBMC();
        page.saveGraphicsState();
        page.setStrokeDashPattern("[] 0");
        page.setLineCapStyle(CapStyle.BUTT);
        // The line separating the names of the days from the dates
        page.setPenColor(Color.black);
        page.setPenWidth(0f);
        page.drawLine(x1, y1 + dy/2 + f1.descent, x1 + 7*dx, y1 + dy/2 + f1.descent);
        page.setPenColor(Color.blue);
        page.setPenWidth(1.25f);
        for (int dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth++) {
            int cell = firstColumn + dayOfMonth - 1;
            page.drawEllipse(
                    x1 + (cell % 7)*dx + dx/2,
                    y1 + (cell / 7 + 1)*dy + f2.getBodyHeight()/2,
                    dx/2.5f,
                    dy/2.5f);
        }
        page.restoreGraphicsState();
        page.addEMC();
        return new float[] {x1 + 7*dx, y1 + 7*dy};
    }
}   // End of CalendarMonth.java
