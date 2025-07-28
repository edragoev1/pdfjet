import Foundation
import PDFjet


/**
 *  Example_48.swift
 */
public class Example_48 {

    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_48.pdf", append: false)

        let pdf = PDF(stream!)

        let f1 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")!,
                Font.STREAM)

        var page = Page(pdf, Letter.PORTRAIT)

        let toc = Bookmark(pdf)

        let x: Float = 70.0
        var y: Float = 50.0
        let offset: Float = 50.0

        y += 30.0
        var title = Title(f1, "This is a test!", x, y)
        toc.addBookmark(page, title)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "General", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "File Header", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "File Body", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Cross-Reference Table", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        y = 50.0
        title = Title(f1, "File Trailer", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Incremental Updates", x, y).setOffset(offset)
        var bm = toc.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Hello", x, y).setOffset(offset)
        bm = bm.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "World", x, y).setOffset(offset)
        bm = bm.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Yahoo!!", x, y).setOffset(offset)
        bm.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "Test Test Test ...", x, y).setOffset(offset)
        bm.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        bm = bm.getParent()!
        title = Title(f1, "Let's see ...", x, y).setOffset(offset)
        bm.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "One more item.", x, y).setOffset(offset)
        toc.addBookmark(page, title).autoNumber(title.prefix!)
        title.drawOn(page)

        y += 30.0
        title = Title(f1, "The End :)", x, y)
        toc.addBookmark(page, title)
        title.drawOn(page)

        pdf.complete()
    }

}   // End of Example_48.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_48()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_48", time0, time1)
