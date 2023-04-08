import Foundation
import PDFjet

/**
 *  Example_73.swift
 */
public class Example_73 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_73.pdf", append: false)!)
        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/Droid/DroidSans.ttf.stream")!,
                Font.STREAM)

        let f2 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/Droid/DroidSansFallback.ttf.stream")!,
                Font.STREAM)

        f1.setSize(12.0)
        f2.setSize(12.0)

        let line1 = TextLine(f1, "Hello, Beautiful World")
        let line2 = TextLine(f1, "Hello,BeautifulWorld")

        var textBox = TextBox(f1, line1.getText()!)
        textBox.setMargin(0.0)
        textBox.setLocation(50.0, 50.0)
        textBox.setWidth(line1.getWidth() + 2*textBox.getMargin())
        textBox.setBgColor(Color.lightgreen)
        // The drawOn method returns the x and y of the bottom right corner of the TextBox
        var xy = textBox.drawOn(page)

        var box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        textBox = TextBox(f1, line1.getText()! + "!")
        textBox.setWidth(line1.getWidth() + 2.0*textBox.getMargin())
        textBox.setLocation(50.0, 100.0)
        xy = textBox.drawOn(page)

        box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)
        
        textBox = TextBox(f1, line2.getText()!)
        textBox.setWidth(line2.getWidth() + 2.0*textBox.getMargin())
        textBox.setLocation(50.0, 200.0)
        xy = textBox.drawOn(page)

        box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        textBox = TextBox(f1, line2.getText()! + "!")
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin())
        textBox.setLocation(50.0, 300.0)
        xy = textBox.drawOn(page)

        box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        textBox = TextBox(f1, line2.getText()! + "! Left Align")
        textBox.setMargin(10.0)
        textBox.setWidth(line2.getWidth() + 2*textBox.getMargin())
        textBox.setLocation(50.0, 400.0)
        xy = textBox.drawOn(page)

        box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        textBox = TextBox(f1, line2.getText()! + "! Right Align")
        textBox.setMargin(10.0)
        textBox.setTextAlignment(Align.RIGHT)
        textBox.setWidth(line2.getWidth() + 2.0*textBox.getMargin())
        textBox.setLocation(50.0, 500.0)
        xy = textBox.drawOn(page)

        box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        textBox = TextBox(f1, line2.getText()! + "! Center")
        textBox.setMargin(10.0)
        textBox.setTextAlignment(Align.CENTER)
        textBox.setWidth(line2.getWidth() + 2.0*textBox.getMargin())
        textBox.setLocation(50.0, 600.0)
        xy = textBox.drawOn(page)

        box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)

        let text = try Content.ofTextFile("data/chinese-text.txt")

        textBox = TextBox(f1)
        textBox.setFallbackFont(f2)
        textBox.setText(text)
        // textBox.setMargin(10.0)
        textBox.setBgColor(Color.lightblue)
        textBox.setVerticalAlignment(Align.TOP)
        // textBox.setHeight(210.0)
        // textBox.setHeight(151.0)
        textBox.setHeight(14.0)
        textBox.setWidth(300.0)
        textBox.setLocation(250.0, 50.0)
        textBox.drawOn(page)

        textBox = TextBox(f1)
        textBox.setFallbackFont(f2)
        textBox.setText(text)
        // textBox.setMargin(10.0)
        textBox.setBgColor(Color.lightblue)
        textBox.setVerticalAlignment(Align.CENTER)
        // textBox.setHeight(210.0)
        textBox.setHeight(151.0)
        textBox.setWidth(300.0)
        textBox.setLocation(250.0, 300.0)
        textBox.drawOn(page)

        textBox = TextBox(f1)
        textBox.setFallbackFont(f2)
        textBox.setText(text)
        // textBox.setMargin(10.0)
        textBox.setBgColor(Color.lightblue)
        textBox.setVerticalAlignment(Align.BOTTOM)
        // textBox.setHeight(210.0)
        textBox.setHeight(151.0)
        textBox.setWidth(300.0)
        textBox.setLocation(250.0, 550.0)
        textBox.drawOn(page)

        pdf.complete()
    }
}   // End of Example_73.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_73()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_73 => \(time1 - time0)")
