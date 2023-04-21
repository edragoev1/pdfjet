import Foundation
import PDFjet

/**
 *  Example_08.swift
 *
 */
public class Example_08 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_08.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setSize(7.0)

        let f2 = Font(pdf, CoreFont.HELVETICA)
        f2.setSize(7.0)

        let f3 = Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE)
        f3.setSize(7.0)

        let stream2 = InputStream(fileAtPath: "images/fruit.jpg")
        let image1 = try Image(pdf, stream2!, ImageType.JPG)
        image1.scaleBy(0.20)

        let barCode = BarCode(BarCode.CODE128, "Hello, World!")
        barCode.setModuleLength(0.75)
        // Uncomment the line below if you want to print the text underneath the barcode.
        // barCode.setFont(f1)

        let table = Table();
        let tableData = try getData(
        		"data/world-communications.txt", "|", Table.DATA_HAS_2_HEADER_ROWS, f1, f2, image1, barCode)

        table.setData(tableData, Table.DATA_HAS_2_HEADER_ROWS)
        table.removeLineBetweenRows(0, 1)
        table.setLocation(100.0, 0.0)
        table.setRightMargin(20.0)
        table.setBottomMargin(0.0)
        table.setCellBordersWidth(0.0)
        table.setTextColorInRow(12, Color.blue)
        table.setTextColorInRow(13, Color.red)
        // TODO:
        table.getCellAt(13, 0).getTextBlock()!.setURIAction("http://pdfjet.com")
        table.setFontInRow(14, f3)
        table.getCellAt(21, 0).setColSpan(6)
        table.getCellAt(21, 6).setColSpan(2)

        // Set the column widths manually:
        // table.setColumnWidth(0, 70f)
        // table.setColumnWidth(1, 50f)
        // table.setColumnWidth(2, 70f)
        // table.setColumnWidth(3, 70f)
        // table.setColumnWidth(4, 70f)
        // table.setColumnWidth(5, 70f)
        // table.setColumnWidth(6, 50f)
        // table.setColumnWidth(7, 50f)

        // Auto adjust the column widths to be just wide enough to fit the text without truncation.
        // Columns with colspan > 1 will not be adjusted.
        // table.autoAdjustColumnWidths()

        // Auto adjust the column widths in a way that allows the table to fit perfectly on the page.
        // Columns with colspan > 1 will not be adjusted.
        table.fitToPage(Letter.PORTRAIT)

        var pages = [Page]()
        table.drawOn(pdf, &pages, Letter.PORTRAIT)
        for i in 0..<pages.count {
            let page = pages[i]
            // try page.addFooter(TextLine(f1, "Page \(i + 1) of \(pages.count)"))
            pdf.addPage(page)
        }

        pdf.complete()
    }

    public func getTextData(_ fileName: String, _ delimiter: String) throws -> [[String]] {
        var tableTextData = [[String]]()
        let lines = (try String(contentsOfFile:
                fileName, encoding: .utf8)).components(separatedBy: "\n")
        for line1 in lines {
            let line = line1.trimmingCharacters(in: .newlines)
            if line == "" {
                continue
            }
            var cols: [String]?
            if delimiter == "|" {
                cols = line.components(separatedBy: "|")
            }
            else if delimiter == "\t" {
                cols = line.components(separatedBy: "\t")
            }
            else {
                print("Only pipes and tabs can be used as delimiters.")
            }
            tableTextData.append(cols!)
        }
        return tableTextData
    }

    public func getData(
            _ fileName: String,
            _ delimiter: String,
            _ numOfHeaderRows: Int,
            _ f1: Font,
            _ f2: Font,
            _ image: Image,
            _ barCode: BarCode) throws -> [[Cell]] {
        var tableData = [[Cell]]()

        let tableTextData = try getTextData(fileName, delimiter)
        var currentRow = 0
        for rowData in tableTextData {        	
        	var row = [Cell]()
            for i in 0..<rowData.count {
            	let text = rowData[i].trim()
                if currentRow < numOfHeaderRows {
                    row.append(Cell(f1, text))
                }
                else {
                    let cell = Cell(f2)
                    if i == 0 && currentRow == 5 {
                        cell.setImage(image)
                    }
                    if i == 0 && currentRow == 6 {
                        cell.setBarcode(barCode)
                        cell.setTextAlignment(Align.CENTER)
                        cell.setColSpan(8)
                    }
                    else {
                        let textBlock = TextBlock(f2, text)
                        if i == 0 {
                            textBlock.setTextAlignment(Align.LEFT)
                        }
                        else {
                            textBlock.setTextAlignment(Align.RIGHT)
                        }
                        cell.setTextBlock(textBlock)
                    }
                    row.append(cell)
                }
            }
            tableData.append(row)
            currentRow += 1
        }
        return tableData
    }

    private func appendMissingCells(_ tableData: inout [[Cell]], _ f2: Font) {
        let numOfColumns = tableData[0].count
        var i = 0
        while i < tableData.count {
            let numOfCells = tableData[i].count
            if numOfCells < numOfColumns {
                for _ in 0..<(numOfColumns - numOfCells) {
                    tableData[i].append(Cell(f2))
                }
                tableData[i][numOfCells - 1].setColSpan(
                        UInt32(numOfColumns - numOfCells) + UInt32(1))
            }
            i += 1
        }
    }
}   // End of Example_08.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_08()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_08 => \(time1 - time0)")
