import Foundation
import PDFjet

/**
 * Example_16.swift
 */
public class Example_16 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_16.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)

        let page = Page(pdf, Letter.PORTRAIT)

        var colors = [String : Int32]()
        colors["Lorem"] = Color.blue
        colors["ipsum"] = Color.red
        colors["dolor"] = Color.green
        colors["ullamcorper"] = Color.gray

        let gs = GraphicsState()
        gs.setAlphaStroking(0.5)        // Stroking alpha
        gs.setAlphaNonStroking(0.5)     // Non-Stroking alpha
        page.setGraphicsState(gs)

        // f1.setSize(72.0)
        // let text = TextLine(f1, "Hello, World")
        // text.setLocation(50.0, 300.0)
        // text.drawOn(page)

        let latinText = try String(contentsOfFile: "data/languages/english.txt", encoding: String.Encoding.utf8)

        f1.setSize(15.0)
        let textBlock = TextBlock(f1, latinText)
        textBlock.setLocation(100.0, 50.0)
        textBlock.setWidth(400.0)
        // If no height is specified the height will be calculated based on the text.
        // textBlock.setHeight(450.0)

        // textBox.setTextDirection(Direction.TOP_TO_BOTTOM)
        // textBox.setTextDirection(Direction.BOTTOM_TO_TOP)
        // textBox.setVerticalAlignment(Align.TOP)
        // textBox.setVerticalAlignment(Align.BOTTOM)
        // textBox.setVerticalAlignment(Align.CENTER)
        // textBlock.setBackgroundColor(Color.whitesmoke)
        // textBlock.setTextColors(colors)
        textBlock.setBorderColor(Color.blue)
        let xy = textBlock.drawOn(page)

        page.setGraphicsState(GraphicsState())      // Reset GS

        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        pdf.complete()
    }
}   // End of Example_16.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_16()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_16", time0, time1)
