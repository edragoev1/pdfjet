import Foundation
import PDFjet

/**
 *  Example_73.swift
 */
public class Example_73 {

    public init() throws {

        let stream = OutputStream(toFileAtPath: "Example_73.pdf", append: false)

        let pdf = PDF(stream!)

        let f1 = Font(pdf, CoreFont.HELVETICA)

        let f2 = try Font(
                pdf,
                InputStream(fileAtPath: "fonts/OpenSans/OpenSans-Regular.ttf.stream")!,
                Font.STREAM)

        var fileName = "images/linux-logo.png"
        let file1 = try EmbeddedFile(
                pdf,
                fileName,
                InputStream(fileAtPath: fileName)!,
                false)      // Don't compress images.

        fileName = "Example_02.java"
        let file2 = try EmbeddedFile(
                pdf,
                fileName,
                InputStream(fileAtPath: fileName)!,
                true)       // Compress text files.

        let page = Page(pdf, Letter.PORTRAIT)

        f1.setSize(10.0)

        var attachment = FileAttachment(pdf, file1)
        attachment.setLocation(0.0, 0.0)
        attachment.setIconPushPin()
        attachment.setTitle("Attached File: " + file1.getFileName())
        attachment.setDescription(
                "Right mouse click or double click on the icon to save the attached file.")
        var xy = attachment.drawOn(page)

        attachment = FileAttachment(pdf, file2)
        attachment.setLocation(0.0, xy[1])
        attachment.setIconPaperclip()
        attachment.setTitle("Attached File: " + file2.getFileName())
        attachment.setDescription(
                "Right mouse click or double click on the icon to save the attached file.")
        xy = attachment.drawOn(page)

        var checkbox = CheckBox(f1, "Hello")
        checkbox.setLocation(0.0, xy[1])
        checkbox.setCheckmark(Color.blue)
        checkbox.check(Mark.CHECK)
        checkbox.setURIAction("https://pdfjet.com/os/edition.html")
        xy = checkbox.drawOn(page)

        checkbox = CheckBox(f1, "Hello")
        checkbox.setLocation(0.0, xy[1])
        checkbox.setCheckmark(Color.blue)
        checkbox.check(Mark.X)
        checkbox.setURIAction("https://pdfjet.com/java")
        xy = checkbox.drawOn(page)

        let box = Box()
        box.setLocation(0.0, xy[1])
        box.setSize(20.0, 20.0)
        xy = box.drawOn(page)

        let radiobutton = RadioButton(f1, "Yes")
        radiobutton.setLocation(0.0, xy[1])
        radiobutton.setURIAction("http://pdfjet.com")
        radiobutton.select(true)
        xy = radiobutton.drawOn(page)

        let qr = QRCode("https://kazuhikoarase.github.io", ErrorCorrectLevel.L)    // Low
        qr.setModuleLength(3.0)
        qr.setLocation(0.0, xy[1])
        xy = qr.drawOn(page)

        var colors = [String : UInt32]()
        colors["brown"] = Color.brown
        colors["fox"] = Color.maroon
        colors["lazy"] = Color.darkolivegreen
        colors["jumps"] = Color.darkviolet
        colors["dog"] = Color.chocolate
        colors["sight"] = Color.blue

        var buf = String()
        buf.append("The quick brown fox jumps over the lazy dog. What a sight!\n\n")

        var textBox = TextBox(f1, buf)
        textBox.setLocation(0.0, xy[1])
        textBox.setBgColor(Color.whitesmoke)
        textBox.setTextColors(colors)
        xy = textBox.drawOn(page)

        buf = String()
        buf.append(
                "Donec a urna ac ipsum fringilla ultricies non vel diam. Morbi vitae lacus ac elit luctus dignissim.")
        buf.append(" Quisque rutrum egestas facilisis. Curabitur tempus, tortor ac fringilla fringilla,")
        buf.append(" libero elit gravida sem, vel aliquam leo nibh sed libero.")
        buf.append(" Proin pretium, augue quis eleifend hendrerit, leo libero auctor magna,")
        buf.append(" vitae porttitor lorem urna eget urna.")
        buf.append(" Lorem ipsum dolor sit amet, consectetur adipiscing elit.")

        let textBlock = TextBlock(f1)
        textBlock.setText(buf)
        textBlock.setLocation(0.0, xy[1])
        xy = textBlock.drawOn(page)

        var barCode = BarCode(BarCode.CODE128, "Hello, World!")
        barCode.setLocation(0.0, xy[1])
        barCode.setModuleLength(0.75)
        barCode.setFont(f1)
        xy = barCode.drawOn(page)

        buf = String()
        buf.append("Using another font ...\n\nThis is a test.")
        textBox = TextBox(f2, buf)
        textBox.setLocation(0.0, xy[1])
        xy = textBox.drawOn(page)

        barCode = BarCode(BarCode.CODE128, "G86513JVW0C")
        barCode.setLocation(0.0, xy[1])
        barCode.setModuleLength(0.75)
        barCode.setDirection(BarCode.TOP_TO_BOTTOM)
        barCode.setFont(f1)
        xy = barCode.drawOn(page)

        buf = String()
        let text = try String(
                contentsOfFile: "Sources/Example_12/main.swift", encoding: .utf8)
        let lines = text.components(separatedBy: "\n")
        for line in lines {
            if line == "\r" {
                continue
            }
            buf.append(line)
            // Both CR and LF are required by the scanner!
            buf.append(String(13))
            buf.append(String(10))
        }

        let code2D = BarCode2D(buf)
        code2D.setModuleWidth(0.5)
        code2D.setLocation(0.0, xy[1])
        code2D.drawOn(page)

        pdf.complete()
    }
}

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_73()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_73 => \(time1 - time0)")
