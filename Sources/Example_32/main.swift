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
        let pdf = PDF(OutputStream(toFileAtPath: "Example_32.pdf", append: false)!)
        let font = try Font(pdf, JetBrainsMono.Regular)
        font.setSize(10.0)

        var colors = [String:Int32]()
        colors["new"] = Color.red
        colors["class"] = Color.blue
        colors["void"] = Color.green

        var page = Page(pdf, Letter.PORTRAIT)
        let x: Float = 50.0
        var y: Float = 50.0
        let leading = font.getBodyHeight()
        let lines = try Text.readLines("examples/Example_02.java")
        for line in lines {
            page.drawString(font, font.getSize(), line, x, y, [0.0, 0.0, 0.0], colors)
            y += leading
            if y > (page.getHeight() - 20.0) {
                page = Page(pdf, Letter.PORTRAIT)
                y = 50.0
            }
        }

        pdf.complete()
    }
}   // End of Example_32.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_32()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_32", time0, time1)
