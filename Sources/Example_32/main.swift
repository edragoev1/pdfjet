import Foundation
import PDFjet

///
/// Example_32.java
///
public class Example_32 {

    private var x: Float = 50.0
    private var y: Float = 50.0
    private var leading: Float = 10.0

    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_32.pdf", append: false)!

        let pdf = PDF(stream)

        // let font = Font(pdf, CoreFont.COURIER)
        let font = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream")!,
                Font.STREAM)
        font.setSize(8.0)

        let lines = try Text.readLines("Sources/Example_02/main.swift")
        var page: Page?
        for line in lines {
            if page == nil {
                y = 50.0
                page = try newPage(pdf, font)
            }
            page!.printString(String(line))
            page!.newLine()
            y += leading
            if y > (Letter.PORTRAIT[1] - 20.0) {
                page!.setTextEnd()
                page = nil
            }
        }
        if page != nil {
            page!.setTextEnd()
        }

        pdf.complete()
    }

    private func newPage(_ pdf: PDF, _ font: Font) throws -> Page {
        let page = Page(pdf, Letter.PORTRAIT)
        page.setTextStart()
        page.setTextFont(font)
        page.setTextLocation(x, y)
        page.setTextLeading(leading)
        return page
    }

}   // End of Example_32.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_32()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_32 => \(time1 - time0)")
