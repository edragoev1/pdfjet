import Foundation
import PDFjet

/**
 *  Example_16.java
 *
 */
public class Example_16 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_16.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA)
        f1.setSize(14.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var colors = [String : Int32]()
        colors["Lorem"] = Color.blue
        colors["ipsum"] = Color.red
        colors["dolor"] = Color.green
        colors["ullamcorper"] = Color.gray

        let gs = GraphicsState()
        gs.setAlphaStroking(0.5)        // Stroking alpha
        gs.setAlphaNonStroking(0.5)     // Nonstroking alpha
        page.setGraphicsState(gs)

        f1.setSize(72.0)
        let text = TextLine(f1, "Hello, World")
        text.setLocation(50.0, 300.0)
        text.drawOn(page)

        let latinText = try String(contentsOfFile: "data/latin.txt", encoding: String.Encoding.utf8)

        f1.setSize(14.0)
        let textBox = TextBox(f1, latinText)
        textBox.setLocation(50.0, 50.0)
        textBox.setWidth(400.0)
        // If no height is specified the height will be calculated based on the text.
        // textBox.setHeight(400.0)
        // textBox.setVerticalAlignment(Align.TOP)
        // textBox.setVerticalAlignment(Align.BOTTOM)
        // textBox.setVerticalAlignment(Align.CENTER)
        textBox.setBgColor(Color.whitesmoke);
        textBox.setTextColors(colors)
        textBox.setBorder(Border.ALL)
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
print("Example_16 => \(time1 - time0)")
