/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_71.swift
 * A monthly safety inspection checklist to print and take on a walk through
 * a building: who inspects, when and where, then for each area a row for
 * each item to check, with a box to tick when it is in order, a box when it
 * needs fixing, and room for a note; under the list, the problems found, the
 * date of the next inspection, and the signature.
 *
 * This is a form, not a finished document: the inspector fills it in. The
 * fields above and below the list are drawn as in the job application of
 * Example_72, a label above each box. The list is a Table: the items come
 * from an array, each area is a row that spans the four columns, and each
 * box to tick is a square, a Rect placed in its cell with setDrawable. The
 * table goes on to as many pages as it needs, with its header row on each,
 * so a building with sixty items prints on two pages with the same code;
 * the fields under it follow the last row, or go to a page of their own
 * when they no longer fit.
 *
 * The document is PDF/UA-1: the title is a heading, the list is a table
 * that a screen reader reads by its column headers, and the boxes, of the
 * fields and to tick, are artifacts. The layout is that of the inspection
 * checklist template of PDFjet Forms. The building is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_71 {
    static let SITE = "Pinecrest Community Center"

    // The areas to inspect, each its name and then its items
    static let AREAS = [
        ["Exits and fire safety",
            "Exits and hallways are clear",
            "Exit signs are lit",
            "Fire extinguishers are in place and charged",
            "Smoke alarms were tested"],
        ["Work areas",
            "Floors are dry and free of trip hazards",
            "Electrical cords are in good shape",
            "Shelves and storage are stable",
            "The first aid kit is stocked"],
    ]

    static let BLUE: Int32 = 0x1f58b0
    static let RED: Int32 = 0xb00020
    static let RULE: Int32 = 0xdde1e8
    static let BOX: Int32 = 0x4d4d4d
    static let HEADER: Int32 = 0xe3edf9
    static let AREA: Int32 = 0xf1f3f6

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    static let LEFT: Float = 48.0
    static let WIDTH: Float = 516.0
    static let GAP: Float = 12.0
    static let HALF: Float = (WIDTH - GAP) / 2.0

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    static let BOX_HEIGHT: Float = 24.0
    static let LINE_HEIGHT: Float = 20.0

    // The side of a box to tick, about the height of the capital letters
    static let TICK: Float = 11.0

    // Room at the foot of each page for the footer, and the height of the
    // fields under the list
    static let FOOTER_ROOM: Float = 72.0   // the bottom margin, clear of "Page i of N"
    static let FIELDS_HEIGHT: Float = 170.0

    private var page: Page!
    private let labelFont: Font
    private let textFont: Font

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_71.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_71.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Monthly safety inspection")
        pdf.setAuthor(Example_71.SITE)
        pdf.setLanguage("en-US")

        textFont = try Font(pdf, IBMPlexSans.Regular)
        textFont.setSize(11.0)
        labelFont = try Font(pdf, IBMPlexSans.SemiBold)
        labelFont.setSize(10.0)
        let italic = try Font(pdf, IBMPlexSans.Italic)
        italic.setSize(10.0)

        let left = Example_71.LEFT
        let width = Example_71.WIDTH
        let half = Example_71.HALF
        let gap = Example_71.GAP

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        var pages = [Page]()
        page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pages.append(page)

        // The title, how to use the list, and a rule
        heading("Monthly safety inspection", StructElem.H1, 20.0, 62.0)
        let intro = TextBlock(textFont, "Walk through each area, tick what is in "
                + "order, and note anything that needs fixing.")
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        var y = intro.drawOn(page)[1] + 12.0
        rule(y)

        TextLine(italic, "Fields marked * are required.")
                .setTextColor(Example_71.RED)
                .setLocation(left, y + 20.0)
                .drawOn(page)

        // Where, who and when, a row of a half and two quarters. The list is
        // printed for one building, so its name is filled in already.
        y += 40.0
        let quarter = (half - gap) / 2.0
        field("Location *", left, y, half, 1, Example_71.SITE)
        field("Inspected by *", left + half + gap, y, quarter, 1, nil)
        y = field("Date *", left + half + gap + quarter + gap, y, quarter, 1, nil)

        // The list: a header row, then each area and its items. The row of an
        // area is kept on the page of its first item.
        var rows = [[Cell]]()
        var areaRows = [Int]()
        rows.append([Cell(labelFont, "Item"), Cell(labelFont, "OK"),
                Cell(labelFont, "To fix"), Cell(labelFont, "Notes")])
        for area in Example_71.AREAS {
            // The name of the area across the row; a cell that spans columns
            // is followed by empty ones that it covers
            let name = Cell(labelFont, area[0]).setColSpan(4).setBackgroundColor(Example_71.AREA)
            areaRows.append(rows.count)
            rows.append([name, Cell(textFont), Cell(textFont), Cell(textFont)])
            for i in 1..<area.count {
                rows.append([Cell(textFont, area[i]), Example_71.tick(textFont), Example_71.tick(textFont),
                        Cell(textFont)])
            }
        }

        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(labelFont, Color.black, Example_71.HEADER)
        table.setCellPadding(5.0, 6.0)
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_71.BOX)
        table.setWidth(width)
        table.setColumnWidthsInPercent(50.0, 9.0, 9.0, 32.0)
        table.setTextAlignmentInColumn(1, Alignment.CENTER)
        table.setTextAlignmentInColumn(2, Alignment.CENTER)
        for index in areaRows {
            table.keepRowWithNext(index)
        }
        // Under the fields on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.setLocation(left, 48.0)
        table.setFirstPageTopMargin(y + 6.0)
        table.setBottomMargin(Example_71.FOOTER_ROOM)
        let end = table.drawOn(pdf, page, &pages, Letter.PORTRAIT)!
        page = pages[pages.count - 1]

        // The fields under the last row. When they no longer fit on its page,
        // they go on a page of their own.
        y = end[1] + 18.0
        if y + Example_71.FIELDS_HEIGHT > page.getHeight() - Example_71.FOOTER_ROOM {
            page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
            pages.append(page)
            y = 48.0
        }
        y = field("Problems found, and who will fix them", left, y, width, 3, nil)
        CheckBox(textFont, "I confirm this inspection and sign below. *")
                .setBorderColor(Example_71.BOX)
                .setLocation(left, y)
                .drawOn(page)
        y += 30.0
        field("Inspector's signature *", left, y, half, 1, nil)
        field("Date *", left + half + gap, y, quarter, 1, nil)
        field("Next inspection", left + half + gap + quarter + gap, y, quarter, 1, nil)

        // The footer, on every page, now that their number is known
        for i in 0..<pages.count {
            let p = pages[i]
            let footer = TextLine(textFont, Example_71.SITE + ", monthly safety inspection, page "
                    + String(i + 1) + " of " + String(pages.count))
            footer.setFontSize(9.0)
            footer.setTextColor(Color.dimgray)
            footer.setLocation(left, p.getHeight() - 36.0)
            p.addArtifactBMC()
            footer.drawOn(p)
            p.addEMC()
        }
        pdf.addPages(pages)
        try pdf.complete()
    }

    // A cell with a box to tick: a square, placed in the cell with
    // setDrawable and centered as the column is. A Rect is drawn as an
    // artifact; the column header says what a tick in it means.
    private static func tick(_ font: Font) -> Cell {
        let box = Rect(0.0, 0.0, TICK, TICK)
        box.setBorderColor(BOX)
        box.setBorderWidth(0.75)
        return Cell(font).setDrawable(box)
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    @discardableResult
    private func heading(_ text: String, _ level: StructElem, _ size: Float, _ y: Float) -> Float {
        let heading = TextLine(labelFont, text)
        heading.setStructureType(level)
        heading.setFontSize(size)
        heading.setTextColor(Example_71.BLUE)
        heading.setLocation(Example_71.LEFT, y)
        heading.drawOn(page)
        return y + 16.0
    }

    // Draws the label of a field, its top at y.
    private func label(_ text: String, _ x: Float, _ y: Float) {
        let label = TextLine(labelFont, text)
        label.setLocation(x, y + labelFont.getAscent())
        label.drawOn(page)
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    @discardableResult
    private func field(
            _ text: String, _ x: Float, _ y: Float, _ width: Float, _ lines: Int, _ value: String?) -> Float {
        label(text, x, y)
        let top = y + 16.0
        let height = Example_71.BOX_HEIGHT + Float(lines - 1) * Example_71.LINE_HEIGHT
        let box = Rect(x, top, width, height)
        box.setBorderColor(Example_71.BOX)
        box.setBorderWidth(0.5)
        box.drawOn(page)
        if let value = value {
            let line = TextLine(textFont, value)
            line.setTextColor(Example_71.BLUE)
            line.setLocation(x + 6.0, top + 16.0)
            line.drawOn(page)
        }
        return top + height + 12.0
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private func rule(_ y: Float) {
        let line = Line(Example_71.LEFT, y, Example_71.LEFT + Example_71.WIDTH, y)
        line.setStrokeColor(Example_71.RULE)
        page.addArtifactBMC()
        line.drawOn(page)
        page.addEMC()
    }
}   // End of Example_71.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_71()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_71 => \(String(format: "%4lld", time1 - time0)) ms")
