import Foundation
import PDFjet

/**
 * Example_29.swift
 */
public class Example_29 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_29.pdf", append: false)!)

        let font = try Font(pdf, IBMPlexSans.Regular)
        font.setSize(15.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let paragraph1 = Paragraph()
        paragraph1.add(TextLine(font, try Content.ofTextFile("data/languages/english.txt")))

        let paragraph2 = Paragraph()
        paragraph2.add(TextLine(font, try Content.ofTextFile("data/languages/greek.txt")))

        let column = TextColumn()
        column.setLocation(50.0, 50.0)
        column.setWidth(400.0)
        column.addParagraph(paragraph1)
        column.addParagraph(paragraph2)
        // column.drawOn(page)

        var tableData = [[Cell]]()
        var row = [Cell]()
        row.append(Cell(font, "Hello"))
        row.append(Cell(font, "World"))
        row[1].setTextColumn(column)
        tableData.append(row)

        let table = Table()
        table.setData(tableData)
        table.setLocation(50.0, 50.0)
        table.drawOn(page)

        pdf.complete()
    }
}   // End of Example_29.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_29()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_29", time0, time1)
