/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_83.swift
 * A volunteer sign-up sheet, to print and put on a clipboard at the front
 * desk: the event at the top, and a table of numbered empty rows in which
 * each volunteer writes their name and how to reach them, ticks the times
 * they are free, and says what they would like to help with.
 *
 * This is a form people fill in, not a document with data. The template of
 * PDFjet Forms is a form for one volunteer, filled in on screen; on paper
 * the same questions go on one sheet for many people, a row for each, which
 * is how a sign-up is done at a table or a door. So the form is a Table
 * whose rows are empty: they are padded to about the height of a box, room
 * enough for handwriting, and the columns for the times are narrow, for a
 * tick. The page is Letter landscape, for the width of the columns.
 *
 * The table is drawn with drawOn(pdf, page, &pages, Letter.LANDSCAPE), so a
 * sheet of more rows than a page holds goes on to the next pages, with its
 * header row on each, and every page says "Page i of N". The pages are kept
 * in a list and added to the document at the end, when their number is
 * known.
 *
 * The document is PDF/UA-1: the title is a heading, and the sheet a table
 * that a screen reader reads by its column headers. The layout follows the
 * volunteer sign-up template of PDFjet Forms. The food bank is made up.
 */
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_83 {
    static let ORGANIZATION = "Harbourview Food Bank"
    static let EVENT = "Holiday food drive, December 5 to 19, 2026"
    static let INTRO = "Thank you for offering your time. Write your name and how "
            + "to reach you, and tick the times you are free: the mornings, afternoons and "
            + "evenings are those of weekdays. Under \"Help with\", write what you would like "
            + "to do: sorting, the front desk, driving, or anything. Our coordinator calls you "
            + "within a week."

    // The rows of the sheet, as many as fit on the page. More go on to the
    // next page.
    static let ROWS = 13

    static let BLUE: Int32 = 0x1f58b0
    static let RULE: Int32 = 0xdde1e8
    static let HEADER: Int32 = 0xe3edf9
    static let BORDER: Int32 = 0x4d4d4d

    static let LEFT: Float = 48.0
    static let TOP: Float = 48.0
    static let WIDTH: Float = 696.0            // 792 less two margins of 48
    static let FOOTER_ROOM: Float = 56.0   // the bottom margin, clear of "Page i of N"

    public init() throws {
        guard let output = OutputStream(toFileAtPath: "Example_83.pdf", append: false) else {
            throw ExampleError(message: "Cannot open Example_83.pdf for writing")
        }
        let pdf = PDF(output)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Volunteer sign-up")
        pdf.setAuthor(Example_83.ORGANIZATION)
        pdf.setLanguage("en-CA")

        let regular = try Font(pdf, IBMPlexSans.Regular)
        regular.setSize(11.0)
        let semiBold = try Font(pdf, IBMPlexSans.SemiBold)
        semiBold.setSize(10.0)

        let left = Example_83.LEFT
        let width = Example_83.WIDTH

        var pages = [Page]()
        let page = Page(pdf, Letter.LANDSCAPE, Page.DETACHED)
        pages.append(page)

        // The title and the event on one line, what to write, and a rule
        let title = TextLine(semiBold, "Volunteer sign-up")
        title.setStructureType(StructElem.H1)
        title.setFontSize(20.0)
        title.setTextColor(Example_83.BLUE)
        title.setLocation(left, 62.0)
        title.drawOn(page)
        let event = TextLine(regular, Example_83.ORGANIZATION + " · " + Example_83.EVENT)
        event.setLocation(left + width - event.getWidth(), 62.0)
        event.drawOn(page)

        let intro = TextBlock(regular, Example_83.INTRO)
        intro.setLineSpacing(1.15)
        intro.setLocation(left, 74.0)
        intro.setWidth(width)
        let y = intro.drawOn(page)[1] + 12.0
        let rule = Line(left, y, left + width, y)
        rule.setStrokeColor(Example_83.RULE)
        page.addArtifactBMC()
        rule.drawOn(page)
        page.addEMC()

        // The sheet: a header row, and numbered empty rows to write in
        var rows = [[Cell]]()
        rows.append(Example_83.row(semiBold, "No.", "Name", "Phone or email",
                "Mornings", "Afternoons", "Evenings", "Weekends", "Help with"))
        for i in 1...Example_83.ROWS {
            rows.append(Example_83.row(regular, String(i), "", "", "", "", "", "", ""))
        }

        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(semiBold, Color.black, Example_83.HEADER)
        // The rows about as tall as a box of a form, the header row less so
        table.setCellPadding(5.0, 7.0)
        for cell in table.getRow(0) {
            cell.setTopPadding(4.0)
            cell.setBottomPadding(4.0)
        }
        table.setCellBorderWidth(0.5)
        table.setCellBorderColor(Example_83.BORDER)
        table.setWidth(width)
        table.setColumnWidthsInPercent(5.0, 22.0, 22.0, 9.0, 9.0, 9.0, 9.0, 15.0)
        table.setTextAlignmentInColumn(0, Alignment.RIGHT)
        table.setTextColorInColumn(0, Color.dimgray)
        table.setLocation(left, Example_83.TOP)
        table.setFirstPageTopMargin(y + 16.0)
        table.setBottomMargin(Example_83.FOOTER_ROOM)
        table.drawOn(pdf, page, &pages, Letter.LANDSCAPE)

        // The footer, on every page, now that their number is known
        for i in 0..<pages.count {
            let p = pages[i]
            let footer = TextLine(regular, "Page " + String(i + 1) + " of " + String(pages.count))
            footer.setFontSize(10.0)
            footer.setLocation(left + width - footer.getWidth(), p.getHeight() - 30.0)
            p.addArtifactBMC()
            footer.drawOn(p)
            p.addEMC()
        }
        pdf.addPages(pages)
        try pdf.complete()
    }

    // A row of the table, a cell in the font for each text.
    private static func row(_ font: Font, _ texts: String...) -> [Cell] {
        var row = [Cell]()
        for text in texts {
            row.append(Cell(font, text))
        }
        return row
    }
}   // End of Example_83.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_83()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_83 => \(String(format: "%4lld", time1 - time0)) ms")
