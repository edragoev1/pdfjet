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

        // let f1 = try Font(pdf, SourceSerif4.Regular)
        // let f1 = try Font(pdf, NotoSans.Regular)
        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(15.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var colors = [String : Int32]()
        colors["Everyone"] = Color.red
        colors["pay"] = Color.green
        colors["freedom"] = Color.blue

        // page.saveGraphicsState()

        let gs = GraphicsState()
        gs.setAlphaStroking(0.5)        // Stroking alpha
        gs.setAlphaNonStroking(0.5)     // Non-Stroking alpha
        page.setGraphicsState(gs)

        let englishText = try Content.ofTextFile("data/languages/english.txt")
        // f1.setSize(14.0)
        let textBlock = TextBlock(f1, englishText)
        textBlock.setLocation(100.0, 50.0)
        textBlock.setWidth(400.0)
        // With a height the text that does not fit is cut; without one the
        // block is as tall as its text.
        textBlock.setHeight(450.0)
        textBlock.setVerticalAlignment(Alignment.TOP)
        // textBlock.setVerticalAlignment(Alignment.BOTTOM)
        // textBlock.setVerticalAlignment(Alignment.CENTER)
        // textBlock.setTextAlignment(Alignment.CENTER)
        textBlock.setBackgroundColor(Color.whitesmoke)
        textBlock.setHighlightColors(colors)
        textBlock.setBorderColor(Color.black)
        let xy = textBlock.drawOn(page)

        page.setGraphicsState(GraphicsState())      // Reset GS
        // page.restoreGraphicsState()

        let rect = Rect(xy[0], xy[1], 20.0, 20.0)
        rect.setBorderColor(Color.black)
        rect.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_16.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_16()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_16 => \(String(format: "%4lld", time1 - time0)) ms")
