import Foundation
import PDFjet

/**
 * Example_27.swift
 */
public class Example_27 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_27.pdf", append: false)!)

        // Thai font
        // let f1 = try Font(pdf, "fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream")
        let f1 = try Font(pdf, IBMPlexSansThai.Regular)
        f1.setSize(12.0)

        // Hebrew font
        // let f2 = try Font(pdf, "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf.stream")
        let f2 = try Font(pdf, IBMPlexSansHebrew.Regular)
        f2.setSize(12.0)

        // Arabic font
        // let f3 = try Font(pdf, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf.stream")
        let f3 = try Font(pdf, IBMPlexSansArabic.Regular)
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
        y += 20.0
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
        y += 20.0
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

        // Right to left text in text blocks, wrapped at their width.
        let page2 = Page(pdf, Letter.PORTRAIT)

        let hebrewBlock = TextBlock(f2, try Content.ofTextFile("data/languages/hebrew.txt"))
        hebrewBlock.setLocation(180.0, 30.0)
        hebrewBlock.setWidth(400.0)
        hebrewBlock.setBorderColor(Color.blue)
        hebrewBlock.setTextPadding(10.0)
        hebrewBlock.setRightToLeft(true)
        let xy2 = hebrewBlock.drawOn(page2)

        let arabicBlock = TextBlock(f3, try Content.ofTextFile("data/languages/arabic.txt"))
        arabicBlock.setLocation(180.0, xy2[1] + 30.0)
        arabicBlock.setWidth(400.0)
        arabicBlock.setBorderColor(Color.blue)
        arabicBlock.setTextPadding(10.0)
        arabicBlock.setRightToLeft(true)
        let xy3 = arabicBlock.drawOn(page2)

        let persianBlock = TextBlock(f3, try Content.ofTextFile("data/languages/persian.txt"))
        persianBlock.setLocation(180.0, xy3[1] + 30.0)
        persianBlock.setWidth(400.0)
        persianBlock.setBorderColor(Color.blue)
        persianBlock.setTextPadding(10.0)
        persianBlock.setRightToLeft(true)
        persianBlock.drawOn(page2)

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
