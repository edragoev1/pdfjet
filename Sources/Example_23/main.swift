import Foundation
import PDFjet

/**
 *  Example_23.swift
 */
public class Example_23 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_23.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f2 = Font(pdf, CoreFont.HELVETICA)
        let f3 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f3.setSize(7.0 * 0.583)

        let image1 = try Image(pdf, "images/mt-map.png")
        image1.scaleBy(0.75)

        var tableData = [[Cell]]()

        var row = [Cell]()
        row.append(Cell(f1, "Hello"))
        row.append(Cell(f1, "World"))
        row.append(Cell(f1, "Next Column"))
        row.append(Cell(f1, "CompositeTextLine"))
        tableData.append(row)

        row = [Cell]()
        row.append(Cell(f2, "This is a test:"))
        // cell = Cell(f2,
        //         "Here we are going to test the wrapAroundCellText method.\n\nWe will create a table and place it near the bottom of the page. When we draw this table the text will wrap around the column edge and stay within the column.\n\nSo - let's  see how this is working?")
        let cell = Cell(f2, "Here we are going to test the AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA method.")
        cell.setColSpan(2)
        row.append(cell)
        row.append(Cell(f2))    // We need an empty cell here because the previous cell had colSpan == 2
        row.append(Cell(f2, "Test 456"))
        tableData.append(row)

        // row = [Cell]()
        // row.append(Cell(f2,
        //         // "Another row.\n\n\nMake sure that this line of text will be wrapped around correctly too."))
        //         "Another row. Make sure that this line of text will be wrapped around correctly too."))
        // row.append(Cell(f2, "Yahoo!"))
        // row.append(Cell(f2, "Test 789"))

        // let composite = CompositeTextLine(0.0, 0.0)
        // composite.setFontSize(12.0)
        // let line1 = TextLine(f2, "Composite Text Line")
        // let line2 = TextLine(f3, "Superscript")
        // let line3 = TextLine(f3, "Subscript")
        // line2.setTextEffect(Effect.SUPERSCRIPT)
        // line3.setTextEffect(Effect.SUBSCRIPT)
        // composite.addComponent(line1)
        // composite.addComponent(line2)
        // composite.addComponent(line3)

        // cell = Cell(f2)
        // cell.setCompositeTextLine(composite)
        // cell.setBgColor(Color.peachpuff)
        // row.append(cell)

        // tableData.append(row)

        let page = Page(pdf, Letter.PORTRAIT)
        let table = Table()
        table.setData(tableData, Table.WITH_1_HEADER_ROW)
        table.setLocation(50.0, 50.0)
        table.setColumnWidth(0, 100.0)
        table.setColumnWidth(1, 100.0)
        table.setColumnWidth(2, 100.0)
        table.setColumnWidth(3, 150.0)
        table.drawOn(page)

        // // let numOfPages = try table.getNumberOfPages(page)
        // while true {
        //     table.drawOn(page)
        //     if !table.hasMoreData() {
        //         break
        //     }
        //     page = Page(pdf, Letter.PORTRAIT)
        //     table.setLocation(50.0, 50.0)
        // }

        // // Populate and draw the second table.
        // tableData = [[Cell]]()

        // row = [Cell]()
        // row.append(Cell(f1))
        // row.append(Cell(f2))
        // tableData.append(row)

        // row = [Cell]()
        // row.append(Cell(f1, "Hello, World!"))
        // row.append(Cell(f2, "This is a test."))
        // tableData.append(row)

        // tableData[0][0].setImage(image1)

        // table = Table()
        // table.setData(tableData)
        // table.setLocation(50.0, 450.0)
        // table.setColumnWidth(0, 260.0)
        // table.setColumnWidth(1, 260.0)

        // var buf = String()
        // buf.append("Name: 20200306_050741\n")
        // buf.append("Recorded: 2018:09:28 18:28:43\n")

        // let textBox = TextBox(f1, buf)
        // textBox.setWidth(400.0)
        // textBox.setBorder(Border.NONE)
        // tableData[0][1].setTextBox(textBox)
        // table.drawOn(page)

        pdf.complete()
    }
}   // End of Example_23.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_23()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_23", time0, time1)
