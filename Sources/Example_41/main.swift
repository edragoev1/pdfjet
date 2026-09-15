import Foundation
import PDFjet

/**
 * Example_41.swift
 *
 * Merges existing PDF documents into one, after a cover page drawn with PDFjet.
 * The pages of each document follow in their order and keep their content,
 * resources, annotations and links. The parts of a document that belong to the
 * whole document, such as its bookmarks, form fields and tagging, are left out.
 */
public class Example_41 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_41.pdf", append: false)!)

        let fileNames = [
            "data/testPDFs/wirth.pdf",
            "data/testPDFs/rc65-16e.pdf",
            "data/testPDFs/PDFjetLogo.pdf"
        ]
        var documents = [[PDFobj]]()
        for fileName in fileNames {
            documents.append(try pdf.read(from: InputStream(fileAtPath: fileName)!))
        }

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(24.0)
        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(f1, "Merged documents").setLocation(50.0, 80.0).drawOn(page)
        var y: Float = 130.0
        for i in 0..<fileNames.count {
            let pages = pdf.getPageObjects(from: documents[i]).count
            let text = fileNames[i] + ", " + String(pages) + (pages == 1 ? " page" : " pages")
            TextLine(f2, text).setLocation(50.0, y).drawOn(page)
            y += 20.0
        }

        for objects in documents {
            try pdf.merge(objects)
        }

        try pdf.complete()
    }
}   // End of Example_41.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_41()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_41 => \(String(format: "%4lld", time1 - time0)) ms")
