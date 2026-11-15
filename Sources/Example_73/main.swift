/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_73.swift
 * The agenda of a team meeting, sent to the people attending before it: when
 * and where it is, who comes and who takes the notes, the topics with the
 * time each one starts and who leads it, and the actions from the last
 * meeting, to be checked at the start.
 *
 * This is a document people receive, not a form: every part has its data.
 * The topics are given with the minutes each one takes, and the start time
 * of each, and the end of the meeting, are worked out from them, so a topic
 * that is added or made longer moves the times after it. The two tables are
 * placed one under the other from where drawOn says the one before ended.
 *
 * The document is PDF/UA-1: the title and the two sections are headings, and
 * the tables are read by a screen reader by their column headers. The layout
 * is that of the meeting agenda template of PDFjet Forms, and Example_74 is
 * the minutes of the same meeting. The studio and the people are made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_73 {
    // A topic of the meeting: what it is, who leads it, and how long it takes
    struct Topic {
        let title: String
        let leader: String
        let minutes: Int

        init(_ title: String, _ leader: String, _ minutes: Int) {
            self.title = title
            self.leader = leader
            self.minutes = minutes
        }
    }

    // The data of the meeting. In an application it comes from the calendar.
    static let TEAM = "Northwind Studio"
    static let DATE = date(2026, 11, 12)
    static let START = time(10, 0)
    static let PLACE = "Meeting room 2, or by video call"
    static let PEOPLE = "Attending: Dana, Sam, Jordan, Lee · Notes: Sam"

    static let TOPICS = [
        Topic("Welcome and the notes of the last meeting", "Dana", 5),
        Topic("Contoso Books website: the wireframes", "Jordan", 20),
        Topic("New clients this month", "Lee", 20),
        Topic("Holiday schedule", "Sam", 10),
        Topic("Anything else, and the next meeting", "All", 5),
    ]

    // The actions agreed at the last meeting: what, who, and by when
    static let OPEN_ACTIONS = [
        ["Write up the interviews at Contoso Books", "Jordan", "2026-11-09"],
        ["Call the two new clients", "Lee", "2026-11-19"],
        ["Post the holiday schedule", "Sam", "2026-11-13"],
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0

    // A time of day as the team writes it, 10:05
    static let TIME = "H:mm"

    private let page: Page
    private let regular: Font
    private let semiBold: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_73.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_73.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Team meeting, " + Example_73.format(Example_73.DATE, "yyyy-MM-dd"))
        pdf.setAuthor(Example_73.TEAM)
        pdf.setLanguage("en-CA")

        regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(11.0)

        page = Page(pdf, Letter.PORTRAIT)

        // The topics, each starting when the one before it ends. The meeting
        // ends when the last one does.
        var topics = [[Cell]]()
        topics.append(Example_73.row(semiBold, ["Time", "Topic", "Who"]))
        var time = Example_73.START
        for topic in Example_73.TOPICS {
            topics.append(Example_73.row(regular, [Example_73.format(time, Example_73.TIME), topic.title, topic.leader]))
            time = time.addingTimeInterval(Double(topic.minutes * 60))
        }
        let end = time

        // The title, when and where, and a rule under them
        var y = heading("Team meeting", StructElem.H1, 18.0, 62.0)
        let when = Example_73.format(Example_73.DATE, "EEEE, MMMM d, yyyy")
                + " · " + Example_73.format(Example_73.START, Example_73.TIME)
                + " to " + Example_73.format(end, Example_73.TIME) + "\n" + Example_73.PLACE
        y = paragraph(when, y) + 12.0
        let rule = Line(Example_73.LEFT, y, Example_73.LEFT + Example_73.WIDTH, y)
        rule.setStrokeColor(Example_73.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // Who comes, and who takes the notes
        y = paragraph(Example_73.PEOPLE, y + 12.0)

        // The agenda
        y = heading("Agenda", StructElem.H2, 14.0, y + 30.0)
        y = table(topics, y, 15.0, 60.0, 25.0)

        // The actions of the last meeting, checked at its start
        var actions = [[Cell]]()
        actions.append(Example_73.row(semiBold, ["Action", "Who", "By"]))
        for action in Example_73.OPEN_ACTIONS {
            actions.append(Example_73.row(regular, action))
        }
        y = heading("Actions from the last meeting", StructElem.H2, 14.0, y + 30.0)
        table(actions, y, 60.0, 20.0, 20.0)

        try pdf.complete()
    }

    // Draws a heading in blue, its baseline at y, and returns where the part
    // under it starts.
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(semiBold, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_73.BLUE)
        heading.setLocation(Example_73.LEFT, y)
        heading.drawOn(page)
        return y + 10.0
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns where it ended.
    private func paragraph(_ text: String, _ y: Float) -> Float {
        let block = TextBlock(regular, text)
        block.setLineSpacing(1.15)
        block.setLocation(Example_73.LEFT, y)
        block.setWidth(Example_73.WIDTH)
        return block.drawOn(page)[1]
    }

    // Draws a table across the page, its first row the header, its three
    // columns in percent of the width, and returns where it ended.
    @discardableResult
    private func table(_ rows: [[Cell]], _ y: Float, _ first: Float, _ second: Float, _ third: Float) -> Float {
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_73.HEADER)
        table.setCellPadding(4.0, 3.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_73.BORDER)
        table.setWidth(Example_73.WIDTH)
        table.setColumnWidthsInPercent(first, second, third)
        table.setLocation(Example_73.LEFT, y)
        return table.drawOn(page)[1]
    }

    // A row of a table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: [String]) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            row.append(Cell(font, text))
        }
        return row
    }

    // A date, a day of the calendar with no time zone, as a LocalDate is in Java.
    private static func date(_ year: Int, _ month: Int, _ day: Int) -> Date {
        return calendar().date(from: DateComponents(year: year, month: month, day: day))!
    }

    // A time of day, on the day of the meeting, as a LocalTime is in Java.
    private static func time(_ hour: Int, _ minute: Int) -> Date {
        return calendar().date(from: DateComponents(year: 2026, month: 11, day: 12, hour: hour, minute: minute))!
    }

    // The date or the time in the pattern, in English whatever the locale
    // of the computer, as Thursday, November 12, 2026, or 10:05.
    private static func format(_ date: Date, _ pattern: String) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = calendar()
        formatter.timeZone = calendar().timeZone
        formatter.dateFormat = pattern
        return formatter.string(from: date)
    }

    // The Gregorian calendar in UTC, so that a day is a day wherever the
    // computer is.
    private static func calendar() -> Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(identifier: "UTC")!
        return calendar
    }
}   // End of Example_73.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_73()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_73 => \(String(format: "%4lld", time1 - time0)) ms")
