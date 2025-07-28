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
        let font = try Font(pdf, "fonts/JetBrainsMono/JetBrainsMono-Regular.ttf.stream")
        font.setSize(8.0)

        var colors = [String:Int32]()
        colors["new"] = Color.red
        colors["ArrayList"] = Color.blue
        colors["List"] = Color.blue
        colors["String"] = Color.blue
        colors["Field"] = Color.blue
        colors["Form"] = Color.blue
        colors["Smart"] = Color.green
        colors["Widget"] = Color.green
        colors["Point"] = Color.green

        var page = Page(pdf, Letter.PORTRAIT)
        let x: Float = 50.0
        var y: Float = 50.0
        let leading = font.getBodyHeight()
        let lines = try Text.readLines("Sources/Example_02/main.swift")
        for line in lines {
            page.drawString(font, nil, line, x, y, Color.black, colors)
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
