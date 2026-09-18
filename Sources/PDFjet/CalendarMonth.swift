/**
 * CalendarMonth.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Draws a calendar for one month: the names of the days in the header font,
/// a line under them, and the dates in the body font, each in a blue circle,
/// in up to six weeks. The week starts on Sunday unless setFirstDayOfWeek
/// sets another day.
///
public class CalendarMonth : Drawable {
    private var f1: Font
    private var f2: Font

    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var dx: Float = 0.0
    private var dy: Float = 0.0

    private static let days = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"]

    private let daysInMonth: Int
    // The day of the week of the first day of the month and the first day of
    // the week, from 0 for Sunday to 6 for Saturday.
    private let firstDayOfMonth: Int
    private var firstDayOfWeek = 0

    ///
    /// Creates a calendar for the specified month.
    ///
    /// - Parameter f1: the header font.
    /// - Parameter f2: the body font.
    /// - Parameter year: the year.
    /// - Parameter month: the month, from 1 to 12.
    ///
    public init(_ f1: Font, _ f2: Font, _ year: Int, _ month: Int) {
        self.f1 = f1
        self.f2 = f2
        // The Gregorian calendar, whatever the locale
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(identifier: "UTC")!
        let first = DateComponents(calendar: calendar, year: year, month: month, day: 1).date!
        daysInMonth = calendar.range(of: .day, in: .month, for: first)!.count
        firstDayOfMonth = calendar.component(.weekday, from: first) - 1
        for day in CalendarMonth.days {
            let w = 2*f1.stringWidth(day)
            if w > dx {
                dx = w
            }
        }
        dy = dx
    }

    /// Sets the header font.
    @discardableResult
    public func setHeadFont(_ font: Font) -> CalendarMonth {
        self.f1 = font
        return self
    }

    /// Sets the body font.
    @discardableResult
    public func setBodyFont(_ font: Font) -> CalendarMonth {
        self.f2 = font
        return self
    }

    /// Sets the width of the day cells.
    @discardableResult
    public func setCellWidth(_ width: Float) -> CalendarMonth {
        self.dx = width
        return self
    }

    /// Sets the height of the day cells.
    @discardableResult
    public func setCellHeight(_ height: Float) -> CalendarMonth {
        self.dy = height
        return self
    }

    ///
    /// Sets the day the weeks start on, in the first column, numbered as the
    /// weekdays of Foundation's Calendar: 1 for Sunday, 2 for Monday and so on
    /// to 7 for Saturday. The default is Sunday; many countries start the week
    /// on Monday.
    ///
    @discardableResult
    public func setFirstDayOfWeek(_ weekday: Int) -> CalendarMonth {
        self.firstDayOfWeek = ((weekday - 1) % 7 + 7) % 7
        return self
    }

    /// Sets the location of the top left corner of the calendar.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x1 = x
        self.y1 = y
        return self
    }

    ///
    /// Draws this calendar on the specified page. The line and the circles are
    /// drawn as an artifact, and the pen of the page is left as it was.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and y coordinates of the bottom right corner of the calendar.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page else {
            return [x1 + 7*dx, y1 + 7*dy]  // Measured, not drawn
        }
        // The first day of the month is in this column of the first week.
        let firstColumn = (firstDayOfMonth - firstDayOfWeek + 7) % 7
        for col in 0..<7 {
            let day = CalendarMonth.days[(firstDayOfWeek + col) % 7]
            let offset = (dx - f1.stringWidth(day)) / 2
            TextLine(f1, day).setLocation(x1 + Float(col)*dx + offset, y1 + dy/2 - f1.descent).drawOn(page)
        }
        for dayOfMonth in 1...daysInMonth {
            let cell = firstColumn + dayOfMonth - 1
            let x = x1 + Float(cell % 7)*dx
            let y = y1 + Float(cell / 7 + 1)*dy
            let date = String(dayOfMonth)
            let offset = (dx - f2.stringWidth(date)) / 2
            TextLine(f2, date).setLocation(x + offset, y + f2.ascent).drawOn(page)
        }

        page.addArtifactBMC()
        page.saveGraphicsState()
        page.setStrokeDashPattern("[] 0")
        page.setLineCapStyle(CapStyle.BUTT)
        // The line separating the names of the days from the dates
        page.setPenColor(Color.black)
        page.setPenWidth(0.0)
        page.drawLine(x1, y1 + dy/2 + f1.descent, x1 + 7*dx, y1 + dy/2 + f1.descent)
        page.setPenColor(Color.blue)
        page.setPenWidth(1.25)
        for dayOfMonth in 1...daysInMonth {
            let cell = firstColumn + dayOfMonth - 1
            page.drawEllipse(
                    x1 + Float(cell % 7)*dx + dx/2,
                    y1 + Float(cell / 7 + 1)*dy + f2.getBodyHeight()/2,
                    dx/2.5,
                    dy/2.5)
        }
        page.restoreGraphicsState()
        page.addEMC()
        return [x1 + 7*dx, y1 + 7*dy]
    }
}   // End of CalendarMonth.swift
