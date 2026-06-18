import Foundation
import PDFjet

/**
 * Example_08.swift
 */
public class Example_08 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_08.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        f1.setSize(7.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(7.0)

        let f3 = try Font(pdf, IBMPlexSans.BoldItalic)
        f3.setSize(7.0)

        let image = try Image(pdf, "images/TeslaX.png")
        image.scaleBy(0.20)

        let barcode = Barcode(Barcode.CODE128, "Hello, World!")
        barcode.setModuleLength(0.75)
	    // Comment out the line below if you don't want to print the text underneath the barcode.
        barcode.setFont(f1)

        let table = try Table(f1, f2, "data/Electric_Vehicle_Population_10_Pages.csv")
        table.setVisibleColumns(1, 2, 3, 4, 5, 6, 7, 9)
        table.getCellAt(4, 0).setImage(image)
        table.getCellAt(5, 0).setColSpan(8)
        table.getCellAt(5, 0).setBarcode(barcode)
        table.getCellAt(20, 0).setColSpan(6)
        table.getCellAt(20, 6).setColSpan(2)
        table.setColumnWidths()
        table.setColumnWidth(0, image.getWidth() + 4.0)
        table.setColumnWidth(3, table.getColumnWidth(3) + 10.0)
        table.setColumnWidth(5, table.getColumnWidth(5) + 10.0)
        table.rightAlignNumbers()

        table.setLocation(35.0, 35.0)
        // table.setFirstPageTopMargin(150.0)
        table.setBottomMargin(15.0)
        table.setTextColorInRow(12, Color.blue)
        table.setTextColorInRow(13, Color.red)
        table.setFontInRow(14, f3)

        var pages = [Page]()
        table.drawOn(pdf, &pages, Letter.PORTRAIT)
        for i in 0..<pages.count {
            let page = pages[i]
            try page.addFooter(TextLine(f1, "Page \(i + 1) of \(pages.count)"))
            pdf.addPage(page)
        }

        pdf.complete()
    }
}   // End of Example_08.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_08()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_08", time0, time1)
