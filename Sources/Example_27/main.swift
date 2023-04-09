import Foundation
import PDFjet

/**
 *  Example_27.swift
 */
public class Example_27 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_27.pdf", append: false)!)
        let page = Page(pdf, Letter.PORTRAIT)

        // Thai font
        let f1 = try Font(pdf, "fonts/Noto/NotoSansThai-Regular.ttf.stream")
        // Latin font
        let f2 = try Font(pdf, "fonts/Droid/DroidSans.ttf.stream")
        f2.setSize(12.0)
        // Hebrew font
        let f3 = try Font(pdf, "fonts/Noto/NotoSansHebrew-Regular.ttf.stream")
        f3.setSize(12.0)
        // Arabic font
        let f4 = try Font(pdf, "fonts/Noto/NotoNaskhArabic-Regular.ttf.stream")

        f1.setSize(14.0)
        f1.setSize(12.0)
        f1.setSize(12.0)
        f4.setSize(12.0)

        let x: Float = 50.0
        var y: Float = 50.0

        let text = TextLine(f1)
        text.setFallbackFont(f2)
        text.setLocation(x, y)

        var scalars = Array<Unicode.Scalar>()
        var i = 0x0E01
        while i < 0x0E5B {
        if i % 16 == 0 {
            y += 24.0
            text.setText(scalarsToString(scalars))
            text.setLocation(x, y)
            text.drawOn(page)
            scalars = [UnicodeScalar]()
        }
        if i > 0x0E30 && i < 0x0E3B {
            scalars.append("\u{0E01}")
        }
        if i > 0x0E46 && i < 0x0E4F {
            scalars.append("\u{0E2D}")
        }
        scalars.append(Unicode.Scalar(i)!)
            i += 1
        }

        y += 20.0
        text.setText(scalarsToString(scalars))
        text.setLocation(x, y)
        text.drawOn(page)

        y += 20.0
        var str = "\u{0E1C}\u{0E1C}\u{0E36}\u{0E49}\u{0E07} abc 123"
        text.setText(str)
        text.setLocation(x, y)
        text.drawOn(page)

        y += 20.0
        str = "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:"
        str = Bidi.reorderVisually(str)
        var textLine = TextLine(f3, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f3.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f3, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f3.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f3, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f3.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f3, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f3.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f3, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f3.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 60.0
        str = Bidi.reorderVisually(
                "قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا")
        textLine = TextLine(f4, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f4.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.")
        textLine = TextLine(f4, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f4.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل")
        textLine = TextLine(f4, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f4.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "من بيجو وسيتروين ودونغفينغ.")
        textLine = TextLine(f4, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f4.stringWidth(f2, str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.")
        textLine = TextLine(f4, str)
        textLine.setFallbackFont(f2)
        textLine.setLocation(600.0 - f4.stringWidth(f2, str), y)
        textLine.drawOn(page)

        pdf.complete()
    }

    private func scalarsToString(_ scalars: [Unicode.Scalar]) -> String {
        var str = ""
        str.unicodeScalars.append(contentsOf: scalars)
        return str
    }
}   // End of Example_27.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_27()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_27 => \(time1 - time0)")
