import Foundation
import PDFjet

/**
 * Example_16.swift
 */
public class Example_16 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_16.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Text block with highlighted keywords")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(15.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var colors = [String : Int32]()
        colors["Everyone"] = Color.red
        colors["pay"] = Color.green
        colors["freedom"] = Color.blue

        let gs = GraphicsState()
        gs.setAlphaStroking(0.5)        // Stroking alpha
        gs.setAlphaNonStroking(0.5)     // Non-Stroking alpha
        page.setGraphicsState(gs)

        let englishText = try Content.ofTextFile("data/languages/english.txt")
        let textBox = TextBox(f1, englishText)
        textBox.setLocation(100.0, 50.0)
        textBox.setWidth(400.0)
        // If no height is specified the height will be calculated based on the text.
        textBox.setHeight(450.0)

        textBox.setVerticalAlignment(Align.TOP)

        textBox.setBackgroundColor(Color.whitesmoke)
        textBox.setTextColors(colors)
        textBox.setBorders(true)
        let xy = textBox.drawOn(page)

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
