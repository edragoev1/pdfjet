/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_29.swift
 * This example draws a table whose cells hold text columns: each paragraph of
 * English and Greek text wraps inside its cell, and the cell grows to fit it.
 */
public class Example_29 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_29.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let text = TextLine(f2, "Text Columns in Table Cells")
        text.setFontSize(22.0)
        text.setLocation(50.0, 70.0)
        text.drawOn(page)

        let languages = ["English", "Greek"]
        let files = ["data/languages/english.txt", "data/languages/greek.txt"]

        var tableData = [[Cell]]()

        var row = [Cell]()
        row.append(Cell(f2, "Language"))
        row.append(Cell(f2, "Text"))
        tableData.append(row)

        for i in 0..<languages.count {
            // Each line of the file after the first two is a paragraph.
            let lines = try Content.linesOfTextFile(files[i])
            let column = TextColumn()
            column.setWidth(400.0)
            for j in 2..<lines.count {
                let paragraph = Paragraph()
                paragraph.add(TextLine(f1, lines[j]))
                column.addParagraph(paragraph)
            }

            row = [Cell]()
            row.append(Cell(f1, languages[i]))
            row.append(Cell(f1, ""))
            row[1].setTextColumn(column)
            tableData.append(row)
        }

        let table = Table()
        table.setTableData(tableData, 1)
        table.setColumnWidth(0, 90.0)
        table.setColumnWidth(1, 420.0)
        table.setLocation(50.0, 100.0)
        table.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_29.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_29()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_29 => \(String(format: "%4lld", time1 - time0)) ms")
