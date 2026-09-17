/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_48.swift
 * This example draws the outline of a short guide to the structure of a PDF
 * file, and adds a bookmark for each of its titles. The numbers of the titles
 * come from their place in the tree of bookmarks.
 */
public class Example_48 {

    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_48.pdf", append: false)

        let pdf = PDF(stream!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("The structure of a PDF file")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(14.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(20.0)

        var page = Page(pdf, Letter.PORTRAIT)

        let toc = Bookmark(pdf)

        let x: Float = 70.0
        var y: Float = 80.0
        let offset: Float = 50.0

        // A bookmark without a number.
        var title = Title(f2, "The structure of a PDF file", x, y)
        toc.addBookmark(page, title)
        title.drawOn(page)

        y += 50.0
        title = Title(f1, "File header", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "File body", x, y).setOffset(offset)
        let body = toc.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        // Bookmarks nested in "File body".
        y += 30.0
        title = Title(f1, "Objects", x, y).setOffset(offset)
        body.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Streams", x, y).setOffset(offset)
        body.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Cross-reference table", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "File trailer", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        y = 80.0
        title = Title(f1, "Incremental updates", x, y).setOffset(offset)
        var bm = toc.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "New and changed objects", x, y).setOffset(offset)
        bm = bm.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        // Two levels down.
        y += 30.0
        title = Title(f1, "Changed objects keep their numbers", x, y).setOffset(offset)
        bm.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Deleted objects are marked as free", x, y).setOffset(offset)
        bm.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        // Back up one level.
        y += 30.0
        bm = bm.getParent()!
        title = Title(f1, "A new cross-reference section", x, y).setOffset(offset)
        bm.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "A new trailer", x, y).setOffset(offset)
        bm.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Linearized files", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.getPrefix())
        title.drawOn(page)

        y += 50.0
        title = Title(f2, "Summary", x, y)
        toc.addBookmark(page, title)
        title.drawOn(page)

        try pdf.complete()
    }

}   // End of Example_48.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_48()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_48 => \(String(format: "%4lld", time1 - time0)) ms")
