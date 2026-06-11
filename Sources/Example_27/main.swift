import Foundation
import PDFjet

/**
 * Example_27.swift
 */
public class Example_27 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_27.pdf", append: false)!)

        // Thai font
        let f1 = try Font(pdf, "fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream")
        f1.setSize(12.0)

        // Hebrew font
        let f2 = try Font(pdf, "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf.stream")
        f2.setSize(12.0)

        // Arabic font
        let f3 = try Font(pdf, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf.stream")
        f3.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let textBlock = TextBlock(f1, try Content.ofTextFile("data/languages/thai.txt"))
        textBlock.setLocation(30.0, 30.0)
        textBlock.setWidth(430.0)
        textBlock.setBorderColor(Color.blue)
        textBlock.setTextPadding(10.0)
        let xy = textBlock.drawOn(page)

        let x: Float = 570.0
        var y: Float = xy[1] + 55.0

        var str = "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:"
        str = Bidi.reorderVisually(str)
        var textLine = TextLine(f2, str)
        textLine.setLocation(x - f2.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f2, str)
        textLine.setLocation(x - f2.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f2, str)
        textLine.setLocation(x - f2.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f2, str)
        textLine.setLocation(x - f2.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)"
        str = Bidi.reorderVisually(str)
        textLine = TextLine(f2, str)
        textLine.setLocation(x - f2.stringWidth(str), y)
        textLine.drawOn(page)

        y += 65.0
        str = Bidi.reorderVisually(
                "قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا")
        textLine = TextLine(f3, str)
        textLine.setLocation(x - f3.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.")
        textLine = TextLine(f3, str)
        textLine.setLocation(x - f3.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل")
        textLine = TextLine(f3, str)
        textLine.setLocation(x - f3.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "من بيجو وسيتروين ودونغفينغ.")
        textLine = TextLine(f3, str)
        textLine.setLocation(x - f3.stringWidth(str), y)
        textLine.drawOn(page)

        y += 20.0
        str = Bidi.reorderVisually(
                "وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.")
        textLine = TextLine(f3, str)
        textLine.setLocation(x - f3.stringWidth(str), y)
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
TextUtils.printDuration("Example_27", time0, time1)
