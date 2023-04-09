import Foundation
import PDFjet

///
/// Example_21.swift
///
public class Example_21 {
    public init() {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_21.pdf", append: false)!)
        let font = Font(pdf, CoreFont.HELVETICA)

        let page = Page(pdf, Letter.PORTRAIT)

        let text = TextLine(font,
                "QR codes encoded with Low, Medium, High and Very High error correction level - Swift")
        text.setLocation(100.0, 30.0)
        text.drawOn(page)

        // Please note:
        // The higher the error correction level - the shorter the string that you can encode.
        var qr = QRCode(
                "https://kazuhikoarase.github.io/qrcode-generator/js/demo",
                ErrorCorrectLevel.L)    // Low
        qr.setLocation(100.0, 100.0)
        qr.setModuleLength(3.0)
        // qr.setColor(Color.blue)
        qr.drawOn(page);

        qr = QRCode(
                "https://github.com/kazuhikoarase/qrcode-generator",
                ErrorCorrectLevel.M)    // Medium
        qr.setLocation(400.0, 100.0)
        qr.setModuleLength(3.0)
        qr.drawOn(page);

        qr = QRCode(
                "https://github.com/kazuhikoarase/jaconv",
                ErrorCorrectLevel.Q)    // High
        qr.setLocation(100.0, 400.0)
        qr.setModuleLength(3.0)
        qr.drawOn(page)

        qr = QRCode(
                "https://github.com/kazuhikoarase",
                ErrorCorrectLevel.H)    // Very High
        qr.setLocation(400.0, 400.0)
        qr.setModuleLength(3.0)
        qr.drawOn(page)
/*
        let box = Box()
        box.setLocation(xy[0], xy[1])
        box.setSize(20.0, 20.0)
        box.drawOn(page)
*/
        pdf.complete()
    }
}   // End of Example_21.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_21()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_21 => \(time1 - time0)")
